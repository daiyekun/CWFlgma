using System;

namespace CWFlgma.Infrastructure.PostgreSQL.Entities;

public class DocumentPermission
{
    public long Id { get; set; }
    public long DocumentId { get; set; }
    public long? UserId { get; set; }
    public long? TeamId { get; set; }
    public string Permission { get; set; } = "view";
    public long GrantedBy { get; set; }
    public DateTime GrantedAt { get; set; }

    // Navigation properties
    public Document Document { get; set; } = null!;
    public User? User { get; set; }
    public Team? Team { get; set; }
}
