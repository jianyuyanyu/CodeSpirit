# CodeSpirit.LLM

大语言模型（LLM）服务组件，提供统一的接口与各种LLM API进行交互。

## 🚀 功能特性

- **支持多种大语言模型API**：OpenAI、阿里云灵积等
- **统一的接口**：便于应用集成，一致的调用方式
- **零配置使用**：默认使用配置文件，无需编写设置提供者
- **流式响应处理**：支持流式响应，提升用户体验
- **代理设置支持**：支持HTTP代理配置
- **统一配置管理**：在Aspire主机中统一配置所有服务的LLM参数

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

#### 方式一：使用LLMAssistant（推荐）

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

#### 方式二：使用LLMClientFactory

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
