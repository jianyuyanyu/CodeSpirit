# Pathfinder（探路者）项目实施方案

## 文档信息

| 项目 | 内容 |
|------|------|
| **文档类型** | 实施方案 |
| **版本** | v1.0 |
| **创建日期** | 2025年11月3日 |
| **目标读者** | 开发团队、架构师、技术负责人 |
| **依赖文档** | pathfinder目录下的设计文档、CodeSpirit框架文档 |
| **基础框架** | CodeSpirit + .NET 9 + Aspire |

---

## 1. 方案概述

### 1.1 项目背景

探路者（Pathfinder）是一款**以AI为核心驱动的"目标-任务-执行"一体化管理工具**，通过智能化的目标拆解、动态督促和自动化执行，帮助用户将模糊的目标转化为可落地的行动路径。

**核心价值主张：** "将模糊目标转化为自动执行的行动路径,让每个人都拥有AI私人教练"

### 1.2 技术栈选型

基于现有 CodeSpirit 框架的技术栈：

**后端框架：**
- ✅ .NET 9.0
- ✅ .NET Aspire（服务编排）
- ✅ ASP.NET Core Web API
- ✅ Entity Framework Core 9.0（多数据库支持）

**数据库：**
- ✅ SQL Server / MySQL（多数据库支持）
- ✅ Redis（分布式缓存）

**消息队列：**
- ✅ RabbitMQ

**前端：**
- ✅ React + AMIS（AntD主题）
- ✅ CodeSpirit.Amis 智能界面生成引擎

**AI能力：**
- ✅ CodeSpirit.LLM 组件（统一的大模型调用抽象）
- ✅ 支持 GPT-4、Claude、Qwen、Llama等

**核心组件：**
- ✅ CodeSpirit.Authorization（权限管理）
- ✅ CodeSpirit.Audit（审计追踪）
- ✅ CodeSpirit.Caching（分布式缓存）
- ✅ CodeSpirit.ScheduledTasks（定时任务）
- ✅ CodeSpirit.Messaging（消息队列）
- ✅ CodeSpirit.MultiTenant（多租户）
- ✅ CodeSpirit.Settings（设置管理）

### 1.3 实施原则

1. **复用现有框架能力**：充分利用CodeSpirit框架的组件和基础设施
2. **模块化设计**：新功能以独立API服务形式实现
3. **渐进式开发**：MVP → 一期扩展 → 二期成长
4. **高可用设计**：利用分布式架构，确保服务高可用
5. **AI优先**：核心业务逻辑基于AI驱动

---

## 2. 架构设计

### 2.1 整体架构

```
┌─────────────────────────────────────────────────────────────────┐
│                      Aspire AppHost（服务编排）                   │
└─────────────────────────────────────────────────────────────────┘
                                │
         ┌──────────────────────┼──────────────────────┐
         │                      │                      │
┌────────▼────────┐  ┌─────────▼─────────┐  ┌────────▼────────┐
│  CodeSpirit.Web │  │ Pathfinder.API    │  │ 现有业务API      │
│  (前端入口)      │  │ (目标管理服务)     │  │ (IdentityApi等)  │
└────────┬────────┘  └─────────┬─────────┘  └────────┬────────┘
         │                      │                      │
         └──────────────────────┼──────────────────────┘
                                │
         ┌──────────────────────┴──────────────────────┐
         │                                             │
┌────────▼────────┐                          ┌────────▼────────┐
│  RabbitMQ       │                          │  Redis          │
│  (消息队列)      │                          │  (缓存/分布式锁) │
└─────────────────┘                          └─────────────────┘
         │                                             │
         └──────────────────────┬──────────────────────┘
                                │
                    ┌───────────▼───────────┐
                    │  SQL Server / MySQL   │
                    │  (主数据库)            │
                    └───────────────────────┘
```

### 2.2 服务拆分

#### 2.2.1 Pathfinder.Api（目标管理服务）

**核心职责：**
- 目标的创建、查询、更新、删除
- AI任务拆解
- 任务看板管理
- 进度追踪与统计

**技术实现：**
- 基于 CodeSpirit.ServiceDefaults
- 使用 CodeSpirit.Amis 生成管理界面
- 集成 CodeSpirit.LLM 实现AI拆解
- 使用 CodeSpirit.Authorization 管理权限
- 使用 CodeSpirit.Audit 记录操作日志

#### 2.2.2 Pathfinder.AutomationApi（自动化执行服务）

**核心职责：**
- 自动化任务的调度与执行
- 执行代理管理
- 工具注册与调用
- 执行结果处理

**技术实现：**
- 独立API服务
- 使用 CodeSpirit.ScheduledTasks 实现定时任务
- 使用 CodeSpirit.Messaging 实现异步任务队列
- 集成 CodeSpirit.LLM 实现智能工具选择

#### 2.2.3 Pathfinder.MessagingApi（提醒与通知服务）

**核心职责：**
- 智能提醒规则管理
- 多渠道通知发送
- 提醒历史记录

**技术实现：**
- 可复用现有 CodeSpirit.MessagingApi
- 扩展提醒规则引擎
- 使用 CodeSpirit.ScheduledTasks 实现定时检查

### 2.3 数据库设计

#### 2.3.1 数据库选择

基于 CodeSpirit 的多数据库支持架构：
- **开发环境：** MySQL（推荐）
- **生产环境：** SQL Server 或 MySQL

#### 2.3.2 核心实体设计

**Goal（目标）**
```csharp
public class Goal : AuditedEntity, ITenantEntity
{
    [Key]
    public Guid Id { get; set; }
    
    public Guid UserId { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Title { get; set; }
    
    [MaxLength(2000)]
    public string Description { get; set; }
    
    public DateTime? TargetDate { get; set; }
    public DateTime? ActualCompletionDate { get; set; }
    
    [Required]
    public GoalPriority Priority { get; set; } = GoalPriority.Medium;
    
    [Required]
    public GoalCategory Category { get; set; }
    
    [Required]
    public GoalStatus Status { get; set; } = GoalStatus.Active;
    
    public int Progress { get; set; }
    public int TaskCount { get; set; }
    public int CompletedTaskCount { get; set; }
    
    // AI解析的结构化数据
    [Column(TypeName = "nvarchar(max)")]
    public string AiParsedData { get; set; }
    
    // 用户偏好设置
    [Column(TypeName = "nvarchar(max)")]
    public string UserPreferences { get; set; }
    
    public DateTime? ArchivedAt { get; set; }
    
    // 导航属性
    public virtual ICollection<Task> Tasks { get; set; }
    public virtual GoalReview Review { get; set; }
    
    // 多租户
    public Guid? TenantId { get; set; }
}
```

**Task（任务）**
```csharp
public class Task : AuditedEntity, ITenantEntity
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    public Guid GoalId { get; set; }
    
    public Guid? ParentTaskId { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Title { get; set; }
    
    [MaxLength(2000)]
    public string Description { get; set; }
    
    public int Order { get; set; }
    
    public int? EstimatedDuration { get; set; } // 分钟
    public int? ActualDuration { get; set; }
    
    public DateTime? SuggestedStartDate { get; set; }
    public DateTime? SuggestedEndDate { get; set; }
    public DateTime? ActualStartDate { get; set; }
    public DateTime? ActualEndDate { get; set; }
    public DateTime? Deadline { get; set; }
    
    [Required]
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    
    [Required]
    public TaskStatus Status { get; set; } = TaskStatus.Pending;
    
    [Required]
    public TaskAssignee Assignee { get; set; } = TaskAssignee.User;
    
    public AutomationType? AutomationType { get; set; }
    public bool IsAutomatable { get; set; }
    
    // 依赖关系（JSON数组）
    [Column(TypeName = "nvarchar(max)")]
    public string Dependencies { get; set; }
    
    [Column(TypeName = "nvarchar(max)")]
    public string Tags { get; set; }
    
    [MaxLength(2000)]
    public string Notes { get; set; }
    
    public DateTime? LastActivityAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    
    // 导航属性
    public virtual Goal Goal { get; set; }
    public virtual Task ParentTask { get; set; }
    public virtual ICollection<Task> SubTasks { get; set; }
    public virtual AutomationConfig AutomationConfig { get; set; }
    public virtual ICollection<Reminder> Reminders { get; set; }
    
    // 多租户
    public Guid? TenantId { get; set; }
}
```

**AutomationConfig（自动化配置）**
```csharp
public class AutomationConfig : AuditedEntity, ITenantEntity
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    public Guid TaskId { get; set; }
    
    public Guid? AgentId { get; set; }
    
    [Required]
    public ToolType ToolType { get; set; }
    
    [MaxLength(50)]
    public string ToolName { get; set; }
    
    public AuthType? AuthType { get; set; }
    
    [MaxLength(500)]
    public string AuthToken { get; set; } // 加密存储
    
    public DateTime? AuthExpiresAt { get; set; }
    
    // 配置参数（JSON）
    [Required]
    [Column(TypeName = "nvarchar(max)")]
    public string ConfigParams { get; set; }
    
    [Required]
    public ScheduleType ScheduleType { get; set; } = ScheduleType.Manual;
    
    [MaxLength(100)]
    public string ScheduleCron { get; set; }
    
    public bool IsEnabled { get; set; }
    
    public int RetryCount { get; set; } = 3;
    public int RetryInterval { get; set; } = 300; // 秒
    
    public DateTime? LastExecutionTime { get; set; }
    public ExecutionStatus? LastExecutionStatus { get; set; }
    
    public int ExecutionCount { get; set; }
    public int SuccessCount { get; set; }
    
    // 导航属性
    public virtual Task Task { get; set; }
    public virtual Agent Agent { get; set; }
    public virtual ICollection<AutomationLog> Logs { get; set; }
    
    // 多租户
    public Guid? TenantId { get; set; }
}
```

**其他核心实体：**
- Agent（执行代理）
- AutomationLog（执行日志）
- Reminder（提醒记录）
- UserReminderSettings（用户提醒设置）
- Achievement（成就定义）
- UserAchievement（用户成就）
- GoalReview（目标复盘）

### 2.4 与现有组件的集成

#### 2.4.1 CodeSpirit.LLM 集成

```csharp
public class GoalBreakdownService
{
    private readonly ILLMService _llmService;
    
    public async Task<List<TaskDto>> BreakdownGoalAsync(Goal goal)
    {
        var prompt = $@"
你是一个项目管理专家，擅长将目标拆解为可执行的任务。

用户目标：{goal.Description}
目标类型：{goal.Category}
完成时间：{goal.TargetDate}

请拆解为5-15个子任务，包含：
1. 任务标题
2. 任务描述
3. 预估耗时
4. 依赖关系
5. 是否可自动化

输出JSON格式...
";

        var response = await _llmService.GenerateAsync(new LLMRequest
        {
            Prompt = prompt,
            Temperature = 0.7,
            MaxTokens = 2000
        });
        
        // 解析响应并返回任务列表
        return ParseTasksFromResponse(response);
    }
}
```

#### 2.4.2 CodeSpirit.Amis 界面生成

```csharp
[ApiController]
[Route("api/goals")]
public class GoalsController : AmisControllerBase<Goal, GoalDto>
{
    public GoalsController(
        IGoalService service,
        IMapper mapper)
        : base(service, mapper)
    {
    }
    
    // AMIS会自动生成CRUD界面
    // 无需手动编写前端代码
}
```

#### 2.4.3 CodeSpirit.ScheduledTasks 集成

```csharp
[ScheduledTask("0 * * * *")] // 每小时执行一次
public class ReminderCheckTask : IScheduledTask
{
    private readonly IReminderService _reminderService;
    
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        // 检查需要发送的提醒
        await _reminderService.CheckAndSendRemindersAsync();
    }
}
```

#### 2.4.4 CodeSpirit.Authorization 集成

```csharp
[ApiController]
[Route("api/goals")]
[Authorize]
public class GoalsController : ControllerBase
{
    [HttpGet]
    [RequirePermission("Goals.View")]
    public async Task<IActionResult> GetGoals()
    {
        // 自动应用租户过滤
        // 自动检查权限
    }
    
    [HttpPost]
    [RequirePermission("Goals.Create")]
    public async Task<IActionResult> CreateGoal([FromBody] GoalDto dto)
    {
        // 自动记录审计日志
    }
}
```

---

## 3. MVP阶段实施计划

### 3.1 功能范围

**MVP核心功能（0-3个月）：**
1. ✅ 目标输入与理解
2. ✅ AI智能任务拆解
3. ✅ 任务看板与管理
4. ✅ 智能提醒系统
5. ✅ 用户账号系统（复用现有IdentityApi）

### 3.2 开发任务分解

#### Phase 1: 基础框架搭建（Week 1-2）

**任务清单：**
- [ ] 创建 Pathfinder.Api 项目
- [ ] 配置数据库迁移（SQL Server & MySQL）
- [ ] 集成 CodeSpirit.ServiceDefaults
- [ ] 配置 Aspire 编排
- [ ] 设置 Redis 缓存
- [ ] 配置 RabbitMQ 连接

**技术实现：**
```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// 添加 ServiceDefaults
builder.AddServiceDefaults();

// 配置数据库
builder.Services.AddDbContext<PathfinderDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("PathfinderDb");
    options.UseSqlServer(connectionString); // 或 UseMySql
});

// 添加 CodeSpirit 核心组件
builder.Services.AddCodeSpiritAuthorization();
builder.Services.AddCodeSpiritAudit();
builder.Services.AddCodeSpiritCaching();
builder.Services.AddCodeSpiritLLM();

var app = builder.Build();
app.MapDefaultEndpoints();
app.Run();
```

#### Phase 2: 目标管理功能（Week 3-4）

**任务清单：**
- [ ] 实现 Goal 实体和 DbContext
- [ ] 实现 GoalService（CRUD）
- [ ] **实现目标可行性评估（重要！）**
  - 评估目标明确性、可执行性、完整性
  - **仅当目标明确、可执行时才支持AI拆解**
  - 模糊目标引导用户澄清
- [ ] 创建 GoalsController（AMIS）
- [ ] AI目标理解（NLP解析）
- [ ] 前端界面生成（AMIS）

**核心代码：**
```csharp
public class GoalService : BaseCRUDService<Goal, GoalDto>
{
    private readonly ILLMService _llmService;
    
    public async Task<GoalDto> CreateGoalAsync(CreateGoalDto dto)
    {
        // 1. AI解析目标
        var parsedData = await ParseGoalWithAIAsync(dto.Description);
        
        // 2. 创建目标实体
        var goal = new Goal
        {
            UserId = _currentUser.Id,
            Title = parsedData.Title,
            Description = dto.Description,
            Category = parsedData.Category,
            TargetDate = parsedData.TargetDate,
            AiParsedData = JsonSerializer.Serialize(parsedData)
        };
        
        // 3. 保存到数据库
        await _repository.InsertAsync(goal);
        
        return _mapper.Map<GoalDto>(goal);
    }
}
```

#### Phase 3: AI任务拆解（Week 5-6）

**任务清单：**
- [ ] 实现 TaskService
- [ ] AI拆解引擎
- [ ] 依赖关系处理
- [ ] 时间节点分配
- [ ] 前端拆解结果展示

**核心实现：**
```csharp
public class TaskBreakdownService
{
    private readonly ILLMService _llmService;
    private readonly IGoalFeasibilityEvaluator _feasibilityEvaluator;
    
    public async Task<BreakdownResult> BreakdownAsync(Goal goal)
    {
        // 1. 目标可行性评估（重要！）
        var feasibility = await _feasibilityEvaluator.EvaluateAsync(goal);
        
        if (!feasibility.IsFeasible)
        {
            // 目标不够明确，返回澄清问题
            return BreakdownResult.RequiresClarification(
                feasibility.ClarificationQuestions,
                feasibility.Suggestions
            );
        }
        
        // 2. 仅当目标明确、可执行时，才调用LLM拆解
        var prompt = BuildBreakdownPrompt(goal);
        var response = await _llmService.GenerateAsync(prompt);
        
        // 3. 解析响应
        var taskDtos = ParseTasksFromLLM(response);
        
        // 4. 分配时间节点
        AssignTimeNodes(taskDtos, goal.TargetDate);
        
        // 5. 创建任务实体
        var tasks = taskDtos.Select(dto => new Task
        {
            GoalId = goal.Id,
            Title = dto.Title,
            Description = dto.Description,
            Order = dto.Order,
            EstimatedDuration = dto.EstimatedDuration,
            SuggestedStartDate = dto.SuggestedStartDate,
            SuggestedEndDate = dto.SuggestedEndDate,
            IsAutomatable = dto.IsAutomatable
        }).ToList();
        
        // 6. 批量插入
        await _repository.BulkInsertAsync(tasks);
        
        return BreakdownResult.Success(tasks);
    }
}

/// <summary>
/// 目标可行性评估器
/// 仅当目标明确、可执行时，才允许AI拆解
/// </summary>
public class GoalFeasibilityEvaluator : IGoalFeasibilityEvaluator
{
    private readonly ILLMService _llmService;
    
    public async Task<FeasibilityEvaluation> EvaluateAsync(Goal goal)
    {
        var prompt = $@"
你是一个目标管理专家，负责评估用户输入的目标是否明确且可执行。

用户目标：{goal.Description}

请评估以下维度：
1. **明确性**：目标是否清晰明确？是否有具体的成功标准？
2. **可执行性**：目标是否可以拆解为具体行动？是否在用户能力范围内？
3. **完整性**：是否包含时间约束、范围、预期结果？

如果目标过于模糊（如"我想变好""提升自己"），需要返回澄清问题。

输出JSON格式：
{{
  ""is_feasible"": true/false,
  ""clarity_score"": 0-10,
  ""executability_score"": 0-10,
  ""completeness_score"": 0-10,
  ""issues"": [""问题1"", ""问题2""],
  ""clarification_questions"": [""请明确具体目标"", ""请设定时间范围""],
  ""suggestions"": [""建议1"", ""建议2""]
}}
";

        var response = await _llmService.GenerateAsync(new LLMRequest
        {
            Prompt = prompt,
            Temperature = 0.3, // 降低创造性，提高判断准确性
            MaxTokens = 1000
        });
        
        var evaluation = JsonSerializer.Deserialize<FeasibilityEvaluation>(response);
        
        // 设定阈值：三个维度都要>=7分才认为可行
        evaluation.IsFeasible = evaluation.ClarityScore >= 7 
                             && evaluation.ExecutabilityScore >= 7 
                             && evaluation.CompletenessScore >= 7;
        
        return evaluation;
    }
}

/// <summary>
/// 可行性评估结果
/// </summary>
public class FeasibilityEvaluation
{
    public bool IsFeasible { get; set; }
    public int ClarityScore { get; set; }         // 明确性评分 0-10
    public int ExecutabilityScore { get; set; }   // 可执行性评分 0-10
    public int CompletenessScore { get; set; }    // 完整性评分 0-10
    public List<string> Issues { get; set; }
    public List<string> ClarificationQuestions { get; set; }
    public List<string> Suggestions { get; set; }
}

/// <summary>
/// 拆解结果（包含成功/需澄清两种状态）
/// </summary>
public class BreakdownResult
{
    public bool Success { get; set; }
    public bool RequiresClarification { get; set; }
    public List<Task> Tasks { get; set; }
    public List<string> ClarificationQuestions { get; set; }
    public List<string> Suggestions { get; set; }
    
    public static BreakdownResult Success(List<Task> tasks)
    {
        return new BreakdownResult
        {
            Success = true,
            Tasks = tasks
        };
    }
    
    public static BreakdownResult RequiresClarification(
        List<string> questions, 
        List<string> suggestions)
    {
        return new BreakdownResult
        {
            RequiresClarification = true,
            ClarificationQuestions = questions,
            Suggestions = suggestions
        };
    }
}
```

#### Phase 4: 任务看板（Week 7-8）

**任务清单：**
- [ ] 任务状态管理
- [ ] 看板视图（AMIS）
- [ ] 依赖关系处理
- [ ] 进度计算
- [ ] 任务编辑功能

**AMIS配置：**
```csharp
[AmisPage]
[AmisPageTitle("任务看板")]
public class TasksController : AmisControllerBase<Task, TaskDto>
{
    [AmisPage(PageType = PageType.List)]
    [AmisCard(Mode = CardMode.List)]
    public async Task<IActionResult> GetTasks()
    {
        // AMIS自动生成看板界面
        // 支持列表/看板/日历视图
    }
}
```

#### Phase 5: 提醒系统（Week 9-10）

**任务清单：**
- [ ] 提醒规则引擎
- [ ] 定时任务调度
- [ ] 多渠道通知
- [ ] 用户提醒设置

**实现：**
```csharp
[ScheduledTask("* * * * *")] // 每分钟执行
public class ReminderCheckTask : IScheduledTask
{
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        // 1. 获取需要发送的提醒
        var reminders = await _reminderService.GetPendingRemindersAsync();
        
        // 2. 批量发送
        foreach (var reminder in reminders)
        {
            await SendReminderAsync(reminder);
        }
    }
}
```

#### Phase 6: 测试与优化（Week 11-12）

**任务清单：**
- [ ] 单元测试
- [ ] 集成测试
- [ ] 性能测试
- [ ] 用户验收测试
- [ ] Bug修复与优化

### 3.3 数据库迁移

```bash
# 创建迁移（SQL Server）
cd Src/ApiServices/Pathfinder.Api
dotnet ef migrations add InitialCreate --context PathfinderDbContext --output-dir Migrations/SqlServer

# 创建迁移（MySQL）
dotnet ef migrations add InitialCreate --context PathfinderDbContext --output-dir Migrations/MySql

# 应用迁移
dotnet ef database update --context PathfinderDbContext
```

### 3.4 Aspire配置

```csharp
// CodeSpirit.AppHost/Program.cs
var builder = DistributedApplication.CreateBuilder(args);

// 添加数据库
var sqlServer = builder.AddSqlServer("sqlserver")
    .AddDatabase("PathfinderDb");

// 添加 Redis
var redis = builder.AddRedis("redis");

// 添加 RabbitMQ
var rabbitmq = builder.AddRabbitMQ("rabbitmq");

// 添加 Pathfinder API
var pathfinderApi = builder.AddProject<Projects.Pathfinder_Api>("pathfinder-api")
    .WithReference(sqlServer)
    .WithReference(redis)
    .WithReference(rabbitmq);

// 添加 Web 前端
builder.AddProject<Projects.CodeSpirit_Web>("web")
    .WithReference(pathfinderApi);

builder.Build().Run();
```

---

## 4. 一期扩展实施计划

### 4.1 功能范围

**一期扩展功能（3-6个月）：**
1. ✅ 自动化执行机器人
2. ✅ 进度预警与成就系统
3. ✅ 目标复盘功能

### 4.2 自动化执行机器人实施

#### 4.2.1 服务架构

创建独立的自动化服务：

```csharp
// Pathfinder.AutomationApi/Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// 添加执行引擎
builder.Services.AddSingleton<IAgentPoolManager, AgentPoolManager>();
builder.Services.AddSingleton<IToolRegistry, ToolRegistry>();
builder.Services.AddScoped<IExecutionEngine, ExecutionEngine>();

// 添加定时任务
builder.Services.AddCodeSpiritScheduledTasks();

// 添加消息队列
builder.Services.AddCodeSpiritMessaging();

var app = builder.Build();
app.MapDefaultEndpoints();
app.Run();
```

#### 4.2.2 执行代理框架

```csharp
public abstract class ExecutionAgentBase
{
    public string AgentId { get; }
    public AgentStatus Status { get; protected set; }
    
    public abstract Task InitializeAsync();
    public abstract Task<ExecutionResult> ExecuteAsync(ExecutionRequest request);
    public abstract Task ShutdownAsync();
}

public class UniversalAgent : ExecutionAgentBase
{
    private readonly IPythonExecutor _pythonExecutor;
    private readonly IToolRegistry _toolRegistry;
    private readonly ILogger<UniversalAgent> _logger;
    
    public override async Task<ExecutionResult> ExecuteAsync(ExecutionRequest request)
    {
        try
        {
            _logger.LogInformation("Agent {AgentId} executing request: {Mode}", AgentId, request.Mode);
            
            if (request.Mode == ExecutionMode.Tool)
            {
                var tool = _toolRegistry.GetTool(request.ToolName);
                return await tool.ExecuteAsync(request.Parameters);
            }
            else if (request.Mode == ExecutionMode.PythonScript)
            {
                return await _pythonExecutor.ExecuteAsync(request.PythonCode, request.Parameters);
            }
            else
            {
                throw new NotSupportedException($"Execution mode {request.Mode} is not supported");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Agent {AgentId} execution failed", AgentId);
            return ExecutionResult.Failure(ex.Message);
        }
    }
}
```

#### 4.2.3 Python脚本执行器（核心实现）

**基于.NET Process的Python执行器：**

```csharp
/// <summary>
/// Python脚本执行器
/// 使用 System.Diagnostics.Process 启动独立Python进程
/// </summary>
public class PythonExecutor : IPythonExecutor
{
    private readonly ILogger<PythonExecutor> _logger;
    private readonly PythonExecutorOptions _options;
    private readonly ISandboxManager _sandboxManager;
    
    public PythonExecutor(
        ILogger<PythonExecutor> logger,
        IOptions<PythonExecutorOptions> options,
        ISandboxManager sandboxManager)
    {
        _logger = logger;
        _options = options.Value;
        _sandboxManager = sandboxManager;
    }
    
    public async Task<ExecutionResult> ExecuteAsync(
        string pythonCode, 
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        // 1. 创建沙箱环境
        var sandbox = await _sandboxManager.CreateSandboxAsync();
        
        try
        {
            // 2. 准备Python脚本文件
            var scriptPath = await PrepareScriptFileAsync(sandbox, pythonCode, parameters);
            
            // 3. 配置Python进程
            var startInfo = new ProcessStartInfo
            {
                FileName = _options.PythonPath ?? "python",
                Arguments = $"\"{scriptPath}\"",
                WorkingDirectory = sandbox.WorkingDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = false,
                CreateNoWindow = true,
                
                // 环境变量隔离
                EnvironmentVariables =
                {
                    ["PYTHONPATH"] = sandbox.LibraryPath,
                    ["PYTHONUNBUFFERED"] = "1" // 禁用缓冲，实时输出
                }
            };
            
            // 4. 启动进程
            using var process = new Process { StartInfo = startInfo };
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();
            
            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    outputBuilder.AppendLine(e.Data);
                    _logger.LogDebug("Python stdout: {Output}", e.Data);
                }
            };
            
            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    errorBuilder.AppendLine(e.Data);
                    _logger.LogWarning("Python stderr: {Error}", e.Data);
                }
            };
            
            var stopwatch = Stopwatch.StartNew();
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            
            // 5. 超时控制
            var timeoutMs = _options.ExecutionTimeoutSeconds * 1000;
            var completed = await Task.Run(() => process.WaitForExit(timeoutMs), cancellationToken);
            
            stopwatch.Stop();
            
            if (!completed)
            {
                // 超时，强制终止
                _logger.LogWarning("Python execution timeout after {Timeout}s", _options.ExecutionTimeoutSeconds);
                process.Kill(entireProcessTree: true);
                return ExecutionResult.Failure("Execution timeout", stopwatch.Elapsed);
            }
            
            // 6. 读取结果
            var output = outputBuilder.ToString();
            var error = errorBuilder.ToString();
            var exitCode = process.ExitCode;
            
            _logger.LogInformation(
                "Python execution completed: ExitCode={ExitCode}, Duration={Duration}ms",
                exitCode, stopwatch.ElapsedMilliseconds);
            
            // 7. 解析输出
            if (exitCode == 0)
            {
                var result = ParseExecutionOutput(output);
                result.ExecutionTime = stopwatch.Elapsed;
                return result;
            }
            else
            {
                return ExecutionResult.Failure(
                    $"Python script failed with exit code {exitCode}\n{error}",
                    stopwatch.Elapsed);
            }
        }
        finally
        {
            // 8. 清理沙箱
            await _sandboxManager.CleanupSandboxAsync(sandbox);
        }
    }
    
    /// <summary>
    /// 准备Python脚本文件，注入参数
    /// </summary>
    private async Task<string> PrepareScriptFileAsync(
        Sandbox sandbox, 
        string pythonCode, 
        Dictionary<string, object> parameters)
    {
        var scriptPath = Path.Combine(sandbox.WorkingDirectory, $"script_{Guid.NewGuid():N}.py");
        
        // 构造完整脚本（注入参数 + 用户代码 + 输出包装）
        var fullScript = $@"
import json
import sys
from datetime import datetime

# === 注入参数 ===
PARAMETERS = {JsonSerializer.Serialize(parameters)}

# === 工具函数 ===
def output_result(data, success=True):
    """"""输出结果（JSON格式）""""""
    result = {{
        'success': success,
        'data': data,
        'timestamp': datetime.utcnow().isoformat()
    }}
    print('__RESULT_START__')
    print(json.dumps(result, ensure_ascii=False))
    print('__RESULT_END__')

def get_parameter(key, default=None):
    """"""获取参数""""""
    return PARAMETERS.get(key, default)

# === 用户代码 ===
try:
{IndentPythonCode(pythonCode, 4)}
except Exception as e:
    output_result({{'error': str(e)}}, success=False)
    sys.exit(1)
";

        await File.WriteAllTextAsync(scriptPath, fullScript);
        return scriptPath;
    }
    
    /// <summary>
    /// 解析Python脚本输出
    /// </summary>
    private ExecutionResult ParseExecutionOutput(string output)
    {
        // 查找 __RESULT_START__ 和 __RESULT_END__ 之间的JSON
        var startMarker = "__RESULT_START__";
        var endMarker = "__RESULT_END__";
        
        var startIndex = output.IndexOf(startMarker);
        var endIndex = output.IndexOf(endMarker);
        
        if (startIndex >= 0 && endIndex > startIndex)
        {
            var jsonStart = startIndex + startMarker.Length;
            var jsonLength = endIndex - jsonStart;
            var json = output.Substring(jsonStart, jsonLength).Trim();
            
            try
            {
                var result = JsonSerializer.Deserialize<PythonExecutionOutput>(json);
                return new ExecutionResult
                {
                    Success = result.Success,
                    Data = result.Data,
                    RawOutput = output
                };
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse Python output JSON");
            }
        }
        
        // 如果没有找到标记，返回原始输出
        return new ExecutionResult
        {
            Success = true,
            Data = output,
            RawOutput = output
        };
    }
    
    private string IndentPythonCode(string code, int spaces)
    {
        var indent = new string(' ', spaces);
        var lines = code.Split('\n');
        return string.Join('\n', lines.Select(line => indent + line));
    }
}

/// <summary>
/// Python执行器配置
/// </summary>
public class PythonExecutorOptions
{
    /// <summary>
    /// Python可执行文件路径（默认使用PATH环境变量中的python）
    /// </summary>
    public string PythonPath { get; set; } = "python";
    
    /// <summary>
    /// 执行超时时间（秒）
    /// </summary>
    public int ExecutionTimeoutSeconds { get; set; } = 30;
    
    /// <summary>
    /// 允许的最大内存（MB）
    /// </summary>
    public int MaxMemoryMB { get; set; } = 512;
    
    /// <summary>
    /// 允许的Python库白名单
    /// </summary>
    public List<string> AllowedLibraries { get; set; } = new()
    {
        "requests", "beautifulsoup4", "pandas", "numpy", "json", "datetime"
    };
}

/// <summary>
/// 沙箱管理器（安全隔离）
/// </summary>
public interface ISandboxManager
{
    Task<Sandbox> CreateSandboxAsync();
    Task CleanupSandboxAsync(Sandbox sandbox);
}

public class SandboxManager : ISandboxManager
{
    private readonly ILogger<SandboxManager> _logger;
    private readonly string _sandboxRootPath;
    
    public SandboxManager(ILogger<SandboxManager> logger, IConfiguration configuration)
    {
        _logger = logger;
        _sandboxRootPath = configuration["Pathfinder:SandboxPath"] 
            ?? Path.Combine(Path.GetTempPath(), "pathfinder_sandbox");
        
        Directory.CreateDirectory(_sandboxRootPath);
    }
    
    public async Task<Sandbox> CreateSandboxAsync()
    {
        var sandboxId = Guid.NewGuid().ToString("N");
        var workingDir = Path.Combine(_sandboxRootPath, sandboxId);
        
        Directory.CreateDirectory(workingDir);
        
        var sandbox = new Sandbox
        {
            Id = sandboxId,
            WorkingDirectory = workingDir,
            LibraryPath = Path.Combine(workingDir, "lib"),
            CreatedAt = DateTime.UtcNow
        };
        
        Directory.CreateDirectory(sandbox.LibraryPath);
        
        _logger.LogInformation("Created sandbox {SandboxId} at {Path}", sandboxId, workingDir);
        
        return await Task.FromResult(sandbox);
    }
    
    public async Task CleanupSandboxAsync(Sandbox sandbox)
    {
        try
        {
            if (Directory.Exists(sandbox.WorkingDirectory))
            {
                Directory.Delete(sandbox.WorkingDirectory, recursive: true);
                _logger.LogInformation("Cleaned up sandbox {SandboxId}", sandbox.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup sandbox {SandboxId}", sandbox.Id);
        }
        
        await Task.CompletedTask;
    }
}

public class Sandbox
{
    public string Id { get; set; }
    public string WorkingDirectory { get; set; }
    public string LibraryPath { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// 执行结果
/// </summary>
public class ExecutionResult
{
    public bool Success { get; set; }
    public object Data { get; set; }
    public string RawOutput { get; set; }
    public string ErrorMessage { get; set; }
    public TimeSpan ExecutionTime { get; set; }
    
    public static ExecutionResult Failure(string error, TimeSpan? executionTime = null)
    {
        return new ExecutionResult
        {
            Success = false,
            ErrorMessage = error,
            ExecutionTime = executionTime ?? TimeSpan.Zero
        };
    }
}

internal class PythonExecutionOutput
{
    public bool Success { get; set; }
    public object Data { get; set; }
    public string Timestamp { get; set; }
}
```

**配置文件（appsettings.json）：**

```json
{
  "Pathfinder": {
    "SandboxPath": "D:/pathfinder_sandbox",
    "PythonExecutor": {
      "PythonPath": "python",
      "ExecutionTimeoutSeconds": 30,
      "MaxMemoryMB": 512,
      "AllowedLibraries": [
        "requests",
        "beautifulsoup4",
        "pandas",
        "numpy",
        "selenium"
      ]
    }
  }
}
```

**注册服务：**

```csharp
// Pathfinder.AutomationApi/Program.cs
builder.Services.Configure<PythonExecutorOptions>(
    builder.Configuration.GetSection("Pathfinder:PythonExecutor"));

builder.Services.AddSingleton<ISandboxManager, SandboxManager>();
builder.Services.AddScoped<IPythonExecutor, PythonExecutor>();
```

#### 4.2.4 工具系统

```csharp
public interface IExecutionTool
{
    ToolMetadata GetMetadata();
    Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters);
}

// 示例：网页爬取工具
public class WebScraperTool : IExecutionTool
{
    public ToolMetadata GetMetadata() => new()
    {
        Name = "WebScraperTool",
        Description = "抓取网页内容并提取信息",
        Category = "DataCollection",
        Parameters = new[]
        {
            new ParameterSchema { Name = "url", Type = "string", Required = true },
            new ParameterSchema { Name = "selector", Type = "string", Required = true }
        }
    };
    
    public async Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        var url = parameters["url"].ToString();
        var selector = parameters["selector"].ToString();
        
        // 使用 Playwright 抓取
        using var playwright = await Playwright.CreateAsync();
        var browser = await playwright.Chromium.LaunchAsync();
        var page = await browser.NewPageAsync();
        await page.GotoAsync(url);
        
        var content = await page.QuerySelectorAsync(selector);
        var text = await content.InnerTextAsync();
        
        return ToolResult.Success(new { content = text });
    }
}
```

#### 4.2.5 Python脚本执行示例

**示例1：数据抓取脚本**

```python
# 用户编写的Python脚本（保存在数据库中）
import requests
from bs4 import BeautifulSoup

# 获取参数（由.NET注入）
url = get_parameter('url', 'https://example.com')
selector = get_parameter('selector', 'h1')

# 执行任务
response = requests.get(url)
soup = BeautifulSoup(response.content, 'html.parser')
result = soup.select_one(selector).text

# 输出结果（会被.NET解析）
output_result({
    'url': url,
    'title': result,
    'status_code': response.status_code
})
```

**示例2：数据处理脚本**

```python
import pandas as pd
import json

# 获取参数
csv_path = get_parameter('csv_path')
filter_column = get_parameter('filter_column', 'status')
filter_value = get_parameter('filter_value', 'active')

# 读取并处理数据
df = pd.read_csv(csv_path)
filtered = df[df[filter_column] == filter_value]

# 输出结果
output_result({
    'total_rows': len(df),
    'filtered_rows': len(filtered),
    'data': filtered.to_dict('records')[:10]  # 只返回前10行
})
```

#### 4.2.6 代理服务API接口

**AutomationController（自动化控制器）：**

```csharp
[ApiController]
[Route("api/automation")]
[Authorize]
public class AutomationController : ControllerBase
{
    private readonly IExecutionEngine _executionEngine;
    private readonly IAutomationConfigService _configService;
    private readonly ILogger<AutomationController> _logger;
    
    public AutomationController(
        IExecutionEngine executionEngine,
        IAutomationConfigService configService,
        ILogger<AutomationController> logger)
    {
        _executionEngine = executionEngine;
        _configService = configService;
        _logger = logger;
    }
    
    /// <summary>
    /// 执行Python脚本
    /// </summary>
    [HttpPost("execute/python")]
    [RequirePermission("Automation.Execute")]
    public async Task<IActionResult> ExecutePython([FromBody] ExecutePythonRequest request)
    {
        _logger.LogInformation("User {UserId} executing Python script", User.GetUserId());
        
        try
        {
            var executionRequest = new ExecutionRequest
            {
                Mode = ExecutionMode.PythonScript,
                PythonCode = request.Code,
                Parameters = request.Parameters ?? new(),
                Timeout = TimeSpan.FromSeconds(request.TimeoutSeconds ?? 30)
            };
            
            var result = await _executionEngine.ExecuteAsync(executionRequest);
            
            return Ok(new
            {
                success = result.Success,
                data = result.Data,
                executionTime = result.ExecutionTime.TotalMilliseconds,
                rawOutput = result.RawOutput,
                errorMessage = result.ErrorMessage
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Python execution failed");
            return StatusCode(500, new { error = ex.Message });
        }
    }
    
    /// <summary>
    /// 执行预定义工具
    /// </summary>
    [HttpPost("execute/tool")]
    [RequirePermission("Automation.Execute")]
    public async Task<IActionResult> ExecuteTool([FromBody] ExecuteToolRequest request)
    {
        try
        {
            var executionRequest = new ExecutionRequest
            {
                Mode = ExecutionMode.Tool,
                ToolName = request.ToolName,
                Parameters = request.Parameters ?? new()
            };
            
            var result = await _executionEngine.ExecuteAsync(executionRequest);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tool execution failed: {ToolName}", request.ToolName);
            return StatusCode(500, new { error = ex.Message });
        }
    }
    
    /// <summary>
    /// 获取可用工具列表
    /// </summary>
    [HttpGet("tools")]
    public async Task<IActionResult> GetAvailableTools()
    {
        var tools = await _executionEngine.GetAvailableToolsAsync();
        return Ok(tools);
    }
    
    /// <summary>
    /// 保存自动化配置
    /// </summary>
    [HttpPost("configs")]
    [RequirePermission("Automation.Create")]
    public async Task<IActionResult> CreateAutomationConfig([FromBody] CreateAutomationConfigDto dto)
    {
        var config = await _configService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetAutomationConfig), new { id = config.Id }, config);
    }
    
    /// <summary>
    /// 获取自动化配置
    /// </summary>
    [HttpGet("configs/{id}")]
    public async Task<IActionResult> GetAutomationConfig(Guid id)
    {
        var config = await _configService.GetByIdAsync(id);
        if (config == null)
            return NotFound();
        
        return Ok(config);
    }
    
    /// <summary>
    /// 测试执行（验证脚本正确性）
    /// </summary>
    [HttpPost("test")]
    [RequirePermission("Automation.Test")]
    public async Task<IActionResult> TestExecution([FromBody] TestExecutionRequest request)
    {
        // 测试模式：更严格的超时，限制资源
        var executionRequest = new ExecutionRequest
        {
            Mode = ExecutionMode.PythonScript,
            PythonCode = request.Code,
            Parameters = request.TestParameters ?? new(),
            Timeout = TimeSpan.FromSeconds(10), // 测试超时10秒
            IsTestMode = true
        };
        
        var result = await _executionEngine.ExecuteAsync(executionRequest);
        
        return Ok(new
        {
            success = result.Success,
            data = result.Data,
            executionTime = result.ExecutionTime.TotalMilliseconds,
            logs = result.RawOutput,
            errorMessage = result.ErrorMessage,
            recommendation = result.Success 
                ? "脚本执行成功，可以保存配置" 
                : "脚本执行失败，请检查错误信息"
        });
    }
}

/// <summary>
/// 执行Python脚本请求
/// </summary>
public class ExecutePythonRequest
{
    [Required]
    public string Code { get; set; }
    
    public Dictionary<string, object> Parameters { get; set; }
    
    public int? TimeoutSeconds { get; set; }
}

/// <summary>
/// 执行工具请求
/// </summary>
public class ExecuteToolRequest
{
    [Required]
    public string ToolName { get; set; }
    
    [Required]
    public Dictionary<string, object> Parameters { get; set; }
}

/// <summary>
/// 测试执行请求
/// </summary>
public class TestExecutionRequest
{
    [Required]
    public string Code { get; set; }
    
    public Dictionary<string, object> TestParameters { get; set; }
}
```

**执行引擎（统一调度）：**

```csharp
/// <summary>
/// 执行引擎（统一调度代理和工具）
/// </summary>
public class ExecutionEngine : IExecutionEngine
{
    private readonly IAgentPoolManager _agentPool;
    private readonly IToolRegistry _toolRegistry;
    private readonly ILogger<ExecutionEngine> _logger;
    
    public ExecutionEngine(
        IAgentPoolManager agentPool,
        IToolRegistry toolRegistry,
        ILogger<ExecutionEngine> logger)
    {
        _agentPool = agentPool;
        _toolRegistry = toolRegistry;
        _logger = logger;
    }
    
    public async Task<ExecutionResult> ExecuteAsync(ExecutionRequest request)
    {
        // 1. 从代理池获取可用代理
        var agent = await _agentPool.AcquireAgentAsync();
        
        try
        {
            _logger.LogInformation("Executing request on agent {AgentId}", agent.AgentId);
            
            // 2. 执行任务
            var result = await agent.ExecuteAsync(request);
            
            // 3. 记录日志
            await LogExecutionAsync(request, result);
            
            return result;
        }
        finally
        {
            // 4. 归还代理到池
            await _agentPool.ReleaseAgentAsync(agent);
        }
    }
    
    public async Task<List<ToolMetadata>> GetAvailableToolsAsync()
    {
        return await _toolRegistry.GetAllToolsAsync();
    }
    
    private async Task LogExecutionAsync(ExecutionRequest request, ExecutionResult result)
    {
        // 记录到 AutomationLog 表
        var log = new AutomationLog
        {
            ExecutionMode = request.Mode.ToString(),
            Success = result.Success,
            ExecutionTime = (int)result.ExecutionTime.TotalMilliseconds,
            ErrorMessage = result.ErrorMessage,
            CreatedAt = DateTime.UtcNow
        };
        
        // 保存日志（异步，不阻塞）
        _ = Task.Run(async () =>
        {
            try
            {
                // await _logRepository.InsertAsync(log);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log execution");
            }
        });
        
        await Task.CompletedTask;
    }
}

/// <summary>
/// 代理池管理器（管理代理生命周期）
/// </summary>
public class AgentPoolManager : IAgentPoolManager
{
    private readonly ConcurrentBag<ExecutionAgentBase> _availableAgents = new();
    private readonly ConcurrentDictionary<string, ExecutionAgentBase> _busyAgents = new();
    private readonly ILogger<AgentPoolManager> _logger;
    private readonly int _maxAgents = 10;
    
    public AgentPoolManager(ILogger<AgentPoolManager> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        
        // 初始化代理池
        for (int i = 0; i < 3; i++)
        {
            var agent = ActivatorUtilities.CreateInstance<UniversalAgent>(serviceProvider);
            _availableAgents.Add(agent);
        }
    }
    
    public async Task<ExecutionAgentBase> AcquireAgentAsync()
    {
        if (_availableAgents.TryTake(out var agent))
        {
            _busyAgents.TryAdd(agent.AgentId, agent);
            _logger.LogDebug("Acquired agent {AgentId}", agent.AgentId);
            return agent;
        }
        
        // 池满，等待可用代理
        await Task.Delay(100);
        return await AcquireAgentAsync();
    }
    
    public async Task ReleaseAgentAsync(ExecutionAgentBase agent)
    {
        _busyAgents.TryRemove(agent.AgentId, out _);
        _availableAgents.Add(agent);
        _logger.LogDebug("Released agent {AgentId}", agent.AgentId);
        await Task.CompletedTask;
    }
}
```

#### 4.2.7 安全性考虑

**关键安全措施：**

1. **沙箱隔离**
   - 每次执行创建独立临时目录
   - 执行完成后自动清理
   - 限制文件系统访问范围

2. **超时控制**
   - 默认30秒超时
   - 超时自动终止进程树
   - 防止无限循环和资源耗尽

3. **资源限制**
   ```csharp
   // 使用 Job Objects (Windows) 限制内存
   var job = new JobObject();
   job.SetLimits(new JobObjectLimits
   {
       MaxMemory = _options.MaxMemoryMB * 1024 * 1024,
       MaxCpuRate = 50 // 50% CPU
   });
   job.AssignProcess(process);
   ```

4. **库白名单**
   - 只允许安装白名单中的Python库
   - 定期审查依赖安全性

5. **代码审查**
   ```csharp
   // 检查危险操作
   private bool ContainsDangerousCode(string code)
   {
       var dangerousPatterns = new[]
       {
           "import os", "import subprocess", "exec(", "eval(",
           "__import__", "open(", "file("
       };
       
       return dangerousPatterns.Any(p => code.Contains(p));
   }
   ```

6. **审计日志**
   - 记录所有执行请求
   - 记录用户、时间、脚本哈希
   - 异常情况告警

### 4.3 进度预警与成就系统

#### 4.3.1 预警规则引擎

```csharp
[ScheduledTask("0 * * * *")] // 每小时
public class ProgressCheckTask : IScheduledTask
{
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        // 1. 检查停滞任务
        await CheckStalledTasksAsync();
        
        // 2. 检查逾期任务
        await CheckOverdueTasksAsync();
        
        // 3. 检查目标进度
        await CheckGoalProgressAsync();
    }
}
```

#### 4.3.2 成就系统

```csharp
public class AchievementService
{
    public async Task CheckAchievementsAsync(Guid userId)
    {
        var achievements = await _achievementRepository.GetAllAsync();
        
        foreach (var achievement in achievements)
        {
            if (await IsUnlockedAsync(userId, achievement))
            {
                await UnlockAchievementAsync(userId, achievement);
            }
        }
    }
    
    private async Task<bool> IsUnlockedAsync(Guid userId, Achievement achievement)
    {
        var condition = JsonSerializer.Deserialize<AchievementCondition>(
            achievement.UnlockCondition);
        
        return condition.Type switch
        {
            "goal_completed" => await CheckGoalCountAsync(userId, condition.Threshold),
            "days_streak" => await CheckStreakAsync(userId, condition.Threshold),
            "automation_used" => await CheckAutomationUsageAsync(userId, condition.Threshold),
            _ => false
        };
    }
}
```

### 4.4 目标复盘功能

```csharp
public class GoalReviewService
{
    public async Task<GoalReview> GenerateReviewAsync(Guid goalId)
    {
        var goal = await _goalRepository.GetByIdAsync(goalId);
        var tasks = await _taskRepository.GetByGoalIdAsync(goalId);
        
        var review = new GoalReview
        {
            GoalId = goalId,
            CompletionRate = CalculateCompletionRate(tasks),
            TotalTasks = tasks.Count,
            CompletedTasks = tasks.Count(t => t.Status == TaskStatus.Completed),
            OnTimeTasks = tasks.Count(t => t.CompletedAt <= t.SuggestedEndDate),
            DelayedTasks = tasks.Count(t => t.CompletedAt > t.SuggestedEndDate),
            EstimatedTotalDuration = tasks.Sum(t => t.EstimatedDuration),
            ActualTotalDuration = tasks.Sum(t => t.ActualDuration)
        };
        
        // AI生成建议
        review.AiSuggestions = await GenerateAISuggestionsAsync(goal, tasks);
        
        await _reviewRepository.InsertAsync(review);
        return review;
    }
}
```

---

## 5. 部署与运维

### 5.1 开发环境部署

```bash
# 1. 启动 Aspire AppHost
cd Src/CodeSpirit.AppHost
dotnet run

# 2. 访问 Aspire Dashboard
# http://localhost:15000

# 3. 所有服务自动启动
# - CodeSpirit.Web
# - Pathfinder.Api
# - Pathfinder.AutomationApi
# - SQL Server / MySQL
# - Redis
# - RabbitMQ
```

### 5.2 生产环境部署

#### 5.2.1 Docker化

```dockerfile
# Pathfinder.Api/Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["Pathfinder.Api/Pathfinder.Api.csproj", "Pathfinder.Api/"]
RUN dotnet restore "Pathfinder.Api/Pathfinder.Api.csproj"
COPY . .
WORKDIR "/src/Pathfinder.Api"
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Pathfinder.Api.dll"]
```

#### 5.2.2 Kubernetes部署（可选）

```yaml
# pathfinder-api-deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: pathfinder-api
spec:
  replicas: 3
  selector:
    matchLabels:
      app: pathfinder-api
  template:
    metadata:
      labels:
        app: pathfinder-api
    spec:
      containers:
      - name: pathfinder-api
        image: pathfinder-api:latest
        ports:
        - containerPort: 80
        env:
        - name: ConnectionStrings__PathfinderDb
          valueFrom:
            secretKeyRef:
              name: db-credentials
              key: connection-string
```

### 5.3 监控与告警

#### 5.3.1 集成Prometheus

```csharp
// Program.cs
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics.AddPrometheusExporter();
        metrics.AddMeter("Pathfinder.Api");
    });
```

#### 5.3.2 自定义指标

```csharp
public class PathfinderMetrics
{
    private readonly Meter _meter;
    private readonly Counter<long> _goalCreatedCounter;
    private readonly Counter<long> _taskCompletedCounter;
    private readonly Histogram<double> _aiBreakdownDuration;
    
    public PathfinderMetrics()
    {
        _meter = new Meter("Pathfinder.Api");
        _goalCreatedCounter = _meter.CreateCounter<long>("goals_created_total");
        _taskCompletedCounter = _meter.CreateCounter<long>("tasks_completed_total");
        _aiBreakdownDuration = _meter.CreateHistogram<double>("ai_breakdown_duration_seconds");
    }
    
    public void RecordGoalCreated() => _goalCreatedCounter.Add(1);
    public void RecordTaskCompleted() => _taskCompletedCounter.Add(1);
    public void RecordAIBreakdownDuration(double seconds) => _aiBreakdownDuration.Record(seconds);
}
```

---

## 6. 测试策略

### 6.1 单元测试

```csharp
public class GoalServiceTests
{
    private readonly Mock<IRepository<Goal>> _mockRepository;
    private readonly Mock<ILLMService> _mockLLMService;
    private readonly GoalService _service;
    
    [Fact]
    public async Task CreateGoal_ShouldCallAIParser()
    {
        // Arrange
        var dto = new CreateGoalDto
        {
            Description = "3个月内通过PMP考试"
        };
        
        // Act
        await _service.CreateGoalAsync(dto);
        
        // Assert
        _mockLLMService.Verify(x => x.GenerateAsync(It.IsAny<LLMRequest>()), Times.Once);
    }
}
```

### 6.2 集成测试

```csharp
public class GoalIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    
    [Fact]
    public async Task CreateGoal_ShouldReturnCreatedGoal()
    {
        // Arrange
        var request = new CreateGoalDto
        {
            Description = "学习.NET 9"
        };
        
        // Act
        var response = await _client.PostAsJsonAsync("/api/goals", request);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var goal = await response.Content.ReadFromJsonAsync<GoalDto>();
        goal.Should().NotBeNull();
    }
}
```

---

## 7. 安全性考虑

### 7.1 认证与授权

```csharp
[ApiController]
[Route("api/goals")]
[Authorize] // 使用 CodeSpirit.Authorization
public class GoalsController : ControllerBase
{
    [HttpGet]
    [RequirePermission("Pathfinder.Goals.View")]
    public async Task<IActionResult> GetGoals()
    {
        // 自动应用租户过滤
        // 只能查看自己的目标
    }
}
```

### 7.2 数据加密

```csharp
public class EncryptionService
{
    // 加密OAuth Token
    public string EncryptToken(string token)
    {
        using var aes = Aes.Create();
        // ... 加密逻辑
    }
    
    // 解密OAuth Token
    public string DecryptToken(string encryptedToken)
    {
        // ... 解密逻辑
    }
}
```

### 7.3 Rate Limiting

```csharp
builder.Services.AddRateLimiting(options =>
{
    options.AddFixedWindowLimiter("api", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 100;
    });
});
```

---

## 8. 性能优化

### 8.1 缓存策略

```csharp
public class GoalService
{
    private readonly IDistributedCache _cache;
    
    public async Task<GoalDto> GetGoalByIdAsync(Guid id)
    {
        var cacheKey = $"goal:{id}";
        
        // 尝试从缓存获取
        var cached = await _cache.GetStringAsync(cacheKey);
        if (cached != null)
        {
            return JsonSerializer.Deserialize<GoalDto>(cached);
        }
        
        // 从数据库查询
        var goal = await _repository.GetByIdAsync(id);
        var dto = _mapper.Map<GoalDto>(goal);
        
        // 写入缓存
        await _cache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(dto),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            });
        
        return dto;
    }
}
```

### 8.2 异步处理

```csharp
public class TaskBreakdownService
{
    private readonly IMessagePublisher _messagePublisher;
    
    public async Task<Guid> BreakdownAsync(Guid goalId)
    {
        // 发布消息到队列
        await _messagePublisher.PublishAsync(new BreakdownGoalMessage
        {
            GoalId = goalId,
            RequestedAt = DateTime.UtcNow
        });
        
        return goalId;
    }
}

// 消息处理器
public class BreakdownGoalMessageHandler : IMessageHandler<BreakdownGoalMessage>
{
    public async Task HandleAsync(BreakdownGoalMessage message)
    {
        // 异步处理AI拆解
        var tasks = await _breakdownEngine.BreakdownAsync(message.GoalId);
        await _taskRepository.BulkInsertAsync(tasks);
    }
}
```

---

## 9. 风险与应对

### 9.1 技术风险

| 风险 | 影响 | 应对措施 |
|------|------|----------|
| AI服务不稳定 | 拆解失败 | 实现降级策略，使用规则引擎兜底 |
| 数据库性能 | 响应慢 | 添加索引、读写分离、缓存优化 |
| 消息队列积压 | 任务延迟 | 增加消费者实例、监控队列长度 |

### 9.2 业务风险

| 风险 | 影响 | 应对措施 |
|------|------|----------|
| 用户留存率低 | 产品失败 | 快速迭代、用户反馈、优化体验 |
| AI成本过高 | 盈利困难 | 使用缓存、优化prompt、自训练模型 |

---

## 10. 后续规划

### 10.1 二期功能（6-12个月）

- 复杂数据处理自动化
- 更多第三方工具集成
- 数据看板与分析
- 移动端应用（iOS/Android）

### 10.2 三期功能（12-18个月）

- 团队协作模式
- AI个性化推荐
- API开放平台
- 企业版功能

---

## 附录

### A. 参考文档

- [CodeSpirit框架核心亮点](../CodeSpirit框架核心亮点.md)
- [CodeSpirit.LLM大语言模型组件使用指南](../03-Core-Components/CodeSpirit.LLM大语言模型组件使用指南.md)
- [CodeSpirit.Amis智能界面生成引擎](../02-UI-Generation/CodeSpirit.Amis智能界面生成引擎.md)
- [CodeSpirit.ScheduledTasks组件文档](../03-Core-Components/CodeSpirit.ScheduledTasks组件文档.md)

### B. 开发规范

遵循现有项目规范：
- 每个.cs文件只定义一个顶级类型
- 所有公共成员必须添加XML文档注释
- 使用异步编程（async/await）
- 避免N+1查询
- 只读查询使用AsNoTracking()

### C. Git分支策略

```
main (生产环境)
  └─ develop (开发环境)
       ├─ feature/goal-management (目标管理)
       ├─ feature/task-breakdown (任务拆解)
       ├─ feature/automation (自动化执行)
       └─ feature/reminder (提醒系统)
```

---

**文档版本：** v1.0  
**最后更新：** 2025年11月3日  
**维护团队：** Pathfinder开发团队  
**联系方式：** [项目仓库](https://github.com/your-repo/pathfinder)

