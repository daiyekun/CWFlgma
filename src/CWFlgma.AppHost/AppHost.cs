var builder = DistributedApplication.CreateBuilder(args);

// 添加基础设施资源 - 禁用健康检查
var postgres = builder.AddPostgres("postgres")
    .WithContainerName("cwflgma-postgres")
    .WithDataVolume("cwflgma-postgres-data")
    .WithEnvironment("POSTGRES_HOST_AUTH_METHOD", "trust")
    .WithPgAdmin(pgAdmin => pgAdmin
        .WithContainerName("cwflgma-pgadmin"))
    .AddDatabase("postgresdb");

var mongodb = builder.AddMongoDB("mongodb")
    .WithContainerName("cwflgma-mongodb")
    .WithDataVolume("cwflgma-mongodb-data")
    .WithMongoExpress(mongo => mongo
        .WithContainerName("cwflgma-mongo-express"))
    .AddDatabase("mongodbdb");

var redis = builder.AddRedis("redis")
    .WithContainerName("cwflgma-redis")
    .WithDataVolume("cwflgma-redis-data");

// 添加种子数据服务
var seeder = builder.AddProject<Projects.CWFlgma_Seeder>("seeder")
    .WithReference(postgres)
    .WithReference(mongodb);

// 添加微服务
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
