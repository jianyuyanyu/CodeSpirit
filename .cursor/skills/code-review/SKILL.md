---
name: code-review
description: 基于 CodeSpirit 项目规范进行系统化代码审查。检查安全、数据库、异步编程、多语言、DTO、控制器、服务类等规范。当用户需要审查代码、检查代码质量、或准备提交代码时使用。
---

# 代码审查 Skill

## 快速开始

CodeSpirit 代码审查采用三级优先级体系：

1. **🔴 严重级别**：必须修复（安全、数据库、异步编程）
2. **🟡 重要级别**：影响质量（多语言、DTO、控制器、服务类）
3. **🟢 建议级别**：最佳实践（注释、命名、性能优化）

---

## 📌 重要说明：权限系统

**CodeSpirit 采用自动权限生成机制**：

- ✅ **自动生成**：框架会根据控制器和方法自动生成权限代码
  - 格式：`{module}_{controller}_{action}`
  - 例如：`identity_users_create`、`exam_questions_getList`

- ✅ **何时无需配置权限特性**：
  - 标准 CRUD 操作
  - 无特殊权限继承需求的操作
  - 大部分业务场景

- ⚠️ **何时需要 `[Permission]` 特性**：
  - 需要自定义权限名称
  - 需要配置权限继承（`AllowInheritedPermissions`）
  - 需要添加权限描述

**审查重点**：不要将"缺少权限特性"标记为问题，而应检查：
1. 是否有特殊权限需求未使用 `[Permission]` 配置
2. 匿名访问控制器是否正确标记 `[AllowAnonymous]`

---

## 审查工作流程

### 步骤 1：确定审查范围

明确需要审查的文件列表：
- 新创建的文件
- 修改的文件
- 特定目录下的文件

### 步骤 2：执行严重级别检查

优先检查安全、数据库、异步编程等严重问题：

- [ ] 安全与数据保护
- [ ] 数据库与迁移
- [ ] 异步编程
- [ ] 依赖注入
- [ ] 包管理

### 步骤 3：执行重要级别检查

检查影响代码质量的问题：

- [ ] 多语言国际化
- [ ] DTO 规范
- [ ] 控制器规范
- [ ] 服务类规范
- [ ] EF Core 查询优化
- [ ] 缓存策略
- [ ] 异常处理

### 步骤 4：执行建议级别检查

检查最佳实践：

- [ ] 代码注释
- [ ] 命名规范
- [ ] 时间处理
- [ ] 序列化
- [ ] 性能优化

### 步骤 5：生成审查报告

按以下结构生成审查报告：

1. **报告头部**：审查日期、范围、审查人
2. **📊 统计摘要**（优先显示）：
   - 问题数量统计表
   - 修复优先级建议
3. **🔴 严重问题**：按优先级列出问题详情
4. **🟡 重要问题**：列出影响质量的问题
5. **🟢 建议改进**：列出最佳实践建议
6. **备注说明**：其他说明或建议

---

## 🔴 严重级别检查清单

### 1. 安全与数据保护

- [ ] **密码明文存储**：密码必须使用哈希存储，禁止明文
- [ ] **SQL注入风险**：禁止使用 `FromSqlRaw` 拼接字符串，使用 EF Core 参数化查询
- [ ] **敏感信息泄露**：日志中禁止记录密码、Token、API密钥等敏感信息
- [ ] **DTO返回敏感字段**：使用 `[JsonIgnore]` 排除密码、密钥等字段
- [ ] **自定义权限配置**：特殊权限需求时使用 `[Permission]` 特性配置权限继承关系

**示例**：
```csharp
// ❌ 错误
public string Password { get; set; }  // 明文密码

// ✅ 正确
[JsonIgnore]
public string PasswordHash { get; set; }  // 哈希密码

// ✅ 自动权限生成（默认情况，无需额外配置）
[HttpPost]
[DisplayName("创建用户")]
public async Task<ActionResult> Create(CreateDto dto) { }
// 自动生成权限：identity_users_create

// ✅ 自定义权限配置（仅在需要继承关系时）
[HttpPut("{id}")]
[Permission(
    Name = "exam_questions_update",
    AllowInheritedPermissions = new[] { "exam_questions_manage" })]
[DisplayName("更新题目")]
public async Task<ActionResult> Update(long id, UpdateDto dto) { }
```

### 2. 数据库与迁移

- [ ] **雪花ID配置缺失**：使用 `IIdGenerator` 的实体必须配置 `ValueGeneratedNever()`
- [ ] **错误的DbContext迁移**：必须使用数据库特定的 DbContext（`SqlServer{Service}DbContext` / `MySql{Service}DbContext`）
- [ ] **多租户隔离缺失**：需要租户隔离的实体必须实现 `IMultiTenant` 接口
- [ ] **审计实体基类缺失**：实体应继承 `AuditableEntityBase<TKey>` 实现自动审计

**示例**：
```csharp
// ❌ 错误
public class Question : AuditableEntityBase<long> { }  // 缺少多租户

// ✅ 正确
public class Question : AuditableEntityBase<long>, IMultiTenant
{
    public string TenantId { get; set; } = string.Empty;
}

// ❌ 错误
builder.Property(x => x.Id);  // 缺少 ValueGeneratedNever

// ✅ 正确
builder.Property(x => x.Id).ValueGeneratedNever();  // 雪花ID配置
```

### 3. 异步编程

- [ ] **阻塞调用**：禁止使用 `Task.Result` 和 `Task.Wait()`
- [ ] **同步I/O操作**：所有 I/O 操作必须使用 `async/await`
- [ ] **异步循环**：避免在循环中执行异步操作，使用批量处理

**示例**：
```csharp
// ❌ 错误
public List<UserDto> GetUsers() => _repository.GetList().Result;

// ✅ 正确
public async Task<List<UserDto>> GetUsersAsync() => await _repository.GetListAsync();

// ❌ 错误
foreach (var item in items) {
    await ProcessAsync(item);  // 循环中异步
}

// ✅ 正确
await Task.WhenAll(items.Select(item => ProcessAsync(item)));  // 批量处理
```

### 4. 依赖注入

- [ ] **缺少生命周期标记**：服务类必须实现 `IScopedDependency` / `ITransientDependency` / `ISingletonDependency`
- [ ] **手动重复注册**：标记接口的服务已由 Scrutor 自动注册，禁止手动注册

**示例**：
```csharp
// ❌ 错误
public interface IUserService { }  // 缺少标记接口

// ✅ 正确
public interface IUserService : IScopedDependency { }
```

### 5. 包管理

- [ ] **包版本不一致**：项目文件中禁止指定版本，必须使用 `Directory.Packages.props` 集中管理
- [ ] **冗余包引用**：避免引用通过传递依赖已可用的包

---

## 🟡 重要级别检查清单

### 6. 多语言国际化

- [ ] **硬编码字符串**：所有面向用户的文本必须支持多语言
- [ ] **缺少Display特性**：DTO 属性必须添加 `[Display(Name = "...", ResourceType = typeof(...))]`
- [ ] **缺少资源文件**：必须同时提供中文（`.resx`）和英文（`.en.resx`）资源文件
- [ ] **验证消息未本地化**：验证特性必须使用 `ErrorMessageResourceType` 和 `ErrorMessageResourceName`

**示例**：
```csharp
// ❌ 错误
return SuccessResponse("操作成功！");

// ✅ 正确
return SuccessResponse(message: _localizer["Common.Save"].Value);

// ❌ 错误
[DisplayName("姓名")]
public string Name { get; set; }

// ✅ 正确
[Display(Name = "Name", ResourceType = typeof(Resources))]
public string Name { get; set; }
```

### 7. DTO 规范

- [ ] **缺少验证特性**：Create/Update DTO 必须添加验证特性（`[Required]`、`[StringLength]` 等）
- [ ] **查询DTO未继承基类**：查询 DTO 必须继承 `QueryDtoBase`
- [ ] **缺少AutoMapper配置**：DTO 添加后必须完善映射配置文件

### 8. 控制器规范

- [ ] **缺少DisplayName特性**：所有控制器方法必须添加 `[DisplayName]` 特性
- [ ] **缺少Navigation特性**：控制器必须添加 `[Navigation]` 特性配置菜单
- [ ] **缺少操作特性**：非标准 CRUD 操作必须添加 `[Operation]` 或派生特性
- [ ] **返回类型错误**：所有方法应返回 `ActionResult<ApiResponse<T>>`

### 9. 服务类规范

- [ ] **未继承基类**：服务类应继承 `BaseCRUDService` 或实现 `IBaseCRUDService`
- [ ] **缺少XML文档注释**：公共成员必须添加 XML 文档注释（`<summary>`、`<param>`、`<returns>`）

### 10. EF Core 查询优化

- [ ] **缺少AsNoTracking**：只读查询必须使用 `AsNoTracking()`
- [ ] **N+1查询问题**：关联数据必须使用 `Include()` 预加载
- [ ] **缺少AsSplitQuery**：多对多关联应使用 `AsSplitQuery()` 避免笛卡尔积
- [ ] **缺少批量操作**：批量更新/删除应使用 `ExecuteUpdateAsync` / `ExecuteDeleteAsync`

**示例**：
```csharp
// ❌ 错误
var users = await _repository.GetListAsync();  // 缺少 AsNoTracking

// ✅ 正确
var users = await _repository.CreateQuery()
    .AsNoTracking()
    .ToListAsync();

// ❌ 错误
var questions = await _repository.GetListAsync();
foreach (var q in questions) {
    var category = q.Category;  // N+1 查询
}

// ✅ 正确
var questions = await _repository.CreateQuery()
    .Include(q => q.Category)
    .AsNoTracking()
    .ToListAsync();
```

### 11. 缓存策略

- [ ] **缺少缓存**：频繁访问的数据应使用缓存（`ICacheService`）
- [ ] **缓存键命名不规范**：使用格式 `{service}:{entity}:{identifier}` 或 `{tenantId}:{service}:{entity}:{identifier}`
- [ ] **缺少缓存失效**：数据更新后必须清除相关缓存

### 12. 异常处理

- [ ] **在Action中捕获异常**：不在 Action 中捕获异常，由统一异常过滤器处理
- [ ] **异常消息未本地化**：使用资源键 `throw new BusinessException("Errors.NotFound")`

---

## 🟢 建议级别检查清单

### 13. 代码注释

- [ ] **缺少行内注释**：复杂业务逻辑应添加行内注释说明
- [ ] **缺少枚举XML注释**：枚举值必须添加 XML 文档注释

### 14. 命名规范

- [ ] **实体命名**：使用单数形式（`User`、`Question`）
- [ ] **DTO命名**：`Create{Entity}Dto`、`Update{Entity}Dto`、`{Entity}QueryDto`
- [ ] **控制器命名**：使用复数形式（`UsersController`、`QuestionsController`）
- [ ] **服务命名**：`{Entity}Service`、`I{Entity}Service`

### 15. 时间处理

- [ ] **未使用UTC时间**：数据库存储使用 UTC 时间，前端显示时转换为本地时间

### 16. 序列化

- [ ] **未使用Newtonsoft.Json**：统一使用 `Newtonsoft.Json` 而非 `System.Text.Json`

### 17. 性能优化

- [ ] **缺少投影查询**：只查询需要的字段，避免查询整个实体
- [ ] **缺少分布式锁**：并发操作应使用分布式锁
- [ ] **缺少事件驱动**：异步处理应使用事件总线解耦

---

## 常见违规模式速查

### 硬编码字符串
```csharp
// ❌ 错误
return SuccessResponse("操作成功！");

// ✅ 正确
return SuccessResponse(message: _localizer["Common.Save"].Value);
```

### 缺少异步
```csharp
// ❌ 错误
public List<UserDto> GetUsers() => _repository.GetList().Result;

// ✅ 正确
public async Task<List<UserDto>> GetUsersAsync() => await _repository.GetListAsync();
```

### 权限配置（仅在需要自定义时）
```csharp
// ✅ 默认情况：自动生成权限（无需额外配置）
[HttpPost]
[DisplayName("创建题目")]
public async Task<ActionResult> Create(CreateDto dto) { }
// 框架自动生成权限：exam_questions_create

// ✅ 自定义权限：需要权限继承时使用 [Permission] 特性
[HttpPut("{id}")]
[Permission(AllowInheritedPermissions = new[] { "exam_questions_manage" })]
[DisplayName("更新题目")]
public async Task<ActionResult> Update(long id, UpdateDto dto) { }
```

### 缺少多租户隔离
```csharp
// ❌ 错误
public class Question : AuditableEntityBase<long> { }

// ✅ 正确
public class Question : AuditableEntityBase<long>, IMultiTenant
{
    public string TenantId { get; set; } = string.Empty;
}
```

### 匿名访问控制器未标记
```csharp
// ❌ 错误：登录接口未标记允许匿名访问
public class AuthController : ApiControllerBase { }

// ✅ 正确
[AllowAnonymous]
[Navigation(Hidden = true)]
public class AuthController : ApiControllerBase { }
```

### 缺少XML注释
```csharp
// ❌ 错误
public class UserService { }

// ✅ 正确
/// <summary>
/// 用户服务
/// </summary>
public class UserService { }
```

---

## 审查报告模板

使用 [report-template.md](report-template.md) 生成结构化的审查报告。

---

## 审查技巧

### 按文件类型审查

- **实体文件**：检查接口实现、多租户、审计字段、雪花ID配置
- **DTO文件**：检查验证特性、Display特性、AMIS特性、多语言支持
- **服务文件**：检查基类继承、依赖注入标记、XML注释、异步方法
- **控制器文件**：检查特性标记、返回类型、权限控制、操作特性

### 使用工具辅助

- **静态分析工具**：使用 IDE 的代码分析功能
- **搜索功能**：搜索常见违规模式（如 `Task.Result`、硬编码字符串）
- **规则文件**：参考项目的 24 个规则文件

---

## 权限审查指南

### 何时标记为问题

❌ **不要标记为问题**：
- 控制器方法缺少 `[RequirePermission]` 特性（框架自动生成）
- 标准 CRUD 操作未配置权限特性

✅ **应标记为问题**：
- 需要权限继承但未使用 `[Permission]` 特性配置
- 公开 API（如登录、注册）未标记 `[AllowAnonymous]`
- 内部 API 未标记 `[DisableAggregator]`

### 权限配置示例

```csharp
// ✅ 标准操作：自动生成权限（无需配置）
[HttpPost]
[DisplayName("创建用户")]
public async Task<ActionResult> Create(CreateUserDto dto) { }
// 自动生成：identity_users_create

// ✅ 权限继承：需要 [Permission] 特性
[HttpPut("{id}")]
[Permission(AllowInheritedPermissions = new[] { "identity_users_manage" })]
[DisplayName("更新用户")]
public async Task<ActionResult> Update(long id, UpdateUserDto dto) { }

// ✅ 公开 API：需要 [AllowAnonymous]
[AllowAnonymous]
[Navigation(Hidden = true)]
public class AuthController : ApiControllerBase { }
```

---

## 相关资源

- [项目规范文档](../../rules/)
- [审查标准](STANDARDS.md)
- [审查报告模板](report-template.md)
- [权限规范](../../rules/security.mdc)
- [控制器规范](../../rules/controller.mdc)
