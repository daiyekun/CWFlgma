
//Âñµã£¬Ìí¼ÓÁ´Â·¸ú×Ù
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var serviceName = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME") ?? "CWFlgma.ResourceService";//nameof(CWFlgma.UserService);
var otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") ?? "http://localhost:4317";

var resource = ResourceBuilder.CreateDefault()
    .AddService(serviceName);

// Setup tracing with resource
Sdk.CreateTracerProviderBuilder()
    .SetResourceBuilder(resource)
    .AddSource("CWFlgma.ResourceService")
    .AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint))
    .Build();

var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults (includes OpenTelemetry, health checks, etc.)
builder.AddServiceDefaults();

// Add services to the container
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
