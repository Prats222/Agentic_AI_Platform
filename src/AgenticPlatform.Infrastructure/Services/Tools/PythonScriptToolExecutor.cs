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

        var scriptPath = Path.Combine(Path.GetTempPath(), $"pratspilot-tool-{Guid.NewGuid():N}.py");
        await File.WriteAllTextAsync(scriptPath, script, Encoding.UTF8, cancellationToken);

        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "python",
                    Arguments = $"\"{scriptPath}\"",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

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
            if (File.Exists(scriptPath))
            {
                File.Delete(scriptPath);
            }
        }
    }

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
