namespace AgenticPlatform.Core.Constants;

public static class ApplicationRealms
{
    public static readonly Guid UserRealmId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid AdminRealmId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public const string UserRealmName = "User Realm";
    public const string AdminRealmName = "Admin Realm";
}
