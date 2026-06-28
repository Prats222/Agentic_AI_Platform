namespace AgenticPlatform.Core.Queries;

public sealed class ToolQueryParameters
{
    private const int MaxPageSize = 100;
    private int _pageNumber = 1;
    private int _pageSize = 10;

    public string? Name { get; set; }
    public string? Category { get; set; }
    public bool? IsEnabled { get; set; }
    public string? SortBy { get; set; } = "createdAt";
    public string? SortDirection { get; set; } = "desc";

    public int PageNumber
    {
        get => _pageNumber;
        set => _pageNumber = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value switch
        {
            < 1 => 10,
            > MaxPageSize => MaxPageSize,
            _ => value
        };
    }
}
