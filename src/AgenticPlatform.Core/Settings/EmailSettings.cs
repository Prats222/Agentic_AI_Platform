namespace AgenticPlatform.Core.Settings;

public sealed class EmailSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string SenderEmail { get; set; } = string.Empty;
    public string SenderName { get; set; } = "PratsPilot";
    public string FrontendBaseUrl { get; set; } = "http://localhost:5173";
    public string LinkedInPostUrl { get; set; } = string.Empty;
}
