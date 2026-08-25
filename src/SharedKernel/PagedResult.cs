namespace HamroSavings.SharedKernel;

/// <summary>
/// One page of results, with enough context for a caller to render controls without
/// having to fetch everything to learn how much there is.
/// </summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total)
{
    public int TotalPages => PageSize <= 0 ? 1 : (int)Math.Ceiling(Total / (double)PageSize);

    public static PagedResult<T> Empty(int page, int pageSize) => new([], page, pageSize, 0);
}
