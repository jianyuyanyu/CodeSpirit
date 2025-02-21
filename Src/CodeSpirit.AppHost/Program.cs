IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache")
                   .WithRedisInsight()
                   .WithRedisCommander()
                   .WithDataVolume(isReadOnly: false);

//IResourceBuilder<ProjectResource> apiService = builder.AddProject<Projects.CodeSpirit_ApiService>("apiservice");

// Add Seq logging service
var seqService = builder.AddSeq("seq")
                 .WithDataVolume()
                 .ExcludeFromManifest()
                 .WithLifetime(ContainerLifetime.Persistent)
                 .WithEnvironment("ACCEPT_EULA", "Y");

builder.AddProject<Projects.CodeSpirit_IdentityApi>("identity-api")
    .WithReference(seqService)
    .WithReference(cache);

// 添加 ConfigCenter 服务
builder.AddProject<Projects.CodeSpirit_ConfigCenter>("config-api")
    .WithReference(seqService)
    .WithReference(cache);

builder.AddProject<Projects.CodeSpirit_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(seqService);

builder.Build().Run();
