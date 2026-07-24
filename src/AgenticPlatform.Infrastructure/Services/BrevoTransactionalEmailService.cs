using System.Net.Http.Json;
using System.Text.Encodings.Web;
using AgenticPlatform.Core.Interfaces;
using AgenticPlatform.Core.Settings;
using Microsoft.Extensions.Options;

namespace AgenticPlatform.Infrastructure.Services;

public sealed class BrevoTransactionalEmailService : ITransactionalEmailService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly EmailSettings _settings;

    public BrevoTransactionalEmailService(
        IHttpClientFactory httpClientFactory,
        IOptions<EmailSettings> settings)
    {
        _httpClientFactory = httpClientFactory;
        _settings = settings.Value;
    }

    public Task SendConfirmationEmailAsync(
        string recipientEmail,
        string recipientName,
        string confirmationUrl,
        CancellationToken cancellationToken = default)
    {
        var safeName = HtmlEncoder.Default.Encode(recipientName);
        var safeUrl = HtmlEncoder.Default.Encode(confirmationUrl);
        var html = BuildLayout(
            "Confirm your PratsPilot account",
            $"""
             <p style="margin:0 0 18px">Hi {safeName},</p>
             <p style="margin:0 0 22px;color:#c8cede">Confirm your email to activate your PratsPilot account and enter the User Realm.</p>
             <p style="margin:0 0 24px">
               <a href="{safeUrl}" style="display:inline-block;padding:13px 22px;border-radius:8px;background:#7257ff;color:#ffffff;text-decoration:none;font-weight:700">Confirm email</a>
             </p>
             <p style="margin:0;color:#8f99ad;font-size:13px">This secure link is tied to your account. If you did not register for PratsPilot, you can ignore this email.</p>
             """);

        return SendAsync(
            recipientEmail,
            recipientName,
            "Confirm your PratsPilot account",
            html,
            $"Hi {recipientName}, confirm your PratsPilot account: {confirmationUrl}",
            cancellationToken);
    }

    public Task SendWelcomeGuideAsync(
        string recipientEmail,
        string recipientName,
        CancellationToken cancellationToken = default)
    {
        var safeName = HtmlEncoder.Default.Encode(recipientName);
        var siteUrl = HtmlEncoder.Default.Encode(_settings.FrontendBaseUrl.TrimEnd('/'));
        var linkedInUrl = HtmlEncoder.Default.Encode(_settings.LinkedInPostUrl);
        var guidanceButton = string.IsNullOrWhiteSpace(_settings.LinkedInPostUrl)
            ? string.Empty
            : $"""
               <a href="{linkedInUrl}" style="display:inline-block;margin-left:8px;padding:12px 18px;border:1px solid #4f8cff;border-radius:8px;color:#8fb4ff;text-decoration:none;font-weight:700">Watch the guided demo</a>
               """;

        var html = BuildLayout(
            "Welcome aboard PratsPilot",
            $"""
             <p style="margin:0 0 18px">Hi {safeName},</p>
             <p style="margin:0 0 18px;color:#c8cede">Your account is ready. PratsPilot lets you build agents without writing agent code, attach knowledge and tools, orchestrate workflows, add human approval gates, and challenge other creators in the Agent Battle Arena.</p>
             <div style="margin:22px 0;padding:18px;border:1px solid #293249;border-radius:10px;background:#0d1320">
               <strong style="color:#ffffff">A good first mission</strong>
               <ol style="margin:10px 0 0;padding-left:20px;color:#c8cede;line-height:1.7">
                 <li>Configure a free LLM key in AI Settings.</li>
                 <li>Create an agent with a clear goal and expected output.</li>
                 <li>Attach a tool or context document.</li>
                 <li>Run the agent, inspect its output, then build a workflow.</li>
               </ol>
             </div>
             <p style="margin:0 0 22px">
               <a href="{siteUrl}" style="display:inline-block;padding:13px 22px;border-radius:8px;background:#7257ff;color:#ffffff;text-decoration:none;font-weight:700">Launch PratsPilot</a>
               {guidanceButton}
             </p>
             <p style="margin:0;color:#8f99ad;font-size:13px">The free backend can take around 30 seconds to wake after inactivity. Shared free LLM quotas can also run out, so you can save your own free provider key in AI Settings.</p>
             """);

        return SendAsync(
            recipientEmail,
            recipientName,
            "Welcome to PratsPilot - your first mission",
            html,
            $"Welcome to PratsPilot, {recipientName}. Launch: {_settings.FrontendBaseUrl}. Guided demo: {_settings.LinkedInPostUrl}",
            cancellationToken);
    }

    private async Task SendAsync(
        string recipientEmail,
        string recipientName,
        string subject,
        string htmlContent,
        string textContent,
        CancellationToken cancellationToken)
    {
        EnsureConfigured();

        var client = _httpClientFactory.CreateClient("brevo-email");
        using var request = new HttpRequestMessage(HttpMethod.Post, "smtp/email");
        request.Headers.Add("api-key", _settings.ApiKey);
        request.Content = JsonContent.Create(new
        {
            sender = new { name = _settings.SenderName, email = _settings.SenderEmail },
            to = new[] { new { email = recipientEmail, name = recipientName } },
            subject,
            htmlContent,
            textContent
        });

        using var response = await client.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new InvalidOperationException(
            $"Brevo rejected the email with status {(int)response.StatusCode}: {responseBody}");
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey)
            || string.IsNullOrWhiteSpace(_settings.SenderEmail)
            || string.IsNullOrWhiteSpace(_settings.FrontendBaseUrl))
        {
            throw new InvalidOperationException(
                "Transactional email is not configured. Set Email:ApiKey, Email:SenderEmail, and Email:FrontendBaseUrl.");
        }
    }

    private static string BuildLayout(string heading, string content)
    {
        return $"""
                <!doctype html>
                <html lang="en">
                  <body style="margin:0;background:#090d16;font-family:Arial,sans-serif;color:#f5f7ff">
                    <div style="max-width:620px;margin:0 auto;padding:32px 18px">
                      <div style="padding:28px;border:1px solid #252f42;border-radius:14px;background:#151b28">
                        <div style="font-size:21px;font-weight:800;margin-bottom:26px;color:#ffffff">PratsPilot <span style="color:#7c5cfc">Mission Control AI</span></div>
                        <h1 style="font-size:27px;line-height:1.25;margin:0 0 22px;color:#ffffff">{heading}</h1>
                        <div style="font-size:16px;line-height:1.65">{content}</div>
                      </div>
                      <p style="margin:16px 4px 0;color:#68738a;font-size:12px">PratsPilot transactional email. Please do not share confirmation links.</p>
                    </div>
                  </body>
                </html>
                """;
    }
}
