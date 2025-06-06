# CodeSpirit.MultiTenant

CodeSpirit多租户组件，提供灵活的多租户数据隔离解决方案。

## 功能特性

- 🏢 **多种租户策略**：支持共享数据库、独立表结构、独立数据库等多种隔离策略
- 🔍 **灵活的租户解析**：支持从Header、Query参数、子域名、路径等多种方式解析租户
- 💾 **多种存储方式**：支持数据库、内存、配置文件等多种租户信息存储方式
- ⚡ **高性能缓存**：内置分布式缓存支持，提升租户解析性能
- 🔧 **易于配置**：提供丰富的配置选项，满足不同场景需求
- 🧪 **完整测试**：包含完整的单元测试，确保组件稳定性
- 👤 **用户上下文集成**：扩展ICurrentUser接口，提供完整的多租户用户上下文
- 🔐 **JWT集成**：登录接口自动在JWT中包含租户信息

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
    "StoreType": "Memory",
    "EnableTenantCache": true,
    "CacheExpirationMinutes": 30
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

## 租户存储类型

### API存储（推荐）

通过HTTP API调用获取租户信息，适合集中式租户管理和内部服务通信：

```json
{
  "MultiTenant": {
    "StoreType": "Api",
    "ApiStore": {
      "BaseUrl": "http://identity-api",
      "Timeout": 30,
      "UseApiResponseFormat": true,
      "GetTenantEndpoint": "api/tenants/{tenantId}",
      "GetActiveTenantsEndpoint": "api/tenants/active"
    }
  }
}
```

支持多种部署环境的服务发现：
- **.NET Aspire**: `http://identityapi`
- **Kubernetes**: `http://identity-api.default.svc.cluster.local`
- **Docker Compose**: `http://identity-api`
- **本地开发**: `http://localhost:5001`

### 内存存储

适合开发和测试环境：

```json
{
  "MultiTenant": {
    "StoreType": "Memory"
  }
}
```

### 自定义租户存储

实现 `ITenantStore` 接口来自定义租户存储：

```csharp
public class DatabaseTenantStore : ITenantStore
{
    private readonly IDbContextFactory<TenantDbContext> _dbContextFactory;

    public DatabaseTenantStore(IDbContextFactory<TenantDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<ITenantInfo?> GetTenantAsync(string tenantId)
    {
        using var context = _dbContextFactory.CreateDbContext();
        return await context.Tenants
            .Where(t => t.TenantId == tenantId && t.IsActive)
            .FirstOrDefaultAsync();
    }

    // 实现其他接口方法...
}
```

然后在服务注册时替换默认实现：

```csharp
builder.Services.AddCodeSpiritMultiTenant(builder.Configuration);
builder.Services.Replace(ServiceDescriptor.Singleton<ITenantStore, DatabaseTenantStore>());
```

## 配置选项说明

| 配置项 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| `Enabled` | bool | true | 是否启用多租户功能 |
| `DefaultTenantId` | string | "default" | 默认租户ID |
| `ResolveFromHeader` | bool | true | 是否从HTTP Header解析租户 |
| `TenantHeaderName` | string | "TenantId" | 租户Header名称 |
| `ResolveFromQuery` | bool | true | 是否从Query参数解析租户 |
| `TenantQueryName` | string | "tenantId" | 租户Query参数名称 |
| `ResolveFromSubdomain` | bool | false | 是否从子域名解析租户 |
| `ResolveFromPath` | bool | false | 是否从路径解析租户 |
| `TenantPathPrefix` | string | "tenant-" | 租户路径前缀 |
| `StoreType` | enum | Database | 租户存储类型（Memory/ConfigFile/Database/Api） |
| `EnableTenantCache` | bool | true | 是否启用租户缓存 |
| `CacheExpirationMinutes` | int | 30 | 缓存过期时间（分钟） |
| `ApiStore.BaseUrl` | string | "" | API存储基础URL，支持服务发现（当StoreType为Api时使用） |
| `ApiStore.Timeout` | int | 30 | API请求超时时间（秒） |
| `ApiStore.UseApiResponseFormat` | bool | true | 是否使用ApiResponse格式 |

## 最佳实践

1. **性能优化**：启用租户缓存以提升解析性能
2. **安全考虑**：验证租户权限，防止跨租户数据访问
3. **监控日志**：记录租户解析过程，便于问题排查
4. **数据备份**：为每个租户制定独立的备份策略
5. **扩展性**：设计时考虑租户数量增长的扩展性
6. **用户体验**：使用ICurrentUser扩展简化多租户开发
7. **权限隔离**：确保权限系统支持租户级别的隔离
8. **JWT优化**：在登录时包含租户信息，减少后续查询开销

## 许可证

MIT License 