using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using CWFlgma.Infrastructure.Authentication;
using CWFlgma.Infrastructure.Authorization;
using CWFlgma.Infrastructure.PostgreSQL;
using CWFlgma.Infrastructure.MongoDB;
using CWFlgma.Infrastructure.Storage;

namespace CWFlgma.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // PostgreSQL - 支持 Aspire 环境变量命名
        var postgresConnectionString = GetConnectionString(configuration, "postgresdb", "postgres", "PostgreSQL");
        
        Console.WriteLine($"Original PG: {postgresConnectionString?[..Math.Min(80, postgresConnectionString?.Length ?? 0)]}");
        
        if (postgresConnectionString != null)
        {
            // 解析连接字符串并强制禁用 SSL
            try
            {
                var builder = new Npgsql.NpgsqlConnectionStringBuilder();
                
                // 手动解析原始连接字符串
                var parts = postgresConnectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in parts)
                {
                    var keyValue = part.Split('=', 2);
                    if (keyValue.Length == 2)
                    {
                        var partKey = keyValue[0].Trim();
                        var partValue = keyValue[1].Trim();
                        
                        switch (partKey.ToLower())
                        {
                            case "host":
                            case "server":
                                builder.Host = partValue;
                                break;
                            case "port":
                                if (int.TryParse(partValue, out var port))
                                    builder.Port = port;
                                break;
                            case "username":
                            case "user id":
                                builder.Username = partValue;
                                break;
                            case "password":
                                builder.Password = partValue;
                                break;
                            case "database":
                            case "initial catalog":
                                builder.Database = partValue;
                                break;
                        }
                    }
                }
                
                // 强制设置 SSL 模式
                builder.SslMode = Npgsql.SslMode.Disable;
                builder.IncludeErrorDetail = true;
                builder.Timeout = 30;
                builder.CommandTimeout = 30;
                builder.Pooling = true;
                builder.MinPoolSize = 1;
                builder.MaxPoolSize = 20;
                
                postgresConnectionString = builder.ConnectionString;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Parse error: {ex.Message}");
                // 回退：直接附加 SSL 参数
                if (!postgresConnectionString.Contains("SSL Mode", StringComparison.OrdinalIgnoreCase))
                {
                    postgresConnectionString += ";SSL Mode=Disable";
                }
            }
        }
        
        Console.WriteLine($"Final PG: {postgresConnectionString?[..Math.Min(120, postgresConnectionString?.Length ?? 0)]}");
        services.AddDbContext<CWFlgmaDbContext>(options =>
            options.UseNpgsql(postgresConnectionString));

        // MongoDB - 支持 Aspire 环境变量命名
        var mongoConnectionString = GetConnectionString(configuration, "mongodbdb", "mongodb", "MongoDB");
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

        // Authentication & Authorization
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IAuthorizationService, AuthorizationService>();
        services.AddScoped<ITeamService, TeamService>();
        services.AddScoped<IDocumentPermissionService, DocumentPermissionService>();

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
                // 替换 localhost 为 127.0.0.1 避免 IPv6 问题
                return envValue.Replace("localhost", "127.0.0.1");
            }

            // 2. 检查标准格式
            var connString = configuration.GetConnectionString(name);
            if (!string.IsNullOrEmpty(connString))
            {
                Console.WriteLine($"Found standard connection string for '{name}'");
                // 替换 localhost 为 127.0.0.1 避免 IPv6 问题
                return connString.Replace("localhost", "127.0.0.1");
            }
        }

        Console.WriteLine($"No connection string found for any of: {string.Join(", ", names)}");
        return null;
    }
}
