using System;

namespace CWFlgma.Contracts.Teams;

public class TeamDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public long OwnerId { get; set; }
    public string? OwnerName { get; set; }
    public int MemberCount { get; set; }
    public int DocumentCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateTeamRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class UpdateTeamRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
}

public class TeamMemberDto
{
    public long Id { get; set; }
    public long TeamId { get; set; }
    public long UserId { get; set; }
    public string? UserName { get; set; }
    public string? UserEmail { get; set; }
    public string? UserAvatarUrl { get; set; }
    public string Role { get; set; } = "member";
    public DateTime JoinedAt { get; set; }
}

public class AddTeamMemberRequest
{
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "member";
}

public class UpdateTeamMemberRequest
{
    public string Role { get; set; } = "member";
}
