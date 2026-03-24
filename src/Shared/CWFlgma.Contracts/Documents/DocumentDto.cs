using System;

namespace CWFlgma.Contracts.Documents;

public class DocumentDto
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public long OwnerId { get; set; }
    public string? OwnerName { get; set; }
    public long? TeamId { get; set; }
    public string? TeamName { get; set; }
    public long? ParentId { get; set; }
    public string Type { get; set; } = "design";
    public string? ThumbnailUrl { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public string BackgroundColor { get; set; } = "#FFFFFF";
    public bool IsPublic { get; set; }
    public bool IsArchived { get; set; }
    public int Version { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? Permission { get; set; }
}

public class CreateDocumentRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public long? TeamId { get; set; }
    public long? ParentId { get; set; }
    public string Type { get; set; } = "design";
    public int Width { get; set; } = 1920;
    public int Height { get; set; } = 1080;
    public string BackgroundColor { get; set; } = "#FFFFFF";
}

public class UpdateDocumentRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public string? BackgroundColor { get; set; }
    public bool? IsPublic { get; set; }
}

public class DocumentVersionDto
{
    public long Id { get; set; }
    public long DocumentId { get; set; }
    public int VersionNumber { get; set; }
    public string? Title { get; set; }
    public long CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Comment { get; set; }
    public string? SnapshotUrl { get; set; }
}

public class CreateVersionRequest
{
    public string? Title { get; set; }
    public string? Comment { get; set; }
}

public class DocumentPermissionDto
{
    public long Id { get; set; }
    public long DocumentId { get; set; }
    public long? UserId { get; set; }
    public string? UserName { get; set; }
    public long? TeamId { get; set; }
    public string? TeamName { get; set; }
    public string Permission { get; set; } = "view";
    public long GrantedBy { get; set; }
    public DateTime GrantedAt { get; set; }
}

public class AddPermissionRequest
{
    public long? UserId { get; set; }
    public long? TeamId { get; set; }
    public string Permission { get; set; } = "view";
}

public class UpdatePermissionRequest
{
    public string Permission { get; set; } = "view";
}

public class ShareLinkResponse
{
    public string ShareUrl { get; set; } = string.Empty;
    public DateTime? ExpiresAt { get; set; }
}
