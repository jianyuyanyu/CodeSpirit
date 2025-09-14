# CodeSpirit.Authorization 权限继承使用指南

## 概述

权限继承机制是 CodeSpirit.Authorization 组件的一个重要特性，它允许拥有特定权限的用户访问相关的其他接口，而无需单独授予每个接口的权限。这在实际业务场景中非常有用，可以简化权限管理并提升用户体验。

## 核心概念

### 权限继承的工作原理

1. **直接权限检查**：系统首先检查用户是否拥有接口的直接权限
2. **继承权限检查**：如果没有直接权限，系统会检查用户是否拥有配置的继承权限
3. **访问授权**：如果用户拥有直接权限或继承权限中的任何一个，则允许访问

### 权限继承的优势

- **简化权限管理**：减少需要单独配置的权限数量
- **提升用户体验**：避免因权限不足导致的功能异常
- **业务逻辑合理性**：符合实际业务场景的权限需求
- **灵活配置**：可以为不同接口配置不同的继承权限

## 使用方法

### 1. 基本语法

在控制器方法上使用 `Permission` 特性，并配置 `AllowInheritedPermissions` 属性：

```csharp
[Permission(AllowInheritedPermissions = new[] { "parent_permission" })]
public async Task<ActionResult<ApiResponse<DataType>>> YourMethod()
{
    // 方法实现
}
```

### 2. 单个继承权限

最常见的场景是配置单个继承权限：

```csharp
/// <summary>
/// 获取分类树形结构
/// </summary>
[HttpGet("tree")]
[DisplayName("获取分类树")]
[Permission(AllowInheritedPermissions = new[] { "survey_surveys" })]
public async Task<ActionResult<ApiResponse<List<SurveyCategoryDto>>>> GetCategoryTree([FromQuery] int? parentId = null)
{
    var result = await _surveyCategoryService.GetCategoryTreeAsync(parentId);
    return SuccessResponse(result);
}
```

### 3. 多个继承权限

如果需要允许多个权限继承，可以配置多个权限：

```csharp
/// <summary>
/// 获取用户选择列表
/// </summary>
[HttpGet("options")]
[DisplayName("获取用户选择")]
[Permission(AllowInheritedPermissions = new[] { "identity_roles", "identity_departments", "system_settings" })]
public async Task<ActionResult<ApiResponse<List<UserOptionDto>>>> GetUserOptions()
{
    var result = await _userService.GetUserOptionsAsync();
    return SuccessResponse(result);
}
```

## 实际应用场景

### 场景1：问卷分类下拉选择

**问题描述**：
- 用户拥有问卷管理权限（`survey_surveys`）
- 问卷管理界面需要显示分类下拉选择
- 分类接口需要分类管理权限（`survey_surveycategories_tree`）
- 导致分类下拉无法加载

**解决方案**：

```csharp
// 在 SurveyCategoriesController 中
[HttpGet("tree")]
[DisplayName("获取分类树")]
[Permission(AllowInheritedPermissions = new[] { "survey_surveys" })]
public async Task<ActionResult<ApiResponse<List<SurveyCategoryDto>>>> GetCategoryTree([FromQuery] int? parentId = null)
{
    var result = await _surveyCategoryService.GetCategoryTreeAsync(parentId);
    return SuccessResponse(result);
}

[HttpGet("enabled")]
[DisplayName("获取启用分类")]
[Permission(AllowInheritedPermissions = new[] { "survey_surveys" })]
public async Task<ActionResult<ApiResponse<List<SurveyCategoryDto>>>> GetEnabledCategories()
{
    var result = await _surveyCategoryService.GetEnabledCategoriesAsync();
    return SuccessResponse(result);
}
```

### 场景2：用户角色选择

**问题描述**：
- 用户管理界面需要显示角色下拉选择
- 用户拥有用户管理权限但没有角色管理权限

**解决方案**：

```csharp
// 在 RolesController 中
[HttpGet("options")]
[DisplayName("获取角色选择")]
[Permission(AllowInheritedPermissions = new[] { "identity_users" })]
public async Task<ActionResult<ApiResponse<List<RoleOptionDto>>>> GetRoleOptions()
{
    var result = await _roleService.GetRoleOptionsAsync();
    return SuccessResponse(result);
}
```

### 场景3：部门树形选择

**问题描述**：
- 多个模块需要部门选择功能
- 不希望为每个模块单独授予部门管理权限

**解决方案**：

```csharp
// 在 DepartmentsController 中
[HttpGet("tree")]
[DisplayName("获取部门树")]
[Permission(AllowInheritedPermissions = new[] { "identity_users", "identity_roles", "system_settings" })]
public async Task<ActionResult<ApiResponse<List<DepartmentTreeDto>>>> GetDepartmentTree()
{
    var result = await _departmentService.GetDepartmentTreeAsync();
    return SuccessResponse(result);
}
```

## 权限检查日志

系统会记录详细的权限检查日志，便于调试和审计：

```
[Information] User 123 has inherited permission survey_surveys for survey_surveycategories_tree
[Warning] User 456 does not have permission survey_surveycategories_tree or any inherited permissions
```

## 最佳实践

### 1. 权限设计原则

- **业务相关性**：只为业务上相关的权限配置继承关系
- **最小权限原则**：不要过度开放继承权限
- **明确性**：继承权限的配置应该清晰明确

### 2. 命名规范

- 使用清晰的权限名称，便于理解继承关系
- 遵循模块_控制器_操作的命名规范

### 3. 文档记录

- 在接口文档中明确说明继承权限的配置
- 记录权限继承的业务原因

### 4. 测试验证

```csharp
[Test]
public async Task GetCategoryTree_WithSurveyPermission_ShouldSucceed()
{
    // Arrange
    var user = CreateUserWithPermission("survey_surveys");
    
    // Act
    var result = await _controller.GetCategoryTree();
    
    // Assert
    Assert.IsTrue(result.IsSuccess);
}

[Test]
public async Task GetCategoryTree_WithoutAnyPermission_ShouldFail()
{
    // Arrange
    var user = CreateUserWithoutPermissions();
    
    // Act & Assert
    await Assert.ThrowsAsync<UnauthorizedAccessException>(
        () => _controller.GetCategoryTree());
}
```

## 注意事项

### 1. 安全考虑

- 权限继承不应该绕过重要的安全检查
- 对于敏感操作，应该要求直接权限而不是继承权限
- 定期审查权限继承配置的合理性

### 2. 性能影响

- 权限继承检查会增加少量的性能开销
- 系统会缓存权限检查结果，减少重复计算
- 避免配置过多的继承权限

### 3. 维护性

- 权限继承关系应该保持简单清晰
- 避免复杂的继承链条
- 定期清理不再需要的继承权限配置

## 故障排除

### 1. 权限继承不生效

**可能原因**：
- 继承权限名称配置错误
- 用户实际没有继承权限
- 权限缓存未更新

**排查步骤**：
1. 检查权限名称拼写
2. 验证用户是否拥有继承权限
3. 清除权限缓存重新测试
4. 查看权限检查日志

### 2. 权限检查日志

启用详细的权限检查日志：

```json
{
  "Logging": {
    "LogLevel": {
      "CodeSpirit.Authorization": "Information"
    }
  }
}
```

### 3. 调试技巧

```csharp
// 在开发环境中添加调试信息
[HttpGet("debug/permissions")]
[DisplayName("调试权限信息")]
public ActionResult<ApiResponse<object>> DebugPermissions()
{
    var currentUser = HttpContext.RequestServices.GetRequiredService<ICurrentUser>();
    var hasPermissionService = HttpContext.RequestServices.GetRequiredService<IHasPermissionService>();
    
    return SuccessResponse(new
    {
        UserId = currentUser.Id,
        UserPermissions = currentUser.Permissions,
        HasSurveyPermission = hasPermissionService.HasPermission("survey_surveys"),
        HasCategoryPermission = hasPermissionService.HasPermission("survey_surveycategories_tree")
    });
}
```

## 总结

权限继承机制是一个强大而灵活的功能，可以显著简化权限管理并提升用户体验。通过合理的配置和使用，可以解决许多实际业务场景中的权限问题。在使用时需要注意安全性、性能和维护性的平衡，确保权限继承配置的合理性和可维护性。
