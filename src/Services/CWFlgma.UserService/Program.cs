using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text.Json;
using System.Text.Json.Serialization;
using CWFlgma.Infrastructure;
using CWFlgma.Infrastructure.Authentication;
using CWFlgma.Infrastructure.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddOpenApi();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

// ==================== Auth endpoints ====================

app.MapPost("/api/auth/login", async (LoginRequest request, IAuthenticationService authService) =>
{
    var result = await authService.LoginAsync(request);
    if (!result.Success)
        return Results.BadRequest(new { error = result.ErrorMessage });
    return Results.Ok(result);
});

app.MapPost("/api/auth/register", async (RegisterRequest request, IAuthenticationService authService) =>
{
    var result = await authService.RegisterAsync(request);
    if (!result.Success)
        return Results.BadRequest(new { error = result.ErrorMessage });
    return Results.Created($"/api/users/{result.User?.Id}", result);
});

app.MapPost("/api/auth/refresh", async (RefreshTokenRequest request, IAuthenticationService authService) =>
{
    var result = await authService.RefreshTokenAsync(request);
    if (!result.Success)
        return Results.BadRequest(new { error = result.ErrorMessage });
    return Results.Ok(result);
});

// ==================== Team endpoints ====================

app.MapPost("/api/teams", async (CreateTeamRequest request, HttpContext context, ITeamService teamService) =>
{
    var userId = context.GetCurrentUserId();
    if (userId == null) return Results.Unauthorized();

    try
    {
        var team = await teamService.CreateTeamAsync(userId.Value, request.Name, request.Description);
        return Results.Created($"/api/teams/{team.Id}", new
        {
            team.Id,
            team.Name,
            team.Description,
            team.OwnerId,
            team.CreatedAt,
            team.UpdatedAt
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.RequireAuthorization();

app.MapGet("/api/teams", async (HttpContext context, ITeamService teamService) =>
{
    var userId = context.GetCurrentUserId();
    if (userId == null) return Results.Unauthorized();

    var teams = await teamService.GetUserTeamsAsync(userId.Value);
    return Results.Ok(teams.Select(t => new
    {
        t.Id,
        t.Name,
        t.Description,
        t.OwnerId,
        t.CreatedAt,
        t.UpdatedAt
    }));
})
.RequireAuthorization();

app.MapGet("/api/teams/{teamId:long}", async (long teamId, HttpContext context, ITeamService teamService) =>
{
    var userId = context.GetCurrentUserId();
    if (userId == null) return Results.Unauthorized();

    var team = await teamService.GetTeamAsync(teamId);
    if (team == null) return Results.NotFound();

    return Results.Ok(new
    {
        team.Id,
        team.Name,
        team.Description,
        team.OwnerId,
        team.CreatedAt,
        team.UpdatedAt
    });
})
.RequireAuthorization();

app.MapPut("/api/teams/{teamId:long}", async (long teamId, UpdateTeamRequest request, HttpContext context, ITeamService teamService) =>
{
    var userId = context.GetCurrentUserId();
    if (userId == null) return Results.Unauthorized();

    try
    {
        var team = await teamService.UpdateTeamAsync(teamId, userId.Value, request.Name, request.Description);
        return Results.Ok(team);
    }
    catch (UnauthorizedAccessException ex)
    {
        return Results.Forbid();
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.RequireAuthorization();

app.MapDelete("/api/teams/{teamId:long}", async (long teamId, HttpContext context, ITeamService teamService) =>
{
    var userId = context.GetCurrentUserId();
    if (userId == null) return Results.Unauthorized();

    try
    {
        var result = await teamService.DeleteTeamAsync(teamId, userId.Value);
        return result ? Results.NoContent() : Results.NotFound();
    }
    catch (UnauthorizedAccessException)
    {
        return Results.Forbid();
    }
})
.RequireAuthorization();

// ==================== Team Members endpoints ====================

app.MapGet("/api/teams/{teamId:long}/members", async (long teamId, HttpContext context, ITeamService teamService) =>
{
    var userId = context.GetCurrentUserId();
    if (userId == null) return Results.Unauthorized();

    try
    {
        var members = await teamService.GetTeamMembersAsync(teamId, userId.Value);
        return Results.Ok(members.Select(m => new
        {
            m.Id,
            m.TeamId,
            m.UserId,
            m.Role,
            m.JoinedAt,
            User = m.User != null ? new
            {
                m.User.Id,
                m.User.Username,
                m.User.Email,
                m.User.DisplayName,
                m.User.AvatarUrl
            } : null
        }));
    }
    catch (UnauthorizedAccessException)
    {
        return Results.Forbid();
    }
})
.RequireAuthorization();

app.MapPost("/api/teams/{teamId:long}/members", async (long teamId, AddMemberRequest request, HttpContext context, ITeamService teamService) =>
{
    var userId = context.GetCurrentUserId();
    if (userId == null) return Results.Unauthorized();

    try
    {
        var member = await teamService.AddMemberAsync(teamId, userId.Value, request.Email, request.Role);
        return Results.Created($"/api/teams/{teamId}/members/{member.Id}", member);
    }
    catch (UnauthorizedAccessException)
    {
        return Results.Forbid();
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.RequireAuthorization();

app.MapDelete("/api/teams/{teamId:long}/members/{memberId:long}", async (long teamId, long memberId, HttpContext context, ITeamService teamService) =>
{
    var userId = context.GetCurrentUserId();
    if (userId == null) return Results.Unauthorized();

    try
    {
        var result = await teamService.RemoveMemberAsync(teamId, userId.Value, memberId);
        return result ? Results.NoContent() : Results.NotFound();
    }
    catch (UnauthorizedAccessException)
    {
        return Results.Forbid();
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.RequireAuthorization();

app.MapPut("/api/teams/{teamId:long}/members/{memberId:long}", async (long teamId, long memberId, UpdateMemberRoleRequest request, HttpContext context, ITeamService teamService) =>
{
    var userId = context.GetCurrentUserId();
    if (userId == null) return Results.Unauthorized();

    try
    {
        var member = await teamService.UpdateMemberRoleAsync(teamId, userId.Value, memberId, request.Role);
        return Results.Ok(member);
    }
    catch (UnauthorizedAccessException)
    {
        return Results.Forbid();
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.RequireAuthorization();

// ==================== Document Permission endpoints ====================

app.MapGet("/api/documents/{documentId:long}/permissions", async (long documentId, HttpContext context, IDocumentPermissionService permissionService) =>
{
    var userId = context.GetCurrentUserId();
    if (userId == null) return Results.Unauthorized();

    try
    {
        var permissions = await permissionService.GetDocumentPermissionsAsync(documentId, userId.Value);
        return Results.Ok(permissions);
    }
    catch (UnauthorizedAccessException)
    {
        return Results.Forbid();
    }
})
.RequireAuthorization();

app.MapPost("/api/documents/{documentId:long}/permissions", async (long documentId, GrantPermissionRequest request, HttpContext context, IDocumentPermissionService permissionService) =>
{
    var userId = context.GetCurrentUserId();
    if (userId == null) return Results.Unauthorized();

    try
    {
        var permission = await permissionService.GrantPermissionAsync(
            documentId, userId.Value, request.GetUserId(), request.GetTeamId(), request.Permission);
        return Results.Created($"/api/documents/{documentId}/permissions/{permission.Id}", permission);
    }
    catch (UnauthorizedAccessException)
    {
        return Results.Forbid();
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.RequireAuthorization();

app.MapDelete("/api/documents/{documentId:long}/permissions/{permissionId:long}", async (long documentId, long permissionId, HttpContext context, IDocumentPermissionService permissionService) =>
{
    var userId = context.GetCurrentUserId();
    if (userId == null) return Results.Unauthorized();

    try
    {
        var result = await permissionService.RevokePermissionAsync(documentId, userId.Value, permissionId);
        return result ? Results.NoContent() : Results.NotFound();
    }
    catch (UnauthorizedAccessException)
    {
        return Results.Forbid();
    }
})
.RequireAuthorization();

app.MapPost("/api/documents/{documentId:long}/share", async (long documentId, ShareDocumentRequest request, HttpContext context, IDocumentPermissionService permissionService) =>
{
    var userId = context.GetCurrentUserId();
    if (userId == null) return Results.Unauthorized();

    try
    {
        var token = await permissionService.GenerateShareLinkAsync(
            documentId, userId.Value, request.Permission, request.ExpiresAt);
        return Results.Ok(new { shareToken = token, expiresAt = request.ExpiresAt });
    }
    catch (UnauthorizedAccessException)
    {
        return Results.Forbid();
    }
})
.RequireAuthorization();

app.Run();

// Request DTOs
record CreateTeamRequest(string Name, string? Description);
record UpdateTeamRequest(string? Name, string? Description);
record AddMemberRequest(string Email, string Role);
record UpdateMemberRoleRequest(string Role);
record GrantPermissionRequest(string? UserId, string? TeamId, string Permission)
{
    public long? GetUserId() => long.TryParse(UserId, out var id) ? id : null;
    public long? GetTeamId() => long.TryParse(TeamId, out var id) ? id : null;
}
record ShareDocumentRequest(string Permission, DateTime? ExpiresAt);
