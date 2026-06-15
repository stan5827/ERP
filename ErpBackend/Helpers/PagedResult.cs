namespace ErpBackend.Helpers;

public static class PagedResult
{
    public static PagedResponse<T> Create<T>(List<T> data, int totalCount, int page, int pageSize)
    {
        return new PagedResponse<T>
        {
            Data = data,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        };
    }
}

public class PagedResponse<T>
{
    public List<T> Data { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage => Page < TotalPages;
    public bool HasPrevPage => Page > 1;
    public int FirstItemOnPage => TotalCount > 0 ? (Page - 1) * PageSize + 1 : 0;
    public int LastItemOnPage => Math.Min(Page * PageSize, TotalCount);
}