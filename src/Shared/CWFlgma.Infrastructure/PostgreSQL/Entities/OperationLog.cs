using System;
using System.Text.Json;

namespace CWFlgma.Infrastructure.PostgreSQL.Entities;

public class OperationLog
{
    public long Id { get; set; }
    public long DocumentId { get; set; }
    public long UserId { get; set; }
    public string OperationType { get; set; } = string.Empty;
    public string? LayerId { get; set; }
    public JsonDocument OperationData { get; set; } = null!;
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public Document Document { get; set; } = null!;
}
