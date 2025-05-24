# AMIS 列自动推断功能说明

## 概述

`CreateAmisColumn` 方法是 CodeSpirit.Amis 组件中用于自动生成 AMIS 表格列配置的核心方法。该方法能够根据 .NET 属性的类型、特性和命名约定，自动推断出最适合的 AMIS 列类型和配置。

## 主要功能

### 1. 聚合器字段支持
- 自动识别 `AggregateFieldAttribute` 特性
- 支持自定义字段名称和显示模板
- 用于显示关联数据，如用户信息等

### 2. 日期列智能配置优化
- **特性支持**：自动识别 `DateColumnAttribute` 特性
- **智能格式推断**：根据属性名称模式智能推断最适合的日期格式：
  - **完整时间格式** (`YYYY-MM-DD HH:mm:ss`)：包含 "time"、"created"、"updated"、"modified"、"login"、"accessed"、"expired"、"finished"、"started"、"ended" 的字段
  - **纯日期格式** (`YYYY-MM-DD`)：包含 "date"、"birth"、"anniversary"、"deadline" 的字段
  - **年份格式** (`YYYY`)：包含 "year" 的字段
  - **年月格式** (`YYYY-MM`)：包含 "month" 的字段
- **相对时间显示**：自动为适合的字段启用相对时间显示（如"3小时前"、"2天前"）
  - 适用字段：包含 "created"、"updated"、"modified"、"last"、"recent"、"login"、"accessed"、"published" 的字段
  - 自动设置更新频率（每分钟更新一次）
- **智能占位符**：根据字段语义自动设置合适的占位符文本
  - "birth" → "出生日期"
  - "deadline" → "截止日期"
  - "created" → "创建时间"
  - "login" → "登录时间"
- **特殊字段优化**：
  - **排序优化**：为创建时间、更新时间等自动启用排序，创建时间默认降序
  - **过期状态指示**：为过期时间、截止日期等字段添加颜色状态指示（过期显示红色警告图标，未过期显示绿色确认图标）
  - **历史记录优化**：为历史、日志、审计相关时间字段自动启用相对时间显示

### 3. 标签列（Tags）支持
- 自动识别名为 "Tags" 的字符串数组属性
- 支持 `TagsColumnAttribute` 特性配置
- 自动生成标签样式和颜色配置

### 4. 枚举类型映射
- 自动将枚举类型映射为 AMIS 的 mapping 列
- 支持可空枚举类型
- 自动生成枚举值到显示名称的映射

### 5. 图片和头像字段
- 自动识别图片相关字段（包含 "Image"、"Avatar" 的属性名）
- 支持 `DataType.ImageUrl` 特性
- 自动配置头像和图片的默认参数

### 6. 数值类型智能格式化
- **金额字段**：自动识别包含 "Amount"、"Price"、"Cost"、"Fee"、"Money" 的字段，格式化为货币显示
- **时长字段**：自动识别包含 "Duration"、"Time"、"Elapsed"、"Delay"、"Latency"、"Timeout" 等的字段，支持：
  - 智能单位显示（毫秒、秒、分钟、小时）
  - 根据字段名称自动选择合适的单位
  - 支持所有数值类型（`int`、`long`、`double`、`decimal` 等）
  - `Duration` → "123 ms"，`Seconds` → "12 s"，`Minutes` → "5 min"
- **百分比字段**：自动识别包含 "Rate"、"Percent"、"Ratio" 的字段，格式化为百分比显示

### 7. 状态字段
- 自动识别包含 "Status"、"State" 的字符串字段
- 设置为 AMIS 的 status 类型

### 8. 链接字段
- **URL 链接**：自动识别包含 "Url"、"Link"、"Website" 的字段或 `DataType.Url` 特性
- **邮箱链接**：自动识别包含 "Email"、"Mail" 的字段或 `DataType.EmailAddress` 特性
- **电话链接**：自动识别包含 "Phone"、"Tel"、"Mobile" 的字段或 `DataType.PhoneNumber` 特性，支持：
  - 点击拨号功能（`tel:` 链接）
  - 电话图标显示
  - 一键复制功能
  - 优雅的样式设计

### 9. 密码字段脱敏
- 自动识别包含 "Password"、"Pwd" 的字段
- 显示为 "******" 并禁用排序

### 10. 长文本字段处理
- 自动识别包含 "Description"、"Content"、"Note"、"Remark"、"Comment" 的字段
- 支持文本截断和弹窗显示完整内容
- 根据 `MaxLengthAttribute` 自动配置截断长度

### 11. 集合类型支持
- **List<string> / string[]**：字符串数组/集合自动显示为 each 类型，支持：
  - 标签样式显示（badge样式）
  - 自动截断超出数量的项目（默认最多显示10项）
  - 溢出提示（"...还有X项"）
  - 空数据占位符
- **List 类型**：复杂对象集合显示为 list 类型，支持：
  - 自动推断标题和副标题字段
  - 智能头像字段识别（avatar, profileImage, image）
  - 描述字段支持
  - 默认操作按钮配置
  - 占位符自定义
- **基础类型集合**：显示为 each 类型（循环显示）

### 12. 复杂对象类型
- **自定义类对象**：复杂对象类型自动显示为 json 列
- **结构体类型**：自定义结构体（如 Point、Rectangle 等）显示为 json 列
- **匿名对象**：匿名类型对象显示为 json 列
- **排除类型**：字符串、数组、集合、已处理的特殊类型除外

### 13. 特殊数据类型
- **JSON 字段**：自动识别包含 "json"、"config"、"setting" 的字段
- **HTML 字段**：自动识别包含 "html"、"rich" 的字段
- **颜色字段**：自动识别包含 "color"、"colour" 的字段
- **GUID 类型**：显示为文本
- **TimeSpan 类型**：显示为文本

## 类型推断优先级

1. **显式特性配置**：优先使用开发者明确指定的列特性
2. **DataType 特性**：根据 `DataTypeAttribute` 推断类型
3. **属性名称模式**：根据属性名称的关键词推断类型
4. **属性数据类型**：根据 .NET 数据类型进行基础映射

## 支持的 AMIS 列类型

- `text` - 文本显示
- `tpl` - 模板显示（用于格式化）
- `date` - 日期显示
- `mapping` - 枚举映射
- `switch` - 开关（布尔值）
- `link` - 链接
- `image` - 图片
- `avatar` - 头像
- `status` - 状态
- `each` - 循环显示（标签、数组）
- `list` - 列表显示
- `json` - JSON 显示
- `html` - HTML 显示
- `color` - 颜色显示

## 使用示例

### 基本示例

```csharp
public class UserDto
{
    [DisplayName("用户ID")]
    public long Id { get; set; }

    [DisplayName("用户名")]
    public string UserName { get; set; }

    [DisplayName("邮箱")]
    [DataType(DataType.EmailAddress)]
    public string Email { get; set; }

    [DisplayName("手机号码")]
    [DataType(DataType.PhoneNumber)]
    public string Phone { get; set; }

    [DisplayName("头像")]
    [DataType(DataType.ImageUrl)]
    public string Avatar { get; set; }

    [DisplayName("状态")]
    public string Status { get; set; }

    [DisplayName("创建时间")]
    public DateTime CreatedTime { get; set; }

    [DisplayName("金额")]
    public decimal Amount { get; set; }

    [DisplayName("标签")]
    public List<string> Tags { get; set; }

    [DisplayName("技能列表")]
    public List<string> Skills { get; set; }

    [DisplayName("用户类型")]
    public UserType UserType { get; set; }

    [DisplayName("联系人列表")]
    [ListColumn("name", "email", "暂无联系人")]
    public List<ContactDto> Contacts { get; set; }

    [DisplayName("用户设置")]
    public UserSettings Settings { get; set; }

    [DisplayName("地址信息")]
    public Address Address { get; set; }
}
```

### 时长字段示例

```csharp
public class PerformanceDto
{
    [DisplayName("请求持续时间")]
    public double Duration { get; set; }  // → "123.45 ms"

    [DisplayName("响应时间(秒)")]
    public double ResponseTimeSeconds { get; set; }  // → "1.23 s"

    [DisplayName("处理时长(分钟)")]
    public double ProcessingMinutes { get; set; }  // → "5.67 min"

    [DisplayName("超时时间(小时)")]
    public double TimeoutHours { get; set; }  // → "2.5 h"

    [DisplayName("网络延迟")]
    public double NetworkLatency { get; set; }  // → "45.2 ms"

    // 整数类型时长字段
    [DisplayName("响应时间(毫秒)")]
    public int ResponseTimeMilliseconds { get; set; }  // → "150 ms"

    [DisplayName("等待时间(秒)")]
    public long WaitTimeSeconds { get; set; }  // → "30 s"

    [DisplayName("运行时长(分钟)")]
    public int RunTimeMinutes { get; set; }  // → "10 min"

    [DisplayName("成功率")]
    public double SuccessRate { get; set; }  // → "95.6%"
}
```

### 日期时间字段智能优化示例

```csharp
public class EventDto
{
    [DisplayName("事件ID")]
    public long Id { get; set; }

    // 自动显示为 YYYY-MM-DD HH:mm:ss 格式，启用相对时间，默认降序排序
    [DisplayName("创建时间")]
    public DateTime CreatedTime { get; set; }  // → "2024-01-15 14:30:25 (3小时前)"

    // 自动显示为 YYYY-MM-DD HH:mm:ss 格式，启用相对时间
    [DisplayName("最后更新时间")]
    public DateTime LastUpdatedTime { get; set; }  // → "2024-01-15 16:45:10 (1小时前)"

    // 自动显示为 YYYY-MM-DD 格式，智能占位符
    [DisplayName("出生日期")]
    public DateTime BirthDate { get; set; }  // → "1990-05-20" (占位符: "出生日期")

    // 自动显示为 YYYY-MM-DD 格式，智能占位符
    [DisplayName("截止日期")]
    public DateTime DeadlineDate { get; set; }  // → "2024-02-01" (占位符: "截止日期")

    // 过期时间 - 自动添加状态指示（红色/绿色图标）
    [DisplayName("过期时间")]
    public DateTime ExpiredTime { get; set; }  // → 红色警告图标 + "2024-01-10 23:59:59" (已过期)

    // 登录时间 - 启用相对时间显示
    [DisplayName("最后登录时间")]
    public DateTime LastLoginTime { get; set; }  // → "2024-01-15 09:30:15 (8小时前)"

    // 年份字段 - 显示为 YYYY 格式
    [DisplayName("毕业年份")]
    public DateTime GraduationYear { get; set; }  // → "2015"

    // 月份字段 - 显示为 YYYY-MM 格式
    [DisplayName("入职月份")]
    public DateTime JoinMonth { get; set; }  // → "2020-03"

    // 审计日志时间 - 自动启用相对时间和频繁更新
    [DisplayName("审计时间")]
    public DateTime AuditTime { get; set; }  // → "2024-01-15 17:25:30 (5分钟前)" (每分钟更新)

    // 历史记录时间 - 自动启用相对时间
    [DisplayName("历史记录时间")]
    public DateTime HistoryTime { get; set; }  // → "2024-01-14 10:15:20 (1天前)"
}
```

### AmisColumnAttribute 高级配置示例

```csharp
public class AuditLogDto
{
    [DisplayName("日志ID")]
    [AmisColumn(Hidden = true)]  // 隐藏列
    public long Id { get; set; }

    [DisplayName("事件类型")]
    [AmisColumn(Fixed = "left")]  // 固定在左侧
    public string EventType { get; set; }

    [DisplayName("权限列表")]
    [AmisColumn(Hidden = true)]  // 即使是List<string>也会被隐藏
    public List<string> Permissions { get; set; }

    [DisplayName("请求持续时间")]
    [AmisColumn(
        BackgroundScaleMin = 0,
        BackgroundScaleMax = 100,
        BackgroundScaleColors = new[] { "#FFEF9C", "#FF7127" })]
    public double Duration { get; set; }

    [DisplayName("请求头")]
    [AmisColumn(Type = "json", Copyable = true, Toggled = false)]
    public string Headers { get; set; }
}
```

## 生成的 AMIS 列配置示例

以上 DTO 会自动生成相应的 AMIS 列配置：

### 基本类型
- `Email` → link 类型（mailto 链接）
- `Phone` → tpl 类型（带图标的电话链接，支持点击拨号和复制）
- `Avatar` → avatar 类型
- `Status` → status 类型
- `CreatedTime` → date 类型（YYYY-MM-DD HH:mm:ss 格式）

### 数值类型
- `Amount` → tpl 类型（货币格式化：¥123.45）
- `Duration` → tpl 类型（时长格式化：123.45 ms）
- `SuccessRate` → tpl 类型（百分比格式化：95.6%）

### 日期时间类型
- `CreatedTime` → date 类型（YYYY-MM-DD HH:mm:ss 格式，启用相对时间显示，默认降序排序）
- `BirthDate` → date 类型（YYYY-MM-DD 格式，占位符："出生日期"）
- `ExpiredTime` → tpl 类型（带状态指示的时间显示，过期时显示红色警告图标）
- `LastLoginTime` → date 类型（启用相对时间显示：fromNow: true）
- `GraduationYear` → date 类型（YYYY 格式）
- `AuditTime` → date 类型（相对时间显示，每分钟更新：updateFrequency: 60000）

### 集合类型
- `Tags` → each 类型（标签显示）
- `Skills` → each 类型（标签显示）
- `Contacts` → list 类型（联系人列表，自动配置头像、标题、副标题和操作按钮）

### 复杂对象
- `Settings` → json 类型（用户设置）
- `Address` → json 类型（地址信息）

### 隐藏字段
- `Id` → hidden: true（隐藏主键）
- `Permissions` → hidden: true（隐藏权限列表，即使是 List<string>）

## 扩展性

该推断系统具有良好的扩展性，开发者可以：

1. **通过添加新的列特性**来支持更多 AMIS 列类型
2. **通过修改属性名称模式识别**添加更多字段名称匹配规则
3. **通过 `AmisColumnAttribute` 覆盖自动推断**的配置，提供完全的控制权
4. **通过特定列特性**（如 `DateColumnAttribute`、`TagsColumnAttribute`）提供精确配置

### 配置优先级保障

系统确保 `AmisColumnAttribute` 的配置始终具有最高优先级：

```csharp
[DisplayName("特殊字段")]
[AmisColumn(Type = "text", Hidden = false, Copyable = true)]
public List<string> SpecialField { get; set; }
// 即使是 List<string> 类型，也会按照 AmisColumn 的配置生成
```

这种设计既提供了智能的默认行为，又保持了足够的灵活性供开发者自定义。