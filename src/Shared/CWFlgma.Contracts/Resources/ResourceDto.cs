using System;

namespace CWFlgma.Contracts.Resources;

public class ResourceDto
{
    public long Id { get; set; }
    public long? DocumentId { get; set; }
    public string Type { get; set; } = "image";
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? MimeType { get; set; }
    public long FileSize { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public long UploadedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class FontDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Family { get; set; } = string.Empty;
    public List<FontVariantDto> Variants { get; set; } = new();
}

public class FontVariantDto
{
    public int Weight { get; set; }
    public string Style { get; set; } = "normal";
    public string Url { get; set; } = string.Empty;
}

public class UploadImageResponse
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "image";
    public string Url { get; set; } = string.Empty;
    public string? MimeType { get; set; }
    public long FileSize { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public DateTime CreatedAt { get; set; }
}
