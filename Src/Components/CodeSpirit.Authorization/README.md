# CodeSpirit.Authorization 权限组件

CodeSpirit 权限管理组件，提供基于角色和权限的访问控制功能。

## 📋 目录

- [核心概念](#核心概念)
- [权限格式](#权限格式)
- [快速开始](#快速开始)
- [权限检查](#权限检查)
- [导航权限](#导航权限)
- [权限优化](#权限优化)
- [最佳实践](#最佳实践)
- [迁移指南](#迁移指南)

## 核心概念

### 权限层级

权限采用三级层级结构，使用下划线 `_` 分隔：

```
一级：module（模块）
二级：module_controller（模块_控制器）
三级：module_controller_action（模块_控制器_操作）
```

**示例：**
- `identity` - 身份模块（一级）
- `identity_users` - 用户管理（二级）
- `identity_users_create` - 创建用户（三级）

### 权限类型

#### 1. 具体权限
不带通配符的精确权限，用于授予特定操作的访问权限。

```csharp
"identity_users_create"   // 创建用户
"identity_users_update"   // 更新用户
"identity_roles_delete"   // 删除角色
```

#### 2. 通配权限
使用 `*` 号明确标识的权限，可以匹配其下所有子权限。

**⚠️ 重要：通配权限必须显式使用 `*` 号！**

```csharp
"identity_*"              // identity 模块下所有权限（一级通配）
"identity_users_*"        // identity_users 控制器下所有权限（二级通配）
"exam_questions_*"        // exam_questions 控制器下所有权限（二级通配）
```

## 权限格式

### ✅ 正确的权限格式

```csharp
// 通配权限 - 必须显式使用 *
"identity_*"              // ✅ 匹配 identity 模块下所有权限
"identity_users_*"        // ✅ 匹配 identity_users 控制器下所有操作

// 具体权限 - 明确的操作
"identity_users"          // ✅ 二级权限（用于导航）
"identity_users_create"   // ✅ 三级权限（具体操作）
```

### ❌ 错误的权限格式

```csharp
// 旧逻辑（已废弃）- 不再支持隐式通配
"identity"                // ❌ 不会匹配 identity_users_create
"identity_users"          // ❌ 不会匹配 identity_users_create

// 必须改为显式通配
"identity_*"              // ✅ 正确
"identity_users_*"        // ✅ 正确
```

## 快速开始

### 1. 注册服务

```csharp
// 在 Program.cs 或 Startup.cs 中
builder.Services.AddAuthorizationServices();
```

### 2. 配置权限

```csharp
// 为角色分配权限
var permissions = new[]
{
    // 方式1：使用通配权限（推荐）
    "identity_*",                    // 授予 identity 模块所有权限
    
    // 方式2：使用二级权限（用于导航）
    "exam_examPapers",               // 授予试卷管理导航权限
    
    // 方式3：使用具体权限
    "exam_examRecords_view",         // 授予查看考试记录的权限
    "exam_examRecords_export"        // 授予导出考试记录的权限
};

await roleService.UpdateRolePermissions(roleId, permissions);
```

### 3. 控制器上使用权限

```csharp
[ApiController]
[Route("api/[controller]")]
[DisplayName("用户管理")]
public class UsersController : ApiControllerBase
{
    // 自动权限：identity_users_create
    [HttpPost]
    [DisplayName("创建用户")]
    public async Task<ActionResult<ApiResponse>> Create([FromBody] CreateUserDto dto)
    {
        // 控制器会自动检查权限
        // ...
    }

    // 自定义权限名称
    [HttpPut("{id}")]
    [Permission("identity_users_update")]
    [DisplayName("更新用户")]
    public async Task<ActionResult<ApiResponse>> Update(long id, [FromBody] UpdateUserDto dto)
    {
        // ...
    }
}
```

## 权限检查

### 使用 IHasPermissionService

```csharp
public class MyService
{
    private readonly IHasPermissionService _hasPermissionService;

    public MyService(IHasPermissionService hasPermissionService)
    {
        _hasPermissionService = hasPermissionService;
    }

    public async Task DoSomething()
    {
        // 检查具体权限
        if (await _hasPermissionService.HasPermission("identity_users_create"))
        {
            // 用户有创建用户的权限
        }

        // 检查导航权限
        if (await _hasPermissionService.HasNavigationPermission("identity_users"))
        {
            // 显示用户管理菜单
        }
    }
}
```

### 权限匹配规则

```csharp
// 用户拥有的权限
var userPermissions = new[] { "identity_*" };

// 权限检查结果
HasPermission("identity_users_create")      // ✅ true - 被 identity_* 覆盖
HasPermission("identity_users_update")      // ✅ true - 被 identity_* 覆盖
HasPermission("identity_roles_delete")      // ✅ true - 被 identity_* 覆盖
HasPermission("exam_questions_create")      // ❌ false - 不在 identity 模块下

// 用户拥有二级通配权限
var userPermissions2 = new[] { "identity_users_*" };

HasPermission("identity_users_create")      // ✅ true - 被 identity_users_* 覆盖
HasPermission("identity_users_update")      // ✅ true - 被 identity_users_* 覆盖
HasPermission("identity_roles_delete")      // ❌ false - 不在 identity_users 控制器下
```

### 大小写不敏感

所有权限匹配都是大小写不敏感的：

```csharp
// 以下权限等效
"identity_*"
"IDENTITY_*"
"Identity_*"

// 匹配也是大小写不敏感的
HasPermission("IDENTITY_USERS_CREATE")  // ✅ 可以被 identity_* 匹配
```

## 导航权限

导航权限用于控制菜单显示，有严格的提取规则。

### 导航权限提取规则

**只有以下两种权限会被提取为导航权限：**

1. **二级具体权限**（格式：`module_controller`）
2. **通配权限**（格式：`module_*` 或 `module_controller_*`）

**三级权限不会自动提取为导航权限！**

### 示例说明

```csharp
// 用户拥有的权限
var userPermissions = new[]
{
    "exam_examPapers",              // 二级权限 ✅ 会被提取
    "exam_examRecords_view",        // 三级权限 ❌ 不会被提取
    "exam_questions_*",             // 二级通配 ✅ 会被提取
    "identity_*",                   // 一级通配 ✅ 会被提取
    "reports_sales_export"          // 三级权限 ❌ 不会被提取
};

// 导航权限检查结果
HasNavigationPermission("exam_examPapers")     // ✅ true - 有二级权限
HasNavigationPermission("exam_examRecords")    // ❌ false - 只有三级权限，不提取
HasNavigationPermission("exam_questions")      // ✅ true - 有二级通配
HasNavigationPermission("identity_users")      // ✅ true - 有一级通配
HasNavigationPermission("reports_sales")       // ❌ false - 只有三级权限，不提取
```

### 如何显示导航菜单

如果要让用户看到某个导航菜单，必须赋予以下权限之一：

```csharp
// 选项1：赋予二级权限（精确控制）
"exam_examPapers"

// 选项2：赋予二级通配权限（控制器级别）
"exam_examPapers_*"

// 选项3：赋予一级通配权限（模块级别）
"exam_*"
```

**⚠️ 仅赋予三级权限不会显示导航菜单！**

```csharp
// ❌ 错误做法 - 不会显示导航
var permissions = new[] { "exam_examPapers_view", "exam_examPapers_create" };

// ✅ 正确做法 - 会显示导航
var permissions = new[] { "exam_examPapers" };  // 或 "exam_examPapers_*" 或 "exam_*"
```

## 权限优化

系统会自动优化权限存储，移除冗余的权限。

### 优化规则

当保存权限时，如果存在通配权限，会自动移除被其覆盖的具体权限：

```csharp
// 保存前的权限
var permissionsToSave = new[]
{
    "identity_*",                   // 一级通配
    "identity_users_create",        // 被 identity_* 覆盖
    "identity_users_update",        // 被 identity_* 覆盖
    "identity_roles_delete",        // 被 identity_* 覆盖
    "exam_questions_*",             // 二级通配
    "exam_questions_create",        // 被 exam_questions_* 覆盖
    "exam_questions_update"         // 被 exam_questions_* 覆盖
};

// 保存后的权限（自动优化）
var optimizedPermissions = new[]
{
    "identity_*",                   // 保留
    "exam_questions_*"              // 保留
};
// 冗余权限已被自动移除
```

### 优化示例

```csharp
// 示例1：一级通配覆盖所有子权限
输入：["identity_*", "identity_users_create", "identity_roles_update"]
输出：["identity_*"]

// 示例2：二级通配覆盖该控制器下所有操作
输入：["identity_users_*", "identity_users_create", "identity_users_update"]
输出：["identity_users_*"]

// 示例3：不同模块的权限不会相互影响
输入：["identity_*", "exam_questions_create"]
输出：["identity_*", "exam_questions_create"]

// 示例4：混合场景
输入：["identity_users_*", "identity_roles_create", "identity_roles_update"]
输出：["identity_users_*", "identity_roles_create", "identity_roles_update"]
```

## 最佳实践

### 1. 权限分配策略

```csharp
// ✅ 推荐：模块管理员 - 使用一级通配
var moduleAdminPermissions = new[] { "identity_*" };

// ✅ 推荐：控制器管理员 - 使用二级通配
var controllerAdminPermissions = new[] { "identity_users_*", "identity_roles_*" };

// ✅ 推荐：导航权限 - 使用二级权限
var navigationPermissions = new[] { "exam_examPapers", "exam_questions" };

// ✅ 推荐：具体操作 - 使用三级权限
var specificPermissions = new[] 
{ 
    "exam_examPapers_view", 
    "exam_examPapers_export",
    "exam_questions_create"
};

// ✅ 推荐：混合使用
var mixedPermissions = new[]
{
    "identity_*",              // 身份模块完全管理
    "exam_examPapers",         // 试卷管理导航
    "exam_examRecords_view",   // 只能查看考试记录
    "exam_questions_*"         // 题目管理完全控制
};
```

### 2. 控制器权限命名

```csharp
// ✅ 推荐：让系统自动生成权限名
[ApiController]
[Route("api/[controller]")]
[DisplayName("用户管理")]
public class UsersController : ApiControllerBase
{
    [HttpPost]
    [DisplayName("创建用户")]
    public async Task<ActionResult> Create()
    {
        // 自动生成权限：identity_users_create
    }
}

// ⚠️ 特殊情况：需要自定义权限名
[HttpPut("bulk-update")]
[Permission("identity_users_batchUpdate")]
[DisplayName("批量更新用户")]
public async Task<ActionResult> BulkUpdate()
{
    // 使用自定义权限：identity_users_batchUpdate
}
```

### 3. 权限检查位置

```csharp
// ✅ 控制器级别 - 自动权限检查
[ApiController]
public class UsersController : ApiControllerBase
{
    // 框架会自动检查权限
}

// ✅ 服务层 - 业务逻辑检查
public class UserService
{
    public async Task CreateUser(CreateUserDto dto)
    {
        // 业务逻辑中检查特殊权限
        if (dto.IsAdmin)
        {
            if (!await _hasPermissionService.HasPermission("identity_users_createAdmin"))
            {
                throw new UnauthorizedException("无权创建管理员用户");
            }
        }
    }
}

// ✅ 前端 - UI 控制
public async Task<NavigationTree> GetNavigationTree()
{
    // 前端根据导航权限显示菜单
    if (await _hasPermissionService.HasNavigationPermission("identity_users"))
    {
        // 显示用户管理菜单
    }
}
```

### 4. 避免的做法

```csharp
// ❌ 不要：依赖一级具体权限
var permissions = new[] { "identity" };  // 不会匹配任何子权限

// ✅ 应该：使用通配权限
var permissions = new[] { "identity_*" };

// ❌ 不要：期望三级权限自动显示导航
var permissions = new[] { "exam_examPapers_view" };  // 不会显示导航菜单

// ✅ 应该：明确添加二级权限或通配权限
var permissions = new[] { "exam_examPapers" };  // 或 "exam_*"

// ❌ 不要：手动管理冗余权限
var permissions = new[] { "identity_*", "identity_users_create" };

// ✅ 应该：让系统自动优化
var permissions = new[] { "identity_*" };  // 系统会自动移除冗余权限
```

## 迁移指南

如果您的系统之前使用旧的隐式通配逻辑，需要进行以下迁移：

### 1. 权限数据迁移

```sql
-- 将一级权限转换为通配权限
-- 旧数据：identity
-- 新数据：identity_*

UPDATE Roles 
SET PermissionIds = REPLACE(PermissionIds, '"identity"', '"identity_*"')
WHERE PermissionIds LIKE '%"identity"%'
  AND PermissionIds NOT LIKE '%identity_%';

-- 对所有模块重复此操作
```

### 2. 添加导航权限

```csharp
// 如果用户只有三级权限但需要看到导航菜单
// 需要添加对应的二级权限

// 旧配置（只有操作权限，看不到菜单）
var oldPermissions = new[]
{
    "exam_examPapers_view",
    "exam_examPapers_create"
};

// 新配置（添加导航权限）
var newPermissions = new[]
{
    "exam_examPapers",          // 添加：显示导航菜单
    "exam_examPapers_view",
    "exam_examPapers_create"
};

// 或使用通配权限（更简洁）
var betterPermissions = new[]
{
    "exam_examPapers_*"         // 包含所有功能和导航
};
```

### 3. 更新代码中的权限检查

```csharp
// ❌ 旧代码
if (user.Permissions.Contains("identity"))
{
    // ...
}

// ✅ 新代码
if (await _hasPermissionService.HasPermission("identity_users_create"))
{
    // 使用具体的权限检查
}

// 或者用户应该被赋予通配权限
var permissions = new[] { "identity_*" };
```

## 技术细节

### 权限匹配算法

```csharp
public bool HasPermission(string permissionName, ISet<string> userPermissions)
{
    // 1. default_ 前缀直接放通
    if (permissionName.StartsWith("default_"))
        return true;

    // 2. 精确匹配
    if (userPermissions.Contains(permissionName, StringComparer.OrdinalIgnoreCase))
        return true;

    // 3. 通配符匹配
    var parts = permissionName.Split('_');
    
    // 逐级检查通配符：identity_* -> identity_users_*
    for (int i = 0; i < parts.Length - 1; i++)
    {
        var wildcardPermission = string.Join("_", parts.Take(i + 1)) + "_*";
        
        if (userPermissions.Contains(wildcardPermission, StringComparer.OrdinalIgnoreCase))
            return true;
    }

    return false;
}
```

### 导航权限提取算法

```csharp
private HashSet<string> ExtractNavigationPermissions(ISet<string> permissions)
{
    var navigationPermissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    foreach (var permission in permissions)
    {
        if (permission.EndsWith("_*"))
        {
            var parts = permission.Split('_');
            
            // 只保留一级通配（module_*）和二级通配（module_controller_*）
            if (parts.Length <= 3)
            {
                navigationPermissions.Add(permission);
            }
        }
        else
        {
            var parts = permission.Split('_');
            
            // 只保留二级具体权限（module_controller）
            if (parts.Length == 2)
            {
                navigationPermissions.Add(permission);
            }
        }
    }

    return navigationPermissions;
}
```

## 常见问题

### Q1: 为什么我的导航菜单不显示？

**A:** 检查以下几点：
1. 是否只赋予了三级权限？三级权限不会显示导航。
2. 是否需要添加二级权限或通配权限？
3. 使用 `HasNavigationPermission` 方法检查。

```csharp
// ❌ 只有三级权限 - 不显示导航
var permissions = new[] { "exam_examPapers_view", "exam_examPapers_create" };

// ✅ 添加二级权限 - 显示导航
var permissions = new[] { "exam_examPapers" };
```

### Q2: 通配权限必须用 * 号吗？

**A:** 是的！新逻辑要求显式使用 `*` 号来标识通配权限。

```csharp
// ❌ 错误 - 不会作为通配符
"identity"              // 只是一个一级具体权限

// ✅ 正确 - 通配符必须显式标记
"identity_*"            // 明确的通配权限
```

### Q3: 如何给用户完全的模块访问权限？

**A:** 使用一级通配权限：

```csharp
var permissions = new[] { "identity_*" };  // 授予 identity 模块所有权限
```

### Q4: 权限是区分大小写的吗？

**A:** 不区分。所有权限匹配都是大小写不敏感的。

```csharp
"identity_*" == "IDENTITY_*" == "Identity_*"  // 这些都是等效的
```

### Q5: 如何查看用户的实际权限？

**A:** 使用当前用户服务：

```csharp
public class MyController : ApiControllerBase
{
    private readonly ICurrentUser _currentUser;

    public async Task<ActionResult> GetMyPermissions()
    {
        var permissions = _currentUser.Permissions;  // 用户的所有权限
        var roles = _currentUser.Roles;              // 用户的所有角色
        
        return Ok(new { permissions, roles });
    }
}
```

## 更新日志

### v2.0.0 (2025-11-02)
- 🔄 **重大变更**：移除隐式通配逻辑，改为显式通配（必须使用 `*`）
- ✨ 新增：自动权限优化功能
- ✨ 新增：严格的导航权限提取规则
- 🔧 修复：大小写不敏感匹配
- 📝 新增：完整的文档和迁移指南

### v1.0.0
- 初始版本，使用隐式通配逻辑（已废弃）

## 参考资料

- [测试文档](../../../Tests/Components/CodeSpirit.Authorization.Tests/README.md)
- [测试迁移指南](../../../Tests/Components/CodeSpirit.Authorization.Tests/TEST_MIGRATION_GUIDE.md)
- [完成总结](../../../Tests/Components/CodeSpirit.Authorization.Tests/COMPLETION_SUMMARY.md)

## 许可证

此组件是 CodeSpirit 项目的一部分。

---

**维护团队**: CodeSpirit Team  
**最后更新**: 2025-11-02  
**版本**: 2.0.0

