IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache")
                   .WithLifetime(ContainerLifetime.Persistent)
                   //.WithEndpoint(port: 61690, targetPort: 6137, name: "redis")
                   .WithRedisCommander((op) =>
                   {
                       op.WithHttpEndpoint(port: 61689, targetPort: 8081, name: "commander-ui");
                   })
                   .WithDataVolume(isReadOnly: false);

// Add Seq logging service
var seqService = builder.AddSeq("seq")
                 .WithDataVolume()
                 .ExcludeFromManifest()
                 .WithLifetime(ContainerLifetime.Persistent)
                 //.WithHttpsEndpoint(port: 10001, targetPort: 45341)
                 .WithHttpEndpoint(port: 61688, targetPort: 80, name: "seq-ui")
                 .WithEnvironment("ACCEPT_EULA", "Y");

builder.AddProject<Projects.CodeSpirit_IdentityApi>("identity")
    .WithReference(seqService)
    .WithReference(cache)
    ;

// 添加 ConfigCenter 服务
builder.AddProject<Projects.CodeSpirit_ConfigCenter>("config")
    .WithReference(seqService)
    .WithReference(cache)
    ;

builder.AddProject<Projects.CodeSpirit_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(cache)
    .WithReference(seqService)
    ;

builder.Build().Run();
