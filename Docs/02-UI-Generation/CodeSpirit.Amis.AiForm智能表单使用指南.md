# CodeSpirit.Amis.AiForm 智能表单使用指南

## 概述

`AiForm` 是 CodeSpirit.Amis 框架的智能表单功能，专为AI驱动的长时间处理任务设计。它自动生成一个多步骤的用户界面，包含表单输入、进度监控、日志显示和结果展示等功能。

## 功能特点

- 🎯 **自动化UI生成**：基于 `@OperationAttribute` 自动生成完整的AI表单界面
- 📊 **多步骤界面**：表单面板、步骤进度、日志面板、结果展示
- 🔄 **实时轮询**：自动轮询AI任务状态，实时更新进度和日志
- ⏱️ **超时控制**：支持自定义轮询间隔和最大轮询时间
- 🚀 **异步处理**：避免长时间AI响应导致的请求超时

## 使用方法

### 1. 控制器端配置

在控制器方法上使用 `@HeaderOperation` 特性：

```csharp
[HttpPost("ai/generate-async")]
[HeaderOperation("AI智能生成问卷", "aiForm", 
    Icon = "fa-solid fa-magic",
    StatusApi = "/survey/api/survey/Surveys/ai/task-status",
    PollingInterval = 2000,
    MaxPollingTime = 300000,
    FormTitle = "问卷生成配置",
    StepsTitle = "AI生成进度",
    LogTitle = "生成日志",
    ResultTitle = "生成结果")]
[DisplayName("AI智能生成问卷")]
public async Task<ActionResult<ApiResponse<string>>> GenerateSurveyAsync([FromBody] GenerateSurveyRequest request)
{
    var taskId = await _surveyAiGeneratorService.GenerateAsync(request);
    return SuccessResponse(taskId);
}
```

### 2. AI表单特有属性

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `StatusApi` | string | - | **必填**，AI任务状态查询API地址 |
| `PollingInterval` | int | 2000 | 轮询间隔（毫秒） |
| `MaxPollingTime` | int | 300000 | 最大轮询时间（毫秒） |
| `SuccessRedirect` | string | - | 成功后跳转的URL |
| `FormTitle` | string | "配置信息" | 表单面板标题 |
| `StepsTitle` | string | "处理进度" | 步骤面板标题 |
| `LogTitle` | string | "处理日志" | 日志面板标题 |
| `ResultTitle` | string | "处理结果" | 结果面板标题 |

### 3. 后端服务实现

#### 3.1 AI任务服务

实现 `IAiTaskService` 接口用于管理任务状态：

```csharp
public class AiTaskService : IAiTaskService, ISingletonDependency
{
    public async Task<string> CreateTaskAsync(string taskType, object parameters)
    {
        // 创建任务并返回任务ID
    }

    public async Task<AiTaskStatusDto?> GetTaskStatusAsync(string taskId)
    {
        // 返回任务状态信息
    }

    // 其他方法实现...
}
```

#### 3.2 AI生成服务

继承 `BaseAiGeneratorService` 基类：

```csharp
public class SurveyAiGeneratorService : BaseAiGeneratorService<GenerateSurveyRequest, GeneratedSurveyDto>, IScopedDependency
{
    protected override string GetTaskType() => "问卷生成";

    protected override async Task<GeneratedSurveyDto> DoGenerateAsync(
        GenerateSurveyRequest request, 
        Action<double, string>? progressCallback = null)
    {
        // 实现具体的AI生成逻辑
        // 使用 progressCallback 报告进度
        progressCallback?.Invoke(0.3, "正在分析需求...");
        
        // 生成逻辑...
        
        return generatedSurvey;
    }
}
```

#### 3.3 状态查询API

创建状态查询接口：

```csharp
[HttpGet("ai/task-status")]
[DisplayName("获取AI任务状态")]
public async Task<ActionResult<ApiResponse<AiTaskStatusDto>>> GetTaskStatus([FromQuery] string taskId)
{
    var status = await _aiTaskService.GetTaskStatusAsync(taskId);
    if (status == null)
    {
        return BadResponse<AiTaskStatusDto>("任务不存在", statusCode: 404);
    }
    
    return SuccessResponse(status);
}
```

## UI界面说明

### 界面结构

AI表单生成的界面包含4个标签页：

1. **表单面板**：显示输入表单和"开始生成"按钮
2. **步骤面板**：显示4步进度条（初始化 → AI处理 → 结果处理 → 完成）
3. **日志面板**：实时显示任务执行日志
4. **结果面板**：显示任务状态、进度、耗时和最终结果

### 交互流程

1. 用户在表单面板填写参数
2. 点击"开始生成"按钮
3. 自动切换到步骤面板显示进度
4. 实时更新日志面板的执行日志
5. 完成后在结果面板显示结果和详情链接

### 轮询机制

使用Amis的标准事件动作系统实现：
- **循环动作**：使用 `loop` 动作类型进行循环轮询
- **等待间隔**：使用 `wait` 动作设置轮询间隔（`PollingInterval` 毫秒）
- **AJAX查询**：使用 `ajax` 动作查询任务状态
- **条件判断**：使用 `condition` 动作检查任务状态并执行相应操作
- **自动停止**：任务完成或失败时自动停止轮询
- **超时控制**：最大循环次数为 `MaxPollingTime / PollingInterval`

## 任务状态

### AiTaskStatus 枚举

- `Pending` (0)：待开始
- `InProgress` (1)：进行中
- `Completed` (2)：已完成
- `Failed` (3)：失败
- `Cancelled` (4)：已取消

### AiTaskStatusDto 结构

```csharp
public class AiTaskStatusDto
{
    public string TaskId { get; set; }           // 任务ID
    public AiTaskStatus Status { get; set; }     // 任务状态
    public string StatusText { get; set; }       // 状态描述
    public int Step { get; set; }                // 当前步骤（0-3）
    public int Progress { get; set; }            // 进度百分比（0-100）
    public List<string> Logs { get; set; }       // 任务日志
    public object? Result { get; set; }          // 任务结果
    public DateTime StartTime { get; set; }      // 开始时间
    public DateTime? EndTime { get; set; }       // 结束时间
    public string ElapsedTime { get; set; }      // 耗时显示
    public string? DetailUrl { get; set; }       // 结果详情页URL
}
```

## 最佳实践

### 1. 任务管理

- 使用 `ISingletonDependency` 确保任务状态在应用程序生命周期内保持
- 定期清理过期的任务记录
- 对于分布式部署，考虑使用Redis存储任务状态

### 2. 进度报告

- 在AI生成过程中适时调用 `progressCallback` 报告进度
- 日志消息应该简洁明了，便于用户理解
- 错误处理要及时更新任务状态为失败

### 3. 超时设置

- 根据实际AI响应时间合理设置 `MaxPollingTime`
- `PollingInterval` 不宜设置过小，避免频繁请求
- 考虑网络延迟因素调整轮询参数

### 4. 用户体验

- 提供有意义的状态文本和日志信息
- 完成后提供合适的详情页面链接
- 支持任务取消功能（可选）

## 错误处理

### 常见错误

1. **依赖注入错误**：确保 `IAiTaskService` 和具体的AI生成服务已正确注册
2. **状态API找不到**：检查 `StatusApi` 路径是否正确
3. **轮询超时**：适当增加 `MaxPollingTime` 值
4. **任务丢失**：检查任务存储机制，确保任务状态持久化

### 调试技巧

- 查看浏览器网络面板的轮询请求
- 检查后端日志的任务创建和状态更新
- 使用开发者工具监控前端状态更新

## 示例代码

完整的示例可以参考：
- `Src/ApiServices/CodeSpirit.SurveyApi/Controllers/SurveysController.cs`
- `Src/ApiServices/CodeSpirit.SurveyApi/Services/Implementations/SurveyAiGeneratorService.cs`
- `Src/CodeSpirit.Shared/Services/AiTaskService.cs`

---

通过 AI表单功能，您可以轻松为长时间运行的AI任务创建用户友好的界面，提供实时反馈和进度监控，显著提升用户体验。