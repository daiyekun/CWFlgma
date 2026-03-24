using System;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace CWFlgma.Infrastructure.Authentication;

public static class ClaimsPrincipalExtensions
{
    public static long? GetUserId(this ClaimsPrincipal principal)
    {
        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return long.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    public static string? GetUsername(this ClaimsPrincipal principal)
    {
        return principal.FindFirst(ClaimTypes.Name)?.Value;
    }

    public static string? GetEmail(this ClaimsPrincipal principal)
    {
        return principal.FindFirst(ClaimTypes.Email)?.Value;
    }

    public static string? GetDisplayName(this ClaimsPrincipal principal)
    {
        return principal.FindFirst("displayName")?.Value;
    }

    public static bool IsInRole(this ClaimsPrincipal principal, string role)
    {
        return principal.IsInRole(role);
    }

    public static bool HasPermission(this ClaimsPrincipal principal, string permission)
    {
        return principal.HasClaim($"Permission", permission);
    }
}

public static class HttpContextExtensions
{
    public static long? GetCurrentUserId(this HttpContext context)
    {
        return context.User.GetUserId();
    }

    public static string? GetCurrentUsername(this HttpContext context)
    {
        return context.User.GetUsername();
    }
}
