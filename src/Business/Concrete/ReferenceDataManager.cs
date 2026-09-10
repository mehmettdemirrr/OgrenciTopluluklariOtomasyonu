using Business.Abstract;
using Business.Constants;
using Business.DTOs.Files;
using Business.DTOs.Reference;
using Core.DataAccess;
using Core.Utilities.Results;
using DataAccess.Repositories;
using Entities;

namespace Business.Concrete;

public sealed class ReferenceDataManager(
    IEntityRepository<Faculty> facultyRepository,
    IEntityRepository<Department> departmentRepository,
    IEntityRepository<AcademicStaff> academicStaffRepository,
    IEntityRepository<Club> clubRepository,
    IEntityRepository<ClubCategory> clubCategoryRepository,
    IEntityRepository<ClubDocumentType> clubDocumentTypeRepository,
    IAcademicStaffDal academicStaffDal,
    IFileService fileService,
    IUnitOfWork unitOfWork) : IReferenceDataService
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public async Task<IDataResult<PagedResult<FacultyListItemDto>>> GetFacultiesPagedAsync(
        int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        var paged = await facultyRepository
            .GetListPagedAsync(pageIndex, ClampPageSize(pageSize), cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        var items = paged.Items.Select(f => new FacultyListItemDto { Id = f.Id, Name = f.Name }).ToList();
        return DataResult<PagedResult<FacultyListItemDto>>.Success(new PagedResult<FacultyListItemDto>(items, paged.TotalCount, paged.PageIndex, paged.PageSize));
    }

    public async Task<IDataResult<PagedResult<DepartmentListItemDto>>> GetDepartmentsPagedAsync(
        int facultyId, int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        var paged = await departmentRepository
            .GetListPagedAsync(pageIndex, ClampPageSize(pageSize), d => d.FacultyId == facultyId, cancellationToken)
            .ConfigureAwait(false);

        var items = paged.Items.Select(d => new DepartmentListItemDto { Id = d.Id, Name = d.Name, FacultyId = d.FacultyId }).ToList();
        return DataResult<PagedResult<DepartmentListItemDto>>.Success(
            new PagedResult<DepartmentListItemDto>(items, paged.TotalCount, paged.PageIndex, paged.PageSize));
    }

    public async Task<IDataResult<FacultyListItemDto>> CreateFacultyAsync(CreateFacultyRequestDto request, CancellationToken cancellationToken = default)
    {
        var name = request.Name.Trim();

        var existing = await facultyRepository.GetAsync(f => f.Name == name, cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            return DataResult<FacultyListItemDto>.Success(
                new FacultyListItemDto { Id = existing.Id, Name = existing.Name }, Messages.FacultyAlreadyExists);
        }

        var faculty = new Faculty { Name = name };
        await facultyRepository.AddAsync(faculty, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return DataResult<FacultyListItemDto>.Success(new FacultyListItemDto { Id = faculty.Id, Name = faculty.Name });
    }

    public async Task<IDataResult<DepartmentListItemDto>> CreateDepartmentAsync(
        int facultyId, CreateDepartmentRequestDto request, CancellationToken cancellationToken = default)
    {
        var faculty = await facultyRepository.GetAsync(f => f.Id == facultyId, cancellationToken).ConfigureAwait(false);
        if (faculty is null)
        {
            return DataResult<DepartmentListItemDto>.NotFound(Messages.FacultyNotFound);
        }

        var name = request.Name.Trim();
        var existing = await departmentRepository
            .GetAsync(d => d.FacultyId == facultyId && d.Name == name, cancellationToken)
            .ConfigureAwait(false);
        if (existing is not null)
        {
            return DataResult<DepartmentListItemDto>.Success(
                new DepartmentListItemDto { Id = existing.Id, Name = existing.Name, FacultyId = existing.FacultyId }, Messages.DepartmentAlreadyExists);
        }

        var department = new Department { Name = name, FacultyId = facultyId };
        await departmentRepository.AddAsync(department, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return DataResult<DepartmentListItemDto>.Success(new DepartmentListItemDto { Id = department.Id, Name = department.Name, FacultyId = department.FacultyId });
    }

    public async Task<IResult> UpdateFacultyAsync(int id, UpdateFacultyRequestDto request, CancellationToken cancellationToken = default)
    {
        var faculty = await facultyRepository.GetAsync(f => f.Id == id, cancellationToken).ConfigureAwait(false);
        if (faculty is null)
        {
            return Result.NotFound(Messages.FacultyNotFound);
        }

        var name = request.Name.Trim();
        var duplicate = await facultyRepository.GetAsync(f => f.Id != id && f.Name == name, cancellationToken).ConfigureAwait(false);
        if (duplicate is not null)
        {
            return Result.Conflict(Messages.FacultyAlreadyExists);
        }

        faculty.Name = name;
        facultyRepository.Update(faculty);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(Messages.FacultyUpdated);
    }

    public async Task<IResult> UpdateDepartmentAsync(
        int facultyId, int departmentId, UpdateDepartmentRequestDto request, CancellationToken cancellationToken = default)
    {
        var department = await departmentRepository
            .GetAsync(d => d.Id == departmentId && d.FacultyId == facultyId, cancellationToken)
            .ConfigureAwait(false);
        if (department is null)
        {
            return Result.NotFound(Messages.DepartmentNotFound);
        }

        var name = request.Name.Trim();
        var duplicate = await departmentRepository
            .GetAsync(d => d.Id != departmentId && d.FacultyId == facultyId && d.Name == name, cancellationToken)
            .ConfigureAwait(false);
        if (duplicate is not null)
        {
            return Result.Conflict(Messages.DepartmentAlreadyExists);
        }

        department.Name = name;
        departmentRepository.Update(department);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(Messages.DepartmentUpdated);
    }

    public async Task<IResult> DeleteDepartmentAsync(int facultyId, int departmentId, CancellationToken cancellationToken = default)
    {
        var department = await departmentRepository
            .GetAsync(d => d.Id == departmentId && d.FacultyId == facultyId, cancellationToken)
            .ConfigureAwait(false);
        if (department is null)
        {
            return Result.NotFound(Messages.DepartmentNotFound);
        }

        departmentRepository.Delete(department);
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (ReferentialIntegrityConflictException)
        {
            return Result.Conflict(Messages.DepartmentInUse);
        }

        return Result.Success(Messages.DepartmentDeleted);
    }

    public async Task<IDataResult<PagedResult<ClubCategoryListItemDto>>> GetClubCategoriesPagedAsync(
        int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        // Y-64: ada göre artan, Id son kırıcı — repository sözleşmesi ThenBy(Id) uygular.
        var paged = await clubCategoryRepository
            .GetListPagedAsync(pageIndex, ClampPageSize(pageSize), c => true, c => c.Name, descending: false, cancellationToken)
            .ConfigureAwait(false);

        var items = paged.Items.Select(c => new ClubCategoryListItemDto { Id = c.Id, Name = c.Name }).ToList();
        return DataResult<PagedResult<ClubCategoryListItemDto>>.Success(
            new PagedResult<ClubCategoryListItemDto>(items, paged.TotalCount, paged.PageIndex, paged.PageSize));
    }

    public async Task<IDataResult<ClubCategoryListItemDto>> CreateClubCategoryAsync(
        CreateClubCategoryRequestDto request, CancellationToken cancellationToken = default)
    {
        var name = request.Name.Trim();

        // CreateFacultyAsync precedent'i: çakışma hata değil, mevcut satırı döndürmek.
        var existing = await clubCategoryRepository.GetAsync(c => c.Name == name, cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            return DataResult<ClubCategoryListItemDto>.Success(
                new ClubCategoryListItemDto { Id = existing.Id, Name = existing.Name }, Messages.ClubCategoryAlreadyExists);
        }

        var category = new ClubCategory { Name = name };
        await clubCategoryRepository.AddAsync(category, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return DataResult<ClubCategoryListItemDto>.Success(
            new ClubCategoryListItemDto { Id = category.Id, Name = category.Name });
    }

    public async Task<IResult> UpdateClubCategoryAsync(
        int categoryId, UpdateClubCategoryRequestDto request, CancellationToken cancellationToken = default)
    {
        var category = await clubCategoryRepository.GetAsync(c => c.Id == categoryId, cancellationToken).ConfigureAwait(false);
        if (category is null)
        {
            return Result.NotFound(Messages.ClubCategoryNotFound);
        }

        var name = request.Name.Trim();
        var duplicate = await clubCategoryRepository
            .GetAsync(c => c.Id != categoryId && c.Name == name, cancellationToken)
            .ConfigureAwait(false);
        if (duplicate is not null)
        {
            return Result.Conflict(Messages.ClubCategoryAlreadyExists);
        }

        category.Name = name;
        clubCategoryRepository.Update(category);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(Messages.ClubCategoryUpdated);
    }

    public async Task<IResult> DeleteClubCategoryAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        var category = await clubCategoryRepository.GetAsync(c => c.Id == categoryId, cancellationToken).ConfigureAwait(false);
        if (category is null)
        {
            return Result.NotFound(Messages.ClubCategoryNotFound);
        }

        clubCategoryRepository.Delete(category);
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (ReferentialIntegrityConflictException)
        {
            // A-60: kulüp veya başvuru bu kategoriye bağlıysa FK Restrict devreye girer.
            return Result.Conflict(Messages.ClubCategoryInUse);
        }

        return Result.Success(Messages.ClubCategoryDeleted);
    }

    public async Task<IDataResult<PagedResult<ClubDocumentTypeListItemDto>>> GetClubDocumentTypesPagedAsync(
        int pageIndex, int pageSize, bool activeOnly = false, CancellationToken cancellationToken = default)
    {
        // Y-64: DisplayOrder'a göre artan — form alanlarının sırası kurumun kararı, alfabetik değil.
        var paged = await clubDocumentTypeRepository
            .GetListPagedAsync(
                pageIndex, ClampPageSize(pageSize),
                t => !activeOnly || t.IsActive,
                t => t.DisplayOrder, descending: false, cancellationToken)
            .ConfigureAwait(false);

        var items = paged.Items.Select(ToDocumentTypeDto).ToList();
        return DataResult<PagedResult<ClubDocumentTypeListItemDto>>.Success(
            new PagedResult<ClubDocumentTypeListItemDto>(items, paged.TotalCount, paged.PageIndex, paged.PageSize));
    }

    public async Task<IDataResult<ClubDocumentTypeListItemDto>> CreateClubDocumentTypeAsync(
        CreateClubDocumentTypeRequestDto request, CancellationToken cancellationToken = default)
    {
        var code = request.Code.Trim();

        // Kategoriden farklı: kod kurumsal bir kimliktir. Sessizce mevcut satıra düşmek (CreateClubCategoryAsync
        // deseni) burada yanlış olurdu — yönetici hangi kodun alınmış olduğunu bilmeli.
        var existing = await clubDocumentTypeRepository.GetAsync(t => t.Code == code, cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            return DataResult<ClubDocumentTypeListItemDto>.Conflict(Messages.ClubDocumentTypeCodeTaken);
        }

        var documentType = new ClubDocumentType
        {
            Code = code,
            Name = request.Name.Trim(),
            IsRequired = request.IsRequired,
            IsActive = request.IsActive,
            DisplayOrder = request.DisplayOrder,
        };

        await clubDocumentTypeRepository.AddAsync(documentType, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return DataResult<ClubDocumentTypeListItemDto>.Success(ToDocumentTypeDto(documentType));
    }

    public async Task<IResult> UpdateClubDocumentTypeAsync(
        int documentTypeId, UpdateClubDocumentTypeRequestDto request, CancellationToken cancellationToken = default)
    {
        var documentType = await clubDocumentTypeRepository
            .GetAsync(t => t.Id == documentTypeId, cancellationToken).ConfigureAwait(false);
        if (documentType is null)
        {
            return Result.NotFound(Messages.ClubDocumentTypeNotFound);
        }

        var code = request.Code.Trim();
        var duplicate = await clubDocumentTypeRepository
            .GetAsync(t => t.Id != documentTypeId && t.Code == code, cancellationToken).ConfigureAwait(false);
        if (duplicate is not null)
        {
            return Result.Conflict(Messages.ClubDocumentTypeCodeTaken);
        }

        documentType.Code = code;
        documentType.Name = request.Name.Trim();
        documentType.IsRequired = request.IsRequired;
        documentType.IsActive = request.IsActive;
        documentType.DisplayOrder = request.DisplayOrder;

        clubDocumentTypeRepository.Update(documentType);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(Messages.ClubDocumentTypeUpdated);
    }

    public async Task<IResult> DeleteClubDocumentTypeAsync(int documentTypeId, CancellationToken cancellationToken = default)
    {
        var documentType = await clubDocumentTypeRepository
            .GetAsync(t => t.Id == documentTypeId, cancellationToken).ConfigureAwait(false);
        if (documentType is null)
        {
            return Result.NotFound(Messages.ClubDocumentTypeNotFound);
        }

        clubDocumentTypeRepository.Delete(documentType);
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (ReferentialIntegrityConflictException)
        {
            // A-62: başvuruya yüklenmiş bir evrak bu tipe bağlıysa FK Restrict devreye girer.
            return Result.Conflict(Messages.ClubDocumentTypeInUse);
        }

        return Result.Success(Messages.ClubDocumentTypeDeleted);
    }

    public async Task<IDataResult<ClubDocumentTypeListItemDto>> UploadClubDocumentTemplateAsync(
        int documentTypeId, UploadFileRequestDto request, CancellationToken cancellationToken = default)
    {
        var documentType = await clubDocumentTypeRepository
            .GetAsync(t => t.Id == documentTypeId, cancellationToken).ConfigureAwait(false);
        if (documentType is null)
        {
            return DataResult<ClubDocumentTypeListItemDto>.NotFound(Messages.ClubDocumentTypeNotFound);
        }

        var stored = await fileService.StoreDocumentTemplateAsync(request, cancellationToken).ConfigureAwait(false);
        if (!stored.IsSuccess)
        {
            return DataResult<ClubDocumentTypeListItemDto>.ValidationError(
                stored.Message ?? Messages.UnsupportedDocumentTemplateType);
        }

        documentType.TemplateFileId = stored.Data.FileId;
        clubDocumentTypeRepository.Update(documentType);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return DataResult<ClubDocumentTypeListItemDto>.Success(
            ToDocumentTypeDto(documentType), Messages.ClubDocumentTypeTemplateUploaded);
    }

    public async Task<IDataResult<FileContentDto>> GetClubDocumentTemplateAsync(
        int documentTypeId, CancellationToken cancellationToken = default)
    {
        var documentType = await clubDocumentTypeRepository
            .GetAsync(t => t.Id == documentTypeId, cancellationToken).ConfigureAwait(false);
        if (documentType is null)
        {
            return DataResult<FileContentDto>.NotFound(Messages.ClubDocumentTypeNotFound);
        }

        if (documentType.TemplateFileId is not { } templateFileId)
        {
            return DataResult<FileContentDto>.NotFound(Messages.ClubDocumentTypeTemplateNotFound);
        }

        var file = await fileService.GetPublicFileAsync(templateFileId, cancellationToken).ConfigureAwait(false);
        if (!file.IsSuccess)
        {
            return DataResult<FileContentDto>.NotFound(Messages.ClubDocumentTypeTemplateNotFound);
        }

        return file;
    }

    private static ClubDocumentTypeListItemDto ToDocumentTypeDto(ClubDocumentType t) => new()
    {
        Id = t.Id,
        Code = t.Code,
        Name = t.Name,
        IsRequired = t.IsRequired,
        IsActive = t.IsActive,
        DisplayOrder = t.DisplayOrder,
        TemplateFileId = t.TemplateFileId,
    };

    public async Task<IDataResult<PagedResult<AcademicStaffListItemDto>>> GetAcademicStaffPagedAsync(
        int pageIndex, int pageSize, string? search = null, CancellationToken cancellationToken = default)
    {
        var paged = await academicStaffDal
            .GetListPagedAsync(pageIndex, ClampPageSize(pageSize), SearchTerm.Normalize(search), cancellationToken)
            .ConfigureAwait(false);

        var items = paged.Items
            .Select(s => new AcademicStaffListItemDto
            {
                Id = s.Id,
                Title = s.Title,
                Email = s.Email,
                FirstName = s.FirstName,
                LastName = s.LastName,
            })
            .ToList();
        return DataResult<PagedResult<AcademicStaffListItemDto>>.Success(
            new PagedResult<AcademicStaffListItemDto>(items, paged.TotalCount, paged.PageIndex, paged.PageSize));
    }

    public async Task<IDataResult<PagedResult<SelectableAcademicStaffDto>>> GetSelectableAcademicStaffAsync(
        int pageIndex, int pageSize, string? search = null, CancellationToken cancellationToken = default)
    {
        var paged = await academicStaffDal
            .GetListPagedAsync(pageIndex, ClampPageSize(pageSize), SearchTerm.Normalize(search), cancellationToken)
            .ConfigureAwait(false);

        // A-56: e-posta bu uçtan çıktı — ad soyad boşsa (eski kayıt) seçici kullanılamaz olmasın diye düşülür.
        var items = paged.Items
            .Select(s => new SelectableAcademicStaffDto
            {
                Id = s.Id,
                Title = s.Title,
                FullName = BuildFullName(s.FirstName, s.LastName) ?? s.Email,
            })
            .ToList();
        return DataResult<PagedResult<SelectableAcademicStaffDto>>.Success(
            new PagedResult<SelectableAcademicStaffDto>(items, paged.TotalCount, paged.PageIndex, paged.PageSize));
    }

    public async Task<IDataResult<int>> CreateAcademicStaffAsync(
        CreateAcademicStaffRequestDto request, CancellationToken cancellationToken = default)
    {
        var department = await departmentRepository.GetAsync(d => d.Id == request.DepartmentId, cancellationToken).ConfigureAwait(false);
        if (department is null)
        {
            return DataResult<int>.NotFound(Messages.DepartmentNotFound);
        }

        // A-14: AcademicStaff ↔ ApplicationUser 1-1 (unique index). Önden kontrol edilir ki
        // kullanıcı 500 yerine anlaşılır bir çakışma mesajı görsün (A-15: DB son sözü söyler).
        var existing = await academicStaffRepository
            .GetAsync(a => a.ApplicationUserId == request.ApplicationUserId, cancellationToken)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            return DataResult<int>.Conflict(Messages.AcademicStaffAlreadyExists);
        }

        var staff = new AcademicStaff
        {
            ApplicationUserId = request.ApplicationUserId,
            Title = request.Title.Trim(),
            DepartmentId = department.Id,
        };

        await academicStaffRepository.AddAsync(staff, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return DataResult<int>.Success(staff.Id, Messages.AcademicStaffCreated);
    }

    public async Task<IResult> UpdateAcademicStaffAsync(
        int staffId, UpdateAcademicStaffRequestDto request, CancellationToken cancellationToken = default)
    {
        var staff = await academicStaffRepository.GetAsync(a => a.Id == staffId, cancellationToken).ConfigureAwait(false);
        if (staff is null)
        {
            return Result.NotFound(Messages.AdvisorNotFound);
        }

        var department = await departmentRepository.GetAsync(d => d.Id == request.DepartmentId, cancellationToken).ConfigureAwait(false);
        if (department is null)
        {
            return Result.NotFound(Messages.DepartmentNotFound);
        }

        staff.Title = request.Title.Trim();
        staff.DepartmentId = department.Id;
        academicStaffRepository.Update(staff);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(Messages.AcademicStaffUpdated);
    }

    public async Task<IResult> DeleteAcademicStaffAsync(int staffId, CancellationToken cancellationToken = default)
    {
        var staff = await academicStaffRepository.GetAsync(a => a.Id == staffId, cancellationToken).ConfigureAwait(false);
        if (staff is null)
        {
            return Result.NotFound(Messages.AdvisorNotFound);
        }

        // Kulüpler `AdvisorId` üzerinden Restrict ile bağlı — silinirse kulüp danışmansız kalırdı
        // ve EnsureClubWriteAccessAsync'in danışman dalı kimseyi eşleştiremezdi.
        var advisedClub = await clubRepository.GetAsync(c => c.AdvisorId == staffId, cancellationToken).ConfigureAwait(false);
        if (advisedClub is not null)
        {
            return Result.Conflict(Messages.AcademicStaffHasClubs);
        }

        academicStaffRepository.Delete(staff);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(Messages.AcademicStaffDeleted);
    }

    /// <summary>A-56: ad soyad birleşimi; ikisi de boşsa null döner ve çağıran e-postaya düşer.</summary>
    private static string? BuildFullName(string? firstName, string? lastName)
    {
        var full = $"{firstName} {lastName}".Trim();
        return full.Length == 0 ? null : full;
    }

    private static int ClampPageSize(int pageSize) =>
        pageSize <= 0 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);
}
