using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
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

// 获取数据库类型配置
var databaseType = builder.Configuration.GetValue<string>("DatabaseType") ?? "MySql";
Console.WriteLine($"使用数据库类型: {databaseType}");

// 数据库资源配置
IResourceBuilder<IResourceWithConnectionString> identityDb, examDb, configDb, settingsDb, messagingDb, fileDb, surveyDb, approvalDb;

if (databaseType.Equals("MySql", StringComparison.OrdinalIgnoreCase))
{
    Console.WriteLine("配置MySQL数据库资源...");

    // 添加MySQL密码参数
    var mysqlPassword = builder.AddParameter("mysql-password", "Password123", secret: true);

    // 添加MySQL服务器 - 使用默认端口3306
    var mysql = builder.AddMySql("mysql", password: mysqlPassword, port: 3306)
                       .WithLifetime(ContainerLifetime.Persistent)
                       .WithDataVolume()
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
    var sqlServerPassword = builder.AddParameter("sqlserver-password", "P@ssword123456", secret: true);
    var sqlServer = builder.AddSqlServer("sqlserver", password: sqlServerPassword, port: 1433)
                           .WithLifetime(ContainerLifetime.Persistent)
                           .WithDataVolume()
                           ;

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

// 添加统一的JWT配置参数
var jwtSecretKey = builder.AddParameter(name: "jwt-SecretKey", "ECBF8FA013844D77AE041A6800D7FF8F", secret: true);
var jwtIssuer = builder.AddParameter(name: "jwt-Issuer", "codespirit.com");
var jwtAudience = builder.AddParameter(name: "jwt-Audience", "CodeSpirit");

// 添加统一的LLM配置参数
var llmApiKey = builder.AddParameter(name: "llm-ApiKey", secret: true);
var llmApiBaseUrl = builder.AddParameter(name: "llm-ApiBaseUrl", "https://dashscope.aliyuncs.com/compatible-mode/v1");
var llmModelName = builder.AddParameter(name: "llm-ModelName", "qwen-plus");
var llmTimeoutSeconds = builder.AddParameter(name: "llm-TimeoutSeconds", "120");
var llmMaxTokens = builder.AddParameter(name: "llm-MaxTokens", "2048");
var llmUseProxy = builder.AddParameter(name: "llm-UseProxy", "false");
var llmProxyAddress = builder.AddParameter(name: "llm-ProxyAddress", "", secret: false);

// 添加AI表单填充专用LLM配置参数
var aiFormFillLlmApiKey = builder.AddParameter(name: "ai-form-fill-llm-ApiKey", secret: true);
var aiFormFillLlmApiBaseUrl = builder.AddParameter(name: "ai-form-fill-llm-ApiBaseUrl", "https://dashscope.aliyuncs.com/compatible-mode/v1");
var aiFormFillLlmModelName = builder.AddParameter(name: "ai-form-fill-llm-ModelName", "qwen-flash");
var aiFormFillLlmDisableThinking = builder.AddParameter(name: "ai-form-fill-llm-DisableThinking", "true");
var aiFormFillLlmResponseFormatType = builder.AddParameter(name: "ai-form-fill-llm-ResponseFormatType", "json_object");
var aiFormFillLlmTemperature = builder.AddParameter(name: "ai-form-fill-llm-Temperature", "0.1");
var aiFormFillLlmTopP = builder.AddParameter(name: "ai-form-fill-llm-TopP", "0.9");
var aiFormFillLlmEnableStreaming = builder.AddParameter(name: "ai-form-fill-llm-EnableStreaming", "true");

// 添加 ConfigCenter 服务
var configService = builder.AddProject<Projects.CodeSpirit_ConfigCenter>("config")
    .WithReference(configDb)
    .WithReference(seqService)
    .WithReference(cache)
    .WithEnvironment("DatabaseType", databaseType)
    .WithEnvironment("Jwt__SecretKey", jwtSecretKey)
    .WithEnvironment("Jwt__Issuer", jwtIssuer)
    .WithEnvironment("Jwt__Audience", jwtAudience)
    .WithEnvironment("LLM__ApiKey", llmApiKey)
    .WithEnvironment("LLM__ApiBaseUrl", llmApiBaseUrl)
    .WithEnvironment("LLM__ModelName", llmModelName)
    .WithEnvironment("LLM__TimeoutSeconds", llmTimeoutSeconds)
    .WithEnvironment("LLM__MaxTokens", llmMaxTokens)
    .WithEnvironment("LLM__UseProxy", llmUseProxy)
    .WithEnvironment("LLM__ProxyAddress", llmProxyAddress)
    .WithEnvironment("AiFormFillLLM__ApiKey", aiFormFillLlmApiKey)
    .WithEnvironment("AiFormFillLLM__ApiBaseUrl", aiFormFillLlmApiBaseUrl)
    .WithEnvironment("AiFormFillLLM__ModelName", aiFormFillLlmModelName)
    .WithEnvironment("AiFormFillLLM__DisableThinking", aiFormFillLlmDisableThinking)
    .WithEnvironment("AiFormFillLLM__ResponseFormatType", aiFormFillLlmResponseFormatType)
    .WithEnvironment("AiFormFillLLM__Temperature", aiFormFillLlmTemperature)
    .WithEnvironment("AiFormFillLLM__TopP", aiFormFillLlmTopP)
    .WithEnvironment("AiFormFillLLM__EnableStreaming", aiFormFillLlmEnableStreaming)
    .WaitFor(configDb);

var identityService = builder.AddProject<Projects.CodeSpirit_IdentityApi>("identity")
    .WithReference(identityDb)
    .WithReference(seqService)
    .WithReference(cache)
    .WithReference(configService)
    .WithReference(rabbitmqService)
    .WithEnvironment("DatabaseType", databaseType)
    .WithEnvironment("Jwt__SecretKey", jwtSecretKey)
    .WithEnvironment("Jwt__Issuer", jwtIssuer)
    .WithEnvironment("Jwt__Audience", jwtAudience)
    .WithEnvironment("LLM__ApiKey", llmApiKey)
    .WithEnvironment("LLM__ApiBaseUrl", llmApiBaseUrl)
    .WithEnvironment("LLM__ModelName", llmModelName)
    .WithEnvironment("LLM__TimeoutSeconds", llmTimeoutSeconds)
    .WithEnvironment("LLM__MaxTokens", llmMaxTokens)
    .WithEnvironment("LLM__UseProxy", llmUseProxy)
    .WithEnvironment("LLM__ProxyAddress", llmProxyAddress)
    .WithEnvironment("AiFormFillLLM__ApiKey", aiFormFillLlmApiKey)
    .WithEnvironment("AiFormFillLLM__ApiBaseUrl", aiFormFillLlmApiBaseUrl)
    .WithEnvironment("AiFormFillLLM__ModelName", aiFormFillLlmModelName)
    .WithEnvironment("AiFormFillLLM__DisableThinking", aiFormFillLlmDisableThinking)
    .WithEnvironment("AiFormFillLLM__ResponseFormatType", aiFormFillLlmResponseFormatType)
    .WithEnvironment("AiFormFillLLM__Temperature", aiFormFillLlmTemperature)
    .WithEnvironment("AiFormFillLLM__TopP", aiFormFillLlmTopP)
    .WithEnvironment("AiFormFillLLM__EnableStreaming", aiFormFillLlmEnableStreaming)
    .WaitFor(identityDb);

// 添加消息服务
var messagingService = builder.AddProject<Projects.CodeSpirit_MessagingApi>("messaging")
    .WithReference(messagingDb)
    .WithReference(seqService)
    .WithReference(cache)
    .WithReference(configService)
    .WithReference(identityService)
    .WithEnvironment("DatabaseType", databaseType)
    .WithEnvironment("Jwt__SecretKey", jwtSecretKey)
    .WithEnvironment("Jwt__Issuer", jwtIssuer)
    .WithEnvironment("Jwt__Audience", jwtAudience)
    .WithEnvironment("LLM__ApiKey", llmApiKey)
    .WithEnvironment("LLM__ApiBaseUrl", llmApiBaseUrl)
    .WithEnvironment("LLM__ModelName", llmModelName)
    .WithEnvironment("LLM__TimeoutSeconds", llmTimeoutSeconds)
    .WithEnvironment("LLM__MaxTokens", llmMaxTokens)
    .WithEnvironment("LLM__UseProxy", llmUseProxy)
    .WithEnvironment("LLM__ProxyAddress", llmProxyAddress)
    .WithEnvironment("AiFormFillLLM__ApiKey", aiFormFillLlmApiKey)
    .WithEnvironment("AiFormFillLLM__ApiBaseUrl", aiFormFillLlmApiBaseUrl)
    .WithEnvironment("AiFormFillLLM__ModelName", aiFormFillLlmModelName)
    .WithEnvironment("AiFormFillLLM__DisableThinking", aiFormFillLlmDisableThinking)
    .WithEnvironment("AiFormFillLLM__ResponseFormatType", aiFormFillLlmResponseFormatType)
    .WithEnvironment("AiFormFillLLM__Temperature", aiFormFillLlmTemperature)
    .WithEnvironment("AiFormFillLLM__TopP", aiFormFillLlmTopP)
    .WithEnvironment("AiFormFillLLM__EnableStreaming", aiFormFillLlmEnableStreaming)
    .WaitFor(messagingDb);

var examService = builder.AddProject<Projects.CodeSpirit_ExamApi>("exam")
    .WithReference(examDb)
    .WithReference(settingsDb)  // 考试服务需要访问设置数据库
    .WithReference(seqService)
    .WithReference(cache)
    .WithReference(configService)
    .WithReference(rabbitmqService)
    .WithReference(elasticsearchService)
    .WithReference(identityService)
    .WithEnvironment("DatabaseType", databaseType)
    .WithEnvironment("Jwt__SecretKey", jwtSecretKey)
    .WithEnvironment("Jwt__Issuer", jwtIssuer)
    .WithEnvironment("Jwt__Audience", jwtAudience)
    .WithEnvironment("LLM__ApiKey", llmApiKey)
    .WithEnvironment("LLM__ApiBaseUrl", llmApiBaseUrl)
    .WithEnvironment("LLM__ModelName", llmModelName)
    .WithEnvironment("LLM__TimeoutSeconds", llmTimeoutSeconds)
    .WithEnvironment("LLM__MaxTokens", llmMaxTokens)
    .WithEnvironment("LLM__UseProxy", llmUseProxy)
    .WithEnvironment("LLM__ProxyAddress", llmProxyAddress)
    .WithEnvironment("AiFormFillLLM__ApiKey", aiFormFillLlmApiKey)
    .WithEnvironment("AiFormFillLLM__ApiBaseUrl", aiFormFillLlmApiBaseUrl)
    .WithEnvironment("AiFormFillLLM__ModelName", aiFormFillLlmModelName)
    .WithEnvironment("AiFormFillLLM__DisableThinking", aiFormFillLlmDisableThinking)
    .WithEnvironment("AiFormFillLLM__ResponseFormatType", aiFormFillLlmResponseFormatType)
    .WithEnvironment("AiFormFillLLM__Temperature", aiFormFillLlmTemperature)
    .WithEnvironment("AiFormFillLLM__TopP", aiFormFillLlmTopP)
    .WithEnvironment("AiFormFillLLM__EnableStreaming", aiFormFillLlmEnableStreaming)
    .WaitFor(examDb)
    .WaitFor(settingsDb);

var fileService = builder.AddProject<Projects.CodeSpirit_FileStorageApi>("file")
    .WithReference(fileDb)
    .WithReference(seqService)
    .WithReference(cache)
    .WithReference(configService)
    .WithReference(rabbitmqService)
    .WithReference(identityService)
    .WithEnvironment("DatabaseType", databaseType)
    .WithEnvironment("Jwt__SecretKey", jwtSecretKey)
    .WithEnvironment("Jwt__Issuer", jwtIssuer)
    .WithEnvironment("Jwt__Audience", jwtAudience)
    .WithEnvironment("LLM__ApiKey", llmApiKey)
    .WithEnvironment("LLM__ApiBaseUrl", llmApiBaseUrl)
    .WithEnvironment("LLM__ModelName", llmModelName)
    .WithEnvironment("LLM__TimeoutSeconds", llmTimeoutSeconds)
    .WithEnvironment("LLM__MaxTokens", llmMaxTokens)
    .WithEnvironment("LLM__UseProxy", llmUseProxy)
    .WithEnvironment("LLM__ProxyAddress", llmProxyAddress)
    .WithEnvironment("AiFormFillLLM__ApiKey", aiFormFillLlmApiKey)
    .WithEnvironment("AiFormFillLLM__ApiBaseUrl", aiFormFillLlmApiBaseUrl)
    .WithEnvironment("AiFormFillLLM__ModelName", aiFormFillLlmModelName)
    .WithEnvironment("AiFormFillLLM__DisableThinking", aiFormFillLlmDisableThinking)
    .WithEnvironment("AiFormFillLLM__ResponseFormatType", aiFormFillLlmResponseFormatType)
    .WithEnvironment("AiFormFillLLM__Temperature", aiFormFillLlmTemperature)
    .WithEnvironment("AiFormFillLLM__TopP", aiFormFillLlmTopP)
    .WithEnvironment("AiFormFillLLM__EnableStreaming", aiFormFillLlmEnableStreaming)
    .WaitFor(fileDb);

var surveyService = builder.AddProject<Projects.CodeSpirit_SurveyApi>("survey")
    .WithReference(surveyDb)
    .WithReference(settingsDb)
    .WithReference(seqService)
    .WithReference(cache)
    .WithReference(configService)
    .WithReference(rabbitmqService)
    .WithReference(identityService)
    .WithEnvironment("DatabaseType", databaseType)
    .WithEnvironment("Jwt__SecretKey", jwtSecretKey)
    .WithEnvironment("Jwt__Issuer", jwtIssuer)
    .WithEnvironment("Jwt__Audience", jwtAudience)
    .WithEnvironment("LLM__ApiKey", llmApiKey)
    .WithEnvironment("LLM__ApiBaseUrl", llmApiBaseUrl)
    .WithEnvironment("LLM__ModelName", llmModelName)
    .WithEnvironment("LLM__TimeoutSeconds", llmTimeoutSeconds)
    .WithEnvironment("LLM__MaxTokens", llmMaxTokens)
    .WithEnvironment("LLM__UseProxy", llmUseProxy)
    .WithEnvironment("LLM__ProxyAddress", llmProxyAddress)
    .WithEnvironment("AiFormFillLLM__ApiKey", aiFormFillLlmApiKey)
    .WithEnvironment("AiFormFillLLM__ApiBaseUrl", aiFormFillLlmApiBaseUrl)
    .WithEnvironment("AiFormFillLLM__ModelName", aiFormFillLlmModelName)
    .WithEnvironment("AiFormFillLLM__DisableThinking", aiFormFillLlmDisableThinking)
    .WithEnvironment("AiFormFillLLM__ResponseFormatType", aiFormFillLlmResponseFormatType)
    .WithEnvironment("AiFormFillLLM__Temperature", aiFormFillLlmTemperature)
    .WithEnvironment("AiFormFillLLM__TopP", aiFormFillLlmTopP)
    .WithEnvironment("AiFormFillLLM__EnableStreaming", aiFormFillLlmEnableStreaming)
    .WaitFor(surveyDb)
    .WaitFor(settingsDb);

// 添加审批服务
var approvalService = builder.AddProject<Projects.CodeSpirit_ApprovalApi>("approval")
    .WithReference(approvalDb)
    .WithReference(settingsDb)
    .WithReference(seqService)
    .WithReference(cache)
    .WithReference(configService)
    .WithReference(rabbitmqService)
    .WithReference(identityService)
    .WithEnvironment("DatabaseType", databaseType)
    .WithEnvironment("Jwt__SecretKey", jwtSecretKey)
    .WithEnvironment("Jwt__Issuer", jwtIssuer)
    .WithEnvironment("Jwt__Audience", jwtAudience)
    .WithEnvironment("LLM__ApiKey", llmApiKey)
    .WithEnvironment("LLM__ApiBaseUrl", llmApiBaseUrl)
    .WithEnvironment("LLM__ModelName", llmModelName)
    .WithEnvironment("LLM__TimeoutSeconds", llmTimeoutSeconds)
    .WithEnvironment("LLM__MaxTokens", llmMaxTokens)
    .WithEnvironment("LLM__UseProxy", llmUseProxy)
    .WithEnvironment("LLM__ProxyAddress", llmProxyAddress)
    .WithEnvironment("AiFormFillLLM__ApiKey", aiFormFillLlmApiKey)
    .WithEnvironment("AiFormFillLLM__ApiBaseUrl", aiFormFillLlmApiBaseUrl)
    .WithEnvironment("AiFormFillLLM__ModelName", aiFormFillLlmModelName)
    .WithEnvironment("AiFormFillLLM__DisableThinking", aiFormFillLlmDisableThinking)
    .WithEnvironment("AiFormFillLLM__ResponseFormatType", aiFormFillLlmResponseFormatType)
    .WithEnvironment("AiFormFillLLM__Temperature", aiFormFillLlmTemperature)
    .WithEnvironment("AiFormFillLLM__TopP", aiFormFillLlmTopP)
    .WithEnvironment("AiFormFillLLM__EnableStreaming", aiFormFillLlmEnableStreaming)
    .WaitFor(approvalDb)
    .WaitFor(settingsDb);

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
    .WithReference(approvalService)
    .WithEnvironment("DatabaseType", databaseType)
    .WithEnvironment("AiFormFillLLM__ApiKey", aiFormFillLlmApiKey)
    .WithEnvironment("AiFormFillLLM__ApiBaseUrl", aiFormFillLlmApiBaseUrl)
    .WithEnvironment("AiFormFillLLM__ModelName", aiFormFillLlmModelName)
    .WithEnvironment("AiFormFillLLM__DisableThinking", aiFormFillLlmDisableThinking)
    .WithEnvironment("AiFormFillLLM__ResponseFormatType", aiFormFillLlmResponseFormatType)
    .WithEnvironment("AiFormFillLLM__Temperature", aiFormFillLlmTemperature)
    .WithEnvironment("AiFormFillLLM__TopP", aiFormFillLlmTopP)
    .WithEnvironment("AiFormFillLLM__EnableStreaming", aiFormFillLlmEnableStreaming)
    .WithUrlForEndpoint("https", url =>
    {
        url.DisplayText = "Web 前端";
    })
    .WithUrlForEndpoint("https", ep => new()
    {
        Url = "/health",
        DisplayText = "健康检查",
        DisplayLocation = UrlDisplayLocation.DetailsOnly
    })
    .WaitFor(messagingDb);

// 注册资源初始化事件，需要提供CancellationToken参数
builder.Eventing.Subscribe<InitializeResourceEvent>((eventData, cancellationToken) =>
{
    Console.WriteLine($"资源初始化: {eventData.Resource.Name}");
    return Task.CompletedTask;
});

Console.WriteLine($"数据库类型 {databaseType} 配置完成，正在启动应用...");
builder.Build().Run();
