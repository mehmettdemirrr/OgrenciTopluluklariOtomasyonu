using System.Linq.Expressions;
using Business.Concrete;
using Business.DTOs.Reference;
using Core.DataAccess;
using Core.Utilities.Time;
using Entities;
using Moq;
using Xunit;

namespace Business.Tests;

/// <summary>docs/MIMARI.md · A-20: her iş kuralı için bir kabul + bir ret.</summary>
public class AcademicTermManagerTests
{
    private readonly Mock<IEntityRepository<AcademicTerm>> _academicTermRepository = new();
    private readonly Mock<IEntityRepository<ClubMembership>> _clubMembershipRepository = new();
    private readonly Mock<IEntityRepository<Club>> _clubRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IClock> _clock = new();
    private readonly AcademicTermManager _sut;

    public AcademicTermManagerTests()
    {
        _clock.Setup(c => c.UtcNow).Returns(new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc));

        // A-51: devir kaynak dönemin üyeliklerini okur. Varsayılan olarak boş — devir kurallarının
        // kendisi entegrasyon testinde (TermRolloverTests) gerçek veritabanına karşı sınanıyor.
        _clubMembershipRepository
            .Setup(r => r.GetListAsync(It.IsAny<Expression<Func<ClubMembership, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _clubRepository
            .Setup(r => r.GetListAsync(It.IsAny<Expression<Func<Club, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _sut = new AcademicTermManager(
            _academicTermRepository.Object,
            _clubMembershipRepository.Object,
            _clubRepository.Object,
            _unitOfWork.Object,
            _clock.Object);
    }

    [Fact(DisplayName = "SetCurrent: zaten güncel olan dönem için hiç yazma yapılmaz")]
    public async Task SetCurrentAsync_AlreadyCurrent_DoesNotSave()
    {
        var term = new AcademicTerm { Id = 1, Name = "2026-Güz", StartDateUtc = DateTime.UtcNow, EndDateUtc = DateTime.UtcNow.AddMonths(4), IsCurrent = true };
        _academicTermRepository.Setup(r => r.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<AcademicTerm, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(term);

        var result = await _sut.SetCurrentAsync(1);

        Assert.True(result.IsSuccess);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "SetCurrent: olmayan dönem NotFound döner")]
    public async Task SetCurrentAsync_NotFound_ReturnsNotFound()
    {
        _academicTermRepository
            .Setup(r => r.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<AcademicTerm, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AcademicTerm?)null);

        var result = await _sut.SetCurrentAsync(999);

        Assert.False(result.IsSuccess);
    }

    [Fact(DisplayName = "SetCurrent: eski dönem önce false yapılıp kaydedilir, sonra hedef true yapılıp kaydedilir (devredilecek üyelik yokken iki SaveChanges)")]
    public async Task SetCurrentAsync_SwitchesCurrentTerm_SavesTwiceInOrder()
    {
        var oldCurrent = new AcademicTerm { Id = 1, Name = "2026-Bahar", StartDateUtc = DateTime.UtcNow.AddMonths(-6), EndDateUtc = DateTime.UtcNow.AddMonths(-1), IsCurrent = true };
        var target = new AcademicTerm { Id = 2, Name = "2026-Güz", StartDateUtc = DateTime.UtcNow, EndDateUtc = DateTime.UtcNow.AddMonths(4), IsCurrent = false };

        _academicTermRepository
            .Setup(r => r.GetAsync(It.Is<System.Linq.Expressions.Expression<Func<AcademicTerm, bool>>>(e => MatchesId(e, target)), It.IsAny<CancellationToken>()))
            .ReturnsAsync(target);
        _academicTermRepository
            .Setup(r => r.GetAsync(It.Is<System.Linq.Expressions.Expression<Func<AcademicTerm, bool>>>(e => MatchesIsCurrent(e)), It.IsAny<CancellationToken>()))
            .ReturnsAsync(oldCurrent);

        var saveOrder = new List<string>();
        _academicTermRepository.Setup(r => r.Update(It.IsAny<AcademicTerm>()))
            .Callback<AcademicTerm>(t => saveOrder.Add($"update:{t.Id}:{t.IsCurrent}"));
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => saveOrder.Add("save"))
            .ReturnsAsync(1);

        var result = await _sut.SetCurrentAsync(2);

        Assert.True(result.IsSuccess);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        Assert.Equal(["update:1:False", "save", "update:2:True", "save"], saveOrder);
    }

    [Fact(DisplayName = "CreateTerm: aynı isimde dönem varsa Conflict döner")]
    public async Task CreateTermAsync_NameTaken_ReturnsConflict()
    {
        _academicTermRepository
            .Setup(r => r.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<AcademicTerm, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AcademicTerm { Id = 1, Name = "2026-Güz", StartDateUtc = DateTime.UtcNow, EndDateUtc = DateTime.UtcNow.AddMonths(4) });

        var result = await _sut.CreateTermAsync(new CreateAcademicTermRequestDto { Name = "2026-Güz", StartDateUtc = DateTime.UtcNow, EndDateUtc = DateTime.UtcNow.AddMonths(4) });

        Assert.False(result.IsSuccess);
        _academicTermRepository.Verify(r => r.AddAsync(It.IsAny<AcademicTerm>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "CreateTerm: benzersiz isimle dönem her zaman IsCurrent=false oluşturulur")]
    public async Task CreateTermAsync_UniqueName_CreatesNonCurrentTerm()
    {
        _academicTermRepository
            .Setup(r => r.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<AcademicTerm, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AcademicTerm?)null);

        AcademicTerm? created = null;
        _academicTermRepository.Setup(r => r.AddAsync(It.IsAny<AcademicTerm>(), It.IsAny<CancellationToken>()))
            .Callback<AcademicTerm, CancellationToken>((t, _) => created = t)
            .Returns(Task.CompletedTask);

        var result = await _sut.CreateTermAsync(
            new CreateAcademicTermRequestDto { Name = "2027-Bahar", StartDateUtc = DateTime.UtcNow, EndDateUtc = DateTime.UtcNow.AddMonths(4) });

        Assert.True(result.IsSuccess);
        Assert.False(created!.IsCurrent);
    }

    [Fact(DisplayName = "UpdateTerm: benzersiz isimle ad ve tarihler güncellenir")]
    public async Task UpdateTermAsync_UniqueName_Updates()
    {
        var target = new AcademicTerm { Id = 1, Name = "2026-Güz", StartDateUtc = DateTime.UtcNow, EndDateUtc = DateTime.UtcNow.AddMonths(4) };
        _academicTermRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicTerm, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<AcademicTerm, bool>> filter, CancellationToken _) => new[] { target }.AsQueryable().Where(filter).FirstOrDefault());

        var newStart = DateTime.UtcNow.AddDays(1);
        var newEnd = newStart.AddMonths(4);
        var result = await _sut.UpdateTermAsync(1, new UpdateAcademicTermRequestDto { Name = "2026-Güz-Rev", StartDateUtc = newStart, EndDateUtc = newEnd });

        Assert.True(result.IsSuccess);
        Assert.Equal("2026-Güz-Rev", target.Name);
        Assert.Equal(newStart, target.StartDateUtc);
        _academicTermRepository.Verify(r => r.Update(target), Times.Once);
    }

    [Fact(DisplayName = "UpdateTerm: aynı isimde başka bir dönem varsa Conflict döner")]
    public async Task UpdateTermAsync_NameTakenByAnotherTerm_ReturnsConflict()
    {
        var target = new AcademicTerm { Id = 1, Name = "2026-Güz", StartDateUtc = DateTime.UtcNow, EndDateUtc = DateTime.UtcNow.AddMonths(4) };
        var other = new AcademicTerm { Id = 2, Name = "2026-Bahar", StartDateUtc = DateTime.UtcNow.AddMonths(-6), EndDateUtc = DateTime.UtcNow.AddMonths(-2) };
        _academicTermRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicTerm, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<AcademicTerm, bool>> filter, CancellationToken _) =>
                new[] { target, other }.AsQueryable().Where(filter).FirstOrDefault());

        var result = await _sut.UpdateTermAsync(1, new UpdateAcademicTermRequestDto { Name = "2026-Bahar", StartDateUtc = target.StartDateUtc, EndDateUtc = target.EndDateUtc });

        Assert.False(result.IsSuccess);
        _academicTermRepository.Verify(r => r.Update(It.IsAny<AcademicTerm>()), Times.Never);
    }

    private static bool MatchesId(System.Linq.Expressions.Expression<Func<AcademicTerm, bool>> expr, AcademicTerm term) =>
        expr.Compile().Invoke(term);

    private static bool MatchesIsCurrent(System.Linq.Expressions.Expression<Func<AcademicTerm, bool>> expr)
    {
        var probe = new AcademicTerm { Id = -1, Name = "probe", StartDateUtc = default, EndDateUtc = default, IsCurrent = true };
        var probeNonCurrent = new AcademicTerm { Id = -2, Name = "probe2", StartDateUtc = default, EndDateUtc = default, IsCurrent = false };
        var compiled = expr.Compile();
        return compiled.Invoke(probe) && !compiled.Invoke(probeNonCurrent);
    }
}
