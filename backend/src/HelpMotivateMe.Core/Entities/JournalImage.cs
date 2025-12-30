namespace HelpMotivateMe.Core.Entities;

public class JournalImage
{
    public Guid Id { get; set; }
    public Guid JournalEntryId { get; set; }
    public required string FileName { get; set; }
    public required string S3Key { get; set; }
    public required string ContentType { get; set; }
    public long FileSizeBytes { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public JournalEntry JournalEntry { get; set; } = null!;
}
