using System.Linq.Expressions;
using Business.Concrete;
using Business.DTOs.Reference;
using Core.DataAccess;
using DataAccess.Repositories;
using Entities;
using Moq;
using Xunit;

namespace Business.Tests;

/// <summary>docs/MIMARI.md · A-27: fakülte/bölüm seed ucu — idempotent create-if-missing.</summary>
public class ReferenceDataManagerTests
{
    private readonly Mock<IEntityRepository<Faculty>> _facultyRepository = new();
    private readonly Mock<IEntityRepository<Department>> _departmentRepository = new();
    private readonly Mock<IAcademicStaffDal> _academicStaffDal = new();

    // Faz 27 (K-33): akademik personel artık yazılabilir, silme ise kulüp bağı kontrol ediyor.
    private readonly Mock<IEntityRepository<AcademicStaff>> _academicStaffRepository = new();
    private readonly Mock<IEntityRepository<Club>> _clubRepository = new();

    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly ReferenceDataManager _sut;

    public ReferenceDataManagerTests() =>
        _sut = new ReferenceDataManager(
            _facultyRepository.Object,
            _departmentRepository.Object,
            _academicStaffRepository.Object,
            _clubRepository.Object,
            _academicStaffDal.Object,
            _unitOfWork.Object);

    [Fact(DisplayName = "K-33: kulübe danışmanlık yapan akademik personel silinemez (Conflict)")]
    public async Task DeleteAcademicStaffAsync_AdvisingAClub_ReturnsConflict()
    {
        _academicStaffRepository
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicStaff, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AcademicStaff { Id = 4, ApplicationUserId = 9, Title = "Dr.", DepartmentId = 1 });
        _clubRepository
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<Club, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Club { Id = 1, Name = "Kulüp", AdvisorId = 4, IsActive = true, CreatedAtUtc = DateTime.UtcNow });

        var result = await _sut.DeleteAcademicStaffAsync(4);

        Assert.False(result.IsSuccess);
        _academicStaffRepository.Verify(r => r.Delete(It.IsAny<AcademicStaff>()), Times.Never);
    }

    [Fact(DisplayName = "K-33: kulübe bağlı olmayan akademik personel silinir")]
    public async Task DeleteAcademicStaffAsync_NotAdvising_ReturnsSuccess()
    {
        _academicStaffRepository
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicStaff, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AcademicStaff { Id = 4, ApplicationUserId = 9, Title = "Dr.", DepartmentId = 1 });
        _clubRepository
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<Club, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Club?)null);

        var result = await _sut.DeleteAcademicStaffAsync(4);

        Assert.True(result.IsSuccess);
        _academicStaffRepository.Verify(r => r.Delete(It.IsAny<AcademicStaff>()), Times.Once);
    }

    [Fact(DisplayName = "A-14: aynı kullanıcıya ikinci akademik personel profili açılamaz")]
    public async Task CreateAcademicStaffAsync_UserAlreadyHasProfile_ReturnsConflict()
    {
        _departmentRepository
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<Department, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Department { Id = 1, Name = "Bilgisayar Mühendisliği", FacultyId = 1 });
        _academicStaffRepository
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicStaff, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AcademicStaff { Id = 4, ApplicationUserId = 9, Title = "Dr.", DepartmentId = 1 });

        var result = await _sut.CreateAcademicStaffAsync(
            new CreateAcademicStaffRequestDto { ApplicationUserId = 9, Title = "Prof. Dr.", DepartmentId = 1 });

        Assert.False(result.IsSuccess);
        _academicStaffRepository.Verify(r => r.AddAsync(It.IsAny<AcademicStaff>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "CreateFaculty: ad zaten varsa yeni satır oluşturmadan mevcut satırı Success ile döner")]
    public async Task CreateFacultyAsync_NameExists_ReturnsExistingWithoutInsert()
    {
        var existing = new Faculty { Id = 5, Name = "Fen Fakültesi" };
        _facultyRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Faculty, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var result = await _sut.CreateFacultyAsync(new CreateFacultyRequestDto { Name = "Fen Fakültesi" });

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Data.Id);
        _facultyRepository.Verify(r => r.AddAsync(It.IsAny<Faculty>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "CreateFaculty: yeni ad kaydedilir")]
    public async Task CreateFacultyAsync_NewName_Inserts()
    {
        _facultyRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Faculty, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync((Faculty?)null);

        var result = await _sut.CreateFacultyAsync(new CreateFacultyRequestDto { Name = "Mühendislik Fakültesi" });

        Assert.True(result.IsSuccess);
        _facultyRepository.Verify(r => r.AddAsync(It.IsAny<Faculty>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "CreateDepartment: olmayan fakülteye eklenemez")]
    public async Task CreateDepartmentAsync_FacultyNotFound_ReturnsNotFound()
    {
        _facultyRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Faculty, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync((Faculty?)null);

        var result = await _sut.CreateDepartmentAsync(999, new CreateDepartmentRequestDto { Name = "Bilgisayar Mühendisliği" });

        Assert.False(result.IsSuccess);
        _departmentRepository.Verify(r => r.AddAsync(It.IsAny<Department>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "CreateDepartment: aynı fakültede ad zaten varsa mevcut satır döner")]
    public async Task CreateDepartmentAsync_NameExistsInFaculty_ReturnsExistingWithoutInsert()
    {
        _facultyRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Faculty, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Faculty { Id = 1, Name = "Fen Fakültesi" });
        _departmentRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Department, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Department { Id = 10, Name = "Bilgisayar Mühendisliği", FacultyId = 1 });

        var result = await _sut.CreateDepartmentAsync(1, new CreateDepartmentRequestDto { Name = "Bilgisayar Mühendisliği" });

        Assert.True(result.IsSuccess);
        Assert.Equal(10, result.Data.Id);
        _departmentRepository.Verify(r => r.AddAsync(It.IsAny<Department>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "UpdateFaculty: geçerli yeniden adlandırma güncellenir")]
    public async Task UpdateFacultyAsync_ValidRename_Updates()
    {
        var faculty = new Faculty { Id = 1, Name = "Eski Ad" };
        _facultyRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Faculty, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<Faculty, bool>> filter, CancellationToken _) => new[] { faculty }.AsQueryable().Where(filter).FirstOrDefault());

        var result = await _sut.UpdateFacultyAsync(1, new UpdateFacultyRequestDto { Name = "Yeni Ad" });

        Assert.True(result.IsSuccess);
        Assert.Equal("Yeni Ad", faculty.Name);
        _facultyRepository.Verify(r => r.Update(faculty), Times.Once);
    }

    [Fact(DisplayName = "UpdateFaculty: başka bir fakültede aynı isim varsa Conflict döner")]
    public async Task UpdateFacultyAsync_DuplicateNameAtAnotherFaculty_ReturnsConflict()
    {
        var faculty = new Faculty { Id = 1, Name = "Fen Fakültesi" };
        var other = new Faculty { Id = 2, Name = "Mühendislik Fakültesi" };
        _facultyRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Faculty, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<Faculty, bool>> filter, CancellationToken _) =>
                new[] { faculty, other }.AsQueryable().Where(filter).FirstOrDefault());

        var result = await _sut.UpdateFacultyAsync(1, new UpdateFacultyRequestDto { Name = "Mühendislik Fakültesi" });

        Assert.False(result.IsSuccess);
        _facultyRepository.Verify(r => r.Update(It.IsAny<Faculty>()), Times.Never);
    }

    [Fact(DisplayName = "UpdateDepartment: geçerli yeniden adlandırma güncellenir")]
    public async Task UpdateDepartmentAsync_ValidRename_Updates()
    {
        var department = new Department { Id = 10, Name = "Eski Ad", FacultyId = 1 };
        _departmentRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Department, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<Department, bool>> filter, CancellationToken _) => new[] { department }.AsQueryable().Where(filter).FirstOrDefault());

        var result = await _sut.UpdateDepartmentAsync(1, 10, new UpdateDepartmentRequestDto { Name = "Yeni Ad" });

        Assert.True(result.IsSuccess);
        Assert.Equal("Yeni Ad", department.Name);
        _departmentRepository.Verify(r => r.Update(department), Times.Once);
    }

    [Fact(DisplayName = "UpdateDepartment: aynı fakültede aynı isim varsa Conflict döner")]
    public async Task UpdateDepartmentAsync_DuplicateNameInSameFaculty_ReturnsConflict()
    {
        var department = new Department { Id = 10, Name = "Yazılım Müh.", FacultyId = 1 };
        var other = new Department { Id = 11, Name = "Bilgisayar Müh.", FacultyId = 1 };
        _departmentRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Department, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<Department, bool>> filter, CancellationToken _) =>
                new[] { department, other }.AsQueryable().Where(filter).FirstOrDefault());

        var result = await _sut.UpdateDepartmentAsync(1, 10, new UpdateDepartmentRequestDto { Name = "Bilgisayar Müh." });

        Assert.False(result.IsSuccess);
        _departmentRepository.Verify(r => r.Update(It.IsAny<Department>()), Times.Never);
    }

    [Fact(DisplayName = "DeleteDepartment: kullanımda olmayan bölüm silinir")]
    public async Task DeleteDepartmentAsync_NotInUse_DeletesSuccessfully()
    {
        var department = new Department { Id = 10, Name = "Bilgisayar Müh.", FacultyId = 1 };
        _departmentRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Department, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(department);

        var result = await _sut.DeleteDepartmentAsync(1, 10);

        Assert.True(result.IsSuccess);
        _departmentRepository.Verify(r => r.Delete(department), Times.Once);
    }

    [Fact(DisplayName = "DeleteDepartment: kullanımda olan bölüm (FK Restrict) Conflict döner")]
    public async Task DeleteDepartmentAsync_InUse_ReturnsConflict()
    {
        var department = new Department { Id = 10, Name = "Bilgisayar Müh.", FacultyId = 1 };
        _departmentRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Department, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(department);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ReferentialIntegrityConflictException("fk violation", new Exception()));

        var result = await _sut.DeleteDepartmentAsync(1, 10);

        Assert.False(result.IsSuccess);
    }
}
