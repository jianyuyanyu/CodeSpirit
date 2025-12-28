# ISettableCurrentUser 可设置用户接口使用指南

## 概述

`ISettableCurrentUser` 是 CodeSpirit 框架中的一个可设置当前用户接口，继承自 `ICurrentUser` 接口。该接口的主要目的是在特定场景下（如事件处理、后台任务等）动态修改当前用户信息，支持临时覆盖用户ID、用户名和租户ID等信息。

## 接口定义

```csharp
/// <summary>
/// 可设置的当前用户接口，支持动态修改用户信息（主要用于事件处理等场景）
/// </summary>
public interface ISettableCurrentUser : ICurrentUser
{
    /// <summary>
    /// 设置当前用户ID
    /// </summary>
    /// <param name="userId">用户ID</param>
    void SetUserId(long? userId);

    /// <summary>
    /// 设置当前租户ID
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    void SetTenantId(string tenantId);

    /// <summary>
    /// 设置当前用户名
    /// </summary>
    /// <param name="userName">用户名</param>
    void SetUserName(string userName);

    /// <summary>
    /// 重置为原始状态
    /// </summary>
    void Reset();
}
```

## 核心功能

### 1. 动态用户信息设置

- **SetUserId(long? userId)**: 临时设置当前用户ID
- **SetUserName(string userName)**: 临时设置当前用户名
- **SetTenantId(string tenantId)**: 临时设置当前租户ID
- **Reset()**: 清除所有临时设置，恢复原始状态

### 2. 继承 ICurrentUser 功能

该接口继承自 `ICurrentUser`，拥有所有基础的用户信息获取功能：

- 用户ID、用户名、角色列表
- 认证状态、权限集合
- 租户ID、租户名称
- 角色判断、租户判断等

## 实现类

### CurrentUser 类实现

`CurrentUser` 类是 `ISettableCurrentUser` 接口的默认实现，位于 `CodeSpirit.Authorization` 组件中。

```csharp
public class CurrentUser : ISettableCurrentUser
{
    // 覆盖值字段
    private long? _overrideUserId = null;
    private string? _overrideTenantId = null;
    private string? _overrideUserName = null;

    // ISettableCurrentUser 实现
    public void SetUserId(long? userId)
    {
        _overrideUserId = userId;
    }

    public void SetTenantId(string tenantId)
    {
        _overrideTenantId = tenantId;
    }

    public void SetUserName(string userName)
    {
        _overrideUserName = userName;
    }

    public void Reset()
    {
        _overrideUserId = null;
        _overrideTenantId = null;
        _overrideUserName = null;
    }
}
```

## 主要使用场景

### 1. 租户感知事件处理

在处理租户感知事件时，需要临时切换到事件发起者的用户上下文：

```csharp
public void SetCurrentUserInfo(long? userId = null, string? userName = null)
{
    var currentUser = GetTenantService<ICurrentUser>();
    if (currentUser is ISettableCurrentUser settableCurrentUser)
    {
        if (userId.HasValue)
        {
            settableCurrentUser.SetUserId(userId.Value);
            _logger.LogDebug("设置CurrentUser用户ID: {UserId}", userId.Value);
        }
        
        if (!string.IsNullOrEmpty(userName))
        {
            settableCurrentUser.SetUserName(userName);
            _logger.LogDebug("设置CurrentUser用户名: {UserName}", userName);
        }
    }
    else
    {
        _logger.LogWarning("CurrentUser未实现ISettableCurrentUser接口，无法设置用户信息");
    }
}
```

### 2. 后台任务执行

在后台任务中需要模拟特定用户身份执行操作：

```csharp
public async Task ExecuteBackgroundTask(long userId, string tenantId)
{
    var currentUser = _serviceProvider.GetService<ICurrentUser>();
    if (currentUser is ISettableCurrentUser settableCurrentUser)
    {
        try
        {
            // 设置任务执行时的用户上下文
            settableCurrentUser.SetUserId(userId);
            settableCurrentUser.SetTenantId(tenantId);
            
            // 执行需要用户上下文的业务逻辑
            await _businessService.ProcessUserData();
        }
        finally
        {
            // 重置用户上下文
            settableCurrentUser.Reset();
        }
    }
}
```

### 3. 自动重置用户作用域

框架提供了 `AutoResetCurrentUserScope` 类，自动管理用户上下文的设置和重置：

```csharp
public class AutoResetCurrentUserScope : IServiceScope
{
    private readonly IServiceScope _innerScope;
    private readonly ISettableCurrentUser _settableCurrentUser;
    private readonly ILogger _logger;

    public AutoResetCurrentUserScope(IServiceScope innerScope, ISettableCurrentUser settableCurrentUser, ILogger logger)
    {
        _innerScope = innerScope;
        _settableCurrentUser = settableCurrentUser;
        _logger = logger;
    }

    public void Dispose()
    {
        try
        {
            // 自动重置用户信息
            _settableCurrentUser.Reset();
            _logger.LogDebug("自动重置CurrentUser状态");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "重置CurrentUser状态时出错");
        }
        finally
        {
            _innerScope.Dispose();
        }
    }
}
```

## 使用注意事项

### 1. 线程安全

- `ISettableCurrentUser` 的设置操作不是线程安全的
- 在多线程环境中使用时需要注意作用域隔离
- 建议在单个请求或任务的生命周期内使用

### 2. 状态管理

- 设置的用户信息只是临时覆盖，不会影响原始的认证状态
- 必须调用 `Reset()` 方法来清除临时设置
- 建议使用 try-finally 或 using 模式确保状态重置

### 3. 权限验证

- 临时设置的用户信息可能影响权限验证结果
- 在设置用户信息时要确保具有相应的操作权限
- 避免在权限敏感的操作中滥用此功能

### 4. 审计日志

- 使用此接口修改用户上下文可能影响审计日志的记录
- 在审计要求严格的场景中要谨慎使用
- 建议记录用户上下文切换的操作日志

## 最佳实践

### 1. 使用 using 模式

```csharp
public async Task ProcessWithUserContext(long userId, string tenantId)
{
    var currentUser = _serviceProvider.GetService<ICurrentUser>();
    if (currentUser is ISettableCurrentUser settableCurrentUser)
    {
        // 保存原始状态
        var originalUserId = currentUser.Id;
        var originalTenantId = currentUser.TenantId;
        
        try
        {
            settableCurrentUser.SetUserId(userId);
            settableCurrentUser.SetTenantId(tenantId);
            
            await _businessService.ProcessData();
        }
        finally
        {
            settableCurrentUser.Reset();
        }
    }
}
```

### 2. 检查接口支持

```csharp
public void SetUserContextIfSupported(long userId)
{
    var currentUser = _serviceProvider.GetService<ICurrentUser>();
    if (currentUser is ISettableCurrentUser settableCurrentUser)
    {
        settableCurrentUser.SetUserId(userId);
    }
    else
    {
        _logger.LogWarning("当前用户服务不支持动态设置功能");
    }
}
```

### 3. 日志记录

```csharp
public void SetUserContext(long userId, string operation)
{
    if (currentUser is ISettableCurrentUser settableCurrentUser)
    {
        _logger.LogInformation("临时切换用户上下文: UserId={UserId}, Operation={Operation}", userId, operation);
        settableCurrentUser.SetUserId(userId);
    }
}
```

## 相关组件

- **ICurrentUser**: 基础当前用户接口
- **CurrentUser**: 实现类，支持动态设置
- **TenantEventContext**: 租户事件上下文，使用该接口进行用户切换
- **AutoResetCurrentUserScope**: 自动重置用户上下文的作用域包装器

## 总结

`ISettableCurrentUser` 接口为 CodeSpirit 框架提供了灵活的用户上下文管理能力，主要用于事件处理、后台任务等需要临时切换用户身份的场景。正确使用该接口可以确保在复杂的多租户和权限管理环境中，系统能够准确地处理不同用户上下文下的业务逻辑。

使用时需要注意线程安全、状态管理和权限验证等方面的问题，建议采用最佳实践模式来确保系统的稳定性和安全性。