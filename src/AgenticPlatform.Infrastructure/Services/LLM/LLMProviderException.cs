using System.Net;

namespace AgenticPlatform.Infrastructure.Services.LLM;

public sealed class LLMProviderException : Exception
{
    public LLMProviderException(string provider, HttpStatusCode statusCode, string responseBody)
        : base($"{provider} request failed with {(int)statusCode} {statusCode}. {ExtractMessage(responseBody)}")
    {
        Provider = provider;
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }

    public string Provider { get; }
    public HttpStatusCode StatusCode { get; }
    public string ResponseBody { get; }

    private static string ExtractMessage(string responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            return "The provider returned an empty error response.";
        }

        return responseBody.Length <= 1000
            ? responseBody
            : string.Concat(responseBody.AsSpan(0, 1000), "...[truncated]");
    }
}
