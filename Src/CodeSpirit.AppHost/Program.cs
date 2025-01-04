var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var apiService = builder.AddProject<Projects.CodeSpirit_ApiService>("apiservice");

builder.AddProject<Projects.CodeSpirit_IdentityApi>("identity-service");

//builder.AddProject<Projects.CodeSpirit_Web>("webfrontend")
//    .WithExternalHttpEndpoints()
//    .WithReference(cache)
//    .WaitFor(cache)
//    .WithReference(apiService)
//    .WaitFor(apiService);

builder.Build().Run();
