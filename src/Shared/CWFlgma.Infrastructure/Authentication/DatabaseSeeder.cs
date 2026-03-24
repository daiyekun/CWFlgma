using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using CWFlgma.Infrastructure.PostgreSQL;
using CWFlgma.Infrastructure.PostgreSQL.Entities;
using CWFlgma.Infrastructure.Common;

namespace CWFlgma.Infrastructure.Authentication;

public class DatabaseSeeder
{
    private readonly CWFlgmaDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(
        CWFlgmaDbContext context,
        IConfiguration configuration,
        ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        // 确保数据库已创建
        await _context.Database.EnsureCreatedAsync();

        // 检查是否已有数据
        if (await _context.Users.AnyAsync())
        {
            _logger.LogInformation("Database already seeded. Skipping...");
            return;
        }

        _logger.LogInformation("Seeding database...");

        // 创建管理员用户
        var adminUser = await SeedAdminUserAsync();
        
        // 创建演示用户
        var demoUser = await SeedDemoUserAsync();

        // 创建默认团队
        var defaultTeam = await SeedDefaultTeamAsync(adminUser);

        // 将用户添加到团队
        await AddUserToTeamAsync(adminUser, defaultTeam, "owner");
        await AddUserToTeamAsync(demoUser, defaultTeam, "member");

        // 创建示例文档
        await SeedSampleDocumentsAsync(adminUser, defaultTeam);

        await _context.SaveChangesAsync();
        _logger.LogInformation("Database seeding completed.");
    }

    private async Task<User> SeedAdminUserAsync()
    {
        var adminConfig = _configuration.GetSection("SeedData:AdminUser");
        
        var adminUser = new User
        {
            Id = IdGeneratorExtensions.NewId(),  // 雪花算法
            Username = adminConfig["Username"] ?? "admin",
            Email = adminConfig["Email"] ?? "admin@cwflgma.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminConfig["Password"] ?? "Admin@123456"),
            DisplayName = adminConfig["DisplayName"] ?? "System Administrator",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.Users.Add(adminUser);
        _logger.LogInformation("Admin user created: {Email}", adminUser.Email);
        
        return adminUser;
    }

    private async Task<User> SeedDemoUserAsync()
    {
        var demoConfig = _configuration.GetSection("SeedData:DemoUser");
        
        var demoUser = new User
        {
            Id = IdGeneratorExtensions.NewId(),  // 雪花算法
            Username = demoConfig["Username"] ?? "demo",
            Email = demoConfig["Email"] ?? "demo@cwflgma.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(demoConfig["Password"] ?? "Demo@123456"),
            DisplayName = demoConfig["DisplayName"] ?? "Demo User",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.Users.Add(demoUser);
        _logger.LogInformation("Demo user created: {Email}", demoUser.Email);
        
        return demoUser;
    }

    private async Task<Team> SeedDefaultTeamAsync(User owner)
    {
        var teamConfig = _configuration.GetSection("SeedData:DefaultTeam");
        
        var defaultTeam = new Team
        {
            Id = IdGeneratorExtensions.NewId(),  // 雪花算法
            Name = teamConfig["Name"] ?? "Default Team",
            Description = teamConfig["Description"] ?? "Default team for all users",
            OwnerId = owner.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Teams.Add(defaultTeam);
        _logger.LogInformation("Default team created: {TeamName}", defaultTeam.Name);
        
        return defaultTeam;
    }

    private async Task AddUserToTeamAsync(User user, Team team, string role)
    {
        var teamMember = new TeamMember
        {
            Id = IdGeneratorExtensions.NewId(),  // 雪花算法
            TeamId = team.Id,
            UserId = user.Id,
            Role = role,
            JoinedAt = DateTime.UtcNow
        };

        _context.TeamMembers.Add(teamMember);
        _logger.LogInformation("User {Username} added to team {TeamName} as {Role}", 
            user.Username, team.Name, role);
    }

    private async Task SeedSampleDocumentsAsync(User owner, Team team)
    {
        // 创建示例文件夹
        var folder = new Document
        {
            Id = IdGeneratorExtensions.NewId(),  // 雪花算法
            Title = "My Designs",
            Description = "Folder for my design files",
            OwnerId = owner.Id,
            TeamId = team.Id,
            Type = "folder",
            Width = 0,
            Height = 0,
            BackgroundColor = "#FFFFFF",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Version = 1
        };

        _context.Documents.Add(folder);
        _logger.LogInformation("Sample folder created: {FolderName}", folder.Title);

        // 创建示例文档
        var document = new Document
        {
            Id = IdGeneratorExtensions.NewId(),  // 雪花算法
            Title = "Welcome to CWFlgma",
            Description = "A sample design document",
            OwnerId = owner.Id,
            TeamId = team.Id,
            ParentId = folder.Id,
            Type = "design",
            Width = 1920,
            Height = 1080,
            BackgroundColor = "#F5F5F5",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Version = 1
        };

        _context.Documents.Add(document);
        _logger.LogInformation("Sample document created: {DocumentTitle}", document.Title);
    }
}
