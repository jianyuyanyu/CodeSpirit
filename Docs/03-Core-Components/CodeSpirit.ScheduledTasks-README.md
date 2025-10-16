# CodeSpirit.ScheduledTasks 定时任务组件

## 概述

CodeSpirit.ScheduledTasks 是一个基于分布式缓存的定时任务组件，专为 CodeSpirit 框架设计。该组件支持分布式执行、超时终止、配置文件定义任务等功能，无需数据库依赖。

## 核心特性

- ✅ **基于缓存存储**: 使用Redis分布式缓存，无需数据库
- ✅ **分布式执行**: 利用分布式锁确保多实例环境下任务不重复执行
- ✅ **超时终止**: 支持任务执行超时自动终止机制
- ✅ **多种任务类型**: 支持Cron表达式定时任务和延迟任务
- ✅ **配置文件定义**: 支持通过appsettings.json预定义任务
- ✅ **查询服务**: 提供专门的查询服务接口
- ✅ **AMIS管理界面**: 在Web项目中集成管理界面

## 项目结构

```
Src/Components/CodeSpirit.ScheduledTasks/
├── Models/                           # 数据模型
│   ├── ScheduledTask.cs             # 定时任务模型
│   ├── TaskExecution.cs             # 任务执行记录
│   ├── TaskStatus.cs                # 任务状态枚举
│   └── ...
├── Configuration/                    # 配置选项
│   └── ScheduledTasksOptions.cs
├── Services/                         # 服务实现
│   ├── IScheduledTaskService.cs
│   ├── ScheduledTaskService.cs
│   ├── IScheduledTaskQueryService.cs
│   ├── ScheduledTaskQueryService.cs
│   ├── ITaskExecutor.cs
│   └── TaskExecutor.cs
├── Background/                       # 后台服务
│   └── ScheduledTaskBackgroundService.cs
├── Extensions/                       # 扩展方法
│   └── ServiceCollectionExtensions.cs
└── Helpers/                         # 辅助类
    ├── CronHelper.cs
    └── TaskTimeoutHelper.cs
```

## 快速开始

### 1. 服务注册

```csharp
// 在 Program.cs 中注册服务
builder.Services.AddCodeSpiritScheduledTasks(builder.Configuration, "YourServiceName");
```

### 2. 配置选项

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
        "Type": "Cron",
        "CronExpression": "0 2 * * *",
        "HandlerType": "YourApp.Tasks.DailyCleanupTaskHandler",
        "Enabled": true
      }
    ]
  }
}
```

### 3. 创建任务处理器

```csharp
public class DailyCleanupTaskHandler : ITaskHandler
{
    public async Task ExecuteAsync(TaskExecutionContext context, CancellationToken cancellationToken)
    {
        // 实现任务逻辑
        await DoCleanupAsync(cancellationToken);
    }
}
```

## Web管理界面

组件提供了完整的Web管理界面，包括：

- 📋 任务列表管理（创建、编辑、删除、启用/禁用）
- 📊 执行历史查看和统计
- 🔍 实时任务监控
- ⚙️ Cron表达式验证器
- 📈 任务统计信息

访问路径：`/api/scheduled-tasks`

## 技术实现

### 分布式锁机制
- 锁键格式：`CodeSpirit:ScheduledTasks:Lock:{TaskId}`
- 基于Redis Lua脚本实现
- 自动过期和续期机制

### 缓存存储设计
- 任务定义：`CodeSpirit:ScheduledTasks:Tasks:{TaskId}`
- 执行记录：`CodeSpirit:ScheduledTasks:Executions:{ExecutionId}`
- 任务索引：`CodeSpirit:ScheduledTasks:Index:Active`

### 超时控制
- 使用 `CancellationTokenSource` 实现
- 支持优雅停止和强制终止
- 超时后自动释放分布式锁

## 测试覆盖

组件包含完整的单元测试：

```bash
# 运行测试
dotnet test Tests/Components/CodeSpirit.ScheduledTasks.Tests/
```

测试覆盖范围：
- ✅ 核心服务功能测试
- ✅ Cron表达式解析测试
- ✅ 任务执行器测试
- ✅ 分布式锁测试
- ✅ 配置加载测试

## 文档

- 📖 [使用指南](./CodeSpirit.ScheduledTasks定时任务组件使用指南.md)
- 🏗️ [技术设计文档](./CodeSpirit.ScheduledTasks技术设计文档.md)

## 依赖组件

- [CodeSpirit.Caching](./CodeSpirit.Caching统一缓存组件指南.md) - 缓存和分布式锁
- [CodeSpirit.Amis](../02-UI-Generation/CodeSpirit.Amis智能界面生成引擎.md) - Web管理界面

## 版本信息

- **当前版本**: v1.0.0
- **兼容框架**: .NET 9.0+
- **创建日期**: 2024年

## 开发状态

🎉 **开发完成** - 所有核心功能已实现并通过测试

- ✅ 核心数据模型和服务
- ✅ 分布式任务调度
- ✅ Web管理界面
- ✅ 单元测试覆盖
- ✅ 完整文档

## 下一步计划

- 🔄 任务依赖关系支持
- 🔄 更丰富的重试策略
- 🔄 任务执行结果通知
- 🔄 更多的监控指标
