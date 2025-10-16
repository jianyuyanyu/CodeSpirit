# CodeSpirit.ScheduledTasks 定时任务组件使用指南

## 概述

CodeSpirit.ScheduledTasks 是一个基于分布式缓存的定时任务组件，支持分布式执行、超时终止、配置文件定义任务等功能。该组件依赖现有的缓存组件，无需数据库支持，适用于分布式环境。

## 核心特性

- **基于缓存存储**：使用Redis分布式缓存存储任务信息，无需数据库依赖
- **分布式执行**：利用分布式锁确保多实例环境下任务不重复执行
- **超时终止**：支持任务执行超时自动终止机制
- **多种任务类型**：支持Cron表达式定时任务和延迟任务
- **配置文件定义**：支持通过appsettings.json预定义任务
- **查询服务**：提供专门的查询服务接口
- **AMIS管理界面**：在Web项目中集成管理界面

## 快速开始

### 1. 安装和配置

在需要使用定时任务的项目中添加组件引用：

```xml
<ProjectReference Include="..\..\Components\CodeSpirit.ScheduledTasks\CodeSpirit.ScheduledTasks.csproj" />
```

### 2. 服务注册

在 `Program.cs` 中注册服务：

```csharp
// 注册定时任务组件
builder.Services.AddCodeSpiritScheduledTasks(builder.Configuration, "YourServiceName");
```

### 3. 配置选项

在 `appsettings.json` 中添加配置：

```json
{
  "ScheduledTasks": {
    "Enabled": true,
    "DefaultTimeout": "00:30:00",
    "MaxConcurrentTasks": 10,
    "TaskCleanupInterval": "01:00:00",
    "ExecutionHistoryRetention": "7.00:00:00",
    "Tasks": [
      {
        "Id": "daily-cleanup",
        "Name": "每日清理任务",
        "Description": "清理过期数据",
        "Type": "Cron",
        "CronExpression": "0 2 * * *",
        "Timeout": "00:15:00",
        "Enabled": true,
        "HandlerType": "YourApp.Tasks.DailyCleanupTaskHandler"
      }
    ]
  }
}
```

## 任务类型

### 1. Cron定时任务

使用Cron表达式定义执行时间：

```csharp
var task = new ScheduledTask
{
    Id = "backup-task",
    Name = "数据备份任务",
    Type = TaskType.Cron,
    CronExpression = "0 0 2 * * ?", // 每天凌晨2点执行
    HandlerType = "MyApp.Tasks.BackupTaskHandler",
    Timeout = TimeSpan.FromHours(2),
    Enabled = true
};

await taskService.CreateTaskAsync(task);
```

### 2. 延迟任务

指定延迟时间后执行：

```csharp
var task = new ScheduledTask
{
    Id = "notification-task",
    Name = "发送通知",
    Type = TaskType.OneTime,
    ScheduledTime = DateTime.UtcNow.AddMinutes(30),
    HandlerType = "MyApp.Tasks.NotificationTaskHandler",
    Timeout = TimeSpan.FromMinutes(5),
    Enabled = true
};

await taskService.CreateTaskAsync(task);
```

## 任务处理器

### 创建任务处理器

实现 `ITaskHandler` 接口：

```csharp
public class BackupTaskHandler : ITaskHandler
{
    private readonly ILogger<BackupTaskHandler> _logger;
    private readonly IBackupService _backupService;

    public BackupTaskHandler(ILogger<BackupTaskHandler> logger, IBackupService backupService)
    {
        _logger = logger;
        _backupService = backupService;
    }

    public async Task ExecuteAsync(TaskExecutionContext context, CancellationToken cancellationToken)
    {
        _logger.LogInformation("开始执行备份任务: {TaskId}", context.Task.Id);

        try
        {
            // 检查取消令牌
            cancellationToken.ThrowIfCancellationRequested();

            // 执行备份逻辑
            await _backupService.CreateBackupAsync(cancellationToken);

            _logger.LogInformation("备份任务执行成功: {TaskId}", context.Task.Id);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("备份任务被取消: {TaskId}", context.Task.Id);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "备份任务执行失败: {TaskId}", context.Task.Id);
            throw;
        }
    }
}
```

### 注册任务处理器

在 `Program.cs` 中注册：

```csharp
builder.Services.AddScoped<BackupTaskHandler>();
```

## 服务接口

### IScheduledTaskService

主要的任务管理服务：

```csharp
public interface IScheduledTaskService
{
    // 任务管理
    Task<ScheduledTask> CreateTaskAsync(ScheduledTask task);
    Task<ScheduledTask?> UpdateTaskAsync(ScheduledTask task);
    Task<bool> DeleteTaskAsync(string taskId);
    Task<ScheduledTask?> GetTaskAsync(string taskId);
    
    // 任务控制
    Task<bool> EnableTaskAsync(string taskId);
    Task<bool> DisableTaskAsync(string taskId);
    Task<string> TriggerTaskAsync(string taskId);
    Task<bool> CancelExecutionAsync(string executionId);
    
    // 配置管理
    Task<int> LoadTasksFromConfigurationAsync();
}
```

### IScheduledTaskQueryService

查询服务：

```csharp
public interface IScheduledTaskQueryService
{
    // 任务查询
    Task<PageList<ScheduledTask>> GetTasksPagedAsync(TaskQueryDto queryDto);
    Task<PageList<TaskExecution>> GetExecutionHistoryAsync(string taskId, QueryDtoBase queryDto);
    Task<PageList<TaskExecution>> GetAllExecutionHistoryAsync(ExecutionQueryDto queryDto);
    Task<List<TaskExecution>> GetRunningExecutionsAsync();
    
    // 统计信息
    Task<TaskStatistics> GetTaskStatisticsAsync();
}
```

## Web管理界面

组件提供了基于AMIS的Web管理界面，包含以下功能：

### 1. 任务列表

- 支持分页、搜索、筛选
- 显示任务状态、下次执行时间等信息
- 支持批量操作

### 2. 任务管理

- 创建/编辑任务表单
- Cron表达式验证器
- 任务启用/禁用操作

### 3. 执行历史

- 查看任务执行记录
- 执行状态和错误信息
- 执行时长统计

### 4. 实时监控

- 正在执行的任务
- 任务统计信息
- 系统状态监控

## Cron表达式

### 格式说明

支持标准的6位Cron表达式：

```
秒 分 时 日 月 星期
```

### 常用示例

```csharp
// 每分钟执行
"0 * * * * ?"

// 每小时执行
"0 0 * * * ?"

// 每天凌晨2点执行
"0 0 2 * * ?"

// 每周一上午9点执行
"0 0 9 ? * MON"

// 每月1号凌晨执行
"0 0 0 1 * ?"
```

### 预设表达式

组件提供了常用的预设表达式：

```csharp
var presets = CronHelper.Presets.GetAll();
// 包含：每分钟、每小时、每天、每周、每月等常用表达式
```

## 分布式执行

### 分布式锁机制

组件使用分布式锁确保任务在多实例环境下不重复执行：

- 锁键格式：`CodeSpirit:ScheduledTasks:Lock:{TaskId}`
- 锁超时时间：任务超时时间 + 30秒缓冲
- 自动释放：任务完成或超时后自动释放锁

### 实例协调

- 只有获得锁的实例才能执行任务
- 其他实例会跳过该任务的执行
- 支持实例故障转移

## 超时控制

### 超时机制

- 每个任务可配置独立的超时时间
- 超时后自动取消任务执行
- 支持优雅停止和强制终止

### 配置示例

```csharp
var task = new ScheduledTask
{
    // ... 其他配置
    Timeout = TimeSpan.FromMinutes(30), // 30分钟超时
};
```

## 错误处理

### 异常处理

- 任务执行异常会被捕获并记录
- 支持重试机制（可配置）
- 异常信息存储在执行记录中

### 日志记录

组件会自动记录以下日志：

- 任务开始/完成
- 执行异常
- 超时取消
- 分布式锁获取/释放

## 性能优化

### 缓存策略

- 任务定义缓存：减少重复读取
- 执行记录批量写入：提高性能
- 合理的过期时间设置

### 资源管理

- 限制并发执行任务数量
- 定期清理过期执行记录
- 内存使用优化

## 监控和诊断

### 健康检查

组件提供健康检查端点：

```csharp
builder.Services.AddHealthChecks()
    .AddCheck<ScheduledTasksHealthCheck>("scheduled-tasks");
```

### 指标收集

- 任务执行次数
- 执行时长统计
- 失败率统计
- 并发任务数量

## 最佳实践

### 1. 任务设计

- 保持任务幂等性
- 合理设置超时时间
- 避免长时间运行的任务

### 2. 错误处理

- 实现适当的重试逻辑
- 记录详细的错误信息
- 监控任务执行状态

### 3. 性能优化

- 避免在高峰期执行重任务
- 合理分配任务执行时间
- 监控系统资源使用

### 4. 安全考虑

- 验证任务处理器类型
- 限制任务执行权限
- 审计任务操作日志

## 故障排除

### 常见问题

1. **任务不执行**
   - 检查任务是否启用
   - 验证Cron表达式
   - 确认分布式锁状态

2. **任务重复执行**
   - 检查分布式锁配置
   - 验证Redis连接
   - 确认实例时钟同步

3. **任务超时**
   - 调整超时时间设置
   - 优化任务执行逻辑
   - 检查系统资源

### 调试技巧

- 启用详细日志记录
- 使用管理界面监控
- 检查缓存中的任务状态

## 版本历史

- **v1.0.0**: 初始版本，支持基本的定时任务功能
- 支持Cron表达式和延迟任务
- 分布式执行和超时控制
- AMIS管理界面集成

## 相关组件

- [CodeSpirit.Caching](./CodeSpirit.Caching统一缓存组件指南.md) - 缓存组件
- [CodeSpirit.Amis](../02-UI-Generation/CodeSpirit.Amis智能界面生成引擎.md) - 界面生成引擎
- [CodeSpirit.Audit](./CodeSpirit.Audit分布式审计完整指南.md) - 审计组件

## 技术支持

如有问题或建议，请通过以下方式联系：

- 项目仓库：提交Issue
- 技术文档：查看相关组件文档
- 开发团队：内部技术支持渠道
