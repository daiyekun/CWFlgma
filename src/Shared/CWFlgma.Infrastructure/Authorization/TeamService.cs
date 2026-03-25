using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CWFlgma.Infrastructure.PostgreSQL;
using CWFlgma.Infrastructure.PostgreSQL.Entities;
using CWFlgma.Infrastructure.Common;

namespace CWFlgma.Infrastructure.Authorization;

public interface ITeamService
{
    Task<Team> CreateTeamAsync(long ownerId, string name, string? description);
    Task<Team?> GetTeamAsync(long teamId);
    Task<List<Team>> GetUserTeamsAsync(long userId);
    Task<Team> UpdateTeamAsync(long teamId, long userId, string? name, string? description);
    Task<bool> DeleteTeamAsync(long teamId, long userId);
    
    Task<TeamMember> AddMemberAsync(long teamId, long inviterId, string email, string role);
    Task<bool> RemoveMemberAsync(long teamId, long removerId, long memberId);
    Task<TeamMember> UpdateMemberRoleAsync(long teamId, long updaterId, long memberId, string role);
    Task<List<TeamMember>> GetTeamMembersAsync(long teamId, long userId);
}

public class TeamService : ITeamService
{
    private readonly CWFlgmaDbContext _context;
    private readonly IAuthorizationService _authorizationService;

    public TeamService(CWFlgmaDbContext context, IAuthorizationService authorizationService)
    {
        _context = context;
        _authorizationService = authorizationService;
    }

    public async Task<Team> CreateTeamAsync(long ownerId, string name, string? description)
    {
        var team = new Team
        {
            Id = IdGeneratorExtensions.NewId(),
            Name = name,
            Description = description,
            OwnerId = ownerId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Teams.Add(team);

        // 创建者自动成为 owner
        var member = new TeamMember
        {
            Id = IdGeneratorExtensions.NewId(),
            TeamId = team.Id,
            UserId = ownerId,
            Role = "owner",
            JoinedAt = DateTime.UtcNow
        };

        _context.TeamMembers.Add(member);
        await _context.SaveChangesAsync();

        return team;
    }

    public async Task<Team?> GetTeamAsync(long teamId)
    {
        return await _context.Teams.FindAsync(teamId);
    }

    public async Task<List<Team>> GetUserTeamsAsync(long userId)
    {
        return await _context.TeamMembers
            .Where(tm => tm.UserId == userId)
            .Select(tm => tm.Team)
            .ToListAsync();
    }

    public async Task<Team> UpdateTeamAsync(long teamId, long userId, string? name, string? description)
    {
        if (!await _authorizationService.CanManageTeamAsync(userId, teamId))
            throw new UnauthorizedAccessException("没有权限修改团队");

        var team = await _context.Teams.FindAsync(teamId)
            ?? throw new KeyNotFoundException("团队不存在");

        if (name != null) team.Name = name;
        if (description != null) team.Description = description;
        team.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return team;
    }

    public async Task<bool> DeleteTeamAsync(long teamId, long userId)
    {
        var team = await _context.Teams.FindAsync(teamId);
        if (team == null) return false;

        // 只有所有者可以删除团队
        if (team.OwnerId != userId)
            throw new UnauthorizedAccessException("只有团队所有者可以删除团队");

        _context.Teams.Remove(team);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<TeamMember> AddMemberAsync(long teamId, long inviterId, string email, string role)
    {
        if (!await _authorizationService.CanManageTeamAsync(inviterId, teamId))
            throw new UnauthorizedAccessException("没有权限添加成员");

        // 查找用户
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email)
            ?? throw new KeyNotFoundException("用户不存在");

        // 检查是否已是成员
        var existing = await _context.TeamMembers
            .FirstOrDefaultAsync(tm => tm.TeamId == teamId && tm.UserId == user.Id);

        if (existing != null)
            throw new InvalidOperationException("用户已是团队成员");

        var member = new TeamMember
        {
            Id = IdGeneratorExtensions.NewId(),
            TeamId = teamId,
            UserId = user.Id,
            Role = role,
            JoinedAt = DateTime.UtcNow
        };

        _context.TeamMembers.Add(member);
        await _context.SaveChangesAsync();

        return member;
    }

    public async Task<bool> RemoveMemberAsync(long teamId, long removerId, long memberId)
    {
        if (!await _authorizationService.CanManageTeamAsync(removerId, teamId))
            throw new UnauthorizedAccessException("没有权限移除成员");

        var member = await _context.TeamMembers
            .FirstOrDefaultAsync(tm => tm.TeamId == teamId && tm.Id == memberId);

        if (member == null) return false;

        // 不能移除所有者
        if (member.Role == "owner")
            throw new InvalidOperationException("不能移除团队所有者");

        _context.TeamMembers.Remove(member);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<TeamMember> UpdateMemberRoleAsync(long teamId, long updaterId, long memberId, string role)
    {
        if (!await _authorizationService.CanManageTeamAsync(updaterId, teamId))
            throw new UnauthorizedAccessException("没有权限修改成员角色");

        var member = await _context.TeamMembers
            .FirstOrDefaultAsync(tm => tm.TeamId == teamId && tm.Id == memberId)
            ?? throw new KeyNotFoundException("成员不存在");

        // 不能修改所有者角色
        if (member.Role == "owner")
            throw new InvalidOperationException("不能修改所有者角色");

        member.Role = role;
        await _context.SaveChangesAsync();

        return member;
    }

    public async Task<List<TeamMember>> GetTeamMembersAsync(long teamId, long userId)
    {
        if (!await _authorizationService.IsTeamMemberAsync(userId, teamId))
            throw new UnauthorizedAccessException("不是团队成员");

        return await _context.TeamMembers
            .Where(tm => tm.TeamId == teamId)
            .Include(tm => tm.User)
            .ToListAsync();
    }
}
