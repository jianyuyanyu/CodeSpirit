# ResourceTagHelper资源管理组件使用指南

## 组件概述

`ResourceTagHelper` 是CodeSpirit框架中的自定义TagHelper组件，专门用于简化前端资源（CSS、JavaScript）的引用管理。该组件支持CDN和本地资源的自动切换，提供智能版本控制，并能够根据不同的资源类型自动生成相应的HTML标签。

## 核心功能

### 1. 统一资源引用语法

使用简洁的`<resource>`标签统一管理所有前端资源：

```html
<!-- 引用CSS文件 -->
<resource path="css/main.css" type="css" />

<!-- 引用JavaScript文件 -->
<resource path="js/app.js" type="js" />

<!-- 获取资源URL -->
<resource path="images/logo.png" type="url" />
```

### 2. CDN和本地资源自动切换

组件根据配置自动选择使用CDN还是本地资源：

- **启用CDN时**：使用配置的CDN域名 + 资源路径
- **未启用CDN时**：使用本地路径，并自动添加ASP.NET Core的版本控制

### 3. 智能版本控制

提供多层次的版本控制策略：

- **配置版本优先**：使用`SiteSettings.ResourceVersion`中配置的版本号
- **应用启动时间备用**：未配置版本号时，使用应用启动时间作为版本标识
- **本地开发支持**：本地环境下使用ASP.NET Core内置的`asp-append-version`

### 4. 多种资源类型支持

| 类型 | 说明 | 生成的HTML标签 |
|------|------|----------------|
| `css` | CSS样式表 | `<link rel="stylesheet" href="..." />` |
| `js` | JavaScript脚本 | `<script src="..."></script>` |
| `url` | 获取资源URL | `<span>资源URL</span>` |

## 配置说明

### SiteSettings配置

在`appsettings.json`中配置资源管理相关选项：

```json
{
  "SiteSettings": {
    "SiteName": "CodeSpirit",
    "TopSiteName": "CodeSpirit", 
    "LogoUrl": "/favicon.ico",
    "EnableCdn": true,
    "CdnUrl": "https://cdn.example.com",
    "ResourceVersion": "v1.2.3"
  }
}
```

### 配置项说明

| 配置项 | 类型 | 说明 | 默认值 |
|--------|------|------|--------|
| `EnableCdn` | bool | 是否启用CDN | false |
| `CdnUrl` | string | CDN基础URL | "" |
| `ResourceVersion` | string | 资源版本号 | "" |

## 使用示例

### 1. 基础使用

```html
<!-- 在Razor页面中引用资源 -->
<resource path="sdk/6.12.0/antd.css" type="css" />
<resource path="sdk/6.12.0/helper.css" type="css" />
<resource path="css/chat.css" type="css" />

<resource path="sdk/6.12.0/amis.js" type="js" />
<resource path="js/app.js" type="js" />
```

### 2. 实际生成的HTML

**启用CDN时：**
```html
<link rel="stylesheet" href="https://cdn.example.com/sdk/6.12.0/antd.css?v=v1.2.3" />
<script src="https://cdn.example.com/js/app.js?v=v1.2.3"></script>
```

**未启用CDN时：**
```html
<link rel="stylesheet" href="/sdk/6.12.0/antd.css" asp-append-version="true" />
<script src="/js/app.js" asp-append-version="true"></script>
```

### 3. 获取资源URL

```html
<!-- 在JavaScript中使用资源URL -->
<script>
    const logoUrl = '<resource path="images/logo.png" type="url" />';
    console.log('Logo URL:', logoUrl);
</script>
```

## 版本控制策略

### 1. 版本控制优先级

1. **显式配置版本**：`SiteSettings.ResourceVersion`
2. **应用启动时间**：格式为`yyyyMMddHHmm`
3. **ASP.NET Core内置**：本地环境使用`asp-append-version`

### 2. 版本控制实现

```csharp
// 应用程序启动时确定版本号
private static readonly string ApplicationStartTimeVersion = DateTime.UtcNow.ToString("yyyyMMddHHmm");

// 处理时选择版本号
if (cdnEnabled)
{
    var version = !string.IsNullOrEmpty(_siteSettings.ResourceVersion) 
        ? _siteSettings.ResourceVersion 
        : ApplicationStartTimeVersion;
    url += $"?v={version}";
}
```

### 3. 版本号格式建议

| 环境 | 格式 | 示例 | 说明 |
|------|------|------|------|
| 开发环境 | 不设置 | - | 使用应用启动时间 |
| 测试环境 | 构建号 | `build-001` | CI/CD自动生成 |
| 预发布环境 | Git哈希 | `abc123f` | Git提交标识 |
| 生产环境 | 语义版本 | `v1.2.3` | 手动或自动管理 |

## 最佳实践

### 1. 开发环境配置

```json
{
  "SiteSettings": {
    "EnableCdn": false,
    "ResourceVersion": ""
  }
}
```

- 不启用CDN，使用本地资源
- 不设置版本号，每次重启自动更新
- 利用ASP.NET Core内置版本控制

### 2. 生产环境配置

```json
{
  "SiteSettings": {
    "EnableCdn": true,
    "CdnUrl": "https://cdn.yourdomain.com",
    "ResourceVersion": "v1.2.3"
  }
}
```

- 启用CDN提升性能
- 设置明确的版本号
- 配合部署流程更新版本

### 3. CI/CD集成

在部署脚本中自动设置版本号：

```bash
# 使用Git提交哈希
VERSION=$(git rev-parse --short HEAD)
sed -i "s/\"ResourceVersion\": \"\"/\"ResourceVersion\": \"$VERSION\"/" appsettings.json

# 使用构建时间
VERSION=$(date +"%Y%m%d%H%M")
sed -i "s/\"ResourceVersion\": \"\"/\"ResourceVersion\": \"$VERSION\"/" appsettings.json
```

### 4. 路径规范化

组件会自动处理路径格式：

```html
<!-- 以下两种写法效果相同 -->
<resource path="/css/main.css" type="css" />
<resource path="css/main.css" type="css" />
```

## 技术实现细节

### 1. TagHelper注册

```csharp
[HtmlTargetElement("resource", TagStructure = TagStructure.WithoutEndTag)]
public class ResourceTagHelper : TagHelper
```

### 2. 依赖注入

```csharp
public ResourceTagHelper(IOptions<SiteSettings> options)
{
    _siteSettings = options.Value;
}
```

### 3. 路径处理逻辑

```csharp
// 规范化路径
var resourcePath = Path.StartsWith("/") ? Path.Substring(1) : Path;

// 生成完整URL
var cdnEnabled = _siteSettings.EnableCdn;
var resourceBase = cdnEnabled ? _siteSettings.CdnUrl : "";
var url = resourceBase.TrimEnd('/') + "/" + resourcePath;
```

## 故障排除

### 1. 常见问题

**问题：资源加载404**
- 检查`Path`属性是否正确
- 确认CDN配置和资源是否存在

**问题：版本号不更新**
- 检查`ResourceVersion`配置
- 确认应用是否重启

**问题：本地资源缓存不更新**
- 确认`EnableCdn`为false
- 检查`asp-append-version`是否生效

### 2. 调试技巧

```html
<!-- 调试时查看生成的URL -->
<resource path="js/debug.js" type="url" />
```

## 扩展建议

### 1. 支持更多资源类型

可以扩展支持图片、字体等资源类型：

```csharp
case "img":
    output.TagName = "img";
    output.Attributes.SetAttribute("src", url);
    output.TagMode = TagMode.SelfClosing;
    break;
```

### 2. 环境特定配置

可以根据不同环境使用不同的CDN配置：

```json
{
  "SiteSettings": {
    "CdnUrl": "https://dev-cdn.example.com", // 开发环境CDN
    "ProdCdnUrl": "https://cdn.example.com"  // 生产环境CDN
  }
}
```

通过ResourceTagHelper，CodeSpirit框架提供了一个强大而灵活的资源管理解决方案，既简化了开发体验，又确保了生产环境的性能优化。 