# CodeSpirit 规范映射到 BMAD 工作流

本文档详细说明 BMAD 各阶段需要遵循的 CodeSpirit 具体规范。

## BMAD 阶段与 CodeSpirit 规范映射

### 1. Analysis 阶段（需求分析）

**BMAD 命令**: `/product-brief`, `/create-prd`

**需要遵循的规范**:

| 规范文档 | 关键要求 |
|---------|---------|
| `.cursor/rules/ai-development.mdc` | AI 功能需求分析、LLM 集成、AI 表单填充 |
| `.cursor/rules/security.mdc` | 权限设计、审计追踪需求 |
| `.cursor/rules/i18n.mdc` | 多语言需求识别 |

**CodeSpirit 特定考量**:

- ✅ **多租户**: 功能是否需要租户隔离？实体是否实现 `IMultiTenant`？
- ✅ **多数据库**: 是否需要同时支持 MySQL 和 SQL Server？
- ✅ **AI 功能**: 是否需要集成 LLM、AI 表单填充（`[AiFormFill]`）或 AI 长任务？
- ✅ **国际化**: 需要哪些多语言资源键？
- ✅ **权限控制**: 需要哪些 `[RequirePermission]` 特性？

**PRD 模板扩展**:

```markdown
## CodeSpirit 特定需求

### 多租户支持
- [ ] 是否需要租户隔离？
- [ ] 实体是否实现 IMultiTenant？

### 数据库支持
- [ ] 需要支持 MySQL
- [ ] 需要支持 SQL Server
- [ ] 是否需要数据库特定的配置？

### AI 功能
- [ ] 是否需要 LLM 集成？
- [ ] 是否需要 AI 表单填充？
- [ ] 是否需要 AI 长任务处理？

### 国际化
- [ ] 需要支持的语言：中文、英文
- [ ] 需要添加的资源键列表

### 权限设计
- [ ] 需要的权限点列表
- [ ] 权限控制级别（控制器/操作）
```

---

### 2. Planning 阶段（规划）

**BMAD 命令**: `/create-epics-and-stories`

**需要遵循的规范**:

| 规范文档 | 关键要求 |
|---------|---------|
| `.cursor/rules/api-design.mdc` | RESTful 标准、路由规范、响应格式 |
| `.cursor/rules/dto.mdc` | DTO 设计、验证特性、映射配置 |
| `.cursor/rules/naming-conventions.mdc` | 命名约定 |

**Story 模板扩展**:

```markdown
## 实现清单

### 实体（Entity）
- [ ] 创建 `{EntityName}.cs`
- [ ] 实现 `IFullAuditable`（审计字段）
- [ ] 实现 `IMultiTenant`（多租户）
- [ ] 实现 `IIsActive`（激活状态）
- [ ] 添加验证特性

### DTO 类
- [ ] 创建 `{EntityName}Dto.cs`（展示 DTO）
- [ ] 创建 `Create{EntityName}Dto.cs`
- [ ] 创建 `Update{EntityName}Dto.cs`
- [ ] 创建 `{EntityName}QueryDto.cs`（继承 QueryDtoBase）
- [ ] 所有属性添加 `[Display]` 特性（多语言）
- [ ] Create/Update DTO 添加验证特性

### AutoMapper 映射
- [ ] 创建 `{EntityName}Profile.cs`
- [ ] 配置 Entity ↔ DTO 映射

### 服务层
- [ ] 创建 `I{EntityName}Service` 接口（继承 IScopedDependency）
- [ ] 创建 `{EntityName}Service` 实现
- [ ] 实现 CRUD 方法
- [ ] 添加 XML 文档注释

### 控制器
- [ ] 创建 `{EntityName}sController`（复数形式）
- [ ] 继承 `ApiControllerBase`
- [ ] 路由：`/{service}/api/[controller]`
- [ ] 添加 `[DisplayName]` 特性
- [ ] 实现 RESTful 端点
- [ ] 使用 `SuccessResponse()` 返回统一格式

### 数据库配置
- [ ] 创建 `{EntityName}Configuration.cs`
- [ ] 配置主键和 `ValueGeneratedNever()`（雪花 ID）
- [ ] 配置租户字段
- [ ] 配置索引
- [ ] 配置关系

### 数据库迁移
- [ ] 创建 SQL Server 迁移：`dotnet ef migrations add Add{EntityName} --context SqlServer{Service}DbContext --output-dir Data/Migrations/SqlServer`
- [ ] 创建 MySQL 迁移：`dotnet ef migrations add Add{EntityName} --context MySql{Service}DbContext --output-dir Data/Migrations/MySql`

### 多语言资源
- [ ] 添加 DisplayResources 资源键
- [ ] 添加 ValidationResources 资源键（如需要）
- [ ] 添加 NavigationResources 资源键（如需要）

### 权限配置
- [ ] 添加权限点定义
- [ ] 控制器/操作添加 `[RequirePermission]` 特性
```

---

### 3. Solutioning 阶段（方案设计）

**BMAD 命令**: `/create-architecture`

**需要遵循的规范**:

| 规范文档 | 关键要求 |
|---------|---------|
| `.cursor/rules/database.mdc` | DbContext 设计、迁移策略 |
| `.cursor/rules/dependency-injection.mdc` | 依赖注入策略、生命周期选择 |
| `.cursor/rules/performance.mdc` | 缓存策略、查询优化 |

**Architecture 模板扩展**:

```markdown
## 数据库设计

### 数据库选型
- **MySQL**: 是/否
- **SQL Server**: 是/否
- **选型理由**: ...

### DbContext 设计
- **基础 DbContext**: `{Service}DbContext`（继承 `MultiDatabaseDbContextBase`）
- **SQL Server DbContext**: `SqlServer{Service}DbContext`
- **MySQL DbContext**: `MySql{Service}DbContext`
- **设计时工厂**: `SqlServer{Service}DbContextFactory`, `MySql{Service}DbContextFactory`

### 实体配置
- **配置位置**: `Data/Configurations/{EntityName}Configuration.cs`
- **雪花 ID**: 使用 `IIdGenerator` 的实体必须配置 `ValueGeneratedNever()`
- **多租户**: 实体实现 `IMultiTenant`，自动应用租户过滤器

### 迁移策略
- **SQL Server 迁移目录**: `Data/Migrations/SqlServer/`
- **MySQL 迁移目录**: `Data/Migrations/MySql/`
- **迁移命令**: 必须使用数据库特定的 DbContext

## 依赖注入设计

### 服务生命周期
- **IScopedDependency**: 业务服务、数据库操作（推荐）
- **ITransientDependency**: 无状态工具类
- **ISingletonDependency**: 配置服务、缓存、ID生成器

### 自动注册
- 接口继承标记接口（如 `IUserService : IScopedDependency`）
- Scrutor 自动扫描注册，无需手动注册

## 缓存策略

### L1 缓存（内存）
- **使用场景**: 频繁访问的配置数据
- **实现**: `IMemoryCache`

### L2 缓存（Redis）
- **使用场景**: 跨服务共享数据
- **实现**: `IDistributedCache`

## API 设计

### RESTful 标准
- **路由**: `/{service}/api/{controller}`（复数形式）
- **HTTP 方法**: GET（查询）、POST（创建）、PUT（更新）、DELETE（删除）
- **响应格式**: `ApiResponse<T>`

### 统一响应
- **成功**: `SuccessResponse(data)` - 返回 200
- **创建成功**: `SuccessResponseWithCreate<T>()` - 返回 201
- **失败**: `BadResponse(message)` - 返回 400

## 审计追踪

### GreptimeDB 配置
- **审计实体**: 实现 `IFullAuditable`
- **审计特性**: `[Audit]`（启用）、`[NoAudit]`（禁用）
- **审计存储**: GreptimeDB
```

---

### 4. Implementation 阶段（实现）

**BMAD 命令**: `/dev-story`, `/quick-dev`

**需要遵循的规范**:

| 规范文档 | 关键要求 |
|---------|---------|
| `.cursor/rules/cs.mdc` | XML 注释、UTC 时间、Newtonsoft.Json |
| `.cursor/rules/controller.mdc` | 控制器规范、操作特性 |
| `.cursor/rules/service.mdc` | 服务类规范、接口定义 |
| `.cursor/rules/dto.mdc` | DTO 验证、映射配置 |

**实现检查清单**:

```markdown
## 代码实现检查清单

### C# 通用规范
- [ ] 所有公共成员添加 XML 文档注释（`<summary>`, `<param>`, `<returns>`, `<exception>`）
- [ ] 时间使用 UTC 格式（`DateTime.UtcNow`）
- [ ] 序列化使用 `Newtonsoft.Json`（`JsonConvert.SerializeObject`）
- [ ] 复杂业务逻辑添加行内注释

### 命名约定
- [ ] 实体：`User`（单数）
- [ ] DTO：`CreateUserDto`, `UpdateUserDto`, `UserQueryDto`, `UserDto`
- [ ] 服务：`UserService`（实现 `IUserService`）
- [ ] 控制器：`UsersController`（复数）

### DTO 规范
- [ ] 所有属性添加 `[Display]` 特性（多语言）
- [ ] Create/Update DTO 添加验证特性（`[Required]`, `[StringLength]` 等）
- [ ] 验证特性使用多语言资源（`ErrorMessageResourceType`）
- [ ] QueryDto 继承 `QueryDtoBase`

### 控制器规范
- [ ] 继承 `ApiControllerBase`
- [ ] 路由：`/{service}/api/[controller]`
- [ ] 添加 `[DisplayName]` 特性
- [ ] 使用 `SuccessResponse()` 返回统一格式
- [ ] 操作特性：`[Operation]`, `[HeaderOperation]`, `[RowOperation]`

### 服务类规范
- [ ] 接口继承标记接口（`IScopedDependency` 等）
- [ ] 实现类实现业务接口
- [ ] 所有公共方法添加 XML 文档注释
- [ ] 异步方法使用 `async/await`，禁止 `Task.Result`

### 数据库规范
- [ ] 实体配置：`{EntityName}Configuration.cs`
- [ ] 雪花 ID：配置 `ValueGeneratedNever()`
- [ ] 多租户：实体实现 `IMultiTenant`
- [ ] 迁移：使用数据库特定的 DbContext

### 多语言支持
- [ ] DisplayResources 添加资源键
- [ ] ValidationResources 添加资源键（如需要）
- [ ] NavigationResources 添加资源键（如需要）

### 权限控制
- [ ] 权限点定义
- [ ] 控制器/操作添加 `[RequirePermission]` 特性
```

---

### 5. Review 阶段（审查）

**BMAD 命令**: `/code-review`

**需要遵循的规范**:

| 规范文档 | 关键要求 |
|---------|---------|
| `.cursor/skills/code-review/SKILL.md` | CodeSpirit 特定审查清单 |
| `.cursor/skills/pre-commit-validation/SKILL.md` | 提交前综合验证 |

**审查流程**:

1. **BMAD 标准审查**
   - 执行 BMAD `/code-review` 工作流
   - 检查代码质量、测试覆盖率等

2. **CodeSpirit 特定审查**
   - 执行 `code-review` 技能
   - 检查是否符合所有项目规范
   - 检查安全、数据库、异步编程、多语言等

3. **提交前验证**
   - 执行 `pre-commit-validation` 技能
   - 静态代码审查
   - 运行时错误检查
   - 功能验证

**审查检查清单**:

```markdown
## CodeSpirit 审查检查清单

### 安全审查
- [ ] 权限控制正确配置
- [ ] 敏感数据保护（密码、密钥等）
- [ ] SQL 注入防护（使用参数化查询）
- [ ] XSS 防护（输入验证）

### 数据库审查
- [ ] 迁移使用数据库特定的 DbContext
- [ ] 同时支持 MySQL 和 SQL Server
- [ ] 雪花 ID 配置 `ValueGeneratedNever()`
- [ ] 多租户过滤器正确应用

### 异步编程审查
- [ ] 所有 I/O 操作使用 `async/await`
- [ ] 禁止 `Task.Result` 和 `Task.Wait()`
- [ ] 异步方法命名以 `Async` 结尾

### 多语言审查
- [ ] 所有面向用户的文本使用资源文件
- [ ] DTO 验证特性使用多语言资源
- [ ] 错误消息支持多语言

### DTO 审查
- [ ] 所有属性添加 `[Display]` 特性
- [ ] Create/Update DTO 添加验证特性
- [ ] AutoMapper 映射正确配置

### 控制器审查
- [ ] RESTful 标准正确应用
- [ ] 统一响应格式（`ApiResponse<T>`）
- [ ] 操作特性正确配置

### 服务类审查
- [ ] 接口继承标记接口
- [ ] XML 文档注释完整
- [ ] 依赖注入正确使用

### 代码质量审查
- [ ] 一个 .cs 文件一个顶级类型
- [ ] XML 文档注释完整
- [ ] 复杂逻辑添加注释
```

---

## 规范引用路径

所有规范文档位于 `.cursor/rules/` 目录：

- `cs.mdc` - C# 通用规范
- `naming-conventions.mdc` - 命名约定
- `dto.mdc` - DTO 规范
- `controller.mdc` - 控制器规范
- `service.mdc` - 服务类规范
- `enum.mdc` - 枚举规范
- `api-design.mdc` - API 设计
- `dependency-injection.mdc` - 依赖注入
- `startup-framework.mdc` - 启动框架
- `database.mdc` - 数据库迁移
- `i18n.mdc` - 多语言
- `ai-development.mdc` - AI 开发
- `performance.mdc` - 性能优化
- `security.mdc` - 安全规范
- `testing.mdc` - 测试规范

## 项目上下文文档

BMAD 会自动加载项目上下文：`project-context.md`

该文档包含：
- 项目概述和技术栈
- 核心架构模式
- 所有开发规范引用
- 关键约束和代码质量要求
