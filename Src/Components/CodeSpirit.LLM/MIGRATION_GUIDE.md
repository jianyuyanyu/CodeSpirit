# LLM组件增强功能迁移指南

本指南帮助您将现有的AI处理代码迁移到新的增强LLM组件。

## 🚀 新增功能概览

### 1. JSON响应处理和修复
- **ILLMJsonProcessor**: 自动处理和修复AI返回的损坏JSON
- **功能**: 提取JSON、修复截断、平衡括号、修复引号等

### 2. 提示词模板管理
- **ILLMPromptBuilder**: 流式API构建复杂提示词
- **ILLMPromptTemplateManager**: 模板注册和渲染
- **功能**: 模板变量替换、条件语句、循环语句

### 3. 批量处理和重试机制
- **ILLMBatchProcessor**: 智能批量处理和重试
- **功能**: 自动分批、并发处理、失败重试、降级策略

### 4. 结构化任务处理
- **LLMAssistant增强**: 一站式结构化AI任务处理
- **功能**: 自动JSON解析、模板渲染、批量处理集成

## 📋 迁移步骤

### 步骤1：更新服务注册

**之前的注册方式保持不变**，新组件会自动注册：

```csharp
// 现有代码无需修改
builder.Services.AddLLMServices();
```

### 步骤2：迁移JSON处理逻辑

**迁移前（QuestionService中约400行代码）**：
```csharp
// 大量的JSON处理代码
private string ExtractJsonFromAiResponse(string aiResponse) { /* 50行代码 */ }
private string TryFixJson(string json) { /* 100行代码 */ }
private string HandleTruncatedJson(string json) { /* 80行代码 */ }
private bool IsValidJson(string json) { /* 10行代码 */ }
// ... 更多JSON处理方法
```

**迁移后**：
```csharp
public class YourService
{
    private readonly ILLMJsonProcessor _jsonProcessor;

    // 构造函数注入
    public YourService(ILLMJsonProcessor jsonProcessor)
    {
        _jsonProcessor = jsonProcessor;
    }

    public async Task<T> ProcessAiResponse<T>(string aiResponse) where T : class
    {
        var result = await _jsonProcessor.ParseStructuredResponseAsync<T>(aiResponse);
        
        if (result.IsSuccess)
        {
            return result.Result!;
        }
        
        throw new InvalidOperationException($"解析失败: {string.Join("; ", result.Errors)}");
    }
}
```

### 步骤3：迁移提示词构建逻辑

**迁移前**：
```csharp
// 硬编码的复杂提示词构建
private string BuildBatchAuditPrompt(List<QuestionPreviewDto> questions, bool autoCorrect)
{
    var prompt = $@"请批量审核以下 {questions.Count} 道题目...
    
{(autoCorrect ? "如果发现错误，请自动修正..." : "如果发现错误，请指出错误...")}

题目列表：";

    for (int i = 0; i < questions.Count; i++)
    {
        var question = questions[i];
        prompt += $@"
题目 {i + 1}:
- 内容：{question.Content}
- 类型：{question.Type}
// ... 更多硬编码内容
";
    }
    
    return prompt;
}
```

**迁移后**：
```csharp
public class YourService
{
    private readonly ILLMPromptBuilder _promptBuilder;

    public async Task<string> BuildAuditPrompt(List<QuestionPreviewDto> questions, bool autoCorrect)
    {
        return _promptBuilder
            .Reset()
            .WithTemplate("question_batch_audit", new { questions, autoCorrect })
            .WithOutputFormat<BatchAuditResult>()
            .Build();
    }
}
```

### 步骤4：迁移批量处理逻辑

**迁移前（QuestionService中约200行代码）**：
```csharp
private async Task<List<QuestionPreviewDto>> BatchAuditQuestionsWithAutoSplitAsync(
    List<QuestionPreviewDto> questions, bool autoCorrect = true)
{
    const int batchSize = 10;
    var allResults = new List<QuestionPreviewDto>();
    var totalBatches = (int)Math.Ceiling((double)questions.Count / batchSize);
    
    for (int batchIndex = 0; batchIndex < totalBatches; batchIndex++)
    {
        var currentBatch = questions.Skip(batchIndex * batchSize).Take(batchSize).ToList();
        
        const int maxRetries = 2;
        var currentAttempt = 0;
        
        while (currentAttempt <= maxRetries)
        {
            try
            {
                // 复杂的重试逻辑...
                var batchResults = await ProcessBatch(currentBatch);
                allResults.AddRange(batchResults);
                break;
            }
            catch (Exception ex)
            {
                // 复杂的错误处理...
            }
        }
        
        await Task.Delay(1000); // 延迟
    }
    
    return allResults;
}
```

**迁移后**：
```csharp
public class YourService
{
    private readonly ILLMBatchProcessor _batchProcessor;

    public async Task<List<TResult>> ProcessBatchWithRetry<TInput, TResult>(
        List<TInput> items,
        Func<List<TInput>, Task<List<TResult>>> processor)
    {
        var options = new BatchProcessingOptions
        {
            BatchSize = 10,
            MaxRetries = 2,
            DelayBetweenBatches = TimeSpan.FromSeconds(1),
            ContinueOnFailure = true
        };

        var result = await _batchProcessor.ProcessBatchWithRetryAsync(items, processor, options);
        return result.SuccessResults;
    }
}
```

### 步骤5：使用增强的LLMAssistant

**最简单的迁移方式**：
```csharp
public class RefactoredQuestionService
{
    private readonly LLMAssistant _llmAssistant;

    public async Task<List<QuestionPreviewDto>> BatchAuditQuestionsAsync(
        List<QuestionPreviewDto> questions, 
        bool autoCorrect = true)
    {
        // 一行代码完成复杂的AI任务处理
        var result = await _llmAssistant.ProcessStructuredTaskWithTemplateAsync<BatchAuditResult>(
            "question_batch_audit", 
            new { questions, autoCorrect });

        if (result.IsSuccess)
        {
            return ApplyAuditResults(questions, result.Result!);
        }
        
        // 处理失败情况
        return MarkQuestionsAsFailed(questions, result.Errors);
    }
}
```

## 🔄 完整迁移示例

### 原始QuestionService方法（约100行）
```csharp
private async Task<List<QuestionPreviewDto>> BatchAuditQuestionsWithAiAsync(
    List<QuestionPreviewDto> questions, bool autoCorrect = true)
{
    // 大量的重试、JSON处理、错误处理代码...
    const int maxRetries = 2;
    var currentAttempt = 0;
    
    while (currentAttempt <= maxRetries)
    {
        try
        {
            var prompt = BuildBatchAuditPrompt(questions, autoCorrect);
            var aiResponse = await _llmAssistant.GenerateContentAsync(prompt);
            var cleanedResponse = ExtractJsonFromAiResponse(aiResponse);
            
            if (!IsValidJson(cleanedResponse))
            {
                cleanedResponse = TryFixJson(cleanedResponse);
                // 更多JSON修复逻辑...
            }
            
            var batchResult = JsonSerializer.Deserialize<JsonElement>(cleanedResponse);
            // 复杂的结果解析和应用逻辑...
            
            return auditedQuestions;
        }
        catch (Exception ex)
        {
            // 复杂的重试逻辑...
        }
    }
    
    // 失败处理...
}
```

### 迁移后的方法（约10行）
```csharp
public async Task<List<QuestionPreviewDto>> BatchAuditQuestionsAsync(
    List<QuestionPreviewDto> questions, 
    bool autoCorrect = true)
{
    var result = await _llmAssistant.ProcessStructuredTaskWithTemplateAsync<BatchAuditResult>(
        "question_batch_audit", 
        new { questions, autoCorrect },
        new StructuredTaskOptions { EnableRetry = true, MaxRetries = 2 });

    return result.IsSuccess 
        ? ApplyAuditResults(questions, result.Result!) 
        : MarkQuestionsAsFailed(questions, result.Errors);
}
```

## 📊 迁移效果对比

| 方面 | 迁移前 | 迁移后 | 改善 |
|------|--------|--------|------|
| 代码行数 | ~800行 | ~100行 | **减少87.5%** |
| JSON处理 | 手动实现400行 | 组件自动处理 | **完全自动化** |
| 重试机制 | 手动实现200行 | 组件自动处理 | **配置化** |
| 提示词管理 | 硬编码字符串 | 模板化管理 | **可维护性大幅提升** |
| 错误处理 | 分散在各处 | 统一处理 | **一致性保证** |
| 测试难度 | 复杂集成测试 | 独立单元测试 | **测试简化** |

## ⚠️ 注意事项

### 1. 向后兼容性
- 现有的`LLMAssistant.GenerateContentAsync()`方法保持不变
- 现有服务注册方式无需修改
- 可以渐进式迁移，新旧代码可以共存

### 2. 性能考虑
- 新组件增加了一些抽象层，但性能影响微乎其微
- 批量处理和重试机制实际上提升了整体性能
- JSON修复功能避免了重新请求AI的开销

### 3. 配置迁移
- 现有LLM配置无需修改
- 新增的批量处理参数都有合理的默认值
- 模板系统会自动注册内置模板

## 🎯 迁移建议

### 优先级1：JSON处理迁移
- 影响：立即减少90%的JSON处理代码
- 风险：低，完全向后兼容
- 工作量：1-2小时

### 优先级2：批量处理迁移
- 影响：提升稳定性和性能
- 风险：低，需要测试重试逻辑
- 工作量：2-4小时

### 优先级3：提示词模板化
- 影响：长期维护性大幅提升
- 风险：中，需要验证模板渲染结果
- 工作量：4-8小时

### 优先级4：完全重构
- 影响：代码量减少80%以上
- 风险：中，需要全面测试
- 工作量：1-2天

## 📚 相关文档

- [LLM组件README](./README.md) - 基础使用指南
- [JSON处理器文档](./Processors/README.md) - JSON处理详细说明
- [提示词模板文档](./Prompts/README.md) - 模板系统使用指南
- [批量处理器文档](./Processors/BatchProcessor.md) - 批量处理配置说明
