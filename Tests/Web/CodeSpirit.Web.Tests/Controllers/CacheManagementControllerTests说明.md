# 缓存管理控制器单元测试说明

## 概述

本测试文件包含了对 `CacheManagementController` 的完整单元测试，覆盖了所有 API 端点和权限控制逻辑。

## 测试覆盖

### 获取缓存键列表测试

1. **GetCacheKeys_AsSystemAdmin_ShouldReturnAllKeys** - 系统管理员可以获取所有键
2. **GetCacheKeys_AsTenantAdmin_ShouldReturnOnlyTenantKeys** - 租户管理员只能获取自己租户的键
3. **GetCacheKeys_AsTenantAdmin_WithoutTenantId_ShouldReturnBadResponse** - 租户管理员无租户ID时返回错误

### 获取缓存值详情测试

1. **GetCacheValue_AsSystemAdmin_ShouldReturnValue** - 系统管理员可以获取任何键的值
2. **GetCacheValue_AsTenantAdmin_WithUnauthorizedKey_ShouldReturnBadResponse** - 租户管理员访问其他租户的键时返回错误
3. **GetCacheValue_WithNonExistentKey_ShouldReturnBadResponse** - 不存在的键返回错误

### 删除缓存键测试

1. **DeleteCacheKey_AsSystemAdmin_ShouldDeleteSuccessfully** - 系统管理员可以删除任何键
2. **DeleteCacheKey_AsTenantAdmin_WithUnauthorizedKey_ShouldReturnBadResponse** - 租户管理员删除其他租户的键时返回错误

### 批量删除测试

1. **DeleteByPattern_AsSystemAdmin_ShouldDeleteSuccessfully** - 系统管理员可以按模式批量删除
2. **DeleteByPattern_AsTenantAdmin_ShouldDeleteOnlyTenantKeys** - 租户管理员只能删除自己租户的键

### 清空所有缓存测试

1. **ClearAllCache_AsSystemAdmin_ShouldClearSuccessfully** - 系统管理员可以清空所有缓存
2. **ClearAllCache_AsTenantAdmin_ShouldReturnBadResponse** - 租户管理员无法清空所有缓存

### 异常处理测试

1. **GetCacheKeys_WithException_ShouldReturnBadResponse** - 异常情况下的错误处理

## 运行测试

```bash
# 运行所有缓存管理控制器测试
dotnet test Tests/Web/CodeSpirit.Web.Tests/CodeSpirit.Web.Tests.csproj --filter "FullyQualifiedName~CacheManagementControllerTests"

# 运行特定测试
dotnet test Tests/Web/CodeSpirit.Web.Tests/CodeSpirit.Web.Tests.csproj --filter "FullyQualifiedName~CacheManagementControllerTests.GetCacheKeys_AsSystemAdmin"
```

## 测试特点

1. **权限控制测试** - 全面测试了系统管理员和租户管理员的权限差异
2. **多租户隔离** - 验证了租户管理员只能访问自己租户的缓存
3. **错误处理** - 测试了各种错误场景和异常情况
4. **Mock 使用** - 使用 Moq 框架 Mock 了所有依赖项

## 注意事项

1. 测试使用了 FluentAssertions 进行断言，使测试代码更易读
2. 测试使用了 xUnit 的 ITestOutputHelper 输出测试信息
3. 所有测试都是独立的，不依赖外部资源

