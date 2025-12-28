# ExampleValueAttribute 示例值特性使用指南

## 📋 概述

`ExampleValueAttribute` 是一个用于为属性指定示例值的特性，主要用于批量导入模板生成。通过在 DTO 属性上标注示例值，可以让生成的 Excel 导入模板包含更有意义的示例数据，帮助用户理解每个字段的填写格式。

## 🎯 核心功能

- **自定义示例值**：为属性指定具体的示例值
- **模板生成优化**：生成的 Excel 模板包含有意义的示例数据
- **优先级机制**：特性中定义的示例值优先于自动生成的值

## 📝 基本用法

### 1. 引入命名空间

```csharp
using CodeSpirit.Core.Attributes;
using Newtonsoft.Json;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
```

### 2. 在 BatchImportItemDto 中使用

```csharp
/// <summary>
/// 题目批量导入项 DTO
/// </summary>
public class QuestionBatchImportItemDto
{
    /// <summary>
    /// 题目内容
    /// </summary>
    [Required(ErrorMessage = "题目内容不能为空")]
    [StringLength(2000, ErrorMessage = "题目内容最多2000字符")]
    [DisplayName("题目内容")]
    [JsonProperty("题目内容")]
    [ExampleValue("地球围绕太阳转一圈需要多长时间？")]
    public string Content { get; set; } = null!;

    /// <summary>
    /// 题目类型
    /// </summary>
    [Required(ErrorMessage = "请选择题目类型")]
    [DisplayName("题目类型")]
    [JsonProperty("题目类型")]
    [ExampleValue("单选题")]
    public string QuestionType { get; set; } = null!;

    /// <summary>
    /// 难度等级
    /// </summary>
    [Required(ErrorMessage = "请选择题目难度")]
    [DisplayName("难度")]
    [JsonProperty("难度")]
    [ExampleValue("2")]
    public int DifficultyLevel { get; set; }

    /// <summary>
    /// 标签列表
    /// </summary>
    [DisplayName("标签")]
    [JsonProperty("标签")]
    [ExampleValue("天文学,科学")]
    public string Tags { get; set; }

    /// <summary>
    /// 答案
    /// </summary>
    [Required(ErrorMessage = "请填写正确答案")]
    [StringLength(1000, ErrorMessage = "正确答案最多1000字符")]
    [DisplayName("正确答案")]
    [JsonProperty("正确答案")]
    [ExampleValue("365天")]
    public string Answer { get; set; } = null!;

    /// <summary>
    /// 解析说明
    /// </summary>
    [StringLength(2000, ErrorMessage = "解析最多2000字符")]
    [DisplayName("解析")]
    [JsonProperty("解析")]
    [ExampleValue("地球公转周期约为365.25天，因此每年都是365天，每4年有一个闰年")]
    public string? Analysis { get; set; }
}
```

### 3. 用户信息批量导入示例

```csharp
/// <summary>
/// 批量导入用户 DTO
/// </summary>
public class UserBatchImportItemDto
{
    /// <summary>
    /// 用户名
    /// </summary>
    [JsonProperty("用户名")]
    [Required]
    [MaxLength(100)]
    [ExampleValue("zhangsan")]
    public string UserName { get; set; }

    /// <summary>
    /// 电子邮箱
    /// </summary>
    [JsonProperty("邮箱")]
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    [ExampleValue("zhangsan@example.com")]
    public string Email { get; set; }

    /// <summary>
    /// 手机号码
    /// </summary>
    [JsonProperty("手机号码")]
    [Phone]
    [MaxLength(20)]
    [ExampleValue("13800138000")]
    public string PhoneNumber { get; set; }

    /// <summary>
    /// 姓名
    /// </summary>
    [JsonProperty("姓名")]
    [Required]
    [MaxLength(20)]
    [ExampleValue("张三")]
    public string Name { get; set; }

    /// <summary>
    /// 身份证号码
    /// </summary>
    [JsonProperty("身份证号码")]
    [MaxLength(18)]
    [ExampleValue("110101199001011234")]
    public string IdNo { get; set; }

    /// <summary>
    /// 性别
    /// </summary>
    [JsonProperty("性别")]
    [ExampleValue("男")]
    public Gender Gender { get; set; }
}
```

## 🔄 工作原理

### 示例值生成优先级

1. **第一优先级**：`ExampleValueAttribute` 中定义的示例值
2. **第二优先级**：根据属性名称智能推断（如包含"姓名"、"手机"、"邮箱"等）
3. **第三优先级**：根据属性类型生成默认值（如字符串 → "示例文本"）

### 生成流程

```csharp
private string? GenerateExampleValue(PropertyInfo property)
{
    // 1. 优先使用 ExampleValueAttribute 特性中定义的示例值
    var exampleAttr = property.GetCustomAttribute<ExampleValueAttribute>();
    if (exampleAttr != null && !string.IsNullOrEmpty(exampleAttr.Value))
    {
        return exampleAttr.Value;
    }

    // 2. 根据属性名称生成示例值
    var propertyName = property.Name.ToLower();
    if (propertyName.Contains("name") || propertyName.Contains("姓名"))
        return "张三";
    // ... 更多规则

    // 3. 根据类型生成示例值
    if (type == typeof(string))
        return "示例文本";
    // ... 更多类型
}
```

## 📊 实际效果

使用 `ExampleValueAttribute` 后，生成的 Excel 导入模板将包含：

| 题目内容 | 题目类型 | 难度 | 标签 | 正确答案 | 解析 |
|---------|---------|------|------|---------|------|
| 地球围绕太阳转一圈需要多长时间？ | 单选题 | 2 | 天文学,科学 | 365天 | 地球公转周期约为365.25天... |

每个列标题还会包含悬停注释，显示：
- 字段名称
- 是否必填
- 字段说明（如果有 Description 特性）
- 示例值

## ✅ 最佳实践

1. **提供有意义的示例**：示例值应该能清晰地说明字段的填写格式和内容要求

   ```csharp
   // ✅ 好的示例
   [ExampleValue("2024-01-15")]
   public string BirthDate { get; set; }
   
   // ❌ 不好的示例
   [ExampleValue("日期")]
   public string BirthDate { get; set; }
   ```

2. **遵循验证规则**：示例值应该符合字段的验证规则

   ```csharp
   [EmailAddress]
   [ExampleValue("user@example.com")]  // ✅ 符合邮箱格式
   public string Email { get; set; }
   ```

3. **考虑业务场景**：根据实际业务场景提供贴切的示例

   ```csharp
   [ExampleValue("计算机科学与技术")]
   public string Major { get; set; }  // 专业字段
   
   [ExampleValue("软件工程师")]
   public string Position { get; set; }  // 职位字段
   ```

4. **枚举类型示例**：对于枚举类型，使用枚举的显示名称

   ```csharp
   [Display(Name = "男")]
   Male = 1,
   [Display(Name = "女")]
   Female = 2
   
   // DTO 中
   [ExampleValue("男")]
   public Gender Gender { get; set; }
   ```

## 🎨 使用场景

1. **复杂业务实体导入**：为包含多个字段的复杂实体提供清晰的导入示例
2. **格式要求严格的字段**：如日期、时间、编号格式等
3. **枚举值字段**：展示枚举的可选值
4. **特殊格式字段**：如标签列表（逗号分隔）、JSON 格式等

## 📚 相关文档

- [增强批量导入组件使用指南](../02-UI-Generation/增强批量导入组件使用指南.md)
- [CodeSpirit.Core核心框架](../01-Core-Docs/CodeSpirit.Core核心框架.md)

## 🔍 注意事项

1. 示例值仅用于展示，不会作为默认值填充
2. 示例值应该是字符串类型，即使属性是数字或日期
3. 不需要为所有字段都添加示例值，系统会自动生成合理的默认示例
4. 示例值会显示在 Excel 模板的第二行，样式为灰色斜体，便于区分

