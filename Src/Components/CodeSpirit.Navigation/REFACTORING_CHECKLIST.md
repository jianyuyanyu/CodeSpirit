# CodeSpirit.Navigation 重构任务清单

## 🗓️ 时间规划: 2-3 天

---

## 第 1 天上午 (2-3 小时)

### ✅ 准备工作
- [ ] 创建备份分支 `backup/navigation-before-refactor`
- [ ] 创建重构分支 `refactor/navigation-simplification`
- [ ] 阅读完整重构文档

### ✅ 创建核心服务接口
- [ ] 创建 `Services/INavigationTreeBuilder.cs`
- [ ] 创建 `Services/INavigationCacheManager.cs`
- [ ] 创建 `Services/INavigationFilterService.cs`
- [ ] 创建 `Services/Filters/INavigationFilter.cs`

---

## 第 1 天下午 (3-4 小时)

### ✅ 实现 NavigationTreeBuilder
- [ ] 创建 `Services/NavigationTreeBuilder.cs`
- [ ] 从 `NavigationService.Tree.cs` 迁移以下方法:
  - [ ] `BuildModuleNavigationTree()`
  - [ ] `BuildCodeBasedNavigation()`
  - [ ] `LoadNavigationFromConfig()`
  - [ ] `MergeNavigationNodes()`
  - [ ] `GetAllModuleNames()`
  - [ ] `GetCurrentModules()`
  - [ ] `GetConfigModules()`
  - [ ] `ProcessPlatformTypeInheritance()`
  - [ ] `CreateNavigationNode()`
  - [ ] `ConvertToNavigationNode()`
- [ ] 编译测试无错误

### ✅ 实现 NavigationCacheManager
- [ ] 创建 `Services/NavigationCacheManager.cs`
- [ ] 实现以下方法:
  - [ ] `GetCachedNavigationAsync()`
  - [ ] `SetCachedNavigationAsync()`
  - [ ] `ClearAllCacheAsync()`
  - [ ] `ClearModuleCacheAsync()`
- [ ] 编译测试无错误

---

## 第 2 天上午 (3-4 小时)

### ✅ 创建过滤器体系
- [ ] 创建 `Services/Filters/` 目录
- [ ] 创建过滤器类:
  - [ ] `PlatformFilter.cs` (Priority: 1)
  - [ ] `PermissionFilter.cs` (Priority: 2)
  - [ ] `AuthenticationFilter.cs` (Priority: 3)
  - [ ] `VersionFilter.cs` (Priority: 4)
  - [ ] `DeviceFilter.cs` (Priority: 5)
  - [ ] `ExperimentalFilter.cs` (Priority: 6)
  - [ ] `GroupFilter.cs` (Priority: 7)
  - [ ] `TagFilter.cs` (Priority: 8)

### ✅ 实现 NavigationFilterService
- [ ] 创建 `Services/NavigationFilterService.cs`
- [ ] 实现以下方法:
  - [ ] `FilterNodes()` (递归过滤)
  - [ ] `RegisterFilter()` (动态注册)
- [ ] 编译测试无错误

---

## 第 2 天下午 (3-4 小时)

### ✅ 重构主服务 NavigationService
- [ ] 修改构造函数，使用新的服务依赖:
  - [ ] `INavigationTreeBuilder`
  - [ ] `INavigationCacheManager`
  - [ ] `INavigationFilterService`
- [ ] 重构以下方法:
  - [ ] `GetNavigationTreeAsync()` - 使用新服务
  - [ ] `InitializeNavigationTree()` - 简化实现
  - [ ] `ClearModuleNavigationCacheAsync()` - 简化实现
  - [ ] `ClearAllNavigationCacheAsync()` - 简化实现
- [ ] 保持向后兼容的方法:
  - [ ] `FilterNodesByPermission()` - 委托给 FilterService
  - [ ] `FilterNodesByPlatform()` - 委托给 FilterService
  - [ ] `FilterNodesByContext()` - 委托给 FilterService
- [ ] 编译测试无错误

### ✅ 更新依赖注入
- [ ] 修改 `Extensions/ServiceCollectionExtensions.cs`
- [ ] 注册所有新服务:
  - [ ] `INavigationTreeBuilder`
  - [ ] `INavigationCacheManager`
  - [ ] `INavigationFilterService`
  - [ ] 所有过滤器 (8 个)
- [ ] 编译测试无错误

### ✅ 清理旧代码
- [ ] 删除 `Services/NavigationService.Tree.cs`
- [ ] 删除 `Services/NavigationService.Cache.cs`
- [ ] 删除旧的缓存键常量:
  - [ ] `CACHE_KEY_PREFIX`
  - [ ] `MODULE_NAMES_CACHE_KEY`
- [ ] 删除旧的 `GetModuleCacheKey()` 方法
- [ ] 编译测试无错误

---

## 第 3 天上午 (3-4 小时)

### ✅ 编写单元测试

#### NavigationTreeBuilder 测试
- [ ] `NavigationTreeBuilderTests.cs`
  - [ ] `BuildNavigationTree_WhenNoModules_ShouldReturnEmptyList`
  - [ ] `BuildModuleNavigationTree_WhenModuleExists_ShouldReturnNodes`
  - [ ] `MergeNavigationNodes_ShouldMergeAllProperties`
  - [ ] `MergeNavigationNodes_ShouldMergeChildren`

#### NavigationCacheManager 测试
- [ ] `NavigationCacheManagerTests.cs`
  - [ ] `GetCachedNavigationAsync_WhenCacheEmpty_ShouldReturnNull`
  - [ ] `GetCachedNavigationAsync_WhenCacheExists_ShouldReturnData`
  - [ ] `SetCachedNavigationAsync_ShouldStoreInCache`
  - [ ] `ClearAllCacheAsync_ShouldRemoveCache`
  - [ ] `ClearModuleCacheAsync_ShouldRemoveCache`

#### 过滤器测试
- [ ] `PlatformFilterTests.cs`
  - [ ] 测试所有平台组合 (System, Tenant, Both)
- [ ] `PermissionFilterTests.cs`
  - [ ] 测试有权限/无权限情况
- [ ] `AuthenticationFilterTests.cs`
  - [ ] 测试已认证/未认证情况
- [ ] `VersionFilterTests.cs`
  - [ ] 测试版本范围过滤
- [ ] 其他过滤器测试...

#### NavigationFilterService 测试
- [ ] `NavigationFilterServiceTests.cs`
  - [ ] `FilterNodes_WithSingleFilter_ShouldWork`
  - [ ] `FilterNodes_WithMultipleFilters_ShouldApplyAll`
  - [ ] `FilterNodes_WithChildNodes_ShouldIncludeParentIfChildMatches`
  - [ ] `RegisterFilter_ShouldAddCustomFilter`
  - [ ] `FilterNodes_WhenFilterThrows_ShouldIncludeNode`

---

## 第 3 天下午 (2-3 小时)

### ✅ 集成测试
- [ ] `NavigationServiceIntegrationTests.cs`
  - [ ] `GetNavigationTreeAsync_ShouldUseCacheAfterFirstCall`
  - [ ] `GetNavigationTreeAsync_WithPlatformFilter_ShouldReturnFilteredNodes`
  - [ ] `InitializeNavigationTree_ShouldBuildAndCache`
  - [ ] `ClearModuleNavigationCacheAsync_ShouldInvalidateCache`

### ✅ 运行所有测试
- [ ] 运行单元测试: `dotnet test`
- [ ] 确保所有测试通过
- [ ] 测试覆盖率 > 80%

### ✅ 更新文档
- [ ] 更新 `README.md`
  - [ ] 更新架构说明
  - [ ] 更新依赖注入示例
  - [ ] 添加过滤器说明
- [ ] 更新 `CHANGELOG.md`
  - [ ] 添加重构说明
  - [ ] 记录破坏性变更
  - [ ] 添加迁移指南
- [ ] 检查所有代码注释完整性

---

## 验收测试

### ✅ 功能验证
- [ ] 在开发环境启动应用
- [ ] 验证导航树正常加载
- [ ] 验证平台过滤正常工作
- [ ] 验证权限过滤正常工作
- [ ] 验证缓存正常工作
- [ ] 验证缓存清除正常工作

### ✅ 性能测试
- [ ] 首次加载时间 < 50ms
- [ ] 缓存命中时间 < 10ms
- [ ] Redis 内存占用比重构前减少

### ✅ 代码质量
- [ ] 代码通过编译，无警告
- [ ] 代码符合 C# 编码规范
- [ ] 所有公共方法有 XML 注释
- [ ] 代码通过静态分析

---

## 提交和部署

### ✅ 版本控制
- [ ] 提交所有更改: `git add .`
- [ ] 创建提交: `git commit -m "refactor: 简化导航组件架构"`
- [ ] 推送到远程: `git push origin refactor/navigation-simplification`

### ✅ 代码审查
- [ ] 创建 Pull Request
- [ ] 等待代码审查
- [ ] 根据反馈修改代码
- [ ] 获得批准

### ✅ 合并和部署
- [ ] 合并到主分支: `git merge refactor/navigation-simplification`
- [ ] 清除旧缓存 (生产环境)
- [ ] 部署到开发环境
- [ ] 验证功能正常
- [ ] 部署到生产环境
- [ ] 监控日志和性能指标

---

## 📊 进度跟踪

| 阶段 | 任务数 | 完成数 | 进度 | 状态 |
|-----|-------|-------|------|------|
| 准备工作 | 3 | 0 | 0% | ⏳ 待开始 |
| 第1天上午 | 4 | 0 | 0% | ⏳ 待开始 |
| 第1天下午 | 12 | 0 | 0% | ⏳ 待开始 |
| 第2天上午 | 9 | 0 | 0% | ⏳ 待开始 |
| 第2天下午 | 17 | 0 | 0% | ⏳ 待开始 |
| 第3天上午 | 20 | 0 | 0% | ⏳ 待开始 |
| 第3天下午 | 8 | 0 | 0% | ⏳ 待开始 |
| 验收测试 | 9 | 0 | 0% | ⏳ 待开始 |
| 提交部署 | 10 | 0 | 0% | ⏳ 待开始 |
| **总计** | **92** | **0** | **0%** | ⏳ 待开始 |

---

## 🎯 关键里程碑

- [ ] **里程碑 1**: 完成核心服务实现 (第1天结束)
- [ ] **里程碑 2**: 完成过滤器体系 (第2天上午)
- [ ] **里程碑 3**: 完成主服务重构 (第2天下午)
- [ ] **里程碑 4**: 完成所有测试 (第3天上午)
- [ ] **里程碑 5**: 通过验收测试 (第3天下午)
- [ ] **里程碑 6**: 成功部署到生产 (部署完成)

---

## 📝 注意事项

1. **每完成一个小任务就提交代码**，便于回滚
2. **先编写测试，再实现功能** (TDD)
3. **保持向后兼容**，确保现有代码不受影响
4. **及时更新文档**，记录所有变更
5. **定期清理旧代码**，保持代码库整洁

---

## 🆘 遇到问题?

如果遇到问题，请参考:
- [重构方案](./REFACTORING_PLAN.md) - 详细的重构方案
- [实施指南](./REFACTORING_IMPLEMENTATION_GUIDE.md) - 代码迁移示例
- [组件 README](./README.md) - 组件使用说明

---

**开始日期**: _______  
**预计完成**: _______  
**实际完成**: _______  
**负责人**: _______  

**祝重构顺利! 🚀**
