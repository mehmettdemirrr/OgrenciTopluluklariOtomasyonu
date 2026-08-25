using Business.Abstract;
using Business.BackgroundJobs;
using Business.Constants;
using Business.DTOs.ClubApplications;
using Core.DataAccess;
using Core.Utilities.Results;
using Core.Utilities.Security;
using Core.Utilities.Time;
using DataAccess.Repositories;
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
    IEntityRepository<AcademicTerm> academicTermRepository,
    IAcademicStaffDal academicStaffDal,
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

        var advisor = await academicStaffRepository.GetAsync(s => s.Id == request.ProposedAdvisorId, cancellationToken).ConfigureAwait(false);
        if (advisor is null)
        {
            return Result.NotFound(Messages.AdvisorNotFound);
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

        var application = new ClubApplication
        {
            StudentId = student.Id,
            AcademicTermId = term.Id,
            ProposedName = proposedName,
            Description = request.Description?.Trim(),
            Justification = request.Justification.Trim(),
            ProposedAdvisorId = advisor.Id,
            Status = ApplicationStatus.Pending,
            AppliedAtUtc = clock.UtcNow,
        };

        await clubApplicationRepository.AddAsync(application, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(Messages.ClubApplicationSubmitted);
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
            Status = a.Status,
            AppliedAtUtc = a.AppliedAtUtc,
            ReviewedAtUtc = a.ReviewedAtUtc,
            ReviewNote = a.ReviewNote,
            CreatedClubId = a.CreatedClubId,
        }).ToList();

        var result = new PagedResult<ClubApplicationListItemDto>(items, paged.TotalCount, paged.PageIndex, paged.PageSize);
        return DataResult<PagedResult<ClubApplicationListItemDto>>.Success(result);
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
                        IsActive = true,
                        CreatedAtUtc = now,
                    };

                    // Club.Id'ye ClubMembership'in düz int FK'si için ihtiyaç var — ara SaveChanges
                    // ile üretilen anahtarı okumak, bu kod tabanında navigation property kullanmamanın
                    // standart çözümü (bkz. FileManager.StoreFileAsync → UploadClubLogoAsync).
                    await clubRepository.AddAsync(club, cancellationToken).ConfigureAwait(false);
                    await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                    var membership = new ClubMembership
                    {
                        ClubId = club.Id,
                        StudentId = application.StudentId,
                        AcademicTermId = application.AcademicTermId,
                        ClubRole = ClubRole.President,
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
            Status = a.Status,
            AppliedAtUtc = a.AppliedAtUtc,
            ReviewedAtUtc = a.ReviewedAtUtc,
            ReviewNote = a.ReviewNote,
            CreatedClubId = a.CreatedClubId,
        }).ToList();
    }

    private static int ClampPageSize(int pageSize) =>
        pageSize <= 0 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);
}
