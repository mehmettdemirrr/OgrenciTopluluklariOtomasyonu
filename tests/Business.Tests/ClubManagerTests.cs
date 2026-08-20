using AutoMapper;
using Business.Concrete;
using Business.Mappings;
using Core.DataAccess;
using Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Business.Tests;

public class ClubManagerTests
{
    private readonly Mock<IEntityRepository<Club>> _clubRepository = new();
    private readonly IMapper _mapper;
    private readonly ClubManager _sut;

    public ClubManagerTests()
    {
        var mapperConfiguration = new MapperConfiguration(cfg => cfg.AddProfile<ClubMappingProfile>(), NullLoggerFactory.Instance);
        _mapper = mapperConfiguration.CreateMapper();
        _sut = new ClubManager(_clubRepository.Object, _mapper);
    }

    [Fact(DisplayName = "GetListPagedAsync: sayfa boyutu 100'ü aşarsa 100'e kırpılır (Y-11/A-16)")]
    public async Task GetListPagedAsync_PageSizeAbove100_ClampedTo100()
    {
        _clubRepository
            .Setup(r => r.GetListPagedAsync(0, 100, It.IsAny<System.Linq.Expressions.Expression<Func<Club, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<Club>([], 0, 0, 100));

        var result = await _sut.GetListPagedAsync(0, 500);

        Assert.True(result.IsSuccess);
        _clubRepository.Verify(
            r => r.GetListPagedAsync(0, 100, It.IsAny<System.Linq.Expressions.Expression<Func<Club, bool>>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact(DisplayName = "GetByIdAsync: bulunamayan kulüp NotFound döner")]
    public async Task GetByIdAsync_NotFound_ReturnsNotFound()
    {
        _clubRepository
            .Setup(r => r.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Club, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Club?)null);

        var result = await _sut.GetByIdAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(Core.Utilities.Results.ResultStatus.NotFound, result.Status);
    }
}
