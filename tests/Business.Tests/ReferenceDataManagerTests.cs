using System.Linq.Expressions;
using Business.Concrete;
using Business.DTOs.Reference;
using Core.DataAccess;
using Entities;
using Moq;
using Xunit;

namespace Business.Tests;

/// <summary>docs/MIMARI.md · A-27: fakülte/bölüm seed ucu — idempotent create-if-missing.</summary>
public class ReferenceDataManagerTests
{
    private readonly Mock<IEntityRepository<Faculty>> _facultyRepository = new();
    private readonly Mock<IEntityRepository<Department>> _departmentRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly ReferenceDataManager _sut;

    public ReferenceDataManagerTests() =>
        _sut = new ReferenceDataManager(_facultyRepository.Object, _departmentRepository.Object, _unitOfWork.Object);

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
}
