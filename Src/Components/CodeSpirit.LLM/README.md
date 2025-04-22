# CodeSpirit.LLM

大语言模型（LLM）服务组件，提供统一的接口与各种LLM API进行交互。

## 功能特性

- 支持多种大语言模型API（如OpenAI、阿里云灵积等）
- 统一的接口，便于应用集成
- 可扩展的设置提供机制
- 流式响应处理
- 代理设置支持

## 安装方式

在项目中添加对此组件的引用：

```shell
dotnet add reference ../Components/CodeSpirit.LLM/CodeSpirit.LLM.csproj
```

## 使用方法

### 1. 注册服务

在`Program.cs`或`Startup.cs`中注册LLM服务：

```csharp
// 基本用法
builder.Services.AddLLMServices();

// 使用自定义设置提供者
builder.Services.AddLLMServices<YourSettingsProvider>();
```

### 2. 配置设置

在配置文件`appsettings.json`中添加LLM设置：

```json
{
  "LLM": {
    "ApiBaseUrl": "https://api.openai.com/v1",
    "ApiKey": "your-api-key",
    "ModelName": "gpt-4o",
    "TimeoutSeconds": 120,
    "MaxTokens": 2048,
    "UseProxy": false,
    "ProxyAddress": null
  }
}
```

### 3. 使用LLM服务

```csharp
// 注入工厂
private readonly ILLMClientFactory _llmClientFactory;

public YourService(ILLMClientFactory llmClientFactory)
{
    _llmClientFactory = llmClientFactory;
}

// 使用LLM客户端
public async Task<string> GenerateContentAsync(string prompt)
{
    var llmClient = await _llmClientFactory.CreateClientAsync();
    if (llmClient == null)
    {
        throw new InvalidOperationException("无法创建LLM客户端，请检查设置");
    }
    
    return await llmClient.GenerateContentAsync(prompt);
}
```

## 实现自定义设置提供者

创建一个实现`ISettingsProvider`接口的类：

```csharp
public class YourSettingsProvider : ISettingsProvider
{
    public async Task<T?> GetSettingsAsync<T>(string settingsKey) where T : class, new()
    {
        // 从数据库、配置中心或其他地方获取设置
        // ...
        
        return settings;
    }
    
    public async Task<bool> SaveSettingsAsync<T>(string settingsKey, T settings) where T : class, new()
    {
        // 保存设置到数据库、配置中心或其他地方
        // ...
        
        return true;
    }
}
``` 