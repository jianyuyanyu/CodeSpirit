# CodeSpirit 多租户数据库上下文架构

## 📚 **概述**

CodeSpirit提供了完整的多租户数据库上下文架构，支持：
- 🏗️ **分层设计**：AuditableDbContext → MultiTenantDbContext → 应用层DbContext
- 🔒 **自动租户隔离**：查询过滤和数据写入自动应用租户ID
- ⚡ **高性能**：租户ID缓存和智能解析策略
- 🎛️ **灵活配置**：支持多种租户解析策略和过滤模式
- 🔧 **易于扩展**：清晰的扩展点和钩子方法

## 🏗️ **架构设计**

### 层次结构
```
DbContext (EF Core基类)
    ↓
AuditableDbContext (审计功能)
    ↓  
MultiTenantDbContext (多租户功能)
    ↓
ApplicationDbContext (应用特定功能，如Identity)
```

### 核心组件

#### 1. AuditableDbContext
**位置**: `Src/CodeSpirit.Shared/Data/AuditableDbContext.cs`

**功能**:
- ✅ 自动审计字段设置（创建者、修改者、删除者、时间戳）
- ✅ 软删除支持
- ✅ 自动ID生成（长整型ID）
- ✅ 全局查询过滤器基础设施
- ✅ 实体事件发布

#### 2. MultiTenantDbContext
**位置**: `Src/CodeSpirit.Shared/Data/MultiTenantDbContext.cs`

**功能**:
- ✅ 租户ID自动解析和缓存
- ✅ 多租户数据过滤
- ✅ 新实体自动设置租户ID
- ✅ 跨租户操作支持
- ✅ 租户验证和安全检查

## 🚀 **快速开始**

### 1. 创建多租户DbContext

```csharp
public class YourDbContext : MultiTenantDbContext
{
    public DbSet<YourEntity> YourEntities { get; set; }

    public YourDbContext(
        DbContextOptions<YourDbContext> options,
        IServiceProvider serviceProvider,
        ICurrentUser currentUser,
        IHttpContextAccessor httpContextAccessor) 
        : base(options, serviceProvider, currentUser, httpContextAccessor)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder); // 重要：调用基类方法

        // 你的模型配置
        modelBuilder.Entity<YourEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            // 其他配置...
        });
    }
}
```

### 2. 实体实现多租户接口

```csharp
public class YourEntity : IMultiTenant, ICreationAuditable, ISoftDeleteAuditable
{
    public long Id { get; set; }
    
    // 多租户字段
    public string TenantId { get; set; } = null!;
    
    // 审计字段
    public long? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public long? DeletedBy { get; set; }
    public DateTime? DeletedAt { get; set; }
    
    // 业务字段
    public string Name { get; set; } = null!;
}
```

### 3. 配置服务

```csharp
// Program.cs 或 Startup.cs
builder.Services.AddDbContext<YourDbContext>(options =>
{
    options.UseSqlServer(connectionString);
});

// 注册必要服务
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddHttpContextAccessor();
```

### 4. 配置文件

```json
{
  "MultiTenant": {
    "Enabled": true,
    "DefaultTenantId": "default",
    "UnknownTenantStrategy": "UseDefault"
  }
}
```

## 🎛️ **配置选项**

### MultiTenantOptions

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `Enabled` | `bool` | `true` | 是否启用多租户功能 |
| `DefaultTenantId` | `string` | `"default"` | 默认租户ID |
| `UnknownTenantStrategy` | `enum` | `UseDefault` | 无法确定租户时的策略 |

### UnknownTenantStrategy 枚举

| 值 | 说明 | 安全性 |
|----|------|--------|
| `UseDefault` | 使用默认租户ID | ⭐⭐⭐ 推荐 |
| `AllowAll` | 允许访问所有数据 | ⚠️ 不安全 |
| `DenyAll` | 拒绝所有访问 | 🔒 最安全 |

## 🔍 **租户解析策略**

系统按以下优先级解析租户ID：

1. **JWT Claims** (`ICurrentUser.TenantId`)
   - 来源：用户登录时JWT中的租户信息
   - 优先级：🥇 最高

2. **HttpContext Items** (`HttpContext.Items["TenantId"]`)
   - 来源：多租户中间件设置
   - 优先级：🥈 中等

3. **默认租户ID** (`MultiTenantOptions.DefaultTenantId`)
   - 来源：配置文件
   - 优先级：🥉 最低

## 🛡️ **安全特性**

### 自动数据隔离
- ✅ **查询过滤**：自动添加租户过滤条件
- ✅ **写入隔离**：新实体自动设置租户ID
- ✅ **错误处理**：租户解析失败时的安全策略

### 跨租户操作
```csharp
// 临时禁用多租户过滤（管理员操作）
var allData = dbContext.WithoutMultiTenantFilter(() => 
{
    return dbContext.YourEntities.ToList();
});

// 使用指定租户ID执行操作
var tenantData = dbContext.WithTenant("tenant-123", () =>
{
    return dbContext.YourEntities.ToList();
});
```

## 🎯 **最佳实践**

### 1. 实体设计
```csharp
public abstract class MultiTenantEntity : IMultiTenant, ICreationAuditable
{
    public long Id { get; set; }
    public string TenantId { get; set; } = null!;
    public long? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class Order : MultiTenantEntity
{
    public string OrderNumber { get; set; } = null!;
    public decimal Amount { get; set; }
}
```

### 2. 服务层集成
```csharp
public class OrderService
{
    private readonly YourDbContext _context;

    public OrderService(YourDbContext context)
    {
        _context = context;
    }

    public async Task<List<Order>> GetOrdersAsync()
    {
        // 自动应用租户过滤，无需手动指定TenantId
        return await _context.Orders.ToListAsync();
    }

    public async Task<Order> CreateOrderAsync(Order order)
    {
        // TenantId会自动设置，但也可以显式指定
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        return order;
    }
}
```

### 3. 迁移管理
```csharp
// 为多租户实体添加索引
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    modelBuilder.Entity<Order>(entity =>
    {
        // 重要：为TenantId添加索引
        entity.HasIndex(e => e.TenantId)
              .HasDatabaseName("IX_Orders_TenantId");
              
        // 复合索引提升性能
        entity.HasIndex(e => new { e.TenantId, e.CreatedAt })
              .HasDatabaseName("IX_Orders_TenantId_CreatedAt");
    });
}
```

## 🔧 **扩展点**

### 自定义租户解析
```csharp
public class CustomDbContext : MultiTenantDbContext
{
    protected override string ResolveTenantId()
    {
        // 自定义租户解析逻辑
        var customTenantId = GetTenantIdFromCustomSource();
        return customTenantId ?? base.ResolveTenantId();
    }
}
```

### 自定义租户验证
```csharp
protected override void ValidateExplicitTenantId(string tenantId, string entityTypeName)
{
    // 自定义验证逻辑
    if (!IsValidTenant(tenantId))
    {
        throw new UnauthorizedAccessException($"无效的租户ID: {tenantId}");
    }
    
    base.ValidateExplicitTenantId(tenantId, entityTypeName);
}
```

## 📊 **性能优化**

### 1. 租户ID缓存
- 租户ID在单个请求内缓存，避免重复解析
- 线程安全的缓存实现

### 2. 查询优化
```sql
-- 自动生成的查询包含租户过滤
SELECT * FROM Orders 
WHERE TenantId = @tenantId AND IsDeleted = 0
```

### 3. 索引建议
```csharp
// 基础索引
entity.HasIndex(e => e.TenantId);

// 复合索引（根据查询模式调整）
entity.HasIndex(e => new { e.TenantId, e.CreatedAt });
entity.HasIndex(e => new { e.TenantId, e.IsDeleted });
```

## ⚠️ **注意事项**

1. **数据迁移**：现有数据需要设置TenantId
2. **备份恢复**：注意租户数据的完整性
3. **性能监控**：监控租户过滤对查询性能的影响
4. **测试覆盖**：确保多租户场景的测试覆盖

## 🆘 **故障排除**

### 常见问题

**Q: 查询返回空结果**
A: 检查租户ID是否正确解析，使用`WithoutMultiTenantFilter`测试

**Q: 新实体TenantId为空**
A: 确保已调用`base.SaveChangesAsync()`并检查租户解析逻辑

**Q: 性能问题**
A: 检查TenantId字段的索引，考虑复合索引

**Q: 循环依赖**
A: 避免在租户解析中引用需要多租户过滤的服务

---

**更多信息请参考**：
- [多租户组件文档](../Components/CodeSpirit.MultiTenant/README.md)
- [身份认证集成](../CodeSpirit.IdentityApi/README.md)
- [审计功能说明](./AuditableDbContext.md) 