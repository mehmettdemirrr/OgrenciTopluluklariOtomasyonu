using Business.Abstract;
using Business.BackgroundJobs;
using Business.Constants;
using Business.DTOs.ClubApplications;
using Business.DTOs.Files;
using Core.DataAccess;
using Core.Utilities.Files;
using Core.Utilities.Results;
using Core.Utilities.Security;
using Core.Utilities.Time;
using DataAccess.Repositories;
using DataAccess.Seed;
using Entities;
using Entities.Enums;
using Hangfire;

namespace Business.Concrete;

/// <summary>docs/PLAN-V3.md §17: MembershipApplicationManager'ın kulüp kurma karşılığı.</summary>
public sealed class ClubApplicationManager(
    IEntityRepository<ClubApplication> clubApplicationRepository,
    IEntityRepository<Club> clubRepository,
    IEntityRepository<ClubMembership> clubMembershipRepository,
    IEntityRepository<Student> studentRepository,
    IEntityRepository<AcademicStaff> academicStaffRepository,
    IEntityRepository<ClubCategory> clubCategoryRepository,
    IEntityRepository<AcademicTerm> academicTermRepository,
    IEntityRepository<ClubRoleDefinition> clubRoleDefinitionRepository,
    IEntityRepository<ClubDocumentType> clubDocumentTypeRepository,
    IEntityRepository<ClubApplicationDocument> clubApplicationDocumentRepository,
    IEntityRepository<StoredFile> storedFileRepository,
    IAcademicStaffDal academicStaffDal,
    IFileService fileService,
    IFileStorage fileStorage,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock,
    IBackgroundJobClient backgroundJobClient) : IClubApplicationService
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public async Task<IResult> SubmitAsync(SubmitClubApplicationRequestDto request, CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Result.Unauthorized(Messages.NotAStudent);
        }

        var student = await studentRepository.GetAsync(s => s.ApplicationUserId == userId, cancellationToken).ConfigureAwait(false);
        if (student is null)
        {
            return Result.Forbidden(Messages.NotAStudent);
        }

        var term = await academicTermRepository.GetAsync(t => t.IsCurrent, cancellationToken).ConfigureAwait(false);
        if (term is null)
        {
            return Result.NotFound(Messages.NoCurrentAcademicTerm);
        }

        // Y-73: pencere muhafızı, danışman/ad çakışması/çift başvuru kontrollerinden ÖNCE gelir —
        // kapalı pencerede öğrenci "bu isim alınmış" değil, sebebi doğru olan cevabı almalı.
        // A-66: karar EvaluateWindow'da; ikinci bir if yok (GetWindowAsync da aynı metodu çağırır).
        if (!EvaluateWindow(term, clock.UtcNow))
        {
            return Result.Conflict(Messages.ClubApplicationsClosed);
        }

        var advisor = await academicStaffRepository.GetAsync(s => s.Id == request.ProposedAdvisorId, cancellationToken).ConfigureAwait(false);
        if (advisor is null)
        {
            return Result.NotFound(Messages.AdvisorNotFound);
        }

        // A-60: kategori opsiyonel, ama verilmişse var olmalı — yoksa yetim FK ile başvuru yazılır
        // ve onayda kulüp oluşturma patlar (Restrict). Hata öğrenciye başvuru anında söylenmeli.
        if (request.ProposedCategoryId is { } proposedCategoryId)
        {
            var category = await clubCategoryRepository
                .GetAsync(c => c.Id == proposedCategoryId, cancellationToken)
                .ConfigureAwait(false);
            if (category is null)
            {
                return Result.NotFound(Messages.ClubCategoryNotFound);
            }
        }

        var proposedName = request.ProposedName.Trim();
        var existingClub = await clubRepository.GetAsync(c => c.Name == proposedName, cancellationToken).ConfigureAwait(false);
        if (existingClub is not null)
        {
            return Result.Conflict(Messages.ClubNameTaken);
        }

        var existingApplication = await clubApplicationRepository
            .GetAsync(a => a.StudentId == student.Id && a.AcademicTermId == term.Id && a.Status == ApplicationStatus.Pending, cancellationToken)
            .ConfigureAwait(false);
        if (existingApplication is not null)
        {
            return Result.Conflict(Messages.DuplicatePendingClubApplication);
        }

        // Y-71: bütünlük kontrolü BURADA — katalog DB'den okunur, biçimsel doğrulama değil iş kuralıdır.
        // FluentValidation'a konulamaz: "hangi evrak zorunlu" cevabı veritabanındadır.
        var activeTypes = await clubDocumentTypeRepository
            .GetListAsync(t => t.IsActive, cancellationToken)
            .ConfigureAwait(false);

        var submittedTypeIds = request.Documents.Select(d => d.DocumentTypeId).ToList();

        if (submittedTypeIds.Count != submittedTypeIds.Distinct().Count())
        {
            return Result.ValidationError(Messages.DuplicateClubDocumentUpload);
        }

        var activeTypeIds = activeTypes.Select(t => t.Id).ToHashSet();
        if (submittedTypeIds.Any(id => !activeTypeIds.Contains(id)))
        {
            return Result.ValidationError(Messages.UnknownClubDocumentType);
        }

        var requiredTypeIds = activeTypes.Where(t => t.IsRequired).Select(t => t.Id).ToList();
        if (requiredTypeIds.Any(id => !submittedTypeIds.Contains(id)))
        {
            return Result.ValidationError(Messages.MissingRequiredClubDocuments);
        }

        var application = new ClubApplication
        {
            StudentId = student.Id,
            AcademicTermId = term.Id,
            ProposedName = proposedName,
            Description = request.Description?.Trim(),
            Justification = request.Justification.Trim(),
            ProposedAdvisorId = advisor.Id,
            ProposedCategoryId = request.ProposedCategoryId,
            Status = ApplicationStatus.Pending,
            AppliedAtUtc = clock.UtcNow,
        };

        await clubApplicationRepository.AddAsync(application, cancellationToken).ConfigureAwait(false);
        // Başvuru kimliği ClubApplicationDocument'in düz int FK'si için gerekli — ara SaveChanges,
        // bu kod tabanında navigation property kullanmamanın standart çözümü (DecideAsync precedent'i).
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        foreach (var document in request.Documents)
        {
            // A-63/A-64: Protected + yalnızca PDF. Tip hatası burada yakalanır ve başvuru
            // [TransactionAspect] sayesinde geri alınır — yarım kayıt kalmaz (O-22 atomiklik).
            var stored = await fileService.StoreApplicationDocumentAsync(document.File, cancellationToken).ConfigureAwait(false);
            if (!stored.IsSuccess)
            {
                return Result.ValidationError(stored.Message ?? Messages.UnsupportedDocumentFileType);
            }

            await clubApplicationDocumentRepository.AddAsync(
                new ClubApplicationDocument
                {
                    ClubApplicationId = application.Id,
                    ClubDocumentTypeId = document.DocumentTypeId,
                    StoredFileId = stored.Data.FileId,
                },
                cancellationToken).ConfigureAwait(false);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(Messages.ClubApplicationSubmitted);
    }

    public async Task<IDataResult<ClubApplicationWindowDto>> GetWindowAsync(CancellationToken cancellationToken = default)
    {
        var term = await academicTermRepository.GetAsync(t => t.IsCurrent, cancellationToken).ConfigureAwait(false);

        // Güncel dönem yoksa pencere kapalıdır — SubmitAsync zaten NoCurrentAcademicTerm ile durur.
        // Burada hata değil "kapalı" dönüyoruz: arayüz düğmeyi pasif gösterip sebebi yazabilsin.
        if (term is null)
        {
            return DataResult<ClubApplicationWindowDto>.Success(new ClubApplicationWindowDto
            {
                IsOpen = false,
                Override = ClubApplicationWindowOverride.FollowSchedule,
                TermName = string.Empty,
            });
        }

        return DataResult<ClubApplicationWindowDto>.Success(new ClubApplicationWindowDto
        {
            IsOpen = EvaluateWindow(term, clock.UtcNow),
            StartUtc = term.ClubApplicationStartUtc,
            EndUtc = term.ClubApplicationEndUtc,
            Override = term.ClubApplicationOverride,
            TermName = term.Name,
        });
    }

    public async Task<IDataResult<IReadOnlyList<ClubApplicationListItemDto>>> GetMineAsync(CancellationToken cancellationToken = default)
    {
        var student = currentUser.UserId is { } userId
            ? await studentRepository.GetAsync(s => s.ApplicationUserId == userId, cancellationToken).ConfigureAwait(false)
            : null;

        if (student is null)
        {
            return DataResult<IReadOnlyList<ClubApplicationListItemDto>>.Success([]);
        }

        var applications = await clubApplicationRepository
            .GetListAsync(a => a.StudentId == student.Id, cancellationToken)
            .ConfigureAwait(false);

        var items = await MapWithAdvisorNamesAsync(applications, student.StudentNumber, cancellationToken).ConfigureAwait(false);
        await AttachDocumentsAsync(items, cancellationToken).ConfigureAwait(false);

        return DataResult<IReadOnlyList<ClubApplicationListItemDto>>.Success(
            items.OrderByDescending(a => a.AppliedAtUtc).ToList());
    }

    public async Task<IDataResult<PagedResult<ClubApplicationListItemDto>>> GetPendingAsync(
        int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        var clampedPageSize = ClampPageSize(pageSize);

        var paged = await clubApplicationRepository
            .GetListPagedAsync(pageIndex, clampedPageSize, a => a.Status == ApplicationStatus.Pending, cancellationToken)
            .ConfigureAwait(false);

        var studentIds = paged.Items.Select(a => a.StudentId).Distinct().ToList();
        var studentNumbers = (await studentRepository.GetListAsync(s => studentIds.Contains(s.Id), cancellationToken).ConfigureAwait(false))
            .ToDictionary(s => s.Id, s => s.StudentNumber);

        // A-56: ad soyad Identity'de yaşadığı için generic repository ile kurulamaz — DAL join'ler.
        var advisorIds = paged.Items.Select(a => a.ProposedAdvisorId).Distinct().ToList();
        var advisorNames = await academicStaffDal.GetDisplayNamesAsync(advisorIds, cancellationToken).ConfigureAwait(false);
        var categoryNames = await GetCategoryNamesAsync(paged.Items.Where(a => a.ProposedCategoryId is not null).Select(a => a.ProposedCategoryId!.Value), cancellationToken).ConfigureAwait(false);

        var items = paged.Items.Select(a => new ClubApplicationListItemDto
        {
            Id = a.Id,
            StudentId = a.StudentId,
            StudentNumber = studentNumbers.GetValueOrDefault(a.StudentId, string.Empty),
            ProposedName = a.ProposedName,
            Description = a.Description,
            Justification = a.Justification,
            ProposedAdvisorId = a.ProposedAdvisorId,
            ProposedAdvisorDisplayName = advisorNames.GetValueOrDefault(a.ProposedAdvisorId, string.Empty),
            ProposedCategoryId = a.ProposedCategoryId,
            ProposedCategoryName = a.ProposedCategoryId is { } cid ? categoryNames.GetValueOrDefault(cid) : null,
            Status = a.Status,
            AppliedAtUtc = a.AppliedAtUtc,
            ReviewedAtUtc = a.ReviewedAtUtc,
            ReviewNote = a.ReviewNote,
            CreatedClubId = a.CreatedClubId,
        }).ToList();

        await AttachDocumentsAsync(items, cancellationToken).ConfigureAwait(false);

        var result = new PagedResult<ClubApplicationListItemDto>(items, paged.TotalCount, paged.PageIndex, paged.PageSize);
        return DataResult<PagedResult<ClubApplicationListItemDto>>.Success(result);
    }

    public async Task<IDataResult<FileContentDto>> GetDocumentAsync(
        int applicationId, int documentId, CancellationToken cancellationToken = default)
    {
        var document = await clubApplicationDocumentRepository
            .GetAsync(d => d.Id == documentId && d.ClubApplicationId == applicationId, cancellationToken)
            .ConfigureAwait(false);
        if (document is null)
        {
            return DataResult<FileContentDto>.NotFound(Messages.FileNotFound);
        }

        var application = await clubApplicationRepository
            .GetAsync(a => a.Id == applicationId, cancellationToken)
            .ConfigureAwait(false);
        if (application is null)
        {
            return DataResult<FileContentDto>.NotFound(Messages.ClubApplicationNotFound);
        }

        // Y-51/Y-70: yetki İNDİRME ANINDA yeniden kontrol edilir — başvuru kuyrukta beklerken
        // öğrencinin veya inceleyicinin yetkisi değişmiş olabilir.
        var accessError = await EnsureDocumentAccessAsync(application, cancellationToken).ConfigureAwait(false);
        if (accessError is not null)
        {
            return DataResult<FileContentDto>.Forbidden(accessError);
        }

        var file = await storedFileRepository
            .GetAsync(f => f.Id == document.StoredFileId, cancellationToken)
            .ConfigureAwait(false);
        if (file is null)
        {
            return DataResult<FileContentDto>.NotFound(Messages.FileNotFound);
        }

        var stream = await fileStorage.OpenReadAsync(file.GeneratedFileName, cancellationToken).ConfigureAwait(false);
        if (stream is null)
        {
            return DataResult<FileContentDto>.NotFound(Messages.FileNotFound);
        }

        return DataResult<FileContentDto>.Success(new FileContentDto
        {
            Content = stream,
            ContentType = file.ContentType,
            DownloadFileName = file.OriginalFileName,
        });
    }

    /// <summary>
    /// Y-23/Y-70: izin claim'i tek başına yetmez. İnceleyici (clubs.manage.all / clubs.write) VEYA
    /// başvurunun sahibi öğrenci görebilir. Y-66: yönetici kontrolü ilk satırda.
    /// </summary>
    private async Task<string?> EnsureDocumentAccessAsync(ClubApplication application, CancellationToken cancellationToken)
    {
        if (currentUser.Permissions.Contains(IdentitySeedData.Permissions.ClubsManageAll)
            || currentUser.Permissions.Contains(IdentitySeedData.Permissions.ClubsWrite))
        {
            return null;
        }

        if (currentUser.UserId is not { } userId)
        {
            return Messages.ClubApplicationDocumentForbidden;
        }

        var student = await studentRepository
            .GetAsync(s => s.ApplicationUserId == userId, cancellationToken)
            .ConfigureAwait(false);

        return student is not null && student.Id == application.StudentId
            ? null
            : Messages.ClubApplicationDocumentForbidden;
    }

    /// <summary>Y-10: evraklar tek toplu sorguyla — başvuru başına sorgu N+1 üretirdi.</summary>
    private async Task AttachDocumentsAsync(
        List<ClubApplicationListItemDto> items, CancellationToken cancellationToken)
    {
        if (items.Count == 0)
        {
            return;
        }

        var applicationIds = items.Select(i => i.Id).ToList();
        var links = await clubApplicationDocumentRepository
            .GetListAsync(d => applicationIds.Contains(d.ClubApplicationId), cancellationToken)
            .ConfigureAwait(false);

        if (links.Count == 0)
        {
            return;
        }

        var typeIds = links.Select(d => d.ClubDocumentTypeId).Distinct().ToList();
        var typesById = (await clubDocumentTypeRepository.GetListAsync(t => typeIds.Contains(t.Id), cancellationToken).ConfigureAwait(false))
            .ToDictionary(t => t.Id, t => t);

        var fileIds = links.Select(d => d.StoredFileId).Distinct().ToList();
        var filesById = (await storedFileRepository.GetListAsync(f => fileIds.Contains(f.Id), cancellationToken).ConfigureAwait(false))
            .ToDictionary(f => f.Id, f => f);

        var documentsByApplication = links
            .GroupBy(d => d.ClubApplicationId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<ClubApplicationDocumentDto>)g.Select(d =>
                    {
                        typesById.TryGetValue(d.ClubDocumentTypeId, out var type);
                        filesById.TryGetValue(d.StoredFileId, out var file);
                        return new ClubApplicationDocumentDto
                        {
                            DocumentId = d.Id,
                            DocumentTypeId = d.ClubDocumentTypeId,
                            Code = type?.Code ?? string.Empty,
                            Name = type?.Name ?? string.Empty,
                            IsRequired = type?.IsRequired ?? false,
                            OriginalFileName = file?.OriginalFileName ?? string.Empty,
                            FileSizeBytes = file?.FileSizeBytes ?? 0,
                        };
                    })
                    // Y-64: belirleyici sıra — kurumsal form kodu.
                    .OrderBy(d => d.Code, StringComparer.Ordinal)
                    .ToList());

        foreach (var item in items)
        {
            item.Documents = documentsByApplication.GetValueOrDefault(item.Id, []);
        }
    }

    public async Task<IResult> DecideAsync(int applicationId, DecideClubApplicationRequestDto request, CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Result.Forbidden(Messages.ClubApplicationNotFound);
        }

        var application = await clubApplicationRepository.GetAsync(a => a.Id == applicationId, cancellationToken).ConfigureAwait(false);
        if (application is null)
        {
            return Result.NotFound(Messages.ClubApplicationNotFound);
        }

        if (application.Status != ApplicationStatus.Pending)
        {
            return Result.Conflict(Messages.ApplicationAlreadyReviewed);
        }

        if (request.Status == ApplicationStatus.Approved)
        {
            var nameTaken = await clubRepository.GetAsync(c => c.Name == application.ProposedName, cancellationToken).ConfigureAwait(false);
            if (nameTaken is not null)
            {
                return Result.Conflict(Messages.ClubNameTaken);
            }

            var advisor = await academicStaffRepository.GetAsync(s => s.Id == application.ProposedAdvisorId, cancellationToken).ConfigureAwait(false);
            if (advisor is null)
            {
                return Result.NotFound(Messages.AdvisorNotFound);
            }
        }

        var now = clock.UtcNow;

        // Y-46/Y-06: [TransactionAspect] kasıtlı olarak kullanılmaz — commit'ten SONRA Hangfire'a
        // kuyruğa ekleme yapılabilmesi için transaction burada elle yönetilir (ReviewAsync precedent'i).
        var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        await using (transaction.ConfigureAwait(false))
        {
            try
            {
                if (request.Status == ApplicationStatus.Approved)
                {
                    var club = new Club
                    {
                        Name = application.ProposedName,
                        Description = application.Description,
                        AdvisorId = application.ProposedAdvisorId,
                        ClubCategoryId = application.ProposedCategoryId,
                        IsActive = true,
                        CreatedAtUtc = now,
                    };

                    // Club.Id'ye ClubMembership'in düz int FK'si için ihtiyaç var — ara SaveChanges
                    // ile üretilen anahtarı okumak, bu kod tabanında navigation property kullanmamanın
                    // standart çözümü (bkz. FileManager.StoreFileAsync → UploadClubLogoAsync).
                    await clubRepository.AddAsync(club, cancellationToken).ConfigureAwait(false);
                    await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                    // O-20: onayla doğan kulüp de varsayılan unvan setini alır (ClubManager.CreateAsync ile aynı).
                    foreach (var (roleName, role, capabilities, displayOrder) in DefaultClubRoles.All)
                    {
                        await clubRoleDefinitionRepository.AddAsync(
                            new ClubRoleDefinition
                            {
                                ClubId = club.Id, Name = roleName, ClubRole = role,
                                Capabilities = capabilities, DisplayOrder = displayOrder,
                            },
                            cancellationToken).ConfigureAwait(false);
                    }

                    var membership = new ClubMembership
                    {
                        ClubId = club.Id,
                        StudentId = application.StudentId,
                        AcademicTermId = application.AcademicTermId,
                        ClubRole = ClubRole.President,
                        // A-68: kapasite makamla birlikte yazılır, yoksa yeni başkan hiçbir şey yapamaz.
                        Capabilities = ClubCapabilityDefaults.ForRole(ClubRole.President),
                        JoinedAtUtc = now,
                    };
                    await clubMembershipRepository.AddAsync(membership, cancellationToken).ConfigureAwait(false);

                    application.CreatedClubId = club.Id;
                }

                application.Status = request.Status;
                application.ReviewedAtUtc = now;
                application.ReviewedByUserId = userId;
                application.ReviewNote = request.ReviewNote?.Trim();
                clubApplicationRepository.Update(application);

                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                throw;
            }
        }

        // Y-46: kuyruğa ekleme yalnızca commit'ten sonra — worker henüz var olmayan kaydı aramasın.
        backgroundJobClient.Enqueue<ClubApplicationDecisionNotificationJob>(job => job.SendAsync(application.Id));

        return Result.Success(request.Status == ApplicationStatus.Approved ? Messages.ClubApplicationApproved : Messages.ClubApplicationRejected);
    }

    private async Task<List<ClubApplicationListItemDto>> MapWithAdvisorNamesAsync(
        List<ClubApplication> applications, string studentNumber, CancellationToken cancellationToken)
    {
        var advisorIds = applications.Select(a => a.ProposedAdvisorId).Distinct().ToList();
        var advisorNames = await academicStaffDal.GetDisplayNamesAsync(advisorIds, cancellationToken).ConfigureAwait(false);
        var categoryNames = await GetCategoryNamesAsync(applications.Where(a => a.ProposedCategoryId is not null).Select(a => a.ProposedCategoryId!.Value), cancellationToken).ConfigureAwait(false);

        return applications.Select(a => new ClubApplicationListItemDto
        {
            Id = a.Id,
            StudentId = a.StudentId,
            StudentNumber = studentNumber,
            ProposedName = a.ProposedName,
            Description = a.Description,
            Justification = a.Justification,
            ProposedAdvisorId = a.ProposedAdvisorId,
            ProposedAdvisorDisplayName = advisorNames.GetValueOrDefault(a.ProposedAdvisorId, string.Empty),
            ProposedCategoryId = a.ProposedCategoryId,
            ProposedCategoryName = a.ProposedCategoryId is { } cid ? categoryNames.GetValueOrDefault(cid) : null,
            Status = a.Status,
            AppliedAtUtc = a.AppliedAtUtc,
            ReviewedAtUtc = a.ReviewedAtUtc,
            ReviewNote = a.ReviewNote,
            CreatedClubId = a.CreatedClubId,
        }).ToList();
    }

    /// <summary>
    /// docs/MIMARI.md · A-66/Y-73: pencere kararının TEK yeri. SubmitAsync muhafızı ve
    /// GetWindowAsync bu metodu çağırır — ikinci bir if yazmak, ekranın "açık" derken API'nin
    /// "kapalı" demesinin garantili yoludur.
    /// Fail-closed: FollowSchedule + eksik tarih = kapalı. Aralık kapsayıcıdır.
    /// </summary>
    private static bool EvaluateWindow(AcademicTerm term, DateTime nowUtc) => term.ClubApplicationOverride switch
    {
        ClubApplicationWindowOverride.ForceOpen => true,
        ClubApplicationWindowOverride.ForceClosed => false,
        _ => term.ClubApplicationStartUtc is { } start
             && term.ClubApplicationEndUtc is { } end
             && nowUtc >= start
             && nowUtc <= end,
    };

    /// <summary>Y-10: tek toplu sorgu — satır başına sorgu N+1 üretirdi.</summary>
    private async Task<Dictionary<int, string>> GetCategoryNamesAsync(
        IEnumerable<int> categoryIds, CancellationToken cancellationToken)
    {
        var ids = categoryIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return [];
        }

        return (await clubCategoryRepository.GetListAsync(c => ids.Contains(c.Id), cancellationToken).ConfigureAwait(false))
            .ToDictionary(c => c.Id, c => c.Name);
    }

    private static int ClampPageSize(int pageSize) =>
        pageSize <= 0 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);
}
