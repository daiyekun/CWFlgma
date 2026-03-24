# 数据库设计文档

## 1. PostgreSQL 数据库设计

### 1.1 用户表 (users)

```sql
CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    username VARCHAR(50) UNIQUE NOT NULL,
    email VARCHAR(255) UNIQUE NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    display_name VARCHAR(100),
    avatar_url VARCHAR(500),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    last_login_at TIMESTAMP WITH TIME ZONE,
    is_active BOOLEAN DEFAULT TRUE
);

CREATE INDEX idx_users_email ON users(email);
CREATE INDEX idx_users_username ON users(username);
```

### 1.2 团队表 (teams)

```sql
CREATE TABLE teams (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(100) NOT NULL,
    description TEXT,
    owner_id UUID NOT NULL REFERENCES users(id),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);
```

### 1.3 团队成员表 (team_members)

```sql
CREATE TABLE team_members (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    team_id UUID NOT NULL REFERENCES teams(id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    role VARCHAR(20) NOT NULL DEFAULT 'member', -- owner, admin, member, viewer
    joined_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(team_id, user_id)
);

CREATE INDEX idx_team_members_team ON team_members(team_id);
CREATE INDEX idx_team_members_user ON team_members(user_id);
```

### 1.4 文档表 (documents)

```sql
CREATE TABLE documents (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    title VARCHAR(255) NOT NULL,
    description TEXT,
    owner_id UUID NOT NULL REFERENCES users(id),
    team_id UUID REFERENCES teams(id),
    parent_id UUID REFERENCES documents(id), -- 文件夹支持
    type VARCHAR(20) NOT NULL DEFAULT 'design', -- design, folder, component
    thumbnail_url VARCHAR(500),
    width INTEGER DEFAULT 1920,
    height INTEGER DEFAULT 1080,
    background_color VARCHAR(7) DEFAULT '#FFFFFF',
    is_public BOOLEAN DEFAULT FALSE,
    is_archived BOOLEAN DEFAULT FALSE,
    version INTEGER DEFAULT 1,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_documents_owner ON documents(owner_id);
CREATE INDEX idx_documents_team ON documents(team_id);
CREATE INDEX idx_documents_parent ON documents(parent_id);
CREATE INDEX idx_documents_type ON documents(type);
```

### 1.5 文档权限表 (document_permissions)

```sql
CREATE TABLE document_permissions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    document_id UUID NOT NULL REFERENCES documents(id) ON DELETE CASCADE,
    user_id UUID REFERENCES users(id) ON DELETE CASCADE,
    team_id UUID REFERENCES teams(id) ON DELETE CASCADE,
    permission VARCHAR(20) NOT NULL DEFAULT 'view', -- view, edit, admin
    granted_by UUID NOT NULL REFERENCES users(id),
    granted_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    CHECK (user_id IS NOT NULL OR team_id IS NOT NULL)
);

CREATE INDEX idx_doc_permissions_document ON document_permissions(document_id);
CREATE INDEX idx_doc_permissions_user ON document_permissions(user_id);
```

### 1.6 文档版本表 (document_versions)

```sql
CREATE TABLE document_versions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    document_id UUID NOT NULL REFERENCES documents(id) ON DELETE CASCADE,
    version_number INTEGER NOT NULL,
    title VARCHAR(255),
    created_by UUID NOT NULL REFERENCES users(id),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    comment TEXT,
    snapshot_url VARCHAR(500), -- MongoDB 中的快照引用
    UNIQUE(document_id, version_number)
);

CREATE INDEX idx_doc_versions_document ON document_versions(document_id);
```

### 1.7 操作日志表 (operation_logs)

```sql
CREATE TABLE operation_logs (
    id BIGSERIAL PRIMARY KEY,
    document_id UUID NOT NULL REFERENCES documents(id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES users(id),
    operation_type VARCHAR(50) NOT NULL, -- add_layer, update_layer, delete_layer, move_layer
    layer_id VARCHAR(100),
    operation_data JSONB NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_op_logs_document ON operation_logs(document_id);
CREATE INDEX idx_op_logs_user ON operation_logs(user_id);
CREATE INDEX idx_op_logs_created ON operation_logs(created_at);
```

---

## 2. MongoDB 数据库设计

### 2.1 设计数据集合 (designs)

存储文档的完整设计数据，包括所有图层和属性。

```javascript
// 设计数据文档结构
{
  "_id": ObjectId("..."),
  "documentId": "uuid-string",  // 关联 PostgreSQL 的 document ID
  "version": 1,
  
  // 页面/画板
  "pages": [
    {
      "id": "page-1",
      "name": "Page 1",
      "backgroundColor": "#FFFFFF",
      
      // 图层树（嵌套结构）
      "layers": [
        {
          "id": "layer-1",
          "type": "frame",  // frame, rectangle, ellipse, text, image, group, component
          "name": "Frame 1",
          
          // 位置和变换
          "transform": {
            "x": 100,
            "y": 100,
            "width": 400,
            "height": 300,
            "rotation": 0,
            "scaleX": 1,
            "scaleY": 1
          },
          
          // 样式属性
          "style": {
            "fill": {
              "type": "solid",  // solid, gradient, image
              "color": "#FF5733",
              "opacity": 1
            },
            "stroke": {
              "color": "#000000",
              "width": 2,
              "align": "center"  // center, inside, outside
            },
            "borderRadius": {
              "topLeft": 8,
              "topRight": 8,
              "bottomLeft": 8,
              "bottomRight": 8
            },
            "shadow": [
              {
                "x": 0,
                "y": 4,
                "blur": 8,
                "spread": 0,
                "color": "rgba(0,0,0,0.25)"
              }
            ],
            "blur": {
              "type": "gaussian",  // gaussian, background
              "radius": 10
            }
          },
          
          // 文本特有属性
          "text": {
            "content": "Hello World",
            "fontFamily": "Roboto",
            "fontSize": 16,
            "fontWeight": 400,
            "fontStyle": "normal",
            "textAlign": "left",
            "lineHeight": 1.5,
            "letterSpacing": 0,
            "textDecoration": "none"
          },
          
          // 图片特有属性
          "image": {
            "resourceId": "resource-uuid",
            "fit": "cover",  // cover, contain, fill, fit
            "position": "center"
          },
          
          // 布局属性（Auto Layout）
          "layout": {
            "mode": "none",  // none, horizontal, vertical
            "gap": 10,
            "padding": {
              "top": 0,
              "right": 0,
              "bottom": 0,
              "left": 0
            },
            "alignment": "start"  // start, center, end
          },
          
          // 约束
          "constraints": {
            "horizontal": "left",  // left, center, right, leftRight, scale
            "vertical": "top"      // top, center, bottom, topBottom, scale
          },
          
          // 可见性和锁定
          "visible": true,
          "locked": false,
          "opacity": 1,
          
          // 子图层（用于 group, frame, component）
          "children": []
        }
      ]
    }
  ],
  
  // 组件和样式库
  "components": [
    {
      "id": "comp-1",
      "name": "Button",
      "description": "Primary button component",
      "mainComponentId": "layer-xxx",
      "variants": []
    }
  ],
  
  "styles": [
    {
      "id": "style-1",
      "type": "fill",  // fill, text, effect, grid
      "name": "Primary Color",
      "value": {}
    }
  ],
  
  "createdAt": ISODate("2024-01-01T00:00:00Z"),
  "updatedAt": ISODate("2024-01-01T00:00:00Z")
}
```

### 2.2 操作历史集合 (operation_history)

存储增量操作，用于实时同步和历史回放。

```javascript
{
  "_id": ObjectId("..."),
  "documentId": "uuid-string",
  "userId": "user-uuid",
  "timestamp": ISODate("2024-01-01T00:00:00Z"),
  
  // 操作类型
  "operation": {
    "type": "update_layer",  // add_layer, update_layer, delete_layer, move_layer, reorder_layer
    
    // 目标图层
    "layerId": "layer-1",
    
    // 变更数据（差量更新）
    "changes": {
      "transform.x": 150,
      "transform.y": 200,
      "style.fill.color": "#00FF00"
    },
    
    // 旧值（用于撤销）
    "previousValues": {
      "transform.x": 100,
      "transform.y": 100,
      "style.fill.color": "#FF5733"
    }
  },
  
  // 操作序列号（用于 OT）
  "sequenceNumber": 12345,
  
  // 会话 ID
  "sessionId": "session-uuid"
}
```

### 2.3 协作会话集合 (collaboration_sessions)

```javascript
{
  "_id": ObjectId("..."),
  "documentId": "uuid-string",
  "sessionId": "session-uuid",
  "userId": "user-uuid",
  "userName": "John Doe",
  "userAvatar": "https://...",
  
  // 当前状态
  "cursor": {
    "x": 500,
    "y": 300
  },
  
  "selection": {
    "layerIds": ["layer-1", "layer-2"],
    "pageId": "page-1"
  },
  
  "viewport": {
    "x": 0,
    "y": 0,
    "zoom": 1
  },
  
  "lastActivity": ISODate("2024-01-01T00:00:00Z"),
  "connectedAt": ISODate("2024-01-01T00:00:00Z")
}
```

---

## 3. 本地文件存储结构

```
storage/
├── images/
│   ├── {document-id}/
│   │   ├── {resource-id}.png
│   │   ├── {resource-id}.jpg
│   │   └── {resource-id}.svg
│   └── thumbnails/
│       └── {document-id}.png
├── fonts/
│   ├── Roboto/
│   │   ├── Roboto-Regular.ttf
│   │   ├── Roboto-Bold.ttf
│   │   └── Roboto-Italic.ttf
│   └── OpenSans/
│       └── ...
└── exports/
    └── {document-id}/
        ├── export-20240101.png
        └── export-20240101.pdf
```

### 资源表 (resources) - PostgreSQL

```sql
CREATE TABLE resources (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    document_id UUID REFERENCES documents(id) ON DELETE CASCADE,
    type VARCHAR(20) NOT NULL, -- image, font, component
    name VARCHAR(255) NOT NULL,
    file_path VARCHAR(500) NOT NULL,
    mime_type VARCHAR(100),
    file_size BIGINT,
    width INTEGER,
    height INTEGER,
    uploaded_by UUID NOT NULL REFERENCES users(id),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_resources_document ON resources(document_id);
CREATE INDEX idx_resources_type ON resources(type);
```

---

## 4. 索引策略

### PostgreSQL 索引

- 所有外键字段建立索引
- 频繁查询字段（email, username）建立唯一索引
- 时间字段用于范围查询时建立索引
- JSONB 字段使用 GIN 索引

### MongoDB 索引

```javascript
// designs 集合
db.designs.createIndex({ "documentId": 1 }, { unique: true })
db.designs.createIndex({ "pages.layers.id": 1 })

// operation_history 集合
db.operation_history.createIndex({ "documentId": 1, "timestamp": -1 })
db.operation_history.createIndex({ "documentId": 1, "sequenceNumber": 1 })

// collaboration_sessions 集合
db.collaboration_sessions.createIndex({ "documentId": 1 })
db.collaboration_sessions.createIndex({ "userId": 1 })
db.collaboration_sessions.createIndex({ "lastActivity": 1 }, { expireAfterSeconds: 3600 })
```
