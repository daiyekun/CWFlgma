using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using CWFlgma.Infrastructure.Authentication;
using CWFlgma.Infrastructure.PostgreSQL;
using CWFlgma.Infrastructure.MongoDB;
using CWFlgma.Infrastructure.Storage;

namespace CWFlgma.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // PostgreSQL - 支持 Aspire 环境变量命名
        var postgresConnectionString = GetConnectionString(configuration, "postgres", "PostgreSQL", "postgresdb");
        Console.WriteLine($"PostgreSQL Connection String: {postgresConnectionString?[..Math.Min(60, postgresConnectionString?.Length ?? 0)]}...");
        services.AddDbContext<CWFlgmaDbContext>(options =>
            options.UseNpgsql(postgresConnectionString));

        // MongoDB - 支持 Aspire 环境变量命名
        var mongoConnectionString = GetConnectionString(configuration, "mongodb", "MongoDB", "mongodbdb");
        Console.WriteLine($"MongoDB Connection String: {mongoConnectionString?[..Math.Min(60, mongoConnectionString?.Length ?? 0)]}...");
        services.AddSingleton<IMongoClient>(sp =>
        {
            return new MongoClient(mongoConnectionString);
        });

        services.AddScoped<CWFlgmaMongoContext>(sp =>
        {
            var client = sp.GetRequiredService<IMongoClient>();
            var databaseName = configuration["MongoDB:DatabaseName"] ?? "cwflgma";
            return new CWFlgmaMongoContext(client, databaseName);
        });

        // Storage
        services.Configure<StorageOptions>(options =>
        {
            var section = configuration.GetSection("Storage");
            options.Type = section["Type"] ?? "Local";
            options.LocalPath = section["LocalPath"] ?? "./storage";
            options.BaseUrl = section["BaseUrl"] ?? "https://localhost:5004/resources";
            options.MaxImageSize = section.GetValue<long>("MaxImageSize", 10 * 1024 * 1024);
            options.MaxFontSize = section.GetValue<long>("MaxFontSize", 5 * 1024 * 1024);
        });
        services.AddScoped<IStorageProvider, LocalStorageProvider>();

        // Authentication
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddScoped<IAuthenticationService, AuthenticationService>();

        // JWT Authentication
        var jwtOptions = new JwtOptions();
        configuration.GetSection(JwtOptions.SectionName).Bind(jwtOptions);
        var key = Encoding.UTF8.GetBytes(jwtOptions.Secret);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtOptions.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("RequireAuthentication", policy =>
                policy.RequireAuthenticatedUser());
        });

        return services;
    }

    /// <summary>
    /// 获取连接字符串，支持 Aspire 环境变量（ConnectionStrings__<name>）和 appsettings.json
    /// </summary>
    private static string? GetConnectionString(IConfiguration configuration, params string[] names)
    {
        foreach (var name in names)
        {
            // 1. 先检查 Aspire 环境变量格式: ConnectionStrings__<name>
            var envValue = configuration[$"ConnectionStrings:{name}"];
            if (!string.IsNullOrEmpty(envValue))
            {
                Console.WriteLine($"Found Aspire connection string for '{name}'");
                return envValue;
            }

            // 2. 检查标准格式
            var connString = configuration.GetConnectionString(name);
            if (!string.IsNullOrEmpty(connString))
            {
                Console.WriteLine($"Found standard connection string for '{name}'");
                return connString;
            }
        }

        Console.WriteLine($"No connection string found for any of: {string.Join(", ", names)}");
        return null;
    }
}
