---
name: db-migration
description: 指导同时为 MySQL 和 SQL Server 创建数据库迁移。使用数据库特定的 DbContext，处理数据库差异，配置雪花ID。当用户需要创建迁移、修改实体、或遇到迁移错误时使用。
---

# 多数据库迁移 Skill

## 快速开始

CodeSpirit 项目支持 MySQL 和 SQL Server 双数据库，**必须**为每个数据库创建独立的迁移文件。

### 基本命令

```powershell
# MySQL 迁移
dotnet ef migrations add MigrationName --context MySql{Service}DbContext --output-dir Data/Migrations/MySql

# SQL Server 迁移
dotnet ef migrations add MigrationName --context SqlServer{Service}DbContext --output-dir Data/Migrations/SqlServer
```

---

## DbContext 架构

### 三层继承结构

```
MultiDatabaseDbContextBase (多数据库基类)
    ↓
{Service}DbContext (服务基础上下文，运行时使用)
    ↓
MySql{Service}DbContext / SqlServer{Service}DbContext (数据库特定上下文，用于迁移)
```

### 关键要点

1. **运行时使用基础 DbContext**：`{Service}DbContext`（如 `ExamDbContext`）
2. **迁移使用特定 DbContext**：`MySql{Service}DbContext` 或 `SqlServer{Service}DbContext`
3. **数据库差异处理**：通过 `DatabaseSpecificConfigurations` 类统一处理

---

## 工作流程

### 步骤 1：修改实体或配置

修改实体类或实体配置类（`IEntityTypeConfiguration<T>`）后，准备创建迁移。

### 步骤 2：检查雪花 ID 配置

如果实体使用 `IIdGenerator` 生成 ID，**必须**在实体配置中添加：

```csharp
builder.Property(x => x.Id).ValueGeneratedNever();
```

**常见错误**：
```
Cannot insert explicit value for identity column in table 'Products' 
when IDENTITY_INSERT is set to OFF
```

**解决方案**：添加 `ValueGeneratedNever()` 配置。

### 步骤 3：为 MySQL 创建迁移

```powershell
# 切换到 API 项目目录
cd Src/ApiServices/CodeSpirit.{Service}Api

# 创建 MySQL 迁移
dotnet ef migrations add {MigrationName} `
  --context MySql{Service}DbContext `
  --output-dir Data/Migrations/MySql
```

**参数说明**：
- `--context`：**必须**使用 `MySql{Service}DbContext`
- `--output-dir`：指定迁移文件输出目录

### 步骤 4：为 SQL Server 创建迁移

```powershell
# 在同一项目目录下
dotnet ef migrations add {MigrationName} `
  --context SqlServer{Service}DbContext `
  --output-dir Data/Migrations/SqlServer
```

**重要**：迁移名称应保持一致，但文件会分别存储在 `MySql/` 和 `SqlServer/` 目录下。

### 步骤 5：验证迁移文件

检查迁移文件是否正确：

- [ ] MySQL 迁移文件在 `Data/Migrations/MySql/` 目录
- [ ] SQL Server 迁移文件在 `Data/Migrations/SqlServer/` 目录
- [ ] 迁移文件包含正确的数据库特定配置（如 MySQL 的 `datetime(6)`）
- [ ] 雪花 ID 字段配置了 `ValueGeneratedNever()`（如适用）

### 步骤 6：应用迁移到本地开发环境

```powershell
# 应用 MySQL 迁移
dotnet ef database update --context MySql{Service}DbContext

# 应用 SQL Server 迁移
dotnet ef database update --context SqlServer{Service}DbContext
```

---

## 迁移命名规范

| 场景 | 命名示例 |
|------|---------|
| 初始创建 | `InitialCreate` |
| 添加实体 | `Add{EntityName}` |
| 添加字段 | `Add{FieldName}To{EntityName}` |
| 修改字段 | `Update{FieldName}In{EntityName}` |
| 删除字段 | `Remove{FieldName}From{EntityName}` |
| 添加索引 | `AddIndexTo{EntityName}` |

---

## 数据库差异处理

### MySQL 特定配置

通过 `DatabaseSpecificConfigurations.ApplyMySqlConfigurations()` 自动处理：

- **DateTime**：映射为 `datetime(6)`（微秒精度）
- **默认值**：`GETUTCDATE()` → `CURRENT_TIMESTAMP(6)`
- **字符串**：`varchar(n) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci`
- **长文本**：超过 1000 字符使用 `text`
- **布尔**：`tinyint(1)`
- **Identity 字段**：限制最大长度为 256

### SQL Server 特定配置

通过 `DatabaseSpecificConfigurations.ApplySqlServerConfigurations()` 处理：

- 使用 EF Core 默认配置
- 仅处理 Identity 字段长度限制（256）

---

## 常见错误和解决方案

### 错误 1：使用了错误的 DbContext

**错误信息**：
```
No design-time factory found for 'ExamDbContext'
```

**原因**：使用了基础 DbContext 而非数据库特定的 DbContext。

**解决方案**：使用 `MySqlExamDbContext` 或 `SqlServerExamDbContext`。

### 错误 2：雪花 ID 配置缺失

**错误信息**：
```
Cannot insert explicit value for identity column
```

**解决方案**：在实体配置中添加 `ValueGeneratedNever()`：

```csharp
builder.Property(x => x.Id).ValueGeneratedNever();
```

### 错误 3：迁移文件位置错误

**问题**：迁移文件没有按数据库类型分离存储。

**解决方案**：使用 `--output-dir` 参数明确指定目录：

```powershell
--output-dir Data/Migrations/MySql
--output-dir Data/Migrations/SqlServer
```

---

## 实用脚本

### validate-migration.ps1

检查迁移是否使用了正确的 DbContext：

```powershell
# 使用方式
.\Scripts\validate-migration.ps1 -MigrationFile "Data/Migrations/MySql/20260123_AddProduct.cs"
```

### apply-migrations.ps1

一键应用 MySQL 和 SQL Server 迁移：

```powershell
# 使用方式
.\Scripts\apply-migrations.ps1 -ServiceName "ExamApi"
```

详见 [scripts/](scripts/) 目录。

---

## 检查清单

创建迁移前：

- [ ] 确认实体或配置已修改
- [ ] 检查雪花 ID 配置（如适用）
- [ ] 确认使用正确的 DbContext（`MySql{Service}DbContext` / `SqlServer{Service}DbContext`）
- [ ] 确认迁移名称符合规范
- [ ] 确认输出目录正确（`Data/Migrations/MySql` / `Data/Migrations/SqlServer`）

创建迁移后：

- [ ] 验证迁移文件位置正确
- [ ] 检查数据库特定配置是否正确应用
- [ ] 在本地开发环境测试迁移
- [ ] 提交迁移文件到版本控制

---

## 最佳实践

1. **始终使用数据库特定的 DbContext**：避免使用基础 DbContext 创建迁移
2. **迁移文件分离存储**：MySQL 和 SQL Server 迁移文件分别存储
3. **迁移名称保持一致**：同一功能的两套迁移使用相同名称
4. **先创建后应用**：先创建迁移文件，验证无误后再应用
5. **定期同步迁移**：确保 MySQL 和 SQL Server 迁移逻辑一致

---

## 示例

### 示例 1：添加新实体

```powershell
# 1. 创建 Product 实体和配置
# 2. 为 MySQL 创建迁移
dotnet ef migrations add AddProduct `
  --context MySqlMallDbContext `
  --output-dir Data/Migrations/MySql

# 3. 为 SQL Server 创建迁移
dotnet ef migrations add AddProduct `
  --context SqlServerMallDbContext `
  --output-dir Data/Migrations/SqlServer

# 4. 应用迁移
dotnet ef database update --context MySqlMallDbContext
dotnet ef database update --context SqlServerMallDbContext
```

### 示例 2：添加字段

```powershell
# 1. 在 Product 实体中添加 Price 字段
# 2. 创建迁移
dotnet ef migrations add AddPriceToProduct `
  --context MySqlMallDbContext `
  --output-dir Data/Migrations/MySql

dotnet ef migrations add AddPriceToProduct `
  --context SqlServerMallDbContext `
  --output-dir Data/Migrations/SqlServer
```

---

## 相关资源

- [数据库规范文档](../../rules/database.mdc)
- [迁移脚本](scripts/)
- [迁移示例](examples.md)
