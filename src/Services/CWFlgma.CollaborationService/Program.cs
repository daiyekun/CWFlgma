using CWFlgma.CollaborationService.Hubs;
using CWFlgma.Infrastructure;
using CWFlgma.Infrastructure.MongoDB.Documents;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Drawing;

var serviceName = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME") ?? nameof(CWFlgma.CollaborationService);
var otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") ?? "http://localhost:4317";

var resource = ResourceBuilder.CreateDefault()
    .AddService(serviceName);

// Setup tracing with resource
Sdk.CreateTracerProviderBuilder()
    .SetResourceBuilder(resource)
    .AddSource(nameof(CWFlgma.CollaborationService))
    .AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint))
    .Build();

var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults (includes OpenTelemetry, health checks, etc.)
builder.AddServiceDefaults();

// Add services to the container
builder.Services.AddOpenApi();

// 添加基础设施服务（MongoDB）
builder.Services.AddInfrastructure(builder.Configuration);

// 添加 SignalR
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
    options.MaximumReceiveMessageSize = 1024 * 1024; // 1MB
    options.ClientTimeoutInterval = TimeSpan.FromMinutes(2);
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
});

// 添加 CORS - 需要允许 credentials 以支持 WebSocket
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins("https://localhost:7255", "http://localhost:7255", "https://localhost:3000", "http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();  // 必须允许 credentials 以支持 WebSocket
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowAll");
app.UseStaticFiles();

// 映射 SignalR Hub
app.MapHub<CollaborationHub>("/hubs/collaboration")
   .RequireCors("AllowAll");

// 健康检查
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "collaboration" }));

// 获取在线用户
app.MapGet("/api/collaboration/{documentId}/users", (string documentId) =>
{
    var hubType = typeof(CollaborationHub);
    var field = hubType.GetField("_documentStates", 
        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
    
    if (field?.GetValue(null) is System.Collections.Concurrent.ConcurrentDictionary<string, CWFlgma.CollaborationService.Models.DocumentCollaborationState> states)
    {
        if (states.TryGetValue(documentId, out var docState))
        {
            return Results.Ok(docState.Users.Values);
        }
    }
    
    return Results.Ok(Array.Empty<object>());
});

// 获取操作历史
app.MapGet("/api/collaboration/{documentId}/history", async (string documentId, long afterSequence, int limit) =>
{
    try
    {
        using var scope = app.Services.CreateScope();
        var mongoContext = scope.ServiceProvider.GetRequiredService<CWFlgma.Infrastructure.MongoDB.CWFlgmaMongoContext>();
        var filter = Builders<OperationHistory>.Filter.And(
            Builders<OperationHistory>.Filter.Eq(h => h.DocumentId, documentId),
            Builders<OperationHistory>.Filter.Gt(h => h.SequenceNumber, afterSequence)
        );
        
        var history = await mongoContext.OperationHistory
            .Find(filter)
            .SortBy(h => h.SequenceNumber)
            .Limit(limit > 0 ? limit : 100)
            .ToListAsync();
        
        return Results.Ok(history);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

app.Run();
