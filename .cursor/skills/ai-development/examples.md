# AI 功能开发示例

## 示例 1：AI 表单填充 - 题目创建

### DTO 配置

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
    
    [DisplayName("选项B")]
    [AiFieldFill(Priority = 2)]
    public string? OptionB { get; set; }
}
```

### 使用效果

用户输入"人工智能"作为主题，AI 自动填充：
- 题目内容：关于人工智能的基础知识题目
- 选项A、B、C、D：相关的选项内容

---

## 示例 2：AI 长任务处理 - 批量生成题目

### 控制器实现

```csharp
[HttpPost("ai/generate-async")]
[HeaderOperation("AI智能生成", "aiForm", 
    Icon = "fa-solid fa-magic",
    StatusApi = "/exam/api/Questions/ai/task-status",
    PollingInterval = 2000,
    MaxPollingTime = 300000)]
public async Task<ActionResult<ApiResponse<string>>> GenerateQuestionsAsync(
    [FromBody] GenerateQuestionsRequest request)
{
    var taskId = await _aiGeneratorService.GenerateAsync(request);
    return SuccessResponse(taskId);
}

[HttpGet("ai/task-status")]
public async Task<ActionResult<ApiResponse<AiTaskStatus>>> GetTaskStatus(
    [FromQuery] string taskId)
{
    var status = await _aiGeneratorService.GetTaskStatusAsync(taskId);
    return SuccessResponse(status);
}
```

### 服务实现

```csharp
public async Task<string> GenerateAsync(GenerateQuestionsRequest request)
{
    var taskId = Guid.NewGuid().ToString();
    var status = new AiTaskStatus
    {
        Status = "pending",
        Progress = 0,
        Logs = new List<string>()
    };
    _cache.Set($"ai_task_{taskId}", status);
    
    _ = Task.Run(async () => await ProcessTaskAsync(taskId, request));
    return taskId;
}

private async Task ProcessTaskAsync(string taskId, GenerateQuestionsRequest request)
{
    var status = _cache.Get<AiTaskStatus>($"ai_task_{taskId}");
    status.Status = "processing";
    
    for (int i = 0; i < request.Count; i++)
    {
        status.Progress = (int)((i + 1) * 100.0 / request.Count);
        status.Logs.Add($"正在生成第 {i + 1} 题...");
        
        var question = await _llmAssistant.GenerateContentAsync(
            $"生成一道关于{request.Topic}的{request.Difficulty}难度题目");
        
        // 保存题目...
    }
    
    status.Status = "completed";
    status.Progress = 100;
}
```

---

## 示例 3：LLM 结构化任务 - 题目审核

### 服务实现

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

### 响应 DTO

```csharp
public class AuditResult
{
    public bool IsValid { get; set; }
    public List<string> Issues { get; set; } = new();
    public List<string> Suggestions { get; set; } = new();
    public string RiskLevel { get; set; } = "low";
    public int? AuditScore { get; set; }
}
```

---

## 示例 4：批量处理 - 批量审核题目

### 服务实现

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

private string BuildBatchPrompt(List<QuestionDto> batch)
{
    var questionsJson = JsonConvert.SerializeObject(batch);
    return $"批量审核以下题目：\n{questionsJson}\n\n请返回审核结果数组。";
}
```

---

## 常见问题

### Q: AI 填充返回的 JSON 格式不正确怎么办？

A: 系统会自动尝试修复 JSON 格式错误。如果修复失败，检查提示词模板中的 JSON 结构说明是否清晰。

### Q: 如何提高 AI 生成内容的质量？

A: 
1. 提供更详细的上下文信息
2. 在提示词中明确输出格式和要求
3. 使用 `CustomDescription` 为字段提供额外说明
4. 调整 `Temperature` 参数（较低的值更稳定，较高的值更有创造性）

### Q: 长任务处理时如何避免超时？

A:
1. 合理设置 `MaxPollingTime`（默认 5 分钟）
2. 在服务层实现任务分片处理
3. 使用后台任务队列（如 Hangfire）处理长时间任务

### Q: 如何控制 Token 消耗？

A:
1. 启用缓存（`EnableCache = true`）
2. 合理设置 `MaxTokens` 参数
3. 使用批量处理时控制批次大小
4. 定期监控 Token 使用量
