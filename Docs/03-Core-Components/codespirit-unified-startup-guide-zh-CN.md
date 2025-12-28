# CodeSpirit 统一启动框架使用指南

## 概述

本指南详细说明如何使用 CodeSpirit 统一启动框架来创建新的API项目，以及如何将现有的API项目迁移到统一启动框架。

## 快速开始

### 创建新的API项目

#### 1. 创建项目结构

```bash
# 创建新的API项目
dotnet new webapi -n CodeSpirit.YourApi
cd CodeSpirit.YourApi

# 添加必要的项目引用
dotnet add reference ../CodeSpirit.Shared/CodeSpirit.Shared.csproj
dotnet add reference ../CodeSpirit.ServiceDefaults/CodeSpirit.ServiceDefaults.csproj
```

#### 2. 创建API配置类

在项目中创建 `Configuration` 文件夹，并添加配置类：

```csharp
// Configuration/YourApiConfiguration.cs
using CodeSpirit.Shared.Startup;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CodeSpirit.YourApi.Configuration;

/// <summary>
/// YourApi 服务配置
/// </summary>
public class YourApiConfiguration : BaseApiConfiguration
{
    /// <summary>
    /// 服务名称，用于Aspire服务发现
    /// </summary>
    public override string ServiceName => "your-api";
    
    /// <summary>
    /// 数据库连接字符串键名
    /// </summary>
    public override string ConnectionStringKey => "your-api";
    
    /// <summary>
    /// 配置特定服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // 配置数据库上下文
        var connectionString = configuration.GetConnectionString(ConnectionStringKey);
        services.AddDbContext<YourDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
        });
        
        // 注册DbContext基类解析
        services.AddScoped<DbContext>(provider => 
            provider.GetRequiredService<YourDbContext>());
        
        // 注册特定服务（使用标记接口自动注册）
        // services.AddScoped<IYourService, YourService>();
        
        // 可选：添加特定功能
        // services.AddSignalR();
        // services.AddRedisDistributedLock();
    }
    
    /// <summary>
    /// 配置特定中间件
    /// </summary>
    /// <param name="app">应用程序构建器</param>
    /// <returns>异步任务</returns>
    public override Task ConfigureMiddlewareAsync(WebApplication app)
    {
        // 可选：添加特定中间件
        // app.MapHub<YourHub>("/your-hub");
        
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// 数据库初始化
    /// </summary>
    /// <param name="app">应用程序构建器</param>
    /// <returns>异步任务</returns>
    public override async Task InitializeDatabaseAsync(WebApplication app)
    {
        // 使用通用数据库初始化方法
        await app.InitializeApiDatabaseAsync<YourDbContext>(async services =>
        {
            // 可选：执行种子数据操作
            // var seeder = services.GetRequiredService<YourSeederService>();
            // await seeder.SeedAsync();
        });
    }
}
```

#### 3. 更新Program.cs

```csharp
// Program.cs
using CodeSpirit.Shared.Startup;
using CodeSpirit.YourApi.Configuration;
using System.Text;

Console.OutputEncoding = Encoding.UTF8;

var builder = WebApplication.CreateBuilder(args);

// 使用统一的API启动框架
builder.AddCodeSpiritApi<YourApiConfiguration>();

var app = builder.Build();

try
{
    // 使用统一的API配置
    await app.UseCodeSpiritApiAsync<YourApiConfiguration>();
    app.Run();
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "应用程序启动失败");
    Console.WriteLine($"应用程序启动失败: {ex.Message}");
}
```

#### 4. 配置连接字符串

在 `appsettings.json` 和 `appsettings.Development.json` 中添加连接字符串：

```json
{
  "ConnectionStrings": {
    "your-api": "Server=(localdb)\\mssqllocaldb;Database=CodeSpirit.YourApi;Trusted_Connection=true;MultipleActiveResultSets=true"
  }
}
```

## 现有项目迁移

### 迁移步骤

#### 1. 分析现有配置

首先分析现有项目的 `ServiceCollectionExtensions.cs` 文件，识别：
- 特定的服务注册
- 数据库配置
- 中间件配置
- 初始化逻辑

#### 2. 创建配置类

基于分析结果创建新的配置类：

```csharp
// 迁移前的代码示例 (ServiceCollectionExtensions.cs)
public static IServiceCollection AddExam(this WebApplicationBuilder builder)
{
    builder.AddServiceDefaults("exam");
    builder.Services.AddSystemServices(builder.Configuration, typeof(Program), builder.Environment);
    
    // 数据库配置
    var connectionString = builder.Configuration.GetConnectionString("exam-api");
    builder.Services.AddDbContext<ExamDbContext>(options =>
    {
        options.UseSqlServer(connectionString);
    });
    
    // 特定服务
    builder.Services.AddScoped<IExamService, ExamService>();
    builder.Services.AddScoped<IQuestionService, QuestionService>();
    
    return builder.Services;
}

// 迁移后的配置类
public class ExamApiConfiguration : BaseApiConfiguration
{
    public override string ServiceName => "exam";
    public override string ConnectionStringKey => "exam-api";
    
    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // 数据库配置
        var connectionString = configuration.GetConnectionString(ConnectionStringKey);
        services.AddDbContext<ExamDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
        });
        
        services.AddScoped<DbContext>(provider => 
            provider.GetRequiredService<ExamDbContext>());
        
        // 特定服务（如果使用标记接口，可以省略手动注册）
        // services.AddScoped<IExamService, ExamService>();
        // services.AddScoped<IQuestionService, QuestionService>();
    }
    
    public override async Task InitializeDatabaseAsync(WebApplication app)
    {
        await app.InitializeApiDatabaseAsync<ExamDbContext>();
    }
}
```

#### 3. 更新Program.cs

```csharp
// 迁移前
var builder = WebApplication.CreateBuilder(args);
builder.AddExam();

var app = builder.Build();
await app.UseExamApiServicesAsync();
app.Run();

// 迁移后
var builder = WebApplication.CreateBuilder(args);
builder.AddCodeSpiritApi<ExamApiConfiguration>();

var app = builder.Build();
await app.UseCodeSpiritApiAsync<ExamApiConfiguration>();
app.Run();
```

#### 4. 清理过时代码

迁移完成后，可以删除或标记过时的扩展方法：

```csharp
[Obsolete("请使用 ExamApiConfiguration 配置类")]
public static IServiceCollection AddExam(this WebApplicationBuilder builder)
{
    // 保留用于向后兼容，或直接删除
}
```

## 高级配置

### 中间件插入点使用

#### 多租户中间件配置

```csharp
public override Task ConfigurePreAuthenticationMiddlewareAsync(WebApplication app)
{
    // 在认证前添加多租户中间件
    app.UseCodeSpiritMultiTenant();
    return Task.CompletedTask;
}
```

#### 审计日志中间件配置

```csharp
public override Task ConfigurePreControllerMiddlewareAsync(WebApplication app)
{
    // 在控制器映射前添加审计中间件
    app.UseCodeSpiritAudit();
    return Task.CompletedTask;
}
```

#### SignalR Hub配置

```csharp
public override Task ConfigureMiddlewareAsync(WebApplication app)
{
    // 添加SignalR Hub
    app.MapHub<NotificationHub>("/notification-hub");
    app.MapHub<ChatHub>("/chat-hub");
    
    return Task.CompletedTask;
}
```

### 复杂服务配置

#### Redis缓存配置

```csharp
public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    // 基础数据库配置
    base.ConfigureServices(services, configuration);
    
    // Redis配置
    services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = configuration.GetConnectionString("Redis");
    });
    
    // 分布式锁
    services.AddRedisDistributedLock(configuration.GetConnectionString("Redis"));
}
```

#### 消息队列配置

```csharp
public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    base.ConfigureServices(services, configuration);
    
    // RabbitMQ配置
    services.AddRabbitMQ(configuration);
    
    // 事件总线
    services.AddEventBus(configuration);
}
```

### 数据库种子数据

#### 简单种子数据

```csharp
public override async Task InitializeDatabaseAsync(WebApplication app)
{
    await app.InitializeApiDatabaseAsync<YourDbContext>(async services =>
    {
        var context = services.GetRequiredService<YourDbContext>();
        
        // 检查是否已有数据
        if (!await context.YourEntities.AnyAsync())
        {
            // 添加种子数据
            context.YourEntities.AddRange(
                new YourEntity { Name = "示例1" },
                new YourEntity { Name = "示例2" }
            );
            
            await context.SaveChangesAsync();
        }
    });
}
```

#### 复杂种子数据服务

```csharp
public override async Task InitializeDatabaseAsync(WebApplication app)
{
    await app.InitializeApiDatabaseAsync<YourDbContext>(async services =>
    {
        var seeder = services.GetRequiredService<YourSeederService>();
        await seeder.SeedAsync();
    });
}

// 种子数据服务
public class YourSeederService : IScopedDependency
{
    private readonly YourDbContext _context;
    private readonly ILogger<YourSeederService> _logger;
    
    public YourSeederService(YourDbContext context, ILogger<YourSeederService> logger)
    {
        _context = context;
        _logger = logger;
    }
    
    public async Task SeedAsync()
    {
        await SeedRolesAsync();
        await SeedUsersAsync();
        await SeedPermissionsAsync();
    }
    
    private async Task SeedRolesAsync()
    {
        // 角色种子数据逻辑
    }
    
    // ... 其他种子数据方法
}
```

## 依赖注入最佳实践

### 使用标记接口

推荐使用标记接口进行自动服务注册：

```csharp
// 服务接口
public interface IExamService
{
    Task<ExamDto> GetExamAsync(long id);
    Task<PagedResult<ExamDto>> GetExamsAsync(ExamQueryDto query);
}

// 服务实现 - 使用标记接口自动注册
public class ExamService : IScopedDependency, IExamService
{
    private readonly IRepository<Exam> _examRepository;
    private readonly IMapper _mapper;
    
    public ExamService(IRepository<Exam> examRepository, IMapper mapper)
    {
        _examRepository = examRepository;
        _mapper = mapper;
    }
    
    public async Task<ExamDto> GetExamAsync(long id)
    {
        var exam = await _examRepository.GetByIdAsync(id);
        return _mapper.Map<ExamDto>(exam);
    }
    
    // ... 其他方法实现
}
```

### 生命周期选择

- **IScopedDependency**: 适用于大多数业务服务、仓储、数据库上下文
- **ITransientDependency**: 适用于轻量级、无状态的工具类
- **ISingletonDependency**: 适用于缓存服务、配置服务、重量级资源

```csharp
// 缓存服务 - 单例
public class CacheService : ISingletonDependency, ICacheService
{
    // 缓存逻辑
}

// 工具类 - 瞬态
public class PasswordHasher : ITransientDependency, IPasswordHasher
{
    // 密码哈希逻辑
}

// 业务服务 - 作用域
public class UserService : IScopedDependency, IUserService
{
    // 用户业务逻辑
}
```

## 配置文件管理

### 环境特定配置

```json
// appsettings.json - 基础配置
{
  "ConnectionStrings": {
    "your-api": "Server=prod-server;Database=YourApi;..."
  },
  "Jwt": {
    "Key": "your-jwt-key",
    "Issuer": "CodeSpirit",
    "Audience": "CodeSpirit.YourApi"
  }
}

// appsettings.Development.json - 开发环境配置
{
  "ConnectionStrings": {
    "your-api": "Server=(localdb)\\mssqllocaldb;Database=CodeSpirit.YourApi.Dev;Trusted_Connection=true;MultipleActiveResultSets=true"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

### 敏感信息管理

使用用户机密或环境变量管理敏感信息：

```bash
# 设置用户机密
dotnet user-secrets set "ConnectionStrings:your-api" "Server=...;Password=secret;"
dotnet user-secrets set "Jwt:Key" "your-secret-jwt-key"
```

## 测试配置

### 单元测试配置

```csharp
[Test]
public void YourApiConfiguration_ServiceName_ShouldBeCorrect()
{
    // Arrange
    var config = new YourApiConfiguration();
    
    // Act & Assert
    Assert.AreEqual("your-api", config.ServiceName);
}

[Test]
public void YourApiConfiguration_ConnectionStringKey_ShouldBeCorrect()
{
    // Arrange
    var config = new YourApiConfiguration();
    
    // Act & Assert
    Assert.AreEqual("your-api", config.ConnectionStringKey);
}
```

### 集成测试配置

```csharp
public class YourApiIntegrationTests
{
    private WebApplication _app;
    
    [SetUp]
    public async Task Setup()
    {
        var builder = WebApplication.CreateBuilder();
        
        // 使用测试配置
        builder.Configuration.AddInMemoryCollection(new[]
        {
            new KeyValuePair<string, string>("ConnectionStrings:your-api", 
                "Server=(localdb)\\mssqllocaldb;Database=YourApi.Test;Trusted_Connection=true")
        });
        
        builder.AddCodeSpiritApi<YourApiConfiguration>();
        
        _app = builder.Build();
        await _app.UseCodeSpiritApiAsync<YourApiConfiguration>();
    }
    
    [Test]
    public void Api_Should_StartSuccessfully()
    {
        // 验证API能够成功启动
        Assert.IsNotNull(_app);
        
        // 验证服务注册
        var yourService = _app.Services.GetService<IYourService>();
        Assert.IsNotNull(yourService);
    }
    
    [TearDown]
    public async Task TearDown()
    {
        if (_app != null)
        {
            await _app.DisposeAsync();
        }
    }
}
```

## 故障排除

### 常见问题

#### 1. 服务注册失败

**问题**: 服务无法解析，出现 `Unable to resolve service` 错误。

**解决方案**:
- 检查服务类是否实现了标记接口
- 确认接口和实现类在正确的程序集中
- 验证程序集扫描配置

```csharp
// 确保服务实现了标记接口
public class YourService : IScopedDependency, IYourService
{
    // 实现
}
```

#### 2. 数据库连接失败

**问题**: 数据库连接字符串错误或数据库不存在。

**解决方案**:
- 检查连接字符串配置
- 确认数据库服务器可访问
- 验证数据库权限

```csharp
// 在配置中添加连接字符串验证
public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    var connectionString = configuration.GetConnectionString(ConnectionStringKey);
    if (string.IsNullOrEmpty(connectionString))
    {
        throw new InvalidOperationException($"连接字符串 '{ConnectionStringKey}' 未配置");
    }
    
    // 配置数据库上下文
    services.AddDbContext<YourDbContext>(options =>
    {
        options.UseSqlServer(connectionString);
    });
}
```

#### 3. 中间件顺序问题

**问题**: 中间件执行顺序不正确导致功能异常。

**解决方案**:
- 理解中间件执行顺序
- 使用正确的插入点
- 避免在错误的位置添加中间件

```csharp
// 正确的中间件配置
public override Task ConfigurePreAuthenticationMiddlewareAsync(WebApplication app)
{
    // 需要在认证前执行的中间件
    app.UseCodeSpiritMultiTenant();
    return Task.CompletedTask;
}

public override Task ConfigureMiddlewareAsync(WebApplication app)
{
    // API特定的中间件
    app.MapHub<YourHub>("/your-hub");
    return Task.CompletedTask;
}
```

### 调试技巧

#### 1. 启用详细日志

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "CodeSpirit": "Trace",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  }
}
```

#### 2. 服务注册验证

```csharp
public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    base.ConfigureServices(services, configuration);
    
    // 验证关键服务是否注册
    var serviceProvider = services.BuildServiceProvider();
    var yourService = serviceProvider.GetService<IYourService>();
    if (yourService == null)
    {
        throw new InvalidOperationException("IYourService 未正确注册");
    }
}
```

## 性能优化

### 启动性能优化

1. **延迟初始化**: 对于非关键服务使用延迟初始化
2. **并行初始化**: 对于独立的初始化任务使用并行执行
3. **缓存预热**: 在启动时预热关键缓存

```csharp
public override async Task InitializeDatabaseAsync(WebApplication app)
{
    // 并行执行数据库初始化和缓存预热
    var tasks = new List<Task>
    {
        app.InitializeApiDatabaseAsync<YourDbContext>(),
        PrewarmCacheAsync(app.Services),
        InitializeBackgroundServicesAsync(app.Services)
    };
    
    await Task.WhenAll(tasks);
}

private async Task PrewarmCacheAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();
    await cacheService.PrewarmAsync();
}
```

### 内存优化

1. **服务生命周期**: 正确选择服务生命周期
2. **资源释放**: 确保实现 `IDisposable` 的服务正确释放
3. **对象池**: 对于频繁创建的对象使用对象池

## 总结

CodeSpirit 统一启动框架提供了一套完整的API项目启动解决方案。通过遵循本指南中的最佳实践，您可以：

1. **快速创建新的API项目**，减少重复配置工作
2. **平滑迁移现有项目**，保持功能完整性
3. **灵活配置中间件**，满足特定需求
4. **高效管理依赖注入**，提高代码质量
5. **优化应用性能**，提升用户体验

该框架的设计理念是简化开发流程，提高代码一致性，同时保持足够的灵活性来满足各种业务需求。
