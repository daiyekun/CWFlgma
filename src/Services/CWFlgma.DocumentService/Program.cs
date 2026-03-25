using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
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

app.Run();

record CreateDocumentRequest(string Title, string? Description, long? TeamId, long? ParentId, string Type, int Width, int Height, string BackgroundColor);
record UpdateDocumentRequest(string? Title, string? Description, int? Width, int? Height, string? BackgroundColor, bool? IsPublic);
