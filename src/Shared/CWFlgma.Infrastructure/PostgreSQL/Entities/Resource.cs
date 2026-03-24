using System;

namespace CWFlgma.Infrastructure.PostgreSQL.Entities;

public class Resource
{
    public long Id { get; set; }
    public long? DocumentId { get; set; }
    public string Type { get; set; } = "image";
    public string Name { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string? MimeType { get; set; }
    public long FileSize { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public long UploadedBy { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public Document? Document { get; set; }
}
