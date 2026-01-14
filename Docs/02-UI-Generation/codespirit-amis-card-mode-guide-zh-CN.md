# CodeSpirit.Amis 卡片模式使用指南

## 概述

CodeSpirit.Amis 卡片模式是基于 AMIS 框架的卡片式数据展示功能，为 CRUD 操作提供了更美观、更直观的卡片布局方式。该功能特别适用于图片展示、用户列表、产品目录等场景。

## 功能特性

- 🎯 **灵活配置**：支持通过特性注解配置卡片显示
- 🎨 **丰富模板**：支持自定义卡片内容模板
- 🔧 **智能过滤**：自动过滤不兼容的操作按钮
- 📱 **响应式布局**：支持自定义每行卡片数量
- 🎭 **多字段类型**：支持标题、副标题、描述、头像等字段类型

## 快速开始

### 1. 为控制器添加卡片模式支持

在控制器类上添加 `[AmisCard]` 特性：

```csharp
[DisplayName("图片管理")]
[Navigation(Icon = "fa-solid fa-image", PlatformType = PlatformType.System)]
[AmisCard(
    DefaultPerPage = 12,          // 每页显示12张卡片
    SwitchPerPage = true,         // 允许切换每页数量
    Placeholder = "暂无图片",      // 空数据提示
    ColumnsCount = 3,             // 每行显示3张卡片
    TitleField = "OriginalFileName",     // 卡片标题字段
    SubTitleField = "SizeFormatted",     // 卡片副标题字段
    DescriptionField = "Description",    // 卡片描述字段
    AvatarField = "DownloadUrl"          // 卡片头像字段
)]
public class SystemImagesController : ApiControllerBase
{
    // 控制器实现...
}
```

### 2. 为 DTO 属性添加卡片字段配置

在 DTO 属性上添加 `[AmisCardField]` 特性：

```csharp
public class SystemImageDto
{
    /// <summary>
    /// 原始文件名
    /// </summary>
    [DisplayName("文件名")]
    [AmisCardField(FieldType = CardFieldType.Title, Order = 1)]
    public string OriginalFileName { get; set; } = string.Empty;

    /// <summary>
    /// 文件大小（格式化显示）
    /// </summary>
    [DisplayName("大小")]
    [AmisCardField(FieldType = CardFieldType.SubTitle, Order = 2)]
    public string SizeFormatted { get; set; } = string.Empty;

    /// <summary>
    /// 文件描述
    /// </summary>
    [DisplayName("描述")]
    [AmisCardField(FieldType = CardFieldType.Description, Order = 3)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 下载URL
    /// </summary>
    [DisplayName("下载链接")]
    [AmisColumn(Type = "image")]
    [AmisCardField(FieldType = CardFieldType.Avatar, Order = 0)]
    public string DownloadUrl { get; set; } = string.Empty;

    /// <summary>
    /// 图片宽度（像素）
    /// </summary>
    [DisplayName("宽度")]
    [AmisCardField(FieldType = CardFieldType.Body, Order = 4, 
        Template = "<span class=\"label label-info\">宽度: ${width}px</span>")]
    public int Width { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [DisplayName("上传时间")]
    [AmisCardField(FieldType = CardFieldType.Body, Order = 6, 
        Template = "<p class=\"text-muted\"><i class=\"fa fa-clock\"></i> 上传于: ${createdTime|date:YYYY-MM-DD HH:mm}</p>")]
    public DateTime CreatedTime { get; set; }
}
```

## 特性详解

### AmisCard 特性

控制器级别的卡片模式配置特性，用于启用和配置卡片模式。

#### 属性说明

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `DefaultPerPage` | int | 6 | 每页默认显示卡片数量 |
| `SwitchPerPage` | bool | false | 是否允许用户切换每页显示数量 |
| `Placeholder` | string | "暂无数据" | 空数据时的提示文本 |
| `ColumnsCount` | int | 2 | 每行显示卡片数量 |
| `TitleField` | string | null | 卡片标题字段名 |
| `SubTitleField` | string | null | 卡片副标题字段名 |
| `DescriptionField` | string | null | 卡片描述字段名 |
| `AvatarField` | string | null | 卡片头像字段名 |
| `AvatarClassName` | string | "pull-left thumb-md avatar b-3x m-r" | 头像CSS类名 |

### AmisCardField 特性

属性级别的卡片字段配置特性，用于指定字段在卡片中的显示方式。

#### 属性说明

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `FieldType` | CardFieldType | Body | 字段在卡片中的类型 |
| `Order` | int | 0 | 字段显示顺序 |
| `Template` | string | null | 自定义模板内容 |

#### CardFieldType 枚举

| 值 | 说明 | 用途 |
|----|------|------|
| `Title` | 标题 | 卡片主标题，通常显示在头部 |
| `SubTitle` | 副标题 | 卡片副标题，显示在标题下方 |
| `Description` | 描述 | 卡片描述信息 |
| `Avatar` | 头像 | 卡片头像或图片 |
| `Body` | 主体内容 | 卡片主体部分的自定义内容 |
| `Highlight` | 高亮标识 | 用于标识重要信息的高亮显示 |

## 模板语法

### 字段引用

在模板中，字段名使用小写首字母格式：

```html
<!-- 正确的字段引用 -->
${originalFileName}
${createdTime|date:YYYY-MM-DD}

<!-- 错误的字段引用 -->
${OriginalFileName}
${CreatedTime}
```

### 常用模板示例

#### 标签样式
```html
<span class="label label-info">状态: ${status}</span>
<span class="label label-success">类型: ${type}</span>
```

#### 图标+文本
```html
<p><i class="fa fa-user"></i> 创建者: ${creatorName}</p>
<p><i class="fa fa-clock"></i> 时间: ${createdTime|date:YYYY-MM-DD HH:mm}</p>
```

#### 条件显示
```html
<% if (this.status === 'active') { %>
  <span class="label label-success">已激活</span>
<% } else { %>
  <span class="label label-default">未激活</span>
<% } %>
```

#### 循环显示
```html
<% if (this.tags && this.tags.length) { %>
  <% this.tags.map(function(tag) { %>
    <span class="label label-primary"><%- tag.name %></span>
  <% }) %>
<% } %>
```

## 限制和注意事项

### 1. 不支持的功能

卡片模式下以下功能会被自动过滤：
- 导出 Excel 功能 (`export-excel` 类型按钮)
- 某些复杂的列操作

### 2. 字段命名规范

- 模板中的字段引用必须使用小写首字母
- 避免使用 `this.` 前缀，直接使用字段名即可

### 3. 性能考虑

- 建议合理设置 `DefaultPerPage` 值，避免单页加载过多卡片
- 大数据量时建议启用分页功能

### 4. 样式兼容性

- 确保使用的 CSS 类名在目标环境中可用
- 建议使用标准的 Bootstrap 或 FontAwesome 类名

## 高级用法

### 1. 自定义卡片操作按钮

卡片会自动继承控制器中定义的编辑、删除等操作按钮：

```csharp
// 在控制器中定义的操作会自动在卡片中显示
[HttpPut("{id}")]
public async Task<ApiResponse> Update(int id, [FromBody] UpdateSystemImageDto dto)
{
    // 更新逻辑
}

[HttpDelete("{id}")]
public async Task<ApiResponse> Delete(int id)
{
    // 删除逻辑
}
```

### 2. 动态字段配置

可以通过控制器特性动态指定字段映射：

```csharp
[AmisCard(
    TitleField = "Name",           // 映射到 Name 字段作为标题
    SubTitleField = "Category",    // 映射到 Category 字段作为副标题
    DescriptionField = "Summary",  // 映射到 Summary 字段作为描述
    AvatarField = "ImageUrl"       // 映射到 ImageUrl 字段作为头像
)]
```

### 3. 复杂模板示例

```csharp
[AmisCardField(FieldType = CardFieldType.Body, Order = 10, 
    Template = @"
        <div class='card-stats'>
            <% if (this.score >= 90) { %>
                <span class='badge badge-success'>优秀</span>
            <% } else if (this.score >= 80) { %>
                <span class='badge badge-info'>良好</span>
            <% } else { %>
                <span class='badge badge-warning'>一般</span>
            <% } %>
            <span class='score-value'>${score}分</span>
        </div>
    ")]
public int Score { get; set; }
```

## 故障排除

### 常见问题

1. **卡片不显示**
   - 检查控制器是否添加了 `[AmisCard]` 特性
   - 确认 DTO 属性上的 `[AmisCardField]` 配置正确

2. **字段显示为空**
   - 检查模板中的字段名是否使用小写首字母
   - 确认字段名拼写正确

3. **样式显示异常**
   - 检查 CSS 类名是否正确
   - 确认目标环境中CSS样式可用

4. **按钮功能异常**
   - 卡片模式会自动过滤不兼容的功能
   - 检查操作方法的路由和权限配置

### 调试技巧

1. 使用浏览器开发者工具查看生成的 AMIS 配置
2. 检查网络请求确认数据正确返回
3. 查看控制台错误信息

## 版本更新日志

### v1.0.0
- 初始版本发布
- 支持基本卡片模式配置
- 支持自定义模板和字段类型
- 自动过滤不兼容的操作按钮

---

## 相关文档

- [CodeSpirit.Amis智能界面生成引擎](./codespirit-amis-engine-zh-CN.md)
- [AMIS列自动推断功能说明](./amis-column-inference-zh-CN.md)
- [AMIS官方文档 - Card组件](https://aisuda.bce.baidu.com/amis/zh-CN/components/card)
