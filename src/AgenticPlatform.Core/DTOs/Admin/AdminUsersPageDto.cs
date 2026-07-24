namespace AgenticPlatform.Core.DTOs.Admin;

public sealed class AdminUsersPageDto
{
    public IReadOnlyList<UserAccessDto> Items { get; init; } = Array.Empty<UserAccessDto>();
    public IReadOnlyList<UserAccessDto> JoinedToday { get; init; } = Array.Empty<UserAccessDto>();
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int MatchingCount { get; init; }
    public int JoinedTodayCount { get; init; }
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(MatchingCount / (double)PageSize);
}
