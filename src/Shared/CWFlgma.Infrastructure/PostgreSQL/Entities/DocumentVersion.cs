using System;

namespace CWFlgma.Infrastructure.PostgreSQL.Entities;

public class DocumentVersion
{
    public long Id { get; set; }
    public long DocumentId { get; set; }
    public int VersionNumber { get; set; }
    public string? Title { get; set; }
    public long CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Comment { get; set; }
    public string? SnapshotUrl { get; set; }

    // Navigation properties
    public Document Document { get; set; } = null!;
}
