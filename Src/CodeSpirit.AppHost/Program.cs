using Aspire.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Aspire.Hosting.Elasticsearch;

var builder = DistributedApplication.CreateBuilder(args);

// 添加 Redis 缓存服务
var cache = builder.AddRedis("cache")
                   .WithLifetime(ContainerLifetime.Persistent)
                   .WithHostPort(61690)  // 使用新的 WithHostPort 方法
                   .WithRedisCommander((op) =>
                   {
                       op.WithHttpEndpoint(port: 61689, targetPort: 8081, name: "commander-ui")
                         .WithUrlForEndpoint("commander-ui", url => 
                             url.DisplayLocation = UrlDisplayLocation.SummaryAndDetails);
                   });

// 添加 Seq 日志服务
var seqService = builder.AddSeq("seq")
                 .WithDataVolume()
                 .WithLifetime(ContainerLifetime.Persistent)
                 .WithHttpEndpoint(port: 61688, targetPort: 80, name: "seq-ui")
                 .WithEnvironment("ACCEPT_EULA", "Y")
                 .WithUrlForEndpoint("seq-ui", url => 
                     url.DisplayText = "Seq 日志界面");

// 添加 RabbitMQ 服务的用户名和密码参数
var rabbitmqUser = builder.AddParameter("rabbitmq-username", "admin");
var rabbitmqPass = builder.AddParameter("rabbitmq-password", "Password123", secret: true);

// 添加 RabbitMQ 服务 (在9.3中可能尚未支持WithUserName方法，使用旧方式)
var rabbitmqService = builder.AddRabbitMQ("rabbitmq", rabbitmqUser, rabbitmqPass)
                     .WithManagementPlugin()
                     .WithLifetime(ContainerLifetime.Persistent)
                     .WithUrlForEndpoint("management", url => {
                         url.DisplayText = "RabbitMQ 管理界面";
                     });

// 添加 Elasticsearch 服务
var elasticsearchService = builder.AddElasticsearch("elasticsearch")
                          .WithLifetime(ContainerLifetime.Persistent)
                          .WithDataVolume()
                          .WithHttpEndpoint(port: 61687, targetPort: 9200, name: "elasticsearch")
                          .WithHttpEndpoint(port: 61686, targetPort: 9300, name: "elasticsearch-nodes")
                          .WithUrlForEndpoint("elasticsearch", ep => new() {
                              Url = "/_cluster/health",
                              DisplayText = "ES 集群健康状态",
                              DisplayLocation = UrlDisplayLocation.DetailsOnly
                          });

// 添加 ConfigCenter 服务
var configService = builder.AddProject<Projects.CodeSpirit_ConfigCenter>("config")
    .WithReference(seqService)
    .WithReference(cache);

var identityService = builder.AddProject<Projects.CodeSpirit_IdentityApi>("identity")
    .WithReference(seqService)
    .WithReference(cache)
    .WithReference(configService)
    .WithReference(rabbitmqService);

// 添加消息服务
var messagingService = builder.AddProject<Projects.CodeSpirit_MessagingApi>("messaging")
    .WithReference(seqService)
    .WithReference(cache)
    .WithReference(configService);

var examService = builder.AddProject<Projects.CodeSpirit_ExamApi>("exam")
    .WithReference(seqService)
    .WithReference(cache)
    .WithReference(configService)
    .WithReference(rabbitmqService)
    .WithReference(elasticsearchService);

builder.AddProject<Projects.CodeSpirit_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(cache)
    .WithReference(seqService)
    .WithReference(rabbitmqService)
    .WithReference(identityService)
    .WithReference(configService)
    .WithReference(messagingService)
    .WithReference(examService)
    .WithReference(elasticsearchService)
    .WithUrlForEndpoint("https", url => { 
        url.DisplayText = "Web 前端"; 
    })
    .WithUrlForEndpoint("https", ep => new() {
        Url = "/health",
        DisplayText = "健康检查",
        DisplayLocation = UrlDisplayLocation.DetailsOnly
    });

// 注册资源初始化事件，需要提供CancellationToken参数
builder.Eventing.Subscribe<InitializeResourceEvent>((eventData, cancellationToken) => {
    Console.WriteLine($"资源初始化: {eventData.Resource.Name}");
    return Task.CompletedTask;
});

builder.Build().Run();
