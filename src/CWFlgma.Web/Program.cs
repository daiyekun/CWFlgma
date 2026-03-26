using CWFlgma.Web;
using CWFlgma.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddOutputCache();

// 配置 HttpClient 指向各个微服务
builder.Services.AddHttpClient("UserService", client =>
{
    client.BaseAddress = new("https+http://userservice");
});

builder.Services.AddHttpClient("DocumentService", client =>
{
    client.BaseAddress = new("https+http://documentservice");
});

builder.Services.AddHttpClient("CollaborationService", client =>
{
    client.BaseAddress = new("https+http://collaborationservice");
});

// 保留原有的 WeatherApiClient（如果需要）
builder.Services.AddHttpClient<WeatherApiClient>(client =>
{
    client.BaseAddress = new("https+http://apiservice");
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();
app.UseOutputCache();
app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.Run();
