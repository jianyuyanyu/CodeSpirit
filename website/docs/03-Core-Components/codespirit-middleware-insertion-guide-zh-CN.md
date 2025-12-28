# CodeSpirit 中间件插入点使用指南

## 概述

CodeSpirit 统一启动框架提供了灵活的中间件插入点机制，允许开发者在标准中间件管道的特定位置插入自定义中间件。本指南详细说明如何正确使用这些插入点来满足不同的业务需求。

## 中间件执行顺序

### 完整的中间件管道

```
请求 → 
1. CORS
2. → ConfigurePreAuthenticationMiddlewareAsync (插入点1)
3. Authentication
4. Authorization  
5. → ConfigurePreControllerMiddlewareAsync (插入点2)
6. MapControllers
7. AMIS UI引擎
8. CodeSpirit权限系统
9. CodeSpirit导航系统
10. → ConfigureMiddlewareAsync (插入点3)
→ 响应
```

### 插入点说明

| 插入点 | 方法名 | 执行时机 | 适用场景 |
|--------|--------|----------|----------|
| 插入点1 | `ConfigurePreAuthenticationMiddlewareAsync` | 认证之前 | 多租户、请求日志、API版本 |
| 插入点2 | `ConfigurePreControllerMiddlewareAsync` | 认证之后，控制器映射之前 | 审计日志、性能监控、限流 |
| 插入点3 | `ConfigureMiddlewareAsync` | 核心中间件之后 | SignalR Hub、静态文件、健康检查 |

## 插入点1：认证前中间件

### 使用场景

认证前中间件适用于需要在用户身份验证之前执行的逻辑：

- **多租户识别**: 根据请求确定租户信息
- **请求日志**: 记录所有进入的请求
- **API版本控制**: 处理API版本路由
- **请求预处理**: 请求格式化、编码转换等

### 实现示例

#### 多租户中间件

```csharp
public override Task ConfigurePreAuthenticationMiddlewareAsync(WebApplication app)
{
    // 多租户中间件 - 必须在认证前执行
    app.UseCodeSpiritMultiTenant();
    
    return Task.CompletedTask;
}
```

#### 请求日志中间件

```csharp
public override Task ConfigurePreAuthenticationMiddlewareAsync(WebApplication app)
{
    // 自定义请求日志中间件
    app.Use(async (context, next) =>
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<ExamApiConfiguration>>();
        var stopwatch = Stopwatch.StartNew();
        
        logger.LogInformation("请求开始: {Method} {Path}", 
            context.Request.Method, context.Request.Path);
        
        try
        {
            await next();
        }
        finally
        {
            stopwatch.Stop();
            logger.LogInformation("请求完成: {Method} {Path} - {StatusCode} - {ElapsedMs}ms",
                context.Request.Method, 
                context.Request.Path, 
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds);
        }
    });
    
    return Task.CompletedTask;
}
```

#### API版本控制

```csharp
public override Task ConfigurePreAuthenticationMiddlewareAsync(WebApplication app)
{
    // API版本中间件
    app.Use(async (context, next) =>
    {
        // 从请求头或查询参数获取API版本
        var apiVersion = context.Request.Headers["X-API-Version"].FirstOrDefault() 
                        ?? context.Request.Query["version"].FirstOrDefault() 
                        ?? "v1";
        
        // 将版本信息添加到请求上下文
        context.Items["ApiVersion"] = apiVersion;
        
        // 根据版本设置路由前缀
        if (apiVersion != "v1")
        {
            context.Request.Path = $"/{apiVersion}" + context.Request.Path;
        }
        
        await next();
    });
    
    return Task.CompletedTask;
}
```

#### 请求预处理中间件

```csharp
public override Task ConfigurePreAuthenticationMiddlewareAsync(WebApplication app)
{
    // 请求预处理中间件
    app.Use(async (context, next) =>
    {
        // 设置请求ID
        if (!context.Request.Headers.ContainsKey("X-Request-ID"))
        {
            context.Request.Headers.Add("X-Request-ID", Guid.NewGuid().ToString());
        }
        
        // 设置响应头
        context.Response.Headers.Add("X-Powered-By", "CodeSpirit");
        context.Response.Headers.Add("X-Request-ID", 
            context.Request.Headers["X-Request-ID"].ToString());
        
        await next();
    });
    
    return Task.CompletedTask;
}
```

## 插入点2：控制器映射前中间件

### 使用场景

控制器映射前中间件适用于需要在用户认证之后、控制器处理之前执行的逻辑：

- **审计日志**: 记录已认证用户的操作
- **性能监控**: 监控API性能指标
- **限流控制**: 基于用户的请求限流
- **权限预检**: 额外的权限验证

### 实现示例

#### 审计日志中间件

```csharp
public override Task ConfigurePreControllerMiddlewareAsync(WebApplication app)
{
    // 审计日志中间件 - 需要在认证后执行
    app.UseCodeSpiritAudit();
    
    return Task.CompletedTask;
}
```

#### 性能监控中间件

```csharp
public override Task ConfigurePreControllerMiddlewareAsync(WebApplication app)
{
    app.Use(async (context, next) =>
    {
        var performanceService = context.RequestServices.GetRequiredService<IPerformanceService>();
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            await next();
        }
        finally
        {
            stopwatch.Stop();
            
            // 记录性能指标
            await performanceService.RecordAsync(new PerformanceMetric
            {
                Endpoint = $"{context.Request.Method} {context.Request.Path}",
                Duration = stopwatch.ElapsedMilliseconds,
                StatusCode = context.Response.StatusCode,
                UserId = context.User.Identity?.Name,
                Timestamp = DateTime.UtcNow
            });
        }
    });
    
    return Task.CompletedTask;
}
```

#### 用户限流中间件

```csharp
public override Task ConfigurePreControllerMiddlewareAsync(WebApplication app)
{
    app.Use(async (context, next) =>
    {
        var rateLimitService = context.RequestServices.GetRequiredService<IRateLimitService>();
        var userId = context.User.Identity?.Name;
        
        if (!string.IsNullOrEmpty(userId))
        {
            var isAllowed = await rateLimitService.IsAllowedAsync(userId, context.Request.Path);
            if (!isAllowed)
            {
                context.Response.StatusCode = 429; // Too Many Requests
                await context.Response.WriteAsync("请求过于频繁，请稍后再试");
                return;
            }
        }
        
        await next();
    });
    
    return Task.CompletedTask;
}
```

#### 权限预检中间件

```csharp
public override Task ConfigurePreControllerMiddlewareAsync(WebApplication app)
{
    app.Use(async (context, next) =>
    {
        // 对特定路径进行额外的权限检查
        if (context.Request.Path.StartsWithSegments("/api/admin"))
        {
            var authService = context.RequestServices.GetRequiredService<IAuthorizationService>();
            var user = context.User;
            
            var result = await authService.AuthorizeAsync(user, "AdminPolicy");
            if (!result.Succeeded)
            {
                context.Response.StatusCode = 403;
                await context.Response.WriteAsync("需要管理员权限");
                return;
            }
        }
        
        await next();
    });
    
    return Task.CompletedTask;
}
```

## 插入点3：API特定中间件

### 使用场景

API特定中间件适用于各个API服务独有的功能：

- **SignalR Hub映射**: 实时通信功能
- **静态文件服务**: 文件下载服务
- **健康检查**: API健康状态监控
- **自定义端点**: 特殊业务端点

### 实现示例

#### SignalR Hub配置

```csharp
public override Task ConfigureMiddlewareAsync(WebApplication app)
{
    // 映射SignalR Hub
    app.MapHub<ExamHub>("/exam-hub", options =>
    {
        options.Transports = HttpTransportType.WebSockets | HttpTransportType.LongPolling;
    });
    
    app.MapHub<NotificationHub>("/notification-hub");
    app.MapHub<ChatHub>("/chat-hub");
    
    return Task.CompletedTask;
}
```

#### 静态文件服务

```csharp
public override Task ConfigureMiddlewareAsync(WebApplication app)
{
    var configuration = app.Services.GetRequiredService<IConfiguration>();
    
    // 配置静态文件服务
    var fileStoragePath = configuration.GetValue<string>("FileStorage:Path", "uploads");
    
    if (Directory.Exists(fileStoragePath))
    {
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(fileStoragePath),
            RequestPath = "/files",
            OnPrepareResponse = context =>
            {
                // 设置缓存头
                context.Context.Response.Headers.Append("Cache-Control", "public,max-age=3600");
            }
        });
    }
    
    return Task.CompletedTask;
}
```

#### 健康检查端点

```csharp
public override Task ConfigureMiddlewareAsync(WebApplication app)
{
    // 基础健康检查
    app.MapHealthChecks("/health");
    
    // 详细健康检查
    app.MapHealthChecks("/health/detailed", new HealthCheckOptions
    {
        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = "application/json";
            
            var result = JsonSerializer.Serialize(new
            {
                status = report.Status.ToString(),
                checks = report.Entries.Select(entry => new
                {
                    name = entry.Key,
                    status = entry.Value.Status.ToString(),
                    description = entry.Value.Description,
                    duration = entry.Value.Duration.TotalMilliseconds
                })
            });
            
            await context.Response.WriteAsync(result);
        }
    });
    
    // 存活检查
    app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("live")
    });
    
    // 就绪检查
    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready")
    });
    
    return Task.CompletedTask;
}
```

#### 自定义API端点

```csharp
public override Task ConfigureMiddlewareAsync(WebApplication app)
{
    // 自定义信息端点
    app.MapGet("/api/info", () =>
    {
        var configuration = app.Services.GetRequiredService<IConfiguration>();
        
        return Results.Ok(new
        {
            service = ServiceName,
            version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(),
            environment = app.Environment.EnvironmentName,
            timestamp = DateTime.UtcNow
        });
    });
    
    // 配置端点（仅开发环境）
    if (app.Environment.IsDevelopment())
    {
        app.MapGet("/api/config", (IConfiguration configuration) =>
        {
            var config = new Dictionary<string, string>();
            foreach (var item in configuration.AsEnumerable())
            {
                // 过滤敏感信息
                if (!item.Key.Contains("Password") && !item.Key.Contains("Secret"))
                {
                    config[item.Key] = item.Value ?? "";
                }
            }
            return Results.Ok(config);
        });
    }
    
    return Task.CompletedTask;
}
```

## 高级中间件模式

### 条件中间件

```csharp
public override Task ConfigurePreAuthenticationMiddlewareAsync(WebApplication app)
{
    var configuration = app.Services.GetRequiredService<IConfiguration>();
    
    // 根据配置条件添加中间件
    if (configuration.GetValue<bool>("EnableRequestLogging", false))
    {
        app.UseRequestLogging();
    }
    
    // 根据环境条件添加中间件
    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }
    else
    {
        app.UseExceptionHandler("/error");
    }
    
    // 根据功能开关添加中间件
    var features = configuration.GetSection("Features");
    if (features.GetValue<bool>("EnableMultiTenant", false))
    {
        app.UseCodeSpiritMultiTenant();
    }
    
    return Task.CompletedTask;
}
```

### 中间件组合

```csharp
public override Task ConfigurePreControllerMiddlewareAsync(WebApplication app)
{
    // 组合多个相关中间件
    app.UseMiddleware<RequestTimingMiddleware>();
    app.UseMiddleware<UserActivityMiddleware>();
    app.UseMiddleware<SecurityHeadersMiddleware>();
    
    return Task.CompletedTask;
}

// 自定义中间件示例
public class RequestTimingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestTimingMiddleware> _logger;
    
    public RequestTimingMiddleware(RequestDelegate next, ILogger<RequestTimingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            
            if (stopwatch.ElapsedMilliseconds > 1000) // 超过1秒的请求
            {
                _logger.LogWarning("慢请求: {Method} {Path} - {ElapsedMs}ms",
                    context.Request.Method,
                    context.Request.Path,
                    stopwatch.ElapsedMilliseconds);
            }
        }
    }
}
```

### 异步中间件

```csharp
public override async Task ConfigureMiddlewareAsync(WebApplication app)
{
    // 异步初始化的中间件
    var backgroundService = app.Services.GetRequiredService<IBackgroundService>();
    await backgroundService.StartAsync(CancellationToken.None);
    
    // 需要异步配置的SignalR Hub
    var hubOptions = await GetHubOptionsAsync(app.Services);
    app.MapHub<ExamHub>("/exam-hub", hubOptions);
}

private async Task<Action<HubOptions>> GetHubOptionsAsync(IServiceProvider services)
{
    var configService = services.GetRequiredService<IConfigurationService>();
    var settings = await configService.GetHubSettingsAsync();
    
    return options =>
    {
        options.EnableDetailedErrors = settings.EnableDetailedErrors;
        options.MaximumReceiveMessageSize = settings.MaxMessageSize;
    };
}
```

## 错误处理和调试

### 中间件异常处理

```csharp
public override Task ConfigurePreAuthenticationMiddlewareAsync(WebApplication app)
{
    app.Use(async (context, next) =>
    {
        try
        {
            // 多租户逻辑
            var tenantService = context.RequestServices.GetRequiredService<ITenantService>();
            await tenantService.ResolveTenantAsync(context);
            
            await next();
        }
        catch (TenantNotFoundException ex)
        {
            var logger = context.RequestServices.GetRequiredService<ILogger<ExamApiConfiguration>>();
            logger.LogWarning(ex, "租户未找到: {TenantId}", ex.TenantId);
            
            context.Response.StatusCode = 404;
            await context.Response.WriteAsync("租户不存在");
        }
        catch (Exception ex)
        {
            var logger = context.RequestServices.GetRequiredService<ILogger<ExamApiConfiguration>>();
            logger.LogError(ex, "多租户中间件执行失败");
            throw;
        }
    });
    
    return Task.CompletedTask;
}
```

### 中间件调试

```csharp
public override Task ConfigurePreAuthenticationMiddlewareAsync(WebApplication app)
{
    if (app.Environment.IsDevelopment())
    {
        // 调试中间件 - 输出请求信息
        app.Use(async (context, next) =>
        {
            var logger = context.RequestServices.GetRequiredService<ILogger<ExamApiConfiguration>>();
            
            logger.LogDebug("中间件调试 - 请求: {Method} {Path}", 
                context.Request.Method, context.Request.Path);
            
            logger.LogDebug("中间件调试 - 请求头: {Headers}", 
                string.Join(", ", context.Request.Headers.Select(h => $"{h.Key}={h.Value}")));
            
            await next();
            
            logger.LogDebug("中间件调试 - 响应: {StatusCode}", 
                context.Response.StatusCode);
        });
    }
    
    return Task.CompletedTask;
}
```

## 性能优化

### 中间件性能优化

```csharp
public override Task ConfigurePreControllerMiddlewareAsync(WebApplication app)
{
    // 使用对象池优化性能
    app.Use(async (context, next) =>
    {
        var objectPool = context.RequestServices.GetRequiredService<ObjectPool<StringBuilder>>();
        var stringBuilder = objectPool.Get();
        
        try
        {
            // 使用StringBuilder进行字符串操作
            stringBuilder.AppendLine($"Request: {context.Request.Method} {context.Request.Path}");
            
            await next();
            
            stringBuilder.AppendLine($"Response: {context.Response.StatusCode}");
            
            var logger = context.RequestServices.GetRequiredService<ILogger<ExamApiConfiguration>>();
            logger.LogInformation(stringBuilder.ToString());
        }
        finally
        {
            objectPool.Return(stringBuilder);
        }
    });
    
    return Task.CompletedTask;
}
```

### 缓存优化

```csharp
public override Task ConfigurePreAuthenticationMiddlewareAsync(WebApplication app)
{
    // 缓存租户信息以提高性能
    app.Use(async (context, next) =>
    {
        var cache = context.RequestServices.GetRequiredService<IMemoryCache>();
        var tenantId = ExtractTenantId(context.Request);
        
        if (!string.IsNullOrEmpty(tenantId))
        {
            var cacheKey = $"tenant:{tenantId}";
            
            if (!cache.TryGetValue(cacheKey, out TenantInfo tenantInfo))
            {
                var tenantService = context.RequestServices.GetRequiredService<ITenantService>();
                tenantInfo = await tenantService.GetTenantAsync(tenantId);
                
                cache.Set(cacheKey, tenantInfo, TimeSpan.FromMinutes(10));
            }
            
            context.Items["TenantInfo"] = tenantInfo;
        }
        
        await next();
    });
    
    return Task.CompletedTask;
}
```

## 最佳实践

### 1. 中间件顺序

- **认证前**: 多租户、请求日志、API版本
- **认证后**: 审计日志、性能监控、权限检查
- **API特定**: SignalR、静态文件、健康检查

### 2. 异常处理

- 在中间件中添加适当的异常处理
- 记录详细的错误日志
- 向客户端返回友好的错误信息

### 3. 性能考虑

- 避免在中间件中执行耗时操作
- 使用对象池和缓存优化性能
- 合理设置中间件的执行顺序

### 4. 可测试性

- 将中间件逻辑封装到独立的类中
- 使用依赖注入提高可测试性
- 编写单元测试验证中间件行为

### 5. 配置驱动

- 使用配置文件控制中间件的启用/禁用
- 支持环境特定的中间件配置
- 提供功能开关控制

## 总结

CodeSpirit 中间件插入点机制提供了灵活而强大的扩展能力，通过正确使用这些插入点，您可以：

1. **在正确的时机执行中间件逻辑**，确保功能正常工作
2. **实现复杂的业务需求**，如多租户、审计、性能监控等
3. **保持代码的清晰和可维护性**，通过合理的中间件组织
4. **优化应用性能**，通过高效的中间件实现
5. **支持全面的测试**，通过可测试的中间件设计

记住，中间件的执行顺序非常重要，选择正确的插入点是实现预期功能的关键。
