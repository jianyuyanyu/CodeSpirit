# CodeSpirit.ConfigCenter.Sdk 单元测试

## 概述

本测试项目验证配置中心 SDK 的功能，特别是**依赖注入生命周期修复**后的正确性。

## 测试覆盖

### 1. ConfigCacheServiceTests

**文件**: `Cache/ConfigCacheServiceTests.cs`

**测试内容**:
- ✅ `GetFromCacheAsync` - 从 Redis 缓存获取配置
- ✅ `SaveToCacheAsync` - 保存配置到 Redis 缓存
- ✅ `ClearCacheAsync` - 清除 Redis 缓存
- ✅ 缓存服务未注册的降级处理
- ✅ 异常处理和日志记录
- ✅ Scope 生命周期管理
- ✅ 多次调用不会泄漏 Scoped 服务

**关键测试**:
```csharp
[Fact]
public async Task ServiceLifetime_ScopeIsDisposedAfterUse()
{
    // 验证每次调用都创建和释放 Scope
    _scopeFactoryMock.Verify(f => f.CreateScope(), Times.Once);
    _scopeMock.Verify(s => s.Dispose(), Times.Once);
}
```

### 2. DependencyInjectionIntegrationTests

**文件**: `Integration/DependencyInjectionIntegrationTests.cs`

**测试内容**:
- ✅ `ConfigCacheService` 是 Singleton
- ✅ `ICacheService` 是 Scoped
- ✅ 依赖注入生命周期验证通过
- ✅ Scope 验证启用时不抛出异常
- ✅ 并发访问测试
- ✅ 模拟 ASP.NET Core 请求 Scope

**关键测试**:
```csharp
[Fact]
public void ServiceRegistration_ConfigCacheService_CanResolveWithoutError()
{
    // 启用 Scope 验证，确保不会抛出生命周期冲突异常
    var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions
    {
        ValidateScopes = true,
        ValidateOnBuild = true
    });

    var act = () => serviceProvider.GetRequiredService<ConfigCacheService>();
    act.Should().NotThrow(); // ✅ 通过
}
```

### 3. ConfigChangedEventHandlerTests

**文件**: `Events/ConfigChangedEventHandlerTests.cs`

**测试内容**:
- ✅ 配置变更事件处理
- ✅ 依赖 `ConfigCacheService` 的正确性

### 4. ConfigCenterOptionsTests

**文件**: `ConfigCenterOptionsTests.cs`

**测试内容**:
- ✅ 配置选项默认值
- ✅ 配置选项验证

## 运行测试

### 运行所有测试

```powershell
# 在测试项目目录
cd Tests/Components/CodeSpirit.ConfigCenter.Sdk.Tests

# 运行所有测试
dotnet test

# 带详细输出
dotnet test --logger "console;verbosity=detailed"

# 带代码覆盖率
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

### 运行特定测试类

```powershell
# 只运行 ConfigCacheServiceTests
dotnet test --filter "FullyQualifiedName~ConfigCacheServiceTests"

# 只运行依赖注入集成测试
dotnet test --filter "FullyQualifiedName~DependencyInjectionIntegrationTests"
```

### 运行特定测试方法

```powershell
# 运行单个测试
dotnet test --filter "FullyQualifiedName~ServiceLifetime_ScopeIsDisposedAfterUse"
```

### 在 Visual Studio 中运行

1. 打开测试资源管理器（`Test` → `Test Explorer`）
2. 点击"运行所有测试"或选择特定测试运行

## 测试结果预期

### ✅ 所有测试应该通过

```
Test run for CodeSpirit.ConfigCenter.Sdk.Tests.dll (.NET 10.0)
Microsoft (R) Test Execution Command Line Tool Version 17.13.0

Starting test execution, please wait...
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:    42, Skipped:     0, Total:    42, Duration: 2.5s
```

### 关键验证点

#### 1. Singleton vs Scoped 验证

```
✅ ServiceRegistration_ConfigCacheService_IsSingleton
   - ConfigCacheService 是单例
   
✅ ServiceRegistration_ICacheService_IsScoped
   - ICacheService 是 Scoped

✅ ServiceRegistration_ConfigCacheService_CanResolveWithoutError
   - 启用 Scope 验证时不抛出生命周期冲突异常
```

#### 2. Scope 生命周期管理

```
✅ ServiceLifetime_ScopeIsDisposedAfterUse
   - 每次调用创建新 Scope
   - 使用后自动释放

✅ ServiceLifetime_ScopeIsDisposedEvenOnException
   - 即使异常也会释放 Scope

✅ ServiceLifetime_NoScopedServiceLeaks
   - 多次调用不会累积 Scoped 服务
```

#### 3. 并发和实际场景

```
✅ ServiceRegistration_ParallelAccess_WorksCorrectly
   - 并发访问正常工作

✅ ServiceRegistration_RealWorldScenario_SimulatesAspNetCoreRequest
   - 模拟 ASP.NET Core 请求场景
```

## 故障排查

### 测试失败：生命周期冲突

**错误信息**:
```
System.InvalidOperationException: Cannot consume scoped service 'ICacheService' from singleton 'ConfigCacheService'
```

**原因**: `ConfigCacheService` 没有使用 `IServiceScopeFactory`

**解决**: 确保 `ConfigCacheService` 的构造函数注入 `IServiceScopeFactory` 而不是 `ICacheService`

### 测试失败：Mock 设置不正确

**错误信息**:
```
Moq.MockException: Expected invocation on the mock once, but was 0 times
```

**原因**: Mock 的 `IServiceScopeFactory` 配置不正确

**解决**: 检查以下 Mock 配置：
```csharp
// ServiceProvider 返回 ICacheService
_serviceProviderMock
    .Setup(sp => sp.GetService(typeof(ICacheService)))
    .Returns(_cacheServiceMock.Object);

// Scope 返回 ServiceProvider
_scopeMock.Setup(s => s.ServiceProvider).Returns(_serviceProviderMock.Object);

// ScopeFactory 创建 Scope
_scopeFactoryMock.Setup(f => f.CreateScope()).Returns(_scopeMock.Object);
```

### 测试超时

**原因**: 可能存在死锁或无限等待

**解决**: 
1. 检查 Mock 的异步方法配置
2. 确保所有异步操作都正确完成
3. 添加超时设置：
   ```csharp
   [Fact(Timeout = 5000)] // 5秒超时
   public async Task MyTest() { ... }
   ```

## 依赖注入生命周期最佳实践

### ✅ 正确：Singleton 通过 IServiceScopeFactory 使用 Scoped

```csharp
public class MySingletonService
{
    private readonly IServiceScopeFactory _scopeFactory;
    
    public MySingletonService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }
    
    public async Task DoWorkAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var scopedService = scope.ServiceProvider.GetRequiredService<IMyScopedService>();
        await scopedService.DoSomethingAsync();
    }
}
```

### ❌ 错误：Singleton 直接依赖 Scoped

```csharp
public class MySingletonService
{
    private readonly IMyScopedService _scopedService; // ❌
    
    public MySingletonService(IMyScopedService scopedService) // ❌
    {
        _scopedService = scopedService;
    }
}
```

## CI/CD 集成

### GitHub Actions

```yaml
- name: Run Tests
  run: dotnet test --no-build --verbosity normal
  
- name: Generate Coverage Report
  run: dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

### Azure DevOps

```yaml
- task: DotNetCoreCLI@2
  displayName: 'Run Tests'
  inputs:
    command: 'test'
    projects: '**/CodeSpirit.ConfigCenter.Sdk.Tests.csproj'
    arguments: '--configuration $(buildConfiguration)'
```

## 相关文档

- [配置中心 SDK 自动集成说明](../../../Docs/03-Core-Components/config-center-sdk-auto-integration-zh-CN.md)
- [依赖注入生命周期修复](../../../Docs/03-Core-Components/config-center-sdk-di-lifetime-fix-zh-CN.md)
- [配置中心重构方案 v4](../../../c:\Users\codel\.cursor\plans\配置中心重构方案v4_234c5555.plan.md)

## 贡献

在提交 PR 之前，请确保：
1. ✅ 所有测试通过
2. ✅ 新功能有对应的单元测试
3. ✅ 代码覆盖率 > 80%
4. ✅ 遵循项目测试规范

## 更新日志

- **2026-01-07**: 创建测试项目，验证依赖注入生命周期修复
- **2026-01-07**: 添加集成测试，验证实际 ASP.NET Core 场景

