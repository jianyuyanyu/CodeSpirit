# CodeSpirit.DataFilter 数据筛选器使用指南

## 📚 **概述**

CodeSpirit提供了强大而灵活的数据筛选器系统，可以动态控制数据访问和过滤规则。主要用于：
- 🏢 **多租户数据隔离**：自动过滤不同租户的数据
- 🗑️ **软删除过滤**：自动隐藏已删除的数据
- 👁️ **状态过滤**：根据实体状态控制数据可见性
- 🔧 **自定义过滤器**：支持业务特定的过滤规则

## 🏗️ **架构设计**

### 核心接口

```csharp
// 通用数据筛选器接口
public interface IDataFilter
{
    IDisposable Enable<TFilter>() where TFilter : class;
    IDisposable Disable<TFilter>() where TFilter : class;
    bool IsEnabled<TFilter>() where TFilter : class;
}

// 类型化数据筛选器接口
public interface IDataFilter<TFilter> where TFilter : class
{
    IDisposable Enable();
    IDisposable Disable();
    bool IsEnabled { get; }
}
```

### 内置筛选器类型

| 筛选器 | 接口 | 用途 |
|--------|------|------|
| 多租户筛选器 | `IMultiTenant` | 自动过滤租户数据 |
| 软删除筛选器 | `ISoftDeleteAuditable` | 隐藏已删除数据 |
| 状态筛选器 | `IIsActive` | 过滤活跃状态数据 |

## 🚀 **快速开始**

### 1. 服务注册

```csharp
// Program.cs
builder.Services.AddDataFilters();

// 或者手动配置默认状态
builder.Services.Configure<DataFilterOptions>(options =>
{
    options.DefaultStates[typeof(ISoftDeleteAuditable)] = new DataFilterState(isEnabled: true);
    options.DefaultStates[typeof(IMultiTenant)] = new DataFilterState(isEnabled: true);
    options.DefaultStates[typeof(IIsActive)] = new DataFilterState(isEnabled: true);
});
```

### 2. 在控制器中使用

```csharp
[ApiController]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IDataFilter _dataFilter;

    public UsersController(IUserService userService, IDataFilter dataFilter)
    {
        _userService = userService;
        _dataFilter = dataFilter;
    }

    [HttpGet]
    public async Task<List<UserDto>> GetUsers()
    {
        // 正常查询，自动应用所有启用的筛选器
        return await _userService.GetUsersAsync();
    }

    [HttpGet("with-deleted")]
    public async Task<List<UserDto>> GetUsersWithDeleted()
    {
        // 临时禁用软删除筛选器，查看包括已删除的用户
        using (_dataFilter.Disable<ISoftDeleteAuditable>())
        {
            return await _userService.GetUsersAsync();
        }
    }

    [HttpGet("all-tenants")]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<List<UserDto>> GetAllTenantsUsers()
    {
        // 系统管理员查看所有租户的用户
        using (_dataFilter.Disable<IMultiTenant>())
        {
            return await _userService.GetUsersAsync();
        }
    }
}
```

### 3. 在服务层中使用

```csharp
public class UserService : IUserService
{
    private readonly IRepository<User> _userRepository;
    private readonly IDataFilter _dataFilter;

    public UserService(IRepository<User> userRepository, IDataFilter dataFilter)
    {
        _userRepository = userRepository;
        _dataFilter = dataFilter;
    }

    public async Task<List<UserDto>> GetActiveUsersAsync()
    {
        // 临时启用状态筛选器
        using (_dataFilter.Enable<IIsActive>())
        {
            return await _userRepository.GetAllAsync();
        }
    }

    public async Task<UserDto> GetUserWithHistoryAsync(long userId)
    {
        // 禁用所有筛选器，获取完整的用户信息
        using (_dataFilter.Disable<ISoftDeleteAuditable>())
        using (_dataFilter.Disable<IIsActive>())
        {
            var user = await _userRepository.GetByIdAsync(userId);
            return _mapper.Map<UserDto>(user);
        }
    }
}
```

## 📋 **常用场景**

### 1. 多租户数据隔离

```csharp
// 租户平台：自动过滤当前租户数据
public async Task<List<OrderDto>> GetTenantOrders()
{
    // IMultiTenant 筛选器默认启用，自动过滤当前租户数据
    return await _orderService.GetOrdersAsync();
}

// 系统平台：查看所有租户数据
public async Task<List<OrderDto>> GetAllOrders()
{
    using (_dataFilter.Disable<IMultiTenant>())
    {
        return await _orderService.GetOrdersAsync();
    }
}
```

### 2. 软删除数据管理

```csharp
// 正常业务：只显示未删除数据
public async Task<List<ProductDto>> GetProducts()
{
    return await _productService.GetProductsAsync();
}

// 回收站：显示已删除数据
public async Task<List<ProductDto>> GetDeletedProducts()
{
    using (_dataFilter.Disable<ISoftDeleteAuditable>())
    {
        var allProducts = await _productService.GetProductsAsync();
        return allProducts.Where(p => p.IsDeleted).ToList();
    }
}

// 管理员：显示所有数据（包括已删除）
public async Task<List<ProductDto>> GetAllProducts()
{
    using (_dataFilter.Disable<ISoftDeleteAuditable>())
    {
        return await _productService.GetProductsAsync();
    }
}
```

### 3. 状态筛选

```csharp
// 只获取活跃用户
public async Task<List<UserDto>> GetActiveUsers()
{
    using (_dataFilter.Enable<IIsActive>())
    {
        return await _userService.GetUsersAsync();
    }
}

// 获取所有用户（包括非活跃）
public async Task<List<UserDto>> GetAllUsers()
{
    using (_dataFilter.Disable<IIsActive>())
    {
        return await _userService.GetUsersAsync();
    }
}
```

### 4. 组合筛选器

```csharp
// 获取指定租户的所有数据（包括已删除和非活跃）
public async Task<List<DataDto>> GetTenantAllData(string tenantId)
{
    using (_dataFilter.Disable<ISoftDeleteAuditable>())
    using (_dataFilter.Disable<IIsActive>())
    {
        // 仍然保持多租户筛选，但禁用其他筛选器
        return await _dataService.GetDataAsync();
    }
}

// 系统管理员获取真正的所有数据
public async Task<List<DataDto>> GetSystemAllData()
{
    using (_dataFilter.Disable<IMultiTenant>())
    using (_dataFilter.Disable<ISoftDeleteAuditable>())
    using (_dataFilter.Disable<IIsActive>())
    {
        return await _dataService.GetDataAsync();
    }
}
```

## ⚙️ **高级用法**

### 1. 检查筛选器状态

```csharp
public async Task<ApiResponse> GetFilterStatus()
{
    var status = new
    {
        MultiTenantEnabled = _dataFilter.IsEnabled<IMultiTenant>(),
        SoftDeleteEnabled = _dataFilter.IsEnabled<ISoftDeleteAuditable>(),
        ActiveStateEnabled = _dataFilter.IsEnabled<IIsActive>()
    };
    
    return ApiResponse.Success(status);
}
```

### 2. 嵌套筛选器控制

```csharp
public async Task<ComplexDataDto> GetComplexData(bool includeDeleted, bool allTenants)
{
    IDisposable softDeleteScope = null;
    IDisposable multiTenantScope = null;
    
    try
    {
        if (includeDeleted)
            softDeleteScope = _dataFilter.Disable<ISoftDeleteAuditable>();
            
        if (allTenants)
            multiTenantScope = _dataFilter.Disable<IMultiTenant>();
            
        return await _dataService.GetComplexDataAsync();
    }
    finally
    {
        softDeleteScope?.Dispose();
        multiTenantScope?.Dispose();
    }
}
```

### 3. 条件筛选器控制

```csharp
public async Task<List<UserDto>> GetUsers(bool includeInactive, bool includeDeleted)
{
    var disposables = new List<IDisposable>();
    
    try
    {
        if (includeInactive)
            disposables.Add(_dataFilter.Disable<IIsActive>());
            
        if (includeDeleted)
            disposables.Add(_dataFilter.Disable<ISoftDeleteAuditable>());
            
        return await _userService.GetUsersAsync();
    }
    finally
    {
        disposables.ForEach(d => d.Dispose());
    }
}
```

## 🔧 **自定义筛选器**

### 1. 定义筛选器接口

```csharp
// 定义自定义筛选器接口
public interface IOrganizationLevel
{
    int OrganizationLevel { get; set; }
}
```

### 2. 实体实现接口

```csharp
public class Employee : IOrganizationLevel, IMultiTenant
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string TenantId { get; set; }
    public int OrganizationLevel { get; set; } // 组织层级
}
```

### 3. 配置筛选器

```csharp
// 注册自定义筛选器
builder.Services.Configure<DataFilterOptions>(options =>
{
    options.DefaultStates[typeof(IOrganizationLevel)] = new DataFilterState(isEnabled: false);
});
```

### 4. 在 DbContext 中实现筛选逻辑

```csharp
protected override Expression<Func<TEntity, bool>> CreateFilterExpression<TEntity>()
{
    var expression = base.CreateFilterExpression<TEntity>();

    // 添加组织层级筛选
    if (typeof(IOrganizationLevel).IsAssignableFrom(typeof(TEntity)) && 
        DataFilter?.IsEnabled<IOrganizationLevel>() == true)
    {
        var currentUserLevel = _currentUser?.OrganizationLevel ?? 0;
        Expression<Func<TEntity, bool>> orgFilter = 
            e => EF.Property<int>(e, "OrganizationLevel") >= currentUserLevel;
        
        expression = expression != null
            ? CombineExpressions(expression, orgFilter)
            : orgFilter;
    }

    return expression;
}
```

### 5. 使用自定义筛选器

```csharp
// 启用组织层级筛选，只显示当前用户级别及以下的数据
public async Task<List<EmployeeDto>> GetSubordinateEmployees()
{
    using (_dataFilter.Enable<IOrganizationLevel>())
    {
        return await _employeeService.GetEmployeesAsync();
    }
}
```

## 🎯 **最佳实践**

### 1. 控制器层面的筛选器管理

```csharp
[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly IDataFilter _dataFilter;

    public AdminController(IDataFilter dataFilter)
    {
        _dataFilter = dataFilter;
    }

    [HttpGet("system-data")]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<IActionResult> GetSystemData()
    {
        // 系统管理员可以跨租户查看数据
        using (_dataFilter.Disable<IMultiTenant>())
        {
            var data = await _dataService.GetAllDataAsync();
            return Ok(data);
        }
    }
}
```

### 2. 服务层面的筛选器封装

```csharp
public class AdminUserService : IAdminUserService
{
    private readonly IUserService _userService;
    private readonly IDataFilter _dataFilter;

    public AdminUserService(IUserService userService, IDataFilter dataFilter)
    {
        _userService = userService;
        _dataFilter = dataFilter;
    }

    public async Task<List<UserDto>> GetAllUsersIncludingDeletedAsync()
    {
        using (_dataFilter.Disable<ISoftDeleteAuditable>())
        {
            return await _userService.GetUsersAsync();
        }
    }

    public async Task<List<UserDto>> GetCrossTenantUsersAsync()
    {
        using (_dataFilter.Disable<IMultiTenant>())
        {
            return await _userService.GetUsersAsync();
        }
    }
}
```

### 3. 扩展方法简化使用

```csharp
public static class DataFilterExtensions
{
    public static async Task<T> WithoutSoftDeleteAsync<T>(this IDataFilter dataFilter, Func<Task<T>> operation)
    {
        using (dataFilter.Disable<ISoftDeleteAuditable>())
        {
            return await operation();
        }
    }

    public static async Task<T> WithoutMultiTenantAsync<T>(this IDataFilter dataFilter, Func<Task<T>> operation)
    {
        using (dataFilter.Disable<IMultiTenant>())
        {
            return await operation();
        }
    }

    public static async Task<T> WithoutAllFiltersAsync<T>(this IDataFilter dataFilter, Func<Task<T>> operation)
    {
        using (dataFilter.Disable<IMultiTenant>())
        using (dataFilter.Disable<ISoftDeleteAuditable>())
        using (dataFilter.Disable<IIsActive>())
        {
            return await operation();
        }
    }
}

// 使用扩展方法
public async Task<List<UserDto>> GetDeletedUsers()
{
    return await _dataFilter.WithoutSoftDeleteAsync(async () =>
    {
        return await _userService.GetUsersAsync();
    });
}
```

## ⚠️ **注意事项**

### 1. 筛选器作用域

- ✅ **线程安全**：筛选器状态是线程隔离的
- ✅ **异步安全**：支持 async/await 模式
- ⚠️ **作用域限制**：筛选器状态仅在当前执行上下文中有效

### 2. 性能考虑

- ✅ **查询优化**：筛选器会被编译到 SQL 查询中
- ✅ **索引友好**：确保筛选字段有适当的索引
- ⚠️ **避免滥用**：不要在高频操作中频繁切换筛选器状态

### 3. 安全性

- 🔒 **权限验证**：在禁用筛选器前进行权限检查
- 🔒 **审计记录**：记录筛选器的禁用操作
- 🔒 **最小权限**：仅在必要时禁用筛选器

```csharp
[HttpGet("all-tenants")]
[Authorize(Roles = "SystemAdmin")] // 确保只有系统管理员可以访问
public async Task<IActionResult> GetAllTenantsData()
{
    // 记录管理员跨租户访问
    _logger.LogWarning("用户 {UserId} 执行跨租户数据访问", _currentUser.Id);
    
    using (_dataFilter.Disable<IMultiTenant>())
    {
        var data = await _dataService.GetAllDataAsync();
        return Ok(data);
    }
}
```

## 📚 **相关文档**

- [CodeSpirit 多租户数据库上下文架构](./CodeSpirit%20多租户数据库上下文架构.md)
- [CodeSpirit.Authorization权限组件详解](./CodeSpirit.Authorization权限组件详解.md)
- [CodeSpirit.Audit审计组件集成使用指南](./CodeSpirit.Audit审计组件集成使用指南.md)

## 🔗 **相关组件**

- **多租户组件**：提供租户解析和隔离
- **权限组件**：控制筛选器的使用权限
- **审计组件**：记录筛选器操作日志 