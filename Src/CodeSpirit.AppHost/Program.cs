IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

//var cache = builder.AddRedis("cache");

//IResourceBuilder<ProjectResource> apiService = builder.AddProject<Projects.CodeSpirit_ApiService>("apiservice");

builder.AddProject<Projects.CodeSpirit_IdentityApi>("identity-api");

// 添加 ConfigCenter 服务
builder.AddProject<Projects.CodeSpirit_ConfigCenter>("config-center");

builder.AddProject<Projects.CodeSpirit_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    //.WithReference(cache)
    //.WaitFor(cache)
    //.WithReference(apiService)
    //.WaitFor(apiService)
    ;

builder.Build().Run();
