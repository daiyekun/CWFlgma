# 种子数据使用说明

## 概述

CWFlgma.Seeder 是一个控制台应用，用于初始化数据库的默认数据。

## 运行种子数据

```bash
cd src/CWFlgma.Seeder/CWFlgma.Seeder
dotnet run
```

## 默认账号

### 管理员账号
- **邮箱**: admin@cwflgma.com
- **密码**: Admin@123456
- **用户名**: admin
- **角色**: 团队所有者

### 演示账号
- **邮箱**: demo@cwflgma.com
- **密码**: Demo@123456
- **用户名**: demo
- **角色**: 团队成员

## 种子数据内容

1. **用户**
   - 管理员用户
   - 演示用户

2. **团队**
   - Default Team（默认团队）

3. **文档**
   - My Designs（文件夹）
   - Welcome to CWFlgma（示例文档）

## 配置

在 `appsettings.json` 中可以修改默认配置：

```json
{
  "SeedData": {
    "AdminUser": {
      "Username": "admin",
      "Email": "admin@cwflgma.com",
      "Password": "Admin@123456",
      "DisplayName": "System Administrator"
    },
    "DemoUser": {
      "Username": "demo",
      "Email": "demo@cwflgma.com",
      "Password": "Demo@123456",
      "DisplayName": "Demo User"
    }
  }
}
```

## 注意事项

- 种子数据只会运行一次（如果数据库已有数据则跳过）
- 密码使用 BCrypt 加密存储
- 生产环境请修改默认密码
