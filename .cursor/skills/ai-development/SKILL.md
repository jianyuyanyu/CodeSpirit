---
name: ai-development
description: 指导在 CodeSpirit 项目中集成 AI 功能的完整开发流程。包括 AI 表单填充、AI 长任务处理、LLM 集成和提示词工程。当用户需要添加 AI 功能、集成 LLM、或开发 AI 驱动的业务功能时使用。
---

# AI 功能开发 Skill

## 快速开始

CodeSpirit 项目支持三种 AI 功能模式：

1. **AI 表单填充**：零代码方案，自动生成 AI 填充端点
2. **AI 长任务处理**：异步任务，支持进度跟踪
3. **LLM 结构化任务**：使用 LLMAssistant 进行内容分析和生成

---

## 模式选择决策树

```
需要AI功能？
├── 单字段触发填充？
│   └── 是 → AI 表单填充（字段触发模式）
│
├── 用户自定义需求填充整个表单？
│   └── 是 → AI 表单填充（全局填充模式）
│
├── 批量生成/长时间任务？
│   └── 是 → AI 长任务处理
│
└── 内容分析/审核/生成？
    └── 是 → LLM 结构化任务
```

---

## 场景 1：AI 表单填充（零代码方案）

### 适用场景

- 用户输入触发字段后，AI 智能填充其他相关字段
- 用户在表单顶部输入自定义需求，AI 一次性填充整个表单

### 工作流程

#### 步骤 1：注册 AI 服务

在 `Program.cs` 或 API 配置类中：

```csharp
// 注册 LLM 服务（必需）
builder.Services.AddLLMServices();

// 注册 AI 表单填充自动端点（推荐）
builder.Services.AddAiFormFillEndpoints();

var app = builder.Build();

// 启用 AI 填充中间件
app.UseAiFormFillEndpoints();
```

#### 步骤 2：配置 DTO

**字段触发模式**：
```csharp
[AiFormFill(TriggerField = nameof(Topic))]
public class CreateQuestionDto
{
    [Required]
    [DisplayName("主题")]
    public string Topic { get; set; } = string.Empty;
    
    [DisplayName("题目内容")]
    [AiFieldFill(Priority = 1, CustomDescription = "根据主题生成的题目内容")]
    public string? Content { get; set; }
    
    [DisplayName("选项A")]
    [AiFieldFill(Priority = 2)]
    public string? OptionA { get; set; }
}
```

**全局填充模式**：
```csharp
[AiFormFill(GlobalFillPrompt = "描述您想创建的内容")]
public class CreateContentDto
{
    [DisplayName("标题")]
    public string? Title { get; set; }
    
    [DisplayName("内容")]
    public string? Content { get; set; }
}
```

#### 步骤 3：测试 AI 填充

系统自动生成 `POST /api/{controller}/ai-fill` 端点，无需编写控制器代码。

**请求示例**：
```json
POST /api/questions/ai-fill
{
  "topic": "人工智能"
}
```

**响应示例**：
```json
{
  "topic": "人工智能",
  "content": "人工智能是计算机科学的一个分支...",
  "optionA": "..."
}
```

### AiFormFillAttribute 参数

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `TriggerField` | string | "" | 触发字段名称，为空时启用全局模式 |
| `IgnoreFields` | string[] | [] | 需要忽略的字段列表 |
| `CustomPromptTemplate` | string | "" | 自定义提示词模板 |
| `ApiEndpoint` | string | "ai-fill" | API端点路径 |
| `EnableCache` | bool | true | 是否启用缓存 |
| `Temperature` | double | 0.1 | 温度参数，控制随机性 |

### 自定义提示词模板

```csharp
[AiFormFill(
    TriggerField = nameof(Topic),
    CustomPromptTemplate = "基于主题 '{Topic}' 生成相关内容，要求专业准确")]
public class CustomPromptDto { }
```

---

## 场景 2：AI 长任务处理

### 适用场景

- 批量生成题目、问卷等耗时较长的任务
- 需要进度跟踪和日志输出的任务

### 工作流程

#### 步骤 1：创建任务请求 DTO

```csharp
public class GenerateQuestionsRequest
{
    public string Topic { get; set; } = string.Empty;
    public int Count { get; set; }
    public string Difficulty { get; set; } = "Medium";
}
```

#### 步骤 2：创建任务状态响应类

```csharp
public class AiTaskStatus
{
    public string Status { get; set; }       // "pending", "processing", "completed", "failed"
    public int Progress { get; set; }        // 0-100
    public List<string> Logs { get; set; }   // 日志列表
    public object? Result { get; set; }      // 任务结果
    public string? ErrorMessage { get; set; } // 错误消息
}
```

#### 步骤 3：实现异步任务 API

```csharp
[HttpPost("ai/generate-async")]
[HeaderOperation("AI智能生成", "aiForm", 
    Icon = "fa-solid fa-magic",
    StatusApi = "/exam/api/Questions/ai/task-status",  // 状态查询 API（必需）
    PollingInterval = 2000,                             // 轮询间隔（毫秒）
    MaxPollingTime = 300000,                            // 最大轮询时间（5分钟）
    FormTitle = "生成配置",
    StepsTitle = "AI生成进度",
    LogTitle = "生成日志",
    ResultTitle = "生成结果")]
[DisplayName("AI智能生成题目")]
public async Task<ActionResult<ApiResponse<string>>> GenerateQuestionsAsync(
    [FromBody] GenerateQuestionsRequest request)
{
    var taskId = await _aiGeneratorService.GenerateAsync(request);
    return SuccessResponse(taskId);
}
```

#### 步骤 4：实现状态查询 API

```csharp
[HttpGet("ai/task-status")]
[DisplayName("查询任务状态")]
public async Task<ActionResult<ApiResponse<AiTaskStatus>>> GetTaskStatus(
    [FromQuery] string taskId)
{
    var status = await _aiGeneratorService.GetTaskStatusAsync(taskId);
    return SuccessResponse(status);
}
```

#### 步骤 5：实现服务层逻辑

```csharp
public class AiGeneratorService : IScopedDependency
{
    private readonly LLMAssistant _llmAssistant;
    private readonly IMemoryCache _cache;
    
    public async Task<string> GenerateAsync(GenerateQuestionsRequest request)
    {
        var taskId = Guid.NewGuid().ToString();
        
        // 初始化任务状态
        var status = new AiTaskStatus
        {
            Status = "pending",
            Progress = 0,
            Logs = new List<string>()
        };
        _cache.Set($"ai_task_{taskId}", status);
        
        // 后台执行任务
        _ = Task.Run(async () => await ProcessTaskAsync(taskId, request));
        
        return taskId;
    }
    
    private async Task ProcessTaskAsync(string taskId, GenerateQuestionsRequest request)
    {
        var status = _cache.Get<AiTaskStatus>($"ai_task_{taskId}");
        status.Status = "processing";
        status.Logs.Add("开始生成题目...");
        
        try
        {
            for (int i = 0; i < request.Count; i++)
            {
                status.Progress = (int)((i + 1) * 100.0 / request.Count);
                status.Logs.Add($"正在生成第 {i + 1} 题...");
                
                // 调用 LLM 生成题目
                var question = await _llmAssistant.GenerateContentAsync(
                    $"生成一道关于{request.Topic}的{request.Difficulty}难度题目");
                
                // 保存题目
                // ...
            }
            
            status.Status = "completed";
            status.Progress = 100;
            status.Logs.Add("生成完成！");
        }
        catch (Exception ex)
        {
            status.Status = "failed";
            status.ErrorMessage = ex.Message;
            status.Logs.Add($"生成失败: {ex.Message}");
        }
    }
    
    public async Task<AiTaskStatus> GetTaskStatusAsync(string taskId)
    {
        return _cache.Get<AiTaskStatus>($"ai_task_{taskId}") 
            ?? new AiTaskStatus { Status = "not_found" };
    }
}
```

---

## 场景 3：LLM 结构化任务

### 适用场景

- 内容审核和分析
- 结构化数据提取
- 批量处理任务

### 工作流程

#### 步骤 1：注入 LLMAssistant

```csharp
public class AuditService : IScopedDependency
{
    private readonly LLMAssistant _llmAssistant;
    
    public AuditService(LLMAssistant llmAssistant)
    {
        _llmAssistant = llmAssistant;
    }
}
```

#### 步骤 2：创建提示词模板

```csharp
public static class PromptTemplates
{
    public const string QuestionAudit = @"你是一个专业的题目审核专家。

任务：审核以下题目是否符合要求。

题目内容：{Content}
选项：{Options}

要求：
1. 题目内容清晰准确
2. 选项设计合理
3. 只有一个正确答案

输出格式（JSON）：
{{
  ""isValid"": true/false,
  ""issues"": [""问题1"", ""问题2""],
  ""suggestions"": [""建议1"", ""建议2""]
}}";
}
```

#### 步骤 3：定义响应 DTO

```csharp
public class AuditResult
{
    public bool IsValid { get; set; }
    public List<string> Issues { get; set; } = new();
    public List<string> Suggestions { get; set; } = new();
}
```

#### 步骤 4：调用 LLM 处理

```csharp
public async Task<AuditResult> AuditQuestionAsync(QuestionDto question)
{
    var prompt = PromptTemplates.QuestionAudit
        .Replace("{Content}", question.Content)
        .Replace("{Options}", string.Join(", ", question.Options));
    
    var result = await _llmAssistant.ProcessStructuredTaskWithTemplateAsync<AuditResult>(
        "question_audit",
        new { question },
        new StructuredTaskOptions 
        { 
            EnableRetry = true, 
            MaxRetries = 2 
        });
    
    if (result.IsSuccess)
    {
        return result.Result!;
    }
    
    throw new BusinessException($"审核失败: {string.Join("; ", result.Errors)}");
}
```

#### 步骤 5：批量处理

```csharp
public async Task<List<AuditResult>> BatchAuditAsync(List<QuestionDto> questions)
{
    var batchResult = await _llmAssistant.ProcessBatchStructuredTaskAsync<QuestionDto, AuditResult>(
        questions,
        batch => BuildBatchPrompt(batch),
        new BatchProcessingOptions 
        { 
            BatchSize = 10,
            MaxRetries = 2,
            DelayBetweenBatches = TimeSpan.FromSeconds(1),
            ContinueOnFailure = true
        });
    
    return batchResult.SuccessResults
        .Where(r => r.IsSuccess)
        .Select(r => r.Result!)
        .ToList();
}
```

---

## 提示词工程最佳实践

### 1. 角色设定清晰

```markdown
你是一个专业的{角色}。
```

### 2. 任务描述具体

```markdown
任务：根据以下信息生成一道高质量的题目。

主题：{Topic}
题型：{QuestionType}
难度：{Difficulty}
```

### 3. 输出格式明确

```markdown
输出格式（JSON）：
{
  "Content": "string, 必填。题目内容",
  "OptionA": "string, 必填。选项A内容",
  "OptionB": "string, 必填。选项B内容"
}
```

### 4. 约束条件清晰

```markdown
要求：
1. 题目内容不超过 2000 字符
2. 选项设计合理，避免明显错误
3. 只有一个正确答案
```

### 5. 提供示例（复杂场景）

```markdown
示例：
输入：主题="人工智能"，难度="Medium"
输出：{
  "Content": "人工智能的核心技术包括哪些？",
  "OptionA": "机器学习、深度学习、自然语言处理",
  ...
}
```

### 提示词模板

参见 [prompt-templates/](prompt-templates/) 目录：
- `question-generator.txt`：题目生成提示词
- `content-audit.txt`：内容审核提示词
- `survey-generator.txt`：问卷生成提示词

---

## 错误处理

### 常见错误类型

| 错误类型 | 原因 | 处理方式 |
|---------|------|---------|
| 401 Unauthorized | API 密钥无效 | 检查配置，更新密钥 |
| 400 Bad Request | 模型名称错误 | 验证模型名称是否正确 |
| 429 Too Many Requests | 请求限流 | 添加重试和延迟 |
| Timeout | 请求超时 | 增加超时时间，拆分请求 |
| JSON解析失败 | 响应格式不正确 | 使用 ILLMJsonProcessor 自动修复 |

### 自动错误处理

系统自动处理：
- ✅ **流式模式检测**：自动检测"只支持流式模式"的模型并重试
- ✅ **JSON 自动修复**：截断、括号不匹配、引号错误等
- ✅ **重试机制**：支持配置重试次数和延迟

### 错误处理示例

```csharp
public async Task<T> SafeGenerateAsync<T>(string prompt) where T : class
{
    try
    {
        var result = await _llmAssistant.ProcessStructuredTaskWithTemplateAsync<T>(
            "template", 
            new { prompt },
            new StructuredTaskOptions { EnableRetry = true, MaxRetries = 3 });
        
        if (result.IsSuccess)
        {
            return result.Result!;
        }
        
        _logger.LogError("AI生成失败: {Errors}", string.Join("; ", result.Errors));
        throw new BusinessException("AI生成失败，请稍后重试");
    }
    catch (HttpRequestException ex)
    {
        _logger.LogError(ex, "LLM API请求失败");
        throw new BusinessException("AI服务暂时不可用");
    }
    catch (TaskCanceledException ex)
    {
        _logger.LogError(ex, "LLM请求超时");
        throw new BusinessException("AI响应超时，请缩短输入或稍后重试");
    }
}
```

---

## 性能优化

### 缓存策略

```csharp
[AiFormFill(
    TriggerField = nameof(Topic),
    EnableCache = true,               // 启用缓存
    CacheExpirationMinutes = 30       // 30分钟过期
)]
```

**缓存键规则**：包含输入内容的哈希值，相同输入直接返回缓存结果。

### 批量处理优化

```csharp
var options = new BatchProcessingOptions
{
    BatchSize = 10,                              // 每批10条
    DelayBetweenBatches = TimeSpan.FromSeconds(1), // 批次间延迟
    MaxRetries = 2,                              // 最大重试次数
    ContinueOnFailure = true                     // 失败时继续处理
};
```

### Token 控制

- 合理设置 `MaxTokens`，避免过度消耗
- 使用缓存减少重复请求
- 长文本分段处理
- 定期监控 Token 使用量

---

## 安全最佳实践

### API 密钥管理

```csharp
// ✅ 使用 Aspire 统一配置（推荐）
var llmApiKey = builder.AddParameter("llm-ApiKey", secret: true);

// ✅ 使用环境变量
.WithEnvironment("LLM__ApiKey", llmApiKey)

// ❌ 禁止：硬编码密钥
var apiKey = "sk-xxxxxxxx";  // 绝对禁止！
```

### 敏感数据保护

```csharp
// 排除敏感字段
[AiFieldFill(Enabled = false)]
public string Password { get; set; }

// 使用 IgnoreFields
[AiFormFill(
    TriggerField = nameof(Name),
    IgnoreFields = new[] { "Password", "IdCard", "BankAccount" }
)]
```

### 权限控制

```csharp
[HttpPost("ai-fill")]
[Authorize]
[RequirePermission("Question.AiFill")]
public async Task<ActionResult> AiFill([FromBody] CreateQuestionDto dto)
{
    // AI 填充需要特定权限
}
```

---

## 检查清单

开发 AI 功能时：

- [ ] 注册 LLM 服务（`AddLLMServices`）
- [ ] 配置 DTO 特性（`AiFormFill`、`AiFieldFill`）
- [ ] 测试 AI 填充功能
- [ ] 处理错误和异常
- [ ] 配置缓存（如适用）
- [ ] 添加权限控制（如适用）
- [ ] 排除敏感字段
- [ ] 监控 Token 使用量

---

## 相关资源

- [AI 开发规范](../../rules/ai-development.mdc)
- [提示词模板](prompt-templates/)
- [使用示例](examples.md)
