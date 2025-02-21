IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache")
                   .WithLifetime(ContainerLifetime.Persistent)
                   .WithRedisCommander()
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

builder.AddProject<Projects.CodeSpirit_IdentityApi>("identity-api")
    .WithReference(seqService)
    .WithReference(cache)
    ;

// 添加 ConfigCenter 服务
builder.AddProject<Projects.CodeSpirit_ConfigCenter>("config-api")
    .WithReference(seqService)
    .WithReference(cache)
    ;

builder.AddProject<Projects.CodeSpirit_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(seqService)
    ;

builder.Build().Run();
