using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CWFlgma.Infrastructure.MongoDB.Documents;

public class CollaborationSession
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [BsonElement("documentId")]
    public string DocumentId { get; set; } = null!;

    [BsonElement("sessionId")]
    public string SessionId { get; set; } = null!;

    [BsonElement("userId")]
    public string UserId { get; set; } = null!;

    [BsonElement("userName")]
    public string UserName { get; set; } = null!;

    [BsonElement("userAvatar")]
    public string? UserAvatar { get; set; }

    [BsonElement("cursor")]
    public CursorPosition? Cursor { get; set; }

    [BsonElement("selection")]
    public Selection? Selection { get; set; }

    [BsonElement("viewport")]
    public Viewport? Viewport { get; set; }

    [BsonElement("lastActivity")]
    public DateTime LastActivity { get; set; }

    [BsonElement("connectedAt")]
    public DateTime ConnectedAt { get; set; }
}

public class CursorPosition
{
    [BsonElement("x")]
    public double X { get; set; }

    [BsonElement("y")]
    public double Y { get; set; }
}

public class Selection
{
    [BsonElement("layerIds")]
    public string[]? LayerIds { get; set; }

    [BsonElement("pageId")]
    public string? PageId { get; set; }
}

public class Viewport
{
    [BsonElement("x")]
    public double X { get; set; }

    [BsonElement("y")]
    public double Y { get; set; }

    [BsonElement("zoom")]
    public double Zoom { get; set; } = 1;
}
