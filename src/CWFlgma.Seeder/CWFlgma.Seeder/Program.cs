using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using CWFlgma.Infrastructure;
using CWFlgma.Infrastructure.PostgreSQL;
using CWFlgma.Infrastructure.PostgreSQL.Entities;
using CWFlgma.Infrastructure.Authentication;
using Microsoft.EntityFrameworkCore;
using Npgsql;

Console.WriteLine("=== CWFlgma Database Seeder Starting ===");

// 创建服务集合
var services = new ServiceCollection();

// 添加配置 - 支持 Aspire 环境变量
var configurationBuilder = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables();

var configuration = configurationBuilder.Build();

// 调试：输出所有环境变量
Console.WriteLine("\n=== Environment Variables ===");
foreach (var env in Environment.GetEnvironmentVariables().Cast<System.Collections.DictionaryEntry>())
{
    var key = env.Key.ToString() ?? "";
    if (key.StartsWith("ConnectionStrings") || key.StartsWith("DOTNET") || key.StartsWith("ASPNETCORE"))
    {
        Console.WriteLine($"{key} = {env.Value}");
    }
}

// 获取连接字符串
var pgConn = configuration.GetConnectionString("postgresdb") ?? configuration.GetConnectionString("PostgreSQL");
Console.WriteLine($"\n=== PostgreSQL Connection String ===");
Console.WriteLine($"Connection: {(string.IsNullOrEmpty(pgConn) ? "NOT FOUND" : pgConn[..Math.Min(60, pgConn.Length)] + "...")}");

if (string.IsNullOrEmpty(pgConn))
{
    Console.WriteLine("ERROR: No PostgreSQL connection string found!");
    return;
}

services.AddSingleton<IConfiguration>(configuration);

// 添加日志
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});

// 添加基础设施服务
services.AddInfrastructure(configuration);
services.AddScoped<DatabaseSeeder>();

var serviceProvider = services.BuildServiceProvider();

using var scope = serviceProvider.CreateScope();
var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

try
{
    // 1. 先创建数据库（如果不存在）
    logger.LogInformation("Step 1: Creating database if not exists...");
    
    // 连接到默认的 postgres 数据库来创建我们的数据库
    var builder = new NpgsqlConnectionStringBuilder(pgConn);
    var targetDatabase = builder.Database;
    builder.Database = "postgres"; // 连接到默认数据库
    
    using (var conn = new NpgsqlConnection(builder.ConnectionString))
    {
        await conn.OpenAsync();
        
        // 检查数据库是否存在
        using var checkCmd = new NpgsqlCommand(
            $"SELECT 1 FROM pg_database WHERE datname = @dbname", conn);
        checkCmd.Parameters.AddWithValue("dbname", targetDatabase);
        var exists = await checkCmd.ExecuteScalarAsync();
        
        if (exists == null)
        {
            // 创建数据库
            using var createCmd = new NpgsqlCommand($"CREATE DATABASE \"{targetDatabase}\"", conn);
            await createCmd.ExecuteNonQueryAsync();
            logger.LogInformation("Database '{Database}' created successfully.", targetDatabase);
        }
        else
        {
            logger.LogInformation("Database '{Database}' already exists.", targetDatabase);
        }
    }
    
    // 2. 创建表
    logger.LogInformation("Step 2: Creating tables...");
    var dbContext = scope.ServiceProvider.GetRequiredService<CWFlgmaDbContext>();
    
    // 测试数据库连接
    var canConnect = await dbContext.Database.CanConnectAsync();
    logger.LogInformation("Database connection test: {CanConnect}", canConnect);
    
    if (!canConnect)
    {
        logger.LogError("Cannot connect to database. Please check connection string.");
        return;
    }
    
    // 创建表
    await dbContext.Database.EnsureCreatedAsync();
    logger.LogInformation("Tables created successfully.");
    
    // 3. 运行种子数据
    logger.LogInformation("Step 3: Seeding data...");
    var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    await seeder.SeedAsync();
    
    logger.LogInformation("=== Database seeding completed successfully. ===");
}
catch (Exception ex)
{
    logger.LogError(ex, "An error occurred while seeding the database.");
    Console.WriteLine($"ERROR: {ex.Message}");
    Console.WriteLine($"STACK: {ex.StackTrace}");
}

Console.WriteLine("=== Seeder finished ===");
