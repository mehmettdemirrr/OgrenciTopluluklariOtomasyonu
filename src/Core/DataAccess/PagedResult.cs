namespace Core.DataAccess;

/// <summary>docs/MIMARI.md · Y-11 / A-16: liste uçları bu sözleşme üzerinden sayfalanır.</summary>
public sealed class PagedResult<T>
{
    public PagedResult(IReadOnlyList<T> items, int totalCount, int pageIndex, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        PageIndex = pageIndex;
        PageSize = pageSize;
    }

    public IReadOnlyList<T> Items { get; }

    public int TotalCount { get; }

    public int PageIndex { get; }

    public int PageSize { get; }

    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
