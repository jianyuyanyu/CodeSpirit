IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

//var cache = builder.AddRedis("cache");

//IResourceBuilder<ProjectResource> apiService = builder.AddProject<Projects.CodeSpirit_ApiService>("apiservice");

// Add Seq logging service
var seqService = builder.AddSeq("seq")
                 .WithDataVolume()
                 .ExcludeFromManifest()
                 .WithLifetime(ContainerLifetime.Persistent)
                 .WithEnvironment("ACCEPT_EULA", "Y");

builder.AddProject<Projects.CodeSpirit_IdentityApi>("identity-api")
    .WithReference(seqService);

// 添加 ConfigCenter 服务
builder.AddProject<Projects.CodeSpirit_ConfigCenter>("config-api")
    .WithReference(seqService);

builder.AddProject<Projects.CodeSpirit_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    //.WithReference(cache)
    //.WaitFor(cache)
    //.WithReference(apiService)
    //.WaitFor(apiService)
    .WithReference(seqService);

builder.Build().Run();
