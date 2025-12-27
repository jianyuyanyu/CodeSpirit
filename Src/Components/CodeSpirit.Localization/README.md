# CodeSpirit.Localization 多语言国际化组件

## 概述

CodeSpirit.Localization 提供了完整的前后端多语言国际化支持，支持中英文双语，基于 .NET 资源文件和 AMIS locale，通过 Settings 组件实现全局、租户、用户三级语言配置。

## 功能特性

- ✅ **双语支持**：中文（默认）+ 英文
- ✅ **全栈覆盖**：后端 API + 前端 UI
- ✅ **多级配置**：系统默认 → 租户默认 → 用户偏好
- ✅ **类型安全**：使用 .resx 资源文件，编译时强类型访问
- ✅ **动态切换**：用户可实时切换语言，无需重新登录
- ✅ **AMIS 兼容**：集成 AMIS 的 locale 机制
- ✅ **DataAnnotations 支持**：验证特性自动本地化

## 快速开始

### 1. 配置

在 `appsettings.json` 中添加配置：

```json
{
  "Localization": {
    "DefaultCulture": "zh-CN",
    "SupportedCultures": [
      { "Code": "zh-CN", "DisplayName": "简体中文" },
      { "Code": "en", "DisplayName": "English" }
    ],
    "EnableTenantLevelLanguage": true,
    "EnableUserLevelLanguage": true,
    "FallbackToParentCultures": true
  }
}
```

### 2. 注册服务

在 `Program.cs` 中：

```csharp
// 已在 ServiceDefaults 中自动注册
builder.AddServiceDefaults("appname");

// 配置 DataAnnotations 本地化（如果需要）
builder.Services.AddControllers()
    .AddCodeSpiritDataAnnotationsLocalization();
```

### 3. 使用中间件

在 `Program.cs` 中：

```csharp
app.UseCodeSpiritMultiTenant();
app.UseCodeSpiritRequestLocalization(); // 添加这一行
app.UseAuthentication();
```

### 4. 后端使用

#### 在 Controller 中使用

```csharp
public class MyController : ApiControllerBase
{
    private readonly IStringLocalizer<Shared> _localizer;
    
    public MyController(IStringLocalizer<Shared> localizer)
    {
        _localizer = localizer;
    }
    
    public IActionResult GetMessage()
    {
        var message = _localizer["Common.Save"];
        return Ok(new { message });
    }
}
```

#### 抛出本地化异常

```csharp
// 使用资源键
throw new BusinessException("Errors.InvalidStartTime");

// 带参数
throw new BusinessException("Errors.NotFound", resourceId);
```

### 5. DTO 验证特性多语言

```csharp
[Display(Name = "Content", ResourceType = typeof(DisplayResources))]
[Required(ErrorMessageResourceType = typeof(ValidationResources), 
         ErrorMessageResourceName = nameof(ValidationResources.Required))]
[StringLength(2000, 
    ErrorMessageResourceType = typeof(ValidationResources),
    ErrorMessageResourceName = nameof(ValidationResources.StringLengthMax))]
public string Content { get; set; } = string.Empty;
```

### 6. 前端使用

#### JavaScript

```javascript
// 获取翻译文本
const message = CodeSpirit.i18n.t('Common.Save');

// 带参数
const message = CodeSpirit.i18n.t('Validation.Required', { 0: '用户名' });

// 切换语言
CodeSpirit.i18n.switchLanguage('en');
```

#### Razor 页面

```razor
@inject IStringLocalizer<Shared> Localizer

<h1>@Localizer["Common.Save"]</h1>
```

## 语言配置管理

### 通过 Settings 组件

语言配置存储在 Settings 组件中：

- **全局默认**：`Module: "Localization"`, `Key: "DefaultLanguage"`, `Scope: Global`
- **租户默认**：`Module: "Localization"`, `Key: "DefaultLanguage"`, `Scope: Tenant`
- **用户偏好**：`Module: "Localization"`, `Key: "PreferredLanguage"`, `Scope: User`

### 优先级

语言解析按以下优先级：

1. **Cookie**（用户手动切换）
2. **User Settings**（用户偏好）
3. **Tenant Settings**（租户默认）
4. **Global Settings**（系统默认）
5. **Fallback**（"zh-CN"）

## 资源文件

组件提供了以下资源文件：

- `Shared.resx` / `Shared.en.resx` - 通用 UI 文本
- `Errors.resx` / `Errors.en.resx` - 错误消息
- `Validation.resx` / `Validation.en.resx` - 验证消息
- `Display.resx` / `Display.en.resx` - 字段显示名称

## 扩展支持

### 添加新语言

1. 在 `appsettings.json` 中添加支持的语言
2. 添加对应的资源文件（如 `Shared.ja.resx`）
3. 下载对应的 AMIS locale 文件
4. 在语言切换器添加选项

### 添加自定义资源

在各 API 服务中创建独立的资源文件：

```
Src/ApiServices/CodeSpirit.ExamApi/Resources/
├── ExamResources.resx
└── ExamResources.en.resx
```

## 参考

- [ASP.NET Core 全球化和本地化](https://learn.microsoft.com/zh-cn/aspnet/core/fundamentals/localization)
- [AMIS 国际化文档](https://aisuda.bce.baidu.com/amis/zh-CN/docs/extend/i18n)
