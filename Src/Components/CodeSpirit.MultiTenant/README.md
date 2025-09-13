# CodeSpirit.MultiTenant

CodeSpirit多租户组件，提供灵活的多租户数据隔离解决方案。

## 🆕 v2.0 架构更新

从 v2.0 开始，多租户组件采用**简化架构**：
- **轻量级中间件**：只负责解析租户ID并设置到HTTP上下文，不进行验证
- **按需验证**：各服务通过 `ITenantContext` 按需进行租户验证和信息获取
- **更好性能**：减少不必要的验证开销，提升请求处理性能
- **更高灵活性**：服务可以根据业务需求选择验证策略

## 功能特性

- 🏢 **多种租户策略**：支持共享数据库、独立表结构、独立数据库等多种隔离策略
- 🔍 **灵活的租户解析**：支持从Header、Query参数、子域名、路径等多种方式解析租户
- 💾 **统一存储策略**：按照内存→分布式缓存→API的固定优先级获取租户信息
- ⚡ **高性能缓存**：内置分布式缓存支持，提升租户解析性能
- 🔧 **易于配置**：提供丰富的配置选项，满足不同场景需求
- 🧪 **完整测试**：包含完整的单元测试，确保组件稳定性
- 👤 **用户上下文集成**：扩展ICurrentUser接口，提供完整的多租户用户上下文
- 🔐 **JWT集成**：登录接口自动在JWT中包含租户信息
- 🚀 **按需验证**：支持服务级别的按需租户验证，提供更好的性能和灵活性

## 快速开始

### 1. 安装包

```bash
dotnet add package CodeSpirit.MultiTenant
```

### 2. 配置服务

在 `Program.cs` 中注册多租户服务：

```csharp
using CodeSpirit.MultiTenant.Extensions;

var builder = WebApplication.CreateBuilder(args);

// 注册多租户服务
builder.Services.AddCodeSpiritMultiTenant(builder.Configuration);

var app = builder.Build();

// 使用多租户中间件
app.UseCodeSpiritMultiTenant();

app.Run();
```

### 3. 配置选项

在 `appsettings.json` 中配置多租户选项：

```json
{
  "MultiTenant": {
    "Enabled": true,
    "DefaultTenantId": "default",
    "ResolveFromHeader": true,
    "TenantHeaderName": "TenantId",
    "ResolveFromQuery": true,
    "TenantQueryName": "tenantId",
    "ResolveFromSubdomain": false,
    "ResolveFromPath": false,
    "EnableTenantCache": true,
    "CacheExpirationMinutes": 30,
    "ValidateInMiddleware": false,
    "CacheTenantInfoInMiddleware": false,
    "SkipPathPatterns": [
      "/health*",
      "/swagger*",
      "/favicon.ico",
      "/_*"
    ]
  }
}
```

### 4. 按需验证使用

在服务中使用 `ITenantContext` 进行按需验证：

```csharp
public class OrderService
{
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<OrderService> _logger;

    public OrderService(ITenantContext tenantContext, ILogger<OrderService> logger)
    {
        _tenantContext = tenantContext;
        _logger = logger;
    }

    // 简单获取租户ID（不验证）
    public async Task<List<Order>> GetOrdersAsync()
    {
        var tenantId = _tenantContext.TenantId;
        if (string.IsNullOrEmpty(tenantId))
        {
            throw new InvalidOperationException("无法获取租户ID");
        }
        
        // 查询订单...
        return orders.Where(o => o.TenantId == tenantId).ToList();
    }

    // 验证租户有效性
    public async Task<Order> CreateOrderAsync(CreateOrderDto dto)
    {
        // 验证当前租户是否有效
        if (!await _tenantContext.ValidateCurrentTenantAsync())
        {
            throw new UnauthorizedAccessException("租户无效或已禁用");
        }

        var tenantId = _tenantContext.TenantId;
        var order = new Order
        {
            TenantId = tenantId,
            ProductName = dto.ProductName,
            Amount = dto.Amount
        };

        // 保存订单...
        return order;
    }

    // 获取验证过的租户信息
    public async Task<TenantSummary> GetTenantSummaryAsync()
    {
        // 获取并验证租户信息，如果无效会根据配置策略处理
        var tenantInfo = await _tenantContext.GetValidatedCurrentTenantInfoAsync();
        if (tenantInfo == null)
        {
            throw new UnauthorizedAccessException("无法获取有效的租户信息");
        }

        return new TenantSummary
        {
            TenantId = tenantInfo.Id,
            TenantName = tenantInfo.Name,
            IsActive = tenantInfo.IsActive
        };
    }
}
```

## 租户策略

### 1. 共享数据库 (SharedDatabase)

所有租户共享同一个数据库和表结构，通过 `TenantId` 字段区分数据。

```csharp
public class Order : IMultiTenant
{
    public int Id { get; set; }
    public string TenantId { get; set; } // 租户ID字段
    public string ProductName { get; set; }
    public decimal Amount { get; set; }
}
```

### 2. 独立表结构 (SharedDatabaseSeparateSchema)

共享数据库，但每个租户使用独立的表（通过表名前缀区分）。

### 3. 独立数据库 (SeparateDatabase)

每个租户使用完全独立的数据库。

### 4. 混合模式 (Hybrid)

根据租户配置动态选择隔离策略。

## 租户解析

### 从HTTP Header解析

```http
GET /api/orders
TenantId: tenant-001
```

### 从Query参数解析

```http
GET /api/orders?tenantId=tenant-001
```

### 从子域名解析

```http
GET https://tenant-001.example.com/api/orders
```

### 从路径解析

```http
GET /tenant-001/api/orders
```

## 登录接口 JWT 扩展

### 自动租户信息注入

登录接口 (`AuthController.Login`) 已自动扩展支持多租户，在生成JWT时会包含以下租户信息：

#### JWT Claims扩展

```json
{
  "sub": "用户ID",
  "jti": "JWT唯一标识",
  "name": "用户姓名", 
  "nameid": "用户ID",
  "email": "用户邮箱",
  "unique_name": "用户名",
  "role": ["角色1", "角色2"],
  "TenantId": "租户ID",
  "iat": "签发时间",
  "exp": "过期时间"
}
```

#### 使用说明

1. **无需修改前端代码**：前端调用登录接口无需任何修改
2. **自动租户识别**：系统从用户的 `ApplicationUser.TenantId` 自动获取租户信息
3. **向后兼容**：现有未设置租户的用户不会受到影响

#### 登录请求示例

```http
POST /api/auth/login
Content-Type: application/json

{
  "userName": "admin@tenant001.com",
  "password": "password123"
}
```

#### 登录响应示例

```json
{
  "success": true,
  "message": "登录成功",
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "refresh_token_here",
    "user": {
      "id": 1,
      "userName": "admin@tenant001.com",
      "name": "管理员",
      "email": "admin@tenant001.com",
      "tenantId": "tenant-001"
    }
  }
}
```

#### 关键特性

- ✅ **自动识别**：根据用户所属租户自动在JWT中添加 `TenantId` 声明
- ✅ **性能优化**：避免循环依赖，直接从用户对象获取租户信息
- ✅ **安全性**：租户信息在JWT中加密存储，防止篡改
- ✅ **灵活性**：支持多种租户解析策略的兼容性

## ITenantContext 统一租户上下文服务

### 核心功能

`ITenantContext` 提供统一的租户信息获取方式，解决了在登录和免登录场景下租户ID获取的重复逻辑问题：

```csharp
public interface ITenantContext : IScopedDependency
{
    /// <summary>
    /// 获取当前租户ID
    /// 优先级：JWT Claims -> HTTP上下文 -> 默认租户
    /// </summary>
    string? TenantId { get; }

    /// <summary>
    /// 获取当前租户名称
    /// </summary>
    string? TenantName { get; }

    /// <summary>
    /// 获取当前租户信息
    /// </summary>
    Task<ITenantInfo?> GetCurrentTenantInfoAsync();

    /// <summary>
    /// 判断是否为指定租户
    /// </summary>
    bool IsInTenant(string tenantId);

    /// <summary>
    /// 判断当前是否有有效的租户上下文
    /// </summary>
    bool HasTenant { get; }

    /// <summary>
    /// 强制刷新租户上下文
    /// </summary>
    Task RefreshTenantContextAsync();
}
```

### 租户解析优先级

`ITenantContext` 按以下优先级获取租户信息：

1. **JWT Claims**：优先从用户的JWT声明中获取 `TenantId`（登录场景）
2. **ICurrentUser接口**：从扩展的ICurrentUser接口获取租户信息
3. **HTTP上下文**：从 `HttpContext.Items` 中获取（免登录场景，由多租户中间件设置）
4. **默认租户**：使用配置的默认租户ID

### 使用示例

#### 在控制器中使用

```csharp
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly ITenantContext _tenantContext;

    public OrdersController(ITenantContext tenantContext)
    {
        _tenantContext = tenantContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetOrders()
    {
        // 检查是否有租户上下文
        if (!_tenantContext.HasTenant)
        {
            return BadRequest("未找到租户上下文");
        }

        // 获取当前租户ID
        var tenantId = _tenantContext.TenantId;
        
        // 获取完整租户信息
        var tenantInfo = await _tenantContext.GetCurrentTenantInfoAsync();
        
        if (tenantInfo == null || !tenantInfo.IsActive)
        {
            return BadRequest("租户不存在或已禁用");
        }

        // 根据租户获取数据
        var orders = await GetOrdersByTenantAsync(tenantId);
        return Ok(orders);
    }

    [HttpPost("transfer/{targetTenantId}")]
    public async Task<IActionResult> TransferOrder(string targetTenantId, [FromBody] TransferOrderDto dto)
    {
        // 验证租户权限
        if (!_tenantContext.IsInTenant(dto.SourceTenantId))
        {
            return Forbid("无权限操作源租户");
        }

        // 执行跨租户操作
        await TransferOrderAsync(dto.OrderId, targetTenantId);
        return Ok();
    }
}
```

#### 在业务服务中使用

```csharp
public class OrderService
{
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<OrderService> _logger;

    public OrderService(ITenantContext tenantContext, ILogger<OrderService> logger)
    {
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<Order> CreateOrderAsync(CreateOrderDto dto)
    {
        var tenantId = _tenantContext.TenantId;
        
        if (string.IsNullOrEmpty(tenantId))
        {
            throw new InvalidOperationException("无法获取租户ID");
        }

        // 验证租户状态
        var tenantInfo = await _tenantContext.GetCurrentTenantInfoAsync();
        if (tenantInfo == null || !tenantInfo.IsActive)
        {
            throw new InvalidOperationException("租户不存在或已禁用");
        }

        var order = new Order
        {
            TenantId = tenantId, // 自动设置租户ID
            ProductName = dto.ProductName,
            Amount = dto.Amount,
            CreatedAt = DateTime.UtcNow
        };

        _logger.LogInformation("为租户 {TenantId} 创建订单", tenantId);
        return order;
    }

    public async Task<List<Order>> GetUserOrdersAsync()
    {
        var tenantId = _tenantContext.TenantId;
        
        if (string.IsNullOrEmpty(tenantId))
        {
            return new List<Order>();
        }

        // 自动按租户过滤数据
        return await _orderRepository
            .Where(o => o.TenantId == tenantId)
            .ToListAsync();
    }
}
```

### 免登录场景支持

`ITenantContext` 特别适用于免登录场景，如：

- 公开API接口
- Webhook处理
- 定时任务
- 系统内部调用

在这些场景下，租户信息通过HTTP Header、Query参数等方式传递，由多租户中间件解析并设置到HTTP上下文中，`ITenantContext` 会自动从上下文中获取。

### 性能优化

- **请求级缓存**：在单个请求中，租户信息会被缓存，避免重复解析
- **延迟加载**：租户信息只在首次访问时加载
- **刷新机制**：支持运行时刷新租户上下文

## ICurrentUser 多租户扩展

### 扩展功能

`ICurrentUser` 接口已扩展支持多租户功能，新增以下属性和方法：

```csharp
public interface ICurrentUser : IScopedDependency
{
    // 原有属性...
    long? Id { get; }
    string UserName { get; }
    string[] Roles { get; }
    bool IsAuthenticated { get; }
    IEnumerable<Claim> Claims { get; }
    HashSet<string> Permissions { get; }
    
    // 新增多租户属性
    /// <summary>
    /// 获取当前用户的租户ID
    /// </summary>
    string? TenantId { get; }
    
    /// <summary>
    /// 获取当前用户的租户名称
    /// </summary>
    string? TenantName { get; }
    
    // 新增多租户方法
    /// <summary>
    /// 判断用户是否属于指定租户
    /// </summary>
    bool IsInTenant(string tenantId);
}
```

### 使用示例

在业务服务中使用多租户用户上下文：

```csharp
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly ICurrentUser _currentUser;
    private readonly IOrderService _orderService;

    public OrdersController(ICurrentUser currentUser, IOrderService orderService)
    {
        _currentUser = currentUser;
        _orderService = orderService;
    }

    [HttpGet]
    public async Task<IActionResult> GetOrders()
    {
        // 获取当前用户的租户信息
        var tenantId = _currentUser.TenantId;
        var tenantName = _currentUser.TenantName;
        
        // 验证租户权限
        if (string.IsNullOrEmpty(tenantId))
        {
            return BadRequest("未指定租户");
        }
        
        // 获取当前租户的订单
        var orders = await _orderService.GetOrdersByTenantAsync(tenantId);
        return Ok(orders);
    }
    
    [HttpPost("transfer")]
    public async Task<IActionResult> TransferOrder([FromBody] TransferOrderDto dto)
    {
        // 检查目标租户权限
        if (!_currentUser.IsInTenant(dto.TargetTenantId))
        {
            return Forbid("无权限操作目标租户");
        }
        
        // 执行订单转移
        await _orderService.TransferOrderAsync(dto.OrderId, dto.TargetTenantId);
        return Ok();
    }
}
```

在业务服务中使用：

```csharp
public class OrderService : IOrderService
{
    private readonly ICurrentUser _currentUser;
    private readonly IRepository<Order> _orderRepository;

    public OrderService(ICurrentUser currentUser, IRepository<Order> orderRepository)
    {
        _currentUser = currentUser;
        _orderRepository = orderRepository;
    }

    public async Task<List<Order>> GetUserOrdersAsync()
    {
        // 自动使用当前用户的租户ID进行过滤
        var tenantId = _currentUser.TenantId;
        var userId = _currentUser.Id;
        
        return await _orderRepository
            .Where(o => o.TenantId == tenantId && o.UserId == userId)
            .ToListAsync();
    }
    
    public async Task<Order> CreateOrderAsync(CreateOrderDto dto)
    {
        var order = new Order
        {
            TenantId = _currentUser.TenantId, // 自动设置租户ID
            UserId = _currentUser.Id.Value,
            ProductName = dto.ProductName,
            Amount = dto.Amount,
            CreatedAt = DateTime.UtcNow
        };
        
        return await _orderRepository.AddAsync(order);
    }
}
```

### 权限缓存优化

扩展后的 `ICurrentUser` 实现会自动在权限缓存键中包含租户信息，确保不同租户的权限数据互不干扰：

```csharp
// 权限缓存键格式：UserPermissions:{UserId}:Tenant:{TenantId}
string cacheKey = $"UserPermissions:{Id.Value}:Tenant:{TenantId}";
```

### 租户解析优先级

`CurrentUser` 实现按以下优先级获取租户信息：

1. **JWT Claims**：优先从用户的JWT声明中获取 `TenantId` 和 `TenantName`
2. **HTTP上下文**：如果JWT中没有，则从 `HttpContext.Items` 中获取（多租户中间件设置）
3. **默认值**：如果都无法获取，返回 `null`

### 最佳实践

1. **JWT中包含租户信息**：在用户登录时将租户信息添加到JWT Claims中
2. **多层验证**：结合多租户中间件和JWT Claims进行双重验证
3. **权限隔离**：确保权限缓存包含租户维度
4. **数据过滤**：在数据访问层自动应用租户过滤
5. **审计日志**：记录跨租户操作的审计信息

## 使用示例和配置

完整的使用示例和配置请参考测试项目：

### 配置示例

- **开发环境配置**：[appsettings.development.example.json](../../Tests/Components/CodeSpirit.MultiTenant.Tests/Examples/appsettings.development.example.json)
- **API存储配置**：[appsettings.api-store.example.json](../../Tests/Components/CodeSpirit.MultiTenant.Tests/Examples/appsettings.api-store.example.json)
- **.NET Aspire配置**：[appsettings.aspire.example.json](../../Tests/Components/CodeSpirit.MultiTenant.Tests/Examples/appsettings.aspire.example.json)
- **Kubernetes配置**：[appsettings.k8s.example.json](../../Tests/Components/CodeSpirit.MultiTenant.Tests/Examples/appsettings.k8s.example.json)
- **生产环境配置**：[appsettings.production.example.json](../../Tests/Components/CodeSpirit.MultiTenant.Tests/Examples/appsettings.production.example.json)
- **详细配置说明**：[配置说明文档](../../Tests/Components/CodeSpirit.MultiTenant.Tests/Examples/README.md)

### 代码示例

- **基础示例**：[MultiTenantExample.cs](../../Tests/Components/CodeSpirit.MultiTenant.Tests/Examples/MultiTenantExample.cs)
- **单元测试**：[Tests目录](../../Tests/Components/CodeSpirit.MultiTenant.Tests/)

示例包含：
- 多租户服务配置
- 多租户实体定义
- 多租户数据库上下文
- 多租户业务服务
- 多租户控制器
- API存储配置示例

## 租户存储策略

### 统一存储模式

组件采用固定的三层存储架构，按照以下优先级自动获取租户信息：

1. **内存存储**：最快速的本地缓存，优先使用
2. **分布式缓存**：支持多实例共享的Redis等分布式缓存
3. **API存储**：通过HTTP API从远程服务获取租户信息

### 存储特性

- ✅ **智能选择**：自动检测服务类型，选择最优存储方式
- ✅ **高性能**：Identity服务直接访问数据库，其他服务内存优先
- ✅ **高可用性**：多层降级，单点故障自动切换
- ✅ **自动同步**：从API获取的数据自动同步到缓存层
- ✅ **简化配置**：无需手动选择存储类型，自动适配
- ✅ **故障恢复**：上层存储恢复后自动优先使用

### 智能存储选择

组件会自动检测当前服务类型并选择最合适的存储方式：

#### Identity服务（自动检测）
- **使用LocalTenantStore**：直接访问数据库，避免HTTP循环调用
- **检测方式**：
  - 程序集名称包含"Identity"
  - 当前目录路径包含"IdentityApi"
  - 存在IdentityApi相关的ApplicationDbContext类型
- **优势**：无需HTTP调用，性能更高，避免循环依赖

#### 其他服务
- **使用UnifiedTenantStore**：通过API调用获取租户信息
- **三层存储架构**：内存→分布式缓存→API
- **支持服务发现**：自动适配不同部署环境

### 配置示例

#### Identity服务配置
```json
{
  "MultiTenant": {
    "Enabled": true,
    "DefaultTenantId": "default",
    "ResolveFromHeader": true,
    "TenantHeaderName": "X-Tenant-Id",
    "EnableTenantCache": true,
    "CacheExpirationMinutes": 30
  },
  "MultiTenant:LocalStore": {
    "MaxActiveTenantsCount": 1000,
    "EnableCache": true,
    "CacheExpirationMinutes": 30
  }
}
```

#### 其他服务配置
```json
{
  "MultiTenant": {
    "Enabled": true,
    "DefaultTenantId": "default",
    "ResolveFromHeader": true,
    "TenantHeaderName": "X-Tenant-Id",
    "EnableTenantCache": true,
    "CacheExpirationMinutes": 30
  },
  "MultiTenant:ApiStore": {
    "BaseUrl": "http://identity-api",
    "Timeout": 30,
    "UseApiResponseFormat": true,
    "GetTenantEndpoint": "api/identity/internal/tenants/{tenantId}",
    "GetActiveTenantsEndpoint": "api/identity/internal/tenants/active"
  }
}
```

### 支持的部署环境

- **.NET Aspire**: `http://identityapi`
- **Kubernetes**: `http://identity-api.default.svc.cluster.local`  
- **Docker Compose**: `http://identity-api`
- **本地开发**: `http://localhost:5001`

### 自定义租户存储

如需完全自定义租户存储逻辑，可以实现 `ITenantStore` 接口：

```csharp
public class CustomTenantStore : ITenantStore
{
    private readonly ILogger<CustomTenantStore> _logger;

    public CustomTenantStore(ILogger<CustomTenantStore> logger)
    {
        _logger = logger;
    }

    public async Task<ITenantInfo?> GetTenantAsync(string tenantId)
    {
        // 实现自定义的租户获取逻辑
        // 例如：从配置文件、外部服务等获取
        return null;
    }

    // 实现其他接口方法...
}
```

然后在服务注册时替换默认实现：

```csharp
builder.Services.AddCodeSpiritMultiTenant(builder.Configuration);
builder.Services.Replace(ServiceDescriptor.Scoped<ITenantStore, CustomTenantStore>());
```

## 配置选项说明

### 基础配置（MultiTenant节）

| 配置项 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| `Enabled` | bool | true | 是否启用多租户功能 |
| `DefaultTenantId` | string | "default" | 默认租户ID |
| `ResolveFromHeader` | bool | true | 是否从HTTP Header解析租户 |
| `TenantHeaderName` | string | "X-Tenant-Id" | 租户Header名称 |
| `ResolveFromQuery` | bool | true | 是否从Query参数解析租户 |
| `TenantQueryName` | string | "tenantId" | 租户Query参数名称 |
| `ResolveFromSubdomain` | bool | false | 是否从子域名解析租户 |
| `ResolveFromPath` | bool | false | 是否从路径解析租户 |
| `TenantPathPrefix` | string | "tenant-" | 租户路径前缀 |
| `EnableTenantCache` | bool | true | 是否启用租户缓存 |
| `CacheExpirationMinutes` | int | 30 | 缓存过期时间（分钟） |

### 本地存储配置（MultiTenant:LocalStore节）
*适用于Identity服务，自动检测启用*

| 配置项 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| `MaxActiveTenantsCount` | int | 1000 | 最大活跃租户数量限制 |
| `EnableCache` | bool | true | 是否启用缓存 |
| `CacheExpirationMinutes` | int | 30 | 缓存过期时间（分钟） |

### API存储配置（MultiTenant:ApiStore节）
*适用于其他服务，自动检测启用*

| 配置项 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| `BaseUrl` | string | "http://identity" | API存储基础URL，支持服务发现 |
| `Timeout` | int | 2 | API请求超时时间（秒） |
| `UseApiResponseFormat` | bool | true | 是否使用ApiResponse格式 |
| `GetTenantEndpoint` | string | "api/identity/internal/tenants/{tenantId}" | 获取租户信息端点 |
| `GetActiveTenantsEndpoint` | string | "api/identity/internal/tenants/active" | 获取活跃租户列表端点 |
| `CreateTenantEndpoint` | string | "api/identity/internal/tenants" | 创建租户端点 |
| `UpdateTenantEndpoint` | string | "api/identity/internal/tenants/{tenantId}" | 更新租户端点 |
| `DeleteTenantEndpoint` | string | "api/identity/internal/tenants/{tenantId}" | 删除租户端点 |
| `CheckTenantExistsEndpoint` | string | "api/identity/internal/tenants/{tenantId}" | 检查租户是否存在端点 |

## 最佳实践

1. **智能存储**：组件自动检测服务类型，无需手动配置存储方式
2. **Identity服务**：自动使用LocalTenantStore，避免HTTP循环调用
3. **其他服务**：自动使用UnifiedTenantStore，通过API获取租户信息
4. **性能优化**：启用租户缓存以提升解析性能
5. **安全考虑**：验证租户权限，防止跨租户数据访问
6. **监控日志**：记录租户解析过程，便于问题排查
7. **数据备份**：为每个租户制定独立的备份策略
8. **扩展性**：设计时考虑租户数量增长的扩展性
9. **用户体验**：使用ICurrentUser扩展简化多租户开发
10. **权限隔离**：确保权限系统支持租户级别的隔离
11. **JWT优化**：在登录时包含租户信息，减少后续查询开销

## 解决方案亮点

### 🔄 自动循环调用检测
- **问题**：Identity服务调用自己的API会造成循环依赖
- **解决**：自动检测Identity服务，使用LocalTenantStore直接访问数据库
- **优势**：避免HTTP调用开销，提升性能，消除循环依赖

### 🎯 智能服务识别
- **多重检测**：程序集名称、目录路径、DbContext类型
- **自动适配**：无需手动配置，自动选择最优存储方式
- **向后兼容**：现有配置继续有效，无需修改

### ⚡ 性能优化
- **Identity服务**：直接数据库访问，无HTTP开销
- **其他服务**：三层缓存架构，最小化API调用
- **内存优先**：最快的访问速度，自动降级机制

## 许可证

MIT License 