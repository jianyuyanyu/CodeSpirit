# CodeSpirit 数据库管理脚本

本目录包含用于管理CodeSpirit项目多数据库架构的PowerShell脚本，支持MySQL和SQL Server的迁移管理。

## 脚本说明

### manage-multi-database-migrations.ps1 ⭐ 推荐使用
**多数据库迁移管理脚本**，支持所有API项目和组件的MySQL/SQL Server迁移管理。

**支持的项目：**
- `ExamApi` - 考试系统
- `ConfigCenter` - 配置中心  
- `FileStorageApi` - 文件存储系统
- `SurveyApi` - 问卷系统
- `Settings` - 设置管理组件
- `Messaging` - 消息系统组件
- `IdentityApi` - 身份认证系统（通过现有脚本支持）

**参数：**
- `ApiProject`: API项目名称
- `DatabaseType`: 数据库类型 (MySql/SqlServer)
- `Action`: 操作类型 (Add/Remove/List/Update)
- `MigrationName`: 迁移名称（Add操作时必需）

**使用示例：**
```powershell
# 为考试系统MySQL添加迁移
.\Scripts\manage-multi-database-migrations.ps1 -ApiProject ExamApi -DatabaseType MySql -Action Add -MigrationName "AddNewFeature"

# 为问卷系统SQL Server添加迁移
.\Scripts\manage-multi-database-migrations.ps1 -ApiProject SurveyApi -DatabaseType SqlServer -Action Add -MigrationName "AddNewFeature"

# 更新MySQL数据库
.\Scripts\manage-multi-database-migrations.ps1 -ApiProject ExamApi -DatabaseType MySql -Action Update

# 列出迁移历史
.\Scripts\manage-multi-database-migrations.ps1 -ApiProject ConfigCenter -DatabaseType MySql -Action List

# 删除最后一个迁移
.\Scripts\manage-multi-database-migrations.ps1 -ApiProject FileStorageApi -DatabaseType SqlServer -Action Remove
```

## 多数据库架构概述

### DbContext层次结构

每个API项目都有三个DbContext：

```
BaseDbContext (基类，包含业务逻辑)
├── MySqlDbContext (MySQL特定配置和迁移)
└── SqlServerDbContext (SQL Server特定配置和迁移)
```

### 迁移文件组织结构

```
Src/ApiServices/CodeSpirit.ExamApi/
├── Migrations/
│   ├── MySql/           # MySQL专用迁移
│   │   ├── 20250912_InitialCreate.cs
│   │   └── ...
│   └── SqlServer/       # SQL Server专用迁移
│       ├── 20250912_InitialCreate.cs
│       └── ...
```

## 使用流程

### 🚀 快速开始

1. **为新项目创建初始迁移**
```powershell
# 为所有数据库类型创建初始迁移
.\Scripts\manage-multi-database-migrations.ps1 -ApiProject ExamApi -DatabaseType MySql -Action Add -MigrationName "InitialCreate"
.\Scripts\manage-multi-database-migrations.ps1 -ApiProject ExamApi -DatabaseType SqlServer -Action Add -MigrationName "InitialCreate"
```

2. **启动应用**
```powershell
cd Src\CodeSpirit.AppHost
dotnet run
```

### 🔄 开发过程中的迁移管理

```powershell
# 1. 修改实体后，为两种数据库都添加迁移
.\Scripts\manage-multi-database-migrations.ps1 -ApiProject ExamApi -DatabaseType MySql -Action Add -MigrationName "AddUserProfile"
.\Scripts\manage-multi-database-migrations.ps1 -ApiProject ExamApi -DatabaseType SqlServer -Action Add -MigrationName "AddUserProfile"

# 2. 查看迁移状态
.\Scripts\manage-multi-database-migrations.ps1 -ApiProject ExamApi -DatabaseType MySql -Action List

# 3. 应用迁移（通常由应用自动处理）
.\Scripts\manage-multi-database-migrations.ps1 -ApiProject ExamApi -DatabaseType MySql -Action Update
```

### 📝 组件迁移管理

```powershell
# Settings组件迁移
.\Scripts\manage-multi-database-migrations.ps1 -ApiProject Settings -DatabaseType MySql -Action Add -MigrationName "AddNewSetting"

# Messaging组件迁移
.\Scripts\manage-multi-database-migrations.ps1 -ApiProject Messaging -DatabaseType SqlServer -Action Add -MigrationName "AddChatFeature"
```

## 数据库配置

### 连接字符串配置

在各个API项目的 `appsettings.Development.json` 中配置：

```json
{
  "ConnectionStrings": {
    "exam-api": "Server=localhost;Port=3306;Database=exam-api;Uid=root;Pwd=password;",
    "config-center": "Server=localhost;Port=3306;Database=config-center;Uid=root;Pwd=password;",
    "file-storage-api": "Server=localhost;Port=3306;Database=file-storage-api;Uid=root;Pwd=password;",
    "survey-api": "Server=localhost;Port=3306;Database=survey-api;Uid=root;Pwd=password;"
  }
}
```

### 数据库类型切换

通过环境变量或配置文件设置：

```json
{
  "DatabaseType": "MySql"  // 或 "SqlServer"
}
```

## 自动迁移

应用启动时会自动处理数据库迁移：

1. 根据配置的数据库类型选择相应的DbContext
2. 自动应用该数据库类型的所有待处理迁移
3. 执行数据种子初始化

## 环境要求

- **MySQL**: 8.0+ （开发环境可通过Aspire自动管理）
- **SQL Server**: LocalDB 或完整SQL Server实例
- **PowerShell**: Windows PowerShell 5.1+ 或 PowerShell Core 7+
- **.NET**: .NET 9.0 SDK
- **EF Core Tools**: `dotnet tool install --global dotnet-ef`

## 故障排除

### 1. 迁移生成失败
- 检查项目是否存在于指定路径
- 确认数据库连接字符串配置正确
- 验证EF Core工具是否已安装

### 2. DbContext创建失败
- 检查DbContextFactory是否正确配置
- 确认配置文件存在且格式正确
- 验证依赖注入配置

### 3. 权限问题
- 确保数据库用户有足够权限创建/修改数据库
- 检查文件系统权限

## 最佳实践

1. 🔄 **同步迁移**: 为每个功能同时创建MySQL和SQL Server迁移
2. 📁 **命名规范**: 使用描述性的迁移名称，如 `AddUserProfile`、`UpdateOrderSchema`
3. 🧪 **测试验证**: 在两种数据库上都进行测试
4. 📝 **版本控制**: 将所有迁移文件纳入版本控制
5. 🔒 **备份策略**: 在生产环境应用迁移前进行数据库备份
6. 📊 **监控日志**: 密切关注迁移执行日志，确保成功应用

## 迁移策略

### 开发环境
- 使用脚本管理迁移
- 可以频繁重置和重建数据库
- 支持快速原型开发

### 生产环境
- 应用自动处理迁移应用
- 严格的备份和回滚策略
- 渐进式部署验证

---

**注意**: 旧的单数据库管理脚本已被移除，请使用 `manage-multi-database-migrations.ps1` 进行所有迁移操作。