# CWFlgma - 基于 .NET Aspire 的协作设计工具

## 项目概述

CWFlgma 是一个类似 Figma 的实时协作设计工具，采用 .NET Aspire 微服务架构构建。支持多人实时编辑、版本历史、权限管理等功能。

## 技术栈

- **后端框架**: .NET Aspire + ASP.NET Core Web API
- **实时通信**: SignalR
- **关系型数据库**: PostgreSQL（用户、文档元数据、权限）
- **文档数据库**: MongoDB（设计数据、图层结构）
- **缓存**: Redis（会话状态、协作状态）
- **本地存储**: 文件系统（图片、字体等资源）
- **前端**: React/Vue + Canvas/SVG（待开发）

## 项目结构

```
CWFlgma/
├── src/
│   ├── CWFlgma.AppHost/              # Aspire AppHost（编排入口）
│   ├── CWFlgma.ServiceDefaults/      # 共享服务配置
│   ├── CWFlgma.Seeder/               # 种子数据
│   ├── Services/
│   │   ├── CWFlgma.UserService/      # 用户服务
│   │   ├── CWFlgma.DocumentService/  # 文档服务
│   │   ├── CWFlgma.CollaborationService/  # 协作服务（SignalR）
│   │   ├── CWFlgma.ResourceService/  # 资源服务（图片/字体）
│   │   └── CWFlgma.Gateway/          # API 网关
│   └── Shared/
│       ├── CWFlgma.Contracts/        # 共享契约（DTO、接口）
│       └── CWFlgma.Infrastructure/   # 基础设施（数据库、存储）
├── tests/                            # 测试项目
├── docs/                             # 文档
│   ├── database-design.md           # 数据库设计文档
│   ├── api-design.md                # API 设计文档
│   ├── architecture.md              # 架构设计文档
│   ├── authentication.md            # 认证授权文档
│   └── seeder.md                    # 种子数据文档
└── docker-compose.yml               # 本地开发环境
```

## 快速开始

### 前置要求

- .NET 9.0 SDK
- Docker Desktop（用于 PostgreSQL、MongoDB、Redis）
- Visual Studio 2022 或 VS Code

### 启动依赖服务

```bash
docker-compose up -d
```

### 运行项目

```bash
cd src/CWFlgma.AppHost
dotnet run
```

### 初始化种子数据

```bash
cd src/CWFlgma.Seeder/CWFlgma.Seeder
dotnet run
```

默认管理员账号：
- 邮箱: admin@cwflgma.com
- 密码: Admin@123456

详细说明请参考 [seeder.md](docs/seeder.md)

## 数据库设计

### PostgreSQL（关系型数据）

存储用户、团队、文档元数据、权限等结构化数据。

### MongoDB（文档数据）

存储设计文档的图层结构、属性、操作历史等半结构化数据。

详细设计请参考 [database-design.md](docs/database-design.md)

## 核心功能

- [x] 用户注册与登录（JWT 认证）
- [x] 团队管理
- [x] 文档 CRUD
- [x] **实时协作编辑**（SignalR）
  - 光标同步
  - 选中状态同步
  - 编辑操作同步
  - 图层锁定
- [x] 版本历史
- [x] 权限控制
- [x] 资源管理（图片/字体）

## 认证授权

项目使用 JWT 进行用户认证和授权：

- **登录/注册**: `/api/auth/login`, `/api/auth/register`
- **刷新令牌**: `/api/auth/refresh`
- **受保护端点**: 使用 `Authorization: Bearer <token>` 头

详细文档请参考 [authentication.md](docs/authentication.md)

## 开发进度

- [x] 阶段 1: 基础框架搭建
- [x] 阶段 2: 用户服务与文档服务
- [x] 阶段 3: 认证授权（JWT）
- [ ] 阶段 4: 实时协作（SignalR）
- [ ] 阶段 5: 版本历史与权限
- [ ] 阶段 6: 性能优化与部署
