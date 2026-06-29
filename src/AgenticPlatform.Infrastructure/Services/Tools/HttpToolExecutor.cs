using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AgenticPlatform.Core.Constants;
using AgenticPlatform.Core.Entities;
using AgenticPlatform.Core.Interfaces;
using AgenticPlatform.Core.Models.Tools;

namespace AgenticPlatform.Infrastructure.Services.Tools;

public sealed class HttpToolExecutor : ToolExecutorBase, IToolExecutor
{
    private readonly IHttpClientFactory _httpClientFactory;

    public HttpToolExecutor(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public string Name => BuiltInToolCategories.Http;

    public bool CanExecute(Tool tool)
    {
        return BuiltInToolCategories.IsHttpCategory(tool.Category);
    }

    public Task<ToolExecutionResult> ExecuteAsync(ToolExecutionRequest request, CancellationToken cancellationToken = default)
    {
        return ExecuteCoreAsync(request.Tool, async () =>
        {
            using var input = JsonDocument.Parse(request.InputJson);
            var root = input.RootElement;

            var method = root.TryGetProperty("method", out var methodElement)
                ? methodElement.GetString()
                : "GET";
            var url = root.TryGetProperty("url", out var urlElement)
                ? urlElement.GetString()
                : request.Tool.EndpointUrl;

            if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                throw new InvalidOperationException("HTTP tool requires a valid absolute 'url' or EndpointUrl.");
            }

            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            {
                throw new InvalidOperationException("HTTP tool only supports HTTP and HTTPS URLs.");
            }

            using var httpRequest = new HttpRequestMessage(new HttpMethod(method ?? "GET"), uri);

            if (root.TryGetProperty("headers", out var headersElement) && headersElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var header in headersElement.EnumerateObject())
                {
                    httpRequest.Headers.TryAddWithoutValidation(header.Name, header.Value.GetString());
                }
            }

            if (root.TryGetProperty("body", out var bodyElement))
            {
                var body = bodyElement.ValueKind == JsonValueKind.String
                    ? bodyElement.GetString()
                    : bodyElement.GetRawText();

                httpRequest.Content = new StringContent(body ?? string.Empty, Encoding.UTF8, "application/json");
                httpRequest.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            }

            var client = _httpClientFactory.CreateClient("tool-runner");
            using var response = await client.SendAsync(httpRequest, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            return JsonSerializer.Serialize(new
            {
                statusCode = (int)response.StatusCode,
                reasonPhrase = response.ReasonPhrase,
                isSuccessStatusCode = response.IsSuccessStatusCode,
                body = responseBody
            });
        });
    }
}
