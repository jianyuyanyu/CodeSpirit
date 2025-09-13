# CodeSpirit 多数据库迁移管理指南

## 概述

CodeSpirit项目采用多数据库架构，支持MySQL和SQL Server两种数据库。每个API项目和组件都有独立的数据库特定DbContext用于迁移管理。

## 项目架构

### 支持的项目

**API服务项目：**
- **ExamApi** - 考试系统API
- **ConfigCenter** - 配置中心API
- **FileStorageApi** - 文件存储API
- **SurveyApi** - 问卷系统API
- **IdentityApi** - 身份认证API（使用独立脚本管理）

**组件项目：**
- **Settings** - 设置管理组件
- **Messaging** - 消息系统组件

### DbContext层次结构

每个项目都遵循以下DbContext结构：

```
BaseDbContext (基础上下文，包含业务逻辑)
├── MySqlDbContext (MySQL特定配置)
└── SqlServerDbContext (SQL Server特定配置)
```

**示例 - ExamApi项目：**
```
ExamDbContext (基础)
├── MySqlExamDbContext (MySQL特定)
└── SqlServerExamDbContext (SQL Server特定)
```

## 迁移管理

### 主要工具：manage-multi-database-migrations.ps1

这是管理所有项目多数据库迁移的统一工具。

**语法：**
```powershell
.\Scripts\manage-multi-database-migrations.ps1 -ApiProject <ProjectName> -DatabaseType <DatabaseType> -Action <Action> [-MigrationName <Name>]
```

**参数说明：**
- `ApiProject`: 项目名称 (ExamApi, ConfigCenter, FileStorageApi, SurveyApi, Settings, Messaging)
- `DatabaseType`: 数据库类型 (MySql, SqlServer)  
- `Action`: 操作类型 (Add, Remove, List, Update)
- `MigrationName`: 迁移名称（Add操作时必需）

### 常用操作示例

#### 1. 添加新迁移

```powershell
# 为考试系统添加MySQL迁移
.\Scripts\manage-multi-database-migrations.ps1 -ApiProject ExamApi -DatabaseType MySql -Action Add -MigrationName "AddQuestionBank"

# 为问卷系统添加SQL Server迁移
.\Scripts\manage-multi-database-migrations.ps1 -ApiProject SurveyApi -DatabaseType SqlServer -Action Add -MigrationName "AddQuestionBank"

# 为设置组件添加迁移
.\Scripts\manage-multi-database-migrations.ps1 -ApiProject Settings -DatabaseType MySql -Action Add -MigrationName "AddUserPreferences"

# 为消息组件添加迁移
.\Scripts\manage-multi-database-migrations.ps1 -ApiProject Messaging -DatabaseType SqlServer -Action Add -MigrationName "AddChatRooms"
```

#### 2. 查看迁移历史

```powershell
# 查看考试系统MySQL迁移
.\Scripts\manage-multi-database-migrations.ps1 -ApiProject ExamApi -DatabaseType MySql -Action List

# 查看配置中心SQL Server迁移
.\Scripts\manage-multi-database-migrations.ps1 -ApiProject ConfigCenter -DatabaseType SqlServer -Action List
```

#### 3. 应用迁移

```powershell
# 更新文件存储系统MySQL数据库
.\Scripts\manage-multi-database-migrations.ps1 -ApiProject FileStorageApi -DatabaseType MySql -Action Update

# 更新问卷系统SQL Server数据库
.\Scripts\manage-multi-database-migrations.ps1 -ApiProject SurveyApi -DatabaseType SqlServer -Action Update
```

#### 4. 删除迁移

```powershell
# 删除最后一个迁移
.\Scripts\manage-multi-database-migrations.ps1 -ApiProject ExamApi -DatabaseType MySql -Action Remove
```

## 迁移目录结构

每个项目的迁移文件按数据库类型分离存储：

```
Src/ApiServices/CodeSpirit.ExamApi/
├── Migrations/
│   ├── MySql/                          # MySQL迁移文件
│   │   ├── 20250912_InitialCreate.cs
│   │   ├── 20250912_InitialCreate.Designer.cs
│   │   ├── 20250915_AddQuestionBank.cs
│   │   └── MySqlExamDbContextModelSnapshot.cs
│   └── SqlServer/                      # SQL Server迁移文件
│       ├── 20250912_InitialCreate.cs
│       ├── 20250912_InitialCreate.Designer.cs
│       ├── 20250915_AddQuestionBank.cs
│       └── SqlServerExamDbContextModelSnapshot.cs

Src/Components/CodeSpirit.Settings/
├── Migrations/
│   ├── MySql/                          # MySQL迁移文件
│   │   ├── 20250912_InitialCreate.cs
│   │   └── MySqlSettingsDbContextModelSnapshot.cs
│   └── SqlServer/                      # SQL Server迁移文件
│       ├── 20250912_InitialCreate.cs
│       └── SqlServerSettingsDbContextModelSnapshot.cs
```

## 开发工作流

### 1. 新功能开发流程

当添加新功能需要数据库变更时：

```powershell
# 步骤1: 修改实体类和DbContext配置

# 步骤2: 为两种数据库都创建迁移
.\Scripts\manage-multi-database-migrations.ps1 -ApiProject ExamApi -DatabaseType MySql -Action Add -MigrationName "AddNewFeature"
.\Scripts\manage-multi-database-migrations.ps1 -ApiProject ExamApi -DatabaseType SqlServer -Action Add -MigrationName "AddNewFeature"

# 步骤3: 检查生成的迁移文件
# 查看MySQL迁移
.\Scripts\manage-multi-database-migrations.ps1 -ApiProject ExamApi -DatabaseType MySql -Action List

# 查看SQL Server迁移  
.\Scripts\manage-multi-database-migrations.ps1 -ApiProject ExamApi -DatabaseType SqlServer -Action List

# 步骤4: 测试迁移（可选，通常应用启动时自动处理）
.\Scripts\manage-multi-database-migrations.ps1 -ApiProject ExamApi -DatabaseType MySql -Action Update
```

### 2. 跨项目功能开发

当功能涉及多个项目时：

```powershell
# 为考试系统添加迁移
.\Scripts\manage-multi-database-migrations.ps1 -ApiProject ExamApi -DatabaseType MySql -Action Add -MigrationName "AddUserAnalytics"
.\Scripts\manage-multi-database-migrations.ps1 -ApiProject ExamApi -DatabaseType SqlServer -Action Add -MigrationName "AddUserAnalytics"

# 为问卷系统添加相关迁移
.\Scripts\manage-multi-database-migrations.ps1 -ApiProject SurveyApi -DatabaseType MySql -Action Add -MigrationName "AddUserAnalytics"
.\Scripts\manage-multi-database-migrations.ps1 -ApiProject SurveyApi -DatabaseType SqlServer -Action Add -MigrationName "AddUserAnalytics"

# 为设置组件添加配置支持
.\Scripts\manage-multi-database-migrations.ps1 -ApiProject Settings -DatabaseType MySql -Action Add -MigrationName "AddAnalyticsSettings"
.\Scripts\manage-multi-database-migrations.ps1 -ApiProject Settings -DatabaseType SqlServer -Action Add -MigrationName "AddAnalyticsSettings"
```

## 数据库配置

### 连接字符串配置

在各项目的配置文件中设置连接字符串：

**appsettings.Development.json示例：**
```json
{
  "ConnectionStrings": {
    "exam-api": "Server=localhost;Port=3306;Database=exam-api;Uid=root;Pwd=password;CharSet=utf8mb4;",
    "config-center": "Server=localhost;Port=3306;Database=config-center;Uid=root;Pwd=password;CharSet=utf8mb4;",
    "file-storage-api": "Server=localhost;Port=3306;Database=file-storage-api;Uid=root;Pwd=password;CharSet=utf8mb4;",
    "survey-api": "Server=localhost;Port=3306;Database=survey-api;Uid=root;Pwd=password;CharSet=utf8mb4;",
    "settings": "Server=localhost;Port=3306;Database=settings;Uid=root;Pwd=password;CharSet=utf8mb4;",
    "messaging": "Server=localhost;Port=3306;Database=messaging;Uid=root;Pwd=password;CharSet=utf8mb4;"
  }
}
```

### 数据库特定配置

每个DbContext都有数据库特定的配置：

**MySQL配置特点：**
- 字符串长度优化
- 索引长度限制处理
- 时间戳默认值使用 `CURRENT_TIMESTAMP`

**SQL Server配置特点：**
- 完整的字符串长度支持
- 丰富的索引选项
- 时间戳默认值使用 `GETUTCDATE()`

## 自动迁移机制

### 应用启动时的自动处理

1. **检测数据库类型**: 根据配置确定使用的数据库类型
2. **选择对应DbContext**: 加载MySQL或SQL Server特定的DbContext
3. **应用待处理迁移**: 自动执行所有未应用的迁移
4. **数据种子初始化**: 执行必要的初始数据创建

### 示例启动流程

```csharp
// 在各个API的Configuration类中
public override async Task InitializeDatabaseAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<ExamApiConfiguration>>();
    var configuration = services.GetRequiredService<IConfiguration>();
    
    try
    {
        // 应用数据库迁移
        await DatabaseMigrationHelper.ApplyDatabaseMigrationsAsync<MySqlExamDbContext, SqlServerExamDbContext>(
            services, configuration, logger, "ExamApi");
        
        // 初始化数据
        var context = services.GetRequiredService<ExamDbContext>();
        await context.InitializeDatabaseAsync();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "初始化数据库时发生错误：{Message}", ex.Message);
        throw;
    }
}
```

## 故障排除

### 常见问题和解决方案

#### 1. 迁移创建失败

**问题**: `Unable to create a 'DbContext' of type 'MySqlXxxDbContext'`

**解决方案**:
- 检查项目路径是否正确
- 确认DbContextFactory是否存在
- 验证配置文件格式正确性

#### 2. 连接字符串错误

**问题**: `The string argument 'connectionString' cannot be empty`

**解决方案**:
- 检查appsettings.json中的连接字符串配置
- 确认DefaultConnection不是空字符串
- 验证数据库服务是否运行

#### 3. 权限问题

**问题**: 无法创建数据库或表

**解决方案**:
- 确认数据库用户权限充足
- 检查数据库服务状态
- 验证网络连接

#### 4. 迁移冲突

**问题**: 迁移文件冲突或重复

**解决方案**:
```powershell
# 删除有问题的迁移
.\Scripts\manage-multi-database-migrations.ps1 -ApiProject ExamApi -DatabaseType MySql -Action Remove

# 重新创建迁移
.\Scripts\manage-multi-database-migrations.ps1 -ApiProject ExamApi -DatabaseType MySql -Action Add -MigrationName "NewMigrationName"
```

## 性能和监控

### 迁移性能优化

1. **批量操作**: 将相关变更合并到单个迁移中
2. **索引策略**: 合理设计数据库索引
3. **数据类型选择**: 选择合适的数据类型和长度

### 监控建议

1. **日志记录**: 密切关注迁移执行日志
2. **性能监控**: 监控迁移执行时间
3. **错误追踪**: 建立迁移失败的报警机制

## 最佳实践

### 开发阶段

1. **同步开发**: 同时为MySQL和SQL Server创建迁移
2. **命名规范**: 使用描述性的迁移名称
3. **增量开发**: 频繁创建小的迁移而不是大的变更
4. **代码审查**: 迁移文件也需要代码审查

### 部署阶段

1. **备份策略**: 部署前备份生产数据库
2. **分步部署**: 先部署迁移，再部署应用代码
3. **回滚计划**: 准备迁移回滚方案
4. **监控验证**: 部署后验证数据完整性

### 维护阶段

1. **清理策略**: 定期清理过时的迁移文件
2. **文档更新**: 保持迁移文档的及时更新
3. **性能调优**: 根据使用情况调整数据库配置
4. **安全审计**: 定期审计数据库权限和访问

---

通过这套多数据库迁移管理系统，CodeSpirit项目能够灵活支持不同的数据库环境，确保在各种部署场景下的兼容性和可维护性。