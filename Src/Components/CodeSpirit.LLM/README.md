# CodeSpirit.LLM

大语言模型（LLM）服务组件，提供统一的接口与各种LLM API进行交互。

## 🚀 功能特性

### 核心功能
- **支持多种大语言模型API**：OpenAI、阿里云灵积等
- **统一的接口**：便于应用集成，一致的调用方式
- **零配置使用**：默认使用配置文件，无需编写设置提供者
- **流式响应处理**：支持流式响应，提升用户体验
- **代理设置支持**：支持HTTP代理配置
- **统一配置管理**：在Aspire主机中统一配置所有服务的LLM参数

### 增强功能 🆕
- **智能JSON处理**：自动修复AI返回的损坏JSON，处理截断、括号不匹配等问题
- **提示词模板系统**：支持变量替换、条件语句、循环语句的模板引擎
- **批量处理和重试**：智能分批处理、自动重试、并发控制
- **结构化任务处理**：一站式AI任务处理，自动JSON解析和错误处理
- **降级策略**：多层次的错误处理和降级机制

## 📦 安装方式

在项目中添加对此组件的引用：

```shell
dotnet add reference ../Components/CodeSpirit.LLM/CodeSpirit.LLM.csproj
```

## 🔧 快速开始

### 1. 注册服务（推荐方式）

在各API项目的配置类中：

```csharp
// 使用默认的基于配置的设置提供者（推荐）
builder.Services.AddLLMServices();
```

### 2. Aspire主机统一配置

在`CodeSpirit.AppHost/Program.cs`中，LLM配置已统一管理：

```csharp
// LLM配置参数已在Aspire主机中统一配置
var llmApiKey = builder.AddParameter("llm-ApiKey", secret: true);
var llmApiBaseUrl = builder.AddParameter("llm-ApiBaseUrl", "https://dashscope.aliyuncs.com/compatible-mode/v1");
var llmModelName = builder.AddParameter("llm-ModelName", "qwen-plus");
// ...

// 所有服务会自动接收这些环境变量
.WithEnvironment("LLM__ApiKey", llmApiKey)
.WithEnvironment("LLM__ApiBaseUrl", llmApiBaseUrl)
.WithEnvironment("LLM__ModelName", llmModelName)
// ...
```

### 3. 本地配置（可选）

如果需要在本地开发时覆盖配置，可在`appsettings.json`中添加：

```json
{
  "LLM": {
    "ApiBaseUrl": "https://dashscope.aliyuncs.com/compatible-mode/v1",
    "ApiKey": "your-api-key",
    "ModelName": "qwen-plus",
    "TimeoutSeconds": 120,
    "MaxTokens": 2048,
    "UseProxy": false,
    "ProxyAddress": null
  }
}
```

### 4. 使用LLM服务

#### 方式一：基础内容生成

```csharp
public class YourService
{
    private readonly LLMAssistant _llmAssistant;

    public YourService(LLMAssistant llmAssistant)
    {
        _llmAssistant = llmAssistant;
    }

    public async Task<string> GenerateContentAsync(string prompt)
    {
        return await _llmAssistant.GenerateContentAsync(prompt);
    }

    public async Task<string> GenerateWithSystemPromptAsync(string systemPrompt, string userPrompt)
    {
        return await _llmAssistant.GenerateContentAsync(systemPrompt, userPrompt);
    }
}
```

#### 方式二：结构化任务处理（推荐） 🆕

```csharp
public class YourService
{
    private readonly LLMAssistant _llmAssistant;

    public YourService(LLMAssistant llmAssistant)
    {
        _llmAssistant = llmAssistant;
    }

    // 使用模板处理结构化任务
    public async Task<MyResult> ProcessStructuredTaskAsync(MyInput input)
    {
        var result = await _llmAssistant.ProcessStructuredTaskWithTemplateAsync<MyResult>(
            "my_template", 
            input,
            new StructuredTaskOptions 
            { 
                EnableRetry = true, 
                MaxRetries = 2 
            });

        if (result.IsSuccess)
        {
            return result.Result!;
        }
        
        throw new InvalidOperationException($"处理失败: {string.Join("; ", result.Errors)}");
    }

    // 批量处理
    public async Task<List<MyResult>> ProcessBatchAsync(List<MyInput> inputs)
    {
        var batchResult = await _llmAssistant.ProcessBatchStructuredTaskAsync<MyInput, MyResult>(
            inputs,
            batch => GeneratePromptForBatch(batch),
            new BatchProcessingOptions 
            { 
                BatchSize = 10, 
                MaxRetries = 2 
            });

        return batchResult.SuccessResults.Where(r => r.IsSuccess).Select(r => r.Result!).ToList();
    }
}
```

#### 方式三：使用LLMClientFactory

```csharp
public class YourService
{
    private readonly ILLMClientFactory _llmClientFactory;

    public YourService(ILLMClientFactory llmClientFactory)
    {
        _llmClientFactory = llmClientFactory;
    }

    public async Task<string> GenerateContentAsync(string prompt)
    {
        var llmClient = await _llmClientFactory.CreateClientAsync();
        if (llmClient == null)
        {
            throw new InvalidOperationException("无法创建LLM客户端，请检查设置");
        }
        
        return await llmClient.GenerateContentAsync(prompt);
    }
}
```

## 🔧 高级用法

### 独立使用各个组件 🆕

#### JSON处理器
```csharp
public class YourService
{
    private readonly ILLMJsonProcessor _jsonProcessor;

    public async Task<T> ParseAiResponse<T>(string aiResponse) where T : class
    {
        var result = await _jsonProcessor.ParseStructuredResponseAsync<T>(aiResponse);
        return result.IsSuccess ? result.Result! : throw new Exception("解析失败");
    }
}
```

#### 提示词构建器
```csharp
public class YourService
{
    private readonly ILLMPromptBuilder _promptBuilder;

    public string BuildComplexPrompt(object data)
    {
        return _promptBuilder
            .Reset()
            .WithSystemPrompt("你是一个专业的助手")
            .WithTemplate("my_template", data)
            .WithValidationRules("规则1", "规则2")
            .WithOutputFormat<MyResult>()
            .Build();
    }
}
```

#### 批量处理器
```csharp
public class YourService
{
    private readonly ILLMBatchProcessor _batchProcessor;

    public async Task<List<TResult>> ProcessWithRetry<TResult>(
        Func<Task<TResult>> operation)
    {
        return await _batchProcessor.ProcessWithRetryAsync(operation, 
            new RetryOptions { MaxRetries = 3 });
    }
}
```

### 自定义设置提供者

如果需要从数据库或其他来源获取设置，可以实现自定义设置提供者：

```csharp
public class DatabaseLLMSettingsProvider : ISettingsProvider
{
    private readonly ISettingsService _settingsService;

    public DatabaseLLMSettingsProvider(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public async Task<T?> GetSettingsAsync<T>(string settingsKey) where T : class, new()
    {
        if (typeof(T) == typeof(LLMSettings) && settingsKey == "LLMSettings")
        {
            return await _settingsService.GetGlobalSettingAsync<T>("LLM", settingsKey);
        }
        
        return null;
    }

    public async Task<bool> SaveSettingsAsync<T>(string settingsKey, T settings) where T : class, new()
    {
        return await _settingsService.SaveGlobalSettingAsync("LLM", settingsKey, settings);
    }
}

// 注册自定义设置提供者
builder.Services.AddLLMServices<DatabaseLLMSettingsProvider>();
```

## 📋 支持的模型

### OpenAI兼容API
- OpenAI GPT-4, GPT-3.5等
- 阿里云灵积大模型（qwen系列）
- 其他兼容OpenAI API格式的模型

### 配置示例

#### OpenAI
```json
{
  "LLM": {
    "ApiBaseUrl": "https://api.openai.com/v1",
    "ApiKey": "sk-xxx",
    "ModelName": "gpt-4o"
  }
}
```

#### 阿里云灵积
```json
{
  "LLM": {
    "ApiBaseUrl": "https://dashscope.aliyuncs.com/compatible-mode/v1",
    "ApiKey": "sk-xxx",
    "ModelName": "qwen-plus"
  }
}
```

## 🔍 故障排除

### 常见问题

1. **无法创建LLM客户端**
   - 检查API密钥是否正确配置
   - 确认网络连接正常
   - 检查代理设置

2. **请求超时**
   - 增加`TimeoutSeconds`配置值
   - 检查网络连接稳定性
   - 考虑使用代理

3. **API调用失败**
   - 检查API基础地址是否正确
   - 确认模型名称是否支持
   - 查看日志获取详细错误信息

### 日志配置

在`appsettings.json`中启用详细日志：

```json
{
  "Logging": {
    "LogLevel": {
      "CodeSpirit.LLM": "Debug"
    }
  }
}
```

## 📖 迁移指南

如果您正在从旧版本的LLM组件迁移，请参阅 [迁移指南](./MIGRATION_GUIDE.md)，其中包含：

- 详细的迁移步骤
- 代码对比示例
- 性能改善说明
- 注意事项和最佳实践

## 🎯 最佳实践

### 1. 结构化任务处理
- 优先使用 `ProcessStructuredTaskWithTemplateAsync` 而不是直接调用 `GenerateContentAsync`
- 为复杂的AI任务创建专门的模板
- 启用重试机制以提高稳定性

### 2. 批量处理
- 对于大量数据处理，使用批量处理功能
- 根据AI模型的限制调整批次大小
- 启用 `ContinueOnFailure` 以处理部分失败的情况

### 3. 错误处理
- 始终检查 `IsSuccess` 属性
- 记录 `WasRepaired` 信息以监控JSON修复情况
- 为失败情况提供降级策略

### 4. 性能优化
- 使用单例模式注册 `ILLMPromptTemplateManager`
- 合理设置批次大小和重试参数
- 监控处理时间和成功率