namespace AgenticPlatform.Core.Entities;

public sealed class ContextDocument : BaseEntity
{
    public Guid RealmId { get; set; }
    public Realm? Realm { get; set; }

    public string Name { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string FileExtension { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string ExtractedText { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;

    public ICollection<Agent> Agents { get; set; } = new List<Agent>();
}
