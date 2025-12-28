# CodeSpirit.Aspire数据库集成实现指南

## 概述

本文档提供CodeSpirit项目中Aspire数据库集成的详细实现步骤和配置示例，帮助开发者快速实施数据库集成方案。

## 实施步骤

### 第一步：更新AppHost项目

#### 1.1 更新项目文件
在`Src/CodeSpirit.AppHost/CodeSpirit.AppHost.csproj`中添加数据库托管集成包：

```xml
<PackageReference Include="Aspire.Hosting.SqlServer" Version="9.4.1" />
<PackageReference Include="Aspire.Hosting.MySql" Version="9.4.1" />
```

#### 1.2 更新配置文件
在`Src/CodeSpirit.AppHost/appsettings.json`中添加数据库类型配置：

```json
{
  "DatabaseType": "MySql",
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

在`Src/CodeSpirit.AppHost/appsettings.Development.json`中：

```json
{
  "DatabaseType": "MySql",
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Aspire.Hosting.Dcp": "Information"
    }
  }
}
```

#### 1.3 更新Program.cs
完整的AppHost配置示例：

```csharp
using Aspire.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Aspire.Hosting.Elasticsearch;
using System.Text;

/// <summary>
/// Aspire应用宿主程序入口点
/// </summary>
/// <remarks>
/// 该程序负责启动和协调整个微服务应用的运行，包含统一的数据库集成方案
/// </remarks>

// 设置控制台编码为UTF-8以正确显示中文字符
Console.OutputEncoding = Encoding.UTF8;

var builder = DistributedApplication.CreateBuilder(args);

// 获取数据库类型配置
var databaseType = builder.Configuration.GetValue<string>("DatabaseType") ?? "MySql";
Console.WriteLine($"使用数据库类型: {databaseType}");

// 添加 Redis 缓存服务
var cache = builder.AddRedis("cache")
                   .WithLifetime(ContainerLifetime.Persistent)
                   .WithHostPort(6380)
                   .WithRedisCommander((op) =>
                   {
                       op.WithUrlForEndpoint("commander-ui", url =>
                           url.DisplayLocation = UrlDisplayLocation.SummaryAndDetails);
                   });

// 添加 Seq 日志服务
var seqService = builder.AddSeq("seq")
                    .WithImageTag("2024.3")
                 .WithDataVolume()
                 .WithLifetime(ContainerLifetime.Persistent)
                 .WithUrlForEndpoint("seq", url => url.DisplayLocation = UrlDisplayLocation.SummaryAndDetails)
                 .WithEnvironment("ACCEPT_EULA", "Y")
                 .WithUrlForEndpoint("seq-ui", url =>
                     url.DisplayText = "Seq 日志界面");

// 添加 RabbitMQ 服务的用户名和密码参数
var rabbitmqUser = builder.AddParameter("rabbitmq-username", "admin");
var rabbitmqPass = builder.AddParameter("rabbitmq-password", "Password123", secret: true);

// 添加 RabbitMQ 服务
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
                          .WithUrlForEndpoint("elasticsearch", ep => new()
                          {
                              Url = "/_cluster/health",
                              DisplayText = "ES 集群健康状态",
                              DisplayLocation = UrlDisplayLocation.DetailsOnly
                          });

// 数据库资源配置
object identityDb, examDb, configDb, settingsDb, messagingDb, fileDb, surveyDb;

if (databaseType.Equals("MySql", StringComparison.OrdinalIgnoreCase))
{
    Console.WriteLine("配置MySQL数据库资源...");
    
    // 添加MySQL服务器
    var mysql = builder.AddMySql("mysql")
                       .WithLifetime(ContainerLifetime.Persistent)
                       .WithDataVolume()
                       .WithEnvironment("MYSQL_ROOT_PASSWORD", "Password123");

    // 创建各个数据库
    identityDb = mysql.AddDatabase("identity-db");
    examDb = mysql.AddDatabase("exam-db");
    configDb = mysql.AddDatabase("config-db");
    settingsDb = mysql.AddDatabase("settings-db");
    messagingDb = mysql.AddDatabase("messaging-db");
    fileDb = mysql.AddDatabase("file-db");
    surveyDb = mysql.AddDatabase("survey-db");
}
else if (databaseType.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
{
    Console.WriteLine("配置SQL Server数据库资源...");
    
    // 添加SQL Server服务器
    var sqlServerPassword = builder.AddParameter("sqlserver-password", "Password123!", secret: true);
    var sqlServer = builder.AddSqlServer("sqlserver", password: sqlServerPassword)
                           .WithLifetime(ContainerLifetime.Persistent)
                           .WithDataVolume();

    // 创建各个数据库
    identityDb = sqlServer.AddDatabase("identity-db");
    examDb = sqlServer.AddDatabase("exam-db");
    configDb = sqlServer.AddDatabase("config-db");
    settingsDb = sqlServer.AddDatabase("settings-db");
    messagingDb = sqlServer.AddDatabase("messaging-db");
    fileDb = sqlServer.AddDatabase("file-db");
    surveyDb = sqlServer.AddDatabase("survey-db");
}
else
{
    throw new InvalidOperationException($"不支持的数据库类型: {databaseType}");
}

// 添加统一的JWT配置参数
var jwtSecretKey = builder.AddParameter(name: "jwt-SecretKey", "ECBF8FA013844D77AE041A6800D7FF8F", secret: true);
var jwtIssuer = builder.AddParameter(name: "jwt-Issuer", "codespirit.com");
var jwtAudience = builder.AddParameter(name: "jwt-Audience", "CodeSpirit");

// 添加 ConfigCenter 服务
var configService = builder.AddProject<Projects.CodeSpirit_ConfigCenter>("config")
    .WithReference(configDb)
    .WithReference(seqService)
    .WithReference(cache)
    .WithEnvironment("DatabaseType", databaseType)
    .WithEnvironment("Jwt__SecretKey", jwtSecretKey)
    .WithEnvironment("Jwt__Issuer", jwtIssuer)
    .WithEnvironment("Jwt__Audience", jwtAudience)
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
    .WaitFor(identityDb);

// 添加消息服务
var messagingService = builder.AddProject<Projects.CodeSpirit_MessagingApi>("messaging")
    .WithReference(messagingDb)
    .WithReference(seqService)
    .WithReference(cache)
    .WithReference(configService)
    .WithEnvironment("DatabaseType", databaseType)
    .WithEnvironment("Jwt__SecretKey", jwtSecretKey)
    .WithEnvironment("Jwt__Issuer", jwtIssuer)
    .WithEnvironment("Jwt__Audience", jwtAudience)
    .WaitFor(messagingDb);

var examService = builder.AddProject<Projects.CodeSpirit_ExamApi>("exam")
    .WithReference(examDb)
    .WithReference(settingsDb)  // 考试服务需要访问设置数据库
    .WithReference(seqService)
    .WithReference(cache)
    .WithReference(configService)
    .WithReference(rabbitmqService)
    .WithReference(elasticsearchService)
    .WithEnvironment("DatabaseType", databaseType)
    .WithEnvironment("Jwt__SecretKey", jwtSecretKey)
    .WithEnvironment("Jwt__Issuer", jwtIssuer)
    .WithEnvironment("Jwt__Audience", jwtAudience)
    .WaitFor(examDb)
    .WaitFor(settingsDb);

var fileService = builder.AddProject<Projects.CodeSpirit_FileStorageApi>("file")
    .WithReference(fileDb)
    .WithReference(seqService)
    .WithReference(cache)
    .WithReference(configService)
    .WithReference(rabbitmqService)
    .WithEnvironment("DatabaseType", databaseType)
    .WithEnvironment("Jwt__SecretKey", jwtSecretKey)
    .WithEnvironment("Jwt__Issuer", jwtIssuer)
    .WithEnvironment("Jwt__Audience", jwtAudience)
    .WaitFor(fileDb);

var surveyService = builder.AddProject<Projects.CodeSpirit_SurveyApi>("survey")
    .WithReference(surveyDb)
    .WithReference(seqService)
    .WithReference(cache)
    .WithReference(configService)
    .WithReference(rabbitmqService)
    .WithReference(identityService)
    .WithEnvironment("DatabaseType", databaseType)
    .WithEnvironment("Jwt__SecretKey", jwtSecretKey)
    .WithEnvironment("Jwt__Issuer", jwtIssuer)
    .WithEnvironment("Jwt__Audience", jwtAudience)
    .WaitFor(surveyDb);

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
    .WithEnvironment("DatabaseType", databaseType)
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

// 注册资源初始化事件
builder.Eventing.Subscribe<InitializeResourceEvent>((eventData, cancellationToken) =>
{
    Console.WriteLine($"资源初始化: {eventData.Resource.Name}");
    return Task.CompletedTask;
});

Console.WriteLine($"数据库类型 {databaseType} 配置完成，正在启动应用...");
builder.Build().Run();
```

### 第二步：更新API服务项目

#### 2.1 添加NuGet包引用
在各个API服务项目（如`CodeSpirit.IdentityApi.csproj`）中添加：

```xml
<PackageReference Include="Aspire.Microsoft.EntityFrameworkCore.SqlServer" Version="9.4.1" />
<PackageReference Include="Aspire.Pomelo.EntityFrameworkCore.MySql" Version="9.4.1" />
```

#### 2.2 更新Program.cs
以`CodeSpirit.IdentityApi`为例：

```csharp
using CodeSpirit.Core.Extensions;
using CodeSpirit.IdentityApi.Data;
using CodeSpirit.Shared.Extensions;

var builder = WebApplication.CreateBuilder(args);

// 配置服务默认设置
builder.AddServiceDefaults();

// 获取数据库类型配置
var databaseType = builder.Configuration.GetValue<string>("DatabaseType") ?? "MySql";
Console.WriteLine($"Identity API 使用数据库类型: {databaseType}");

// 根据数据库类型配置EF集成
if (databaseType.Equals("MySql", StringComparison.OrdinalIgnoreCase))
{
    // 使用MySql集成
    builder.AddMySqlDbContext<IdentityDbContext>("identity-db", configureSettings: settings =>
    {
        settings.DisableHealthChecks = false;
        settings.DisableTracing = false;
        settings.DisableMetrics = false;
        settings.CommandTimeout = 30;
    });
    
    Console.WriteLine("已配置MySql数据库集成");
}
else if (databaseType.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
{
    // 使用SqlServer集成
    builder.AddSqlServerDbContext<IdentityDbContext>("identity-db", configureSettings: settings =>
    {
        settings.DisableHealthChecks = false;
        settings.DisableTracing = false;
        settings.DisableMetrics = false;
        settings.CommandTimeout = 30;
    });
    
    Console.WriteLine("已配置SqlServer数据库集成");
}
else
{
    throw new InvalidOperationException($"不支持的数据库类型: {databaseType}");
}

// 添加其他服务
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 添加CodeSpirit核心服务
builder.Services.AddCodeSpiritCore(builder.Configuration);

var app = builder.Build();

// 配置HTTP请求管道
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// 配置默认端点
app.MapDefaultEndpoints();

Console.WriteLine($"Identity API 启动完成，数据库类型: {databaseType}");
app.Run();
```

#### 2.3 更新配置文件
在各API服务的`appsettings.json`中添加Aspire配置：

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Aspire": {
    "Pomelo": {
      "EntityFrameworkCore": {
        "MySql": {
          "DisableHealthChecks": false,
          "DisableTracing": false,
          "DisableMetrics": false,
          "CommandTimeout": 30
        }
      }
    },
    "Microsoft": {
      "EntityFrameworkCore": {
        "SqlServer": {
          "DisableHealthChecks": false,
          "DisableTracing": false,
          "DisableMetrics": false,
          "CommandTimeout": 30
        }
      }
    }
  }
}
```

### 第三步：更新DbContext配置

#### 3.1 创建数据库无关的基类
创建`Src/CodeSpirit.Shared/Data/AspireDbContext.cs`：

```csharp
using CodeSpirit.Core;
using CodeSpirit.Shared.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CodeSpirit.Shared.Data;

/// <summary>
/// 支持Aspire集成的数据库上下文基类
/// </summary>
/// <remarks>
/// 提供数据库无关的配置，支持MySql和SqlServer的自动适配
/// </remarks>
public abstract class AspireDbContext : MultiTenantDbContext
{
    /// <summary>
    /// 初始化AspireDbContext实例
    /// </summary>
    /// <param name="options">数据库上下文选项</param>
    /// <param name="serviceProvider">服务提供者</param>
    /// <param name="currentUser">当前用户</param>
    /// <param name="httpContextAccessor">HTTP上下文访问器</param>
    protected AspireDbContext(DbContextOptions options, IServiceProvider serviceProvider, 
        ICurrentUser currentUser, IHttpContextAccessor httpContextAccessor)
        : base(options, serviceProvider, currentUser, httpContextAccessor)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // 应用数据库无关的配置
        ApplyBaseConfigurations(modelBuilder);
        
        // 应用数据库特定的配置
        ApplyDatabaseSpecificConfigurations(modelBuilder);
    }

    /// <summary>
    /// 应用数据库特定的配置
    /// </summary>
    /// <param name="modelBuilder">模型构建器</param>
    protected virtual void ApplyDatabaseSpecificConfigurations(ModelBuilder modelBuilder)
    {
        var databaseType = GetDatabaseType();
        
        Console.WriteLine($"正在为 {GetType().Name} 应用 {databaseType} 特定配置");
        
        switch (databaseType)
        {
            case "MySql":
                ApplyMySqlConfigurations(modelBuilder);
                break;
            case "SqlServer":
                ApplySqlServerConfigurations(modelBuilder);
                break;
            default:
                throw new NotSupportedException($"不支持的数据库类型: {databaseType}");
        }
    }

    /// <summary>
    /// 应用MySql特定的配置
    /// </summary>
    /// <param name="modelBuilder">模型构建器</param>
    protected virtual void ApplyMySqlConfigurations(ModelBuilder modelBuilder)
    {
        // MySql特定配置，子类可以重写
        // 例如：日期时间类型配置
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                {
                    property.SetColumnType("datetime(6)");
                }
            }
        }
    }

    /// <summary>
    /// 应用SqlServer特定的配置
    /// </summary>
    /// <param name="modelBuilder">模型构建器</param>
    protected virtual void ApplySqlServerConfigurations(ModelBuilder modelBuilder)
    {
        // SqlServer特定配置，子类可以重写
        // 例如：日期时间类型配置
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                {
                    property.SetColumnType("datetime2");
                }
            }
        }
    }

    /// <summary>
    /// 应用通用配置
    /// </summary>
    /// <param name="modelBuilder">模型构建器</param>
    private void ApplyBaseConfigurations(ModelBuilder modelBuilder)
    {
        // 通用配置逻辑
        // 例如：字符串长度限制、精度配置等
    }

    /// <summary>
    /// 获取当前数据库类型
    /// </summary>
    /// <returns>数据库类型名称</returns>
    private string GetDatabaseType()
    {
        var providerName = Database.ProviderName ?? "";
        
        if (providerName.Contains("MySql", StringComparison.OrdinalIgnoreCase))
        {
            return "MySql";
        }
        else if (providerName.Contains("SqlServer", StringComparison.OrdinalIgnoreCase))
        {
            return "SqlServer";
        }
        
        throw new NotSupportedException($"无法识别的数据库提供程序: {providerName}");
    }
}
```

#### 3.2 更新具体的DbContext实现
以`IdentityDbContext`为例：

```csharp
using CodeSpirit.Core;
using CodeSpirit.IdentityApi.Entities;
using CodeSpirit.Shared.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CodeSpirit.IdentityApi.Data;

/// <summary>
/// 身份认证数据库上下文
/// </summary>
public class IdentityDbContext : AspireDbContext
{
    /// <summary>
    /// 初始化IdentityDbContext实例
    /// </summary>
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options, 
        IServiceProvider serviceProvider, 
        ICurrentUser currentUser, 
        IHttpContextAccessor httpContextAccessor)
        : base(options, serviceProvider, currentUser, httpContextAccessor)
    {
    }

    /// <summary>
    /// 用户实体集合
    /// </summary>
    public DbSet<User> Users { get; set; } = null!;

    /// <summary>
    /// 角色实体集合
    /// </summary>
    public DbSet<Role> Roles { get; set; } = null!;

    /// <summary>
    /// 用户角色关系实体集合
    /// </summary>
    public DbSet<UserRole> UserRoles { get; set; } = null!;

    protected override void ApplyMySqlConfigurations(ModelBuilder modelBuilder)
    {
        base.ApplyMySqlConfigurations(modelBuilder);
        
        // Identity特定的MySql配置
        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(e => e.Email)
                  .HasColumnType("varchar(256)")
                  .HasCharSet("utf8mb4")
                  .HasCollation("utf8mb4_unicode_ci");
                  
            entity.Property(e => e.UserName)
                  .HasColumnType("varchar(128)")
                  .HasCharSet("utf8mb4")
                  .HasCollation("utf8mb4_unicode_ci");
        });
    }

    protected override void ApplySqlServerConfigurations(ModelBuilder modelBuilder)
    {
        base.ApplySqlServerConfigurations(modelBuilder);
        
        // Identity特定的SqlServer配置
        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(e => e.Email)
                  .HasColumnType("nvarchar(256)");
                  
            entity.Property(e => e.UserName)
                  .HasColumnType("nvarchar(128)");
        });
    }
}
```

### 第四步：更新DbContext工厂

#### 4.1 更新设计时工厂
以`IdentityDbContextFactory`为例：

```csharp
using CodeSpirit.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;

namespace CodeSpirit.IdentityApi.Data;

/// <summary>
/// 用于EF Core设计时工具的DbContext工厂
/// </summary>
/// <remarks>
/// 支持根据配置自动选择数据库类型进行迁移
/// </remarks>
public class IdentityDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    public IdentityDbContext CreateDbContext(string[] args)
    {
        // 构建配置
        var configBuilder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"}.json", optional: true)
            .AddEnvironmentVariables();

        var configuration = configBuilder.Build();

        // 获取数据库类型配置
        var databaseType = configuration.GetValue<string>("DatabaseType") ?? "MySql";
        Console.WriteLine($"设计时工厂使用数据库类型: {databaseType}");

        // 创建DbContext选项
        var optionsBuilder = new DbContextOptionsBuilder<IdentityDbContext>();

        if (databaseType.Equals("MySql", StringComparison.OrdinalIgnoreCase))
        {
            var connectionString = configuration.GetConnectionString("identity-api")
                ?? "Server=localhost;Port=3306;Database=codespirit-identity;Uid=root;Pwd=Password123;";
            
            optionsBuilder.UseMySql(connectionString, 
                ServerVersion.AutoDetect(connectionString),
                options => options.MigrationsAssembly("CodeSpirit.IdentityApi"));
                
            Console.WriteLine($"使用MySql连接字符串: {connectionString}");
        }
        else if (databaseType.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
        {
            var connectionString = configuration.GetConnectionString("identity-api")
                ?? "Server=(localdb)\\mssqllocaldb;Database=codespirit-identity;Trusted_Connection=True;MultipleActiveResultSets=true;Packet Size=512";
            
            optionsBuilder.UseSqlServer(connectionString,
                options => options.MigrationsAssembly("CodeSpirit.IdentityApi"));
                
            Console.WriteLine($"使用SqlServer连接字符串: {connectionString}");
        }
        else
        {
            throw new InvalidOperationException($"不支持的数据库类型: {databaseType}");
        }

        // 创建服务提供者和依赖项
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.AddScoped<ICurrentUser, DesignTimeCurrentUser>();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        
        var serviceProvider = services.BuildServiceProvider();

        return new IdentityDbContext(
            optionsBuilder.Options, 
            serviceProvider, 
            serviceProvider.GetRequiredService<ICurrentUser>(),
            serviceProvider.GetRequiredService<IHttpContextAccessor>());
    }
}

/// <summary>
/// 设计时当前用户实现
/// </summary>
internal class DesignTimeCurrentUser : ICurrentUser
{
    public long? Id => 1; // 设计时使用固定用户ID
    public string UserId => "design-time-user";
    public string UserName => "设计时用户";
    public string? Email => "design@codespirit.com";
    public string? DisplayName => "设计时用户";
    public string? TenantId => "default";
    public bool IsAuthenticated => true;
    public bool IsSuperAdmin => true;
    public IEnumerable<string> Roles => new[] { "SuperAdmin" };
    public IEnumerable<string> Permissions => new[] { "*" };
}
```

### 第五步：配置数据库切换

#### 5.1 创建切换脚本
创建`Scripts/switch-database.ps1`：

```powershell
# 数据库切换脚本
param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("MySql", "SqlServer")]
    [string]$DatabaseType,
    
    [Parameter(Mandatory=$false)]
    [switch]$Force
)

Write-Host "正在切换数据库类型到: $DatabaseType" -ForegroundColor Green

# 更新AppHost配置
$appHostConfig = "Src/CodeSpirit.AppHost/appsettings.json"
if (Test-Path $appHostConfig) {
    $config = Get-Content $appHostConfig | ConvertFrom-Json
    $config.DatabaseType = $DatabaseType
    $config | ConvertTo-Json -Depth 10 | Set-Content $appHostConfig
    Write-Host "已更新 $appHostConfig" -ForegroundColor Yellow
}

$appHostDevConfig = "Src/CodeSpirit.AppHost/appsettings.Development.json"
if (Test-Path $appHostDevConfig) {
    $config = Get-Content $appHostDevConfig | ConvertFrom-Json
    $config.DatabaseType = $DatabaseType
    $config | ConvertTo-Json -Depth 10 | Set-Content $appHostDevConfig
    Write-Host "已更新 $appHostDevConfig" -ForegroundColor Yellow
}

# 清理现有容器和卷（如果指定了Force参数）
if ($Force) {
    Write-Host "正在清理现有容器和数据卷..." -ForegroundColor Yellow
    
    # 停止所有相关容器
    docker ps -a --filter "name=mysql" --filter "name=sqlserver" -q | ForEach-Object { docker stop $_ }
    docker ps -a --filter "name=mysql" --filter "name=sqlserver" -q | ForEach-Object { docker rm $_ }
    
    # 删除数据卷
    docker volume ls --filter "name=mysql" --filter "name=sqlserver" -q | ForEach-Object { docker volume rm $_ }
    
    Write-Host "容器和数据卷清理完成" -ForegroundColor Green
}

Write-Host "数据库类型切换完成！" -ForegroundColor Green
Write-Host "请运行以下命令应用更改：" -ForegroundColor Cyan
Write-Host "1. dotnet run --project Src/CodeSpirit.AppHost" -ForegroundColor White
Write-Host "2. 在各API服务中运行EF迁移命令" -ForegroundColor White
```

#### 5.2 创建迁移管理脚本
创建`Scripts/manage-migrations.ps1`：

```powershell
# EF迁移管理脚本
param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("Add", "Update", "Remove", "Reset")]
    [string]$Action,
    
    [Parameter(Mandatory=$false)]
    [string]$MigrationName,
    
    [Parameter(Mandatory=$false)]
    [ValidateSet("MySql", "SqlServer")]
    [string]$DatabaseType = "MySql",
    
    [Parameter(Mandatory=$false)]
    [ValidateSet("Identity", "Exam", "Config", "Settings", "Messaging", "File", "Survey")]
    [string[]]$Services = @("Identity", "Exam", "Config", "Settings", "Messaging", "File", "Survey")
)

# 设置环境变量
$env:DATABASE_TYPE = $DatabaseType

Write-Host "执行EF迁移操作: $Action, 数据库类型: $DatabaseType" -ForegroundColor Green

foreach ($service in $Services) {
    $projectPath = ""
    $contextName = ""
    
    switch ($service) {
        "Identity" { 
            $projectPath = "Src/ApiServices/CodeSpirit.IdentityApi"
            $contextName = "IdentityDbContext"
        }
        "Exam" { 
            $projectPath = "Src/ApiServices/CodeSpirit.ExamApi"
            $contextName = "ExamDbContext"
        }
        "Config" { 
            $projectPath = "Src/ApiServices/CodeSpirit.ConfigCenter"
            $contextName = "ConfigDbContext"
        }
        "Settings" { 
            $projectPath = "Src/Components/CodeSpirit.Settings"
            $contextName = "SettingsDbContext"
        }
        "Messaging" { 
            $projectPath = "Src/ApiServices/CodeSpirit.MessagingApi"
            $contextName = "MessagingDbContext"
        }
        "File" { 
            $projectPath = "Src/ApiServices/CodeSpirit.FileStorageApi"
            $contextName = "FileStorageDbContext"
        }
        "Survey" { 
            $projectPath = "Src/ApiServices/CodeSpirit.SurveyApi"
            $contextName = "SurveyDbContext"
        }
    }
    
    if (-not (Test-Path $projectPath)) {
        Write-Warning "项目路径不存在: $projectPath"
        continue
    }
    
    Write-Host "处理 $service 服务..." -ForegroundColor Yellow
    
    try {
        switch ($Action) {
            "Add" {
                if ([string]::IsNullOrEmpty($MigrationName)) {
                    $MigrationName = "Migration_$DatabaseType_$(Get-Date -Format 'yyyyMMddHHmmss')"
                }
                dotnet ef migrations add $MigrationName --project $projectPath --context $contextName
            }
            "Update" {
                dotnet ef database update --project $projectPath --context $contextName
            }
            "Remove" {
                dotnet ef migrations remove --project $projectPath --context $contextName
            }
            "Reset" {
                # 删除所有迁移并重新创建
                $migrationsPath = "$projectPath/Migrations"
                if (Test-Path $migrationsPath) {
                    Remove-Item $migrationsPath -Recurse -Force
                }
                dotnet ef migrations add "Initial_$DatabaseType" --project $projectPath --context $contextName
                dotnet ef database update --project $projectPath --context $contextName
            }
        }
        
        Write-Host "$service 服务迁移操作完成" -ForegroundColor Green
    }
    catch {
        Write-Error "$service 服务迁移操作失败: $_"
    }
}

Write-Host "所有迁移操作完成！" -ForegroundColor Green
```

## 使用示例

### 切换到MySql
```bash
# 使用PowerShell脚本切换
./Scripts/switch-database.ps1 -DatabaseType MySql -Force

# 或手动设置环境变量
$env:DATABASE_TYPE="MySql"
dotnet run --project Src/CodeSpirit.AppHost
```

### 切换到SqlServer
```bash
# 使用PowerShell脚本切换
./Scripts/switch-database.ps1 -DatabaseType SqlServer -Force

# 或手动设置环境变量
$env:DATABASE_TYPE="SqlServer"
dotnet run --project Src/CodeSpirit.AppHost
```

### 管理EF迁移
```bash
# 为所有服务添加迁移
./Scripts/manage-migrations.ps1 -Action Add -MigrationName "InitialCreate" -DatabaseType MySql

# 更新数据库
./Scripts/manage-migrations.ps1 -Action Update -DatabaseType MySql

# 只为特定服务操作
./Scripts/manage-migrations.ps1 -Action Add -MigrationName "AddNewFeature" -DatabaseType MySql -Services @("Identity", "Exam")
```

## 验证步骤

### 1. 验证MySql模式
1. 确保配置为MySql模式
2. 启动AppHost：`dotnet run --project Src/CodeSpirit.AppHost`
3. 检查容器状态：`docker ps`
4. 验证数据库连接和API功能

### 2. 验证SqlServer模式
1. 切换配置为SqlServer模式
2. 重新启动AppHost
3. 检查容器状态
4. 验证数据库连接和API功能

### 3. 验证切换功能
1. 在两种模式间切换
2. 验证数据持久性
3. 检查应用功能完整性

## 注意事项

1. **数据迁移**: 切换数据库类型时需要重新执行EF迁移
2. **数据备份**: 在生产环境切换前务必备份数据
3. **性能测试**: 不同数据库的性能特征可能不同，需要进行测试
4. **连接池配置**: 根据实际负载调整连接池参数
5. **监控配置**: 确保监控系统适配不同的数据库类型

## 故障排除

### 常见问题
1. **容器启动失败**: 检查端口占用和Docker配置
2. **连接字符串错误**: 验证连接字符串格式和参数
3. **迁移失败**: 检查数据库权限和网络连接
4. **性能问题**: 调整连接池和超时设置

### 调试技巧
1. 启用详细日志记录
2. 使用Docker日志查看容器状态
3. 监控数据库连接和查询性能
4. 检查Aspire仪表板的健康状态

通过以上配置，CodeSpirit项目将获得完整的Aspire数据库集成能力，支持MySql和SqlServer的灵活切换，并提供优秀的开发和运维体验。
