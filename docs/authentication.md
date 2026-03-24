# 认证授权文档

## 概述

CWFlgma 使用 JWT (JSON Web Token) 进行用户认证和授权。

## API 端点

### 用户注册

```
POST /api/auth/register
Content-Type: application/json

{
  "username": "johndoe",
  "email": "john@example.com",
  "password": "securePassword123",
  "displayName": "John Doe"
}
```

**响应:**
```json
{
  "success": true,
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "base64-encoded-token",
  "expiresAt": "2024-01-01T01:00:00Z",
  "user": {
    "id": "guid",
    "username": "johndoe",
    "email": "john@example.com",
    "displayName": "John Doe"
  }
}
```

### 用户登录

```
POST /api/auth/login
Content-Type: application/json

{
  "email": "john@example.com",
  "password": "securePassword123"
}
```

**响应:** 同注册

### 刷新令牌

```
POST /api/auth/refresh
Content-Type: application/json

{
  "refreshToken": "base64-encoded-token"
}
```

## 使用认证

### 在请求头中添加 Token

```
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
```

### 在代码中使用

```csharp
// 需要认证的端点
app.MapGet("/api/protected", (HttpContext context) =>
{
    var userId = context.GetCurrentUserId();
    var username = context.GetCurrentUsername();
    return Results.Ok(new { userId, username });
})
.RequireAuthorization();

// 需要特定角色的端点
app.MapGet("/api/admin", () => Results.Ok())
.RequireAuthorization(policy => policy.RequireRole("Admin"));

// 使用自定义属性
[RequireAuth]
public class ProtectedController : ControllerBase
{
    [RequireRole("Admin", "Editor")]
    public IActionResult AdminOnly() => Ok();
}
```

## 配置

在 `appsettings.json` 中配置 JWT:

```json
{
  "Jwt": {
    "Secret": "YourSuperSecretKeyThatIsAtLeast32CharactersLong!",
    "Issuer": "CWFlgma",
    "Audience": "CWFlgma",
    "AccessTokenExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7
  }
}
```

## 安全建议

1. **生产环境**:
   - 使用更强的密钥（至少 64 字符）
   - 使用环境变量存储密钥
   - 启用 HTTPS
   - 设置合适的过期时间

2. **密码安全**:
   - 使用 BCrypt 哈希
   - 最小密码长度 8 字符
   - 要求包含大小写字母和数字

3. **Token 安全**:
   - 短期 Access Token（15-60 分钟）
   - 长期 Refresh Token（7-30 天）
   - 实现 Token 黑名单机制
