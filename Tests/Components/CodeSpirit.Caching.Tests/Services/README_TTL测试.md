# TTL时间一致性测试 - 快速指南

## 快速开始

### 运行所有TTL测试
```powershell
.\run-ttl-consistency-tests.ps1
```

### 只运行单元测试（快速验证）
```powershell
dotnet test --filter "FullyQualifiedName~TtlConsistencyTests&FullyQualifiedName!~Integration" --verbosity minimal
```

**结果**: ✅ 19个测试，耗时 < 100ms

### 只运行集成测试（完整验证）
```powershell
dotnet test --filter "FullyQualifiedName~TtlConsistencyIntegrationTests" --verbosity minimal
```

**结果**: ✅ 15个测试，耗时 5-8分钟

## 测试覆盖的场景

### ✅ L1缓存TTL
- 显式过期时间设置
- 默认过期时间应用
- 滑动过期处理
- 优先级验证

### ✅ L2缓存TTL
- 显式过期时间设置
- 默认过期时间应用
- 滑动过期处理
- 优先级验证

### ✅ 两级缓存独立性
- L1和L2独立过期
- 部分设置场景
- 共同过期时间

### ✅ 边界条件
- 零值过期时间（异常）
- 负值过期时间（异常）
- 多种时间参数

### ✅ 实际行为（集成测试）
- 真实过期行为
- 滑动过期效果
- 并发场景
- 多键独立性

## 修复的Bug

**问题**: 当设置了 `AbsoluteExpirationRelativeToNow` 或 `AbsoluteExpiration` 时，仍然会错误地应用 `DefaultSlidingExpiration`，导致缓存提前过期。

**修复**: 修改了 `CreateMemoryCacheOptions` 和 `CreateDistributedCacheOptions` 方法，使用 `hasExplicitExpiration` 标记来判断是否有任何显式过期时间设置。

**验证**: 所有34个测试通过，证明修复有效。

## 详细文档

查看完整文档：[TtlConsistencyTests说明.md](./TtlConsistencyTests说明.md)

## 测试统计

| 类型 | 数量 | 状态 | 耗时 |
|------|------|------|------|
| 单元测试 | 19 | ✅ 全部通过 | < 100ms |
| 集成测试 | 15 | ✅ 全部通过 | 5-8分钟 |
| **总计** | **34** | **✅ 全部通过** | - |

## CI/CD集成

```yaml
- name: Run TTL Unit Tests
  run: |
    cd Tests/Components/CodeSpirit.Caching.Tests
    dotnet test --filter "FullyQualifiedName~TtlConsistencyTests&FullyQualifiedName!~Integration" --logger "trx"

- name: Run TTL Integration Tests (Nightly)
  run: |
    cd Tests/Components/CodeSpirit.Caching.Tests
    dotnet test --filter "FullyQualifiedName~TtlConsistencyIntegrationTests" --logger "trx"
  if: github.event_name == 'schedule' # 只在定时任务中运行
```

## 问题排查

### 测试失败？
1. 检查是否有其他进程占用内存缓存
2. 确保系统时间准确
3. 检查是否有防病毒软件干扰

### 测试超时？
集成测试需要等待实际的缓存过期，这是正常的。可以：
- 只运行单元测试进行快速验证
- 在CI/CD中运行集成测试
- 调整测试超时设置

## 相关文件

- `TtlConsistencyTests.cs` - 单元测试
- `TtlConsistencyIntegrationTests.cs` - 集成测试
- `TtlConsistencyTests说明.md` - 详细文档
- `run-ttl-consistency-tests.ps1` - 运行脚本
- `MultiLevelCacheService.cs` - 修复的代码

