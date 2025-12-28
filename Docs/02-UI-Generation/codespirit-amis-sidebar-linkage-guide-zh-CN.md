# CodeSpirit.Amis 侧边栏联动功能使用指南

## 概述

侧边栏联动功能是 CodeSpirit.Amis 智能界面生成引擎的一个重要特性，允许开发者通过简单的特性注解实现页面侧边栏与主内容区域的数据联动效果。该功能特别适用于分类筛选、导航树等场景。

## 核心组件

### 1. PageAsideAttribute 特性

用于标记需要在页面侧边栏显示的表单字段。**标记了此特性的字段不会在主查询表单中重复显示**。

```csharp
/// <summary>
/// 页面侧边栏配置特性，用于标记需要在侧边栏显示的表单字段
/// 在查询DTO的属性上使用此特性，该字段将被包含在页面的aside区域中
/// 注意：标记了此特性的字段会自动从主查询表单中排除，避免重复显示
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class PageAsideAttribute : Attribute
{
    /// <summary>
    /// 表单提交目标。如果为空，则自动设置为CRUD组件名称；如果不为空，则使用指定值
    /// </summary>
    public string Target { get; set; } = "";
    
    /// <summary>
    /// 是否在初始化时提交，默认为false
    /// </summary>
    public bool SubmitOnInit { get; set; } = false;
    
    /// <summary>
    /// 是否不使用面板包装，默认为false
    /// </summary>
    public bool WrapWithPanel { get; set; } = false;
}
```

### 2. AsideHelper 助手类

负责生成侧边栏配置的核心逻辑。

主要方法：
- `ShouldGenerateAside(Type? queryDtoType)` - 检查是否需要生成侧边栏
- `GenerateAsideConfig(Type? queryDtoType, string? crudName)` - 生成侧边栏JSON配置

### 3. 智能Target设置

系统会根据以下优先级自动设置表单的提交目标：

1. **优先级1：明确指定** - 如果PageAside特性中设置了Target值，则使用该值
2. **优先级2：自动设置** - 如果Target为空，则自动设置为CRUD组件名称（如`questionsCrud`）
3. **优先级3：默认回退** - 如果都没有，则使用默认值"window"

```csharp
// 示例1：使用自动设置的CRUD组件名称
[PageAside] // Target自动设置为"questionsCrud"
[AmisInputTreeField(...)]
public long? CategoryId { get; set; }

// 示例2：明确指定Target
[PageAside(Target = "customForm")] // 使用指定的目标
[AmisInputTreeField(...)]
public long? CategoryId { get; set; }

// 示例3：使用默认值
[PageAside(Target = "window")] // 明确使用window
[AmisInputTreeField(...)]
public long? CategoryId { get; set; }
```

### 4. 扩展的 AmisInputTreeField 支持

新增了对侧边栏优化的属性支持：

```csharp
/// <summary>
/// 是否在值变化时提交表单，默认为false
/// </summary>
public bool SubmitOnChange { get; set; } = false;

/// <summary>
/// 是否自动调整高度，默认为false
/// </summary>
public bool HeightAuto { get; set; } = false;

/// <summary>
/// 是否默认选择第一个选项，默认为false
/// </summary>
public bool SelectFirst { get; set; } = false;

/// <summary>
/// 是否只显示输入框不显示边框等样式，默认为false
/// </summary>
public bool InputOnly { get; set; } = false;
```

## 使用方法

### 步骤1：在查询DTO中标记侧边栏字段

```csharp
using CodeSpirit.Amis.Attributes;
using CodeSpirit.Amis.Attributes.FormFields;
using CodeSpirit.Core.Dtos;

/// <summary>
/// 题目查询DTO
/// </summary>
public class QuestionQueryDto : QueryDtoBase
{
    /// <summary>
    /// 分类ID - 在侧边栏显示的分类树
    /// </summary>
    [DisplayName("分类")]
    [PageAside]  // 标记为侧边栏字段
    [AmisInputTreeField(
        DataSource = "${ROOT_API}/api/exam/QuestionCategories/tree",
        Multiple = false,
        Cascade = true,
        ShowOutline = true,
        LabelField = "name",
        ValueField = "id",
        Required = false,
        Clearable = true,
        SubmitOnChange = true,    // 值变化时自动提交
        HeightAuto = true,        // 自动调整高度
        SelectFirst = true,       // 默认选择第一个
        InputOnly = true          // 简洁样式
    )]
    public long? CategoryId { get; set; }

    /// <summary>
    /// 其他查询条件
    /// </summary>
    [DisplayName("题目类型")]
    public QuestionType? Type { get; set; }
}
```

### 步骤2：确保数据源API可用

确保侧边栏数据源API返回正确的树形数据格式：

```csharp
[HttpGet("tree")]
[DisplayName("获取分类树")]
public async Task<ActionResult<ApiResponse<List<QuestionCategoryTreeDto>>>> GetCategoryTree()
{
    var categories = await _questionCategoryService.GetCategoryTreeAsync();
    return SuccessResponse(categories);
}
```

数据格式示例：
```json
[
  {
    "id": 1,
    "name": "数学",
    "children": [
      {
        "id": 11,
        "name": "代数",
        "children": []
      },
      {
        "id": 12,
        "name": "几何",
        "children": []
      }
    ]
  },
  {
    "id": 2,
    "name": "物理",
    "children": []
  }
]
```

### 步骤3：控制器无需额外配置

AmisCRUDConfigBuilder 会自动检测 PageAside 特性并生成相应的页面配置：

```csharp
[DisplayName("题目管理")]
[Navigation(Icon = "fa-solid fa-book")]
public class QuestionsController : ApiControllerBase
{
    // 标准的CRUD操作即可
    [HttpGet]
    [DisplayName("获取题目列表")]
    public async Task<ActionResult<ApiResponse<PagedList<QuestionDto>>>> GetQuestions(
        [FromQuery] QuestionQueryDto query)
    {
        var result = await _questionService.GetPagedListAsync(query);
        return SuccessResponse(result);
    }
}
```

## 生成的页面配置

系统会自动生成包含侧边栏的页面配置：

```json
{
  "type": "page",
  "title": "题目管理",
  "aside": {
    "type": "form",
    "wrapWithPanel": false,
    "target": "window",
    "submitOnInit": false,
    "body": [
      {
        "type": "input-tree",
        "name": "categoryId",
        "label": "分类",
        "source": "${ROOT_API}/api/exam/QuestionCategories/tree",
        "labelField": "name",
        "valueField": "id",
        "multiple": false,
        "cascade": true,
        "showOutline": true,
        "clearable": true,
        "submitOnChange": true,
        "heightAuto": true,
        "selectFirst": true,
        "inputOnly": true,
        "inputClassName": "no-border no-padder mt-1"
      }
    ]
  },
  "body": [
    {
      "type": "crud",
      // CRUD配置...
    }
  ]
}
```

## 高级配置

### 多个侧边栏字段

```csharp
public class ProductQueryDto : QueryDtoBase
{
    [DisplayName("产品分类")]
    [PageAside]
    [AmisInputTreeField(
        DataSource = "${ROOT_API}/api/ProductCategories/tree",
        SubmitOnChange = true,
        HeightAuto = true,
        InputOnly = true
    )]
    public long? CategoryId { get; set; }

    [DisplayName("品牌")]
    [PageAside]
    [AmisSelectField(
        DataSource = "${ROOT_API}/api/Brands/options",
        SubmitOnChange = true
    )]
    public long? BrandId { get; set; }
}
```

### 自定义侧边栏配置

```csharp
[DisplayName("地区")]
[PageAside(Target = "window", SubmitOnInit = true, WrapWithPanel = true)]
[AmisInputTreeField(
    DataSource = "${ROOT_API}/api/Regions/tree",
    SubmitOnChange = true,
    SelectFirst = false,  // 不默认选择
    InputOnly = false     // 显示完整样式
)]
public string? RegionCode { get; set; }
```

## 支持的字段类型

侧边栏联动支持以下字段类型：

### 1. 树形选择 (input-tree)
```csharp
[AmisInputTreeField(...)]
public long? CategoryId { get; set; }
```

### 2. 下拉选择 (select)
```csharp
[AmisSelectField(...)]
public int? StatusId { get; set; }
```

### 3. 单选按钮组 (radios)
```csharp
[AmisRadiosField(...)]
public string? Type { get; set; }
```

### 4. 复选框组 (checkboxes)
```csharp
[AmisCheckboxesField(...)]
public List<string>? Tags { get; set; }
```

## 重要特性说明

### 🔄 智能字段过滤

**标记了 `[PageAside]` 特性的字段会自动从主查询表单中排除**，这样可以：

- ✅ **避免重复显示** - 同一字段不会同时出现在侧边栏和查询表单中
- ✅ **简化界面布局** - 主查询表单更加简洁，专注于其他筛选条件
- ✅ **提升用户体验** - 用户只需在侧边栏操作分类等导航性字段

```csharp
public class QuestionQueryDto : QueryDtoBase
{
    /// <summary>
    /// 分类ID - 只会显示在侧边栏，不会出现在查询表单中
    /// </summary>
    [PageAside]
    [AmisInputTreeField(...)]
    public long? CategoryId { get; set; }

    /// <summary>
    /// 题目类型 - 会显示在查询表单中
    /// </summary>
    [DisplayName("题目类型")]
    public QuestionType? Type { get; set; }
}
```

### 📐 布局效果

使用侧边栏联动后的页面布局：

```
┌─────────────────────────────────────────────────────────┐
│  题目管理                                                 │
├──────────────┬──────────────────────────────────────────┤
│   分类树     │           主内容区域                     │
│  ┌─ 数学     │  ┌─ 查询表单 (不包含分类字段)            │
│  ├─ 物理     │  │ 题目类型: [下拉选择]                  │
│  └─ 化学     │  │ 难度: [下拉选择]                     │
│              │  └─ [搜索] [重置]                       │
│              │  ┌─ 数据表格                            │
│              │  │ ID │ 题目 │ 类型 │ 难度 │ 操作      │
│              │  └─────────────────────────────────────│
└──────────────┴──────────────────────────────────────────┘
```

## 最佳实践

### 1. 数据源优化
- 为侧边栏数据源添加缓存
- 使用适当的数据分页或懒加载
- 确保API响应速度

```csharp
[HttpGet("tree")]
[ResponseCache(Duration = 300)] // 缓存5分钟
public async Task<ActionResult<ApiResponse<List<CategoryTreeDto>>>> GetCategoryTree()
{
    // 实现缓存逻辑
}
```

### 2. 用户体验优化
- 使用 `SelectFirst = true` 为常用场景提供默认选择
- 合理使用 `SubmitOnChange` 避免过度提交
- 使用 `HeightAuto = true` 充分利用侧边栏空间

### 3. 性能考虑
- 限制侧边栏字段数量（建议不超过3个）
- 避免在侧边栏中使用复杂的联动逻辑
- 合理使用 `SubmitOnInit` 避免不必要的初始加载

### 4. 字段规划建议
- **侧边栏字段** - 适合导航性、分类性的字段（如分类、地区、部门等）
- **查询表单字段** - 适合条件性、搜索性的字段（如关键词、状态、时间范围等）

## 样式定制

### CSS 类名说明
- `.aside-form` - 侧边栏表单容器
- `.no-border` - 移除边框样式
- `.no-padder` - 移除内边距
- `.mt-1` - 顶部边距

### 自定义样式示例
```css
.aside-form .cxd-Tree {
    border: none;
    background: transparent;
}

.aside-form .cxd-Tree-item {
    padding: 4px 8px;
}
```

## 故障排除

### 常见问题

1. **侧边栏不显示**
   - 检查是否正确添加了 `[PageAside]` 特性
   - 确认 AsideHelper 已正确注册到依赖注入容器
   - 验证查询DTO是否正确传递给控制器方法

2. **数据联动不工作**
   - 检查 `SubmitOnChange = true` 是否设置
   - 验证数据源API是否正常返回数据
   - 确认字段名称映射是否正确

3. **样式显示异常**
   - 检查 `InputOnly` 和 `HeightAuto` 设置
   - 验证自定义CSS是否冲突
   - 确认Amis版本兼容性

### 调试方法

1. **查看生成的配置**
```csharp
// 在开发环境中输出生成的JSON配置
var config = await _crudConfigBuilder.CreateCrudConfigAsync(typeof(QuestionsController));
_logger.LogInformation("Generated config: {Config}", JsonConvert.SerializeObject(config, Formatting.Indented));
```

2. **验证数据源**
```bash
# 直接访问数据源API
curl -X GET "https://localhost:7001/api/exam/QuestionCategories/tree"
```

## 总结

侧边栏联动功能通过简单的特性注解就能实现复杂的页面布局和交互效果，显著提升了开发效率和用户体验。结合 CodeSpirit.Amis 的其他功能，可以快速构建现代化的管理后台界面。

更多高级用法和配置选项，请参考 [CodeSpirit.Amis智能界面生成引擎.md](CodeSpirit.Amis智能界面生成引擎.md)。
