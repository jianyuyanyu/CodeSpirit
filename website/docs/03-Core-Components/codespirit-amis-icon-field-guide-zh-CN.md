# CodeSpirit.Amis 图标字段特性使用指南

## 概述

`AmisIconFieldAttribute` 是 CodeSpirit.Amis 提供的表单字段特性，用于在 AMIS 表单中生成图标选择器字段。该特性支持 FontAwesome、IconFont 等多种图标库，提供搜索、预览、清除等功能，为用户提供友好的图标选择体验。

## 命名空间

```csharp
using CodeSpirit.Amis.Attributes.FormFields;
```

## 基础用法

### 1. 简单图标选择器

```csharp
public class CreateMenuDto
{
    [DisplayName("菜单名称")]
    public string Name { get; set; }
    
    [DisplayName("菜单图标")]
    [AmisIconField]
    public string Icon { get; set; }
}
```

### 2. 指定图标库类型

```csharp
public class CategoryDto
{
    [DisplayName("分类名称")]
    public string Name { get; set; }
    
    [DisplayName("分类图标")]
    [AmisIconField("fontawesome")]  // 显式指定FontAwesome图标库
    public string Icon { get; set; }
}
```

## 特性属性详解

### 构造函数

| 构造函数 | 说明 |
|---------|------|
| `AmisIconFieldAttribute()` | 默认构造函数，使用 FontAwesome 图标库 |
| `AmisIconFieldAttribute(string iconType)` | 指定图标库类型的构造函数 |

### 属性配置

| 属性 | 类型 | 默认值 | 描述 |
|------|------|---------|------|
| `IconType` | `string` | `"fontawesome"` | 图标库类型，支持 "fontawesome", "iconfont" 等 |
| `Searchable` | `bool` | `true` | 是否允许搜索图标 |
| `Clearable` | `bool` | `true` | 是否允许清除图标 |
| `Placeholder` | `string` | `"请选择图标"` | 图标选择器的占位符文本 |
| `PreviewSize` | `string` | `"md"` | 图标预览大小，支持 "sm", "md", "lg" |
| `IconSource` | `string` | `null` | 自定义图标列表数据源 URL |
| `ShowPreview` | `bool` | `true` | 是否显示图标预览 |

## 使用示例

### 1. 基础图标选择器

```csharp
public class MenuDto
{
    [DisplayName("菜单名称")]
    [Required(ErrorMessage = "菜单名称不能为空")]
    public string Name { get; set; }
    
    [DisplayName("菜单图标")]
    [AmisIconField]
    public string Icon { get; set; }
}
```

生成的表单字段包含：
- 文本输入框显示图标类名
- 图标选择按钮
- 图标预览功能
- 清除图标功能

### 2. 不可搜索的图标选择器

```csharp
public class CategoryDto
{
    [DisplayName("分类图标")]
    [AmisIconField(Searchable = false)]
    public string Icon { get; set; }
}
```

### 3. 不可清除的必选图标

```csharp
public class StatusDto
{
    [DisplayName("状态图标")]
    [Required(ErrorMessage = "状态图标不能为空")]
    [AmisIconField(Clearable = false)]
    public string StatusIcon { get; set; }
}
```

### 4. 自定义占位符和预览大小

```csharp
public class ThemeDto
{
    [DisplayName("主题图标")]
    [AmisIconField(
        Placeholder = "选择主题图标", 
        PreviewSize = "lg")]
    public string ThemeIcon { get; set; }
}
```

### 5. 禁用预览功能

```csharp
public class SimpleIconDto
{
    [DisplayName("简单图标")]
    [AmisIconField(ShowPreview = false)]
    public string Icon { get; set; }
}
```

### 6. 使用自定义图标数据源

```csharp
public class CustomIconDto
{
    [DisplayName("自定义图标")]
    [AmisIconField(
        IconType = "custom",
        IconSource = "/api/custom/icons",
        Placeholder = "选择自定义图标")]
    public string CustomIcon { get; set; }
}
```

## 图标库支持

### FontAwesome 图标库

默认支持的图标库，包含常用的图标分类：

- **通用图标**: 文件夹、文件、首页、用户等
- **操作图标**: 添加、编辑、删除、保存等  
- **状态图标**: 成功、失败、警告、信息等
- **教育图标**: 书籍、毕业帽、学校、考试等
- **商业图标**: 商店、购物车、钱包、图表等

图标类名格式：`fa-solid fa-home`、`fa-regular fa-star` 等

### IconFont 图标库

支持 IconFont 图标库：

```csharp
[AmisIconField("iconfont")]
public string Icon { get; set; }
```

### 自定义图标库

通过 `IconSource` 属性可以指定自定义的图标数据源：

```csharp
[AmisIconField(
    IconType = "custom",
    IconSource = "/api/my-icons")]
public string Icon { get; set; }
```

自定义数据源需要返回如下格式的 JSON：

```json
{
  "items": [
    {
      "className": "custom-icon-home",
      "name": "首页图标",
      "category": "导航"
    }
  ],
  "total": 100,
  "categories": ["导航", "操作", "状态"]
}
```

## 生成的 AMIS 配置

`AmisIconFieldAttribute` 会生成包含以下功能的 AMIS 表单字段：

### 1. 基础输入框

```json
{
  "type": "input-text",
  "name": "icon",
  "label": "图标",
  "placeholder": "请选择图标",
  "clearable": true
}
```

### 2. 图标选择按钮

在输入框右侧添加图标选择按钮，点击打开图标选择对话框。

### 3. 图标预览

在输入框内显示当前选中图标的预览。

### 4. 选择对话框

包含以下功能的图标选择对话框：
- 图标分类筛选
- 搜索功能（如果启用）
- 分页显示
- 图标预览
- 确认和取消按钮

## 前端交互逻辑

### 图标选择流程

1. 用户点击图标选择按钮
2. 打开图标选择对话框
3. 用户可以通过分类筛选或搜索查找图标
4. 点击图标进行选择
5. 确认后更新输入框的值
6. 在输入框中显示图标预览

### 键盘支持

- **Enter**: 确认选择
- **Escape**: 取消选择
- **方向键**: 在图标网格中导航

## 控制器支持

系统提供了 `IconsController` 来支持图标数据的获取：

```csharp
// 获取图标列表
GET /api/common/icons?iconType=fontawesome&search=home&page=1&limit=50
```

返回数据格式：

```json
{
  "success": true,
  "data": {
    "items": [...],
    "total": 500,
    "page": 1,
    "limit": 50,
    "categories": ["通用", "操作", "状态"]
  }
}
```

## 样式和主题

### CSS 类名

图标选择器使用以下 CSS 类名：

- `.icon-selector-button`: 选择按钮样式
- `.icon-preview`: 图标预览样式
- `.icon-dialog`: 对话框样式
- `.icon-grid`: 图标网格样式
- `.icon-item`: 单个图标项样式

### 响应式设计

图标选择器在不同屏幕尺寸下都能正常工作：

- **桌面端**: 显示网格布局的图标列表
- **平板端**: 调整网格列数适应屏幕
- **移动端**: 使用单列或双列布局

## 验证和约束

### 表单验证

可以与标准的验证特性配合使用：

```csharp
[DisplayName("必选图标")]
[Required(ErrorMessage = "请选择一个图标")]
[AmisIconField]
public string RequiredIcon { get; set; }

[DisplayName("图标类名")]
[RegularExpression(@"^fa-.+", ErrorMessage = "图标类名必须以 fa- 开头")]
[AmisIconField]
public string ValidatedIcon { get; set; }
```

### 自定义验证

```csharp
[DisplayName("业务图标")]
[AmisIconField]
[CustomValidation(typeof(IconValidator), "ValidateBusinessIcon")]
public string BusinessIcon { get; set; }
```

## 性能优化

### 图标数据缓存

图标列表数据会在前端进行缓存，减少重复请求：

```javascript
// 缓存配置示例
{
  "cache": {
    "enabled": true,
    "duration": 300000,  // 5分钟
    "key": "icon-data"
  }
}
```

### 懒加载

大型图标库支持分页和懒加载，提升加载性能。

## 故障排除

### 常见问题

#### 1. 图标不显示

**问题**: 选择的图标在预览中不显示

**解决方案**:
- 检查图标类名是否正确
- 确认页面已引入相应的图标字体文件
- 验证图标库是否支持该图标

#### 2. 选择对话框无法打开

**问题**: 点击选择按钮没有反应

**解决方案**:
- 检查控制台是否有JavaScript错误
- 确认 `IconsController` 是否正常工作
- 验证API路径是否正确

#### 3. 搜索功能异常

**问题**: 搜索功能无法正常工作

**解决方案**:
- 检查后端搜索逻辑是否正确实现
- 确认搜索参数传递是否正常
- 验证数据源是否支持搜索

#### 4. 自定义图标源无法加载

**问题**: 自定义 `IconSource` 无法加载数据

**解决方案**:
- 检查自定义API的返回格式是否正确
- 确认API是否支持跨域请求
- 验证API路径和参数是否正确

### 调试技巧

#### 1. 启用调试模式

```csharp
[AmisIconField(IconType = "fontawesome")]
[DebugMode(true)]  // 启用调试输出
public string Icon { get; set; }
```

#### 2. 检查生成的配置

在浏览器开发者工具中查看生成的 AMIS 配置，确认字段配置是否正确。

#### 3. 监控网络请求

检查图标数据的网络请求，确认API响应是否正常。

## 扩展和自定义

### 自定义图标渲染器

可以创建自定义的图标渲染组件：

```javascript
// 自定义图标渲染器
function CustomIconRenderer(props) {
  return (
    <div className="custom-icon-wrapper">
      <i className={props.icon} />
      <span>{props.name}</span>
    </div>
  );
}
```

### 扩展图标库

通过继承 `AmisIconFieldAttribute` 创建专用的图标字段特性：

```csharp
public class BusinessIconFieldAttribute : AmisIconFieldAttribute
{
    public BusinessIconFieldAttribute() : base("business")
    {
        IconSource = "/api/business/icons";
        Placeholder = "选择业务图标";
    }
}
```

## 最佳实践

### 1. 图标命名规范

- 使用语义化的图标类名
- 保持图标命名的一致性
- 为业务特定的图标建立命名规范

### 2. 图标分类管理

- 按功能或业务模块对图标进行分类
- 提供有意义的分类名称
- 控制每个分类下的图标数量

### 3. 用户体验优化

- 提供图标搜索功能
- 显示图标的名称和描述
- 支持键盘导航
- 在移动端优化触摸体验

### 4. 性能考虑

- 合理控制图标列表的大小
- 使用分页或懒加载
- 缓存常用图标数据
- 优化图标字体文件的加载

## 总结

`AmisIconFieldAttribute` 为 CodeSpirit.Amis 提供了强大的图标选择功能，支持多种图标库、搜索、预览等特性。通过合理的配置和使用，可以为用户提供友好的图标选择体验，提升表单的易用性和美观度。

在使用过程中，建议结合项目的实际需求选择合适的图标库和配置选项，并注意性能优化和用户体验的平衡。
