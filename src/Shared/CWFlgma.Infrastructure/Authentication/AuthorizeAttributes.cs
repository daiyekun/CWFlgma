using System;
using Microsoft.AspNetCore.Authorization;

namespace CWFlgma.Infrastructure.Authentication;

/// <summary>
/// 需要认证的属性
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequireAuthAttribute : AuthorizeAttribute
{
    public RequireAuthAttribute()
    {
        Policy = "RequireAuthentication";
    }
}

/// <summary>
/// 需要特定角色的属性
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequireRoleAttribute : AuthorizeAttribute
{
    public RequireRoleAttribute(params string[] roles)
    {
        Roles = string.Join(",", roles);
    }
}

/// <summary>
/// 需要特定权限的属性
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequirePermissionAttribute : AuthorizeAttribute
{
    public RequirePermissionAttribute(string permission)
    {
        Policy = $"Permission:{permission}";
    }
}
