# CodeSpirit.ScheduledTasks - 定时任务组件

## 概述

CodeSpirit.ScheduledTasks 是一个基于缓存的分布式定时任务组件，支持Cron表达式、延迟任务、超时终止和AMIS管理界面。

## 核心特性

- **基于缓存存储**：使用Redis分布式缓存存储任务信息，无需数据库依赖
- **去中心化架构**：每个微服务独立管理自己的任务，通过HTTP端点支持跨服务触发
- **分布式执行**：利用分布式锁确保多实例环境下任务不重复执行
- **超时终止**：支持任务执行超时自动终止机制
- **多种任务类型**：支持Cron表达式定时任务、延迟任务和一次性任务
- **配置文件定义**：支持通过appsettings.json预定义任务
- **查询服务**：提供专门的查询服务接口
- **AMIS管理界面**：在Web项目中集成管理界面
- **JWT认证**：Web UI触发任务时使用JWT认证，复用现有认证体系
- **服务发现**：自动注册任务处理器，支持动态服务发现

## 快速开始

### 1. 安装组件

在你的项目中添加对 `CodeSpirit.ScheduledTasks` 的引用：

```xml
<ProjectReference Include="..\..\Components\CodeSpirit.ScheduledTasks\CodeSpirit.ScheduledTasks.csproj" />
```

### 2. 注册服务

在 `Program.cs` 中注册定时任务服务：

```csharp
// 添加定时任务服务（需要指定服务名称，用于服务发现）
builder.Services.AddCodeSpiritScheduledTasks(builder.Configuration, "YourServiceName");

// 注册任务处理器（自动注册到任务注册表）
builder.Services.AddTaskHandler<SampleTaskHandler>();
builder.Services.AddTaskHandler<DataCleanupTaskHandler>();
```

**重要说明**：
- `ServiceName` 参数用于标识当前服务，任务处理器会自动注册到该服务名下
- 每个服务只执行属于自己服务的任务
- Web UI 通过查询任务注册表找到任务所属服务，然后调用该服务的执行端点

### 3. 配置选项

在 `appsettings.json` 中添加配置：

```json
{
  "ScheduledTasks": {
    "Enabled": true,
    "ServiceName": "your-service",  // ✅ 必填：服务名称，用于任务注册和服务发现
    "DefaultTimeout": "00:30:00",
    "MaxConcurrentTasks": 10,
    "ScanInterval": "00:00:30",
    "TaskCleanupInterval": "01:00:00",
    "ExecutionHistoryRetention": "7.00:00:00",
    "Tasks": [
      {
        "Id": "sample-task",
        "Name": "示例任务",
        "Description": "这是一个示例定时任务",
        "Type": "Cron",
        "CronExpression": "0 */5 * * * *",
        "Enabled": true,
        "HandlerType": "YourApp.Tasks.SampleTaskHandler",  // ✅ 只需类型名称，无需程序集名称
        "Parameters": "{\"message\": \"Hello from scheduled task!\"}"
      },
      {
        "Id": "cleanup-task",
        "Name": "数据清理任务",
        "Description": "定期清理过期数据",
        "Type": "Cron",
        "CronExpression": "0 0 2 * * *",
        "Enabled": true,
        "HandlerType": "YourApp.Tasks.DataCleanupTaskHandler",
        "Timeout": "01:00:00",
        "Parameters": "{\"cleanupDays\": 30}"
      }
    ]
  }
}
```

**配置说明**：
- `ServiceName`：必填，用于标识当前服务，任务会自动注册到该服务名下
- `HandlerType`：只需类型名称（如 `YourApp.Tasks.SampleTaskHandler`），无需包含程序集名称
- `Parameters`：JSON字符串格式，任务处理器中需要自行反序列化

## 任务类型

### 1. Cron定时任务

使用Cron表达式定义执行时间：

```csharp
var task = new ScheduledTask
{
    Id = "cron-task",
    Name = "Cron定时任务",
    Type = TaskType.Cron,
    CronExpression = "0 0 9 * * 1-5", // 工作日上午9点
    HandlerType = "YourNamespace.YourTaskHandler",
    Status = TaskStatus.Enabled
};
```

### 2. 延迟任务

指定延迟时间后执行：

```csharp
var task = new ScheduledTask
{
    Id = "delay-task",
    Name = "延迟任务",
    Type = TaskType.Delay,
    DelayTime = TimeSpan.FromMinutes(30),
    HandlerType = "YourNamespace.YourTaskHandler",
    Status = TaskStatus.Enabled
};
```

### 3. 一次性任务

指定具体执行时间：

```csharp
var task = new ScheduledTask
{
    Id = "onetime-task",
    Name = "一次性任务",
    Type = TaskType.OneTime,
    ExecuteAt = DateTime.UtcNow.AddHours(2),
    HandlerType = "YourNamespace.YourTaskHandler",
    Status = TaskStatus.Enabled
};
```

## 创建任务处理器

### 基础任务处理器

```csharp
public class MyTaskHandler : ITaskHandler
{
    private readonly ILogger<MyTaskHandler> _logger;

    public MyTaskHandler(ILogger<MyTaskHandler> logger)
    {
        _logger = logger;
    }

    public async Task<string?> ExecuteAsync(string? parameters, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("开始执行任务，参数: {Parameters}", parameters);

        try
        {
            // 你的任务逻辑
            await DoSomethingAsync(cancellationToken);

            return "任务执行成功";
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("任务被取消");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "任务执行失败");
            throw;
        }
    }

    private async Task DoSomethingAsync(CancellationToken cancellationToken)
    {
        // 实现你的业务逻辑
        await Task.Delay(1000, cancellationToken);
    }
}
```

### 强类型参数处理器

```csharp
public class TypedTaskHandler : ITaskHandler<MyTaskParameters>
{
    public async Task<string?> ExecuteAsync(string? parameters, CancellationToken cancellationToken = default)
    {
        MyTaskParameters? typedParams = null;
        if (!string.IsNullOrEmpty(parameters))
        {
            typedParams = JsonConvert.DeserializeObject<MyTaskParameters>(parameters);
        }
        
        return await ExecuteAsync(typedParams, cancellationToken);
    }

    public async Task<string?> ExecuteAsync(MyTaskParameters? parameters, CancellationToken cancellationToken = default)
    {
        // 使用强类型参数
        var message = parameters?.Message ?? "默认消息";
        
        // 执行任务逻辑
        await Task.Delay(1000, cancellationToken);
        
        return $"处理消息: {message}";
    }
}

public class MyTaskParameters
{
    public string Message { get; set; } = string.Empty;
    public int Count { get; set; }
}
```

## 架构设计

### 去中心化架构

CodeSpirit.ScheduledTasks 采用去中心化架构，每个微服务独立管理自己的任务：

```
┌─────────────────┐         ┌─────────────────┐         ┌─────────────────┐
│   Web UI        │         │   ExamApi       │         │   OtherApi      │
│   (AMIS)        │         │                 │         │                 │
└─────────────────┘         └─────────────────┘         └─────────────────┘
       │                            │                            │
       │  1. 查询任务所属服务        │                            │
       ├────────────────────────────┼────────────────────────────┤
       │                            │                            │
       │  2. HTTP调用执行端点        │                            │
       │  POST /api/scheduled-tasks │                            │
       │  /execute/{taskId}         │                            │
       │                            │                            │
       │  3. JWT认证                │                            │
       │                            │                            │
       │  4. 执行任务               │                            │
       │                            │                            │
       │  后台服务                  │  后台服务                  │
       │  扫描本服务任务            │  扫描本服务任务            │
       │  自动执行                  │  自动执行                  │
       │                            │                            │
       └────────────────────────────┴────────────────────────────┘
                            │
                    ┌───────┴────────┐
                    │  Redis Cache   │
                    │  - 任务注册表   │
                    │  - 任务定义     │
                    │  - 执行历史     │
                    └────────────────┘
```

### 核心组件

#### 任务注册表 (ITaskHandlerRegistry)
- 存储任务处理器与服务名的映射关系
- 支持查询任务所属服务
- 基于Redis实现，支持分布式环境

#### 任务执行端点 (ScheduledTaskExecutionController)
- 提供统一的HTTP执行端点：`POST /api/scheduled-tasks/execute/{taskId}`
- 使用JWT认证，复用现有认证体系
- 验证任务归属，确保安全执行

#### 任务处理器注册服务 (TaskHandlerRegistrationService)
- 服务启动时自动扫描并注册任务处理器
- 将任务与服务的映射关系写入Redis
- 支持动态服务发现

## 管理API

### 任务管理

```csharp
// 创建任务
var task = await taskService.CreateTaskAsync(newTask);

// 更新任务
var updatedTask = await taskService.UpdateTaskAsync(task);

// 删除任务
var success = await taskService.DeleteTaskAsync(taskId);

// 启用/禁用任务
await taskService.EnableTaskAsync(taskId);
await taskService.DisableTaskAsync(taskId);

// 手动触发任务（Web UI会自动查询任务所属服务并调用对应端点）
var executionId = await taskService.TriggerTaskAsync(taskId);
```

### HTTP执行端点

每个服务都提供了统一的执行端点，供Web UI或其他服务调用：

```http
POST /api/scheduled-tasks/execute/{taskId}
Authorization: Bearer {JWT_TOKEN}
```

**响应示例**：
```json
{
  "status": 0,
  "message": "任务已成功触发执行",
  "data": {
    "executionId": "guid-string"
  }
}
```

### 查询服务

```csharp
// 分页查询任务
var queryDto = new TaskQueryDto
{
    Name = "示例",
    Status = TaskStatus.Enabled,
    PageNumber = 1,
    PageSize = 20
};
var result = await queryService.GetTasksPagedAsync(queryDto);

// 获取执行历史
var history = await queryService.GetExecutionHistoryAsync(taskId, queryDto);

// 获取统计信息
var statistics = await queryService.GetTaskStatisticsAsync();
```

## Cron表达式

组件支持包含秒的Cron表达式格式：`秒 分 时 日 月 周`

### 常用表达式

- `* * * * * *` - 每秒执行
- `0 * * * * *` - 每分钟执行
- `0 0 * * * *` - 每小时执行
- `0 0 0 * * *` - 每天凌晨执行
- `0 0 9 * * 1-5` - 工作日上午9点执行
- `0 */15 * * * *` - 每15分钟执行

### 预设表达式

组件提供了常用的Cron表达式预设：

```csharp
CronHelper.Presets.EveryMinute;     // 每分钟
CronHelper.Presets.EveryHour;       // 每小时
CronHelper.Presets.Daily;           // 每天
CronHelper.Presets.Weekly;          // 每周
CronHelper.Presets.Weekdays;        // 工作日
```

## 分布式支持

### 去中心化执行

每个服务只执行属于自己的任务，天然支持分布式环境：

- **服务自治**：每个服务独立管理自己的任务
- **自动注册**：服务启动时自动注册任务处理器
- **服务发现**：通过任务注册表查询任务所属服务
- **HTTP调用**：Web UI通过HTTP调用对应服务的执行端点

### 分布式锁

组件使用分布式锁确保任务在多实例环境下不重复执行：

```csharp
var task = new ScheduledTask
{
    // ...其他配置
    ExecutionStrategy = ExecutionStrategy.Distributed // 分布式执行
};
```

### 单实例执行

如果你的任务只需要在单个实例上执行：

```csharp
var task = new ScheduledTask
{
    // ...其他配置
    ExecutionStrategy = ExecutionStrategy.SingleInstance // 单实例执行
};
```

## 超时控制

### 设置超时时间

```csharp
var task = new ScheduledTask
{
    // ...其他配置
    Timeout = TimeSpan.FromMinutes(30) // 30分钟超时
};
```

### 在处理器中响应取消

```csharp
public async Task<string?> ExecuteAsync(string? parameters, CancellationToken cancellationToken = default)
{
    for (int i = 0; i < 100; i++)
    {
        // 检查取消请求
        cancellationToken.ThrowIfCancellationRequested();
        
        // 执行工作
        await DoWorkAsync(cancellationToken);
    }
    
    return "完成";
}
```

## 错误处理和重试

### 配置重试

```csharp
var task = new ScheduledTask
{
    // ...其他配置
    MaxRetryCount = 3,
    RetryInterval = TimeSpan.FromMinutes(5)
};
```

### 在处理器中处理异常

```csharp
public async Task<string?> ExecuteAsync(string? parameters, CancellationToken cancellationToken = default)
{
    try
    {
        // 任务逻辑
        return "成功";
    }
    catch (SpecificException ex)
    {
        // 记录特定异常
        _logger.LogWarning(ex, "处理特定异常");
        throw; // 重新抛出以触发重试
    }
    catch (Exception ex)
    {
        // 记录一般异常
        _logger.LogError(ex, "任务执行失败");
        throw;
    }
}
```

## 监控和日志

### 执行日志

任务执行过程中的日志会自动记录到执行记录中：

```csharp
// 在TaskExecution中查看日志
var execution = await GetExecutionAsync(executionId);
foreach (var log in execution.Logs)
{
    Console.WriteLine(log);
}
```

### 性能指标

```csharp
// 在TaskExecution中查看性能指标
var metrics = execution.Metrics;
var cpuUsage = metrics.ContainsKey("CpuUsage") ? metrics["CpuUsage"] : null;
```

## 最佳实践

### 1. 任务设计原则

- **幂等性**：确保任务可以安全地重复执行
- **原子性**：任务应该是原子操作，要么全部成功，要么全部失败
- **可取消性**：响应CancellationToken，支持优雅取消

### 2. 错误处理

- 使用适当的异常类型
- 记录详细的错误信息
- 考虑是否需要重试

### 3. 性能优化

- 避免长时间运行的任务
- 使用异步操作
- 合理设置超时时间
- 监控任务执行性能

### 4. 配置管理

- 使用配置文件定义常规任务
- 通过API动态创建临时任务
- 定期清理不需要的任务

## 故障排除

### 常见问题

1. **任务不执行**
   - 检查任务状态是否为Enabled
   - 验证Cron表达式是否正确
   - 确认下次执行时间是否合理

2. **任务重复执行**
   - 检查分布式锁配置
   - 确认Redis连接正常
   - 验证任务ID是否唯一

3. **任务超时**
   - 检查超时配置是否合理
   - 优化任务执行逻辑
   - 考虑拆分长时间任务

4. **处理器未找到**
   - 确认处理器类型名称正确（只需类型名称，无需程序集名称）
   - 检查处理器是否已注册到DI容器（使用 `AddTaskHandler<T>`）
   - 验证服务名称（ServiceName）是否正确配置
   - 检查任务注册表（Redis）中是否有该服务的注册信息

5. **任务不属于当前服务**
   - 确认 `ServiceName` 配置正确
   - 检查任务是否已注册到当前服务
   - 验证任务处理器的注册状态

6. **HTTP调用失败**
   - 检查服务发现配置（Aspire服务发现）
   - 验证JWT Token是否有效
   - 确认目标服务是否正常运行
   - 检查网络连接

### 调试技巧

1. 启用详细日志：
```json
{
  "ScheduledTasks": {
    "EnableDetailedLogging": true
  }
}
```

2. 查看执行历史：
```csharp
var history = await queryService.GetExecutionHistoryAsync(taskId, queryDto);
```

3. 监控正在运行的任务：
```csharp
var runningTasks = await queryService.GetRunningExecutionsAsync();
```

## 许可证

本组件遵循 MIT 许可证。
