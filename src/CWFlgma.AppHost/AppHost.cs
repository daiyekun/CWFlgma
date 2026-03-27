var builder = DistributedApplication.CreateBuilder(args);

// 连接字符串 - 直接使用 docker-compose 容器
var postgresConnString = "Host=localhost;Port=5432;Database=postgresdb;Username=postgres;Password=postgres";
var mongodbConnString = "mongodb://localhost:27017/cwflgma";
var redisConnString = "localhost:6379";

// 添加微服务 - 使用环境变量传递连接字符串
var userService = builder.AddProject<Projects.CWFlgma_UserService>("userservice")
    .WithEnvironment("ConnectionStrings__postgresdb", postgresConnString)
    .WithEnvironment("ConnectionStrings__redis", redisConnString);

var documentService = builder.AddProject<Projects.CWFlgma_DocumentService>("documentservice")
    .WithEnvironment("ConnectionStrings__postgresdb", postgresConnString)
    .WithEnvironment("ConnectionStrings__mongodbdb", mongodbConnString)
    .WithEnvironment("ConnectionStrings__redis", redisConnString);

var collaborationService = builder.AddProject<Projects.CWFlgma_CollaborationService>("collaborationservice")
    .WithEnvironment("ConnectionStrings__mongodbdb", mongodbConnString)
    .WithEnvironment("ConnectionStrings__redis", redisConnString);

var resourceService = builder.AddProject<Projects.CWFlgma_ResourceService>("resourceservice")
    .WithEnvironment("ConnectionStrings__postgresdb", postgresConnString);

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
