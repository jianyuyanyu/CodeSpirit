using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Aspire.Hosting.Elasticsearch;
using System.Text;
using CodeSpirit.AppHost.Configuration;
using CodeSpirit.AppHost.Extensions;

// 抑制实验性 API 警告 - WithDeploymentImageTag 是 Aspire 9.5 的实验性功能
#pragma warning disable ASPIREEXP001

/// <summary>
/// Aspire应用宿主程序入口点
/// </summary>
/// <remarks>
/// 该程序负责启动和协调整个微服务应用的运行
/// </remarks>

// 设置控制台编码为UTF-8以正确显示中文字符
Console.OutputEncoding = Encoding.UTF8;

var builder = DistributedApplication.CreateBuilder(args);

// === 创建集中的参数管理 ===
var parameters = AppParameters.Create(builder);

// === 基础设施服务 ===

// 添加 Redis 缓存服务
var cache = builder.AddRedis("cache")
                   .WithLifetime(ContainerLifetime.Persistent)
                   .WithHostPort(6380)  // 修改为安全端口范围
                   // .WithDeploymentImageTag(_ => $"redis-7.0-alpine") // Aspire 9.5 实验性功能
                   .WithRedisCommander((op) =>
                   {
                       op
                         .WithLifetime (ContainerLifetime.Persistent)
                         //.WithHttpEndpoint(port: 8082, targetPort: 8081, name: "commander-ui")
                         .WithUrlForEndpoint("commander-ui", url =>
                             url.DisplayLocation = UrlDisplayLocation.SummaryAndDetails);
                   });

// 添加 Seq 日志服务
var seqService = builder.AddSeq("seq")
                    .WithImageTag("2024.3")
                 .WithDataVolume()
                 .WithLifetime(ContainerLifetime.Persistent)
                 // .WithDeploymentImageTag(_ => $"seq-2024.3") // Aspire 9.5 实验性功能
                 //.WithHttpEndpoint(port: 5341, targetPort: 80, name: "seq-ui")
                 .WithUrlForEndpoint("seq", url => url.DisplayLocation = UrlDisplayLocation.SummaryAndDetails)
                 .WithEnvironment("ACCEPT_EULA", "Y")
                 .WithUrlForEndpoint("seq-ui", url =>
                     url.DisplayText = "Seq 日志界面");

// 添加 RabbitMQ 服务
var rabbitmqService = builder.AddRabbitMQ("rabbitmq", parameters.RabbitMq.Username, parameters.RabbitMq.Password)
                     .WithManagementPlugin()
                     .WithLifetime(ContainerLifetime.Persistent)
                     // .WithDeploymentImageTag(_ => $"rabbitmq-3.13-management") // Aspire 9.5 实验性功能
                     .WithUrlForEndpoint("management", url =>
                     {
                         url.DisplayText = "RabbitMQ 管理界面";
                     });

//// 添加 Elasticsearch 服务
//var esPassword = builder.AddParameter("password", "Password123", secret: true);
//var elasticsearchService = builder.AddElasticsearch("elasticsearch", password: esPassword)
//                          .WithLifetime(ContainerLifetime.Persistent)
//                          .WithDataVolume()
//                          //.WithHttpEndpoint(port: 9200, targetPort: 9200, name: "elasticsearch")
//                          //.WithHttpEndpoint(port: 9300, targetPort: 9300, name: "elasticsearch-nodes")
//                          .WithUrlForEndpoint("elasticsearch", ep => new()
//                          {
//                              Url = "/_cluster/health",
//                              DisplayText = "ES 集群健康状态",
//                              DisplayLocation = UrlDisplayLocation.DetailsOnly
//                          });

// 添加 GreptimeDB 服务 - 使用固定端口便于外部访问
var greptimedbService = builder.AddContainer("greptimedb", "greptime/greptimedb", "latest")
                              .WithArgs("standalone", "start", "--http-addr", "0.0.0.0:4000", "--rpc-addr", "0.0.0.0:4001")
                              .WithHttpEndpoint(port: 4000, targetPort: 4000, name: "greptimedb-http")
                              .WithHttpEndpoint(port: 4001, targetPort: 4001, name: "greptimedb-grpc")
                              //.WithHttpEndpoint(port: 4002, targetPort: 4002, name: "greptimedb-mysql")
                              //.WithHttpEndpoint(port: 4003, targetPort: 4003, name: "greptimedb-postgres")
                              .WithLifetime(ContainerLifetime.Persistent)
                              // .WithDeploymentImageTag(_ => $"greptimedb-v0.9.5") // Aspire 9.5 实验性功能
                              .WithEnvironment("GREPTIME_OPTS", "--log-level=info");

// === 数据库配置 ===

// 获取数据库类型配置
var databaseType = builder.Configuration.GetValue<string>("DatabaseType") ?? "MySql";
Console.WriteLine($"使用数据库类型: {databaseType}");

// 数据库资源配置
IResourceBuilder<IResourceWithConnectionString> identityDb, examDb, configDb, settingsDb, messagingDb, fileDb, surveyDb, approvalDb;

if (databaseType.Equals("MySql", StringComparison.OrdinalIgnoreCase))
{
    Console.WriteLine("配置MySQL数据库资源...");

    // 添加MySQL服务器 - 使用默认端口3306
    var mysql = builder.AddMySql("mysql", password: parameters.Database.MySqlPassword!, port: 3306)
                       .WithLifetime(ContainerLifetime.Persistent)
                       .WithDataVolume()
                       // .WithDeploymentImageTag(_ => $"mysql-8.0") // Aspire 9.5 实验性功能
                       .WithPhpMyAdmin();

    // 创建各个数据库
    identityDb = mysql.AddDatabase("identity-api");
    examDb = mysql.AddDatabase("exam-api");
    configDb = mysql.AddDatabase("config-api");
    settingsDb = mysql.AddDatabase("settings");
    messagingDb = mysql.AddDatabase("messaging-api");
    fileDb = mysql.AddDatabase("file-api");
    surveyDb = mysql.AddDatabase("survey-api");
    approvalDb = mysql.AddDatabase("approval-api");
}
else if (databaseType.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
{
    Console.WriteLine("配置SQL Server数据库资源...");

    // 添加SQL Server服务器
    var sqlServer = builder.AddSqlServer("sqlserver", password: parameters.Database.SqlServerPassword!, port: 1433)
                           .WithLifetime(ContainerLifetime.Persistent)
                           .WithDataVolume();
                           // .WithDeploymentImageTag(_ => $"sqlserver-2022-latest"); // Aspire 9.5 实验性功能

    // 创建各个数据库
    identityDb = sqlServer.AddDatabase("identity-api");
    examDb = sqlServer.AddDatabase("exam-api");
    configDb = sqlServer.AddDatabase("config-api");
    settingsDb = sqlServer.AddDatabase("settings");
    messagingDb = sqlServer.AddDatabase("messaging-api");
    fileDb = sqlServer.AddDatabase("file-api");
    surveyDb = sqlServer.AddDatabase("survey-api");
    approvalDb = sqlServer.AddDatabase("approval-api");
}
else
{
    throw new InvalidOperationException($"不支持的数据库类型: {databaseType}");
}

// === 添加 API 服务 ===

// 添加 ConfigCenter 服务（第一个服务，没有依赖其他 API）
var configService = builder.AddProject<Projects.CodeSpirit_ConfigCenter>("config")
    .WithReference(configDb)
    .WithReference(seqService)
    .WithReference(cache)
    .WithEnvironment("DatabaseType", databaseType)
    .WithJwtConfiguration(parameters.Jwt.SecretKey, parameters.Jwt.Issuer, parameters.Jwt.Audience)
    .WithLlmConfiguration(
        parameters.Llm.ApiKey,
        parameters.Llm.ApiBaseUrl,
        parameters.Llm.ModelName,
        parameters.Llm.TimeoutSeconds,
        parameters.Llm.MaxTokens,
        parameters.Llm.UseProxy,
        parameters.Llm.ProxyAddress)
    .WithAiFormFillLlmConfiguration(
        parameters.AiFormFillLlm.ApiKey,
        parameters.AiFormFillLlm.ApiBaseUrl,
        parameters.AiFormFillLlm.ModelName,
        parameters.AiFormFillLlm.DisableThinking,
        parameters.AiFormFillLlm.ResponseFormatType,
        parameters.AiFormFillLlm.Temperature,
        parameters.AiFormFillLlm.TopP,
        parameters.AiFormFillLlm.EnableStreaming)
    .WaitFor(configDb)
    .WithHealthCheck()
    .WithEnvironmentAwareDeploymentTag("config", () => "2.0.0");

// 添加 IdentityService 服务
var identityService = builder.AddStandardApiService<Projects.CodeSpirit_IdentityApi>(
        name: "identity",
        database: identityDb,
        parameters: parameters,
        cache: cache,
        rabbitmqService: rabbitmqService,
        identityService: null!, // 第一个身份服务，传入 null
        databaseType: databaseType)
    .WithReference(seqService)
    .WithReference(configService)
    .WithHealthCheck()
    .WithEnvironmentAwareDeploymentTag("identity", () => "2.1.0");

// 添加消息服务
var messagingService = builder.AddStandardApiService<Projects.CodeSpirit_MessagingApi>(
        name: "messaging",
        database: messagingDb,
        parameters: parameters,
        cache: cache,
        rabbitmqService: rabbitmqService,
        identityService: identityService,
        databaseType: databaseType)
    .WithReference(seqService)
    .WithReference(configService)
    .WithHealthCheck()
    .WithEnvironmentAwareDeploymentTag("messaging", () => "2.0.0");

// 添加考试服务（需要访问设置数据库）
var examService = builder.AddStandardApiService<Projects.CodeSpirit_ExamApi>(
        name: "exam",
        database: examDb,
        parameters: parameters,
        cache: cache,
        rabbitmqService: rabbitmqService,
        identityService: identityService,
        databaseType: databaseType,
        settingsDb: settingsDb)  // 考试服务需要访问设置数据库
    .WithReference(seqService)
    .WithReference(configService)
    .WithHealthCheck()
    .WithEnvironmentAwareDeploymentTag("exam", () => "2.0.0");

// 添加文件存储服务
var fileService = builder.AddStandardApiService<Projects.CodeSpirit_FileStorageApi>(
        name: "file",
        database: fileDb,
        parameters: parameters,
        cache: cache,
        rabbitmqService: rabbitmqService,
        identityService: identityService,
        databaseType: databaseType)
    .WithReference(seqService)
    .WithReference(configService)
    .WithHealthCheck()
    .WithEnvironmentAwareDeploymentTag("file", () => "2.0.0");

// 添加问卷调查服务（需要访问设置数据库）
var surveyService = builder.AddStandardApiService<Projects.CodeSpirit_SurveyApi>(
        name: "survey",
        database: surveyDb,
        parameters: parameters,
        cache: cache,
        rabbitmqService: rabbitmqService,
        identityService: identityService,
        databaseType: databaseType,
        settingsDb: settingsDb)  // 问卷调查服务需要访问设置数据库
    .WithReference(seqService)
    .WithReference(configService)
    .WithHealthCheck()
    .WithEnvironmentAwareDeploymentTag("survey", () => "2.0.0");

// 添加审批服务（需要访问设置数据库）
var approvalService = builder.AddStandardApiService<Projects.CodeSpirit_ApprovalApi>(
        name: "approval",
        database: approvalDb,
        parameters: parameters,
        cache: cache,
        rabbitmqService: rabbitmqService,
        identityService: identityService,
        databaseType: databaseType,
        settingsDb: settingsDb)  // 审批服务需要访问设置数据库
    .WithReference(seqService)
    .WithReference(configService)
    .WithHealthCheck()
    .WithEnvironmentAwareDeploymentTag("approval", () => "2.0.0");

// 添加 Web 前端应用
builder.AddProject<Projects.CodeSpirit_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(cache)
    .WithReference(seqService)
    .WithReference(rabbitmqService)
    .WithReference(identityService)
    .WithReference(configService)
    .WithReference(messagingService)
    .WithReference(examService)
    .WithReference(fileService)
    .WithReference(surveyService)
    .WithReference(approvalService)
    .WithEnvironment("DatabaseType", databaseType)
    .WithAiFormFillLlmConfiguration(
        parameters.AiFormFillLlm.ApiKey,
        parameters.AiFormFillLlm.ApiBaseUrl,
        parameters.AiFormFillLlm.ModelName,
        parameters.AiFormFillLlm.DisableThinking,
        parameters.AiFormFillLlm.ResponseFormatType,
        parameters.AiFormFillLlm.Temperature,
        parameters.AiFormFillLlm.TopP,
        parameters.AiFormFillLlm.EnableStreaming)
    .WithEnvironment("Audit__StorageProvider", "GreptimeDB")
    .WithEnvironment("Audit__GreptimeDB__Url", greptimedbService.GetEndpoint("greptimedb-http"))
    .WithEnvironment("Audit__GreptimeDB__Database", "audit_logs")
    .WithEnvironment("Audit__GreptimeDB__TableName", "audit_logs")
    .WithEnvironment("Audit__GreptimeDB__TablePrefix", "web")
    .WithUrlForEndpoint("https", url =>
    {
        url.DisplayText = "Web 前端";
    })
    .WithHealthCheck()
    .WithEnvironmentAwareDeploymentTag("webfrontend", () => "2.0.0")
    .WaitFor(greptimedbService);

// 注册资源初始化事件，需要提供CancellationToken参数
builder.Eventing.Subscribe<InitializeResourceEvent>((eventData, cancellationToken) =>
{
    Console.WriteLine($"资源初始化: {eventData.Resource.Name}");
    return Task.CompletedTask;
});

Console.WriteLine("审计存储：已配置GreptimeDB作为默认存储提供者");
Console.WriteLine("正在启动应用...");
Console.WriteLine($"数据库类型 {databaseType} 配置完成，正在启动应用...");
builder.Build().Run();
