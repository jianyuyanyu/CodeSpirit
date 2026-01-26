---
name: code-review
description: 基于 CodeSpirit 项目规范进行系统化代码审查。检查安全、数据库、异步编程、多语言、DTO、控制器、服务类等规范。当用户需要审查代码、检查代码质量、或准备提交代码时使用。
---

# 代码审查 Skill

## 快速开始

CodeSpirit 代码审查采用三级优先级体系：

1. **🔴 严重级别**：必须修复（安全、数据库、异步编程、反射调用）
2. **🟡 重要级别**：影响质量（多语言、DTO、控制器、服务类）
3. **🟢 建议级别**：最佳实践（注释、命名、性能优化）

---

## 审查工作流程

### 步骤 1：确定审查范围

明确需要审查的文件列表（通过 git status、文件列表等方式）。

### 步骤 2：执行严重级别检查（🔴 优先）

按优先级依次检查，发现严重问题立即标记：

1. **安全与数据保护** → 📖 [详细规范](mdc:../../rules/security.mdc)
   - [ ] 密码明文存储、SQL注入、敏感信息泄露
   - [ ] DTO返回敏感字段（需 `[JsonIgnore]`）

2. **数据库与迁移** → 📖 [详细规范](mdc:../../rules/database.mdc)
   - [ ] 雪花ID缺少 `ValueGeneratedNever()` 配置
   - [ ] 迁移使用错误的 DbContext（必须用数据库特定的）
   - [ ] 多租户实体缺少 `IMultiTenant` 接口

3. **异步编程** → 📖 [详细规范](mdc:../../rules/performance.mdc)
   - [ ] 使用 `Task.Result` 或 `Task.Wait()` 阻塞调用
   - [ ] I/O 操作未使用 async/await

4. **反射调用（新增）**
   - [ ] 使用反射调用扩展方法（应使用回调委托）
   - [ ] 反射导致编译时类型安全缺失

5. **依赖注入** → 📖 [详细规范](mdc:../../rules/dependency-injection.mdc)
   - [ ] 服务类缺少生命周期标记接口
   - [ ] 手动重复注册已自动注册的服务

6. **包管理** → 📖 [详细规范](mdc:../../rules/package-management.mdc)
   - [ ] 项目文件中指定版本（应在 Directory.Packages.props）
   - [ ] 冗余包引用（传递依赖已可用）

### 步骤 3：执行重要级别检查（🟡）

1. **多语言国际化** → 📖 [详细规范](mdc:../../rules/i18n.mdc)
   - [ ] 硬编码字符串（需使用 `_localizer`）
   - [ ] DTO 缺少 `[Display]` 特性
   - [ ] 验证消息未本地化

2. **DTO 规范** → 📖 [详细规范](mdc:../../rules/dto.mdc)
   - [ ] 缺少验证特性（`[Required]`、`[StringLength]`）
   - [ ] 查询 DTO 未继承 `QueryDtoBase`

3. **控制器规范** → 📖 [详细规范](mdc:../../rules/controller.mdc)
   - [ ] 缺少 `[DisplayName]` 特性
   - [ ] 返回类型不是 `ActionResult<ApiResponse<T>>`

4. **服务类规范** → 📖 [详细规范](mdc:../../rules/service.mdc)
   - [ ] 未继承 `BaseCRUDService` 或实现 `IBaseCRUDService`
   - [ ] 缺少 XML 文档注释

5. **EF Core 查询优化**
   - [ ] 只读查询缺少 `AsNoTracking()`
   - [ ] N+1 查询问题（需用 `Include()`）

### 步骤 4：执行建议级别检查（🟢）

1. **代码注释** - 复杂逻辑添加注释
2. **命名规范** → 📖 [详细规范](mdc:../../rules/naming-conventions.mdc)
3. **时间处理** - 优先使用 UTC 时间
4. **序列化** - 统一使用 Newtonsoft.Json
5. **性能优化** - 投影查询、分布式锁、事件驱动

### 步骤 5：生成审查报告

使用 [report-template.md](report-template.md) 生成结构化审查报告：

1. **📊 统计摘要**（优先显示）- 问题数量和优先级
2. **🔴 严重问题** - 必须修复
3. **🟡 重要问题** - 影响质量
4. **🟢 建议改进** - 最佳实践

---

## 📌 特别注意：权限系统

**CodeSpirit 采用自动权限生成**，大部分情况无需配置 `[Permission]` 特性。

### ✅ 不要标记为问题
- 标准 CRUD 操作缺少 `[RequirePermission]`（框架自动生成）

### ❌ 应标记为问题
- 需要权限继承但未用 `[Permission]` 配置
- 公开 API 未标记 `[AllowAnonymous]`

**示例**：
```csharp
// ✅ 自动生成权限（无需配置）
[HttpPost]
[DisplayName("创建用户")]
public async Task<ActionResult> Create(CreateDto dto) { }

// ✅ 需要权限继承时才配置
[HttpPut("{id}")]
[Permission(AllowInheritedPermissions = new[] { "users_manage" })]
[DisplayName("更新用户")]
public async Task<ActionResult> Update(long id, UpdateDto dto) { }
```

---

## 常见问题速查

### 反射调用扩展方法 🔴
```csharp
// ❌ 错误
method?.Invoke(null, new object[] { services, configuration });

// ✅ 正确：使用回调委托
this.ConfigureStandardInfrastructureServices(services, configuration, (s, c) =>
{
    s.AddCodeSpiritMultiTenant(c);
});
```

### 硬编码字符串 🟡
```csharp
// ❌ 错误
return SuccessResponse("操作成功！");

// ✅ 正确
return SuccessResponse(message: _localizer["Common.Save"].Value);
```

### 阻塞调用 🔴
```csharp
// ❌ 错误
var result = _repository.GetListAsync().Result;

// ✅ 正确
var result = await _repository.GetListAsync();
```

### 多租户隔离缺失 🔴
```csharp
// ❌ 错误
public class Question : AuditableEntityBase<long> { }

// ✅ 正确
public class Question : AuditableEntityBase<long>, IMultiTenant
{
    public string TenantId { get; set; } = string.Empty;
}
```

---

## 审查技巧

### 按文件类型审查
- **实体**：接口实现、多租户、审计字段、雪花ID
- **DTO**：验证特性、Display特性、多语言
- **服务**：基类继承、依赖注入、XML注释、异步
- **控制器**：特性标记、返回类型、权限控制

### 使用工具辅助
- **Grep/搜索**：搜索 `Task.Result`、`Task.Wait()`、`method.Invoke`、硬编码字符串
- **ReadLints**：检查编辑过的文件的 linter 错误
- **规则文档**：参考 24 个规则文件的详细说明

---

## 相关资源

- 📖 [项目规范文档](../../rules/) - 24 个规范文件
- 📋 [审查报告模板](report-template.md) - 标准化报告格式
- 🔒 [安全规范](../../rules/security.mdc)
- 🗄️ [数据库规范](../../rules/database.mdc)
- 🚀 [启动框架规范](../../rules/startup-framework.mdc)
- 🌐 [多语言规范](../../rules/i18n.mdc)