# ClientIpService 使用指南

## 概述

`ClientIpService` 是一个统一的客户端IP地址获取服务，用于在整个解决方案中标准化IP地址的获取逻辑。该服务已被移动到 `CodeSpirit.Shared` 项目中，供所有项目使用。

## 功能特性

- **多代理头支持**：支持多种反向代理和负载均衡器的IP传递头
- **智能IP提取**：按优先级从不同来源提取真实客户端IP地址
- **IPv4/IPv6支持**：完整支持IPv4和IPv6地址
- **私有网络检测**：能够识别和处理私有网络地址
- **RFC 7239兼容**：支持标准Forwarded头格式
- **详细日志记录**：提供完整的调试和监控日志

## 支持的代理头

服务按以下优先级检查代理头：

1. `X-Forwarded-For` - 最常用的代理头
2. `X-Real-IP` - Nginx常用
3. `CF-Connecting-IP` - Cloudflare
4. `True-Client-IP` - Akamai
5. `X-Client-IP` - 自定义头
6. `X-Original-IP` - 其他代理
7. `X-Forwarded` - 备用选项
8. `Forwarded-For` - 备用选项
9. `Forwarded` - RFC 7239标准

## 安装和配置

### 1. 服务注册

在项目的`Program.cs`或Startup类中注册服务：

```csharp
using CodeSpirit.Shared.DependencyInjection;

// 在服务注册部分添加
builder.Services.AddSharedServices();
```

### 2. 依赖注入

在需要使用IP服务的类中注入接口：

```csharp
using CodeSpirit.Shared.Services;

public class YourController : ControllerBase
{
    private readonly IClientIpService _clientIpService;

    public YourController(IClientIpService clientIpService)
    {
        _clientIpService = clientIpService;
    }

    public IActionResult SomeAction()
    {
        var clientIp = _clientIpService.GetClientIpAddress(HttpContext);
        // 使用IP地址...
        return Ok();
    }
}
```

## 使用方式

### 方式一：使用HttpContext

```csharp
public class ExampleService
{
    private readonly IClientIpService _clientIpService;

    public ExampleService(IClientIpService clientIpService)
    {
        _clientIpService = clientIpService;
    }

    public void ProcessRequest(HttpContext context)
    {
        var clientIp = _clientIpService.GetClientIpAddress(context);
        Console.WriteLine($"客户端IP: {clientIp}");
    }
}
```

### 方式二：使用IHttpContextAccessor

```csharp
public class BackgroundService
{
    private readonly IClientIpService _clientIpService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public BackgroundService(
        IClientIpService clientIpService,
        IHttpContextAccessor httpContextAccessor)
    {
        _clientIpService = clientIpService;
        _httpContextAccessor = httpContextAccessor;
    }

    public void ProcessBackgroundTask()
    {
        var clientIp = _clientIpService.GetClientIpAddress(_httpContextAccessor);
        Console.WriteLine($"客户端IP: {clientIp}");
    }
}
```

## 中间件使用

在自定义中间件中使用：

```csharp
public class CustomMiddleware
{
    private readonly RequestDelegate _next;

    public CustomMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IClientIpService clientIpService)
    {
        var clientIp = clientIpService.GetClientIpAddress(context);
        context.Items["ClientIP"] = clientIp;
        
        await _next(context);
    }
}
```

## 已集成的项目组件

### 1. 审计中间件 (CodeSpirit.Audit)

审计中间件已经集成了新的IP服务，在`InvokeAsync`方法中会自动注入：

```csharp
public async Task InvokeAsync(HttpContext context, IAuditService auditService, IClientIpService clientIpService)
{
    // 获取客户端IP地址
    var ipAddress = clientIpService.GetClientIpAddress(context);
    // ... 其他审计逻辑
}
```

## 需要更新的项目

以下项目中的IP获取代码需要更新为使用新的统一服务：

### 1. CodeSpirit.IdentityApi

- `Controllers/AuthController.cs` (第56行)
- `Services/CustomSignInManager.cs` (第36行)
- `Audit/CustomAuditDataProvider.cs` (第111行)

#### 更新示例：

**之前：**
```csharp
IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
```

**之后：**
```csharp
public class AuthController : ControllerBase
{
    private readonly IClientIpService _clientIpService;

    public AuthController(IClientIpService clientIpService)
    {
        _clientIpService = clientIpService;
    }

    public async Task<IActionResult> Login(LoginDto model)
    {
        // ...
        IpAddress = _clientIpService.GetClientIpAddress(HttpContext),
        // ...
    }
}
```

### 2. CodeSpirit.ExamApi

- `Controllers/Client/IndexController.cs` (第243行的`GetClientIpAddress`方法)

#### 更新示例：

**之前：**
```csharp
private string GetClientIpAddress()
{
    // 复杂的IP获取逻辑...
}
```

**之后：**
```csharp
public class IndexController : ApiControllerBase
{
    private readonly IClientIpService _clientIpService;

    public IndexController(IClientIpService clientIpService)
    {
        _clientIpService = clientIpService;
    }

    // 删除原有的GetClientIpAddress方法，直接使用服务
    private string GetClientIpAddress()
    {
        return _clientIpService.GetClientIpAddress(HttpContext);
    }
}
```

### 3. CodeSpirit.ConfigCenter

- `Hubs/ConfigHub.cs` (第100行和第122行)

#### 更新示例：

**之前：**
```csharp
IpAddress = Context.GetHttpContext()?.Connection?.RemoteIpAddress?.ToString() ?? "未知"
```

**之后：**
```csharp
public class ConfigHub : Hub
{
    private readonly IClientIpService _clientIpService;

    public ConfigHub(IClientIpService clientIpService)
    {
        _clientIpService = clientIpService;
    }

    public async Task SomeMethod()
    {
        var httpContext = Context.GetHttpContext();
        var ipAddress = httpContext != null 
            ? _clientIpService.GetClientIpAddress(httpContext) 
            : "未知";
    }
}
```

### 4. CodeSpirit.Web

- `Middlewares/ProxyMiddleware.cs` (第253行)

## IP地址验证规则

服务包含以下IP地址验证规则：

1. **本地回环地址过滤**：`127.0.0.1`, `::1`, `localhost`
2. **私有网络地址检测**：
   - IPv4: `10.0.0.0/8`, `172.16.0.0/12`, `192.168.0.0/16`
   - IPv6: `fc00::/7` (私有单播地址)
3. **空值处理**：返回"未知"而不是null或空字符串

## 错误处理

服务具有完善的错误处理机制：

- **参数验证**：检查HttpContext是否为null
- **异常捕获**：捕获IP解析过程中的异常
- **降级处理**：当无法获取IP时返回"未知"
- **日志记录**：记录警告和错误日志

## 性能考虑

- **头部优先级**：按使用频率排序，最常用的头部优先检查
- **早期退出**：找到有效IP后立即返回，避免不必要的处理
- **缓存友好**：静态配置数据，减少重复计算

## 调试和监控

服务提供详细的日志记录：

- **Debug级别**：记录成功获取IP的来源
- **Warning级别**：记录参数为null或无法获取IP的情况
- **Error级别**：记录异常情况

启用调试日志示例：

```json
{
  "Logging": {
    "LogLevel": {
      "CodeSpirit.Shared.Services.ClientIpService": "Debug"
    }
  }
}
```

## 最佳实践

1. **依赖注入**：始终通过依赖注入使用服务，避免直接实例化
2. **错误处理**：检查返回值是否为"未知"，并相应处理
3. **日志记录**：在关键业务流程中记录获取到的IP地址
4. **安全考虑**：在公网环境中，考虑IP地址的真实性和可伪造性

## 示例：完整的控制器实现

```csharp
using CodeSpirit.Shared.Services;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class ExampleController : ControllerBase
{
    private readonly IClientIpService _clientIpService;
    private readonly ILogger<ExampleController> _logger;

    public ExampleController(
        IClientIpService clientIpService,
        ILogger<ExampleController> logger)
    {
        _clientIpService = clientIpService;
        _logger = logger;
    }

    [HttpPost("action")]
    public async Task<IActionResult> SomeAction()
    {
        var clientIp = _clientIpService.GetClientIpAddress(HttpContext);
        
        _logger.LogInformation("处理来自 {ClientIP} 的请求", clientIp);

        if (clientIp == "未知")
        {
            _logger.LogWarning("无法获取客户端IP地址");
        }

        // 业务逻辑...
        
        return Ok(new { ClientIP = clientIp });
    }
}
```

## 更新检查清单

在迁移现有代码时，请检查以下项目：

- [ ] 更新`CodeSpirit.IdentityApi`中的IP获取逻辑
- [ ] 更新`CodeSpirit.ExamApi`中的IP获取逻辑  
- [ ] 更新`CodeSpirit.ConfigCenter`中的IP获取逻辑
- [ ] 更新`CodeSpirit.Web`中的IP获取逻辑
- [ ] 在各项目的`Program.cs`中添加`AddSharedServices()`调用
- [ ] 测试各项目中IP获取功能的正确性
- [ ] 更新相关的单元测试
- [ ] 更新API文档中涉及IP地址的部分 