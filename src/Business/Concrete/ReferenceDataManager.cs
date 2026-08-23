using Business.Abstract;
using Business.Constants;
using Business.DTOs.Reference;
using Core.DataAccess;
using Core.Utilities.Results;
using DataAccess.Repositories;
using Entities;

namespace Business.Concrete;

public sealed class ReferenceDataManager(
    IEntityRepository<Faculty> facultyRepository,
    IEntityRepository<Department> departmentRepository,
    IAcademicStaffDal academicStaffDal,
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

    public async Task<IDataResult<PagedResult<AcademicStaffListItemDto>>> GetAcademicStaffPagedAsync(
        int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        var paged = await academicStaffDal.GetListPagedAsync(pageIndex, ClampPageSize(pageSize), cancellationToken).ConfigureAwait(false);

        var items = paged.Items.Select(s => new AcademicStaffListItemDto { Id = s.Id, Title = s.Title, Email = s.Email }).ToList();
        return DataResult<PagedResult<AcademicStaffListItemDto>>.Success(
            new PagedResult<AcademicStaffListItemDto>(items, paged.TotalCount, paged.PageIndex, paged.PageSize));
    }

    public async Task<IDataResult<PagedResult<SelectableAcademicStaffDto>>> GetSelectableAcademicStaffAsync(
        int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        var paged = await academicStaffDal.GetListPagedAsync(pageIndex, ClampPageSize(pageSize), cancellationToken).ConfigureAwait(false);

        var items = paged.Items.Select(s => new SelectableAcademicStaffDto { Id = s.Id, Title = s.Title, Email = s.Email }).ToList();
        return DataResult<PagedResult<SelectableAcademicStaffDto>>.Success(
            new PagedResult<SelectableAcademicStaffDto>(items, paged.TotalCount, paged.PageIndex, paged.PageSize));
    }

    private static int ClampPageSize(int pageSize) =>
        pageSize <= 0 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);
}
