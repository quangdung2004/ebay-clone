namespace CloneEbay.Infrastructure.Common.Helpers;

/// <summary>
/// Shared pagination param normalization, used by every paginated query.
/// </summary>
public static class PaginationHelper
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public static (int page, int pageSize) Normalize(int page, int pageSize)
    {
        page = page <= 0 ? DefaultPage : page;
        pageSize = pageSize is < 1 or > MaxPageSize ? DefaultPageSize : pageSize;
        return (page, pageSize);
    }
}
