using Aspire.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Aspire.Hosting.Elasticsearch;
using System.Text;

/// <summary>
/// Aspire应用宿主程序入口点
/// </summary>
/// <remarks>
/// 该程序负责启动和协调整个微服务应用的运行
/// </remarks>

// 设置控制台编码为UTF-8以正确显示中文字符
Console.OutputEncoding = Encoding.UTF8;

var builder = DistributedApplication.CreateBuilder(args);

// 添加 Redis 缓存服务
var cache = builder.AddRedis("cache")
                   .WithLifetime(ContainerLifetime.Persistent)
                   .WithHostPort(6380)  // 修改为安全端口范围
                   .WithRedisCommander((op) =>
                   {
                       op
                         //.WithHttpEndpoint(port: 8082, targetPort: 8081, name: "commander-ui")
                         .WithUrlForEndpoint("commander-ui", url =>
                             url.DisplayLocation = UrlDisplayLocation.SummaryAndDetails);
                   });

// 添加 Seq 日志服务
var seqService = builder.AddSeq("seq")
                    .WithImageTag("2024.3")
                 .WithDataVolume()
                 .WithLifetime(ContainerLifetime.Persistent)
                 //.WithHttpEndpoint(port: 5341, targetPort: 80, name: "seq-ui")
                 .WithUrlForEndpoint("seq", url => url.DisplayLocation = UrlDisplayLocation.SummaryAndDetails)
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
                     .WithUrlForEndpoint("management", url =>
                     {
                         url.DisplayText = "RabbitMQ 管理界面";
                     });

// 添加 Elasticsearch 服务
var esPassword = builder.AddParameter("password", "Password123", secret: true);
var elasticsearchService = builder.AddElasticsearch("elasticsearch", password: esPassword)
                          .WithLifetime(ContainerLifetime.Persistent)
                          .WithDataVolume()
                          //.WithHttpEndpoint(port: 9200, targetPort: 9200, name: "elasticsearch")
                          //.WithHttpEndpoint(port: 9300, targetPort: 9300, name: "elasticsearch-nodes")
                          .WithUrlForEndpoint("elasticsearch", ep => new()
                          {
                              Url = "/_cluster/health",
                              DisplayText = "ES 集群健康状态",
                              DisplayLocation = UrlDisplayLocation.DetailsOnly
                          });

// 添加统一的JWT配置参数
var jwtSecretKey = builder.AddParameter(name: "jwt-SecretKey", "ECBF8FA013844D77AE041A6800D7FF8F", secret: true);
var jwtIssuer = builder.AddParameter(name: "jwt-Issuer", "codespirit.com");
var jwtAudience = builder.AddParameter(name: "jwt-Audience", "CodeSpirit");

// 添加 ConfigCenter 服务
var configService = builder.AddProject<Projects.CodeSpirit_ConfigCenter>("config")
    .WithReference(seqService)
    .WithReference(cache)
    .WithEnvironment("Jwt__SecretKey", jwtSecretKey)
    .WithEnvironment("Jwt__Issuer", jwtIssuer)
    .WithEnvironment("Jwt__Audience", jwtAudience);

var identityService = builder.AddProject<Projects.CodeSpirit_IdentityApi>("identity")
    .WithReference(seqService)
    .WithReference(cache)
    .WithReference(configService)
    .WithReference(rabbitmqService)
    .WithEnvironment("Jwt__SecretKey", jwtSecretKey)
    .WithEnvironment("Jwt__Issuer", jwtIssuer)
    .WithEnvironment("Jwt__Audience", jwtAudience);

// 添加消息服务
var messagingService = builder.AddProject<Projects.CodeSpirit_MessagingApi>("messaging")
    .WithReference(seqService)
    .WithReference(cache)
    .WithReference(configService)
    .WithEnvironment("Jwt__SecretKey", jwtSecretKey)
    .WithEnvironment("Jwt__Issuer", jwtIssuer)
    .WithEnvironment("Jwt__Audience", jwtAudience);

var examService = builder.AddProject<Projects.CodeSpirit_ExamApi>("exam")
    .WithReference(seqService)
    .WithReference(cache)
    .WithReference(configService)
    .WithReference(rabbitmqService)
    .WithReference(elasticsearchService)
    .WithEnvironment("Jwt__SecretKey", jwtSecretKey)
    .WithEnvironment("Jwt__Issuer", jwtIssuer)
    .WithEnvironment("Jwt__Audience", jwtAudience);

var fileService = builder.AddProject<Projects.CodeSpirit_FileStorageApi>("file")
    .WithReference(seqService)
    .WithReference(cache)
    .WithReference(configService)
    .WithReference(rabbitmqService)
    .WithEnvironment("Jwt__SecretKey", jwtSecretKey)
    .WithEnvironment("Jwt__Issuer", jwtIssuer)
    .WithEnvironment("Jwt__Audience", jwtAudience);

var surveyService = builder.AddProject<Projects.CodeSpirit_SurveyApi>("survey")
    .WithReference(seqService)
    .WithReference(cache)
    .WithReference(configService)
    .WithReference(rabbitmqService)
    .WithReference(identityService)
    .WithEnvironment("Jwt__SecretKey", jwtSecretKey)
    .WithEnvironment("Jwt__Issuer", jwtIssuer)
    .WithEnvironment("Jwt__Audience", jwtAudience);

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
    .WithReference(fileService)
    .WithReference(surveyService)
    .WithUrlForEndpoint("https", url =>
    {
        url.DisplayText = "Web 前端";
    })
    .WithUrlForEndpoint("https", ep => new()
    {
        Url = "/health",
        DisplayText = "健康检查",
        DisplayLocation = UrlDisplayLocation.DetailsOnly
    });

// 注册资源初始化事件，需要提供CancellationToken参数
builder.Eventing.Subscribe<InitializeResourceEvent>((eventData, cancellationToken) =>
{
    Console.WriteLine($"资源初始化: {eventData.Resource.Name}");
    return Task.CompletedTask;
});

builder.Build().Run();
