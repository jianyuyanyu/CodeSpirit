# CodeSpirit.Amis 图标列使用指南

## 概述

CodeSpirit.Amis 提供了强大的图标列功能，能够自动识别图标字段并生成相应的 AMIS 图标列配置。该功能支持自定义图标（设置 vendor 为空字符串）以及多种图标样式配置。

## 自动识别规则

### 字段名称模式

系统会自动识别以下命名模式的字段作为图标字段：

- `icon` - 直接命名为 icon
- `*Icon` - 以 Icon 结尾的字段（如 statusIcon、typeIcon）
- `icon*` - 以 icon 开头的字段（如 iconClass、iconName）
- `*_icon` 或 `icon_*` - 包含下划线的图标字段
- `iconClass`、`iconName`、`iconCss`、`fontIcon` - 常见的图标相关字段名

### 排除规则

以下字段不会被识别为图标字段：
- 包含 `url`、`path`、`image`、`photo`、`picture`、`avatar` 的字段
- 这些字段会被识别为图片或头像字段

## 基础用法

### 1. 自动识别

```csharp
public class MenuDto
{
    [DisplayName("菜单名称")]
    public string Name { get; set; }
    
    [DisplayName("图标")]
    public string Icon { get; set; }  // 自动识别为图标列
    
    [DisplayName("状态图标")]
    public string StatusIcon { get; set; }  // 自动识别为图标列
}
```

### 2. 使用特性配置

```csharp
public class MenuDto
{
    [DisplayName("菜单名称")]
    public string Name { get; set; }
    
    [DisplayName("图标")]
    [IconColumn("", "lg", Color = "#007bff")]
    public string Icon { get; set; }
    
    [DisplayName("状态")]
    [IconColumn(DefaultIcon = "fas fa-circle", Size = "sm")]
    public string Status { get; set; }
}
```

## IconColumnAttribute 详细配置

### 构造函数

```csharp
// 默认构造函数
[IconColumn]

// 指定厂商
[IconColumn("")]  // 空字符串表示自定义图标

// 指定厂商和大小
[IconColumn("", "lg")]
```

### 属性配置

| 属性 | 类型 | 默认值 | 描述 |
|------|------|---------|------|
| `Vendor` | string | `""` | 图标厂商，空字符串表示自定义图标 |
| `Size` | string | `"md"` | 图标大小：xs, sm, md, lg, xl, 2xl, 3xl, 4xl（生成对应的CSS类名） |
| `Color` | string | - | 图标颜色：primary, secondary, success, danger, warning, info, light, dark, muted（生成对应的CSS类名） |
| `Spin` | bool | `false` | 是否旋转动画 |
| `ClassName` | string | - | 自定义 CSS 类名 |
| `DefaultIcon` | string | - | 默认图标（当值为空时显示） |
| `ShowText` | bool | `false` | 是否显示文本 |
| `TextPosition` | string | `"right"` | 文本位置：left, right, top, bottom |

## 使用示例

### 1. 基础图标列

```csharp
public class CategoryDto
{
    [DisplayName("分类名称")]
    public string Name { get; set; }
    
    [DisplayName("图标")]
    public string Icon { get; set; }  // 值示例：fa-solid fa-home
}
```

生成的 AMIS 配置：
```json
{
  "name": "icon",
  "label": "图标",
  "type": "icon",
  "vendor": "",
  "icon": "${icon}"
}
```

### 2. 带默认图标和语义化颜色

```csharp
public class StatusDto
{
    [DisplayName("状态")]
    [IconColumn(DefaultIcon = "fas fa-question-circle", Color = "muted")]
    public string StatusIcon { get; set; }
}
```

生成的配置会包含：`"className": "text-muted"`

### 3. 大尺寸彩色图标

```csharp
public class PriorityDto
{
    [DisplayName("优先级")]
    [IconColumn(Size = "xl", Color = "danger")]  // xl 大小，危险色
    public string PriorityIcon { get; set; }  // 值示例：fas fa-exclamation-triangle
}
```

生成的配置会包含：`"className": "text-xl text-danger"`

### 4. 旋转动画图标

```csharp
public class LoadingDto
{
    [DisplayName("加载状态")]
    [IconColumn(Spin = true, Color = "#007bff")]
    public string LoadingIcon { get; set; }  // 值示例：fas fa-spinner
}
```

### 5. 图标与文本组合

```csharp
public class ActionDto
{
    [DisplayName("操作")]
    [IconColumn(ShowText = true, TextPosition = "right")]
    public string ActionIcon { get; set; }  // 图标类名
    
    [DisplayName("操作名称")]
    public string ActionText { get; set; }  // 对应的文本
}
```

### 6. 自定义样式

```csharp
public class CustomDto
{
    [DisplayName("自定义图标")]
    [IconColumn(ClassName = "custom-icon-style text-3xl text-purple-500")]  // 完全自定义
    public string CustomIcon { get; set; }
}
```

生成的配置会包含：`"className": "custom-icon-style text-3xl text-purple-500"`

## AMIS 图标组件说明

根据 [AMIS 图标组件文档](https://aisuda.bce.baidu.com/amis/zh-CN/components/icon)，图标组件支持以下配置：

- **vendor**: 图标厂商，设置为空字符串 `""` 表示使用自定义图标类名
- **icon**: 图标类名或图标名称
- **className**: CSS类名，用于控制图标的大小、颜色等样式（如示例中的 `"text-info text-xl"`）
- **spin**: 是否旋转

## CSS 类名说明

图标的样式完全通过 `className` 属性中的CSS类来控制：

### 大小类名（基于 Tailwind CSS）
- `text-xs` - 12px
- `text-sm` - 14px  
- `text-base` - 16px（默认）
- `text-lg` - 18px
- `text-xl` - 20px
- `text-2xl` - 24px
- `text-3xl` - 30px
- `text-4xl` - 36px

### 颜色类名（基于 Bootstrap）
- `text-primary` - 主要颜色
- `text-secondary` - 次要颜色
- `text-success` - 成功颜色
- `text-danger` - 危险颜色
- `text-warning` - 警告颜色
- `text-info` - 信息颜色
- `text-light` - 浅色
- `text-dark` - 深色
- `text-muted` - 静音色

## 常见图标类名示例

### Font Awesome 6

```
// 实心图标
fa-solid fa-home
fa-solid fa-user
fa-solid fa-cog
fa-solid fa-check

// 线框图标
fa-regular fa-heart
fa-regular fa-star
fa-regular fa-file

// 品牌图标
fa-brands fa-github
fa-brands fa-twitter
```

### Bootstrap Icons

```
bi-house
bi-person
bi-gear
bi-check-circle
```

### 自定义图标

```
icon-custom-home
my-icon-set-user
custom-icons-settings
```

## 注意事项

1. **vendor 属性**: 系统默认设置为空字符串，支持使用任何自定义图标类名
2. **字段值格式**: 图标字段的值应该是完整的 CSS 类名（如 `fa-solid fa-home`）
3. **样式依赖**: 确保页面中包含了相应的图标字体文件（如 Font Awesome CSS）
4. **CSS类名控制**: 图标大小和颜色通过 `className` 属性中的CSS类来控制，符合 AMIS 规范
   - 大小：通过 `text-xs`、`text-xl` 等类名控制
   - 颜色：通过 `text-primary`、`text-danger` 等语义化类名控制
   - 自定义：支持任意CSS类名组合
5. **性能考虑**: 图标列的渲染性能较好，适合在大量数据的表格中使用
6. **响应式**: 图标列在移动端设备上也能很好地显示

## 故障排除

### 图标不显示

1. 检查图标类名是否正确
2. 确认页面是否引入了图标字体文件
3. 验证 CSS 类名的格式是否符合图标库要求

### 样式问题

1. 使用浏览器开发者工具检查生成的 HTML
2. 确认自定义 CSS 类名是否生效
3. 检查颜色和大小配置是否正确应用

### 自动识别问题

1. 确认字段名称符合识别规则
2. 考虑使用 `IconColumnAttribute` 显式标记
3. 检查字段类型是否为字符串类型
