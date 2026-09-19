namespace OneAccess.Client.Models;

public record PermissionDto(
    Guid Id,
    string Code,
    string Module,
    string Description,
    bool IsDelegable
);

public record DivisionDto(
    Guid Id,
    string Name,
    string Description,
    int TotalSections,
    int TotalUsers,
    DateTime CreatedAt
);

public record SectionDto(
    Guid Id,
    Guid DivisionId,
    string Name,
    string Description,
    int TotalUsers,
    DateTime CreatedAt
);

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = new List<T>();
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}
