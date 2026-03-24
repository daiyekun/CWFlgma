# 架构设计文档

## 1. 系统架构概览

```
┌─────────────────────────────────────────────────────────────────┐
│                         客户端 (Web/Mobile)                      │
│  ┌─────────┐  ┌─────────┐  ┌─────────┐  ┌─────────┐            │
│  │ React   │  │ Canvas  │  │ SignalR │  │  State  │            │
│  │  UI     │  │ Render  │  │ Client  │  │ Manager │            │
│  └────┬────┘  └────┬────┘  └────┬────┘  └────┬────┘            │
└───────┼────────────┼────────────┼────────────┼──────────────────┘
        │            │            │            │
        └────────────┴─────┬──────┴────────────┘
                           │
                    ┌──────▼──────┐
                    │   Gateway   │ (YARP)
                    │   :5000     │
                    └──────┬──────┘
                           │
        ┌──────────────────┼──────────────────┐
        │                  │                  │
┌───────▼──────┐  ┌───────▼──────┐  ┌───────▼──────┐
│ User Service │  │Document Svc  │  │Collaboration │
│    :5001     │  │    :5002     │  │   Service    │
└───────┬──────┘  └───────┬──────┘  │    :5003     │
        │                 │         └───────┬──────┘
        │                 │                 │
┌───────▼──────┐  ┌──────▼───────┐  ┌──────▼───────┐
│  PostgreSQL  │  │   MongoDB    │  │    Redis     │
│   :5432      │  │    :27017    │  │    :6379     │
└──────────────┘  └──────────────┘  └──────────────┘
                           │
                    ┌──────▼──────┐
                    │ Local File  │
                    │   Storage   │
                    └─────────────┘
```

## 2. 微服务架构

### 2.1 AppHost (编排层)

Aspire AppHost 负责：
- 服务发现与注册
- 配置管理
- 健康检查
- 遥测收集
- 本地开发环境编排

### 2.2 ServiceDefaults (共享配置)

共享服务配置：
- 日志配置 (OpenTelemetry)
- 健康检查端点
- 服务发现客户端
- 默认中间件

### 2.3 微服务

| 服务 | 端口 | 职责 | 数据库 |
|------|------|------|--------|
| UserService | 5001 | 用户认证、团队管理 | PostgreSQL |
| DocumentService | 5002 | 文档 CRUD、版本、权限 | PostgreSQL + MongoDB |
| CollaborationService | 5003 | 实时协作、操作同步 | Redis |
| ResourceService | 5004 | 图片/字体管理 | 本地文件系统 |
| Gateway | 5000 | API 网关、路由 | - |

## 3. 实时协作架构

### 3.1 协作流程

```
用户A编辑图层         用户B编辑图层
      │                    │
      ▼                    ▼
┌──────────┐         ┌──────────┐
│ SignalR  │         │ SignalR  │
│ Client A │         │ Client B │
└────┬─────┘         └────┬─────┘
     │                    │
     │  ┌─────────────────┘
     │  │
     ▼  ▼
┌─────────────────────────────────┐
│      Collaboration Hub          │
│  ┌─────────────────────────┐   │
│  │   Operation Handler     │   │
│  │  ┌───────────────────┐  │   │
│  │  │  OT/CRDT Engine   │  │   │
│  │  └───────────────────┘  │   │
│  └─────────────────────────┘   │
└───────────────┬─────────────────┘
                │
        ┌───────▼───────┐
        │     Redis     │
        │  - Session    │
        │  - Locks      │
        │  - Pub/Sub    │
        └───────────────┘
```

### 3.2 操作转换 (OT)

使用操作转换解决并发编辑冲突：

```csharp
// 操作类型
public enum OperationType
{
    AddLayer,
    UpdateLayer,
    DeleteLayer,
    MoveLayer,
    ReorderLayer
}

// 操作定义
public class Operation
{
    public OperationType Type { get; set; }
    public string LayerId { get; set; }
    public Dictionary<string, object> Changes { get; set; }
    public long SequenceNumber { get; set; }
    public string SessionId { get; set; }
}

// OT 转换函数
public static Operation Transform(Operation op1, Operation op2)
{
    // 如果操作不同图层，无需转换
    if (op1.LayerId != op2.LayerId)
        return op1;
    
    // 根据操作类型进行转换
    return (op1.Type, op2.Type) switch
    {
        (OperationType.UpdateLayer, OperationType.UpdateLayer) 
            => TransformUpdateUpdate(op1, op2),
        (OperationType.MoveLayer, OperationType.MoveLayer) 
            => TransformMoveMove(op1, op2),
        _ => op1
    };
}
```

### 3.3 状态同步策略

1. **乐观更新**: 客户端立即应用本地操作
2. **服务端确认**: 服务器验证并广播操作
3. **冲突解决**: 使用 OT 转换冲突操作
4. **状态修复**: 定期同步完整状态

## 4. 数据流

### 4.1 文档加载流程

```
客户端请求文档
       │
       ▼
┌──────────────┐
│   Gateway    │ ──── 路由到 DocumentService
└──────────────┘
       │
       ▼
┌──────────────┐
│  DocumentSvc │ ──── 从 PostgreSQL 获取元数据
└──────────────┘
       │
       ▼
┌──────────────┐
│   MongoDB    │ ──── 获取设计数据
└──────────────┘
       │
       ▼
┌──────────────┐
│   返回数据   │ ──── 组合并返回给客户端
└──────────────┘
```

### 4.2 实时编辑流程

```
用户编辑图层属性
       │
       ▼
┌──────────────┐
│ 客户端本地   │ ──── 立即应用更改（乐观更新）
│ 状态更新     │
└──────────────┘
       │
       ▼
┌──────────────┐
│ 发送操作到   │ ──── SendOperation(operation)
│ SignalR Hub  │
└──────────────┘
       │
       ▼
┌──────────────┐
│ 服务器验证   │ ──── 检查权限、锁定状态
│ OT 转换      │ ──── 处理并发冲突
└──────────────┘
       │
       ▼
┌──────────────┐
│ 广播操作     │ ──── 发送给所有协作用户
│ 保存到       │ ──── 存储到 MongoDB
│ MongoDB      │
└──────────────┘
       │
       ▼
┌──────────────┐
│ 其他客户端   │ ──── 接收并应用操作
│ 状态同步     │
└──────────────┘
```

## 5. 技术选型详解

### 5.1 .NET Aspire

优势：
- 内置服务发现
- 统一配置管理
- 开箱即用的遥测
- 简化的本地开发

### 5.2 SignalR

优势：
- WebSocket 支持
- 自动回退机制
- 强类型 Hub
- 组管理

### 5.3 PostgreSQL vs MongoDB

| 场景 | 数据库 | 原因 |
|------|--------|------|
| 用户/团队 | PostgreSQL | 结构化、关系明确、事务支持 |
| 文档元数据 | PostgreSQL | 查询频繁、需要事务 |
| 设计数据 | MongoDB | 灵活 schema、嵌套结构 |
| 操作历史 | MongoDB | 时间序列、高写入 |

### 5.4 Redis

用途：
- 协作会话管理
- 图层锁定
- 操作序列号
- Pub/Sub 消息

## 6. 安全设计

### 6.1 认证

- JWT Token 认证
- Refresh Token 机制
- Token 黑名单（Redis）

### 6.2 授权

- 基于角色的访问控制 (RBAC)
- 文档级权限
- 操作级权限验证

### 6.3 数据安全

- 密码 bcrypt 哈希
- 敏感数据加密存储
- SQL 注入防护（参数化查询）
- XSS 防护

## 7. 性能优化

### 7.1 数据库优化

- 连接池配置
- 读写分离（未来）
- 索引优化
- 查询优化

### 7.2 缓存策略

- Redis 缓存热数据
- 文档元数据缓存
- 用户会话缓存

### 7.3 前端优化

- 增量更新（差量同步）
- 虚拟滚动
- Canvas 分层渲染
- Web Worker 后台处理

## 8. 部署架构

### 8.1 本地开发

```yaml
# docker-compose.yml
services:
  postgres:
    image: postgres:16
    ports:
      - "5432:5432"
    environment:
      POSTGRES_DB: cwflgma
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres

  mongodb:
    image: mongo:7
    ports:
      - "27017:27017"

  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"
```

### 8.2 生产部署（未来）

- Kubernetes 部署
- 水平扩展协作服务
- 数据库主从复制
- CDN 加速资源
- 负载均衡

## 9. 监控与日志

### 9.1 遥测

- OpenTelemetry 集成
- 分布式追踪
- 性能指标
- 错误追踪

### 9.2 日志

- 结构化日志
- 日志聚合
- 告警配置
