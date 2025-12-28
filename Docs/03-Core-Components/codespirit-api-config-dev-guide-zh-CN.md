# CodeSpirit API配置类开发指南

## 概述

API配置类是 CodeSpirit 统一启动框架的核心组件，负责定义每个API服务的特定配置。本指南详细说明如何设计和实现高质量的API配置类。

## 基础概念

### 配置类层次结构

```
IApiServiceConfiguration (接口)
    ↓
BaseApiConfiguration (抽象基类)
    ↓
YourApiConfiguration (具体实现)
```

### 核心职责

1. **服务标识**: 定义服务名称和连接字符串键
2. **服务注册**: 配置API特有的服务
3. **中间件配置**: 设置API特有的中间件
4. **数据库初始化**: 处理数据库迁移和种子数据

## 配置类设计原则

### 1. 单一职责原则

每个配置类只负责一个API服务的配置：

```csharp
// ✅ 正确 - 只配置考试相关服务
public class ExamApiConfiguration : BaseApiConfiguration
{
    public override string ServiceName => "exam";
    public override string ConnectionStringKey => "exam-api";
    
    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // 只配置考试相关的服务
        services.AddDbContext<ExamDbContext>(...);
        // 考试特有的服务会通过标记接口自动注册
    }
}

// ❌ 错误 - 配置了多个不相关的服务
public class MixedApiConfiguration : BaseApiConfiguration
{
    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // 不应该在一个配置类中混合多个API的配置
        services.AddDbContext<ExamDbContext>(...);
        services.AddDbContext<UserDbContext>(...);
        services.AddDbContext<OrderDbContext>(...);
    }
}
```

### 2. 命名规范

遵循一致的命名模式：

```csharp
// 命名模式: {ApiName}Configuration
public class ExamApiConfiguration : BaseApiConfiguration { }
public class IdentityApiConfiguration : BaseApiConfiguration { }
public class ConfigCenterApiConfiguration : BaseApiConfiguration { }
public class MessagingApiConfiguration : BaseApiConfiguration { }
```

### 3. 文档注释规范

为所有公共成员添加XML文档注释：

```csharp
/// <summary>
/// 考试系统API配置
/// 负责配置考试相关的服务、数据库和中间件
/// </summary>
public class ExamApiConfiguration : BaseApiConfiguration
{
    /// <summary>
    /// 服务名称，用于Aspire服务发现和日志标识
    /// </summary>
    public override string ServiceName => "exam";
    
    /// <summary>
    /// 数据库连接字符串键名，对应appsettings.json中的ConnectionStrings节点
    /// </summary>
    public override string ConnectionStringKey => "exam-api";
    
    /// <summary>
    /// 配置考试系统特有的服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // 实现细节...
    }
}
```

## 服务配置最佳实践

### 1. 数据库配置

#### 标准数据库配置模式

```csharp
public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    // 1. 获取连接字符串
    var connectionString = configuration.GetConnectionString(ConnectionStringKey);
    
    // 2. 验证连接字符串
    if (string.IsNullOrEmpty(connectionString))
    {
        throw new InvalidOperationException($"连接字符串 '{ConnectionStringKey}' 未配置");
    }
    
    // 3. 配置数据库上下文
    services.AddDbContext<ExamDbContext>(options =>
    {
        options.UseSqlServer(connectionString);
        
        // 开发环境启用敏感数据日志
        if (configuration.GetValue<bool>("EnableSensitiveDataLogging", false))
        {
            options.EnableSensitiveDataLogging();
        }
    });
    
    // 4. 注册DbContext基类解析（用于仓储模式）
    services.AddScoped<DbContext>(provider => 
        provider.GetRequiredService<ExamDbContext>());
}
```

#### 多数据库配置

```csharp
public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    // 主数据库
    var mainConnectionString = configuration.GetConnectionString(ConnectionStringKey);
    services.AddDbContext<ExamDbContext>(options =>
    {
        options.UseSqlServer(mainConnectionString);
    });
    
    // 只读数据库（可选）
    var readOnlyConnectionString = configuration.GetConnectionString($"{ConnectionStringKey}-readonly");
    if (!string.IsNullOrEmpty(readOnlyConnectionString))
    {
        services.AddDbContext<ExamReadOnlyDbContext>(options =>
        {
            options.UseSqlServer(readOnlyConnectionString);
        });
    }
    
    // 注册主数据库上下文为默认DbContext
    services.AddScoped<DbContext>(provider => 
        provider.GetRequiredService<ExamDbContext>());
}
```

### 2. 服务注册策略

#### 优先使用标记接口

```csharp
// ✅ 推荐 - 使用标记接口自动注册
public class ExamService : IScopedDependency, IExamService
{
    // 服务实现
}

public class QuestionService : IScopedDependency, IQuestionService
{
    // 服务实现
}

// 配置类中无需手动注册，框架会自动扫描并注册
public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    // 只需要配置数据库和特殊服务
    services.AddDbContext<ExamDbContext>(...);
    
    // 标记接口的服务会自动注册，无需手动添加
    // services.AddScoped<IExamService, ExamService>(); // 不需要
}
```

#### 特殊服务手动注册

```csharp
public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    // 基础配置
    services.AddDbContext<ExamDbContext>(...);
    
    // 需要特殊配置的服务
    services.AddSingleton<IMemoryCache, MemoryCache>();
    
    // 带配置的服务
    services.Configure<ExamSettings>(configuration.GetSection("ExamSettings"));
    
    // 工厂模式服务
    services.AddSingleton<IExamServiceFactory>(provider => 
        new ExamServiceFactory(provider));
    
    // 条件注册
    if (configuration.GetValue<bool>("EnableRedisCache", false))
    {
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis");
        });
    }
}
```

### 3. 第三方服务集成

#### SignalR配置

```csharp
public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    base.ConfigureServices(services, configuration);
    
    // SignalR配置
    services.AddSignalR(options =>
    {
        options.EnableDetailedErrors = configuration.GetValue<bool>("SignalR:EnableDetailedErrors", false);
        options.MaximumReceiveMessageSize = configuration.GetValue<long>("SignalR:MaxMessageSize", 32 * 1024);
    });
    
    // Redis背板（生产环境）
    var redisConnectionString = configuration.GetConnectionString("Redis");
    if (!string.IsNullOrEmpty(redisConnectionString))
    {
        services.AddSignalR().AddStackExchangeRedis(redisConnectionString);
    }
}

public override Task ConfigureMiddlewareAsync(WebApplication app)
{
    // 映射SignalR Hub
    app.MapHub<ExamHub>("/exam-hub");
    app.MapHub<NotificationHub>("/notification-hub");
    
    return Task.CompletedTask;
}
```

#### 消息队列配置

```csharp
public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    base.ConfigureServices(services, configuration);
    
    // RabbitMQ配置
    var rabbitMQSettings = configuration.GetSection("RabbitMQ");
    if (rabbitMQSettings.Exists())
    {
        services.Configure<RabbitMQSettings>(rabbitMQSettings);
        services.AddSingleton<IRabbitMQConnection, RabbitMQConnection>();
        services.AddScoped<IEventBus, RabbitMQEventBus>();
    }
    
    // 事件处理器自动注册
    services.AddEventHandlers(typeof(ExamApiConfiguration).Assembly);
}
```

## 中间件配置最佳实践

### 1. 中间件插入点选择

#### 认证前中间件 (ConfigurePreAuthenticationMiddlewareAsync)

适用于需要在认证之前执行的中间件：

```csharp
public override Task ConfigurePreAuthenticationMiddlewareAsync(WebApplication app)
{
    // 多租户中间件 - 需要在认证前确定租户
    app.UseCodeSpiritMultiTenant();
    
    // 请求日志中间件 - 记录所有请求
    app.UseRequestLogging();
    
    // API版本中间件
    app.UseApiVersioning();
    
    return Task.CompletedTask;
}
```

#### 控制器映射前中间件 (ConfigurePreControllerMiddlewareAsync)

适用于需要在控制器映射之前执行的中间件：

```csharp
public override Task ConfigurePreControllerMiddlewareAsync(WebApplication app)
{
    // 审计日志中间件 - 需要在认证后记录用户操作
    app.UseCodeSpiritAudit();
    
    // 性能监控中间件
    app.UsePerformanceMonitoring();
    
    // 限流中间件
    app.UseRateLimiting();
    
    return Task.CompletedTask;
}
```

#### API特定中间件 (ConfigureMiddlewareAsync)

适用于API特有的中间件：

```csharp
public override Task ConfigureMiddlewareAsync(WebApplication app)
{
    // SignalR Hub映射
    app.MapHub<ExamHub>("/exam-hub");
    
    // 静态文件服务（如果需要）
    if (app.Environment.IsDevelopment())
    {
        app.UseStaticFiles("/exam-files");
    }
    
    // 健康检查端点
    app.MapHealthChecks("/health/exam");
    
    return Task.CompletedTask;
}
```

### 2. 条件中间件配置

```csharp
public override Task ConfigureMiddlewareAsync(WebApplication app)
{
    var configuration = app.Services.GetRequiredService<IConfiguration>();
    
    // 根据配置条件添加中间件
    if (configuration.GetValue<bool>("EnableSwagger", false))
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }
    
    // 根据环境条件添加中间件
    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }
    
    // 根据功能开关添加中间件
    if (configuration.GetValue<bool>("Features:EnableRealTimeNotifications", true))
    {
        app.MapHub<NotificationHub>("/notifications");
    }
    
    return Task.CompletedTask;
}
```

## 数据库初始化最佳实践

### 1. 基础数据库初始化

```csharp
public override async Task InitializeDatabaseAsync(WebApplication app)
{
    // 使用框架提供的通用初始化方法
    await app.InitializeApiDatabaseAsync<ExamDbContext>();
}
```

### 2. 带种子数据的初始化

```csharp
public override async Task InitializeDatabaseAsync(WebApplication app)
{
    await app.InitializeApiDatabaseAsync<ExamDbContext>(async services =>
    {
        // 获取种子数据服务
        var seeder = services.GetRequiredService<ExamSeederService>();
        
        // 执行种子数据操作
        await seeder.SeedAsync();
    });
}
```

### 3. 复杂初始化逻辑

```csharp
public override async Task InitializeDatabaseAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<ExamApiConfiguration>>();
    
    try
    {
        // 1. 数据库迁移
        await app.InitializeApiDatabaseAsync<ExamDbContext>();
        
        // 2. 检查并创建必要的目录
        var fileService = services.GetRequiredService<IFileService>();
        await fileService.EnsureDirectoriesExistAsync();
        
        // 3. 初始化缓存
        var cacheService = services.GetRequiredService<ICacheService>();
        await cacheService.InitializeAsync();
        
        // 4. 启动后台服务
        var backgroundService = services.GetRequiredService<IExamBackgroundService>();
        await backgroundService.StartAsync();
        
        logger.LogInformation("考试系统初始化完成");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "考试系统初始化失败");
        throw;
    }
}
```

### 4. 种子数据服务设计

```csharp
/// <summary>
/// 考试系统种子数据服务
/// </summary>
public class ExamSeederService : IScopedDependency
{
    private readonly ExamDbContext _context;
    private readonly ILogger<ExamSeederService> _logger;
    
    public ExamSeederService(ExamDbContext context, ILogger<ExamSeederService> logger)
    {
        _context = context;
        _logger = logger;
    }
    
    /// <summary>
    /// 执行种子数据操作
    /// </summary>
    public async Task SeedAsync()
    {
        await SeedQuestionTypesAsync();
        await SeedDifficultyLevelsAsync();
        await SeedDefaultCategoriesAsync();
        await SeedAdminUserAsync();
    }
    
    /// <summary>
    /// 种子题目类型数据
    /// </summary>
    private async Task SeedQuestionTypesAsync()
    {
        if (await _context.QuestionTypes.AnyAsync())
        {
            _logger.LogInformation("题目类型数据已存在，跳过种子数据");
            return;
        }
        
        var questionTypes = new[]
        {
            new QuestionType { Name = "单选题", Code = "SINGLE_CHOICE" },
            new QuestionType { Name = "多选题", Code = "MULTIPLE_CHOICE" },
            new QuestionType { Name = "判断题", Code = "TRUE_FALSE" },
            new QuestionType { Name = "填空题", Code = "FILL_BLANK" },
            new QuestionType { Name = "简答题", Code = "SHORT_ANSWER" }
        };
        
        _context.QuestionTypes.AddRange(questionTypes);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("已添加 {Count} 个题目类型", questionTypes.Length);
    }
    
    // ... 其他种子数据方法
}
```

## 配置验证和错误处理

### 1. 配置验证

```csharp
public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    // 验证必要的配置
    ValidateConfiguration(configuration);
    
    // 配置服务
    var connectionString = configuration.GetConnectionString(ConnectionStringKey);
    services.AddDbContext<ExamDbContext>(options =>
    {
        options.UseSqlServer(connectionString);
    });
}

/// <summary>
/// 验证配置的完整性
/// </summary>
/// <param name="configuration">配置对象</param>
/// <exception cref="InvalidOperationException">配置无效时抛出</exception>
private void ValidateConfiguration(IConfiguration configuration)
{
    var errors = new List<string>();
    
    // 验证连接字符串
    var connectionString = configuration.GetConnectionString(ConnectionStringKey);
    if (string.IsNullOrEmpty(connectionString))
    {
        errors.Add($"连接字符串 '{ConnectionStringKey}' 未配置");
    }
    
    // 验证JWT配置
    var jwtKey = configuration["Jwt:Key"];
    if (string.IsNullOrEmpty(jwtKey))
    {
        errors.Add("JWT密钥未配置");
    }
    
    // 验证必要的配置节
    var examSettings = configuration.GetSection("ExamSettings");
    if (!examSettings.Exists())
    {
        errors.Add("ExamSettings配置节未找到");
    }
    
    if (errors.Any())
    {
        throw new InvalidOperationException($"配置验证失败:\n{string.Join("\n", errors)}");
    }
}
```

### 2. 启动错误处理

```csharp
public override async Task InitializeDatabaseAsync(WebApplication app)
{
    var logger = app.Services.GetRequiredService<ILogger<ExamApiConfiguration>>();
    
    try
    {
        await app.InitializeApiDatabaseAsync<ExamDbContext>(async services =>
        {
            var seeder = services.GetRequiredService<ExamSeederService>();
            await seeder.SeedAsync();
        });
        
        logger.LogInformation("考试系统数据库初始化成功");
    }
    catch (SqlException ex)
    {
        logger.LogError(ex, "数据库连接失败: {Message}", ex.Message);
        throw new InvalidOperationException("无法连接到数据库，请检查连接字符串配置", ex);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "数据库初始化过程中发生未知错误");
        throw;
    }
}
```

## 性能优化

### 1. 服务注册优化

```csharp
public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    // 基础配置
    var connectionString = configuration.GetConnectionString(ConnectionStringKey);
    
    // 数据库连接池配置
    services.AddDbContextPool<ExamDbContext>(options =>
    {
        options.UseSqlServer(connectionString, sqlOptions =>
        {
            sqlOptions.CommandTimeout(30);
            sqlOptions.EnableRetryOnFailure(3);
        });
    }, poolSize: 128);
    
    // 内存缓存配置
    services.AddMemoryCache(options =>
    {
        options.SizeLimit = configuration.GetValue<long>("Cache:SizeLimit", 100);
        options.CompactionPercentage = 0.25;
    });
    
    // HTTP客户端配置
    services.AddHttpClient<IExternalApiService, ExternalApiService>(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(30);
        client.DefaultRequestHeaders.Add("User-Agent", "CodeSpirit-ExamApi/1.0");
    });
}
```

### 2. 启动性能优化

```csharp
public override async Task InitializeDatabaseAsync(WebApplication app)
{
    // 并行执行独立的初始化任务
    var initializationTasks = new List<Task>
    {
        InitializeDatabaseAsync(app),
        PrewarmCacheAsync(app.Services),
        InitializeBackgroundServicesAsync(app.Services)
    };
    
    await Task.WhenAll(initializationTasks);
}

/// <summary>
/// 预热缓存
/// </summary>
private async Task PrewarmCacheAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();
    
    // 预加载常用数据
    await cacheService.PreloadQuestionTypesAsync();
    await cacheService.PreloadDifficultyLevelsAsync();
}

/// <summary>
/// 初始化后台服务
/// </summary>
private async Task InitializeBackgroundServicesAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var backgroundServices = scope.ServiceProvider.GetServices<IBackgroundService>();
    
    var startTasks = backgroundServices.Select(service => service.StartAsync(CancellationToken.None));
    await Task.WhenAll(startTasks);
}
```

## 测试支持

### 1. 可测试的配置设计

```csharp
/// <summary>
/// 考试API配置 - 支持依赖注入测试
/// </summary>
public class ExamApiConfiguration : BaseApiConfiguration
{
    private readonly IConfiguration? _testConfiguration;
    
    /// <summary>
    /// 默认构造函数
    /// </summary>
    public ExamApiConfiguration() { }
    
    /// <summary>
    /// 测试构造函数
    /// </summary>
    /// <param name="testConfiguration">测试配置</param>
    public ExamApiConfiguration(IConfiguration testConfiguration)
    {
        _testConfiguration = testConfiguration;
    }
    
    public override string ServiceName => "exam";
    public override string ConnectionStringKey => "exam-api";
    
    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        var config = _testConfiguration ?? configuration;
        
        // 使用提供的配置进行服务注册
        var connectionString = config.GetConnectionString(ConnectionStringKey);
        services.AddDbContext<ExamDbContext>(options =>
        {
            if (config.GetValue<bool>("UseInMemoryDatabase", false))
            {
                options.UseInMemoryDatabase("ExamTestDb");
            }
            else
            {
                options.UseSqlServer(connectionString);
            }
        });
    }
}
```

### 2. 单元测试示例

```csharp
[TestFixture]
public class ExamApiConfigurationTests
{
    [Test]
    public void ServiceName_ShouldReturnCorrectValue()
    {
        // Arrange
        var config = new ExamApiConfiguration();
        
        // Act
        var serviceName = config.ServiceName;
        
        // Assert
        Assert.AreEqual("exam", serviceName);
    }
    
    [Test]
    public void ConfigureServices_WithValidConfiguration_ShouldRegisterServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string>("ConnectionStrings:exam-api", "TestConnectionString"),
                new KeyValuePair<string, string>("UseInMemoryDatabase", "true")
            })
            .Build();
        
        var config = new ExamApiConfiguration();
        
        // Act
        config.ConfigureServices(services, configuration);
        var serviceProvider = services.BuildServiceProvider();
        
        // Assert
        var dbContext = serviceProvider.GetService<ExamDbContext>();
        Assert.IsNotNull(dbContext);
    }
    
    [Test]
    public void ConfigureServices_WithMissingConnectionString_ShouldThrowException()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();
        var config = new ExamApiConfiguration();
        
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => 
            config.ConfigureServices(services, configuration));
    }
}
```

## 常见模式和示例

### 1. 微服务配置模式

```csharp
/// <summary>
/// 微服务API配置基类
/// </summary>
public abstract class MicroserviceApiConfiguration : BaseApiConfiguration
{
    /// <summary>
    /// 服务发现配置
    /// </summary>
    protected virtual void ConfigureServiceDiscovery(IServiceCollection services, IConfiguration configuration)
    {
        services.AddConsul(configuration.GetSection("Consul"));
    }
    
    /// <summary>
    /// 分布式追踪配置
    /// </summary>
    protected virtual void ConfigureTracing(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOpenTelemetry()
            .WithTracing(builder => builder
                .AddAspNetCoreInstrumentation()
                .AddEntityFrameworkCoreInstrumentation()
                .AddJaegerExporter());
    }
    
    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        base.ConfigureServices(services, configuration);
        
        ConfigureServiceDiscovery(services, configuration);
        ConfigureTracing(services, configuration);
    }
}

/// <summary>
/// 考试微服务配置
/// </summary>
public class ExamMicroserviceConfiguration : MicroserviceApiConfiguration
{
    public override string ServiceName => "exam-microservice";
    public override string ConnectionStringKey => "exam-api";
    
    // 其他特定配置...
}
```

### 2. 多租户配置模式

```csharp
/// <summary>
/// 多租户API配置
/// </summary>
public class MultiTenantExamApiConfiguration : BaseApiConfiguration
{
    public override string ServiceName => "exam";
    public override string ConnectionStringKey => "exam-api";
    
    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // 多租户数据库配置
        services.AddDbContext<ExamDbContext>((serviceProvider, options) =>
        {
            var tenantService = serviceProvider.GetRequiredService<ITenantService>();
            var connectionString = tenantService.GetConnectionString(ConnectionStringKey);
            options.UseSqlServer(connectionString);
        });
        
        // 多租户服务
        services.AddScoped<ITenantService, TenantService>();
        services.AddScoped<ITenantResolver, TenantResolver>();
    }
    
    public override Task ConfigurePreAuthenticationMiddlewareAsync(WebApplication app)
    {
        // 多租户中间件必须在认证前配置
        app.UseCodeSpiritMultiTenant();
        return Task.CompletedTask;
    }
}
```

## 总结

API配置类是 CodeSpirit 统一启动框架的核心，通过遵循本指南中的最佳实践，您可以：

1. **设计清晰的配置结构**，提高代码可读性和可维护性
2. **实现高效的服务注册**，优化应用性能
3. **配置合适的中间件**，满足特定业务需求
4. **处理复杂的初始化逻辑**，确保应用稳定启动
5. **支持全面的测试**，提高代码质量

记住，好的配置类应该是简洁、可测试、可扩展的，同时遵循单一职责原则和依赖注入最佳实践。
