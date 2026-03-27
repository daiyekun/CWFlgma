var builder = DistributedApplication.CreateBuilder(args);

// 添加基础设施资源 - 由 Aspire 管理
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume("cwflgma-postgres-data")
    .WithPgAdmin(pgadmin => pgadmin.WithHostPort(5050))
    .AddDatabase("postgresdb");

var mongodb = builder.AddMongoDB("mongodb")
    .WithDataVolume("cwflgma-mongodb-data")
    .WithMongoExpress(me => me.WithHostPort(8081))
    .AddDatabase("mongodbdb");

var redis = builder.AddRedis("redis")
    .WithDataVolume("cwflgma-redis-data");

// 添加种子数据服务
var seeder = builder.AddProject<Projects.CWFlgma_Seeder>("seeder")
    .WithReference(postgres)
    .WithReference(mongodb);

// 添加微服务 - Aspire 会自动注入 OTLP 环境变量
var userService = builder.AddProject<Projects.CWFlgma_UserService>("userservice")
    .WithReference(postgres)
    .WithReference(redis);

var documentService = builder.AddProject<Projects.CWFlgma_DocumentService>("documentservice")
    .WithReference(postgres)
    .WithReference(mongodb)
    .WithReference(redis);

var collaborationService = builder.AddProject<Projects.CWFlgma_CollaborationService>("collaborationservice")
    .WithReference(mongodb)
    .WithReference(redis);

var resourceService = builder.AddProject<Projects.CWFlgma_ResourceService>("resourceservice")
    .WithReference(postgres);

var gateway = builder.AddProject<Projects.CWFlgma_Gateway>("gateway")
    .WithExternalHttpEndpoints()
    .WithReference(userService)
    .WithReference(documentService)
    .WithReference(collaborationService)
    .WithReference(resourceService);

// 添加前端 Web 应用
var webApp = builder.AddProject<Projects.CWFlgma_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(userService)
    .WithReference(documentService)
    .WithReference(collaborationService)
    .WithReference(gateway);

builder.Build().Run();
