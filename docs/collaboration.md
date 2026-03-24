# 实时协作功能文档

## 概述

CWFlgma 使用 SignalR 实现实时协作功能，支持多人同时编辑设计文档。

## 协作功能

### 1. 用户加入/离开
- 用户加入文档时通知其他协作者
- 用户离开时自动释放锁定的图层

### 2. 光标同步
- 实时显示其他用户的光标位置
- 支持自定义光标样式

### 3. 选中状态同步
- 显示其他用户选中的图层
- 支持多选状态同步

### 4. 视口同步
- 可选：同步用户的缩放和位置

### 5. 编辑操作同步
- 支持图层添加、更新、删除、移动
- 使用序列号保证操作顺序

### 6. 图层锁定
- 编辑图层前请求锁定
- 防止多人同时编辑同一图层
- 用户离开时自动释放锁定

## API 端点

### SignalR Hub
**URL**: `/hubs/collaboration`

### 客户端方法（发送到服务器）

```javascript
// 加入文档协作
await connection.invoke("JoinDocument", documentId, userId, username, displayName, avatarUrl);

// 离开文档
await connection.invoke("LeaveDocument", documentId);

// 更新光标位置
await connection.invoke("UpdateCursor", documentId, x, y);

// 更新选中状态
await connection.invoke("UpdateSelection", documentId, layerIds, pageId);

// 更新视口
await connection.invoke("UpdateViewport", documentId, x, y, zoom);

// 发送编辑操作
await connection.invoke("SendOperation", documentId, {
    type: "update_layer",
    layerId: "layer-1",
    changes: { "transform.x": 100 }
});

// 请求锁定图层
await connection.invoke("RequestLock", documentId, layerId);

// 释放图层锁定
await connection.invoke("ReleaseLock", documentId, layerId);
```

### 服务器方法（接收从服务器）

```javascript
// 文档状态（加入时接收）
connection.on("DocumentState", (state) => { ... });

// 用户加入
connection.on("UserJoined", (user) => { ... });

// 用户离开
connection.on("UserLeft", (sessionId, userId, unlockedLayers) => { ... });

// 光标更新
connection.on("CursorUpdated", (sessionId, userId, x, y) => { ... });

// 选中状态更新
connection.on("SelectionUpdated", (sessionId, userId, layerIds, pageId) => { ... });

// 视口更新
connection.on("ViewportUpdated", (sessionId, userId, x, y, zoom) => { ... });

// 收到编辑操作
connection.on("OperationReceived", (operation) => { ... });

// 图层被锁定
connection.on("LayerLocked", (layerId, sessionId, userId) => { ... });

// 图层被解锁
connection.on("LayerUnlocked", (layerId) => { ... });

// 锁定失败
connection.on("LockFailed", (layerId, lockedBySession) => { ... });
```

## 前端集成示例

```javascript
import * as signalR from "@microsoft/signalr";

const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/collaboration")
    .withAutomaticReconnect()
    .build();

// 启动连接
await connection.start();

// 加入文档
await connection.invoke("JoinDocument", documentId, userId, username);

// 监听其他用户光标
connection.on("CursorUpdated", (sessionId, userId, x, y) => {
    updateRemoteCursor(userId, x, y);
});

// 监听编辑操作
connection.on("OperationReceived", (operation) => {
    applyOperation(operation);
});

// 发送操作前先锁定
const locked = await connection.invoke("RequestLock", documentId, layerId);
if (locked) {
    // 执行编辑
    await connection.invoke("SendOperation", documentId, operation);
    // 释放锁定
    await connection.invoke("ReleaseLock", documentId, layerId);
}
```
