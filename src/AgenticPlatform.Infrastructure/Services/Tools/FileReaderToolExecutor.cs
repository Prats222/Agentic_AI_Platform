using System.Text.Json;
using AgenticPlatform.Core.Constants;
using AgenticPlatform.Core.Entities;
using AgenticPlatform.Core.Interfaces;
using AgenticPlatform.Core.Models.Tools;
using Microsoft.Extensions.Hosting;

namespace AgenticPlatform.Infrastructure.Services.Tools;

public sealed class FileReaderToolExecutor : ToolExecutorBase, IToolExecutor
{
    private const long MaxFileBytes = 1024 * 1024;
    private readonly IHostEnvironment _hostEnvironment;

    public FileReaderToolExecutor(IHostEnvironment hostEnvironment)
    {
        _hostEnvironment = hostEnvironment;
    }

    public string Name => BuiltInToolCategories.FileReader;

    public bool CanExecute(Tool tool)
    {
        return tool.Category.Equals(BuiltInToolCategories.FileReader, StringComparison.OrdinalIgnoreCase);
    }

    public Task<ToolExecutionResult> ExecuteAsync(ToolExecutionRequest request, CancellationToken cancellationToken = default)
    {
        return ExecuteCoreAsync(request.Tool, async () =>
        {
            using var input = JsonDocument.Parse(request.InputJson);
            var path = input.RootElement.TryGetProperty("path", out var pathElement)
                ? pathElement.GetString()
                : null;

            if (string.IsNullOrWhiteSpace(path))
            {
                throw new InvalidOperationException("File reader input requires a 'path' value.");
            }

            var root = Path.GetFullPath(_hostEnvironment.ContentRootPath);
            var fullPath = Path.GetFullPath(Path.Combine(root, path));

            if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("File reader can only access files under the API content root.");
            }

            var fileInfo = new FileInfo(fullPath);
            if (!fileInfo.Exists)
            {
                throw new FileNotFoundException("Requested file was not found.", path);
            }

            if (fileInfo.Length > MaxFileBytes)
            {
                throw new InvalidOperationException("Requested file exceeds the 1 MB read limit.");
            }

            var content = await File.ReadAllTextAsync(fullPath, cancellationToken);

            return JsonSerializer.Serialize(new
            {
                path,
                sizeBytes = fileInfo.Length,
                content
            });
        });
    }
}
