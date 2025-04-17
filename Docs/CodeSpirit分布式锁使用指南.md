# CodeSpirit分布式锁使用指南

## 概述

分布式锁是分布式系统中的一种重要同步机制，用于在分布式环境下协调多个进程或服务对共享资源的访问。CodeSpirit框架提供了基于Redis的分布式锁实现，可用于微服务架构中的并发控制和资源争用处理。

## 功能特性

- 基于Redis实现的高性能分布式锁
- 支持锁超时和自动释放机制
- 灵活的锁获取策略和重试机制
- 完整的异步API支持
- 支持依赖注入

## 核心组件

### IDistributedLockProvider

分布式锁提供程序接口，定义了获取和释放锁的基本操作：

```csharp
public interface IDistributedLockProvider
{
    Task<IDisposable> AcquireLockAsync(string key, TimeSpan timeout, TimeSpan? ttl = null);
    Task<IDisposable> AcquireLockAsync(string key, TimeSpan? ttl = null);
    Task<bool> ReleaseLockAsync(string key);
    Task<bool> IsLockedAsync(string key);
}
```

### RedisDistributedLockProvider

基于Redis实现的分布式锁提供程序，通过Redis的原子操作实现分布式锁机制：

```csharp
public class RedisDistributedLockProvider : IDistributedLockProvider
{
    // 实现细节...
}
```

### RedisDistributedLockOptions

Redis分布式锁配置选项：

```csharp
public class RedisDistributedLockOptions
{
    public string KeyPrefix { get; set; } = "CodeSpirit:DistLock:";
    public TimeSpan DefaultLockTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public TimeSpan DefaultAcquireTimeout { get; set; } = TimeSpan.FromSeconds(10);
    public TimeSpan RetryInterval { get; set; } = TimeSpan.FromMilliseconds(100);
    public bool EnableWatchdog { get; set; } = false;
    public TimeSpan? WatchdogInterval { get; set; } = null;
}
```

## 使用方法

### 注册服务

在应用启动时，通过依赖注入注册分布式锁服务：

```csharp
// 方法1：使用内置的Redis连接配置
services.AddRedisDistributedLock(options =>
{
    options.KeyPrefix = "MyApp:Lock:";
    options.DefaultLockTimeout = TimeSpan.FromSeconds(60);
    options.DefaultAcquireTimeout = TimeSpan.FromSeconds(15);
    options.RetryInterval = TimeSpan.FromMilliseconds(200);
});

// 方法2：使用现有的Redis连接
services.AddRedisDistributedLock(
    options => {
        // 配置选项
    },
    provider => provider.GetRequiredService<IConnectionMultiplexer>()
);
```

### 获取和使用锁

通过依赖注入获取`IDistributedLockProvider`实例，然后使用它来获取和释放锁：

```csharp
// 在服务类中
public class SomeService
{
    private readonly IDistributedLockProvider _lockProvider;

    public SomeService(IDistributedLockProvider lockProvider)
    {
        _lockProvider = lockProvider;
    }

    public async Task DoSomethingConcurrentSafeAsync()
    {
        // 获取锁（自动释放）
        using (await _lockProvider.AcquireLockAsync("resource-key"))
        {
            // 在获得锁的情况下执行操作
            await ProcessSharedResourceAsync();
        } // 离开using块时自动释放锁

        // 自定义超时和TTL
        using (await _lockProvider.AcquireLockAsync(
            "another-resource", 
            TimeSpan.FromSeconds(5),   // 获取锁的超时时间 
            TimeSpan.FromMinutes(2)))  // 锁的生存时间
        {
            // 业务逻辑
        }
    }
}
```

### 手动检查和释放锁

```csharp
// 检查锁是否存在
bool isLocked = await _lockProvider.IsLockedAsync("resource-key");

// 手动释放锁（通常不需要）
bool released = await _lockProvider.ReleaseLockAsync("resource-key");
```

## 最佳实践

1. **合理设置超时时间**：设置适当的锁获取超时时间和锁的TTL，避免锁超时过长导致系统响应慢，或过短导致任务未完成锁就已释放

2. **使用using语句**：推荐通过using语句自动释放锁，避免手动释放时的遗漏

3. **错误处理**：妥善处理锁获取超时或失败的情况，提供合适的降级策略

4. **避免长时间持有锁**：尽量减少锁定的时间，只在必要的最短时间内持有锁

5. **合理命名锁键**：使用有意义的、能够反映资源的锁键名，避免不同资源使用相同的锁键

6. **考虑资源粒度**：根据业务需求选择合适的锁粒度，避免过粗粒度导致不必要的阻塞

## 注意事项

- 分布式锁不是万能的，需要结合具体业务场景使用
- Redis锁在极端情况下可能会有失效的风险，如Redis节点故障切换
- 当前未实现看门狗机制（自动续期），长时间操作可能导致锁过期

## 高级场景

### 锁续期（未实现）

当前版本未实现锁自动续期功能，但保留了相关配置选项，为未来版本支持看门狗机制做准备。 