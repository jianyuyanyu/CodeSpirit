# CodeSpirit LLM审计组件使用指南

## 概述

本指南详细说明如何在CodeSpirit项目中使用LLM审计功能，包括配置、服务注册、审计上下文设置和查询API使用。

---

## 1. 服务注册

### 1.1 在API服务中注册LLM审计服务

在你的API服务（如`CodeSpirit.ExamApi`）的`Program.cs`中添加：

```csharp
using CodeSpirit.Audit.Extensions;

var builder = WebApplication.CreateBuilder(args);

// ... 其他服务注册 ...

// 注册审计服务（必须）
builder.Services.AddAuditServices(builder.Configuration);

// 注册LLM服务（必须）
builder.Services.AddLLMServices();

// 注册LLM审计服务
builder.Services.AddLLMAuditServices(builder.Configuration);

var app = builder.Build();

// ... 应用配置 ...

app.Run();
```

---

## 2. 配置

### 2.1 基础配置

在`appsettings.json`中添加LLM审计配置：

```json
{
  "Audit": {
    "Enabled": true,
    "StorageProvider": "Elasticsearch",
    "Elasticsearch": {
      "Uri": "http://localhost:9200",
      "Username": "",
      "Password": ""
    },
    "RabbitMQ": {
      "HostName": "localhost",
      "Port": 5672,
      "UserName": "guest",
      "Password": "guest"
    },
    "LLMAudit": {
      "Enabled": true,
      "LogPrompts": true,
      "LogResponses": true,
      "LogProcessedData": false,
      "MaxPromptLength": 10000,
      "MaxResponseLength": 50000,
      "RabbitMQ": {
        "ExchangeName": "llm.audit.exchange",
        "QueueName": "llm.audit.queue",
        "RoutingKey": "llm.audit.log"
      },
      "Elasticsearch": {
        "IndexName": "llm_audit_logs",
        "IndexPrefix": "codespirit",
        "NumberOfShards": 3,
        "NumberOfReplicas": 1
      },
      "SensitiveData": {
        "Enabled": true,
        "SensitiveFieldPatterns": [
          "password",
          "pwd",
          "secret",
          "token",
          "apiKey"
        ],
        "MaskCharacter": "*",
        "KeepFirstChars": 0,
        "KeepLastChars": 0
      },
      "CostCalculation": {
        "Enabled": true,
        "ModelPricing": {
          "gpt-4": {
            "InputPer1K": 0.03,
            "OutputPer1K": 0.06
          },
          "gpt-3.5-turbo": {
            "InputPer1K": 0.0015,
            "OutputPer1K": 0.002
          },
          "qwen-plus": {
            "InputPer1K": 0.004,
            "OutputPer1K": 0.012
          }
        }
      },
      "ScenarioMapping": {
        "QuestionGeneration": "题目生成",
        "QuestionAudit": "题目审核",
        "QuestionCorrection": "题目校正",
        "ContentGeneration": "内容生成"
      }
    }
  }
}
```

**注意**：
- `LLMAudit.StorageProvider` 已被移除，LLM审计将自动跟随 `Audit.StorageProvider` 的配置
- 支持的存储提供者：`Elasticsearch`、`GreptimeDB`

---

## 3. 使用可审计的LLM助手

### 3.1 基本用法

在你的服务中注入`AuditableLLMAssistant`：

```csharp
using CodeSpirit.Audit.LLM;

public class QuestionGeneratorService
{
    private readonly AuditableLLMAssistant _auditableLLM;
    private readonly ILogger<QuestionGeneratorService> _logger;
    
    public QuestionGeneratorService(
        AuditableLLMAssistant auditableLLM,
        ILogger<QuestionGeneratorService> logger)
    {
        _auditableLLM = auditableLLM;
        _logger = logger;
    }
    
    public async Task<string> GenerateQuestionAsync(string topic)
    {
        // 简单调用，自动审计
        var prompt = $"请生成一道关于{topic}的选择题";
        return await _auditableLLM.GenerateContentAsync(prompt);
    }
}
```

### 3.2 配置审计上下文

为了更好地追踪和分析LLM使用，建议在调用前设置审计上下文：

```csharp
public async Task<string> GenerateQuestionAsync(string topic, string questionId)
{
    var prompt = $"请生成一道关于{topic}的选择题";
    
    // 配置审计上下文
    var result = await _auditableLLM
        .WithBusinessScenario("QuestionGeneration")      // 业务场景
        .WithInteractionType("Generation")               // 交互类型
        .WithBusinessEntity("Question", questionId, 1)   // 业务实体
        .WithMetadata("topic", topic)                    // 附加元数据
        .GenerateContentAsync(prompt);
    
    // 清除上下文（可选，如果下次调用需要不同的上下文）
    _auditableLLM.ResetAuditContext();
    
    return result;
}
```

### 3.3 批量处理场景

在批量生成或审核题目时：

```csharp
public async Task<List<string>> GenerateBatchQuestionsAsync(
    List<string> topics, 
    string batchId)
{
    var results = new List<string>();
    
    for (int i = 0; i < topics.Count; i++)
    {
        var topic = topics[i];
        var prompt = $"请生成一道关于{topic}的选择题";
        
        // 为批次中的每个请求设置序号
        var result = await _auditableLLM
            .WithBusinessScenario("QuestionGeneration")
            .WithInteractionType("BatchGeneration")
            .WithBatch(batchId, i + 1)                   // 批次ID和序号
            .WithBusinessEntity("Question", $"batch_{i}", topics.Count)
            .GenerateContentAsync(prompt);
        
        results.Add(result);
    }
    
    return results;
}
```

### 3.4 重试和修正场景

当LLM返回的结果需要修正时，可以关联到父审计记录：

```csharp
public async Task<string> GenerateAndCorrectQuestionAsync(string topic)
{
    string? parentAuditId = null;
    
    try
    {
        // 首次生成
        var prompt = $"请生成一道关于{topic}的选择题，要求JSON格式";
        var result = await _auditableLLM
            .WithBusinessScenario("QuestionGeneration")
            .WithInteractionType("Generation")
            .GenerateContentAsync(prompt);
        
        // 保存审计ID（TODO: 需要从审计服务获取最后一次审计ID）
        // parentAuditId = ...
        
        return result;
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "生成题目失败，尝试修正");
        
        // 修正请求
        var correctionPrompt = $"上次生成失败，错误: {ex.Message}。请重新生成";
        var correctedResult = await _auditableLLM
            .WithBusinessScenario("QuestionGeneration")
            .WithInteractionType("Correction")
            .WithParentAuditId(parentAuditId ?? "unknown")  // 关联到父审计
            .GenerateContentAsync(correctionPrompt);
        
        return correctedResult;
    }
}
```

### 3.5 结构化任务处理

使用结构化任务处理并自动审计：

```csharp
public class QuestionDto
{
    public string Question { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new();
    public int CorrectAnswer { get; set; }
}

public async Task<QuestionDto?> GenerateStructuredQuestionAsync(string topic)
{
    var prompt = $@"请生成一道关于{topic}的选择题，返回JSON格式：
{{
    ""question"": ""题目内容"",
    ""options"": [""选项A"", ""选项B"", ""选项C"", ""选项D""],
    ""correctAnswer"": 0
}}";
    
    var result = await _auditableLLM
        .WithBusinessScenario("QuestionGeneration")
        .WithInteractionType("StructuredGeneration")
        .ProcessStructuredTaskAsync<QuestionDto>(prompt);
    
    if (result.IsSuccess)
    {
        return result.Result;
    }
    
    _logger.LogError("生成结构化题目失败: {Errors}", 
        string.Join(", ", result.Errors));
    return null;
}
```

---

## 4. 查询审计日志

### 4.1 使用API查询

LLM审计控制器提供了多个查询端点：

#### 搜索审计日志

```http
GET /api/llmaudit/search?page=1&pageSize=20&businessScenario=QuestionGeneration&isSuccess=true
```

#### 获取单条审计日志

```http
GET /api/llmaudit/{auditId}
```

#### 获取使用统计

```http
GET /api/llmaudit/stats/usage?startTime=2024-01-01&endTime=2024-12-31
```

#### 获取成本统计

```http
GET /api/llmaudit/stats/cost?startTime=2024-01-01&endTime=2024-12-31
```

#### 获取质量统计

```http
GET /api/llmaudit/stats/quality?startTime=2024-01-01&endTime=2024-12-31
```

#### 获取使用趋势

```http
GET /api/llmaudit/stats/trend?startTime=2024-01-01&endTime=2024-12-31&intervalHours=24
```

### 4.2 使用服务直接查询

在代码中直接使用`ILLMAuditService`：

```csharp
public class LLMAnalyticsService
{
    private readonly ILLMAuditService _auditService;
    
    public LLMAnalyticsService(ILLMAuditService auditService)
    {
        _auditService = auditService;
    }
    
    public async Task<object> GetMonthlyReportAsync()
    {
        var startTime = DateTime.UtcNow.AddMonths(-1);
        var endTime = DateTime.UtcNow;
        
        var usageStats = await _auditService.GetUsageStatsAsync(startTime, endTime);
        var costStats = await _auditService.GetCostStatsAsync(startTime, endTime);
        var qualityStats = await _auditService.GetQualityStatsAsync(startTime, endTime);
        
        return new
        {
            usage = usageStats,
            cost = costStats,
            quality = qualityStats
        };
    }
    
    public async Task<List<Models.LLM.LLMAuditLog>> SearchFailedInteractionsAsync()
    {
        var query = new LLMAuditQueryDto
        {
            IsSuccess = false,
            StartTime = DateTime.UtcNow.AddDays(-7),
            Page = 1,
            PageSize = 100
        };
        
        var (items, total) = await _auditService.SearchAsync(query);
        
        return items.ToList();
    }
}
```

---

## 5. 实际场景示例

### 5.1 题目生成服务（QuestionAiGeneratorService）

```csharp
public class QuestionAiGeneratorService
{
    private readonly AuditableLLMAssistant _auditableLLM;
    private readonly ILogger<QuestionAiGeneratorService> _logger;
    
    public async Task<List<QuestionDto>> GenerateQuestionsAsync(
        GenerateQuestionsRequestDto request)
    {
        var batchId = Guid.NewGuid().ToString();
        var questions = new List<QuestionDto>();
        
        try
        {
            // 构建提示词
            var prompt = BuildPrompt(request);
            
            // 调用LLM生成内容，自动审计
            var generatedContent = await _auditableLLM
                .WithBusinessScenario("QuestionGeneration")
                .WithInteractionType("Generation")
                .WithBatch(batchId)
                .WithBusinessEntity("Question", request.CategoryId, request.Count)
                .WithMetadata("difficulty", request.Difficulty)
                .WithMetadata("type", request.Type)
                .GenerateContentAsync(prompt);
            
            // 解析生成的内容
            questions = ParseQuestions(generatedContent);
            
            return questions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "题目生成失败");
            
            // 尝试修正
            try
            {
                var correctedContent = await RequestFormatCorrectionAsync(
                    request, ex.Message, batchId);
                
                questions = ParseQuestions(correctedContent);
            }
            catch
            {
                throw;
            }
        }
        
        return questions;
    }
    
    private async Task<string> RequestFormatCorrectionAsync(
        GenerateQuestionsRequestDto request, 
        string errorMessage,
        string batchId)
    {
        var correctionPrompt = $@"上次生成的题目格式有问题，错误信息：{errorMessage}
请重新生成，确保格式正确。";
        
        // 关联到批次，标记为修正操作
        return await _auditableLLM
            .WithBusinessScenario("QuestionGeneration")
            .WithInteractionType("Correction")
            .WithBatch(batchId)
            .WithBusinessEntity("Question", request.CategoryId, request.Count)
            .GenerateContentAsync(correctionPrompt);
    }
}
```

### 5.2 题目审核服务（QuestionService）

```csharp
public class QuestionService
{
    private readonly AuditableLLMAssistant _auditableLLM;
    private readonly ILogger<QuestionService> _logger;
    
    public async Task<List<QuestionAuditResultDto>> BatchAuditQuestionsAsync(
        List<QuestionDto> questions)
    {
        var batchId = Guid.NewGuid().ToString();
        var results = new List<QuestionAuditResultDto>();
        
        // 分批处理（每批10个题目）
        var batches = questions.Chunk(10).ToList();
        
        for (int batchIndex = 0; batchIndex < batches.Count; batchIndex++)
        {
            var batch = batches[batchIndex];
            
            // 构建审核提示词
            var prompt = BuildAuditPrompt(batch);
            
            // 调用LLM审核，自动审计
            var auditResponse = await _auditableLLM
                .WithBusinessScenario("QuestionAudit")
                .WithInteractionType("BatchAudit")
                .WithBatch(batchId, batchIndex + 1)
                .WithBusinessEntity("Question", "batch", batch.Count())
                .GenerateContentAsync(prompt);
            
            // 解析审核结果
            var batchResults = ParseBatchAuditResponse(auditResponse, batch);
            results.AddRange(batchResults);
        }
        
        return results;
    }
}
```

---

## 6. 权限配置

LLM审计查询API使用了权限控制，需要配置相应的权限：

- `llm.audit.view`：查看审计日志
- `llm.audit.stats`：查看统计数据

在权限管理系统中添加这些权限并分配给相应的角色。

---

## 7. 监控和告警

### 7.1 监控指标

建议监控以下指标：

1. **使用量指标**
   - 总交互次数
   - 成功率
   - 平均响应时间

2. **成本指标**
   - 每日/每月成本
   - 按模型分类的成本
   - 按业务场景分类的成本

3. **质量指标**
   - JSON修复率
   - 平均重试次数
   - 质量评分

### 7.2 设置告警

可以基于审计数据设置告警：

```csharp
public class LLMMonitoringService
{
    private readonly ILLMAuditService _auditService;
    
    public async Task CheckAnomaliesAsync()
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);
        
        var stats = await _auditService.GetUsageStatsAsync(today, tomorrow);
        
        // 检查成功率
        if (stats.SuccessRate < 90)
        {
            // 发送告警
            await SendAlertAsync($"LLM成功率过低: {stats.SuccessRate:F2}%");
        }
        
        // 检查失败次数
        if (stats.FailedInteractions > 100)
        {
            // 发送告警
            await SendAlertAsync($"LLM失败次数过多: {stats.FailedInteractions}");
        }
    }
}
```

---

## 8. 常见问题

### Q1: 如何禁用LLM审计？

在配置中设置 `Audit.LLMAudit.Enabled = false`。

### Q2: 审计会影响性能吗？

LLM审计使用RabbitMQ异步处理，对主流程的性能影响极小。如果未配置RabbitMQ，将使用同步存储，可能会有轻微的性能影响。

### Q3: 审计数据占用空间大吗？

可以通过以下配置控制：
- `MaxPromptLength`: 限制提示词长度
- `MaxResponseLength`: 限制响应长度
- `LogProcessedData`: 是否记录处理后的数据

### Q4: 如何查看某个批次的所有审计记录？

使用批次ID查询：

```http
GET /api/llmaudit/search?batchId={batchId}
```

### Q5: 如何追踪重试和修正的关系？

使用 `ParentAuditId` 字段关联父审计记录：

```csharp
.WithParentAuditId(parentAuditId)
```

---

## 9. 最佳实践

1. **始终设置业务场景**：使用 `WithBusinessScenario()` 设置有意义的业务场景名称
2. **使用批次ID**：在批量操作中使用 `WithBatch()` 关联相关的审计记录
3. **关联业务实体**：使用 `WithBusinessEntity()` 关联题目、试卷等业务对象
4. **添加元数据**：使用 `WithMetadata()` 添加对分析有用的附加信息
5. **定期清理**：根据存储容量定期清理旧的审计数据
6. **监控成本**：定期检查LLM使用成本，优化提示词和调用频率

---

## 10. 总结

LLM审计组件提供了全面的LLM使用追踪和分析能力，帮助你：

- 🔍 **追踪所有LLM交互**：完整记录提示词、响应、处理结果
- 📊 **分析使用情况**：提供使用量、成本、质量等多维度统计
- 🔗 **关联业务场景**：将LLM审计与业务实体关联，便于问题排查
- 💰 **成本控制**：实时监控LLM使用成本
- ⚡ **性能优化**：异步处理，不影响主流程性能

通过合理使用LLM审计功能，可以更好地管理和优化LLM的使用。

