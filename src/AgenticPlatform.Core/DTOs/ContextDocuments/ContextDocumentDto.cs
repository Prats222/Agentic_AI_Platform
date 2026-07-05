namespace AgenticPlatform.Core.DTOs.ContextDocuments;

public sealed class ContextDocumentDto
{
    public Guid Id { get; set; }
    public Guid RealmId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string FileExtension { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
