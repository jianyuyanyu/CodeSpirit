# CodeSpirit.Caching 统一缓存组件

## 概述

CodeSpirit.Caching 是一个基于 .NET 9 的统一缓存组件，提供了多级缓存、缓存穿透防护、缓存预热和灵活的过期策略等功能。

## 主要特性

- **多级缓存**: 支持内存缓存 (L1) 和分布式缓存 (L2) 的组合使用
- **缓存穿透防护**: 通过分布式锁防止缓存击穿
- **缓存预热**: 支持批量预热缓存数据
- **灵活的过期策略**: 支持绝对过期时间和滑动过期时间
- **分布式锁**: 基于 Redis 的分布式锁实现
- **统一接口**: 简化业务系统中的缓存使用

## 快速开始

### 1. 安装依赖

在项目中添加对 `CodeSpirit.Caching` 的引用：

```xml
<ProjectReference Include="..\..\Components\CodeSpirit.Caching\CodeSpirit.Caching.csproj" />
```

### 2. 配置服务

在 `Program.cs` 或服务配置中注册缓存服务：

```csharp
// 基本配置
services.AddCodeSpiritCaching(options =>
{
    options.DefaultAbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
    options.DefaultSlidingExpiration = TimeSpan.FromMinutes(5);
    options.EnableL1Cache = true;
    options.EnableL2Cache = true;
});

// 或从配置文件读取
services.AddCodeSpiritCaching(configuration.GetSection("Caching"));

// 添加 Redis 分布式缓存和锁
services.AddRedisDistributedCacheAndLock(configuration.GetConnectionString("Redis"));
```

### 3. 使用缓存服务

```csharp
public class ExampleService
{
    private readonly ICacheService _cacheService;
    
    public ExampleService(ICacheService cacheService)
    {
        _cacheService = cacheService;
    }
    
    public async Task<UserDto> GetUserAsync(long userId)
    {
        var key = _cacheService.CreateKey("user", userId);
        
        return await _cacheService.GetOrSetAsync(
            key,
            async () =>
            {
                // 从数据库获取数据
                return await _userRepository.GetByIdAsync(userId);
            },
            new CacheOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
                SlidingExpiration = TimeSpan.FromMinutes(5),
                Level = CacheLevel.Both,
                Tags = { $"user:{userId}" }
            });
    }
}
```

## 配置选项

### CachingOptions

```csharp
public class CachingOptions
{
    /// <summary>
    /// 默认绝对过期时间
    /// </summary>
    public TimeSpan? DefaultAbsoluteExpirationRelativeToNow { get; set; }
    
    /// <summary>
    /// 默认滑动过期时间
    /// </summary>
    public TimeSpan? DefaultSlidingExpiration { get; set; }
    
    /// <summary>
    /// 是否启用 L1 缓存（内存缓存）
    /// </summary>
    public bool EnableL1Cache { get; set; } = true;
    
    /// <summary>
    /// 是否启用 L2 缓存（分布式缓存）
    /// </summary>
    public bool EnableL2Cache { get; set; } = true;
    
    /// <summary>
    /// 缓存键前缀
    /// </summary>
    public string KeyPrefix { get; set; } = "CodeSpirit";
}
```

### 配置文件示例

```json
{
  "Caching": {
    "DefaultAbsoluteExpirationRelativeToNow": "00:30:00",
    "DefaultSlidingExpiration": "00:05:00",
    "EnableL1Cache": true,
    "EnableL2Cache": true,
    "KeyPrefix": "CodeSpirit"
  },
  "ConnectionStrings": {
    "Redis": "localhost:6379"
  }
}
```

## 高级功能

### 缓存预热

```csharp
public async Task WarmupCacheAsync()
{
    var warmupItems = new List<CacheWarmupItem>
    {
        CacheWarmupItem.Create(
            "popular_users",
            async () => await GetPopularUsersAsync(),
            new CacheOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1) }
        )
    };
    
    await _cacheWarmupService.WarmupBatchAsync(warmupItems);
}
```

### 分布式锁

```csharp
public async Task<string> GetExpensiveDataAsync(string key)
{
    using var lockHandle = await _distributedLockProvider.AcquireLockAsync(
        $"lock:{key}",
        TimeSpan.FromMinutes(5));
    
    if (lockHandle != null)
    {
        // 执行需要锁保护的操作
        return await ComputeExpensiveDataAsync(key);
    }
    
    // 获取锁失败，返回默认值或抛出异常
    throw new InvalidOperationException("无法获取分布式锁");
}
```

### 缓存标签和批量清除

```csharp
// 设置带标签的缓存
await _cacheService.SetAsync("user:123", userData, new CacheOptions
{
    Tags = { "user", "user:123", "department:456" }
});

// 按标签清除缓存
await _cacheService.RemoveByTagAsync("department:456");
```

## 集成示例

参考 `CodeSpirit.ExamApi` 项目中的 `ExamCacheService` 实现，了解如何在业务系统中使用统一缓存组件。

## 性能建议

1. **合理设置过期时间**: 根据数据更新频率设置合适的过期时间
2. **使用缓存标签**: 便于批量清除相关缓存
3. **避免缓存穿透**: 对于可能不存在的数据，也要缓存空结果
4. **监控缓存命中率**: 定期检查缓存效果，优化缓存策略

## 故障排除

### 常见问题

1. **Redis 连接失败**: 检查 Redis 服务是否正常运行，连接字符串是否正确
2. **缓存未生效**: 确认缓存配置是否正确，检查缓存键是否重复
3. **内存使用过高**: 调整 L1 缓存的过期时间，避免缓存过多数据

### 日志记录

组件内置了详细的日志记录，可以通过配置日志级别来调试问题：

```json
{
  "Logging": {
    "LogLevel": {
      "CodeSpirit.Caching": "Debug"
    }
  }
}
```
