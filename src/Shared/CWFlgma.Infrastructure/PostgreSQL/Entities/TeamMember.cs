using System;

namespace CWFlgma.Infrastructure.PostgreSQL.Entities;

public class TeamMember
{
    public long Id { get; set; }
    public long TeamId { get; set; }
    public long UserId { get; set; }
    public string Role { get; set; } = "member";
    public DateTime JoinedAt { get; set; }

    // Navigation properties
    public Team Team { get; set; } = null!;
    public User User { get; set; } = null!;
}
