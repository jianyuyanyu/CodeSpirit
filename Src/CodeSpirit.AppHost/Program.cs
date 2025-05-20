using Aspire.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Aspire.Hosting.Elasticsearch;

var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache")
                   .WithLifetime(ContainerLifetime.Persistent)
                   //.WithRedisInsight()
                   //.WithEndpoint(port: 61690, targetPort: 6137, name: "redis")
                   .WithRedisCommander((op) =>
                   {
                       op.WithHttpEndpoint(port: 61689, targetPort: 8081, name: "commander-ui");
                   })
                   //.WithDataVolume(isReadOnly: false)
                   ;

// Add Seq logging service
var seqService = builder.AddSeq("seq")
                 .WithDataVolume()
                 .ExcludeFromManifest()
                 .WithLifetime(ContainerLifetime.Persistent)
                 .WithHttpEndpoint(port: 61688, targetPort: 80, name: "seq-ui")
                 .WithEnvironment("ACCEPT_EULA", "Y");

// 添加 RabbitMQ 服务的用户名和密码参数
var rabbitmqUser = builder.AddParameter("rabbitmq-username", "admin");
var rabbitmqPass = builder.AddParameter("rabbitmq-password", "Password123", secret: true);

// 添加 RabbitMQ 服务
var rabbitmqService = builder.AddRabbitMQ("rabbitmq", rabbitmqUser, rabbitmqPass)
                     .WithManagementPlugin()
                     .WithLifetime(ContainerLifetime.Persistent)
                     //.WithEndpoint(port: 5672, name: "rabbitmq")
                     //.WithHttpEndpoint(port: 20000, targetPort: 15672, name: "rabbitmq-management")
                    ;

// 添加 Elasticsearch 服务
var elasticsearchService = builder.AddElasticsearch("elasticsearch")
                          .WithLifetime(ContainerLifetime.Persistent)
                          .WithDataVolume()
                          .WithHttpEndpoint(port: 61687, targetPort: 9200, name: "elasticsearch")
                          .WithHttpEndpoint(port: 61686, targetPort: 9300, name: "elasticsearch-nodes");

// 添加 ConfigCenter 服务
var configService = builder.AddProject<Projects.CodeSpirit_ConfigCenter>("config")
    .WithReference(seqService)
        .WaitFor(seqService)
    .WithReference(cache)
        .WaitFor(cache)
    //.PublishAsDockerFile()
    ;

var identityService = builder.AddProject<Projects.CodeSpirit_IdentityApi>("identity")
    .WithReference(seqService)
        .WaitFor(seqService)
    .WithReference(cache)
        .WaitFor(cache)
    .WithReference(configService)
        .WaitFor(configService)
    .WithReference(rabbitmqService)
        .WaitFor(rabbitmqService)
    ;

// 添加消息服务
var messagingService = builder.AddProject<Projects.CodeSpirit_MessagingApi>("messaging")
    .WithReference(seqService)
        .WaitFor(seqService)
    .WithReference(cache)
        .WaitFor(cache)
    .WithReference(configService)
        .WaitFor(configService)
    ;

var examService = builder.AddProject<Projects.CodeSpirit_ExamApi>("exam")
    .WithReference(seqService)
        .WaitFor(seqService)
    .WithReference(cache)
        .WaitFor(cache)
    .WithReference(configService)
        .WaitFor(configService)
    .WithReference(rabbitmqService)
        .WaitFor(rabbitmqService)
    .WithReference(elasticsearchService)
        .WaitFor(elasticsearchService)
    ;

builder.AddProject<Projects.CodeSpirit_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(cache)
        .WaitFor(cache)
    .WithReference(seqService)
        .WaitFor(seqService)
    .WithReference(rabbitmqService)
        .WaitFor(rabbitmqService)
    .WithReference(identityService)
        .WaitFor(identityService)
    .WithReference(configService)
        .WaitFor(configService)
    .WithReference(messagingService)
        .WaitFor(messagingService)
    .WithReference(examService)
        .WaitFor(examService)
    .WithReference(elasticsearchService)
        .WaitFor(elasticsearchService)
    ;

builder.Build().Run();
