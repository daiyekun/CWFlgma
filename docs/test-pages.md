# CWFlgma 测试页面文档

## 概述

本项目包含多个测试页面，用于验证各个功能模块的 API 接口。

## 测试页面列表

| 页面 | 服务 | 默认地址 | 说明 |
|------|------|----------|------|
| `permission-test.html` | UserService | https://localhost:7108/permission-test.html | 权限管理测试 |
| `version-test.html` | DocumentService | https://localhost:7184/version-test.html | 版本历史测试 |

---

## 1. 权限管理测试页面 (permission-test.html)

**位置**: `src/Services/CWFlgma.UserService/wwwroot/permission-test.html`

**访问地址**: https://localhost:7108/permission-test.html

### 功能说明

#### 1.1 API 端口配置
- 配置 UserService 和 DocumentService 的 HTTPS 端口
- 默认端口: UserService=7108, DocumentService=7184

#### 1.2 用户登录
- 支持管理员账号登录: `admin@cwflgma.com` / `Admin@123456`
- 支持演示账号登录: `demo@cwflgma.com` / `Demo@123456`
- 快速切换按钮: "登录为 Demo"

#### 1.3 团队管理
- **创建团队**: 输入团队名称和描述
- **获取我的团队**: 列出当前用户所属的所有团队
- **查看团队详情**: 根据团队ID查看详细信息
- **删除团队**: 删除指定团队（仅所有者可操作）

#### 1.4 团队成员管理
- **获取成员列表**: 查看指定团队的所有成员
- **添加成员**: 通过邮箱添加新成员，支持设置角色（成员/管理员）

#### 1.5 文档权限管理
- **获取权限列表**: 查看指定文档的所有权限记录
- **授予权限**: 为用户或团队授予文档权限（查看/编辑/管理）

### API 接口测试

| 功能 | 方法 | 接口 |
|------|------|------|
| 登录 | POST | `/api/auth/login` |
| 注册 | POST | `/api/auth/register` |
| 获取团队列表 | GET | `/api/teams` |
| 创建团队 | POST | `/api/teams` |
| 获取团队详情 | GET | `/api/teams/{id}` |
| 删除团队 | DELETE | `/api/teams/{id}` |
| 获取成员列表 | GET | `/api/teams/{id}/members` |
| 添加成员 | POST | `/api/teams/{id}/members` |
| 获取文档权限 | GET | `/api/documents/{id}/permissions` |
| 授予权限 | POST | `/api/documents/{id}/permissions` |

---

## 2. 版本历史测试页面 (version-test.html)

**位置**: `src/Services/CWFlgma.DocumentService/wwwroot/version-test.html`

**访问地址**: https://localhost:7184/version-test.html

### 功能说明

#### 2.1 API 端口配置
- 配置 UserService 和 DocumentService 的 HTTPS 端口
- 登录需要调用 UserService 的认证接口

#### 2.2 用户登录
- 使用 UserService 的登录接口获取 Token
- Token 用于后续的 DocumentService API 调用

#### 2.3 文档选择
- 输入文档ID获取文档信息
- 显示文档标题、类型、版本号、所有者等信息

#### 2.4 版本管理
- **获取版本列表**: 显示文档的所有版本记录
- **创建新版本**:
  - 版本标题（可选）
  - 版本备注（可选）
  - 快照URL（可选）

#### 2.5 版本操作
- **查看详情**: 查看指定版本的详细信息
- **回滚版本**: 将文档恢复到指定版本（自动备份当前状态）
- **版本对比**: 对比两个版本的差异

### API 接口测试

| 功能 | 方法 | 接口 |
|------|------|------|
| 获取版本列表 | GET | `/api/documents/{id}/versions` |
| 创建版本 | POST | `/api/documents/{id}/versions` |
| 获取版本详情 | GET | `/api/documents/{id}/versions/{versionId}` |
| 回滚版本 | POST | `/api/documents/{id}/versions/{versionId}/restore` |
| 版本对比 | GET | `/api/documents/{id}/versions/compare?v1={id1}&v2={id2}` |

---

## 使用说明

### 前置条件

1. 启动 Docker 依赖服务:
   ```bash
   docker-compose up -d
   ```

2. 启动 Aspire AppHost:
   ```bash
   cd src/CWFlgma.AppHost
   dotnet run
   ```

3. 初始化种子数据:
   ```bash
   cd src/CWFlgma.Seeder/CWFlgma.Seeder
   dotnet run
   ```

### 默认账号

| 角色 | 邮箱 | 密码 |
|------|------|------|
| 管理员 | admin@cwflgma.com | Admin@123456 |
| 演示用户 | demo@cwflgma.com | Demo@123456 |

### 注意事项

1. **大整数精度问题**: JavaScript 无法精确处理超过 2^53-1 的整数，所有 ID 应作为字符串传递
2. **CORS 配置**: 跨域请求需要服务端配置 CORS 策略
3. **端口配置**: 每次重启 Aspire 后，端口可能变化，需从 Aspire Dashboard 获取

---

## 测试数据

### 示例文档 ID
- `691922341136674816` - My Designs 文件夹

### 示例用户 ID
- `691922339794497536` - 管理员用户

---

## 故障排除

### 1. 登录失败: Failed to fetch
- 检查 UserService 是否正常运行
- 检查端口配置是否正确
- 确认 CORS 配置已启用

### 2. 数据库连接失败
- 检查 Docker 容器是否正常运行: `docker ps`
- 检查 Aspire Dashboard 中的连接字符串

### 3. 权限不足
- 确认已登录并获取有效的 Token
- 检查用户是否有对应资源的访问权限

### 4. 大整数精度丢失
- 所有 ID 字段使用字符串类型传递
- 避免使用 `parseInt()` 转换 ID
