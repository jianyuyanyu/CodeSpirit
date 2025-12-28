# CodeSpirit.AiFormFill - 自动时间上下文增强

## 概述

为了解决AI在处理日期时间字段时经常返回过去时间的问题，`AiFormPromptBuilder` 现在会自动检测DTO中的DateTime字段，并在提示词中添加当前时间上下文信息。

## 功能特性

### 自动检测DateTime字段

当DTO包含以下类型的字段时，系统会自动添加时间上下文：
- `DateTime`
- `DateTime?` (可空类型)
- `Nullable<DateTime>`

### 时间上下文内容

系统会自动添加以下时间信息：

```
**重要时间上下文：**
当前日期时间：2025年11月05日 14:30:00（星期三）
- 今天是：2025-11-05
- 当前时刻：14:30:00
- ISO 8601格式：2025-11-05T14:30:00Z

**注意事项：**
- 所有日期必须基于当前时间推算，不能返回过去的日期
- 目标日期应该是未来的时间点
- 日期格式请使用ISO 8601标准格式（如：2025-12-31T00:00:00Z）
```

## 使用场景

### 自动触发场景

所有使用 `AiFormFillAttribute` 的DTO，只要包含DateTime类型的可填充字段，都会自动享受此功能：

```csharp
[AiFormFill(TriggerField = nameof(Description))]
public class CreateGoalDto
{
    public string Description { get; set; }
    
    // 👇 包含DateTime字段，会自动添加时间上下文
    [AiFieldFill(Priority = 2)]
    public DateTime? TargetDate { get; set; }
}
```

### 自定义提示词模式

即使使用自定义提示词模板，时间上下文也会自动追加：

```csharp
[AiFormFill(
    TriggerField = nameof(Description),
    CustomPromptTemplate = @"你是一个目标管理专家...")]
public class CreateGoalDto
{
    // DateTime字段会触发自动时间上下文
    public DateTime? TargetDate { get; set; }
}
```

### 默认提示词模式

使用默认提示词构建器时，时间上下文会自动追加到生成的提示词末尾。

## 实现原理

### 检测逻辑

1. 扫描DTO的所有公共实例属性
2. 检查属性是否为可写
3. 检查属性是否未被忽略（`IgnoreFields`）
4. 检查属性是否启用AI填充（`AiFieldFill.Enabled != false`）
5. 检查属性类型是否为DateTime相关类型

### 时间上下文追加

```csharp
private string AppendCurrentTimeIfNeeded<T>(string basePrompt)
`{
    // 检测DateTime字段
    var dateTimeProperties = GetDateTimeProperties<T>();
    
    if (dateTimeProperties.Count == 0)
    {
        return basePrompt; // 无DateTime字段，不添加
    }`
    
    // 构建时间上下文
    var timeContext = BuildTimeContext();
    
    return basePrompt + timeContext;
}
```

## 效果对比

### 优化前

**AI提示词：**
```
请基于用户输入的目标描述生成合理的完成日期...
```

**AI可能返回：**
```json
{
  "targetDate": "2024-06-01T00:00:00Z"  // ❌ 过去的日期
}
```

### 优化后

**AI提示词（自动追加）：**
```
请基于用户输入的目标描述生成合理的完成日期...

**重要时间上下文：**
当前日期时间：2025年11月05日 14:30:00（星期三）
- 今天是：2025-11-05
...
```

**AI返回：**
```json
{
  "targetDate": "2025-12-31T00:00:00Z"  // ✅ 未来的日期
}
```

## 禁用时间上下文

如果某个DateTime字段不需要时间上下文，可以通过 `AiFieldFill` 特性禁用：

```csharp
[AiFieldFill(Enabled = false)]
public DateTime? SomeDate { get; set; }
```

或者将字段添加到 `AiFormFillAttribute.IgnoreFields` 中：

```csharp
[AiFormFill(
    TriggerField = nameof(Description),
    IgnoreFields = new[] { nameof(SomeDate) })]
public class MyDto
{
    public string Description { get; set; }
    public DateTime? SomeDate { get; set; }
}
```

## 日志输出

当检测到DateTime字段并添加时间上下文时，会输出调试日志：

```
检测到1个DateTime字段，已添加当前时间上下文
```

## 最佳实践

### 1. 使用ISO 8601格式

在提示词和字段描述中明确说明使用ISO 8601格式：

```csharp
[AiFieldFill(
    Priority = 2, 
    CustomDescription = "基于目标复杂度建议的合理完成日期（ISO 8601格式）")]
public DateTime? TargetDate { get; set; }
```

### 2. 明确时间要求

在自定义提示词中强调日期必须是未来时间：

```csharp
CustomPromptTemplate = @"...
- 建议合理的完成日期（必须是未来的日期）
..."
```

### 3. 提供时间范围参考

可以在提示词中给出合理的时间范围建议：

```csharp
CustomPromptTemplate = @"...
建议完成日期：
- 简单任务：1-7天
- 中等任务：1-4周
- 复杂任务：1-3个月
..."
```

## 技术细节

### 时间格式

- 使用 `DateTime.Now` 获取当前时间
- 支持多种格式输出便于AI理解
- 推荐AI返回ISO 8601格式（`yyyy-MM-ddTHH:mm:ssZ`）

### 性能影响

- 仅在检测到DateTime字段时执行
- 时间上下文构建开销极小（\<1ms）
- 不影响无DateTime字段的DTO性能

### 兼容性

- 完全向后兼容
- 不影响现有DTO的行为
- 自动生效，无需修改现有代码

## 相关组件

- `AiFormPromptBuilder` - 提示词构建器（核心实现）
- `AiFormFillService` - AI表单填充服务
- `AiFormFillAttribute` - AI表单填充特性
- `AiFieldFillAttribute` - AI字段填充特性

## 更新日志

### 2025-11-05
- ✨ 新增自动时间上下文增强功能
- 🔧 修复AI返回过去日期的问题
- 📝 添加完整的时间格式参考信息

