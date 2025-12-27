# Redis缓存管理服务单元测试说明

## 概述

本测试文件包含了对 `RedisCacheManagementService` 的单元测试。由于 StackExchange.Redis 的某些 API（特别是 `IServer.KeysAsync`）Mock 较为复杂，部分测试被标记为需要集成测试。

## 测试覆盖

### 已实现的单元测试

1. **GetValueAsync_WithStringType_ShouldReturnValue** - 测试获取字符串类型缓存值
2. **GetValueAsync_WithHashType_ShouldReturnHashValue** - 测试获取哈希类型缓存值
3. **GetValueAsync_WithNonExistentKey_ShouldReturnNull** - 测试获取不存在的键
4. **DeleteKeyAsync_WithExistingKey_ShouldReturnTrue** - 测试删除存在的键
5. **DeleteKeyAsync_WithNonExistentKey_ShouldReturnFalse** - 测试删除不存在的键
6. **ClearAllAsync_ShouldFlushDatabase** - 测试清空所有缓存
7. **ExistsAsync_WithExistingKey_ShouldReturnTrue** - 测试检查键是否存在（存在）
8. **ExistsAsync_WithNonExistentKey_ShouldReturnFalse** - 测试检查键是否存在（不存在）

### 需要集成测试的场景

以下测试由于需要真实的 Redis 连接或复杂的 Mock 设置，被标记为 Skip，建议使用集成测试：

1. **GetKeysAsync_WithPattern_ShouldReturnMatchingKeys** - 获取匹配模式的键列表
2. **GetKeysAsync_WithPagination_ShouldReturnPagedResults** - 分页获取键列表
3. **DeleteByPatternAsync_ShouldDeleteMatchingKeys** - 按模式批量删除
4. **DeleteByPatternAsync_WithTenantId_ShouldFilterByTenant** - 按租户过滤删除

## 运行测试

```bash
# 运行所有缓存管理服务测试
dotnet test Tests/Components/CodeSpirit.Caching.Tests/CodeSpirit.Caching.Tests.csproj --filter "FullyQualifiedName~RedisCacheManagementServiceTests"

# 运行所有测试（包括跳过的）
dotnet test Tests/Components/CodeSpirit.Caching.Tests/CodeSpirit.Caching.Tests.csproj --filter "FullyQualifiedName~RedisCacheManagementServiceTests" --no-skip
```

## 注意事项

1. 部分测试需要 Mock StackExchange.Redis 的复杂 API，建议使用真实的 Redis 连接进行集成测试
2. 测试使用了 FluentAssertions 进行断言，使测试代码更易读
3. 测试使用了 xUnit 的 ITestOutputHelper 输出测试信息，便于调试

