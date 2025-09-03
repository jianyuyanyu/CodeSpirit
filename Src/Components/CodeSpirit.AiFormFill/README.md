# CodeSpirit.AiFormFill

CodeSpirit AI表单智能填充组件，提供基于LLM的表单内容自动生成功能。

## 🚀 核心特性

- **零配置使用**：只需2行代码即可启用完整功能
- **双模式支持**：支持全局AI填充和字段触发模式
- **自动端点生成**：基于DTO自动生成API端点，无需手动编写控制器代码
- **智能提示词构建**：自动分析DTO结构，生成结构化提示词
- **完整的缓存机制**：内置智能缓存提升性能
- **响应解析与验证**：自动解析LLM返回的JSON格式数据

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

### 自定义提示词模板

```csharp
[AiFormFill(
    TriggerField = nameof(Topic),
    CustomPromptTemplate = "基于主题 '{0}' 生成相关内容...")]
public class CustomPromptDto
{
    public string Topic { get; set; } = string.Empty;
    public string? GeneratedContent { get; set; }
}
```

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

## 🚀 自动生成的端点

基于DTO配置，系统会自动生成以下端点：

| DTO类型 | 自动生成的端点 | 说明 |
|---------|----------------|------|
| `CreateQuestionDto` | `POST /api/survey/questions/ai-fill` | 题目AI填充 |
| `CreateSurveyDto` | `POST /api/survey/surveys/ai-fill` | 问卷AI填充 |
| `GenerateContentDto` | `POST /api/content/contents/ai-fill` | 内容AI填充 |

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
