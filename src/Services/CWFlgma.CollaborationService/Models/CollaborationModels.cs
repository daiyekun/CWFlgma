using System;
using System.Collections.Concurrent;

namespace CWFlgma.CollaborationService.Models;

/// <summary>
/// 协作用户信息
/// </summary>
public class CollaborationUser
{
    public long UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public string SessionId { get; set; } = string.Empty;
    
    // 光标位置
    public CursorPosition? Cursor { get; set; }
    
    // 选中的图层
    public SelectionState? Selection { get; set; }
    
    // 视口位置
    public ViewportState? Viewport { get; set; }
    
    public DateTime ConnectedAt { get; set; }
    public DateTime LastActivity { get; set; }
}

/// <summary>
/// 光标位置
/// </summary>
public class CursorPosition
{
    public double X { get; set; }
    public double Y { get; set; }
}

/// <summary>
/// 选中状态
/// </summary>
public class SelectionState
{
    public string[] LayerIds { get; set; } = Array.Empty<string>();
    public string? PageId { get; set; }
}

/// <summary>
/// 视口状态
/// </summary>
public class ViewportState
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Zoom { get; set; } = 1;
}

/// <summary>
/// 编辑操作
/// </summary>
public class EditOperation
{
    public string Type { get; set; } = string.Empty;
    public string? LayerId { get; set; }
    public Dictionary<string, object>? Changes { get; set; }
    public Dictionary<string, object>? PreviousValues { get; set; }
    public long SequenceNumber { get; set; }
    public string SessionId { get; set; } = string.Empty;
}

/// <summary>
/// 锁定请求
/// </summary>
public class LockRequest
{
    public string DocumentId { get; set; } = string.Empty;
    public string LayerId { get; set; } = string.Empty;
}

/// <summary>
/// 文档协作状态
/// </summary>
public class DocumentCollaborationState
{
    public string DocumentId { get; set; } = string.Empty;
    public ConcurrentDictionary<string, CollaborationUser> Users { get; set; } = new();
    public ConcurrentDictionary<string, string> LayerLocks { get; set; } = new(); // LayerId -> SessionId
    public long LastSequenceNumber { get; set; }
}
