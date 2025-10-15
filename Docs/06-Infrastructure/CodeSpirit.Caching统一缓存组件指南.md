# CodeSpirit.Caching 统一缓存组件指南

## 📋 目录

- [概述](#概述)
- [核心特性](#核心特性)
- [快速开始](#快速开始)
- [核心概念](#核心概念)
- [配置说明](#配置说明)
- [使用示例](#使用示例)
- [最佳实践](#最佳实践)
- [API参考](#api参考)
- [常见问题](#常见问题)

## 概述

`CodeSpirit.Caching` 是一个基于 .NET 9 的统一缓存组件，旨在简化分布式系统中的缓存管理。它提供了多级缓存、缓存穿透防护、缓存预热和灵活的过期策略等功能，帮助开发者轻松构建高性能的缓存架构。

### 技术栈

- .NET 9.0
- Microsoft.Extensions.Caching.Memory (L1缓存)
- Microsoft.Extensions.Caching.StackExchangeRedis (L2缓存)
- StackExchange.Redis (Redis客户端)
- Newtonsoft.Json (序列化)

## 核心特性

### 1. 多级缓存策略

支持两级缓存架构：

- **L1缓存（内存缓存）**：毫秒级访问速度，适合热点数据
- **L2缓存（分布式缓存）**：Redis实现，支持跨实例共享

```
请求流程:
查询 L1 → L1命中返回
       → L1未命中 → 查询 L2 → L2命中 → 回写L1 → 返回
                         → L2未命中 → 查询数据源 → 写入L2 → 写入L1 → 返回
```

### 2. 缓存穿透防护

通过Redis分布式锁防止缓存击穿：

- 当多个请求同时访问未缓存的数据时，只有一个请求会去查询数据源
- 其他请求等待锁释放后直接从缓存获取数据
- 支持锁超时和重试机制

### 3. 缓存预热功能

支持在应用启动时或按需预热缓存：

- 单个缓存项预热
- 批量预热
- 并发预热控制
- 预热状态跟踪

### 4. 灵活的过期策略

支持多种过期策略：

- **绝对过期时间**：指定固定的过期时间点
- **相对过期时间**：从当前时间开始的过期时长
- **滑动过期时间**：访问后重新计算过期时间
- **分级过期配置**：L1和L2可设置不同的过期时间

### 5. 缓存键管理

提供统一的缓存键生成策略：

- 自动添加应用前缀
- 支持多租户缓存隔离
- 支持用户级缓存隔离
- 键格式验证

## 快速开始

### 1. 安装依赖

在项目中添加对 `CodeSpirit.Caching` 的引用：

```xml
<ItemGroup>
  <ProjectReference Include="..\..\Components\CodeSpirit.Caching\CodeSpirit.Caching.csproj" />
</ItemGroup>
```

### 2. 配置服务

#### 方式一：代码配置

```csharp
services.AddCodeSpiritCaching(options =>
{
    options.EnableL1Cache = true;
    options.EnableL2Cache = true;
    options.DefaultExpiration = TimeSpan.FromMinutes(30);
    options.KeyPrefix = "MyApp:Cache:";
});
```

#### 方式二：配置文件

在 `appsettings.json` 中添加配置：

```json
{
  "Caching": {
    "EnableL1Cache": true,
    "EnableL2Cache": true,
    "DefaultExpiration": "00:30:00",
    "KeyPrefix": "MyApp:Cache:",
    "EnableBreakthroughProtection": true,
    "LockTimeout": "00:00:30",
    "L1Cache": {
      "SizeLimit": 100,
      "CompactionPercentage": 0.25,
      "ExpirationScanFrequency": "00:01:00"
    },
    "L2Cache": {
      "ConnectionName": "cache",
      "Database": 0,
      "KeyPrefix": "L2:"
    }
  }
}
```

然后在代码中使用：

```csharp
services.AddCodeSpiritCaching(configuration.GetSection("Caching"));
```

### 3. 添加Redis配置

```csharp
services.AddRedisDistributedCacheAndLock("localhost:6379");
```

### 4. 基本使用

```csharp
public class UserService
{
    private readonly ICacheService _cache;

    public UserService(ICacheService cache)
    {
        _cache = cache;
    }

    public async Task<UserDto?> GetUserAsync(long userId)
    {
        var key = _cache.CreateKey("user", userId);
        
        return await _cache.GetOrSetAsync(
            key,
            async () => await LoadUserFromDatabaseAsync(userId),
            new CacheOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
                Level = CacheLevel.Both
            });
    }
}
```

## 核心概念

### ICacheService

统一缓存服务接口，提供以下核心方法：

- `GetAsync<T>`: 获取缓存值
- `GetOrSetAsync<T>`: 获取或设置缓存
- `SetAsync<T>`: 设置缓存值
- `RemoveAsync`: 删除缓存
- `ExistsAsync`: 检查缓存是否存在
- `CreateKey`: 生成缓存键

### CacheLevel

缓存级别枚举：

- `L1Only`: 仅使用内存缓存
- `L2Only`: 仅使用分布式缓存
- `Both`: 同时使用两级缓存
- `Auto`: 自动选择最佳级别

### CacheOptions

缓存选项配置：

```csharp
public class CacheOptions
{
    public CacheLevel Level { get; set; }
    public TimeSpan? AbsoluteExpirationRelativeToNow { get; set; }
    public TimeSpan? SlidingExpiration { get; set; }
    public TimeSpan? L1Expiration { get; set; }
    public TimeSpan? L2Expiration { get; set; }
    public bool EnableBreakthroughProtection { get; set; }
    public CachePriority Priority { get; set; }
    public List<string> Tags { get; set; }
}
```

## 配置说明

### 全局配置选项

| 配置项 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| EnableL1Cache | bool | true | 是否启用L1缓存 |
| EnableL2Cache | bool | true | 是否启用L2缓存 |
| DefaultExpiration | TimeSpan | 30分钟 | 默认缓存过期时间 |
| DefaultL1Expiration | TimeSpan | 5分钟 | L1缓存默认过期时间 |
| DefaultL2Expiration | TimeSpan | 30分钟 | L2缓存默认过期时间 |
| KeyPrefix | string | "CodeSpirit:Cache:" | 缓存键前缀 |
| EnableBreakthroughProtection | bool | true | 启用缓存击穿保护 |
| LockTimeout | TimeSpan | 30秒 | 分布式锁超时时间 |

### L1缓存配置

| 配置项 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| SizeLimit | int | 100 | 内存缓存大小限制(MB) |
| CompactionPercentage | double | 0.25 | 压缩比例 |
| ExpirationScanFrequency | TimeSpan | 1分钟 | 过期扫描频率 |

### L2缓存配置

| 配置项 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| ConnectionName | string | "cache" | Redis连接名称 |
| Database | int | 0 | Redis数据库编号 |
| KeyPrefix | string | "L2:" | L2缓存键前缀 |

## 使用示例

### 示例1：基本缓存操作

```csharp
public class ProductService
{
    private readonly ICacheService _cache;
    private readonly IProductRepository _repository;

    public async Task<ProductDto?> GetProductAsync(long productId)
    {
        var key = _cache.CreateKey("product", productId);
        
        // 获取或设置缓存
        return await _cache.GetOrSetAsync(
            key,
            async () => await _repository.GetByIdAsync(productId),
            new CacheOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
            });
    }

    public async Task UpdateProductAsync(long productId, ProductDto product)
    {
        await _repository.UpdateAsync(productId, product);
        
        // 更新后清除缓存
        var key = _cache.CreateKey("product", productId);
        await _cache.RemoveAsync(key);
    }
}
```

### 示例2：用户级缓存

```csharp
public class UserPreferenceService
{
    private readonly ICacheService _cache;

    public async Task<UserPreferences?> GetPreferencesAsync(long userId)
    {
        // 使用用户级缓存键
        var key = _cache.CreateUserKey(userId, "preferences");
        
        return await _cache.GetOrSetAsync(
            key,
            async () => await LoadPreferencesFromDbAsync(userId),
            new CacheOptions
            {
                SlidingExpiration = TimeSpan.FromMinutes(15),
                Level = CacheLevel.Both
            });
    }
}
```

### 示例3：租户隔离缓存

```csharp
public class TenantConfigService
{
    private readonly ICacheService _cache;
    private readonly ICacheKeyGenerator _keyGenerator;

    public async Task<TenantConfig?> GetConfigAsync(string tenantId)
    {
        // 使用租户级缓存键
        var key = _keyGenerator.GenerateTenantKey(tenantId, "config");
        
        return await _cache.GetOrSetAsync(
            key,
            async () => await LoadTenantConfigAsync(tenantId),
            new CacheOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1),
                Level = CacheLevel.L2Only // 租户配置只缓存在L2
            });
    }
}
```

### 示例4：强类型缓存键（推荐）

使用强类型缓存键可以避免字符串拼接错误，提供编译时类型检查和IDE智能提示支持。

#### 定义强类型缓存键

```csharp
/// <summary>
/// 产品相关的强类型缓存键
/// </summary>
public static class ProductCaches
{
    /// <summary>
    /// 产品详情缓存键
    /// </summary>
    public record Detail(long Id) : ICacheKey<ProductDto>
    {
        // 使用 nameof 确保类型安全，使用下划线连接避免冒号被规范化
        public string Key => $"{nameof(ProductCaches)}_{nameof(Detail)}_{Id}";
        
        public CacheOptions Options => new()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
            SlidingExpiration = TimeSpan.FromMinutes(10),
            Level = CacheLevel.Both
        };
        
        public IReadOnlyList<string> Tags => [$"product:{Id}"];
    }
    
    /// <summary>
    /// 产品列表缓存键
    /// </summary>
    public record List(int CategoryId, int Page) : ICacheKey<List<ProductDto>>
    {
        public string Key => $"{nameof(ProductCaches)}_{nameof(List)}_{CategoryId}_{Page}";
        
        public CacheOptions Options => new()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15),
            Level = CacheLevel.Both
        };
        
        public IReadOnlyList<string> Tags => [$"category:{CategoryId}", "product-list"];
    }
}
```

#### 使用强类型缓存键

```csharp
public class ProductService
{
    private readonly ICacheService _cache;
    private readonly IProductRepository _repository;

    // ✅ 新方式：使用强类型键 - 简洁、安全、易维护
    public async Task<ProductDto?> GetProductAsync(long productId)
    {
        return await _cache.GetOrSetAsync(
            new ProductCaches.Detail(productId),  // 强类型键，IDE智能提示
            async () => await _repository.GetByIdAsync(productId));
    }

    public async Task<List<ProductDto>> GetProductListAsync(int categoryId, int page)
    {
        return await _cache.GetOrSetAsync(
            new ProductCaches.List(categoryId, page),
            async () => await _repository.GetPagedListAsync(categoryId, page));
    }

    public async Task UpdateProductAsync(long productId, ProductDto product)
    {
        await _repository.UpdateAsync(productId, product);
        
        // 清除缓存也很简单
        await _cache.RemoveAsync(new ProductCaches.Detail(productId));
    }
}
```

#### 强类型键 vs 字符串键对比

| 特性 | 字符串键 | 强类型键 |
|------|---------|---------|
| 类型安全 | ❌ 运行时错误 | ✅ 编译时检查 |
| 易出错性 | ⚠️ 拼写错误 | ✅ 编译器保证 |
| IDE支持 | ⚠️ 无智能提示 | ✅ 智能提示 |
| 配置管理 | ⚠️ 分散在多处 | ✅ 集中定义 |
| 代码量 | 较多 | 大幅减少 |
| 可维护性 | ⚠️ 需要多处修改 | ✅ 单点修改 |

```csharp
// ❌ 旧方式：字符串键 - 容易出错，配置分散
var key = _cache.CreateKey("product:detail", productId);
return await _cache.GetOrSetAsync(
    key,
    async () => await _repository.GetByIdAsync(productId),
    new CacheOptions  // 配置分散，容易遗漏
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
        SlidingExpiration = TimeSpan.FromMinutes(10),
        Level = CacheLevel.Both,
        Tags = { $"product:{productId}" }
    });

// ✅ 新方式：强类型键 - 简洁安全，配置集中
return await _cache.GetOrSetAsync(
    new ProductCaches.Detail(productId),  // 配置已在键定义中，类型安全
    async () => await _repository.GetByIdAsync(productId));
```

### 示例5：缓存预热

```csharp
public class CacheWarmupHostedService : IHostedService
{
    private readonly ICacheWarmupService _warmupService;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // 预热热门商品
        var warmupItems = new List<CacheWarmupItem>();
        
        foreach (var productId in hotProductIds)
        {
            warmupItems.Add(CacheWarmupItem.Create(
                $"product:{productId}",
                async () => await LoadProductAsync(productId),
                new CacheOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
                }
            ));
        }

        await _warmupService.WarmupBatchAsync(warmupItems, cancellationToken);
    }
}
```

### 示例5：分级过期策略

```csharp
public class ArticleService
{
    private readonly ICacheService _cache;

    public async Task<ArticleDto?> GetArticleAsync(long articleId)
    {
        var key = _cache.CreateKey("article", articleId);
        
        return await _cache.GetOrSetAsync(
            key,
            async () => await LoadArticleAsync(articleId),
            new CacheOptions
            {
                Level = CacheLevel.Both,
                L1Expiration = TimeSpan.FromMinutes(5),  // L1缓存5分钟
                L2Expiration = TimeSpan.FromHours(1),    // L2缓存1小时
                EnableBreakthroughProtection = true
            });
    }
}
```

### 示例6：仅L1缓存

```csharp
public class SessionService
{
    private readonly ICacheService _cache;

    public async Task<SessionData?> GetSessionAsync(string sessionId)
    {
        var key = _cache.CreateKey("session", sessionId);
        
        // 会话数据只缓存在本地内存
        return await _cache.GetOrSetAsync(
            key,
            async () => await LoadSessionAsync(sessionId),
            new CacheOptions
            {
                Level = CacheLevel.L1Only,
                SlidingExpiration = TimeSpan.FromMinutes(20)
            });
    }
}
```

## 最佳实践

### 1. 缓存键命名规范

```csharp
// ✅ 推荐：使用分层命名
_cache.CreateKey("user", userId);
_cache.CreateKey("user", userId, "profile");
_cache.CreateKey("product", "category", categoryId);

// ❌ 不推荐：手动拼接键
var key = $"user:{userId}"; // 缺少前缀，不利于管理
```

### 2. 选择合适的缓存级别

```csharp
// 热点数据：两级缓存
Level = CacheLevel.Both

// 个人数据：仅L1缓存
Level = CacheLevel.L1Only

// 共享配置：仅L2缓存
Level = CacheLevel.L2Only
```

### 3. 设置合理的过期时间

```csharp
// 静态数据：长过期时间
AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)

// 动态数据：短过期时间 + 滑动过期
SlidingExpiration = TimeSpan.FromMinutes(5)

// 热点数据：L1短过期，L2长过期
L1Expiration = TimeSpan.FromMinutes(5)
L2Expiration = TimeSpan.FromHours(1)
```

### 4. 及时清理缓存

```csharp
// 数据更新后立即清理
public async Task UpdateUserAsync(UserDto user)
{
    await _repository.UpdateAsync(user);
    
    // 清理相关缓存
    await _cache.RemoveAsync(_cache.CreateKey("user", user.Id));
    await _cache.RemoveAsync(_cache.CreateKey("user", user.Id, "profile"));
}
```

### 5. 使用缓存标签

```csharp
var options = new CacheOptions
{
    Tags = new List<string> { "user", "profile", $"user:{userId}" }
};

// 批量清理同一标签的缓存
await _cache.RemoveByTagAsync("user");
```

## API参考

### ICacheService

```csharp
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    
    Task<T> GetOrSetAsync<T>(
        string key, 
        Func<Task<T>> factory, 
        CacheOptions? options = null, 
        CancellationToken cancellationToken = default);
    
    Task SetAsync<T>(
        string key, 
        T value, 
        CacheOptions? options = null, 
        CancellationToken cancellationToken = default);
    
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
    
    string CreateKey(params object[] segments);
    
    string CreateUserKey(long userId, params object[] segments);
    
    Task RemoveByTagAsync(string tag, CancellationToken cancellationToken = default);
}
```

### ICacheWarmupService

```csharp
public interface ICacheWarmupService
{
    Task WarmupAsync<T>(
        string key, 
        Func<Task<T>> factory, 
        CacheOptions? options = null, 
        CancellationToken cancellationToken = default);
    
    Task WarmupBatchAsync(
        IEnumerable<CacheWarmupItem> items, 
        CancellationToken cancellationToken = default);
    
    CacheWarmupStatus? GetWarmupStatus(string key);
    
    Dictionary<string, CacheWarmupStatus> GetAllWarmupStatus();
}
```

### ICacheKeyGenerator

```csharp
public interface ICacheKeyGenerator
{
    string GenerateKey(string prefix, params object[] parts);
    
    string GenerateTenantKey(string tenantId, string prefix, params object[] parts);
    
    string GenerateUserKey(long userId, string prefix, params object[] parts);
    
    bool ValidateKey(string key);
    
    string ExtractPrefix(string key);
}
```

## 常见问题

### Q1: 如何监控缓存命中率？

A: 启用缓存统计功能：

```csharp
options.EnableStatistics = true;
```

### Q2: 如何处理缓存雪崩？

A: 使用随机过期时间：

```csharp
var randomOffset = TimeSpan.FromSeconds(Random.Shared.Next(0, 60));
var options = new CacheOptions
{
    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30) + randomOffset
};
```

### Q3: 如何在多租户环境中隔离缓存？

A: 使用租户级缓存键：

```csharp
var key = _keyGenerator.GenerateTenantKey(tenantId, "data", dataId);
```

### Q4: 如何禁用缓存击穿保护？

A: 设置 `EnableBreakthroughProtection` 为 `false`：

```csharp
var options = new CacheOptions
{
    EnableBreakthroughProtection = false
};
```

### Q5: 缓存预热失败怎么办？

A: 预热服务会自动重试，可通过以下方式查看状态：

```csharp
var status = _warmupService.GetWarmupStatus("your-cache-key");
if (status?.State == WarmupState.Failed)
{
    _logger.LogError("预热失败: {Error}", status.ErrorMessage);
}
```

### Q6: 如何测试缓存功能？

A: 使用Mock对象：

```csharp
var mockCache = new Mock<ICacheService>();
mockCache
    .Setup(x => x.GetOrSetAsync(
        It.IsAny<string>(), 
        It.IsAny<Func<Task<UserDto>>>(), 
        It.IsAny<CacheOptions?>(), 
        It.IsAny<CancellationToken>()))
    .ReturnsAsync(testUser);
```

## 性能优化建议

1. **合理设置缓存大小**：根据服务器内存调整 `L1Cache.SizeLimit`
2. **使用批量操作**：减少网络往返次数
3. **避免缓存大对象**：考虑只缓存必要的字段
4. **使用压缩**：对大对象启用压缩 `EnableCompression = true`
5. **监控缓存命中率**：定期分析并优化缓存策略

## 总结

`CodeSpirit.Caching` 提供了一个强大而灵活的缓存解决方案，帮助开发者：

- ✅ 统一缓存访问接口
- ✅ 自动管理多级缓存
- ✅ 防止缓存击穿和雪崩
- ✅ 支持灵活的过期策略
- ✅ 提供缓存预热功能
- ✅ 简化分布式缓存管理

通过合理使用这些功能，可以显著提升应用性能和用户体验。

