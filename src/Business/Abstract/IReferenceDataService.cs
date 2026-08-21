using Business.DTOs.Reference;
using Business.ValidationRules;
using Core.Aspects.Autofac;
using Core.DataAccess;
using Core.Utilities.Results;
using DataAccess.Seed;

namespace Business.Abstract;

/// <summary>docs/MIMARI.md · A-12/A-27: fakülte/bölüm — idempotent "seed ucu", tam CRUD değil.</summary>
public interface IReferenceDataService
{
    [SecuredOperation(IdentitySeedData.Permissions.ReferenceManage)]
    [CacheAspect(durationMinutes: 60)]
    Task<IDataResult<PagedResult<FacultyListItemDto>>> GetFacultiesPagedAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default);

    [SecuredOperation(IdentitySeedData.Permissions.ReferenceManage)]
    [CacheAspect(durationMinutes: 60)]
    Task<IDataResult<PagedResult<DepartmentListItemDto>>> GetDepartmentsPagedAsync(
        int facultyId, int pageIndex, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>Ad zaten varsa yeni satır oluşturmaz — mevcut satırı Success ile döner (Conflict değil, kasıtlı: seed ucu tekrar çalıştırılabilir olmalı).</summary>
    [SecuredOperation(IdentitySeedData.Permissions.ReferenceManage)]
    [ValidationAspect(typeof(CreateFacultyRequestValidator))]
    [CacheRemoveAspect("ReferenceDataManager.")]
    [TransactionAspect]
    Task<IDataResult<FacultyListItemDto>> CreateFacultyAsync(CreateFacultyRequestDto request, CancellationToken cancellationToken = default);

    [SecuredOperation(IdentitySeedData.Permissions.ReferenceManage)]
    [ValidationAspect(typeof(CreateDepartmentRequestValidator))]
    [CacheRemoveAspect("ReferenceDataManager.")]
    [TransactionAspect]
    Task<IDataResult<DepartmentListItemDto>> CreateDepartmentAsync(
        int facultyId, CreateDepartmentRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>docs/PLAN-V2.md §9: kulüp oluşturma diyaloğundaki danışman seçici için sayfalı liste.</summary>
    [SecuredOperation(IdentitySeedData.Permissions.ReferenceManage)]
    [CacheAspect(durationMinutes: 5)]
    Task<IDataResult<PagedResult<AcademicStaffListItemDto>>> GetAcademicStaffPagedAsync(
        int pageIndex, int pageSize, CancellationToken cancellationToken = default);
}
