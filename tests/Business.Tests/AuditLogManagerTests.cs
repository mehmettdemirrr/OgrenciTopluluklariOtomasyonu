using Business.Concrete;
using Core.DataAccess;
using DataAccess.Repositories;
using Entities.Dtos.Audit;
using Moq;
using Xunit;

namespace Business.Tests;

/// <summary>docs/PLAN-V2.md · Faz 13 (K-12): audit.read'in tek işi — filtreleri DAL'a iletmek.</summary>
public class AuditLogManagerTests
{
    private readonly Mock<IAuditLogDal> _auditLogDal = new();
    private readonly AuditLogManager _sut;

    public AuditLogManagerTests() => _sut = new AuditLogManager(_auditLogDal.Object);

    [Fact(DisplayName = "GetPaged: filtreler DAL'a olduğu gibi iletilir")]
    public async Task GetPagedAsync_PassesFiltersToDal()
    {
        _auditLogDal.Setup(d => d.GetPagedAsync("Club", "5", 7, null, null, 0, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<AuditLogListItemDto>([], 0, 0, 20));

        var result = await _sut.GetPagedAsync("Club", "5", 7, null, null, 0, 20);

        Assert.True(result.IsSuccess);
        _auditLogDal.Verify(d => d.GetPagedAsync("Club", "5", 7, null, null, 0, 20, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "GetPaged: geçersiz sayfa boyutu (0) varsayılan 20'ye düşürülür")]
    public async Task GetPagedAsync_InvalidPageSize_ClampsToDefault()
    {
        _auditLogDal.Setup(d => d.GetPagedAsync(null, null, null, null, null, 0, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<AuditLogListItemDto>([], 0, 0, 20));

        var result = await _sut.GetPagedAsync(null, null, null, null, null, 0, 0);

        Assert.True(result.IsSuccess);
        _auditLogDal.Verify(d => d.GetPagedAsync(null, null, null, null, null, 0, 20, It.IsAny<CancellationToken>()), Times.Once);
    }
}
