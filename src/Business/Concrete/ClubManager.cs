using AutoMapper;
using Business.Abstract;
using Business.Constants;
using Business.DTOs.Clubs;
using Core.DataAccess;
using Core.Utilities.Results;
using Entities;

namespace Business.Concrete;

public sealed class ClubManager(IEntityRepository<Club> clubRepository, IMapper mapper) : IClubService
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public async Task<IDataResult<PagedResult<ClubListItemDto>>> GetListPagedAsync(
        int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        var clampedPageSize = ClampPageSize(pageSize);

        // Y-11/A-16: sayfalama kırpma bir iş kuralıdır, controller'da değil burada yapılır.
        var paged = await clubRepository
            .GetListPagedAsync(pageIndex, clampedPageSize, c => c.IsActive, cancellationToken)
            .ConfigureAwait(false);

        var items = mapper.Map<IReadOnlyList<ClubListItemDto>>(paged.Items);
        var result = new PagedResult<ClubListItemDto>(items, paged.TotalCount, paged.PageIndex, paged.PageSize);

        return DataResult<PagedResult<ClubListItemDto>>.Success(result);
    }

    public async Task<IDataResult<ClubDetailDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var club = await clubRepository.GetAsync(c => c.Id == id, cancellationToken).ConfigureAwait(false);
        if (club is null)
        {
            return DataResult<ClubDetailDto>.NotFound(Messages.ClubNotFound);
        }

        return DataResult<ClubDetailDto>.Success(mapper.Map<ClubDetailDto>(club));
    }

    private static int ClampPageSize(int pageSize) =>
        pageSize <= 0 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);
}
