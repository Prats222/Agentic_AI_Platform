using System.Diagnostics;
using System.Text;
using System.Text.Json;
using AgenticPlatform.Core.Constants;
using AgenticPlatform.Core.Entities;
using AgenticPlatform.Core.Interfaces;
using AgenticPlatform.Core.Models.Tools;

namespace AgenticPlatform.Infrastructure.Services.Tools;

public sealed class PythonScriptToolExecutor : ToolExecutorBase, IToolExecutor
{
    public string Name => BuiltInToolCategories.PythonScript;

    public bool CanExecute(Tool tool)
    {
        return tool.Category.Equals(BuiltInToolCategories.PythonScript, StringComparison.OrdinalIgnoreCase);
    }

    public Task<ToolExecutionResult> ExecuteAsync(ToolExecutionRequest request, CancellationToken cancellationToken = default)
    {
        return ExecuteCoreAsync(request.Tool, () => RunPythonAsync(request.Tool.EndpointUrl, request.InputJson, cancellationToken));
    }

    private static async Task<string> RunPythonAsync(string script, string inputJson, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(script))
        {
            throw new InvalidOperationException("Python tool requires script content.");
        }

        var workingDirectory = Path.Combine(Path.GetTempPath(), $"pratspilot-tool-{Guid.NewGuid():N}");
        Directory.CreateDirectory(workingDirectory);
        var scriptPath = Path.Combine(workingDirectory, "tool.py");
        var siteCustomizePath = Path.Combine(workingDirectory, "sitecustomize.py");
        var requestsCompatPath = Path.Combine(workingDirectory, "requests_compat.py");
        await File.WriteAllTextAsync(scriptPath, script, Encoding.UTF8, cancellationToken);
        await File.WriteAllTextAsync(siteCustomizePath, SiteCustomizeModule, Encoding.UTF8, cancellationToken);
        await File.WriteAllTextAsync(requestsCompatPath, RequestsCompatibilityModule, Encoding.UTF8, cancellationToken);

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "python",
                Arguments = $"\"{scriptPath}\"",
                WorkingDirectory = workingDirectory,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            var existingPythonPath = startInfo.Environment.TryGetValue("PYTHONPATH", out var pythonPath)
                ? pythonPath
                : null;
            startInfo.Environment["PYTHONPATH"] = string.IsNullOrWhiteSpace(existingPythonPath)
                ? workingDirectory
                : $"{workingDirectory}{Path.PathSeparator}{existingPythonPath}";

            using var process = new Process { StartInfo = startInfo };

            process.Start();
            await process.StandardInput.WriteAsync(inputJson.AsMemory(), cancellationToken);
            process.StandardInput.Close();

            var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

            await process.WaitForExitAsync(cancellationToken);
            var output = await outputTask;
            var error = await errorTask;

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(string.IsNullOrWhiteSpace(error)
                    ? $"Python script exited with code {process.ExitCode}."
                    : error.Trim());
            }

            if (string.IsNullOrWhiteSpace(output))
            {
                return JsonSerializer.Serialize(new { output = string.Empty });
            }

            return IsValidJson(output)
                ? output
                : JsonSerializer.Serialize(new { output = output.Trim() });
        }
        finally
        {
            if (Directory.Exists(workingDirectory))
            {
                Directory.Delete(workingDirectory, recursive: true);
            }
        }
    }

    private const string SiteCustomizeModule = """
import sys

try:
    import requests
except ModuleNotFoundError:
    import requests_compat as requests
    sys.modules["requests"] = requests
""";

    private const string RequestsCompatibilityModule = """
import json as _json
import urllib.error as _url_error
import urllib.parse as _url_parse
import urllib.request as _url_request


class RequestException(Exception):
    pass


class HTTPError(RequestException):
    pass


class _Exceptions:
    RequestException = RequestException
    HTTPError = HTTPError


exceptions = _Exceptions()


class Response:
    def __init__(self, status_code, body, headers=None, url=""):
        self.status_code = status_code
        self.content = body
        self.headers = headers or {}
        self.url = url
        self.text = body.decode("utf-8", errors="replace")

    @property
    def ok(self):
        return 200 <= self.status_code < 400

    def json(self):
        return _json.loads(self.text)

    def raise_for_status(self):
        if not self.ok:
            raise HTTPError(f"HTTP {self.status_code}: {self.text}")


def request(method, url, headers=None, params=None, data=None, json=None, timeout=None, **kwargs):
    if params:
        separator = "&" if "?" in url else "?"
        url = f"{url}{separator}{_url_parse.urlencode(params, doseq=True)}"

    request_headers = dict(headers or {})
    payload = None
    if json is not None:
        payload = _json.dumps(json).encode("utf-8")
        request_headers.setdefault("Content-Type", "application/json")
    elif data is not None:
        if isinstance(data, dict):
            payload = _url_parse.urlencode(data).encode("utf-8")
            request_headers.setdefault("Content-Type", "application/x-www-form-urlencoded")
        elif isinstance(data, str):
            payload = data.encode("utf-8")
        else:
            payload = data

    outgoing_request = _url_request.Request(
        url,
        data=payload,
        headers=request_headers,
        method=method.upper())

    try:
        with _url_request.urlopen(outgoing_request, timeout=timeout) as result:
            return Response(result.status, result.read(), dict(result.headers), result.url)
    except _url_error.HTTPError as error:
        return Response(error.code, error.read(), dict(error.headers or {}), error.url)
    except _url_error.URLError as error:
        raise RequestException(str(error.reason)) from error


def get(url, **kwargs):
    return request("GET", url, **kwargs)


def post(url, **kwargs):
    return request("POST", url, **kwargs)


def put(url, **kwargs):
    return request("PUT", url, **kwargs)


def patch(url, **kwargs):
    return request("PATCH", url, **kwargs)


def delete(url, **kwargs):
    return request("DELETE", url, **kwargs)
""";

    private static bool IsValidJson(string value)
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
