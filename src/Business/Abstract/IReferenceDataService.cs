using Business.DTOs.Files;
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

    /// <summary>docs/PLAN-V2.md · Faz 13: isim değişikliğinde diğer fakültelerle çakışma önden kontrol edilir.</summary>
    [SecuredOperation(IdentitySeedData.Permissions.ReferenceManage)]
    [ValidationAspect(typeof(UpdateFacultyRequestValidator))]
    [CacheRemoveAspect("ReferenceDataManager.")]
    [TransactionAspect]
    Task<IResult> UpdateFacultyAsync(int id, UpdateFacultyRequestDto request, CancellationToken cancellationToken = default);

    [SecuredOperation(IdentitySeedData.Permissions.ReferenceManage)]
    [ValidationAspect(typeof(UpdateDepartmentRequestValidator))]
    [CacheRemoveAspect("ReferenceDataManager.")]
    [TransactionAspect]
    Task<IResult> UpdateDepartmentAsync(
        int facultyId, int departmentId, UpdateDepartmentRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// docs/PLAN-V2.md · Faz 13 (A-12): referans verisi hard delete edilir. Bölüm kullanımdaysa
    /// (kayıtlı öğrenci var — FK Restrict) <see cref="Core.DataAccess.ReferentialIntegrityConflictException"/>
    /// yakalanıp Türkçe Conflict'e çevrilir.
    /// </summary>
    [SecuredOperation(IdentitySeedData.Permissions.ReferenceManage)]
    [CacheRemoveAspect("ReferenceDataManager.")]
    [TransactionAspect]
    Task<IResult> DeleteDepartmentAsync(int facultyId, int departmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// docs/MIMARI.md · K-35/A-60: kategori seçici hem kulüp formunda hem başvuru formunda
    /// kullanılır, bu yüzden okuma izni `clubs.read` (Member rolünde var) — `reference.manage` değil.
    /// </summary>
    [SecuredOperation(IdentitySeedData.Permissions.ClubsRead)]
    [CacheAspect(durationMinutes: 60)]
    Task<IDataResult<PagedResult<ClubCategoryListItemDto>>> GetClubCategoriesPagedAsync(
        int pageIndex, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Y-45: kategori adı `ClubListItemDto`'da taşınır — kulüp ve vitrin cache'i de düşer.
    /// Yalnızca "ReferenceDataManager." demek listeye eski adı servis ettirir.
    /// </summary>
    [SecuredOperation(IdentitySeedData.Permissions.ReferenceManage)]
    [ValidationAspect(typeof(CreateClubCategoryRequestValidator))]
    [CacheRemoveAspect("ReferenceDataManager.", "ClubManager.", "PublicContentManager.")]
    [TransactionAspect]
    Task<IDataResult<ClubCategoryListItemDto>> CreateClubCategoryAsync(
        CreateClubCategoryRequestDto request, CancellationToken cancellationToken = default);

    [SecuredOperation(IdentitySeedData.Permissions.ReferenceManage)]
    [ValidationAspect(typeof(UpdateClubCategoryRequestValidator))]
    [CacheRemoveAspect("ReferenceDataManager.", "ClubManager.", "PublicContentManager.")]
    [TransactionAspect]
    Task<IResult> UpdateClubCategoryAsync(
        int categoryId, UpdateClubCategoryRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>A-12: hard delete serbest; kullanımdaysa FK Restrict → 409 (DeleteDepartmentAsync precedent'i).</summary>
    [SecuredOperation(IdentitySeedData.Permissions.ReferenceManage)]
    [CacheRemoveAspect("ReferenceDataManager.", "ClubManager.", "PublicContentManager.")]
    [TransactionAspect]
    Task<IResult> DeleteClubCategoryAsync(int categoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// docs/MIMARI.md · K-37/A-62: başvuru formu bu katalogdan render edilir, bu yüzden okuma izni
    /// `clubs.read` (kategori kararının aynısı). `activeOnly = true` yalnızca yürürlükteki formları döner.
    /// </summary>
    [SecuredOperation(IdentitySeedData.Permissions.ClubsRead)]
    [CacheAspect(durationMinutes: 60)]
    Task<IDataResult<PagedResult<ClubDocumentTypeListItemDto>>> GetClubDocumentTypesPagedAsync(
        int pageIndex, int pageSize, bool activeOnly = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Y-45: tek önek yeterli. Kategoriden (A-60) farkı: evrak tipi kodu/adı yalnızca
    /// GetClubDocumentTypesPagedAsync'in cache'inde yaşıyor — ClubApplicationManager'da
    /// [CacheAspect] YOK, inceleme listesi katalogu her istekte taze okuyor.
    /// </summary>
    [SecuredOperation(IdentitySeedData.Permissions.ReferenceManage)]
    [ValidationAspect(typeof(CreateClubDocumentTypeRequestValidator))]
    [CacheRemoveAspect("ReferenceDataManager.")]
    [TransactionAspect]
    Task<IDataResult<ClubDocumentTypeListItemDto>> CreateClubDocumentTypeAsync(
        CreateClubDocumentTypeRequestDto request, CancellationToken cancellationToken = default);

    [SecuredOperation(IdentitySeedData.Permissions.ReferenceManage)]
    [ValidationAspect(typeof(UpdateClubDocumentTypeRequestValidator))]
    [CacheRemoveAspect("ReferenceDataManager.")]
    [TransactionAspect]
    Task<IResult> UpdateClubDocumentTypeAsync(
        int documentTypeId, UpdateClubDocumentTypeRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>A-62: kullanımdaysa 409 — yürürlükten kaldırmanın yolu IsActive = false.</summary>
    [SecuredOperation(IdentitySeedData.Permissions.ReferenceManage)]
    [CacheRemoveAspect("ReferenceDataManager.")]
    [TransactionAspect]
    Task<IResult> DeleteClubDocumentTypeAsync(int documentTypeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Boş kurumsal şablonu yükler/değiştirir. Liste cache'i TemplateFileId taşıdığı için düşer.
    /// </summary>
    [SecuredOperation(IdentitySeedData.Permissions.ReferenceManage)]
    [CacheRemoveAspect("ReferenceDataManager.")]
    [TransactionAspect]
    Task<IDataResult<ClubDocumentTypeListItemDto>> UploadClubDocumentTemplateAsync(
        int documentTypeId, UploadFileRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Başvuru formundaki "Şablonu indir" — `clubs.read` (katalog okuma izninin aynısı).
    /// </summary>
    [SecuredOperation(IdentitySeedData.Permissions.ClubsRead)]
    Task<IDataResult<FileContentDto>> GetClubDocumentTemplateAsync(
        int documentTypeId, CancellationToken cancellationToken = default);

    /// <summary>docs/PLAN-V2.md §9: kulüp oluşturma diyaloğundaki danışman seçici için sayfalı liste.</summary>
    [SecuredOperation(IdentitySeedData.Permissions.ReferenceManage)]
    [CacheAspect(durationMinutes: 5)]
    Task<IDataResult<PagedResult<AcademicStaffListItemDto>>> GetAcademicStaffPagedAsync(
        int pageIndex, int pageSize, string? search = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// docs/PLAN-V3.md §17.4: topluluk kurma başvurusundaki danışman seçici — [SecuredOperation]
    /// kasıtlı olarak yok (reference.manage değil), herhangi bir kimliği doğrulanmış öğrenci görebilir.
    /// </summary>
    [CacheAspect(durationMinutes: 5)]
    Task<IDataResult<PagedResult<SelectableAcademicStaffDto>>> GetSelectableAcademicStaffAsync(
        int pageIndex, int pageSize, string? search = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// docs/MIMARI.md · K-33: akademik personel artık salt-okunur değil. v5.0'a kadar hiçbir uç
    /// `AcademicStaff` oluşturmuyordu — tek kayıt demo seed'inden geliyordu, dolayısıyla sisteme
    /// yeni danışman eklemek imkânsızdı (PLAN-V5 bildirilen #3).
    /// </summary>
    [SecuredOperation(IdentitySeedData.Permissions.ReferenceManage)]
    [ValidationAspect(typeof(CreateAcademicStaffRequestValidator))]
    [CacheRemoveAspect("ReferenceDataManager.")]
    [TransactionAspect]
    Task<IDataResult<int>> CreateAcademicStaffAsync(CreateAcademicStaffRequestDto request, CancellationToken cancellationToken = default);

    [SecuredOperation(IdentitySeedData.Permissions.ReferenceManage)]
    [ValidationAspect(typeof(UpdateAcademicStaffRequestValidator))]
    [CacheRemoveAspect("ReferenceDataManager.")]
    [TransactionAspect]
    Task<IResult> UpdateAcademicStaffAsync(int staffId, UpdateAcademicStaffRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>Kulübe danışmanlık yapıyorsa <c>Conflict</c> — yetim kulüp bırakılmaz.</summary>
    [SecuredOperation(IdentitySeedData.Permissions.ReferenceManage)]
    [CacheRemoveAspect("ReferenceDataManager.")]
    [TransactionAspect]
    Task<IResult> DeleteAcademicStaffAsync(int staffId, CancellationToken cancellationToken = default);
}
