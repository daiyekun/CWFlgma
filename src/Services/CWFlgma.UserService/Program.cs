using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CWFlgma.Infrastructure;
using CWFlgma.Infrastructure.Authentication;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddOpenApi();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// Auth endpoints
app.MapPost("/api/auth/login", async (LoginRequest request, IAuthenticationService authService) =>
{
    var result = await authService.LoginAsync(request);
    if (!result.Success)
    {
        return Results.BadRequest(new { error = result.ErrorMessage });
    }
    return Results.Ok(result);
});

app.MapPost("/api/auth/register", async (RegisterRequest request, IAuthenticationService authService) =>
{
    var result = await authService.RegisterAsync(request);
    if (!result.Success)
    {
        return Results.BadRequest(new { error = result.ErrorMessage });
    }
    return Results.Created($"/api/users/{result.User?.Id}", result);
});

app.MapPost("/api/auth/refresh", async (RefreshTokenRequest request, IAuthenticationService authService) =>
{
    var result = await authService.RefreshTokenAsync(request);
    if (!result.Success)
    {
        return Results.BadRequest(new { error = result.ErrorMessage });
    }
    return Results.Ok(result);
});

app.MapPost("/api/auth/validate", async (string token, IAuthenticationService authService) =>
{
    var isValid = await authService.ValidateTokenAsync(token);
    return Results.Ok(new { isValid });
});

app.Run();
