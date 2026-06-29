namespace AgenticPlatform.Core.Constants;

public static class BuiltInToolCategories
{
    public const string Calculator = "Calculator";
    public const string Http = "Http";
    public const string RestApi = "REST API";
    public const string WebSearch = "WebSearch";
    public const string FileReader = "FileReader";
    public const string PythonScript = "PythonScript";

    public static bool IsHttpCategory(string category)
    {
        return category.Equals(Http, StringComparison.OrdinalIgnoreCase)
            || category.Equals(RestApi, StringComparison.OrdinalIgnoreCase)
            || category.Equals("HTTP", StringComparison.OrdinalIgnoreCase);
    }
}
