using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CWFlgma.Infrastructure;
using CWFlgma.Infrastructure.Authentication;
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

// Document endpoints (protected)
app.MapGet("/api/documents", async (HttpContext context, CWFlgmaDbContext db) =>
{
    var userId = context.GetCurrentUserId();
    if (userId == null) return Results.Unauthorized();

    var documents = await db.Documents
        .Where(d => d.OwnerId == userId && !d.IsArchived)
        .Select(d => new
        {
            d.Id,
            d.Title,
            d.Type,
            d.ThumbnailUrl,
            d.Width,
            d.Height,
            d.Version,
            d.UpdatedAt
        })
        .ToListAsync();

    return Results.Ok(documents);
})
.RequireAuthorization();

app.MapGet("/api/documents/{id:long}", async (long id, HttpContext context, CWFlgmaDbContext db) =>
{
    var userId = context.GetCurrentUserId();
    if (userId == null) return Results.Unauthorized();

    var document = await db.Documents
        .Include(d => d.Permissions)
        .FirstOrDefaultAsync(d => d.Id == id);

    if (document == null) return Results.NotFound();

    // Check permission
    if (document.OwnerId != userId && 
        !document.IsPublic && 
        !document.Permissions.Any(p => p.UserId == userId))
    {
        return Results.Forbid();
    }

    return Results.Ok(document);
})
.RequireAuthorization();

app.MapPost("/api/documents", async (CreateDocumentRequest request, HttpContext context, CWFlgmaDbContext db) =>
{
    var userId = context.GetCurrentUserId();
    if (userId == null) return Results.Unauthorized();

    var document = new Document
    {
        Id = IdGeneratorExtensions.NewId(),  // 雪花算法
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

app.MapPut("/api/documents/{id:long}", async (long id, UpdateDocumentRequest request, HttpContext context, CWFlgmaDbContext db) =>
{
    var userId = context.GetCurrentUserId();
    if (userId == null) return Results.Unauthorized();

    var document = await db.Documents.FindAsync(id);
    if (document == null) return Results.NotFound();

    // Check ownership or edit permission
    if (document.OwnerId != userId)
    {
        var hasPermission = await db.DocumentPermissions
            .AnyAsync(p => p.DocumentId == id && p.UserId == userId && p.Permission == "edit");
        if (!hasPermission) return Results.Forbid();
    }

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

app.MapDelete("/api/documents/{id:long}", async (long id, HttpContext context, CWFlgmaDbContext db) =>
{
    var userId = context.GetCurrentUserId();
    if (userId == null) return Results.Unauthorized();

    var document = await db.Documents.FindAsync(id);
    if (document == null) return Results.NotFound();

    // Check ownership
    if (document.OwnerId != userId) return Results.Forbid();

    document.IsArchived = true;
    document.UpdatedAt = DateTime.UtcNow;

    await db.SaveChangesAsync();

    return Results.NoContent();
})
.RequireAuthorization();

app.Run();

record CreateDocumentRequest(string Title, string? Description, long? TeamId, long? ParentId, string Type, int Width, int Height, string BackgroundColor);
record UpdateDocumentRequest(string? Title, string? Description, int? Width, int? Height, string? BackgroundColor, bool? IsPublic);
