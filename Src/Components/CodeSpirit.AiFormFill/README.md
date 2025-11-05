# CodeSpirit.AiFormFill

CodeSpirit AI表单智能填充组件，提供基于LLM的表单内容自动生成功能。

## 🚀 核心特性

- **零配置使用**：只需2行代码即可启用完整功能
- **双模式支持**：支持全局AI填充和字段触发模式
- **自动端点生成**：基于DTO自动生成API端点，无需手动编写控制器代码
- **智能提示词构建**：自动分析DTO结构，生成结构化提示词
- **智能JSON结构检测**：自动检测自定义模板中的JSON结构说明，避免重复追加
- **CustomDescription集成**：自动在JSON结构注释中包含字段的CustomDescription，为LLM提供更详细的指导
- **完整的缓存机制**：内置智能缓存提升性能
- **响应解析与验证**：自动解析LLM返回的JSON格式数据
- **独立LLM配置**：支持为AI表单填充配置专用的LLM设置，包括禁用思考、JSON响应格式等

## 📦 安装

```bash
# 通过项目引用安装
<ProjectReference Include="..\..\Components\CodeSpirit.AiFormFill\CodeSpirit.AiFormFill.csproj" />
```

## 🔧 快速开始

### 1. 服务注册

```csharp
// Program.cs

// 注册LLM服务（必需）
builder.Services.AddLLMServices<YourLLMSettingsProvider>();

// 方案一：基础服务（需要手动编写控制器代码）
builder.Services.AddAiFormFill();

// 方案二：自动端点服务（推荐，零代码）
builder.Services.AddAiFormFillEndpoints();

var app = builder.Build();

// 启用AI填充中间件（仅方案二需要）
app.UseAiFormFillEndpoints();
```

### 2. DTO配置

```csharp
/// <summary>
/// 字段触发模式示例
/// </summary>
[AiFormFill(TriggerField = nameof(Topic))]
public class CreateSurveyDto
{
    [Required]
    [DisplayName("问卷主题")]
    public string Topic { get; set; } = string.Empty;
    
    [DisplayName("问卷描述")]
    [AiFieldFill(Priority = 1)]
    public string? Description { get; set; }
}

/// <summary>
/// 全局填充模式示例
/// </summary>
[AiFormFill(GlobalFillPrompt = "智能生成问卷题目")]
public class CreateQuestionDto
{
    [Required]
    [DisplayName("题目标题")]
    public string Title { get; set; } = string.Empty;
    
    [DisplayName("题目类型")]
    public QuestionType Type { get; set; }
}
```

### 3. 控制器使用

#### 方案一：手动控制器方法

```csharp
[HttpPost("ai-fill")]
public async Task<ActionResult<ApiResponse<CreateSurveyDto>>> AiFill([FromBody] CreateSurveyDto request)
{
    return await this.HandleAiFillAsync(_aiFormFillService, request);
}
```

#### 方案二：零代码（推荐）

```csharp
public class SurveysController : ApiControllerBase
{
    // 无需任何AI相关代码！
    // 系统自动提供: POST /api/surveys/ai-fill
    
    [HttpPost]
    public async Task<ActionResult<ApiResponse<SurveyDto>>> CreateSurvey([FromBody] CreateSurveyDto createDto)
    {
        // 专注业务逻辑
        var result = await _surveyService.CreateAsync(createDto);
        return SuccessResponse(result);
    }
}
```

## 📋 特性配置

### AiFormFillAttribute

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `TriggerField` | string | "" | 触发字段名称，为空时启用全局模式 |
| `IgnoreFields` | string[] | [] | 需要忽略的字段列表 |
| `CustomPromptTemplate` | string | "" | 自定义提示词模板 |
| `ApiEndpoint` | string | "ai-fill" | API端点路径 |
| `MaxTokens` | int | 1000 | 最大Token数量 |
| `EnableCache` | bool | true | 是否启用缓存 |
| `CacheExpirationMinutes` | int | 30 | 缓存过期时间（分钟） |
| `GlobalFillPrompt` | string | "使用AI智能优化表单" | 全局模式提示文本 |
| `UseIndependentLLM` | bool | false | 是否使用独立的LLM配置 |
| `LLMSettingsKey` | string | "AiFormFillLLM" | 独立LLM配置的设置键名 |
| `DisableThinking` | bool | true | 是否禁用思考（enable_thinking: false） |
| `ResponseFormatType` | string | "json_object" | 响应格式类型 |
| `Temperature` | double | 0.1 | 温度参数，控制生成内容的随机性 |
| `TopP` | double | 0.9 | Top-p参数，控制生成内容的多样性 |

### AiFieldFillAttribute

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `Enabled` | bool | true | 是否参与AI填充 |
| `Weight` | int | 1 | 字段权重 |
| `Priority` | int | 0 | 字段填充优先级 |
| `CustomDescription` | string | "" | 自定义字段描述 |

## 🎯 使用模式

### 全局AI填充模式

适用于创意性内容生成，用户在表单顶部输入自定义需求，AI一次性填充整个表单。

```csharp
[AiFormFill(GlobalFillPrompt = "智能生成内容")]
public class CreateContentDto
{
    [DisplayName("标题")]
    public string Title { get; set; } = string.Empty;
    
    [DisplayName("内容")]
    public string Content { get; set; } = string.Empty;
}
```

### 字段触发模式

适用于基于关键信息的内容扩展，用户输入触发字段后，AI智能填充其他相关字段。

```csharp
[AiFormFill(TriggerField = nameof(Keyword))]
public class GenerateContentDto
{
    [Required]
    [DisplayName("关键词")]
    public string Keyword { get; set; } = string.Empty;
    
    [DisplayName("生成的标题")]
    public string? Title { get; set; }
    
    [DisplayName("生成的描述")]
    public string? Description { get; set; }
}
```

## 🔧 高级配置

### 独立LLM配置

为AI表单填充配置专用的LLM设置，支持禁用思考、JSON响应格式等高级特性：

#### 1. 配置文件设置

```json
{
  "AiFormFillLLM": {
    "ApiBaseUrl": "https://dashscope.aliyuncs.com/compatible-mode/v1",
    "ApiKey": "your-api-key",
    "ModelName": "qwq-plus",
    "TimeoutSeconds": 120,
    "MaxTokens": 2048,
    "DisableThinking": true,
    "ResponseFormatType": "json_object",
    "Temperature": 0.1,
    "TopP": 0.9,
    "EnableStreaming": false
  }
}
```

#### 2. DTO配置

```csharp
[AiFormFill(
    TriggerField = nameof(Topic),
    UseIndependentLLM = true,
    LLMSettingsKey = "AiFormFillLLM",
    DisableThinking = true,
    ResponseFormatType = "json_object",
    Temperature = 0.1,
    TopP = 0.9)]
public class SmartSurveyDto
{
    [Required]
    [DisplayName("问卷主题")]
    public string Topic { get; set; } = string.Empty;
    
    [DisplayName("问卷描述")]
    [AiFieldFill(Priority = 1)]
    public string? Description { get; set; }
    
    [DisplayName("目标受众")]
    [AiFieldFill(Priority = 2)]
    public string? TargetAudience { get; set; }
}
```

#### 3. 服务注册

```csharp
// Program.cs
builder.Services.AddLLMServices<YourLLMSettingsProvider>();
builder.Services.AddAiFormFillEndpoints();

// 可选：配置独立LLM选项
builder.Services.AddAiFormFillIndependentLLM(options =>
{
    options.DefaultSettingsKey = "AiFormFillLLM";
    options.DefaultDisableThinking = true;
    options.DefaultResponseFormatType = "json_object";
    options.DefaultTemperature = 0.1;
    options.DefaultTopP = 0.9;
});
```

### 自定义提示词模板

#### 基础自定义模板（自动追加JSON结构）

```csharp
[AiFormFill(
    TriggerField = nameof(Topic),
    CustomPromptTemplate = "基于主题 '{Topic}' 生成相关内容...")]
public class CustomPromptDto
{
    public string Topic { get; set; } = string.Empty;
    
    [DisplayName("生成的内容")]
    [AiFieldFill(CustomDescription = "与主题高度相关的详细内容")]
    public string? GeneratedContent { get; set; }
}
```

系统会自动追加JSON结构说明：
```json
请以JSON格式回复，格式如下：
{
  "GeneratedContent": "字段值" // 与主题高度相关的详细内容
}
```

#### 完整自定义模板（包含JSON结构，不会重复追加）

```csharp
[AiFormFill(
    TriggerField = nameof(Description),
    CustomPromptTemplate = @"你是一个目标管理专家。

用户输入：{Description}
请优化目标描述，并提取关键信息。

**返回JSON结构说明：**
```json
{
  ""description"": ""string, 必填。优化后的目标描述"",
  ""title"": ""string, 必填。提取的简短标题"",
  ""category"": ""string, 必填。目标类型""
}
```

请严格按照上述JSON结构返回。")]
public class GoalDto
{
    public string Description { get; set; } = string.Empty;
    
    [AiFieldFill(Priority = 1, CustomDescription = "优化后的目标描述")]
    public string? Title { get; set; }
    
    [AiFieldFill(Priority = 2, CustomDescription = "目标分类")]
    public string? Category { get; set; }
}
```

系统会智能检测到模板中已包含JSON结构说明（关键词：` ```json `），不会重复追加。

### 字段级控制

```csharp
public class DetailedDto
{
    [DisplayName("重要字段")]
    [AiFieldFill(Priority = 1, Weight = 3)]
    public string ImportantField { get; set; } = string.Empty;

    [DisplayName("内部备注")]
    [AiFieldFill(Enabled = false)]
    public string? InternalNote { get; set; }
}
```

## 🎨 智能提示词特性

### 1. 智能JSON结构检测

系统会自动检测自定义提示词模板中是否已包含JSON结构说明，避免重复追加。

#### 检测关键词

**中文关键词：**
- ` ```json ` / ` ```JSON ` - Markdown JSON代码块
- `JSON结构说明`
- `返回JSON结构`
- `JSON格式说明`
- `以JSON格式回复`

**英文关键词：**
- `JSON Schema`
- `return JSON structure`
- `response format`
- `output format`

#### 工作原理

```csharp
// 场景1：简单模板 - 系统自动追加JSON结构
[AiFormFill(CustomPromptTemplate = "请生成相关内容")]
// 结果：模板 + 自动追加的JSON结构说明

// 场景2：包含JSON结构的完整模板 - 不会重复追加
[AiFormFill(CustomPromptTemplate = @"请生成内容
**返回JSON结构：**
```json
{ ""field"": ""value"" }
```")]
// 结果：仅使用自定义模板，不追加额外JSON结构
```

### 2. CustomDescription自动集成

系统会自动在JSON结构注释中包含字段的`CustomDescription`，为LLM提供更详细的指导。

#### 字段描述优先级

1. **CustomDescription**（最高优先级）- 专为AI设计的详细描述
2. **DisplayName** - 字段显示名称

#### 示例效果

```csharp
public class ExampleDto
{
    [DisplayName("标题")]
    [AiFieldFill(CustomDescription = "简短精炼的标题，不超过20字")]
    public string? Title { get; set; }
    
    [DisplayName("内容")]
    [Required]
    [MaxLength(500)]
    [AiFieldFill(CustomDescription = "详细内容，需包含关键信息和上下文")]
    public string? Content { get; set; }
}
```

**自动生成的JSON结构：**
```json
{
  "Title": "字段值" // 简短精炼的标题，不超过20字
  "Content": "字段值" // 详细内容，需包含关键信息和上下文, 必填, 最多500字符
}
```

#### 优势

- **更清晰的AI指导**：CustomDescription为LLM提供了更详细的字段说明
- **更好的上下文**：LLM能够更准确地理解每个字段的用途和要求
- **更高质量的输出**：详细的描述帮助LLM生成更符合预期的内容
- **验证信息可见**：字段的约束条件直接显示在JSON结构中

## 🚀 自动生成的端点

基于DTO配置，系统会自动生成以下端点：

| DTO类型 | 自动生成的端点 | 说明 |
|---------|----------------|------|
| `CreateQuestionDto` | `POST /api/survey/questions/ai-fill` | 题目AI填充 |
| `CreateSurveyDto` | `POST /api/survey/surveys/ai-fill` | 问卷AI填充 |
| `GenerateContentDto` | `POST /api/content/contents/ai-fill` | 内容AI填充 |

## 🆕 独立LLM配置特性

### 主要优势

1. **专用优化**：为AI表单填充场景专门优化的LLM配置
2. **禁用思考**：通过 `enable_thinking: false` 提高响应速度和准确性
3. **JSON格式**：强制 `response_format: {"type": "json_object"}` 确保结构化输出
4. **精确控制**：独立的温度、Top-p等参数控制
5. **配置隔离**：不影响全局LLM配置，可以使用不同的模型和API

### 使用场景

- 需要快速、准确的结构化数据生成
- 对响应格式有严格要求的表单填充
- 需要使用特定模型（如qwq-plus）进行表单智能填充
- 希望与其他LLM应用场景隔离配置

### 配置优先级

1. DTO特性中的参数设置（最高优先级）
2. 独立LLM配置文件中的设置
3. 全局LLM配置（当UseIndependentLLM=false时）

## 📊 响应内容日志

### 日志配置

为了便于调试和监控，组件提供了详细的日志输出功能：

```json
{
  "Logging": {
    "LogLevel": {
      "CodeSpirit.AiFormFill": "Information"
    }
  }
}
```

### 日志内容

- **请求日志**：完整的 LLM API 请求体和参数
- **响应日志**：API 返回的完整响应内容
- **解析日志**：JSON 提取和属性设置过程
- **错误日志**：详细的错误信息和调试数据

### 示例日志输出

```
[Information] 使用独立AI表单填充LLM配置，设置键：AiFormFillLLM
[Information] AI表单填充LLM响应内容：{"choices":[{"message":{"content":"{\"description\":\"智能问卷描述\"}"}}]}
[Information] AI表单填充解析后的结果：{"Topic":"AI调研","Description":"智能问卷描述"}
[Information] AI表单填充成功设置属性 Description = 智能问卷描述
```

详细的日志配置和调试指南请参考：[日志配置文档](Examples/LoggingConfiguration.md)

## 🚨 错误处理

### 自动错误检测

组件会自动检测和处理各种错误情况：

- **HTTP 错误状态码**：401、400、429、500 等
- **网络连接问题**：DNS 解析失败、SSL 证书错误、超时等
- **JSON 解析错误**：响应格式不正确、字段类型不匹配等
- **配置错误**：无效的 API 密钥、模型名称等
- **流式模式要求**：自动检测"只支持流式模式"的模型并重试

### 错误日志输出

所有错误都会输出详细的日志信息，包括：

```
[Error] AI表单填充API请求失败，状态码: Unauthorized, 错误内容: {"error":{"message":"Invalid API key"}}
[Error] AI表单填充HTTP请求失败: The SSL connection could not be established
[Error] AI表单填充请求超时: The operation was canceled.
[Warning] 检测到模型只支持流式模式，尝试启用流式响应重新请求
[Information] 使用流式模式重新发送AI表单填充请求
```

### 错误处理示例

参考 [错误处理示例](Examples/ErrorHandlingExample.cs) 了解如何测试和处理各种错误情况。

## 📚 依赖关系

- **CodeSpirit.Core**：核心框架
- **CodeSpirit.LLM**：LLM服务抽象
- **Microsoft.AspNetCore.App**：ASP.NET Core框架
- **Newtonsoft.Json**：JSON序列化
- **Microsoft.Extensions.Caching.Memory**：内存缓存

## 🤝 贡献

欢迎提交Issue和Pull Request来帮助改进这个组件。

## 📄 许可证

本项目采用MIT许可证。
