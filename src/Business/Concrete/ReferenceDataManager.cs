using Business.Abstract;
using Business.Constants;
using Business.DTOs.Reference;
using Core.DataAccess;
using Core.Utilities.Results;
using Entities;

namespace Business.Concrete;

public sealed class ReferenceDataManager(
    IEntityRepository<Faculty> facultyRepository,
    IEntityRepository<Department> departmentRepository,
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

    private static int ClampPageSize(int pageSize) =>
        pageSize <= 0 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);
}
