# CodeSpirit 统一启动框架核心架构

## 概述

CodeSpirit 统一启动框架是为整个项目中的所有API服务提供统一启动配置模式的核心组件。该框架通过标准化的配置接口和扩展方法，大幅简化了各个API项目的启动代码，提高了代码重用性和一致性。

## 核心设计理念

### 1. 统一性原则
- 所有API项目使用相同的启动模式
- 标准化的配置接口和实现方式
- 一致的中间件配置顺序

### 2. 可扩展性原则
- 支持各API项目的特定配置需求
- 提供中间件插入点机制
- 易于添加新的通用功能

### 3. 简洁性原则
- 最小化重复代码
- 简化API项目的启动配置
- 清晰的配置分离

## 核心组件架构

### 1. 配置接口层

#### IApiServiceConfiguration 接口
```csharp
public interface IApiServiceConfiguration
{
    string ServiceName { get; }
    string ConnectionStringKey { get; }
    void ConfigureServices(IServiceCollection services, IConfiguration configuration);
    Task ConfigurePreAuthenticationMiddlewareAsync(WebApplication app);
    Task ConfigurePreControllerMiddlewareAsync(WebApplication app);
    Task ConfigureMiddlewareAsync(WebApplication app);
    Task InitializeDatabaseAsync(WebApplication app);
}
```

**职责说明：**
- `ServiceName`: 定义服务名称，用于Aspire服务发现
- `ConnectionStringKey`: 指定数据库连接字符串的配置键
- `ConfigureServices`: 配置API特有的服务
- `ConfigurePreAuthenticationMiddlewareAsync`: 认证前中间件配置（如多租户）
- `ConfigurePreControllerMiddlewareAsync`: 控制器映射前中间件配置（如审计）
- `ConfigureMiddlewareAsync`: API特有中间件配置
- `InitializeDatabaseAsync`: 数据库初始化逻辑

#### BaseApiConfiguration 抽象类
```csharp
public abstract class BaseApiConfiguration : IApiServiceConfiguration
{
    public abstract string ServiceName { get; }
    public abstract string ConnectionStringKey { get; }
    
    public virtual void ConfigureServices(IServiceCollection services, IConfiguration configuration) { }
    public virtual Task ConfigurePreAuthenticationMiddlewareAsync(WebApplication app) => Task.CompletedTask;
    public virtual Task ConfigurePreControllerMiddlewareAsync(WebApplication app) => Task.CompletedTask;
    public virtual Task ConfigureMiddlewareAsync(WebApplication app) => Task.CompletedTask;
    public virtual Task InitializeDatabaseAsync(WebApplication app) => Task.CompletedTask;
}
```

**设计优势：**
- 提供默认实现，减少子类实现负担
- 强制要求子类定义服务名称和连接字符串键
- 所有配置方法都是虚方法，支持按需重写

### 2. 启动扩展层

#### ApiStartupExtensions 类
提供统一的API启动扩展方法：

```csharp
public static class ApiStartupExtensions
{
    public static IServiceCollection AddCodeSpiritApi<TConfig>(
        this WebApplicationBuilder builder,
        TConfig? configuration = null) 
        where TConfig : class, IApiServiceConfiguration, new()
    
    public static async Task<WebApplication> UseCodeSpiritApiAsync<TConfig>(
        this WebApplication app,
        TConfig? configuration = null)
        where TConfig : class, IApiServiceConfiguration, new()
}
```

**执行流程：**

1. **AddCodeSpiritApi 阶段**：
   - 注册Aspire服务默认配置
   - 使用Scrutor自动注册标记接口的服务
   - 添加系统服务和通用API服务
   - 执行API特定的服务配置

2. **UseCodeSpiritApiAsync 阶段**：
   - 配置通用中间件（包含插入点）
   - 执行API特定的中间件配置
   - 执行数据库初始化

### 3. 通用服务层

#### CommonApiServiceExtensions 类
提供通用API服务和中间件的配置：

```csharp
public static class CommonApiServiceExtensions
{
    public static IServiceCollection AddCommonApiServices(
        this IServiceCollection services, 
        IConfiguration configuration,
        string connectionStringKey)
    
    public static async Task<WebApplication> UseCommonApiMiddlewareAsync<TConfig>(
        this WebApplication app, 
        TConfig config)
        where TConfig : class, IApiServiceConfiguration
    
    public static async Task InitializeApiDatabaseAsync<TDbContext>(
        this WebApplication app,
        Func<IServiceProvider, Task>? seedAction = null)
        where TDbContext : DbContext
}
```

**通用服务包括：**
- JWT认证配置
- 控制器配置
- 仓储模式注册
- AutoMapper配置

**通用中间件包括：**
- CORS配置
- 认证授权中间件
- 控制器映射
- AMIS UI引擎
- CodeSpirit权限系统
- CodeSpirit导航系统

## 中间件插入点机制

### 插入点设计

框架提供了两个关键的中间件插入点：

1. **认证前插入点** (`ConfigurePreAuthenticationMiddlewareAsync`)
   - 用于配置需要在认证之前执行的中间件
   - 典型用例：多租户中间件、请求日志中间件

2. **控制器映射前插入点** (`ConfigurePreControllerMiddlewareAsync`)
   - 用于配置需要在控制器映射之前执行的中间件
   - 典型用例：审计日志中间件、性能监控中间件

### 中间件执行顺序

```
1. CORS
2. → ConfigurePreAuthenticationMiddlewareAsync (插入点1)
3. Authentication
4. Authorization
5. → ConfigurePreControllerMiddlewareAsync (插入点2)
6. MapControllers
7. AMIS UI引擎
8. CodeSpirit权限系统
9. CodeSpirit导航系统
10. → ConfigureMiddlewareAsync (API特定中间件)
```

## 依赖注入集成

### Scrutor 自动注册

框架集成了Scrutor库，支持基于标记接口的自动服务注册：

```csharp
// 在服务类上标记接口
public class ExamService : IScopedDependency, IExamService
{
    // 自动注册为Scoped生命周期
}

public class CacheService : ISingletonDependency, ICacheService
{
    // 自动注册为Singleton生命周期
}
```

**支持的标记接口：**
- `IScopedDependency`: Scoped生命周期
- `ITransientDependency`: Transient生命周期
- `ISingletonDependency`: Singleton生命周期

### 程序集扫描策略

框架使用智能程序集扫描策略：

1. **入口程序集扫描**: 扫描API项目的入口程序集
2. **CodeSpirit程序集扫描**: 自动包含所有CodeSpirit相关程序集
3. **去重处理**: 确保程序集不会重复扫描

## 数据库集成

### 统一数据库初始化

```csharp
public static async Task InitializeApiDatabaseAsync<TDbContext>(
    this WebApplication app,
    Func<IServiceProvider, Task>? seedAction = null)
    where TDbContext : DbContext
```

**初始化流程：**
1. 创建服务作用域
2. 获取数据库上下文
3. 执行数据库迁移
4. 执行种子数据操作（可选）
5. 记录初始化日志

### 连接字符串管理

- 支持多数据库配置
- 通过 `ConnectionStringKey` 指定特定的连接字符串
- 自动输出连接字符串信息用于调试

## 错误处理和日志

### 启动错误处理

框架提供统一的启动错误处理机制：

```csharp
try
{
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

### 数据库初始化日志

- 成功初始化时记录信息日志
- 初始化失败时记录错误日志并重新抛出异常
- 包含数据库上下文类型信息

## 性能优化

### 程序集缓存

- 程序集扫描结果进行缓存
- 避免重复的反射操作
- 优化启动性能

### 服务注册优化

- 使用Scrutor批量注册服务
- 减少手动服务注册的性能开销
- 支持条件注册和生命周期管理

## 扩展性设计

### 新功能添加

框架设计支持轻松添加新的通用功能：

1. **通用服务扩展**: 在 `AddCommonApiServices` 中添加
2. **通用中间件扩展**: 在 `UseCommonApiMiddlewareAsync` 中添加
3. **新插入点**: 可以在接口中添加新的插入点方法

### 向后兼容性

- 提供过时方法的兼容性支持
- 使用 `[Obsolete]` 特性标记过时方法
- 渐进式迁移支持

## 最佳实践

### 配置类设计

1. **单一职责**: 每个配置类只负责一个API的配置
2. **命名规范**: 使用 `{ApiName}Configuration` 命名模式
3. **文档注释**: 为所有公共成员添加XML文档注释
4. **异步优先**: 优先使用异步方法

### 服务注册

1. **标记接口优先**: 优先使用标记接口进行自动注册
2. **生命周期明确**: 明确指定服务的生命周期
3. **接口分离**: 遵循接口分离原则

### 中间件配置

1. **顺序重要**: 注意中间件的执行顺序
2. **异常处理**: 在中间件中添加适当的异常处理
3. **性能考虑**: 避免在中间件中执行耗时操作

## 总结

CodeSpirit 统一启动框架通过标准化的配置接口、智能的依赖注入、灵活的中间件插入点和统一的数据库初始化，为整个项目提供了一致、可维护、可扩展的API启动解决方案。该框架大幅减少了重复代码，提高了开发效率，并为未来的功能扩展奠定了坚实的基础。
