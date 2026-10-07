namespace BeeCloud.Application.DTOs.Pagination;

public class CursorPageResponse<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];

    public string? NextCursor { get; init; }

    public string? PreviousCursor { get; init; }
}
