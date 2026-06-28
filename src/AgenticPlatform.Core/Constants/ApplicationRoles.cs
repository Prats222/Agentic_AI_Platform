namespace AgenticPlatform.Core.Constants;

public static class ApplicationRoles
{
    public const string Admin = "Admin";
    public const string Developer = "Developer";
    public const string Viewer = "Viewer";

    public static readonly string[] All = [Admin, Developer, Viewer];
}
