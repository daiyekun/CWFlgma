using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CWFlgma.Infrastructure.PostgreSQL;
using CWFlgma.Infrastructure.PostgreSQL.Entities;

namespace CWFlgma.Infrastructure.Authorization;

public interface IAuthorizationService
{
    // 团队权限
    Task<bool> CanManageTeamAsync(long userId, long teamId);
    Task<bool> IsTeamMemberAsync(long userId, long teamId);
    Task<string?> GetTeamRoleAsync(long userId, long teamId);
    
    // 文档权限
    Task<bool> CanAccessDocumentAsync(long userId, long documentId);
    Task<bool> CanEditDocumentAsync(long userId, long documentId);
    Task<bool> CanManageDocumentAsync(long userId, long documentId);
    Task<string?> GetDocumentPermissionAsync(long userId, long documentId);
}

public class AuthorizationService : IAuthorizationService
{
    private readonly CWFlgmaDbContext _context;

    public AuthorizationService(CWFlgmaDbContext context)
    {
        _context = context;
    }

    #region 团队权限

    public async Task<bool> CanManageTeamAsync(long userId, long teamId)
    {
        var role = await GetTeamRoleAsync(userId, teamId);
        return role == "owner" || role == "admin";
    }

    public async Task<bool> IsTeamMemberAsync(long userId, long teamId)
    {
        return await _context.TeamMembers
            .AnyAsync(tm => tm.TeamId == teamId && tm.UserId == userId);
    }

    public async Task<string?> GetTeamRoleAsync(long userId, long teamId)
    {
        var member = await _context.TeamMembers
            .FirstOrDefaultAsync(tm => tm.TeamId == teamId && tm.UserId == userId);
        return member?.Role;
    }

    #endregion

    #region 文档权限

    public async Task<bool> CanAccessDocumentAsync(long userId, long documentId)
    {
        var permission = await GetDocumentPermissionAsync(userId, documentId);
        return permission != null;
    }

    public async Task<bool> CanEditDocumentAsync(long userId, long documentId)
    {
        var permission = await GetDocumentPermissionAsync(userId, documentId);
        return permission == "edit" || permission == "admin" || permission == "owner";
    }

    public async Task<bool> CanManageDocumentAsync(long userId, long documentId)
    {
        var permission = await GetDocumentPermissionAsync(userId, documentId);
        return permission == "admin" || permission == "owner";
    }

    public async Task<string?> GetDocumentPermissionAsync(long userId, long documentId)
    {
        // 1. 检查是否是文档所有者
        var document = await _context.Documents
            .FirstOrDefaultAsync(d => d.Id == documentId);
        
        if (document == null)
            return null;
        
        if (document.OwnerId == userId)
            return "owner";
        
        // 2. 检查文档是否公开
        if (document.IsPublic)
            return "view";
        
        // 3. 检查用户直接权限
        var userPermission = await _context.DocumentPermissions
            .FirstOrDefaultAsync(p => p.DocumentId == documentId && p.UserId == userId);
        
        if (userPermission != null)
            return userPermission.Permission;
        
        // 4. 检查团队权限
        if (document.TeamId.HasValue)
        {
            var teamMember = await _context.TeamMembers
                .FirstOrDefaultAsync(tm => tm.TeamId == document.TeamId.Value && tm.UserId == userId);
            
            if (teamMember != null)
            {
                // 团队成员默认有查看权限
                // 检查团队级别的文档权限
                var teamPermission = await _context.DocumentPermissions
                    .FirstOrDefaultAsync(p => p.DocumentId == documentId && p.TeamId == document.TeamId.Value);
                
                return teamPermission?.Permission ?? "view";
            }
        }
        
        return null;
    }

    #endregion
}
