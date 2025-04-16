# CodeSpirit（码灵）时间处理机制

## 概述

在分布式系统中，时间处理是一个复杂而重要的问题。CodeSpirit框架采用了统一的时间处理机制，以确保前后端交互过程中时间数据的一致性。默认情况下，CodeSpirit采用UTC时间作为内部存储和处理的标准，而在界面展示时转换为本地时间。

## 时间处理原则

1. **统一使用UTC时间**：系统内部所有时间存储和处理均使用UTC时间
2. **自动转换**：前端提交的本地时间自动转换为UTC时间进行存储
3. **展示一致性**：后端返回的UTC时间自动转换为本地时间进行展示

## 实现机制

CodeSpirit框架提供了三种主要的时间处理机制：

### 方案一：DateTimeModelBinder（推荐）

在控制器查询参数中，系统已全局注册`DateTimeModelBinderProvider`，该组件会自动处理DateTime类型参数的时区转换。

#### 工作原理

1. `DateTimeModelBinder`会拦截所有`DateTime`和`DateTime?`类型的参数
2. 解析前端传入的本地时间
3. 自动将其转换为UTC时间

#### 配置位置

在`Src/CodeSpirit.Shared/Extensions/ServiceCollectionExtensions.cs`中注册：

```csharp
public static IServiceCollection ConfigureDefaultControllers(this IServiceCollection services, Action<MvcOptions> optionsAction = null)
{
    services.AddControllers(options =>
    {
        // ...其他配置
        options.ModelBinderProviders.Insert(0, new DateTimeModelBinderProvider());
        // ...
    })
    // ...
}
```

#### 使用示例

由于已全局注册，所有控制器方法中的DateTime参数都会自动使用此绑定器：

```csharp
[HttpGet]
public async Task<ActionResult<ApiResponse<PageList<ExamSettingDto>>>> GetExamSettings(
    [FromQuery] ExamSettingQueryDto queryDto)
{
    // queryDto中的所有DateTime属性已自动转换为UTC时间
    // ...处理逻辑
}
```

### 方案二：AmisDatetimeFieldAttribute

对于需要在表单中进行精确控制的日期时间字段，可以使用`AmisDatetimeFieldAttribute`特性，并设置其`Utc`属性。

#### 使用示例

```csharp
/// <summary>
/// 开始时间范围起始
/// </summary>
[DisplayName("开始时间")]
[AmisDatetimeFieldAttribute(Utc = true)]
public DateTime? StartTimeFrom { get; set; }
```

当`Utc`设置为`true`时，前端表单提交的值会作为UTC时间处理。这在跨时区应用中特别有用。

### UTCToLocalDateTimeConverter

CodeSpirit框架使用`UTCToLocalDateTimeConverter`处理序列化和反序列化过程中的时间转换。

#### 工作原理

1. **反序列化（ReadJson）**：
   - 将前端传来的字符串解析为DateTime
   - 将其指定为本地时间（Local）
   - 自动转换为UTC时间后存储

2. **序列化（WriteJson）**：
   - 将内部存储的UTC时间转换为本地时间
   - 格式化为"yyyy-MM-dd HH:mm:ss"格式的字符串
   - 返回给前端显示

#### 注册位置

在`ServiceCollectionExtensions.cs`中配置：

```csharp
.AddNewtonsoftJson(options =>
{
    // ...其他设置
    options.SerializerSettings.Converters.Add(new UTCToLocalDateTimeConverter());
    // ...
})
```

## 最佳实践

1. **新增字段时的处理**：
   - 对于一般查询字段，直接使用`DateTime`类型，系统会自动处理时区转换
   - 对于需要特殊处理的字段，使用`AmisDatetimeFieldAttribute`并配置`Utc`属性

2. **数据库交互**：
   - 所有存入数据库的时间应为UTC时间
   - 从数据库读取的时间应视为UTC时间并在显示前转换

3. **序列化处理**：
   - 使用框架内置的`UTCToLocalDateTimeConverter`进行序列化和反序列化
   - 避免自定义时间处理逻辑，保持一致性

## 注意事项

1. **时区检测**：
   - 前端时区基于用户浏览器设置自动检测
   - 可通过`TimeZone`属性手动指定时区（如"+0800"）

2. **格式化输出**：
   - 默认格式为"yyyy-MM-dd HH:mm:ss"
   - 可通过`DisplayFormat`属性自定义格式

3. **特殊场景处理**：
   - 对于跨日期的时区问题，确保正确设置`Utc`属性
   - 处理历史数据时注意时区一致性

## 示例：完整的时间处理流程

1. **前端表单提交**：
   - 用户在北京时区（UTC+8）选择"2023-01-01 10:00:00"

2. **请求到达后端**：
   - `DateTimeModelBinder`将本地时间转换为UTC时间："2023-01-01 02:00:00Z"

3. **数据库存储**：
   - 以UTC时间"2023-01-01 02:00:00Z"存储

4. **查询并返回前端**：
   - 从数据库读取UTC时间"2023-01-01 02:00:00Z"
   - `UTCToLocalDateTimeConverter`将其转换回本地时间"2023-01-01 10:00:00"
   - 前端显示本地时间"2023-01-01 10:00:00"

通过这种机制，无论用户位于哪个时区，系统都能保证时间数据的一致性和准确性。 