# 多数据库DbContext架构使用指南

## 概述

CodeSpirit项目采用多数据库架构设计，支持MySQL和SQL Server两种数据库。每个API项目和组件都有独立的数据库特定DbContext用于迁移管理，同时保持业务逻辑的统一性。

## 整体架构设计

### 支持的项目

**API服务项目：**
- **IdentityApi** - 身份认证系统
- **ExamApi** - 考试系统
- **ConfigCenter** - 配置中心
- **FileStorageApi** - 文件存储系统
- **SurveyApi** - 问卷系统

**组件项目：**
- **Settings** - 设置管理组件
- **Messaging** - 消息系统组件

### 类层次结构模式

每个项目都遵循相同的DbContext架构模式：

```
BaseDbContext (基类，包含业务逻辑)
├── MySqlDbContext (MySQL特定配置)
└── SqlServerDbContext (SQL Server特定配置)
```

### 具体项目示例

**IdentityApi项目：**
```
ApplicationDbContext (基类)
├── MySqlDbContext (MySQL特定)
└── SqlServerDbContext (SQL Server特定)
```

**ExamApi项目：**
```
ExamDbContext (基类)
├── MySqlExamDbContext (MySQL特定)
└── SqlServerExamDbContext (SQL Server特定)
```

**Settings组件：**
```
SettingsDbContext (基类)
├── MySqlSettingsDbContext (MySQL特定)
└── SqlServerSettingsDbContext (SQL Server特定)
```

## 文件结构

### API服务项目结构

```
Src/ApiServices/CodeSpirit.ExamApi/Data/
├── ExamDbContext.cs                    # 基础DbContext，包含业务逻辑
├── MySqlExamDbContext.cs               # MySQL特定DbContext
├── SqlServerExamDbContext.cs           # SQL Server特定DbContext
├── MySqlExamDbContextFactory.cs        # MySQL设计时工厂
├── SqlServerExamDbContextFactory.cs    # SQL Server设计时工厂
└── ExamDbContextFactory.cs             # 原有工厂（保留兼容性）
```

### 组件项目结构

```
Src/Components/CodeSpirit.Settings/Data/
├── SettingsDbContext.cs                # 基础DbContext，包含业务逻辑
├── MySqlSettingsDbContext.cs           # MySQL特定DbContext
├── SqlServerSettingsDbContext.cs       # SQL Server特定DbContext
├── MySqlSettingsDbContextFactory.cs    # MySQL设计时工厂
├── SqlServerSettingsDbContextFactory.cs # SQL Server设计时工厂
└── SettingsDbContextFactory.cs         # 原有工厂（保留兼容性）
```

## 核心特性

### 1. 统一的基础架构

所有项目的基础DbContext都继承自`MultiDatabaseDbContextBase`，提供：
- 多租户支持
- 审计功能
- 软删除
- 全局查询过滤器
- 事件处理
- 数据库特定配置抽象

### 2. 数据库特定配置

**MySQL配置特点：**
- DateTime类型配置为`datetime(6)`
- 字符串类型UTF8MB4字符集配置
- 布尔类型配置为`tinyint(1)`
- 长度优化和索引支持
- 时间戳默认值使用`CURRENT_TIMESTAMP`

**SQL Server配置特点：**
- Identity字段长度限制
- 完整的字符串长度支持
- 丰富的索引选项
- 时间戳默认值使用`GETUTCDATE()`

### 3. 业务代码保持统一

业务服务和控制器只需要注入基础DbContext，无需关心具体的数据库类型：

```csharp
// ExamApi示例
public class QuestionService : BaseCRUDService<Question, QuestionDto, QuestionQueryDto>
{
    public QuestionService(ExamDbContext context, IMapper mapper) 
        : base(context, mapper)
    {
        // 业务逻辑无需关心数据库类型
    }
}

// Settings组件示例
public class SettingsService : ISettingsService
{
    private readonly SettingsDbContext _context;
    
    public SettingsService(SettingsDbContext context)
    {
        _context = context;
    }
    
    // 业务逻辑统一
}
```

## 迁移管理

### 统一迁移工具

使用`manage-multi-database-migrations.ps1`脚本管理所有项目的迁移：

```powershell
# 语法
.\Scripts\manage-multi-database-migrations.ps1 -ApiProject <ProjectName> -DatabaseType <DatabaseType> -Action <Action> [-MigrationName <Name>]
```

### 各项目迁移示例

#### API项目迁移

```powershell
# ExamApi迁移
.\Scripts\manage-multi-database-migrations.ps1 -ApiProject ExamApi -DatabaseType MySql -Action Add -MigrationName "AddQuestionBank"
.\Scripts\manage-multi-database-migrations.ps1 -ApiProject ExamApi -DatabaseType SqlServer -Action Add -MigrationName "AddQuestionBank"

# SurveyApi迁移
.\Scripts\manage-multi-database-migrations.ps1 -ApiProject SurveyApi -DatabaseType MySql -Action Add -MigrationName "AddSurveyLogic"
.\Scripts\manage-multi-database-migrations.ps1 -ApiProject SurveyApi -DatabaseType SqlServer -Action Add -MigrationName "AddSurveyLogic"

# ConfigCenter迁移
.\Scripts\manage-multi-database-migrations.ps1 -ApiProject ConfigCenter -DatabaseType MySql -Action Add -MigrationName "AddAppConfig"
.\Scripts\manage-multi-database-migrations.ps1 -ApiProject ConfigCenter -DatabaseType SqlServer -Action Add -MigrationName "AddAppConfig"

# FileStorageApi迁移
.\Scripts\manage-multi-database-migrations.ps1 -ApiProject FileStorageApi -DatabaseType MySql -Action Add -MigrationName "AddMetadata"
.\Scripts\manage-multi-database-migrations.ps1 -ApiProject FileStorageApi -DatabaseType SqlServer -Action Add -MigrationName "AddMetadata"
```

#### 组件迁移

```powershell
# Settings组件迁移
.\Scripts\manage-multi-database-migrations.ps1 -ApiProject Settings -DatabaseType MySql -Action Add -MigrationName "AddUserPreferences"
.\Scripts\manage-multi-database-migrations.ps1 -ApiProject Settings -DatabaseType SqlServer -Action Add -MigrationName "AddUserPreferences"

# Messaging组件迁移
.\Scripts\manage-multi-database-migrations.ps1 -ApiProject Messaging -DatabaseType MySql -Action Add -MigrationName "AddChatRooms"
.\Scripts\manage-multi-database-migrations.ps1 -ApiProject Messaging -DatabaseType SqlServer -Action Add -MigrationName "AddChatRooms"
```

#### 其他操作

```powershell
# 查看迁移历史
.\Scripts\manage-multi-database-migrations.ps1 -ApiProject ExamApi -DatabaseType MySql -Action List

# 更新数据库
.\Scripts\manage-multi-database-migrations.ps1 -ApiProject SurveyApi -DatabaseType SqlServer -Action Update

# 删除最后一个迁移
.\Scripts\manage-multi-database-migrations.ps1 -ApiProject Settings -DatabaseType MySql -Action Remove
```

## 自动迁移机制

### 应用启动时的自动处理

每个API项目在启动时都会自动处理数据库迁移：

#### ExamApi示例

```csharp
// 在 ExamApiConfiguration.InitializeDatabaseAsync 中
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
        logger.LogError(ex, "初始化考试系统数据库时发生错误：{Message}", ex.Message);
        throw;
    }
}
```

#### SurveyApi示例

```csharp
// 在 SurveyApiConfiguration.InitializeDatabaseAsync 中
public override async Task InitializeDatabaseAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<SurveyApiConfiguration>>();
    var configuration = services.GetRequiredService<IConfiguration>();
    
    try
    {
        // 应用数据库迁移
        await DatabaseMigrationHelper.ApplyDatabaseMigrationsAsync<MySqlSurveyDbContext, SqlServerSurveyDbContext>(
            services, configuration, logger, "SurveyApi");
        
        // 初始化数据
        var context = services.GetRequiredService<SurveyDbContext>();
        await context.InitializeDatabaseAsync();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "初始化问卷系统数据库时发生错误：{Message}", ex.Message);
        throw;
    }
}
```

### 启动流程

1. **检测数据库类型**: 根据配置确定使用的数据库类型
2. **选择对应DbContext**: 加载MySQL或SQL Server特定的DbContext
3. **应用待处理迁移**: 自动执行所有未应用的迁移
4. **数据种子初始化**: 执行必要的初始数据创建

## 依赖注入配置

### 多数据库支持配置

每个项目都使用`DatabaseMigrationHelper.ConfigureMultiDatabaseDbContext`进行配置：

#### ExamApi配置示例

```csharp
public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    // 配置多数据库支持的考试系统数据库
    DatabaseMigrationHelper.ConfigureMultiDatabaseDbContext<ExamDbContext, MySqlExamDbContext, SqlServerExamDbContext>(
        services, configuration, ConnectionStringKey);
    
    // 其他服务注册...
}
```

#### Settings组件配置示例

```csharp
public static IServiceCollection AddSettingsManagerWithDatabase(this IServiceCollection services, IConfiguration configuration)
{
    // 配置多数据库支持
    DatabaseMigrationHelper.ConfigureMultiDatabaseDbContext<SettingsDbContext, MySqlSettingsDbContext, SqlServerSettingsDbContext>(
        services, configuration, "settings");
    
    // 注册设置服务
    services.AddScoped<ISettingsService, SettingsService>();
    
    return services;
}
```

## 迁移目录结构

### 统一的目录组织

每个项目的迁移文件都按数据库类型分离存储：

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

## 配置说明

### 数据库类型配置

在各个API项目的配置文件中：

```json
{
  "DatabaseType": "MySql"  // 或 "SqlServer"
}
```

### 连接字符串配置

#### API项目连接字符串

```json
{
  "ConnectionStrings": {
    "exam-api": "Server=localhost;Port=3306;Database=exam-api;Uid=root;Pwd=password;CharSet=utf8mb4;",
    "config-center": "Server=localhost;Port=3306;Database=config-center;Uid=root;Pwd=password;CharSet=utf8mb4;",
    "file-storage-api": "Server=localhost;Port=3306;Database=file-storage-api;Uid=root;Pwd=password;CharSet=utf8mb4;",
    "survey-api": "Server=localhost;Port=3306;Database=survey-api;Uid=root;Pwd=password;CharSet=utf8mb4;"
  }
}
```

#### 组件连接字符串

```json
{
  "ConnectionStrings": {
    "settings": "Server=localhost;Port=3306;Database=settings;Uid=root;Pwd=password;CharSet=utf8mb4;",
    "messaging": "Server=localhost;Port=3306;Database=messaging;Uid=root;Pwd=password;CharSet=utf8mb4;"
  }
}
```

## 开发工作流

### 1. 添加新功能的数据库变更

```powershell
# 步骤1: 修改实体类和DbContext配置

# 步骤2: 为每种数据库创建迁移
.\Scripts\manage-multi-database-migrations.ps1 -ApiProject ExamApi -DatabaseType MySql -Action Add -MigrationName "AddNewFeature"
.\Scripts\manage-multi-database-migrations.ps1 -ApiProject ExamApi -DatabaseType SqlServer -Action Add -MigrationName "AddNewFeature"

# 步骤3: 检查生成的迁移文件
.\Scripts\manage-multi-database-migrations.ps1 -ApiProject ExamApi -DatabaseType MySql -Action List

# 步骤4: 应用迁移（通常应用启动时自动处理）
.\Scripts\manage-multi-database-migrations.ps1 -ApiProject ExamApi -DatabaseType MySql -Action Update
```

### 2. 跨项目功能开发

当功能涉及多个项目时：

```powershell
# 为考试系统添加用户分析功能
.\Scripts\manage-multi-database-migrations.ps1 -ApiProject ExamApi -DatabaseType MySql -Action Add -MigrationName "AddUserAnalytics"
.\Scripts\manage-multi-database-migrations.ps1 -ApiProject ExamApi -DatabaseType SqlServer -Action Add -MigrationName "AddUserAnalytics"

# 为问卷系统添加相关分析功能
.\Scripts\manage-multi-database-migrations.ps1 -ApiProject SurveyApi -DatabaseType MySql -Action Add -MigrationName "AddUserAnalytics"
.\Scripts\manage-multi-database-migrations.ps1 -ApiProject SurveyApi -DatabaseType SqlServer -Action Add -MigrationName "AddUserAnalytics"

# 为设置组件添加分析配置支持
.\Scripts\manage-multi-database-migrations.ps1 -ApiProject Settings -DatabaseType MySql -Action Add -MigrationName "AddAnalyticsSettings"
.\Scripts\manage-multi-database-migrations.ps1 -ApiProject Settings -DatabaseType SqlServer -Action Add -MigrationName "AddAnalyticsSettings"
```

### 3. 组件开发

```powershell
# Settings组件新功能
.\Scripts\manage-multi-database-migrations.ps1 -ApiProject Settings -DatabaseType MySql -Action Add -MigrationName "AddThemeSettings"
.\Scripts\manage-multi-database-migrations.ps1 -ApiProject Settings -DatabaseType SqlServer -Action Add -MigrationName "AddThemeSettings"

# Messaging组件新功能
.\Scripts\manage-multi-database-migrations.ps1 -ApiProject Messaging -DatabaseType MySql -Action Add -MigrationName "AddGroupChat"
.\Scripts\manage-multi-database-migrations.ps1 -ApiProject Messaging -DatabaseType SqlServer -Action Add -MigrationName "AddGroupChat"
```

## 优势

### 1. 统一的架构模式
- 所有项目遵循相同的DbContext设计模式
- 一致的迁移管理方式
- 标准化的配置和部署流程

### 2. 代码复用最大化
- 业务逻辑在基础DbContext中统一管理
- 数据库特定配置分离，避免重复
- 公共组件可被多个项目复用

### 3. 数据库优化
- 每种数据库都有专门的优化配置
- 充分利用数据库特性
- 更好的性能表现

### 4. 维护性和扩展性
- 数据库特定逻辑分离
- 迁移文件独立管理
- 易于添加新的数据库支持
- 便于问题排查和调试

### 5. 开发体验
- 统一的迁移管理工具
- 自动化的数据库初始化
- 一致的开发和部署流程

## 注意事项

### 1. 迁移文件管理
- 不同数据库的迁移文件分别存储
- 需要为每种数据库分别生成迁移
- 使用统一的迁移脚本自动处理目录和DbContext选择

### 2. 开发环境配置
- 每个项目需要配置正确的连接字符串
- 确保数据库服务正在运行
- 验证配置文件中的DatabaseType设置

### 3. 部署考虑
- 生产环境需要为每个项目明确指定数据库类型
- 确保所有迁移文件正确部署
- 验证数据库连接字符串配置正确

### 4. 性能监控
- 关注不同项目的数据库性能
- 定期检查数据库特定配置的效果
- 监控迁移执行时间和成功率

## 故障排除

### 1. 迁移生成失败
- 检查项目路径是否正确
- 确认DbContextFactory是否存在
- 验证配置文件格式正确性
- 确保数据库服务正在运行

### 2. 运行时错误
- 检查依赖注入配置
- 确认数据库类型配置正确
- 验证连接字符串有效性
- 查看应用程序日志

### 3. 性能问题
- 检查数据库特定配置是否生效
- 验证索引创建情况
- 分析查询执行计划
- 优化数据库连接池配置

### 4. 迁移冲突
- 使用脚本删除有问题的迁移
- 重新生成迁移文件
- 检查迁移文件的依赖关系

## 最佳实践

### 1. 开发阶段
- 同时为MySQL和SQL Server创建迁移
- 使用描述性的迁移名称
- 频繁创建小的迁移而不是大的变更
- 迁移文件也需要代码审查

### 2. 测试阶段
- 在两种数据库上都进行测试
- 验证迁移的向前和向后兼容性
- 测试不同数据库配置的性能差异

### 3. 部署阶段
- 部署前备份生产数据库
- 先部署迁移，再部署应用代码
- 准备迁移回滚方案
- 监控部署过程和结果

### 4. 维护阶段
- 定期清理过时的迁移文件
- 保持迁移文档的及时更新
- 根据使用情况调整数据库配置
- 定期审计数据库权限和访问

## 总结

通过这套统一的多数据库DbContext架构，CodeSpirit项目实现了：

- ✅ **架构统一性**: 所有项目遵循相同的设计模式
- ✅ **代码复用性**: 业务逻辑统一管理，避免重复
- ✅ **数据库优化**: 充分利用不同数据库的特性
- ✅ **维护便利性**: 分离的迁移管理和清晰的配置
- ✅ **扩展灵活性**: 易于添加新项目和数据库支持
- ✅ **开发效率**: 统一的工具和流程

这种设计为系统的长期维护、性能优化和功能扩展奠定了坚实的基础，同时保持了良好的开发体验和代码质量。