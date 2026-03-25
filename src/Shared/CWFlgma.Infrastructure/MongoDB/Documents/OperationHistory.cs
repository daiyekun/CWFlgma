using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CWFlgma.Infrastructure.MongoDB.Documents;

public class OperationHistory
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [BsonElement("documentId")]
    public string DocumentId { get; set; } = null!;

    [BsonElement("userId")]
    public string UserId { get; set; } = null!;

    [BsonElement("timestamp")]
    public DateTime Timestamp { get; set; }

    [BsonElement("operation")]
    public Operation Operation { get; set; } = null!;

    [BsonElement("sequenceNumber")]
    public long SequenceNumber { get; set; }

    [BsonElement("sessionId")]
    public string SessionId { get; set; } = null!;
}

public class Operation
{
    [BsonElement("type")]
    public string Type { get; set; } = null!;

    [BsonElement("layerId")]
    public string? LayerId { get; set; }

    [BsonElement("changes")]
    public BsonDocument? Changes { get; set; }

    [BsonElement("previousValues")]
    public BsonDocument? PreviousValues { get; set; }
}
