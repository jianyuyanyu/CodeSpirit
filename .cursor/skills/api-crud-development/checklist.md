# API CRUD 开发检查清单

## 步骤 1：实体（Entity）

- [ ] 实现 `IFullAuditable` 接口（审计字段）
- [ ] 实现 `IMultiTenant` 接口（多租户）
- [ ] 实现 `IIsActive` 接口（激活状态）
- [ ] 使用 `long` 作为主键类型
- [ ] 添加验证特性（`[Required]`、`[MaxLength]`）
- [ ] 添加 XML 文档注释

## 步骤 2：DTO 类

### 展示 DTO（{EntityName}Dto）
- [ ] 添加 `[DisplayName]` 特性
- [ ] 添加列特性（`TplColumn`、`AvatarColumn`、`DateColumn` 等）
- [ ] 包含必要的关联数据字段

### 创建 DTO（Create{EntityName}Dto）
- [ ] 添加验证特性（`[Required]`、`[MaxLength]`）
- [ ] 添加 `[DisplayName]` 特性
- [ ] 添加表单字段特性（`AmisInputTextField`、`AmisSelectField` 等）
- [ ] 使用 `[FormGroup]` 分组（如需要）
- [ ] 添加 AI 表单填充特性（可选）

### 更新 DTO（Update{EntityName}Dto）
- [ ] 复用或继承 Create DTO 的字段
- [ ] 添加验证特性

### 查询 DTO（{EntityName}QueryDto）
- [ ] 继承 `QueryDtoBase`
- [ ] 添加查询字段
- [ ] 使用 `[PageAside]` 标记侧边栏字段（如需要）

## 步骤 3：AutoMapper 映射

- [ ] 创建 `{EntityName}Profile` 类
- [ ] 配置 Entity → DTO 映射
- [ ] 配置 CreateDTO → Entity 映射
- [ ] 配置 UpdateDTO → Entity 映射
- [ ] 配置 `PageList<Entity>` → `PageList<DTO>` 映射
- [ ] 处理关联数据映射（如 `DepartmentName`）

## 步骤 4：服务接口和实现

### 服务接口
- [ ] 继承 `IBaseCRUDIService<...>`
- [ ] 继承 `IScopedDependency`（或 `ITransientDependency`、`ISingletonDependency`）
- [ ] 定义自定义方法（如 `Get{EntityName}sAsync`）

### 服务实现
- [ ] 继承 `BaseCRUDIService<...>`
- [ ] 实现服务接口
- [ ] 注入必要的依赖（`IRepository`、`IMapper`、`IIdGenerator`、`ICurrentUser`）
- [ ] 重写 `CreateAsync`（设置 ID 和 TenantId）
- [ ] 实现自定义查询方法（使用 `PredicateBuilder`、`Include`、`AsNoTracking`）
- [ ] 重写验证方法（`ValidateCreateDto`、`ValidateUpdateDto`）
- [ ] 重写生命周期方法（`OnCreating`、`OnUpdating`、`OnDeleting`）

## 步骤 5：控制器

- [ ] 继承 `ApiControllerBase`
- [ ] 添加 `[DisplayName]` 特性
- [ ] 添加 `[Navigation]` 特性（配置图标和平台类型）
- [ ] 实现标准 CRUD 方法：
  - [ ] `Get{EntityName}s`（列表查询）
  - [ ] `Detail`（详情查询）
  - [ ] `Create{EntityName}`（创建）
  - [ ] `Update{EntityName}`（更新）
  - [ ] `Delete{EntityName}`（删除）
  - [ ] `BatchDelete`（批量删除）
- [ ] 所有方法返回 `ActionResult<ApiResponse<T>>`
- [ ] 添加 `[Operation]` 特性（操作按钮）
- [ ] 添加 `[HeaderOperation]` 特性（头部操作，如需要）
- [ ] 添加权限控制（`[Permission]` 或 `[RequirePermission]`）

## 步骤 6：数据库上下文

- [ ] 在 `{Service}DbContext` 中添加 `DbSet<{EntityName}>`
- [ ] 创建实体配置类（`IEntityTypeConfiguration<{EntityName}>`）
- [ ] 配置表名、主键
- [ ] 配置雪花 ID（`ValueGeneratedNever()`）
- [ ] 配置字段类型和长度
- [ ] 配置索引（唯一索引、复合索引）
- [ ] 配置关联关系（`HasOne`、`HasMany`、`OnDelete`）

## 步骤 7：数据库迁移

- [ ] 使用 `MySql{Service}DbContext` 创建 MySQL 迁移
- [ ] 使用 `SqlServer{Service}DbContext` 创建 SQL Server 迁移
- [ ] 验证迁移文件位置正确
- [ ] 在本地开发环境测试迁移

## 最终检查

- [ ] 所有公共成员添加 XML 文档注释
- [ ] 代码符合命名规范（实体单数、控制器复数）
- [ ] 多语言支持（使用资源文件，避免硬编码）
- [ ] 异常处理（使用 `BusinessException`）
- [ ] 日志记录（关键操作记录日志）
- [ ] 单元测试（核心业务逻辑编写测试）
