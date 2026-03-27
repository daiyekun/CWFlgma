using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text.Json;
using System.Text.Json.Serialization;
using CWFlgma.Infrastructure;
using CWFlgma.Infrastructure.Authentication;
using CWFlgma.Infrastructure.Authorization;
using CWFlgma.Infrastructure.PostgreSQL;
using CWFlgma.Infrastructure.PostgreSQL.Entities;
using CWFlgma.Infrastructure.Common;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddOpenApi();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// ==================== Document endpoints with permission check ====================

app.MapGet("/api/documents", async (HttpContext context, CWFlgmaDbContext db) =>
{
    var userId = context.GetCurrentUserId();
    if (userId == null) return Results.Unauthorized();

    // 获取用户有权限访问的文档
    var documents = await db.Documents
        .Where(d => 
            d.OwnerId == userId || // 自己的文档
            d.IsPublic || // 公开文档
            db.DocumentPermissions.Any(p => p.DocumentId == d.Id && p.UserId == userId) || // 直接权限
            (d.TeamId != null && db.TeamMembers.Any(tm => tm.TeamId == d.TeamId && tm.UserId == userId)) // 团队文档
        )
        .Where(d => !d.IsArchived)
        .Select(d => new
        {
            d.Id,
            d.Title,
            d.Type,
            d.ThumbnailUrl,
            d.Width,
            d.Height,
            d.Version,
            d.OwnerId,
            d.IsPublic,
            d.UpdatedAt,
            Permission = d.OwnerId == userId ? "owner" :
                         d.IsPublic ? "view" :
                         db.DocumentPermissions.Where(p => p.DocumentId == d.Id && p.UserId == userId)
                             .Select(p => p.Permission).FirstOrDefault() ?? "view"
        })
        .ToListAsync();

    return Results.Ok(documents);
})
.RequireAuthorization();

app.MapGet("/api/documents/{id:long}", async (long id, HttpContext context, CWFlgmaDbContext db, IAuthorizationService authService) =>
{
    var userId = context.GetCurrentUserId();
    if (userId == null) return Results.Unauthorized();

    // 检查访问权限
    if (!await authService.CanAccessDocumentAsync(userId.Value, id))
        return Results.Forbid();

    var document = await db.Documents
        .Include(d => d.Owner)
        .FirstOrDefaultAsync(d => d.Id == id);

    if (document == null) return Results.NotFound();

    var permission = await authService.GetDocumentPermissionAsync(userId.Value, id);

    return Results.Ok(new
    {
        document.Id,
        document.Title,
        document.Description,
        document.OwnerId,
        OwnerName = document.Owner.DisplayName,
        document.TeamId,
        document.ParentId,
        document.Type,
        document.ThumbnailUrl,
        document.Content,  // 添加 Content 字段
        document.Width,
        document.Height,
        document.BackgroundColor,
        document.IsPublic,
        document.IsArchived,
        document.Version,
        document.CreatedAt,
        document.UpdatedAt,
        Permission = permission
    });
})
.RequireAuthorization();

app.MapPost("/api/documents", async (CreateDocumentRequest request, HttpContext context, CWFlgmaDbContext db) =>
{
    var userId = context.GetCurrentUserId();
    if (userId == null) return Results.Unauthorized();

    var document = new Document
    {
        Id = IdGeneratorExtensions.NewId(),
        Title = request.Title,
        Description = request.Description,
        OwnerId = userId.Value,
        TeamId = request.TeamId,
        ParentId = request.ParentId,
        Type = request.Type,
        Width = request.Width,
        Height = request.Height,
        BackgroundColor = request.BackgroundColor,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        Version = 1
    };

    db.Documents.Add(document);
    await db.SaveChangesAsync();

    return Results.Created($"/api/documents/{document.Id}", document);
})
.RequireAuthorization();

app.MapPut("/api/documents/{id:long}", async (long id, UpdateDocumentRequest request, HttpContext context, CWFlgmaDbContext db, IAuthorizationService authService) =>
{
    var userId = context.GetCurrentUserId();
    if (userId == null) return Results.Unauthorized();

    // 检查编辑权限
    if (!await authService.CanEditDocumentAsync(userId.Value, id))
        return Results.Forbid();

    var document = await db.Documents.FindAsync(id);
    if (document == null) return Results.NotFound();

    if (request.Title != null) document.Title = request.Title;
    if (request.Description != null) document.Description = request.Description;
    if (request.Width.HasValue) document.Width = request.Width.Value;
    if (request.Height.HasValue) document.Height = request.Height.Value;
    if (request.BackgroundColor != null) document.BackgroundColor = request.BackgroundColor;
    if (request.IsPublic.HasValue) document.IsPublic = request.IsPublic.Value;
    if (request.Content != null) document.Content = request.Content;
    document.UpdatedAt = DateTime.UtcNow;
    document.Version++;

    await db.SaveChangesAsync();

    return Results.Ok(document);
})
.RequireAuthorization();

app.MapDelete("/api/documents/{id:long}", async (long id, HttpContext context, CWFlgmaDbContext db, IAuthorizationService authService) =>
{
    var userId = context.GetCurrentUserId();
    if (userId == null) return Results.Unauthorized();

    // 检查管理权限（只有所有者可以删除）
    if (!await authService.CanManageDocumentAsync(userId.Value, id))
        return Results.Forbid();

    var document = await db.Documents.FindAsync(id);
    if (document == null) return Results.NotFound();

    document.IsArchived = true;
    document.UpdatedAt = DateTime.UtcNow;

    await db.SaveChangesAsync();

    return Results.NoContent();
})
.RequireAuthorization();

// ==================== Document Version History endpoints ====================

app.MapGet("/api/documents/{documentId:long}/versions", async (long documentId, HttpContext context, CWFlgmaDbContext db, IAuthorizationService authService) =>
{
    var userId = context.GetCurrentUserId();
    if (userId == null) return Results.Unauthorized();

    // 检查访问权限
    if (!await authService.CanAccessDocumentAsync(userId.Value, documentId))
        return Results.Forbid();

    var versions = await db.DocumentVersions
        .Where(v => v.DocumentId == documentId)
        .OrderByDescending(v => v.VersionNumber)
        .Select(v => new
        {
            v.Id,
            v.DocumentId,
            v.VersionNumber,
            v.Title,
            v.CreatedBy,
            v.CreatedAt,
            v.Comment,
            v.SnapshotUrl
        })
        .ToListAsync();

    return Results.Ok(versions);
})
.RequireAuthorization();

app.MapPost("/api/documents/{documentId:long}/versions", async (long documentId, CreateVersionRequest request, HttpContext context, CWFlgmaDbContext db, IAuthorizationService authService) =>
{
    var userId = context.GetCurrentUserId();
    if (userId == null) return Results.Unauthorized();

    // 检查编辑权限
    if (!await authService.CanEditDocumentAsync(userId.Value, documentId))
        return Results.Forbid();

    var document = await db.Documents.FindAsync(documentId);
    if (document == null) return Results.NotFound(new { error = "文档不存在" });

    // 获取当前最大版本号
    var maxVersion = await db.DocumentVersions
        .Where(v => v.DocumentId == documentId)
        .MaxAsync(v => (int?)v.VersionNumber) ?? 0;

    var version = new DocumentVersion
    {
        Id = IdGeneratorExtensions.NewId(),
        DocumentId = documentId,
        VersionNumber = maxVersion + 1,
        Title = request.Title ?? document.Title,
        CreatedBy = userId.Value,
        CreatedAt = DateTime.UtcNow,
        Comment = request.Comment,
        SnapshotUrl = request.SnapshotUrl
    };

    db.DocumentVersions.Add(version);

    // 更新文档版本号
    document.Version = version.VersionNumber;
    document.UpdatedAt = DateTime.UtcNow;

    await db.SaveChangesAsync();

    return Results.Created($"/api/documents/{documentId}/versions/{version.Id}", version);
})
.RequireAuthorization();

app.MapGet("/api/documents/{documentId:long}/versions/{versionId:long}", async (long documentId, long versionId, HttpContext context, CWFlgmaDbContext db, IAuthorizationService authService) =>
{
    var userId = context.GetCurrentUserId();
    if (userId == null) return Results.Unauthorized();

    // 检查访问权限
    if (!await authService.CanAccessDocumentAsync(userId.Value, documentId))
        return Results.Forbid();

    var version = await db.DocumentVersions
        .FirstOrDefaultAsync(v => v.Id == versionId && v.DocumentId == documentId);

    if (version == null) return Results.NotFound(new { error = "版本不存在" });

    return Results.Ok(version);
})
.RequireAuthorization();

app.MapPost("/api/documents/{documentId:long}/versions/{versionId:long}/restore", async (long documentId, long versionId, HttpContext context, CWFlgmaDbContext db, IAuthorizationService authService) =>
{
    var userId = context.GetCurrentUserId();
    if (userId == null) return Results.Unauthorized();

    // 检查编辑权限
    if (!await authService.CanEditDocumentAsync(userId.Value, documentId))
        return Results.Forbid();

    var document = await db.Documents.FindAsync(documentId);
    if (document == null) return Results.NotFound(new { error = "文档不存在" });

    var version = await db.DocumentVersions
        .FirstOrDefaultAsync(v => v.Id == versionId && v.DocumentId == documentId);

    if (version == null) return Results.NotFound(new { error = "版本不存在" });

    // 先保存当前状态为新版本
    var maxVersion = await db.DocumentVersions
        .Where(v => v.DocumentId == documentId)
        .MaxAsync(v => (int?)v.VersionNumber) ?? 0;

    var backupVersion = new DocumentVersion
    {
        Id = IdGeneratorExtensions.NewId(),
        DocumentId = documentId,
        VersionNumber = maxVersion + 1,
        Title = document.Title,
        CreatedBy = userId.Value,
        CreatedAt = DateTime.UtcNow,
        Comment = $"回滚前自动备份"
    };

    db.DocumentVersions.Add(backupVersion);

    // 恢复到指定版本
    if (version.Title != null) document.Title = version.Title;
    document.Version = backupVersion.VersionNumber + 1;
    document.UpdatedAt = DateTime.UtcNow;

    await db.SaveChangesAsync();

    return Results.Ok(new
    {
        message = $"已回滚到版本 {version.VersionNumber}",
        newVersion = document.Version,
        backupVersionId = backupVersion.Id
    });
})
.RequireAuthorization();

app.MapGet("/api/documents/{documentId:long}/versions/compare", async (long documentId, long v1, long v2, HttpContext context, CWFlgmaDbContext db, IAuthorizationService authService) =>
{
    var userId = context.GetCurrentUserId();
    if (userId == null) return Results.Unauthorized();

    // 检查访问权限
    if (!await authService.CanAccessDocumentAsync(userId.Value, documentId))
        return Results.Forbid();

    var version1 = await db.DocumentVersions
        .FirstOrDefaultAsync(v => v.Id == v1 && v.DocumentId == documentId);

    var version2 = await db.DocumentVersions
        .FirstOrDefaultAsync(v => v.Id == v2 && v.DocumentId == documentId);

    if (version1 == null || version2 == null)
        return Results.NotFound(new { error = "版本不存在" });

    return Results.Ok(new
    {
        Version1 = version1,
        Version2 = version2,
        Differences = new
        {
            TitleChanged = version1.Title != version2.Title,
            TimeDiff = (version2.CreatedAt - version1.CreatedAt).ToString()
        }
    });
})
.RequireAuthorization();

app.Run();

record CreateDocumentRequest(string Title, string? Description, long? TeamId, long? ParentId, string Type, int Width, int Height, string BackgroundColor);
record UpdateDocumentRequest(string? Title, string? Description, int? Width, int? Height, string? BackgroundColor, bool? IsPublic, string? Content);
record CreateVersionRequest(string? Title, string? Comment, string? SnapshotUrl);
