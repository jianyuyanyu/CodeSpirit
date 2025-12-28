# CodeSpirit 统一启动框架迁移指南

## 概述

本指南详细说明如何将现有的API项目从传统的启动方式迁移到 CodeSpirit 统一启动框架。迁移过程设计为渐进式、安全的，确保在迁移过程中不影响现有功能。

## 迁移前准备

### 1. 评估现有项目

在开始迁移之前，需要对现有项目进行全面评估：

#### 分析现有启动代码

```csharp
// 典型的现有启动代码结构
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddExam(this WebApplicationBuilder builder)
    {
        // 1. 基础服务配置
        builder.AddServiceDefaults("exam");
        builder.Services.AddSystemServices(builder.Configuration, typeof(Program), builder.Environment);
        
        // 2. 数据库配置
        var connectionString = builder.Configuration.GetConnectionString("exam-api");
        builder.Services.AddDbContext<ExamDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
        });
        
        // 3. 特定服务注册
        builder.Services.AddScoped<IExamService, ExamService>();
        builder.Services.AddScoped<IQuestionService, QuestionService>();
        
        // 4. 第三方服务
        builder.Services.AddSignalR();
        
        return builder.Services;
    }
    
    public static async Task<WebApplication> UseExamApiServicesAsync(this WebApplication app)
    {
        // 中间件配置
        app.UseCors("AllowSpecificOriginsWithCredentials");
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        
        // 特定中间件
        app.MapHub<ExamHub>("/exam-hub");
        
        // 数据库初始化
        await app.InitializeDatabaseAsync<ExamDbContext>();
        
        return app;
    }
}
```

#### 识别迁移要素

从现有代码中识别以下要素：

1. **服务名称**: 用于Aspire服务发现（如 "exam"）
2. **连接字符串键**: 数据库连接配置键（如 "exam-api"）
3. **数据库上下文**: 使用的DbContext类型
4. **特定服务**: 需要手动注册的服务
5. **中间件配置**: 特定的中间件和Hub映射
6. **初始化逻辑**: 数据库迁移和种子数据

### 2. 备份现有代码

在开始迁移之前，创建代码备份：

```bash
# 创建迁移分支
git checkout -b feature/migrate-to-unified-startup

# 或者创建备份文件
cp ServiceCollectionExtensions.cs ServiceCollectionExtensions.cs.backup
cp Program.cs Program.cs.backup
```

### 3. 确认依赖项

确保项目已引用必要的包：

```xml
<PackageReference Include="CodeSpirit.Shared" Version="1.0.0" />
<PackageReference Include="CodeSpirit.ServiceDefaults" Version="1.0.0" />
```

## 迁移步骤

### 步骤1：创建API配置类

#### 1.1 创建配置文件夹

```bash
mkdir Configuration
```

#### 1.2 创建配置类

```csharp
// Configuration/ExamApiConfiguration.cs
using CodeSpirit.Shared.Startup;
using CodeSpirit.ExamApi.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CodeSpirit.ExamApi.Configuration;

/// <summary>
/// 考试系统API配置
/// </summary>
public class ExamApiConfiguration : BaseApiConfiguration
{
    /// <summary>
    /// 服务名称，用于Aspire服务发现
    /// </summary>
    public override string ServiceName => "exam";
    
    /// <summary>
    /// 数据库连接字符串键名
    /// </summary>
    public override string ConnectionStringKey => "exam-api";
    
    /// <summary>
    /// 配置考试系统特有的服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // 数据库配置
        var connectionString = configuration.GetConnectionString(ConnectionStringKey);
        services.AddDbContext<ExamDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
        });
        
        // 注册DbContext基类解析
        services.AddScoped<DbContext>(provider => 
            provider.GetRequiredService<ExamDbContext>());
        
        // 特定服务（如果使用标记接口，可以省略）
        // services.AddScoped<IExamService, ExamService>();
        // services.AddScoped<IQuestionService, QuestionService>();
        
        // SignalR配置
        services.AddSignalR();
    }
    
    /// <summary>
    /// 配置考试系统特有的中间件
    /// </summary>
    /// <param name="app">应用程序构建器</param>
    /// <returns>异步任务</returns>
    public override Task ConfigureMiddlewareAsync(WebApplication app)
    {
        // 映射SignalR Hub
        app.MapHub<ExamHub>("/exam-hub");
        
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// 数据库初始化
    /// </summary>
    /// <param name="app">应用程序构建器</param>
    /// <returns>异步任务</returns>
    public override async Task InitializeDatabaseAsync(WebApplication app)
    {
        await app.InitializeApiDatabaseAsync<ExamDbContext>();
    }
}
```

### 步骤2：更新Program.cs

#### 2.1 创建新的Program.cs

```csharp
// Program.cs
using CodeSpirit.Shared.Startup;
using CodeSpirit.ExamApi.Configuration;
using System.Text;

Console.OutputEncoding = Encoding.UTF8;

var builder = WebApplication.CreateBuilder(args);

// 使用统一的API启动框架
builder.AddCodeSpiritApi<ExamApiConfiguration>();

var app = builder.Build();

try
{
    // 使用统一的API配置
    await app.UseCodeSpiritApiAsync<ExamApiConfiguration>();
    app.Run();
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "应用程序启动失败");
    Console.WriteLine($"应用程序启动失败: {ex.Message}");
}
```

#### 2.2 保留原Program.cs作为备份

```csharp
// Program.Old.cs - 保留原有实现作为回滚备份
// ... 原有的Program.cs内容
```

### 步骤3：更新服务注册

#### 3.1 转换为标记接口（推荐）

```csharp
// 原有服务实现
public class ExamService : IExamService
{
    // 实现
}

// 迁移后 - 添加标记接口
public class ExamService : IScopedDependency, IExamService
{
    // 实现保持不变
}

public class QuestionService : IScopedDependency, IQuestionService
{
    // 实现保持不变
}
```

#### 3.2 或保持手动注册

如果不想使用标记接口，可以在配置类中保持手动注册：

```csharp
public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    // ... 数据库配置
    
    // 手动注册服务
    services.AddScoped<IExamService, ExamService>();
    services.AddScoped<IQuestionService, QuestionService>();
    services.AddScoped<IAnswerService, AnswerService>();
}
```

### 步骤4：测试迁移结果

#### 4.1 编译测试

```bash
dotnet build
```

#### 4.2 启动测试

```bash
dotnet run
```

#### 4.3 功能测试

- 验证API端点正常工作
- 检查数据库连接和迁移
- 测试SignalR Hub连接
- 验证认证和授权功能

### 步骤5：清理过时代码

#### 5.1 标记过时方法

```csharp
// ServiceCollectionExtensions.cs
[Obsolete("请使用 ExamApiConfiguration 配置类，此方法将在下个版本中移除")]
public static IServiceCollection AddExam(this WebApplicationBuilder builder)
{
    // 保留实现用于向后兼容
    return builder.Services;
}

[Obsolete("请使用 ExamApiConfiguration 配置类，此方法将在下个版本中移除")]
public static async Task<WebApplication> UseExamApiServicesAsync(this WebApplication app)
{
    // 保留实现用于向后兼容
    return app;
}
```

#### 5.2 删除过时代码（可选）

在确认迁移成功后，可以删除过时的扩展方法：

```csharp
// 完全删除 AddExam 和 UseExamApiServicesAsync 方法
```

## 复杂迁移场景

### 场景1：多数据库配置

#### 原有配置

```csharp
public static IServiceCollection AddExam(this WebApplicationBuilder builder)
{
    // 主数据库
    var mainConnectionString = builder.Configuration.GetConnectionString("exam-api");
    builder.Services.AddDbContext<ExamDbContext>(options =>
    {
        options.UseSqlServer(mainConnectionString);
    });
    
    // 只读数据库
    var readOnlyConnectionString = builder.Configuration.GetConnectionString("exam-api-readonly");
    builder.Services.AddDbContext<ExamReadOnlyDbContext>(options =>
    {
        options.UseSqlServer(readOnlyConnectionString);
    });
    
    return builder.Services;
}
```

#### 迁移后配置

```csharp
public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    // 主数据库
    var mainConnectionString = configuration.GetConnectionString(ConnectionStringKey);
    services.AddDbContext<ExamDbContext>(options =>
    {
        options.UseSqlServer(mainConnectionString);
    });
    
    // 只读数据库
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

### 场景2：复杂中间件配置

#### 原有配置

```csharp
public static async Task<WebApplication> UseExamApiServicesAsync(this WebApplication app)
{
    // 多租户中间件
    app.UseCodeSpiritMultiTenant();
    
    // 认证授权
    app.UseAuthentication();
    app.UseAuthorization();
    
    // 审计日志
    app.UseCodeSpiritAudit();
    
    // 控制器映射
    app.MapControllers();
    
    // SignalR Hub
    app.MapHub<ExamHub>("/exam-hub");
    app.MapHub<NotificationHub>("/notification-hub");
    
    return app;
}
```

#### 迁移后配置

```csharp
public override Task ConfigurePreAuthenticationMiddlewareAsync(WebApplication app)
{
    // 多租户中间件 - 必须在认证前
    app.UseCodeSpiritMultiTenant();
    return Task.CompletedTask;
}

public override Task ConfigurePreControllerMiddlewareAsync(WebApplication app)
{
    // 审计日志 - 在认证后，控制器映射前
    app.UseCodeSpiritAudit();
    return Task.CompletedTask;
}

public override Task ConfigureMiddlewareAsync(WebApplication app)
{
    // SignalR Hub映射
    app.MapHub<ExamHub>("/exam-hub");
    app.MapHub<NotificationHub>("/notification-hub");
    return Task.CompletedTask;
}
```

### 场景3：复杂初始化逻辑

#### 原有配置

```csharp
public static async Task<WebApplication> UseExamApiServicesAsync(this WebApplication app)
{
    // ... 中间件配置
    
    // 复杂的数据库初始化
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    
    try
    {
        var context = services.GetRequiredService<ExamDbContext>();
        await context.Database.MigrateAsync();
        
        var seeder = services.GetRequiredService<ExamSeederService>();
        await seeder.SeedAsync();
        
        var cacheService = services.GetRequiredService<ICacheService>();
        await cacheService.InitializeAsync();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "初始化失败");
        throw;
    }
    
    return app;
}
```

#### 迁移后配置

```csharp
public override async Task InitializeDatabaseAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<ExamApiConfiguration>>();
    
    try
    {
        // 1. 数据库迁移
        await app.InitializeApiDatabaseAsync<ExamDbContext>(async serviceProvider =>
        {
            var seeder = serviceProvider.GetRequiredService<ExamSeederService>();
            await seeder.SeedAsync();
        });
        
        // 2. 缓存初始化
        var cacheService = services.GetRequiredService<ICacheService>();
        await cacheService.InitializeAsync();
        
        logger.LogInformation("考试系统初始化完成");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "考试系统初始化失败");
        throw;
    }
}
```

## 迁移验证清单

### 功能验证

- [ ] API端点正常响应
- [ ] 数据库连接正常
- [ ] 数据库迁移执行成功
- [ ] 种子数据正确创建
- [ ] JWT认证功能正常
- [ ] 权限系统正常工作
- [ ] SignalR Hub连接正常
- [ ] 多租户功能正常（如果使用）
- [ ] 审计日志正常记录（如果使用）

### 性能验证

- [ ] 应用启动时间对比
- [ ] 内存使用情况对比
- [ ] API响应时间对比
- [ ] 数据库连接池使用情况

### 代码质量验证

- [ ] 编译无错误和警告
- [ ] 单元测试通过
- [ ] 集成测试通过
- [ ] 代码覆盖率保持或提升

## 回滚计划

### 快速回滚

如果迁移过程中出现问题，可以快速回滚：

```csharp
// Program.cs - 临时回滚
// 注释掉新的启动方式
// builder.AddCodeSpiritApi<ExamApiConfiguration>();
// await app.UseCodeSpiritApiAsync<ExamApiConfiguration>();

// 恢复原有启动方式
builder.AddExam();
var app = builder.Build();
await app.UseExamApiServicesAsync();
app.Run();
```

### 功能开关回滚

使用配置开关控制启动方式：

```csharp
// Program.cs
var useNewFramework = builder.Configuration.GetValue<bool>("UseUnifiedStartup", true);

if (useNewFramework)
{
    builder.AddCodeSpiritApi<ExamApiConfiguration>();
    var app = builder.Build();
    await app.UseCodeSpiritApiAsync<ExamApiConfiguration>();
}
else
{
    builder.AddExam();
    var app = builder.Build();
    await app.UseExamApiServicesAsync();
}

app.Run();
```

```json
// appsettings.json
{
  "UseUnifiedStartup": false  // 设置为false回滚到原有方式
}
```

## 迁移最佳实践

### 1. 渐进式迁移

- 一次只迁移一个API项目
- 先迁移简单的项目作为试点
- 逐步迁移复杂的项目

### 2. 充分测试

- 在迁移前编写全面的测试用例
- 迁移后进行完整的回归测试
- 使用自动化测试验证功能

### 3. 文档更新

- 更新项目文档和README
- 记录迁移过程中的问题和解决方案
- 为团队成员提供迁移培训

### 4. 监控和观察

- 密切监控应用性能
- 观察错误日志和异常
- 收集用户反馈

### 5. 代码审查

- 进行代码审查确保质量
- 验证最佳实践的遵循
- 确保代码风格一致

## 常见问题和解决方案

### 问题1：服务注册失败

**现象**: 应用启动时出现 "Unable to resolve service" 错误

**原因**: 服务未正确注册或程序集扫描失败

**解决方案**:
```csharp
// 确保服务实现了标记接口
public class ExamService : IScopedDependency, IExamService
{
    // 实现
}

// 或者在配置类中手动注册
public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    services.AddScoped<IExamService, ExamService>();
}
```

### 问题2：数据库连接失败

**现象**: 应用启动时数据库连接错误

**原因**: 连接字符串配置错误或键名不匹配

**解决方案**:
```csharp
// 验证连接字符串键名
public override string ConnectionStringKey => "exam-api"; // 确保与配置文件中的键名一致

// 在配置方法中添加验证
public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    var connectionString = configuration.GetConnectionString(ConnectionStringKey);
    if (string.IsNullOrEmpty(connectionString))
    {
        throw new InvalidOperationException($"连接字符串 '{ConnectionStringKey}' 未配置");
    }
    
    // 配置数据库上下文
}
```

### 问题3：中间件顺序问题

**现象**: 某些功能不正常工作，如多租户或认证

**原因**: 中间件执行顺序不正确

**解决方案**:
```csharp
// 使用正确的插入点
public override Task ConfigurePreAuthenticationMiddlewareAsync(WebApplication app)
{
    // 多租户必须在认证前
    app.UseCodeSpiritMultiTenant();
    return Task.CompletedTask;
}

public override Task ConfigurePreControllerMiddlewareAsync(WebApplication app)
{
    // 审计日志在认证后
    app.UseCodeSpiritAudit();
    return Task.CompletedTask;
}
```

## 总结

CodeSpirit 统一启动框架的迁移是一个系统性的过程，需要仔细规划和执行。通过遵循本指南中的步骤和最佳实践，您可以：

1. **安全地迁移现有项目**，最小化风险
2. **保持功能完整性**，确保业务不受影响
3. **提高代码质量**，减少重复和维护成本
4. **为未来扩展奠定基础**，支持新功能的快速开发

迁移完成后，您将获得一个更加统一、可维护、可扩展的API启动架构，为整个项目的长期发展提供坚实的基础。
