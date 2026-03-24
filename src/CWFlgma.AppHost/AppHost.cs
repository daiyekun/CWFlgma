var builder = DistributedApplication.CreateBuilder(args);

// 添加基础设施资源
var postgres = builder.AddPostgres("postgres")
    .WithContainerName("cwflgma-postgres")
    .WithDataVolume("cwflgma-postgres-data")
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

// 添加种子数据服务（先运行）
var seeder = builder.AddProject<Projects.CWFlgma_Seeder>("seeder")
    .WithReference(postgres)
    .WithReference(mongodb)
    .WaitFor(postgres)
    .WaitFor(mongodb);

// 添加微服务（等待种子数据完成）
var userService = builder.AddProject<Projects.CWFlgma_UserService>("userservice")
    .WithReference(postgres)
    .WithReference(redis)
    .WaitFor(postgres)
    .WaitFor(redis)
    .WaitForCompletion(seeder);

var documentService = builder.AddProject<Projects.CWFlgma_DocumentService>("documentservice")
    .WithReference(postgres)
    .WithReference(mongodb)
    .WithReference(redis)
    .WaitFor(postgres)
    .WaitFor(mongodb)
    .WaitFor(redis)
    .WaitForCompletion(seeder);

var collaborationService = builder.AddProject<Projects.CWFlgma_CollaborationService>("collaborationservice")
    .WithReference(mongodb)
    .WithReference(redis)
    .WaitFor(mongodb)
    .WaitFor(redis)
    .WaitForCompletion(seeder);

var resourceService = builder.AddProject<Projects.CWFlgma_ResourceService>("resourceservice")
    .WithReference(postgres)
    .WaitFor(postgres)
    .WaitForCompletion(seeder);

var gateway = builder.AddProject<Projects.CWFlgma_Gateway>("gateway")
    .WithExternalHttpEndpoints()
    .WithReference(userService)
    .WithReference(documentService)
    .WithReference(collaborationService)
    .WithReference(resourceService)
    .WaitFor(userService)
    .WaitFor(documentService)
    .WaitFor(collaborationService)
    .WaitFor(resourceService);

builder.Build().Run();
