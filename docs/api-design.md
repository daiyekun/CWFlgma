# API 设计文档

## 1. API 网关

所有 API 请求通过 Gateway 统一入口，路由到对应的微服务。

**基础路径**: `https://localhost:5000/api/v1`

---

## 2. 用户服务 API

### 2.1 认证相关

#### POST /auth/register
注册新用户

**Request:**
```json
{
  "username": "johndoe",
  "email": "john@example.com",
  "password": "securePassword123",
  "displayName": "John Doe"
}
```

**Response: 201**
```json
{
  "id": "uuid",
  "username": "johndoe",
  "email": "john@example.com",
  "displayName": "John Doe",
  "createdAt": "2024-01-01T00:00:00Z"
}
```

#### POST /auth/login
用户登录

**Request:**
```json
{
  "email": "john@example.com",
  "password": "securePassword123"
}
```

**Response: 200**
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "refresh-token-string",
  "expiresIn": 3600,
  "user": {
    "id": "uuid",
    "username": "johndoe",
    "email": "john@example.com",
    "displayName": "John Doe",
    "avatarUrl": "https://..."
  }
}
```

#### POST /auth/refresh
刷新访问令牌

**Request:**
```json
{
  "refreshToken": "refresh-token-string"
}
```

### 2.2 用户相关

#### GET /users/me
获取当前用户信息

**Headers:** `Authorization: Bearer {token}`

**Response: 200**
```json
{
  "id": "uuid",
  "username": "johndoe",
  "email": "john@example.com",
  "displayName": "John Doe",
  "avatarUrl": "https://...",
  "createdAt": "2024-01-01T00:00:00Z"
}
```

#### PUT /users/me
更新当前用户信息

**Request:**
```json
{
  "displayName": "John Updated",
  "avatarUrl": "https://..."
}
```

#### GET /users/:id
获取指定用户信息（公开信息）

---

## 3. 团队服务 API

### 3.1 团队管理

#### POST /teams
创建团队

**Request:**
```json
{
  "name": "Design Team",
  "description": "Our design team workspace"
}
```

**Response: 201**
```json
{
  "id": "uuid",
  "name": "Design Team",
  "description": "Our design team workspace",
  "ownerId": "user-uuid",
  "memberCount": 1,
  "createdAt": "2024-01-01T00:00:00Z"
}
```

#### GET /teams
获取用户的团队列表

**Query Parameters:**
- `page`: 页码（默认 1）
- `pageSize`: 每页数量（默认 20）

**Response: 200**
```json
{
  "items": [
    {
      "id": "uuid",
      "name": "Design Team",
      "memberCount": 5,
      "documentCount": 12,
      "role": "owner"
    }
  ],
  "total": 10,
  "page": 1,
  "pageSize": 20
}
```

#### GET /teams/:id
获取团队详情

#### PUT /teams/:id
更新团队信息

#### DELETE /teams/:id
删除团队（仅所有者）

### 3.2 团队成员

#### POST /teams/:id/members
添加团队成员

**Request:**
```json
{
  "email": "newmember@example.com",
  "role": "member"  // owner, admin, member, viewer
}
```

#### GET /teams/:id/members
获取团队成员列表

#### PUT /teams/:id/members/:userId
更新成员角色

#### DELETE /teams/:id/members/:userId
移除团队成员

---

## 4. 文档服务 API

### 4.1 文档管理

#### POST /documents
创建文档

**Request:**
```json
{
  "title": "New Design",
  "description": "A new design project",
  "teamId": "team-uuid",  // 可选，关联团队
  "parentId": "folder-uuid",  // 可选，父文件夹
  "type": "design",  // design, folder
  "width": 1920,
  "height": 1080,
  "backgroundColor": "#FFFFFF"
}
```

**Response: 201**
```json
{
  "id": "uuid",
  "title": "New Design",
  "type": "design",
  "ownerId": "user-uuid",
  "teamId": "team-uuid",
  "thumbnailUrl": null,
  "width": 1920,
  "height": 1080,
  "version": 1,
  "createdAt": "2024-01-01T00:00:00Z",
  "updatedAt": "2024-01-01T00:00:00Z"
}
```

#### GET /documents
获取文档列表

**Query Parameters:**
- `teamId`: 团队 ID（筛选）
- `parentId`: 父文件夹 ID（筛选）
- `type`: 类型（design, folder）
- `search`: 搜索关键词
- `page`: 页码
- `pageSize`: 每页数量
- `sortBy`: 排序字段（title, updatedAt, createdAt）
- `sortOrder`: 排序方向（asc, desc）

**Response: 200**
```json
{
  "items": [
    {
      "id": "uuid",
      "title": "Design 1",
      "type": "design",
      "thumbnailUrl": "https://...",
      "ownerId": "user-uuid",
      "ownerName": "John Doe",
      "updatedAt": "2024-01-01T00:00:00Z",
      "permission": "edit"
    }
  ],
  "total": 50,
  "page": 1,
  "pageSize": 20
}
```

#### GET /documents/:id
获取文档详情

**Response: 200**
```json
{
  "id": "uuid",
  "title": "Design 1",
  "description": "Description",
  "type": "design",
  "ownerId": "user-uuid",
  "teamId": "team-uuid",
  "thumbnailUrl": "https://...",
  "width": 1920,
  "height": 1080,
  "backgroundColor": "#FFFFFF",
  "isPublic": false,
  "version": 5,
  "createdAt": "2024-01-01T00:00:00Z",
  "updatedAt": "2024-01-02T00:00:00Z",
  "permission": "admin"
}
```

#### PUT /documents/:id
更新文档元数据

**Request:**
```json
{
  "title": "Updated Title",
  "description": "Updated description",
  "width": 1920,
  "height": 1080,
  "backgroundColor": "#F0F0F0",
  "isPublic": true
}
```

#### DELETE /documents/:id
删除文档（软删除）

#### POST /documents/:id/archive
归档文档

#### POST /documents/:id/restore
恢复已删除文档

### 4.2 文档设计数据

#### GET /documents/:id/design
获取文档完整设计数据（从 MongoDB）

**Response: 200**
```json
{
  "documentId": "uuid",
  "version": 5,
  "pages": [
    {
      "id": "page-1",
      "name": "Page 1",
      "layers": [...]
    }
  ],
  "components": [...],
  "styles": [...]
}
```

#### PUT /documents/:id/design
更新文档设计数据

**Request:**
```json
{
  "pages": [...],
  "components": [...],
  "styles": [...]
}
```

### 4.3 版本历史

#### GET /documents/:id/versions
获取文档版本列表

**Query Parameters:**
- `page`: 页码
- `pageSize`: 每页数量

**Response: 200**
```json
{
  "items": [
    {
      "id": "version-uuid",
      "versionNumber": 5,
      "title": "Version 5",
      "createdBy": "user-uuid",
      "createdByName": "John Doe",
      "createdAt": "2024-01-02T00:00:00Z",
      "comment": "Added new components",
      "snapshotUrl": "https://..."
    }
  ],
  "total": 10
}
```

#### POST /documents/:id/versions
创建新版本（快照）

**Request:**
```json
{
  "title": "Version 5",
  "comment": "Added new components"
}
```

#### POST /documents/:id/versions/:versionId/restore
恢复到指定版本

### 4.4 文档权限

#### GET /documents/:id/permissions
获取文档权限列表

#### POST /documents/:id/permissions
添加权限

**Request:**
```json
{
  "userId": "user-uuid",  // 或 teamId
  "permission": "edit"    // view, edit, admin
}
```

#### PUT /documents/:id/permissions/:permissionId
更新权限

#### DELETE /documents/:id/permissions/:permissionId
删除权限

#### POST /documents/:id/share
生成分享链接

**Request:**
```json
{
  "permission": "view",
  "expiresAt": "2024-12-31T23:59:59Z"  // 可选
}
```

**Response: 200**
```json
{
  "shareUrl": "https://app.example.com/share/abc123",
  "expiresAt": "2024-12-31T23:59:59Z"
}
```

---

## 5. 协作服务 API (SignalR)

### 5.1 Hub 端点

**Hub URL**: `wss://localhost:5000/hubs/collaboration`

### 5.2 客户端方法（发送到服务器）

#### JoinDocument(documentId, token)
加入文档协作会话

#### LeaveDocument(documentId)
离开文档协作会话

#### UpdateCursor(documentId, x, y)
更新光标位置

#### UpdateSelection(documentId, layerIds)
更新选中图层

#### UpdateViewport(documentId, x, y, zoom)
更新视口位置

#### SendOperation(documentId, operation)
发送编辑操作

**Operation 结构:**
```json
{
  "type": "update_layer",
  "layerId": "layer-1",
  "changes": {
    "transform.x": 150
  },
  "sequenceNumber": 12345
}
```

#### RequestLock(documentId, layerId)
请求图层锁定

#### ReleaseLock(documentId, layerId)
释放图层锁定

### 5.3 服务器方法（发送到客户端）

#### UserJoined(user)
有用户加入

**User 结构:**
```json
{
  "userId": "uuid",
  "userName": "John Doe",
  "avatarUrl": "https://...",
  "cursor": { "x": 0, "y": 0 },
  "selection": { "layerIds": [] },
  "sessionId": "session-uuid"
}
```

#### UserLeft(sessionId)
有用户离开

#### CursorUpdated(sessionId, x, y)
光标更新

#### SelectionUpdated(sessionId, layerIds)
选中更新

#### OperationReceived(operation)
收到编辑操作

#### LockAcquired(layerId, sessionId)
图层被锁定

#### LockReleased(layerId)
图层锁定释放

#### DocumentUpdated(document)
文档元数据更新

#### Error(message)
错误消息

---

## 6. 资源服务 API

### 6.1 图片资源

#### POST /resources/images
上传图片

**Request:** `multipart/form-data`
- `file`: 图片文件
- `documentId`: 文档 ID

**Response: 201**
```json
{
  "id": "resource-uuid",
  "name": "image.png",
  "type": "image",
  "url": "/api/v1/resources/images/resource-uuid",
  "mimeType": "image/png",
  "fileSize": 102400,
  "width": 800,
  "height": 600,
  "createdAt": "2024-01-01T00:00:00Z"
}
```

#### GET /resources/images/:id
获取图片（返回图片流）

#### GET /resources/images/:id/thumbnail
获取缩略图

#### DELETE /resources/images/:id
删除图片

### 6.2 字体资源

#### GET /resources/fonts
获取可用字体列表

**Response: 200**
```json
{
  "fonts": [
    {
      "id": "font-uuid",
      "name": "Roboto",
      "family": "Roboto",
      "variants": [
        { "weight": 400, "style": "normal", "url": "/resources/fonts/roboto-regular" },
        { "weight": 700, "style": "normal", "url": "/resources/fonts/roboto-bold" }
      ]
    }
  ]
}
```

#### GET /resources/fonts/:id/:variant
获取字体文件

#### POST /resources/fonts
上传自定义字体

---

## 7. 导出服务 API

#### POST /documents/:id/export
导出文档

**Request:**
```json
{
  "format": "png",  // png, jpg, svg, pdf
  "scale": 2,       // 导出倍率
  "pageIds": ["page-1"],  // 导出页面（可选，默认全部）
  "background": true      // 是否包含背景
}
```

**Response: 202**
```json
{
  "exportId": "export-uuid",
  "status": "processing",
  "estimatedTime": 5
}
```

#### GET /exports/:id
获取导出状态

**Response: 200**
```json
{
  "id": "export-uuid",
  "status": "completed",
  "downloadUrl": "/exports/download/export-uuid",
  "expiresAt": "2024-01-01T01:00:00Z"
}
```

#### GET /exports/:id/download
下载导出文件
