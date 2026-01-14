# Pathfinder（探路者）项目文档

## 📚 文档总览

本目录包含 Pathfinder（探路者）项目在 CodeSpirit 框架下的完整实施方案和技术文档。

## 📖 项目简介

**Pathfinder（探路者）** 是一款以AI为核心驱动的"目标-任务-执行"一体化管理工具，通过智能化的目标拆解、动态督促和自动化执行，帮助用户将模糊的目标转化为可落地的行动路径。

**核心价值主张：** "将模糊目标转化为自动执行的行动路径，让每个人都拥有AI私人教练"

## 🗂️ 文档列表

### 核心文档

1. **[Pathfinder实施方案.md](./pathfinder-implementation-plan-zh-CN.md)** ⭐ **重点文档**
   - 完整的技术实施方案
   - 基于 CodeSpirit 框架的架构设计
   - MVP 和一期扩展的详细开发计划
   - 数据库设计、服务拆分、部署方案
   - 测试策略、安全性考虑、性能优化

2. **[技术路线图.md](./pathfinder-technology-roadmap-zh-CN.md)** ⭐ **开发指南**
   - 详细的开发时间表（12周MVP + 12周一期）
   - 按周拆解的开发任务
   - 里程碑与验收标准
   - 资源分配与风险管理
   - 技术产出清单

3. **[快速参考指南.md](./pathfinder-quick-reference-zh-CN.md)** ⭐ **速查手册**
   - 核心功能清单
   - 技术架构速查
   - API速查表
   - 代码模板
   - 常用命令
   - 常见问题解答

### 源设计文档（参考）

位于 `D:\repos\pathfinder` 目录：

- **产品需求文档.md** - 完整的PRD文档，定义产品功能和需求
- **详细需求设计文档.md** - 详细的业务逻辑和数据模型设计
- **自动化机器人技术设计方案.md** - 自动化执行引擎的技术方案
- **精益画布.md** - 商业模式和产品定位

## 🚀 快速开始

### 1. 了解项目背景

先阅读源设计文档，了解产品定位和业务需求：
```bash
# 查看产品需求
cat D:\repos\pathfinder\产品需求文档.md

# 查看精益画布（商业模式）
cat D:\repos\pathfinder\精益画布.md
```

### 2. 学习技术方案

阅读本目录的实施方案文档：
- [Pathfinder实施方案.md](./pathfinder-implementation-plan-zh-CN.md)

### 3. 准备开发环境

参考 [开发环境搭建指南](../01-Core-Docs/03-development-environment-setup-zh-CN.md)

### 4. 了解基础框架

学习 CodeSpirit 框架的核心组件：
- [CodeSpirit框架核心亮点](../codespirit-framework-highlights-zh-CN.md)
- [项目整体架构设计](../01-Core-Docs/01-project-architecture-zh-CN.md)

## 🏗️ 技术架构

### 技术栈

- **后端框架：** .NET 10 + ASP.NET Core + Entity Framework Core
- **服务编排：** .NET Aspire
- **数据库：** SQL Server / MySQL（多数据库支持）
- **缓存：** Redis
- **消息队列：** RabbitMQ
- **前端：** React + AMIS (AntD主题)
- **AI能力：** CodeSpirit.LLM 组件

### 服务架构

```
CodeSpirit.AppHost (Aspire编排)
├── Pathfinder.Api (目标管理服务)
├── Pathfinder.AutomationApi (自动化执行服务)
├── CodeSpirit.MessagingApi (提醒与通知服务)
├── CodeSpirit.IdentityApi (身份认证服务)
└── CodeSpirit.Web (前端入口)
```

### 核心组件集成

- ✅ **CodeSpirit.LLM** - AI大语言模型集成（目标理解、任务拆解）
- ✅ **CodeSpirit.Amis** - 智能界面生成引擎（管理界面）
- ✅ **CodeSpirit.Authorization** - 权限管理
- ✅ **CodeSpirit.Audit** - 审计追踪
- ✅ **CodeSpirit.ScheduledTasks** - 定时任务（提醒检查）
- ✅ **CodeSpirit.Messaging** - 消息队列（异步任务）
- ✅ **CodeSpirit.Caching** - 分布式缓存
- ✅ **CodeSpirit.MultiTenant** - 多租户支持

## 📋 开发计划

### MVP阶段（0-3个月）

**核心功能：**
1. ✅ 目标输入与理解
2. ✅ AI智能任务拆解
3. ✅ 任务看板与管理
4. ✅ 智能提醒系统
5. ✅ 用户账号系统（复用现有）

**成功指标：**
- 7日留存率 > 30%
- 用户平均创建目标数 > 2
- NPS > 40

### 一期扩展（3-6个月）

**扩展功能：**
1. ✅ 自动化执行机器人（信息查询、日程通知）
2. ✅ 进度预警与成就系统
3. ✅ 目标复盘功能

**成功指标：**
- 月留存率 > 50%
- 自动化功能使用率 > 40%
- 付费用户 > 200
- 免费到付费转化率 > 5%

### 二期成长（6-12个月）

**成长功能：**
- 复杂数据处理自动化
- 更多第三方工具集成
- 数据看板与分析
- 移动端应用

## 🎯 核心功能特性

### 1. AI智能任务拆解

基于 **CodeSpirit.LLM** 组件实现：

```csharp
public async Task<List<TaskDto>> BreakdownGoalAsync(Goal goal)
{
    var prompt = BuildBreakdownPrompt(goal);
    var response = await _llmService.GenerateAsync(prompt);
    return ParseTasksFromResponse(response);
}
```

### 2. 智能界面生成

基于 **CodeSpirit.Amis** 实现零代码界面：

```csharp
[ApiController]
[Route("api/goals")]
public class GoalsController : AmisControllerBase<Goal, GoalDto>
{
    // AMIS自动生成CRUD界面
}
```

### 3. 定时提醒系统

基于 **CodeSpirit.ScheduledTasks** 实现：

```csharp
[ScheduledTask("0 * * * *")] // 每小时执行
public class ReminderCheckTask : IScheduledTask
{
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await _reminderService.CheckAndSendRemindersAsync();
    }
}
```

### 4. 自动化执行引擎

基于工具化抽象和AI驱动：

```csharp
public interface IExecutionTool
{
    ToolMetadata GetMetadata();
    Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters);
}

// AI智能选择工具并执行
var tool = await _toolSelector.SelectToolAsync(taskDescription);
var result = await tool.ExecuteAsync(parameters);
```

## 📊 数据模型

### 核心实体

- **Goal（目标）** - 用户创建的目标
- **Task（任务）** - AI拆解的任务
- **AutomationConfig（自动化配置）** - 任务的自动化执行配置
- **Agent（执行代理）** - 自动化任务的执行代理
- **Reminder（提醒记录）** - 系统发送的提醒
- **Achievement（成就）** - 用户解锁的成就
- **GoalReview（目标复盘）** - 目标完成后的复盘报告

### 实体关系

```
User → Goal → Task → AutomationConfig → Agent
              ↓
         Reminder
              ↓
      UserAchievement
```

## 🚀 部署指南

### 开发环境

```bash
# 启动 Aspire AppHost
cd Src/CodeSpirit.AppHost
dotnet run

# 访问 Aspire Dashboard
# http://localhost:15000
```

### 数据库迁移

```bash
# SQL Server
dotnet ef migrations add InitialCreate --context PathfinderDbContext --output-dir Migrations/SqlServer

# MySQL
dotnet ef migrations add InitialCreate --context PathfinderDbContext --output-dir Migrations/MySql

# 应用迁移
dotnet ef database update
```

## 📖 相关文档

### CodeSpirit框架文档

- [总体技术体系说明](../01-Core-Docs/02-technical-system-overview-zh-CN.md)
- [项目整体架构设计](../01-Core-Docs/01-project-architecture-zh-CN.md)
- [CodeSpirit.Core核心框架](../01-Core-Docs/04-codespirit-core-framework-zh-CN.md)

### 核心组件文档

- [CodeSpirit.LLM大语言模型组件使用指南](../03-Core-Components/codespirit-llm-guide-zh-CN.md)
- [CodeSpirit.Amis智能界面生成引擎](../02-UI-Generation/codespirit-amis-engine-zh-CN.md)
- [CodeSpirit.ScheduledTasks组件文档](../03-Core-Components/codespirit-scheduled-tasks-doc-zh-CN.md)
- [CodeSpirit.Messaging消息队列组件](../03-Core-Components/)
- [CodeSpirit.Authorization权限组件详解](../04-Identity-Auth/codespirit-authorization-guide-zh-CN.md)

### 基础设施文档

- [CodeSpirit.Aspire数据库集成统一方案](../06-Infrastructure/codespirit-aspire-database-integration-guide-zh-CN.md)
- [CodeSpirit.Caching统一缓存组件指南](../06-Infrastructure/codespirit-caching-guide-zh-CN.md)
- [RabbitMQ-Aspire-Integration](../06-Infrastructure/rabbitmq-aspire-integration-zh-CN.md)

## 🔧 开发规范

### 代码规范

遵循 CodeSpirit 项目规范：

1. **文件组织**
   - 每个 .cs 文件只定义一个顶级类型
   - 文件夹结构反映命名空间结构

2. **文档注释**
   - 所有公共成员必须添加 XML 文档注释
   - 必须包含：`<summary>`、`<param>`、`<returns>`、`<exception>`

3. **异步编程**
   - I/O 操作必须使用异步方法（`async/await`）
   - 避免 `Task.Result` 和 `Task.Wait()`

4. **数据访问**
   - 使用 Code First 迁移
   - 实体配置使用 `IEntityTypeConfiguration<T>`
   - 只读查询使用 `AsNoTracking()`
   - 避免 N+1 查询，合理使用 `Include`

### 分支策略

```
main (生产环境)
  └─ develop (开发环境)
       ├─ feature/goal-management (目标管理)
       ├─ feature/task-breakdown (任务拆解)
       ├─ feature/automation (自动化执行)
       └─ feature/reminder (提醒系统)
```

## ❓ 常见问题

### Q: 如何添加新的自动化工具？

实现 `IExecutionTool` 接口并注册到工具注册中心：

```csharp
public class MyCustomTool : IExecutionTool
{
    public ToolMetadata GetMetadata() { ... }
    public Task<ToolResult> ExecuteAsync(...) { ... }
}

// 注册
services.AddSingleton<IExecutionTool, MyCustomTool>();
```

### Q: 如何扩展AI拆解能力？

优化 Prompt 模板或添加领域知识库：

```csharp
var prompt = $@"
你是一个{goal.Category}领域的专家...
参考知识库：{knowledgeBase}
";
```

### Q: 如何实现多租户隔离？

继承 `ITenantEntity` 接口，自动应用租户过滤：

```csharp
public class Goal : AuditedEntity, ITenantEntity
{
    public Guid? TenantId { get; set; }
}
```

## 📞 联系方式

- **项目仓库：** [GitHub](https://github.com/your-repo/pathfinder)
- **技术支持：** [Issues](https://github.com/your-repo/pathfinder/issues)
- **文档反馈：** [Discussions](https://github.com/your-repo/pathfinder/discussions)

---

**文档版本：** v1.0  
**最后更新：** 2025年11月3日  
**维护团队：** Pathfinder开发团队

