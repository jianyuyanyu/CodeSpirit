# CodeSpirit.Amis 表单项组使用指南

## 概述

表单项组功能允许您将相关的表单字段组织成逻辑组，提供更好的用户体验和表单结构。本功能基于 AMIS 的 Group 组件实现，支持不同显示模式、间距控制、条件显示等丰富的配置选项。

![image-20250929130648727](./../../Res/image-20250929130648727.png)

## 核心组件

### 1. FormGroupAttribute 特性类

用于在 DTO 类型上定义表单项组的特性类，支持在类或属性上使用，允许多重定义。

#### 基本属性

- **Name**: 组名称，用于标识组
- **Title**: 组标题，显示在界面上
- **Description**: 组描述
- **Fields**: 包含的字段名称列表，用逗号分隔
- **Order**: 组的排序权重，数值越小越靠前

#### 显示控制

- **Mode**: 显示模式（Normal、Inline、Horizontal）
- **Gap**: 间距大小（None、Small、Normal、Large）
- **Direction**: 方向（Vertical、Horizontal）
- **ShowBorder**: 是否显示边框

#### 交互控制

- **Hidden**: 是否隐藏
- **Disabled**: 是否禁用

#### 条件控制

- **VisibleOn**: 显示条件表达式
- **DisabledOn**: 禁用条件表达式

#### 样式定制

- **ClassName**: 自定义 CSS 类名
- **AdditionalConfig**: 自定义配置，以 JSON 字符串形式提供

### 2. FormGroupFieldFactory 字段工厂

负责创建表单项组的 AMIS 配置的工厂类，已自动注册到 DI 容器中。该工厂实现了 `IAmisFieldFactory` 接口，专门处理 `FormGroupAttribute` 特性。

### 3. FormGroupAttributeProvider 特性提供者

内部类，用于将 `FormGroupAttribute` 包装成 `ICustomAttributeProvider` 接口，便于统一处理。

### 4. FormFieldHelper 增强

`FormFieldHelper` 已增强支持表单项组处理：

- `GetAmisFormFieldsFromProperties(properties, dtoType)`: 生成带表单项组的字段配置
- `ProcessFormGroups(properties, dtoType)`: 专门用于处理表单项组的内部方法
- `ProcessParametersWithGroups(parameters, dtoType)`: 处理方法参数的表单项组

## 使用方法

### 1. 基本使用

```csharp
[FormGroup("basic", "基本信息", "Name,Email,Phone")]
[FormGroup("address", "地址信息", "Province,City,Address")]
public class UserFormDto
{
    [DisplayName("姓名")]
    [Required]
    public string Name { get; set; }

    [DisplayName("邮箱")]
    [EmailAddress]
    public string Email { get; set; }

    [DisplayName("电话")]
    public string Phone { get; set; }

    [DisplayName("省份")]
    public string Province { get; set; }

    [DisplayName("城市")]
    public string City { get; set; }

    [DisplayName("详细地址")]
    public string Address { get; set; }
}
```

### 2. 高级配置

```csharp
[FormGroup("basic", "基本信息", "Name,Email", 
    Order = 1, 
    Mode = FormGroupMode.Horizontal,
    Gap = FormGroupGap.Large,
    ShowBorder = true)]
[FormGroup("optional", "可选信息", "Phone,Remark", 
    Order = 2,
    Hidden = false,
    VisibleOn = "${showOptional}")]
public class AdvancedFormDto
{
    // 属性定义...
}
```

### 3. 自定义配置

```csharp
[FormGroup("custom", "自定义组", "Field1,Field2",
    ClassName = "my-custom-group",
    AdditionalConfig = "{\"style\":{\"backgroundColor\":\"#f5f5f5\"}}")]
public class CustomFormDto
{
    // 属性定义...
}
```

### 4. 实际项目示例 - FormDesignDto

以下是基于 CodeSpirit.ApprovalApi 项目中的 `FormDesignDto` 的实际使用示例：

```csharp
/// <summary>
/// 表单设计DTO - 展示表单项组的实际应用
/// </summary>
[AiFormFill(
    TriggerField = nameof(BusinessScenario),
    UseIndependentLLM = true,
    MaxTokens = 3000,
    EnableCache = true,
    CacheExpirationMinutes = 30,
    GlobalFillPrompt = "根据选定的工作流和业务场景，智能设计符合AMIS规范的审批表单结构"
)]
[FormGroup("workflow", "工作流信息", "Name,Code,Description", Order = 1)]
[FormGroup("requirements", "需求描述", "BusinessScenario,RequiredFields,OptionalFields,SpecialFeatures", Order = 2)]
[FormGroup("formSchema", "表单设计", "FormSchema,FormSchemaReview", Order = 3, Direction = FormGroupDirection.Vertical)]
public class FormDesignDto
{
    /// <summary>
    /// 工作流名称（只读，用于显示）
    /// </summary>
    [DisplayName("工作流名称")]
    [AmisFormField(Type = "static", VisibleOn = "${id}", ColumnRatio = 6)]
    public string? Name { get; set; }

    /// <summary>
    /// 工作流代码（只读，用于显示）
    /// </summary>
    [DisplayName("工作流代码")]
    [AmisFormField(Type = "static", VisibleOn = "${id}", ColumnRatio = 6)]
    public string? Code { get; set; }

    /// <summary>
    /// 工作流描述（只读，用于显示）
    /// </summary>
    [DisplayName("工作流描述")]
    [AmisFormField(Type = "static", VisibleOn = "${id}", ColumnRatio = 12)]
    public string? Description { get; set; }

    /// <summary>
    /// 业务场景描述
    /// </summary>
    [Required]
    [StringLength(500, ErrorMessage = "业务场景描述长度不能超过500个字符")]
    [DisplayName("业务场景描述")]
    [Description("详细描述表单的业务场景和用途")]
    [AiFieldFill(Weight = 4, Priority = 1)]
    [AmisInputTextField(Placeholder = "请详细描述表单的业务场景和用途", ColumnRatio = 12)]
    public string BusinessScenario { get; set; } = string.Empty;

    /// <summary>
    /// 必需字段要求
    /// </summary>
    [StringLength(1000)]
    [DisplayName("必需字段要求")]
    [Description("描述表单中必须包含的字段和信息")]
    [AiFieldFill(Weight = 3, Priority = 2)]
    [AmisTextareaField(Placeholder = "请描述表单必须包含的字段信息", ColumnRatio = 6)]
    public string? RequiredFields { get; set; }

    /// <summary>
    /// 可选字段要求
    /// </summary>
    [StringLength(1000)]
    [DisplayName("可选字段要求")]
    [Description("描述表单中可选的字段和信息")]
    [AiFieldFill(Weight = 2, Priority = 3)]
    [AmisTextareaField(Placeholder = "请描述表单可选的字段信息", ColumnRatio = 6)]
    public string? OptionalFields { get; set; }

    /// <summary>
    /// 特殊功能需求
    /// </summary>
    [StringLength(1000)]
    [DisplayName("特殊功能需求")]
    [Description("表单的特殊功能需求")]
    [AiFieldFill(Weight = 2, Priority = 6)]
    [AmisTextareaField(Placeholder = "请描述表单需要的特殊功能", ColumnRatio = 12)]
    public string? SpecialFeatures { get; set; }

    /// <summary>
    /// 生成的表单Schema
    /// </summary>
    [DisplayName("表单定义")]
    [Description("AI生成的符合AMIS规范的审批表单JSON结构")]
    [AiFieldFill(Weight = 5, Priority = 7)]
    [AmisFormField(Type = "editor", AdditionalConfig = "{\"language\":\"json\"}", ColumnRatio = 6)]
    public string? FormSchema { get; set; }

    /// <summary>
    /// 表单预览
    /// </summary>
    [DisplayName("")]
    [AmisFormField(Type = "amis", AdditionalConfig = "{\"name\":\"formSchema\"}", ColumnRatio = 6)]
    public string? FormSchemaReview { get; set; }
}
```

效果如图所示：

![image-20250929130648727](./../../Res/image-20250929130648727.png)

#### 示例说明

1. **工作流信息组** (`workflow`): 包含只读的工作流基本信息，Order = 1 确保最先显示
2. **需求描述组** (`requirements`): 包含用户输入的需求信息，Order = 2 排在第二位
3. **表单设计组** (`formSchema`): 包含生成的表单定义和预览，Order = 3 排在最后，使用垂直方向布局

这个示例展示了如何在复杂的业务场景中使用表单项组来组织表单结构，提供清晰的用户界面。

## 最佳实践

### 1. 组织原则

- **逻辑分组**: 将相关的字段分组到一起，如基本信息、详细信息、配置选项等
- **有意义的命名**: 使用有意义的组名称和标题，便于理解和维护
- **合理排序**: 通过 `Order` 属性设置组的显示顺序，重要信息优先显示
- **字段完整性**: 确保 `Fields` 属性中的字段名称与 DTO 属性名称完全匹配

### 2. 用户体验

- **视觉层次**: 使用不同的显示模式（Normal、Inline、Horizontal）创建清晰的视觉层次
- **间距控制**: 合理使用 `Gap` 属性控制组内字段的间距，提供舒适的视觉体验
- **条件显示**: 使用 `VisibleOn` 和 `DisabledOn` 实现动态的表单交互
- **方向布局**: 根据内容特点选择合适的 `Direction`（垂直或水平）

### 3. 性能考虑

- **避免过度分组**: 不要创建过多的小组，每组至少包含2-3个相关字段
- **合理使用条件**: 通过条件显示减少初始渲染的字段数量
- **字段复用**: 避免同一字段被包含在多个组中

### 4. 维护性

- **一致性**: 保持组定义与字段定义的一致性，及时更新字段列表
- **文档化**: 为复杂的组配置添加注释说明
- **版本控制**: 在修改组结构时，考虑向后兼容性

### 5. 实际应用建议

基于 `FormDesignDto` 的实践经验：

- **信息展示组**: 对于只读信息（如工作流信息），使用较小的 Order 值优先显示
- **用户输入组**: 将用户需要填写的字段分组，按照填写的逻辑顺序排列
- **结果展示组**: 对于生成的结果或预览内容，使用较大的 Order 值放在最后
- **布局优化**: 对于并排显示的字段（如编辑器和预览），使用垂直方向布局

## 注意事项

1. **字段名称匹配**: `Fields` 属性中的字段名称必须与 DTO 属性名称完全匹配（不区分大小写）
2. **未分组字段**: 没有被任何组包含的字段将显示在所有组的后面
3. **多重分组**: 同一个字段不应该被包含在多个组中，避免重复显示
4. **排序**: 组按照 `Order` 属性排序，未分组的字段显示在最后
5. **权限控制**: 表单项组会遵循字段级别的权限控制
6. **特性位置**: `FormGroupAttribute` 必须应用在类上，不能应用在属性上
7. **字段可见性**: 只有公共属性才会被包含在表单项组中

## 故障排除

### 常见问题

1. **字段未显示在组中**
   - 检查 `Fields` 属性中的字段名称是否与属性名称完全匹配
   - 确认属性是公共属性且有 getter/setter
   - 验证属性是否有 `[JsonIgnore]` 或类似的忽略特性

2. **组不显示**
   - 确认组中至少包含一个有效的字段
   - 检查组的 `Hidden` 属性是否设置为 true
   - 验证 `VisibleOn` 条件表达式是否正确

3. **排序不正确**
   - 检查 `Order` 属性的设置，数值越小越靠前
   - 确认所有组都设置了 `Order` 值

4. **样式问题**
   - 验证 `AdditionalConfig` 中的 JSON 格式是否正确
   - 检查 `ClassName` 是否包含有效的 CSS 类名
   - 确认 antd 主题样式是否正确加载

5. **布局问题**
   - 检查 `Direction` 和 `Mode` 的组合是否合理
   - 验证 `Gap` 设置是否适当
   - 确认字段的 `ColumnRatio` 设置是否正确

### 调试技巧

#### 1. 检查组定义

```csharp
// 检查类型是否定义了表单项组
var hasGroups = typeof(FormDesignDto).HasFormGroups();
Console.WriteLine($"Has form groups: {hasGroups}");

// 获取所有表单项组
var groups = typeof(FormDesignDto).GetFormGroups();
foreach (var group in groups)
{
    Console.WriteLine($"Group: {group.Name}, Title: {group.Title}, Fields: {group.Fields}");
}
```

#### 2. 验证字段归属

```csharp
var properties = typeof(FormDesignDto).GetProperties();
foreach (var property in properties)
{
    var belongsToGroup = property.BelongsToFormGroup(typeof(FormDesignDto));
    var group = property.GetBelongingFormGroup(typeof(FormDesignDto));
    Console.WriteLine($"Property: {property.Name}, Belongs to group: {belongsToGroup}, Group: {group?.Name}");
}
```

#### 3. 调试生成的配置

```csharp
// 获取生成的配置
var properties = typeof(FormDesignDto).GetProperties();
var fieldsWithGroups = formFieldHelper.GetAmisFormFieldsFromProperties(properties, typeof(FormDesignDto));

// 输出配置用于调试
foreach (var field in fieldsWithGroups)
{
    Console.WriteLine($"Field Type: {field["type"]}, Name: {field["name"]}, Label: {field["label"]}");
    if (field["body"] != null)
    {
        Console.WriteLine($"Group Body: {field["body"]}");
    }
}
```

#### 4. 验证 JSON 配置

```csharp
// 验证 AdditionalConfig 的 JSON 格式
var group = typeof(FormDesignDto).GetFormGroups().FirstOrDefault();
if (!string.IsNullOrEmpty(group?.AdditionalConfig))
{
    try
    {
        var config = JObject.Parse(group.AdditionalConfig);
        Console.WriteLine("JSON config is valid");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Invalid JSON config: {ex.Message}");
    }
}
```

## 更新日志

### v1.0.0 - 初始版本
- 支持基本的表单项组功能
- 实现 `FormGroupAttribute` 特性类
- 支持多种显示模式（Normal、Inline、Horizontal）
- 支持间距控制（None、Small、Normal、Large）
- 支持方向控制（Vertical、Horizontal）

### v1.1.0 - 功能增强
- 添加 `FormGroupFieldFactory` 字段工厂
- 实现 `FormGroupAttributeProvider` 特性提供者
- 增强 `FormFieldHelper` 支持表单项组处理
- 提供丰富的扩展方法（HasFormGroups、GetFormGroups 等）
- 完整的 AMIS Group 组件集成

### v1.2.0 - 实际应用
- 基于 `FormDesignDto` 的实际项目应用
- 支持条件显示（VisibleOn、DisabledOn）
- 支持自定义配置（AdditionalConfig）
- 改进的调试和故障排除功能
- 完善的文档和示例

### 特性支持
- ✅ 多重表单项组定义
- ✅ 字段自动分组和排序
- ✅ 条件显示和禁用
- ✅ 自定义样式和配置
- ✅ 完整的扩展方法支持
- ✅ 调试和故障排除工具
- ✅ 与 AI 表单填充功能集成
