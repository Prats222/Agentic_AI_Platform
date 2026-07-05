using System.Text;
using System.IO.Compression;
using System.Xml.Linq;
using AgenticPlatform.Core.Common;
using AgenticPlatform.Core.Constants;
using AgenticPlatform.Core.DTOs.ContextDocuments;
using AgenticPlatform.Core.Entities;
using AgenticPlatform.Infrastructure.Data;
using AgenticPlatform.API.Realms;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgenticPlatform.API.Controllers;

[ApiController]
[ApiVersion(1.0)]
[Authorize(Roles = $"{ApplicationRoles.Admin},{ApplicationRoles.Developer},{ApplicationRoles.Viewer}")]
[Route("api/v{version:apiVersion}/context-documents")]
public sealed class ContextDocumentsController : ControllerBase
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt", ".json", ".md", ".csv", ".pdf", ".docx", ".xlsx"
    };

    private readonly ApplicationDbContext _dbContext;
    private readonly IWebHostEnvironment _environment;

    public ContextDocumentsController(ApplicationDbContext dbContext, IWebHostEnvironment environment)
    {
        _dbContext = dbContext;
        _environment = environment;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ContextDocumentDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ContextDocumentDto>>>> GetDocuments(CancellationToken cancellationToken)
    {
        var realmId = RealmAccess.ResolveRealmId(this);
        if (!RealmAccess.CanAccessRealm(this, realmId))
        {
            return Forbid();
        }

        var documents = await _dbContext.ContextDocuments
            .AsNoTracking()
            .InRealm(realmId)
            .OrderByDescending(document => document.CreatedAt)
            .Select(document => new ContextDocumentDto
            {
                Id = document.Id,
                RealmId = document.RealmId,
                Name = document.Name,
                FileName = document.FileName,
                ContentType = document.ContentType,
                FileExtension = document.FileExtension,
                SizeBytes = document.SizeBytes,
                CreatedAt = document.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<ContextDocumentDto>>.Ok(documents));
    }

    [HttpPost]
    [Authorize(Roles = $"{ApplicationRoles.Admin},{ApplicationRoles.Developer}")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<ContextDocumentDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<ContextDocumentDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<ContextDocumentDto>>> UploadDocument(
        IFormFile file,
        [FromForm] string? name,
        CancellationToken cancellationToken)
    {
        var realmId = RealmAccess.ResolveRealmId(this);
        if (!RealmAccess.CanAccessRealm(this, realmId))
        {
            return Forbid();
        }

        if (file.Length == 0)
        {
            return BadRequest(ApiResponse<ContextDocumentDto>.Fail("File is empty."));
        }

        var extension = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(extension))
        {
            return BadRequest(ApiResponse<ContextDocumentDto>.Fail("Unsupported context document type."));
        }

        var directory = Path.Combine(_environment.ContentRootPath, "App_Data", "context-documents");
        Directory.CreateDirectory(directory);

        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var storagePath = Path.Combine(directory, storedFileName);
        await using (var stream = System.IO.File.Create(storagePath))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        var extractedText = await ExtractTextAsync(storagePath, extension, cancellationToken);
        var document = new ContextDocument
        {
            Name = string.IsNullOrWhiteSpace(name) ? Path.GetFileNameWithoutExtension(file.FileName) : name.Trim(),
            RealmId = realmId,
            FileName = file.FileName,
            ContentType = file.ContentType,
            FileExtension = extension,
            SizeBytes = file.Length,
            StoragePath = storagePath,
            ExtractedText = extractedText
        };

        await _dbContext.ContextDocuments.AddAsync(document, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var dto = new ContextDocumentDto
        {
            Id = document.Id,
            RealmId = document.RealmId,
            Name = document.Name,
            FileName = document.FileName,
            ContentType = document.ContentType,
            FileExtension = document.FileExtension,
            SizeBytes = document.SizeBytes,
            CreatedAt = document.CreatedAt
        };

        return CreatedAtAction(nameof(GetDocuments), ApiResponse<ContextDocumentDto>.Ok(dto, "Context document uploaded successfully."));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = $"{ApplicationRoles.Admin},{ApplicationRoles.Developer}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteDocument(Guid id, CancellationToken cancellationToken)
    {
        var document = await _dbContext.ContextDocuments
            .Include(item => item.Agents)
            .FirstOrDefaultAsync(item => item.Id == id && item.RealmId == RealmAccess.ResolveRealmId(this), cancellationToken);

        if (document is null)
        {
            return NoContent();
        }

        document.Agents.Clear();
        _dbContext.ContextDocuments.Remove(document);
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (System.IO.File.Exists(document.StoragePath))
        {
            System.IO.File.Delete(document.StoragePath);
        }

        return NoContent();
    }

    private static async Task<string> ExtractTextAsync(string path, string extension, CancellationToken cancellationToken)
    {
        if (extension.Equals(".txt", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".json", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".md", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".csv", StringComparison.OrdinalIgnoreCase))
        {
            return await System.IO.File.ReadAllTextAsync(path, Encoding.UTF8, cancellationToken);
        }

        if (extension.Equals(".docx", StringComparison.OrdinalIgnoreCase))
        {
            return ExtractDocxText(path);
        }

        if (extension.Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            return ExtractXlsxText(path);
        }

        return $"Uploaded binary document '{Path.GetFileName(path)}'. Full text extraction for {extension} can be added in the document ingestion pipeline.";
    }

    private static string ExtractDocxText(string path)
    {
        using var archive = ZipFile.OpenRead(path);
        var textParts = archive.Entries
            .Where(entry => entry.FullName.Equals("word/document.xml", StringComparison.OrdinalIgnoreCase)
                || entry.FullName.StartsWith("word/header", StringComparison.OrdinalIgnoreCase)
                || entry.FullName.StartsWith("word/footer", StringComparison.OrdinalIgnoreCase))
            .SelectMany(ReadXmlTextNodes);

        var text = string.Join(Environment.NewLine, textParts.Where(part => !string.IsNullOrWhiteSpace(part)));
        return string.IsNullOrWhiteSpace(text)
            ? "DOCX uploaded, but no readable text was found."
            : text;
    }

    private static string ExtractXlsxText(string path)
    {
        using var archive = ZipFile.OpenRead(path);
        var sharedStrings = archive.GetEntry("xl/sharedStrings.xml") is { } sharedEntry
            ? ReadXmlTextNodes(sharedEntry).ToList()
            : new List<string>();

        var rows = new List<string>();
        foreach (var sheetEntry in archive.Entries.Where(entry => entry.FullName.StartsWith("xl/worksheets/sheet", StringComparison.OrdinalIgnoreCase)))
        {
            using var stream = sheetEntry.Open();
            var document = XDocument.Load(stream);
            XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

            foreach (var row in document.Descendants(ns + "row"))
            {
                var cells = row.Elements(ns + "c")
                    .Select(cell => ReadCellValue(cell, ns, sharedStrings))
                    .Where(value => !string.IsNullOrWhiteSpace(value));

                var rowText = string.Join(" | ", cells);
                if (!string.IsNullOrWhiteSpace(rowText))
                {
                    rows.Add(rowText);
                }
            }
        }

        return rows.Count == 0
            ? "XLSX uploaded, but no readable cell text was found."
            : string.Join(Environment.NewLine, rows);
    }

    private static IEnumerable<string> ReadXmlTextNodes(ZipArchiveEntry entry)
    {
        using var stream = entry.Open();
        var document = XDocument.Load(stream);
        XNamespace word = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        XNamespace sheet = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

        return document.Descendants(word + "t")
            .Concat(document.Descendants(sheet + "t"))
            .Select(node => node.Value)
            .ToList();
    }

    private static string ReadCellValue(XElement cell, XNamespace ns, IReadOnlyList<string> sharedStrings)
    {
        var rawValue = cell.Element(ns + "v")?.Value ?? cell.Element(ns + "is")?.Element(ns + "t")?.Value ?? string.Empty;
        if (cell.Attribute("t")?.Value == "s"
            && int.TryParse(rawValue, out var sharedIndex)
            && sharedIndex >= 0
            && sharedIndex < sharedStrings.Count)
        {
            return sharedStrings[sharedIndex];
        }

        return rawValue;
    }
}
