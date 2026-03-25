using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CWFlgma.Infrastructure.PostgreSQL;
using CWFlgma.Infrastructure.PostgreSQL.Entities;
using CWFlgma.Infrastructure.Common;

namespace CWFlgma.Infrastructure.Authorization;

public interface IDocumentPermissionService
{
    Task<DocumentPermission> GrantPermissionAsync(long documentId, long granterId, long? userId, long? teamId, string permission);
    Task<bool> RevokePermissionAsync(long documentId, long revokerId, long permissionId);
    Task<List<DocumentPermission>> GetDocumentPermissionsAsync(long documentId, long userId);
    Task<string> GenerateShareLinkAsync(long documentId, long userId, string permission, DateTime? expiresAt);
    Task<string?> ValidateShareLinkAsync(string shareToken);
}

public class DocumentPermissionService : IDocumentPermissionService
{
    private readonly CWFlgmaDbContext _context;
    private readonly IAuthorizationService _authorizationService;

    public DocumentPermissionService(CWFlgmaDbContext context, IAuthorizationService authorizationService)
    {
        _context = context;
        _authorizationService = authorizationService;
    }

    public async Task<DocumentPermission> GrantPermissionAsync(
        long documentId, long granterId, long? userId, long? teamId, string permission)
    {
        if (!await _authorizationService.CanManageDocumentAsync(granterId, documentId))
            throw new UnauthorizedAccessException("没有权限授予文档权限");

        // 验证授权者是否存在
        var granterExists = await _context.Users.AnyAsync(u => u.Id == granterId);
        if (!granterExists)
            throw new ArgumentException($"授权者 {granterId} 不存在");

        // 验证用户是否存在
        if (userId.HasValue)
        {
            var userExists = await _context.Users.AnyAsync(u => u.Id == userId.Value);
            if (!userExists)
                throw new ArgumentException($"用户 {userId.Value} 不存在");
        }

        // 验证团队是否存在
        if (teamId.HasValue)
        {
            var teamExists = await _context.Teams.AnyAsync(t => t.Id == teamId.Value);
            if (!teamExists)
                throw new ArgumentException($"团队 {teamId.Value} 不存在");
        }

        // 检查是否已有权限
        var existing = await _context.DocumentPermissions
            .FirstOrDefaultAsync(p => 
                p.DocumentId == documentId && 
                p.UserId == userId && 
                p.TeamId == teamId);

        if (existing != null)
        {
            existing.Permission = permission;
            existing.GrantedBy = granterId;
            existing.GrantedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return existing;
        }

        var docPermission = new DocumentPermission
        {
            Id = IdGeneratorExtensions.NewId(),
            DocumentId = documentId,
            UserId = userId,
            TeamId = teamId,
            Permission = permission,
            GrantedBy = granterId,
            GrantedAt = DateTime.UtcNow
        };

        _context.DocumentPermissions.Add(docPermission);
        await _context.SaveChangesAsync();

        return docPermission;
    }

    public async Task<bool> RevokePermissionAsync(long documentId, long revokerId, long permissionId)
    {
        if (!await _authorizationService.CanManageDocumentAsync(revokerId, documentId))
            throw new UnauthorizedAccessException("没有权限撤销文档权限");

        var permission = await _context.DocumentPermissions
            .FirstOrDefaultAsync(p => p.Id == permissionId && p.DocumentId == documentId);

        if (permission == null) return false;

        _context.DocumentPermissions.Remove(permission);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<DocumentPermission>> GetDocumentPermissionsAsync(long documentId, long userId)
    {
        if (!await _authorizationService.CanAccessDocumentAsync(userId, documentId))
            throw new UnauthorizedAccessException("没有权限查看文档权限");

        return await _context.DocumentPermissions
            .Where(p => p.DocumentId == documentId)
            .Include(p => p.User)
            .Include(p => p.Team)
            .ToListAsync();
    }

    public async Task<string> GenerateShareLinkAsync(
        long documentId, long userId, string permission, DateTime? expiresAt)
    {
        if (!await _authorizationService.CanManageDocumentAsync(userId, documentId))
            throw new UnauthorizedAccessException("没有权限生成分享链接");

        // 生成分享令牌
        var shareToken = Guid.NewGuid().ToString("N");

        // 存储分享信息（这里简化处理，生产环境应存储到数据库）
        var document = await _context.Documents.FindAsync(documentId);
        if (document != null)
        {
            // 更新文档的分享状态
            document.IsPublic = true;
            await _context.SaveChangesAsync();
        }

        return shareToken;
    }

    public async Task<string?> ValidateShareLinkAsync(string shareToken)
    {
        // 简化实现：直接返回文档ID
        // 生产环境应从数据库验证令牌
        return await Task.FromResult<string?>(null);
    }
}
