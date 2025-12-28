# Pathfinder 快速参考指南

## 📋 项目基本信息

| 项目 | 信息 |
|------|------|
| **项目名称** | Pathfinder（探路者） |
| **核心价值** | 将模糊目标转化为自动执行的行动路径 |
| **技术栈** | .NET 10 + Aspire + React + AMIS + CodeSpirit |
| **开发周期** | MVP 3个月 + 一期 3个月 |
| **团队规模** | 5-6人 |

---

## 🎯 核心功能清单

### MVP阶段（0-3个月）

✅ **目标管理**
- 自然语言输入目标
- AI解析目标类型和时间约束
- 目标列表与详情页面

✅ **AI任务拆解**
- 智能拆解为5-15个子任务
- 自动识别依赖关系
- 智能分配时间节点

✅ **任务看板**
- 列表/看板/日历多视图
- 任务状态管理
- 进度追踪与统计

✅ **智能提醒**
- 多渠道通知（站内/微信/邮件）
- 多种提醒类型（每日清单/截止/逾期/停滞）
- 用户自定义设置

### 一期扩展（3-6个月）

✅ **自动化执行机器人**
- 执行代理框架
- 工具注册中心
- AI智能工具选择
- 异步任务调度

✅ **进度预警与成就**
- 停滞任务预警
- 目标进度监控
- 成就徽章系统

✅ **目标复盘**
- 自动生成复盘报告
- AI改进建议
- 分享与导出

---

## 🏗️ 技术架构速查

### 服务架构

```
CodeSpirit.AppHost (Aspire编排)
├── Pathfinder.Api              # 目标管理服务
├── Pathfinder.AutomationApi    # 自动化执行服务
├── CodeSpirit.MessagingApi     # 提醒与通知服务
├── CodeSpirit.IdentityApi      # 身份认证服务
└── CodeSpirit.Web              # 前端入口
```

### 数据库实体

| 实体 | 说明 | 关键字段 |
|------|------|---------|
| Goal | 目标 | UserId, Title, Status, Progress |
| Task | 任务 | GoalId, Title, Status, Dependencies |
| AutomationConfig | 自动化配置 | TaskId, ToolType, ConfigParams |
| Agent | 执行代理 | Type, Status, Capabilities |
| Reminder | 提醒记录 | UserId, Type, Channel, Status |
| Achievement | 成就 | Name, UnlockCondition |
| GoalReview | 目标复盘 | GoalId, CompletionRate, AiSuggestions |

### 核心组件集成

| 组件 | 用途 | 主要功能 |
|------|------|---------|
| CodeSpirit.LLM | AI能力 | 目标理解、任务拆解、工具选择 |
| CodeSpirit.Amis | 界面生成 | 自动生成管理界面 |
| CodeSpirit.Authorization | 权限管理 | 租户隔离、权限控制 |
| CodeSpirit.Audit | 审计追踪 | 操作日志记录 |
| CodeSpirit.ScheduledTasks | 定时任务 | 提醒检查、预警检测 |
| CodeSpirit.Messaging | 消息队列 | 异步任务处理 |
| CodeSpirit.Caching | 分布式缓存 | Redis缓存 |

---

## 🚀 快速开始

### 1. 创建项目

```bash
# 创建 Pathfinder.Api 项目
dotnet new webapi -n Pathfinder.Api -o Src/ApiServices/Pathfinder.Api

# 添加到解决方案
dotnet sln add Src/ApiServices/Pathfinder.Api/Pathfinder.Api.csproj

# 添加必要的包
cd Src/ApiServices/Pathfinder.Api
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Tools
```

### 2. 配置 AppHost

```csharp
// CodeSpirit.AppHost/Program.cs
var pathfinderApi = builder.AddProject<Projects.Pathfinder_Api>("pathfinder-api")
    .WithReference(sqlServer)
    .WithReference(redis)
    .WithReference(rabbitmq);

builder.AddProject<Projects.CodeSpirit_Web>("web")
    .WithReference(pathfinderApi);
```

### 3. 配置数据库

```csharp
// Pathfinder.Api/Program.cs
builder.Services.AddDbContext<PathfinderDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("PathfinderDb");
    options.UseSqlServer(connectionString);
});
```

### 4. 创建迁移

```bash
# SQL Server
dotnet ef migrations add InitialCreate --output-dir Migrations/SqlServer

# MySQL
dotnet ef migrations add InitialCreate --output-dir Migrations/MySql

# 应用迁移
dotnet ef database update
```

### 5. 启动服务

```bash
cd Src/CodeSpirit.AppHost
dotnet run

# 访问 Aspire Dashboard
# http://localhost:15000
```

---

## 📊 API速查

### 目标管理 API

```
POST   /api/goals              创建目标
GET    /api/goals              获取目标列表
GET    /api/goals/{id}         获取目标详情
PUT    /api/goals/{id}         更新目标
DELETE /api/goals/{id}         删除目标
POST   /api/goals/{id}/breakdown  AI任务拆解
```

### 任务管理 API

```
GET    /api/tasks              获取任务列表
GET    /api/tasks/{id}         获取任务详情
PATCH  /api/tasks/{id}/status  更新任务状态
PUT    /api/tasks/{id}         更新任务
DELETE /api/tasks/{id}         删除任务
```

### 自动化 API

```
POST   /api/automation/configs        创建自动化配置
GET    /api/automation/configs/{id}   获取配置详情
POST   /api/automation/execute/{id}   手动触发执行
GET    /api/automation/logs/{taskId}  获取执行日志
```

---

## 💡 代码模板

### 1. 创建实体

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
    
    [Required]
    public GoalStatus Status { get; set; } = GoalStatus.Active;
    
    public int Progress { get; set; }
    
    // 多租户
    public Guid? TenantId { get; set; }
}
```

### 2. 创建服务

```csharp
public class GoalService : BaseCRUDService<Goal, GoalDto>
{
    private readonly ILLMService _llmService;
    
    public GoalService(
        IRepository<Goal> repository,
        IMapper mapper,
        ILLMService llmService)
        : base(repository, mapper)
    {
        _llmService = llmService;
    }
    
    public async Task<GoalDto> CreateGoalAsync(CreateGoalDto dto)
    {
        // AI解析
        var parsedData = await ParseGoalWithAIAsync(dto.Description);
        
        // 创建实体
        var goal = new Goal
        {
            UserId = _currentUser.Id,
            Title = parsedData.Title,
            Description = dto.Description,
            Category = parsedData.Category
        };
        
        await _repository.InsertAsync(goal);
        return _mapper.Map<GoalDto>(goal);
    }
}
```

### 3. 目标可行性评估（重要！）

**核心原则：仅当目标明确、可执行时，才支持AI拆解**

```csharp
/// <summary>
/// 目标可行性评估器
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
}}";

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
```

### 4. 创建控制器

```csharp
[ApiController]
[Route("api/goals")]
[Authorize]
public class GoalsController : AmisControllerBase<Goal, GoalDto>
{
    public GoalsController(
        IGoalService service,
        IMapper mapper)
        : base(service, mapper)
    {
    }
    
    [HttpPost("{id}/breakdown")]
    [RequirePermission("Goals.Breakdown")]
    public async Task<IActionResult> BreakdownGoal(Guid id)
    {
        var tasks = await _service.BreakdownGoalAsync(id);
        return Ok(tasks);
    }
}
```

### 4. AI调用

```csharp
public async Task<List<TaskDto>> BreakdownGoalAsync(Goal goal)
{
    var prompt = $@"
你是一个项目管理专家，擅长将目标拆解为可执行的任务。

用户目标：{goal.Description}
目标类型：{goal.Category}
完成时间：{goal.TargetDate}

请拆解为5-15个子任务，输出JSON格式...
";

    var response = await _llmService.GenerateAsync(new LLMRequest
    {
        Prompt = prompt,
        Temperature = 0.7,
        MaxTokens = 2000
    });
    
    return ParseTasksFromResponse(response);
}
```

### 5. 前端澄清对话框

**处理目标不明确的情况：**

```typescript
// 用户创建目标
const handleCreateGoal = async (description: string) => {
  // 1. 创建目标
  const goal = await api.createGoal({ description });
  
  // 2. 触发拆解
  const breakdownResult = await api.breakdownGoal(goal.id);
  
  // 3. 判断结果
  if (breakdownResult.requiresClarification) {
    // 目标不够明确，显示澄清对话框
    showClarificationDialog({
      title: "需要补充目标信息",
      message: "您的目标需要更明确，请回答以下问题：",
      questions: breakdownResult.clarificationQuestions,
      suggestions: breakdownResult.suggestions,
      onSubmit: async (clarifiedDescription) => {
        // 用户补充说明后，更新目标并重新拆解
        await api.updateGoal(goal.id, { description: clarifiedDescription });
        await handleBreakdown(goal.id);
      },
      onCancel: () => {
        // 用户取消，保持目标但不拆解
        navigateToGoalList();
      }
    });
  } else if (breakdownResult.success) {
    // 目标明确，拆解成功
    navigateToBreakdownResult(breakdownResult.tasks);
  }
};
```

**AMIS澄清对话框配置：**

```json
{
  "type": "dialog",
  "title": "需要补充目标信息",
  "body": {
    "type": "form",
    "api": "PUT /api/goals/${goalId}",
    "body": [
      {
        "type": "static",
        "label": "当前目标",
        "value": "${originalDescription}"
      },
      {
        "type": "alert",
        "level": "warning",
        "body": "您的目标需要更明确，请回答以下问题："
      },
      {
        "type": "list",
        "source": "${clarificationQuestions}",
        "listItem": {
          "body": "- ${item}"
        }
      },
      {
        "type": "divider"
      },
      {
        "type": "alert",
        "level": "info",
        "body": "建议："
      },
      {
        "type": "list",
        "source": "${suggestions}",
        "listItem": {
          "body": "💡 ${item}"
        }
      },
      {
        "type": "textarea",
        "name": "description",
        "label": "请完善您的目标",
        "required": true,
        "minRows": 4,
        "placeholder": "请根据上述问题和建议，重新描述您的目标"
      }
    ]
  }
}
```

### 6. Python脚本执行器（代理服务核心） ⭐

**基于.NET Process的Python执行器：**

```csharp
public class PythonExecutor : IPythonExecutor
{
    private readonly ISandboxManager _sandboxManager;
    private readonly PythonExecutorOptions _options;
    
    public async Task<ExecutionResult> ExecuteAsync(
        string pythonCode, 
        Dictionary<string, object> parameters)
    {
        var sandbox = await _sandboxManager.CreateSandboxAsync();
        
        try
        {
            // 准备脚本（注入参数）
            var scriptPath = await PrepareScriptFileAsync(sandbox, pythonCode, parameters);
            
            // 配置Process
            var startInfo = new ProcessStartInfo
            {
                FileName = "python",
                Arguments = $"\"{scriptPath}\"",
                WorkingDirectory = sandbox.WorkingDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            
            using var process = new Process { StartInfo = startInfo };
            process.Start();
            
            // 超时控制（30秒）
            if (!process.WaitForExit(_options.ExecutionTimeoutSeconds * 1000))
            {
                process.Kill(entireProcessTree: true);
                return ExecutionResult.Failure("Timeout");
            }
            
            var output = await process.StandardOutput.ReadToEndAsync();
            return ParseExecutionOutput(output);
        }
        finally
        {
            await _sandboxManager.CleanupSandboxAsync(sandbox);
        }
    }
}
```

**用户Python脚本示例：**

```python
import requests

# 获取参数（由.NET注入）
url = get_parameter('url')

# 执行任务
response = requests.get(url)

# 输出结果
output_result({
    'status': response.status_code,
    'title': response.text[:100]
})
```

**API调用示例：**

```bash
POST /api/automation/execute/python
{
  "code": "import requests\nurl = get_parameter('url')\nresponse = requests.get(url)\noutput_result({'status': response.status_code})",
  "parameters": {
    "url": "https://example.com"
  },
  "timeoutSeconds": 30
}
```

### 7. 定时任务

```csharp
[ScheduledTask("0 * * * *")] // 每小时执行
public class ReminderCheckTask : IScheduledTask
{
    private readonly IReminderService _reminderService;
    
    public ReminderCheckTask(IReminderService reminderService)
    {
        _reminderService = reminderService;
    }
    
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await _reminderService.CheckAndSendRemindersAsync();
    }
}
```

### 8. 执行工具

```csharp
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
        
        var element = await page.QuerySelectorAsync(selector);
        var text = await element.InnerTextAsync();
        
        return ToolResult.Success(new { content = text });
    }
}
```

---

## 🔧 常用命令

### 数据库操作

```bash
# 添加迁移
dotnet ef migrations add MigrationName --context PathfinderDbContext

# 应用迁移
dotnet ef database update --context PathfinderDbContext

# 回滚迁移
dotnet ef database update PreviousMigration --context PathfinderDbContext

# 生成SQL脚本
dotnet ef migrations script --context PathfinderDbContext
```

### 运行与调试

```bash
# 启动 Aspire
cd Src/CodeSpirit.AppHost
dotnet run

# 启动单个服务（调试用）
cd Src/ApiServices/Pathfinder.Api
dotnet run

# 运行测试
dotnet test

# 代码格式化
dotnet format
```

### Docker操作

```bash
# 构建镜像
docker build -t pathfinder-api:latest .

# 运行容器
docker run -d -p 8080:80 pathfinder-api:latest

# 查看日志
docker logs -f <container-id>
```

---

## 📈 关键指标

### MVP阶段

| 指标 | 目标值 |
|------|--------|
| 7日留存率 | > 30% |
| 用户平均创建目标数 | > 2 |
| NPS | > 40 |
| 响应时间 | < 2秒 |

### 一期扩展

| 指标 | 目标值 |
|------|--------|
| 月留存率 | > 50% |
| 自动化功能使用率 | > 40% |
| 付费用户 | > 200 |
| 免费到付费转化率 | > 5% |

---

## ⚠️ 常见问题

### Q: 如何调试AI拆解功能？

```csharp
// 添加详细日志
_logger.LogInformation("AI拆解请求: {@Goal}", goal);
var response = await _llmService.GenerateAsync(prompt);
_logger.LogInformation("AI拆解响应: {Response}", response);
```

### Q: 如何处理多租户隔离？

所有实体继承 `ITenantEntity` 接口，框架自动应用租户过滤：

```csharp
public class Goal : AuditedEntity, ITenantEntity
{
    public Guid? TenantId { get; set; }
}
```

### Q: 如何优化AI调用成本？

1. 使用缓存减少重复调用
2. 优化 Prompt，减少 Token 使用
3. 对常见场景使用规则引擎兜底

```csharp
// 缓存AI响应
var cacheKey = $"breakdown:{goal.Id}";
var cached = await _cache.GetStringAsync(cacheKey);
if (cached != null)
{
    return JsonSerializer.Deserialize<List<TaskDto>>(cached);
}
```

### Q: 如何扩展新的自动化工具？

实现 `IExecutionTool` 接口并注册：

```csharp
// 1. 实现工具
public class MyTool : IExecutionTool { ... }

// 2. 注册工具
services.AddSingleton<IExecutionTool, MyTool>();
```

---

## 📚 相关文档

- [Pathfinder实施方案](./Pathfinder实施方案.md)
- [技术路线图](./技术路线图.md)
- [CodeSpirit框架核心亮点](../CodeSpirit框架核心亮点.md)
- [CodeSpirit.LLM使用指南](../03-Core-Components/CodeSpirit.LLM大语言模型组件使用指南.md)

---

**文档版本：** v1.0  
**最后更新：** 2025年11月3日  
**维护团队：** Pathfinder开发团队

