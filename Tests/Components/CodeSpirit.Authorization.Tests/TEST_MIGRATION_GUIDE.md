# 权限测试迁移指南

## 背景

权限组件已从隐式通配逻辑更新为显式通配逻辑。这导致一些现有测试需要更新。

## 失败的测试及修复方案

### 1. PermissionServiceTests.cs

以下测试基于旧的隐式通配逻辑，需要更新：

#### 失败测试列表

1. **HasPermission_WhenParentPermissionExists_ReturnsTrue**
   - **当前逻辑**：`module_controller` 可以匹配 `module_controller_action`
   - **新逻辑**：只有 `module_controller_*` 才能匹配
   - **修复方案**：
     ```csharp
     // 旧代码
     var userPermissions = new HashSet<string> { "module_controller" };
     
     // 新代码（选项1：使用通配权限）
     var userPermissions = new HashSet<string> { "module_controller_*" };
     
     // 新代码（选项2：更改测试预期）
     Assert.False(result); // 因为不再支持隐式继承
     ```

2. **HasPermission_WhenModuleLevelPermissionExists_ReturnsTrue**
   - **当前逻辑**：`module` 可以匹配 `module_controller_action`
   - **新逻辑**：只有 `module_*` 才能匹配
   - **修复方案**：
     ```csharp
     // 旧代码
     var userPermissions = new HashSet<string> { "module" };
     
     // 新代码
     var userPermissions = new HashSet<string> { "module_*" };
     ```

3. **HasPermission_WhenDeepLevelHierarchyWithMiddlePermission_ReturnsTrue**
   - **当前逻辑**：`module_controller_group` 可以匹配 `module_controller_group_subgroup_action`
   - **新逻辑**：只有 `module_controller_group_*` 才能匹配
   - **修复方案**：
     ```csharp
     // 旧代码
     var userPermissions = new HashSet<string> { "module_controller_group" };
     
     // 新代码
     var userPermissions = new HashSet<string> { "module_controller_group_*" };
     ```

4. **HasPermission_WhenMultiLevelPermissionWithTopLevelAccess_ReturnsTrue**
   - 修复方案同上，将 `"module"` 改为 `"module_*"`

5. **HasPermission_WhenPermissionHasMultipleUnderscores_HandlesCorrectly**
   - 修复方案：将 `"module_controller"` 改为 `"module_controller_*"`

6. **HasPermission_CaseSensitivity_WorksAsExpected**
   - **问题**：测试中期望 `Module` 不能匹配 `module_controller_action`，但由于大小写不敏感，仍然会匹配
   - **修复方案**：测试逻辑需要重新设计

### 2. HasPermissionServiceTests.cs

以下测试基于旧的导航权限提取逻辑，需要更新：

#### 失败测试列表

1. **HasNavigationPermission_UserWithThreeLevelPermissions_ShouldExtractOnlySecondLevelPermissions**
   - **问题**：期望三级权限自动提升为二级导航权限
   - **新逻辑**：三级权限不再自动提升
   - **修复方案**：
     ```csharp
     // 旧测试期望
     // 用户有 exam_examPapers_create -> 应能访问 exam_examPapers 导航
     
     // 新逻辑
     // 用户必须明确拥有 exam_examPapers 或 exam_* 或 exam_examPapers_* 才能访问导航
     
     // 更新测试数据
     var userPermissions = new HashSet<string>
     {
         "exam_examPapers",  // 添加二级权限
         "exam_examPapers_create",
         "exam_examRecords_update"
     };
     ```

2. **HasNavigationPermission_ParentPermissionInheritance_ShouldOnlyExtractSecondLevel**
   - 类似的问题，需要添加显式的二级权限

3. **HasNavigationPermission_ComplexScenario_ShouldWorkAsExpected**
   - **问题**：期望一级具体权限（如 `"exam"`, `"system"`）能被提取为导航权限
   - **新逻辑**：一级具体权限不会被提取（除非是通配 `exam_*`）
   - **修复方案**：
     ```csharp
     // 旧测试数据
     var input = new HashSet<string> { "exam", "system", "reports", ... };
     
     // 新逻辑下，这些一级权限不会被提取
     // 如果需要导航权限，应使用：
     var input = new HashSet<string> { "exam_*", "system_*", "reports_*", ... };
     ```

4. **HasNavigationPermission_SpecialPermissionKeys_ShouldWorkAsExpected**
   - 类似问题，需要使用通配权限或二级权限

## 快速修复脚本

### 批量替换建议

在 `PermissionServiceTests.cs` 中：

```csharp
// 查找所有这些模式并替换：

// 模式1：一级权限
{ "module" }                  -> { "module_*" }

// 模式2：二级权限作为通配
{ "module_controller" }       -> { "module_controller_*" }

// 模式3：多级权限作为通配
{ "module_controller_group" } -> { "module_controller_group_*" }
```

### 大小写敏感性测试更新

```csharp
[Fact]
public void HasPermission_CaseSensitivity_WorksAsExpected()
{
    // Arrange
    var mockServiceProvider = new Mock<IServiceProvider>();
    var mockCache = new Mock<IDistributedCache>();
    var mockLogger = new Mock<ILogger<PermissionService>>();

    var permissionService = new PermissionService(
        mockServiceProvider.Object,
        mockCache.Object,
        mockLogger.Object);

    // 使用不同模块名来测试大小写
    var permissionName = "identity_users_create";

    // 用户拥有不同模块的权限（大小写不同，但模块完全不同）
    var userPermissions = new HashSet<string>
    {
        "exam_*",  // 完全不同的模块
        "SYSTEM_*" // 完全不同的模块
    };

    // Act
    var result = permissionService.HasPermission(permissionName, userPermissions);

    // Assert
    Assert.False(result); // 应该返回false，因为没有identity模块的权限

    // 测试大小写不敏感的正确匹配
    var userPermissions2 = new HashSet<string> { "IDENTITY_*" };
    var result2 = permissionService.HasPermission(permissionName, userPermissions2);
    Assert.True(result2); // 应该返回true，因为有identity模块的通配权限（大小写不敏感）
}
```

## 导航权限测试更新

在 `HasPermissionServiceTests.cs` 中：

```csharp
// 如果期望用户能访问导航菜单，必须显式赋予：
// 1. 二级权限: exam_examPapers
// 2. 一级通配: exam_*
// 3. 二级通配: exam_examPapers_*

// 仅有三级权限不会显示导航菜单
```

## 测试更新优先级

### 高优先级（必须更新）
1. `HasPermission_WhenParentPermissionExists_ReturnsTrue`
2. `HasPermission_WhenModuleLevelPermissionExists_ReturnsTrue`
3. `HasNavigationPermission_ComplexScenario_ShouldWorkAsExpected`

### 中优先级（应该更新）
4. `HasPermission_WhenDeepLevelHierarchyWithMiddlePermission_ReturnsTrue`
5. `HasPermission_WhenMultiLevelPermissionWithTopLevelAccess_ReturnsTrue`
6. `HasNavigationPermission_UserWithThreeLevelPermissions_ShouldExtractOnlySecondLevelPermissions`

### 低优先级（可选更新）
7. `HasPermission_CaseSensitivity_WorksAsExpected`
8. `HasNavigationPermission_SpecialPermissionKeys_ShouldWorkAsExpected`

## 总结

新的权限逻辑更加明确和严格：
- ✅ 显式通配：必须使用 `*` 明确标识通配权限
- ✅ 导航控制：只有二级权限和通配权限显示导航
- ❌ 隐式继承：不再支持父级权限自动继承

所有测试都应更新为反映这种新的明确语义。

