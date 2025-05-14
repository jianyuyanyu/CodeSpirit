# CodeSpirit.Amis表单默认值使用指南

## 概述

CodeSpirit.Amis组件库支持在生成表单时设置字段的默认值。通过特性（Attribute）的方式，可以为表单字段配置静态默认值、动态表达式值以及特殊类型的默认值（如当前时间、当前用户等）。

## 基础用法

### 1. 静态默认值

使用 `DefaultValue` 属性设置静态默认值：

```csharp
public class UserDto
{
    [AmisFormField(DefaultValue = "未命名用户")]
    public string Name { get; set; }

    [AmisFormField(DefaultValue = 1)]
    public int Age { get; set; }

    [AmisFormField(DefaultValue = true)]
    public bool IsActive { get; set; }
}
```

### 2. 表达式默认值

使用 `DefaultValueExpression` 属性设置动态表达式：

```csharp
public class PostDto
{
    [AmisFormField(DefaultValueExpression = "${user.name}")]
    public string Author { get; set; }

    [AmisFormField(DefaultValueExpression = "${now | date}")]
    public string PublishDate { get; set; }
}
```

### 3. 特殊默认值类型

使用 `ValueType` 属性设置特殊类型的默认值：

```csharp
public class ArticleDto
{
    [AmisFormField(ValueType = DefaultValueType.CurrentUser)]
    public string Creator { get; set; }

    [AmisFormField(ValueType = DefaultValueType.CurrentDateTime)]
    public DateTime CreateTime { get; set; }
}
```

## 日期时间字段的高级用法

### 1. 相对时间表达式

`AmisDatetimeField` 特性支持使用相对时间表达式：

```csharp
public class EventDto
{
    [AmisDatetimeField(RelativeTime = "today")]
    public DateTime StartDate { get; set; }

    [AmisDatetimeField(RelativeTime = "tomorrow")]
    public DateTime EndDate { get; set; }

    [AmisDatetimeField(RelativeTime = "nextweek")]
    public DateTime DueDate { get; set; }
}
```

支持的相对时间表达式：
- today：今天
- yesterday：昨天
- tomorrow：明天
- lastweek：上周
- nextweek：下周
- lastmonth：上月
- nextmonth：下月

### 2. 时间偏移

可以使用 `TimeOffset` 属性设置分钟级别的时间偏移：

```csharp
public class MeetingDto
{
    // 设置为当前时间后推30分钟
    [AmisDatetimeField(RelativeTime = "today", TimeOffset = 30)]
    public DateTime StartTime { get; set; }

    // 设置为当前时间后推90分钟
    [AmisDatetimeField(RelativeTime = "today", TimeOffset = 90)]
    public DateTime EndTime { get; set; }
}
```

## 完整示例

```csharp
public class TaskDto
{
    [AmisFormField(DefaultValue = "新任务")]
    public string Title { get; set; }

    [AmisFormField(DefaultValueExpression = "${user.department}")]
    public string Department { get; set; }

    [AmisDatetimeField(RelativeTime = "today")]
    public DateTime StartDate { get; set; }

    [AmisDatetimeField(RelativeTime = "today", TimeOffset = 480)] // 8小时后
    public DateTime EndDate { get; set; }

    [AmisFormField(ValueType = DefaultValueType.CurrentUser)]
    public string AssignedTo { get; set; }

    [AmisFormField(DefaultValue = "Normal")]
    public string Priority { get; set; }
}
```

## 默认值优先级

当多个默认值设置同时存在时，系统按以下优先级处理：

1. DefaultValue（最高优先级）
2. DefaultValueExpression
3. ValueType
4. Value（已弃用，最低优先级）

## 注意事项

1. 类型匹配
   - 确保设置的默认值类型与字段类型匹配
   - 对于复杂类型，建议使用 DefaultValueExpression

2. 表达式语法
   - 表达式使用 ${xxx} 语法
   - 支持管道运算符，如 ${now | date}
   - 可以访问上下文变量，如 user、now 等

3. 向后兼容
   - 原有的 Value 属性仍然可用，但已标记为过时
   - AmisDatetimeField 的 UseCurrentTime 属性已弃用，建议使用 ValueType = DefaultValueType.CurrentDateTime

4. 最佳实践
   - 对于简单的静态值，使用 DefaultValue
   - 对于需要动态计算的值，使用 DefaultValueExpression
   - 对于特殊场景（如当前用户、当前时间），使用 ValueType
   - 对于日期时间字段，优先使用 RelativeTime 和 TimeOffset

## 扩展开发

如果需要为特定类型的字段添加自定义的默认值处理逻辑，可以：

1. 继承相应的字段特性类
2. 重写字段工厂类的 HandleDefaultValue 方法

例如：

```csharp
public class CustomDatetimeFieldAttribute : AmisDatetimeFieldAttribute
{
    public string CustomTimeLogic { get; set; }
}

public class CustomDatetimeFieldFactory : AmisDatetimeFieldFactory
{
    protected override void HandleDefaultValue(JObject field, AmisFormFieldAttribute attr)
    {
        var customAttr = attr as CustomDatetimeFieldAttribute;
        if (customAttr?.CustomTimeLogic != null)
        {
            // 实现自定义的默认值处理逻辑
        }
        else
        {
            base.HandleDefaultValue(field, attr);
        }
    }
}
```

## 常见问题

1. 默认值不生效
   - 检查默认值类型是否与字段类型匹配
   - 确认优先级设置是否正确
   - 验证表达式语法是否正确

2. 日期时间偏移计算错误
   - TimeOffset 单位为分钟
   - 正数表示向后偏移，负数表示向前偏移
   - 检查时区设置是否正确

3. 表达式无法解析
   - 确保表达式语法正确
   - 检查所需的上下文变量是否可用
   - 验证管道运算符使用是否正确