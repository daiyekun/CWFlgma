using System;
using System.Collections.Generic;

namespace CWFlgma.Infrastructure.PostgreSQL.Entities;

public class Team
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public long OwnerId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public User Owner { get; set; } = null!;
    public ICollection<TeamMember> Members { get; set; } = new List<TeamMember>();
    public ICollection<Document> Documents { get; set; } = new List<Document>();
}
