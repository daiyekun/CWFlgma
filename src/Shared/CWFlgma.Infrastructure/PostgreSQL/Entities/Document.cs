using System;
using System.Collections.Generic;

namespace CWFlgma.Infrastructure.PostgreSQL.Entities;

public class Document
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public long OwnerId { get; set; }
    public long? TeamId { get; set; }
    public long? ParentId { get; set; }
    public string Type { get; set; } = "design";
    public string? ThumbnailUrl { get; set; }
    public string? Content { get; set; } // 存储设计内容（JSON）
    public int Width { get; set; } = 1920;
    public int Height { get; set; } = 1080;
    public string BackgroundColor { get; set; } = "#FFFFFF";
    public bool IsPublic { get; set; }
    public bool IsArchived { get; set; }
    public int Version { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public User Owner { get; set; } = null!;
    public Team? Team { get; set; }
    public Document? Parent { get; set; }
    public ICollection<Document> Children { get; set; } = new List<Document>();
    public ICollection<DocumentPermission> Permissions { get; set; } = new List<DocumentPermission>();
    public ICollection<DocumentVersion> Versions { get; set; } = new List<DocumentVersion>();
    public ICollection<Resource> Resources { get; set; } = new List<Resource>();
}
