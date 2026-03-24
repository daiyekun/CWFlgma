using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using CWFlgma.CollaborationService.Models;
using CWFlgma.Infrastructure.MongoDB;
using CWFlgma.Infrastructure.MongoDB.Documents;
using MongoDB.Driver;

namespace CWFlgma.CollaborationService.Hubs;

/// <summary>
/// 协作 Hub - 处理实时协作通信
/// </summary>
public class CollaborationHub : Hub
{
    private readonly ILogger<CollaborationHub> _logger;
    private readonly IServiceProvider _serviceProvider;
    
    // 文档协作状态（生产环境应使用 Redis）
    private static readonly ConcurrentDictionary<string, DocumentCollaborationState> _documentStates = new();
    
    // 用户会话映射
    private static readonly ConcurrentDictionary<string, (string DocumentId, long UserId)> _sessionMap = new();

    public CollaborationHub(ILogger<CollaborationHub> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// 加入文档协作
    /// </summary>
    public async Task JoinDocument(string documentId, long userId, string username, string? displayName, string? avatarUrl)
    {
        var sessionId = Context.ConnectionId;
        
        // 加入文档组
        await Groups.AddToGroupAsync(sessionId, documentId);
        
        // 获取或创建文档状态
        var docState = _documentStates.GetOrAdd(documentId, _ => new DocumentCollaborationState
        {
            DocumentId = documentId
        });
        
        // 添加用户
        var user = new CollaborationUser
        {
            UserId = userId,
            Username = username,
            DisplayName = displayName,
            AvatarUrl = avatarUrl,
            SessionId = sessionId,
            ConnectedAt = DateTime.UtcNow,
            LastActivity = DateTime.UtcNow
        };
        
        docState.Users[sessionId] = user;
        _sessionMap[sessionId] = (documentId, userId);
        
        // 通知其他用户
        await Clients.OthersInGroup(documentId).SendAsync("UserJoined", user);
        
        // 发送当前文档状态给新用户（包括在线用户和锁定状态）
        var onlineUsers = docState.Users.Values.ToArray();
        var lockedLayers = docState.LayerLocks.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        await Clients.Caller.SendAsync("DocumentState", new
        {
            documentId,
            users = onlineUsers,
            lockedLayers,
            lastSequenceNumber = docState.LastSequenceNumber
        });
        
        _logger.LogInformation("User {Username} joined document {DocumentId}", username, documentId);
    }

    /// <summary>
    /// 离开文档协作
    /// </summary>
    public async Task LeaveDocument(string documentId)
    {
        var sessionId = Context.ConnectionId;
        await RemoveUserFromDocument(sessionId, documentId);
    }

    /// <summary>
    /// 更新光标位置
    /// </summary>
    public async Task UpdateCursor(string documentId, double x, double y)
    {
        var sessionId = Context.ConnectionId;
        
        if (_documentStates.TryGetValue(documentId, out var docState) &&
            docState.Users.TryGetValue(sessionId, out var user))
        {
            user.Cursor = new Models.CursorPosition { X = x, Y = y };
            user.LastActivity = DateTime.UtcNow;
            
            await Clients.OthersInGroup(documentId).SendAsync("CursorUpdated", sessionId, user.UserId, x, y);
        }
    }

    /// <summary>
    /// 更新选中状态
    /// </summary>
    public async Task UpdateSelection(string documentId, string[] layerIds, string? pageId)
    {
        var sessionId = Context.ConnectionId;
        
        if (_documentStates.TryGetValue(documentId, out var docState) &&
            docState.Users.TryGetValue(sessionId, out var user))
        {
            user.Selection = new SelectionState
            {
                LayerIds = layerIds,
                PageId = pageId
            };
            user.LastActivity = DateTime.UtcNow;
            
            await Clients.OthersInGroup(documentId).SendAsync("SelectionUpdated", sessionId, user.UserId, layerIds, pageId);
        }
    }

    /// <summary>
    /// 更新视口
    /// </summary>
    public async Task UpdateViewport(string documentId, double x, double y, double zoom)
    {
        var sessionId = Context.ConnectionId;
        
        if (_documentStates.TryGetValue(documentId, out var docState) &&
            docState.Users.TryGetValue(sessionId, out var user))
        {
            user.Viewport = new ViewportState { X = x, Y = y, Zoom = zoom };
            user.LastActivity = DateTime.UtcNow;
            
            await Clients.OthersInGroup(documentId).SendAsync("ViewportUpdated", sessionId, user.UserId, x, y, zoom);
        }
    }

    /// <summary>
    /// 发送编辑操作
    /// </summary>
    public async Task SendOperation(string documentId, EditOperation operation)
    {
        var sessionId = Context.ConnectionId;
        
        if (_documentStates.TryGetValue(documentId, out var docState))
        {
            // 分配序列号
            operation.SequenceNumber = ++docState.LastSequenceNumber;
            operation.SessionId = sessionId;
            
            // 更新用户活动时间
            if (docState.Users.TryGetValue(sessionId, out var user))
            {
                user.LastActivity = DateTime.UtcNow;
            }
            
            // 保存操作到 MongoDB
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var mongoContext = scope.ServiceProvider.GetRequiredService<CWFlgmaMongoContext>();
                    
                    var history = new OperationHistory
                    {
                        DocumentId = documentId,
                        UserId = user?.UserId.ToString() ?? "unknown",
                        Timestamp = DateTime.UtcNow,
                        Operation = new Operation
                        {
                            Type = operation.Type,
                            LayerId = operation.LayerId,
                            Changes = operation.Changes,
                            PreviousValues = operation.PreviousValues
                        },
                        SequenceNumber = operation.SequenceNumber,
                        SessionId = sessionId
                    };
                    
                    await mongoContext.OperationHistory.InsertOneAsync(history);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to save operation to MongoDB");
                }
            });
            
            // 广播给其他用户
            await Clients.OthersInGroup(documentId).SendAsync("OperationReceived", operation);
            
            _logger.LogDebug("Operation {Type} on layer {LayerId} in document {DocumentId}", 
                operation.Type, operation.LayerId, documentId);
        }
    }

    /// <summary>
    /// 获取操作历史
    /// </summary>
    public async Task GetOperationHistory(string documentId, long afterSequence = 0, int limit = 100)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var mongoContext = scope.ServiceProvider.GetRequiredService<CWFlgmaMongoContext>();
            
            var filter = Builders<OperationHistory>.Filter.And(
                Builders<OperationHistory>.Filter.Eq(h => h.DocumentId, documentId),
                Builders<OperationHistory>.Filter.Gt(h => h.SequenceNumber, afterSequence)
            );
            
            var history = await mongoContext.OperationHistory
                .Find(filter)
                .SortBy(h => h.SequenceNumber)
                .Limit(limit)
                .ToListAsync();
            
            await Clients.Caller.SendAsync("OperationHistory", history);
            
            _logger.LogInformation("Sent {Count} operations for document {DocumentId}", history.Count, documentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get operation history");
            await Clients.Caller.SendAsync("Error", "Failed to get operation history");
        }
    }

    /// <summary>
    /// 保存设计数据快照
    /// </summary>
    public async Task SaveDesignSnapshot(string documentId, int version, object designData)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var mongoContext = scope.ServiceProvider.GetRequiredService<CWFlgmaMongoContext>();
            
            // 查找现有文档
            var filter = Builders<DesignDocument>.Filter.Eq(d => d.DocumentId, documentId);
            var existing = await mongoContext.Designs.Find(filter).FirstOrDefaultAsync();
            
            if (existing != null)
            {
                // 更新现有文档
                var update = Builders<DesignDocument>.Update
                    .Set(d => d.Version, version)
                    .Set(d => d.UpdatedAt, DateTime.UtcNow);
                
                await mongoContext.Designs.UpdateOneAsync(filter, update);
            }
            else
            {
                // 创建新文档
                var design = new DesignDocument
                {
                    DocumentId = documentId,
                    Version = version,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                
                await mongoContext.Designs.InsertOneAsync(design);
            }
            
            await Clients.Caller.SendAsync("DesignSaved", documentId, version);
            
            _logger.LogInformation("Design snapshot saved for document {DocumentId}, version {Version}", documentId, version);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save design snapshot");
            await Clients.Caller.SendAsync("Error", "Failed to save design snapshot");
        }
    }

    /// <summary>
    /// 获取设计数据
    /// </summary>
    public async Task GetDesignData(string documentId)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var mongoContext = scope.ServiceProvider.GetRequiredService<CWFlgmaMongoContext>();
            
            var filter = Builders<DesignDocument>.Filter.Eq(d => d.DocumentId, documentId);
            var design = await mongoContext.Designs.Find(filter).FirstOrDefaultAsync();
            
            await Clients.Caller.SendAsync("DesignData", design);
            
            _logger.LogInformation("Sent design data for document {DocumentId}", documentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get design data");
            await Clients.Caller.SendAsync("Error", "Failed to get design data");
        }
    }

    /// <summary>
    /// 请求锁定图层
    /// </summary>
    public async Task<bool> RequestLock(string documentId, string layerId)
    {
        var sessionId = Context.ConnectionId;
        
        if (_documentStates.TryGetValue(documentId, out var docState))
        {
            // 检查是否已锁定
            if (docState.LayerLocks.TryGetValue(layerId, out var lockedBySession))
            {
                if (docState.Users.ContainsKey(lockedBySession))
                {
                    await Clients.Caller.SendAsync("LockFailed", layerId, lockedBySession);
                    return false;
                }
            }
            
            if (docState.Users.TryGetValue(sessionId, out var user))
            {
                docState.LayerLocks[layerId] = sessionId;
                await Clients.Group(documentId).SendAsync("LayerLocked", layerId, sessionId, user.UserId);
                
                _logger.LogInformation("Layer {LayerId} locked by user {UserId}", layerId, user.UserId);
                return true;
            }
        }
        
        return false;
    }

    /// <summary>
    /// 释放图层锁定
    /// </summary>
    public async Task ReleaseLock(string documentId, string layerId)
    {
        var sessionId = Context.ConnectionId;
        
        if (_documentStates.TryGetValue(documentId, out var docState))
        {
            if (docState.LayerLocks.TryGetValue(layerId, out var lockedBySession) && lockedBySession == sessionId)
            {
                docState.LayerLocks.TryRemove(layerId, out _);
                await Clients.Group(documentId).SendAsync("LayerUnlocked", layerId);
                
                _logger.LogInformation("Layer {LayerId} unlocked", layerId);
            }
        }
    }

    /// <summary>
    /// 连接断开时清理
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var sessionId = Context.ConnectionId;
        
        if (_sessionMap.TryRemove(sessionId, out var info))
        {
            await RemoveUserFromDocument(sessionId, info.DocumentId);
        }
        
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// 从文档中移除用户
    /// </summary>
    private async Task RemoveUserFromDocument(string sessionId, string documentId)
    {
        await Groups.RemoveFromGroupAsync(sessionId, documentId);
        
        if (_documentStates.TryGetValue(documentId, out var docState))
        {
            if (docState.Users.TryRemove(sessionId, out var user))
            {
                var lockedLayers = docState.LayerLocks
                    .Where(kvp => kvp.Value == sessionId)
                    .Select(kvp => kvp.Key)
                    .ToList();
                
                foreach (var layerId in lockedLayers)
                {
                    docState.LayerLocks.TryRemove(layerId, out _);
                }
                
                await Clients.OthersInGroup(documentId).SendAsync("UserLeft", sessionId, user.UserId, lockedLayers);
                
                _logger.LogInformation("User {Username} left document {DocumentId}", user.Username, documentId);
            }
        }
        
        _sessionMap.TryRemove(sessionId, out _);
    }
}
