using System.Text.Json;

namespace AgenticPlatform.Core.Validators;

public static class JsonValidator
{
    public static bool BeValidJson(string value)
    {
        try
        {
            JsonDocument.Parse(value);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
