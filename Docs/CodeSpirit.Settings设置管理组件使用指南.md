# CodeSpirit.Settings设置管理组件使用指南

## 1. 组件介绍

CodeSpirit.Settings是码灵框架中的设置管理组件，提供了一套完整的应用配置管理解决方案。该组件支持全局设置和用户个性化设置管理，可以方便地对应用程序的各种配置进行集中管理，同时保留配置历史记录以便审计和回滚。

### 1.1 主要功能

- **多级设置管理**：支持全局设置、用户设置、模块设置、组织设置和角色设置
- **设置项类型丰富**：支持字符串、数值、布尔值、日期时间、JSON、单选、多选等多种数据类型
- **历史记录追踪**：记录设置变更历史，支持版本追踪和审计
- **批量操作**：支持批量设置导入导出，便于环境迁移和备份
- **类型化访问**：提供泛型方法和扩展方法，方便进行类型转换
- **易于集成**：提供简单的扩展方法快速集成到现有应用

## 2. 快速入门

### 2.1 安装和配置

将CodeSpirit.Settings添加到您的项目中，然后在服务配置中添加设置管理服务：

```csharp
// 在Program.cs或Startup.cs中配置服务
builder.Services.AddSettingsManagerWithDatabase(
    builder.Configuration,
    options => options.UseSqlServer(connectionString)
);

// 在应用程序启动时初始化设置数据库
var app = builder.Build();
await app.UseSettingsManagerAsync();
```

### 2.2 基本用法

#### 获取设置值

```csharp
// 注入设置服务
private readonly ISettingsService _settingsService;

public MyService(ISettingsService settingsService)
{
    _settingsService = settingsService;
}

// 获取全局设置
var value = await _settingsService.GetGlobalSettingAsync("System", "Theme");

// 获取用户设置
var userValue = await _settingsService.GetUserSettingAsync("System", "Theme", userId);

// 获取类型化设置
var config = await _settingsService.GetGlobalSettingAsync<AppConfig>("System", "Configuration");
```

#### 设置值

```csharp
// 设置全局设置
await _settingsService.SetGlobalSettingAsync("System", "Theme", "Dark", "更新默认主题");

// 设置用户设置
await _settingsService.SetUserSettingAsync("System", "Theme", "Light", userId, "用户偏好设置");

// 设置对象
var config = new AppConfig { /* 配置内容 */ };
await _settingsService.SetGlobalSettingAsync("System", "Configuration", config);
```

## 3. 核心概念

### 3.1 设置项结构

设置项由以下核心字段组成：

- **模块(Module)**：设置所属的模块或功能区域
- **键(Key)**：设置的唯一标识符
- **值(Value)**：设置的实际值
- **名称(Name)**：设置的显示名称
- **描述(Description)**：设置的详细说明
- **值类型(ValueType)**：设置值的数据类型
- **范围(Scope)**：设置的应用范围
- **作用对象ID(ScopeId)**：当范围不是全局时，指定设置适用的对象ID

### 3.2 设置范围

设置范围定义了设置的作用域：

- **全局(Global)**：适用于整个应用的设置
- **用户(User)**：用户个性化设置
- **模块(Module)**：特定模块的设置
- **组织(Organization)**：组织级别的设置
- **角色(Role)**：角色级别的设置

### 3.3 设置值类型

支持多种设置值类型，以适应不同的配置需求：

- **字符串(String)**：文本类型
- **整数(Integer)**：整数类型
- **布尔值(Boolean)**：逻辑类型
- **小数(Decimal)**：浮点数类型
- **日期时间(DateTime)**：日期时间类型
- **JSON(Json)**：JSON格式的复杂数据
- **单选(Select)**：单选项
- **多选(MultiSelect)**：多选项
- **密码(Password)**：加密存储的密码
- **富文本(RichText)**：富文本格式
- **颜色(Color)**：颜色值

## 4. 高级功能

### 4.1 设置项定义管理

```csharp
// 创建设置项定义
var settingItem = new SettingItem
{
    Module = "System",
    Key = "MaxFileSize",
    Name = "最大文件大小",
    Description = "上传文件的最大大小限制(MB)",
    Value = "10",
    ValueType = SettingValueType.Integer,
    Scope = SettingScope.Global
};

await _settingsService.CreateOrUpdateSettingDefinitionAsync(settingItem);

// 获取设置项定义
var definition = await _settingsService.GetSettingDefinitionAsync("System", "MaxFileSize");
```

### 4.2 设置历史管理

```csharp
// 获取设置历史记录
var history = await _settingsService.GetSettingHistoryAsync("System", "Theme");
```

### 4.3 设置导入导出

```csharp
// 导出设置
var exportData = await _settingsService.ExportSettingsAsync("System");

// 导入设置
await _settingsService.ImportSettingsAsync("System", exportData);
```

### 4.4 批量设置管理

```csharp
// 批量设置全局配置
var settings = new Dictionary<string, string>
{
    ["Theme"] = "Dark",
    ["Language"] = "zh-CN",
    ["TimeZone"] = "Asia/Shanghai"
};

await _settingsService.BatchSetGlobalSettingsAsync("System", settings, "系统初始化");

// 批量设置用户配置
await _settingsService.BatchSetUserSettingsAsync("System", settings, userId, "用户偏好初始化");
```

### 4.5 扩展方法

可以使用Settings扩展方法轻松从设置字典中获取不同类型的值：

```csharp
// 获取所有设置
var allSettings = await _settingsService.GetAllGlobalSettingsAsync("System");

// 使用扩展方法获取类型化值
int maxSize = allSettings.GetInt("MaxFileSize", 10);
bool isDarkMode = allSettings.GetBool("DarkMode", false);
decimal taxRate = allSettings.GetDecimal("TaxRate", 0.17m);
AppConfig config = allSettings.GetJson<AppConfig>("Configuration");
```

## 5. 最佳实践

### 5.1 模块化设置

将应用设置按功能模块组织，例如：

- `System`: 系统级配置
- `Security`: 安全相关设置
- `UI`: 用户界面设置
- `Notification`: 通知相关设置

### 5.2 设置缓存

对于频繁访问的设置，考虑实现缓存层以提高性能：

```csharp
// 缓存键格式：{Module}:{Scope}:{ScopeId}:{Key}
var cacheKey = $"Settings:System:Global::{key}";
```

### 5.3 定义常量

为常用设置键定义常量，避免硬编码：

```csharp
public static class SettingKeys
{
    public static class System
    {
        public const string Theme = "Theme";
        public const string Language = "Language";
        public const string TimeZone = "TimeZone";
    }
}
```

## 6. 集成示例

### 6.1 与ASP.NET Core集成

```csharp
// 在控制器中使用
[ApiController]
[Route("api/[controller]")]
public class SettingsController : ControllerBase
{
    private readonly ISettingsService _settingsService;
    
    public SettingsController(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }
    
    [HttpGet("global/{module}/{key}")]
    public async Task<IActionResult> GetGlobalSetting(string module, string key)
    {
        var value = await _settingsService.GetGlobalSettingAsync(module, key);
        return Ok(value);
    }
    
    [HttpGet("user/{module}/{key}")]
    public async Task<IActionResult> GetUserSetting(string module, string key)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var value = await _settingsService.GetUserSettingAsync(module, key, userId);
        return Ok(value);
    }
}
```

### 6.2 与CodeSpirit.Amis集成

可以利用CodeSpirit.Amis组件自动生成设置管理界面：

```csharp
// 在Amis页面中添加设置表单
[HttpGet("settings/form")]
public IActionResult GetSettingsForm()
{
    var form = new Form
    {
        Title = "系统设置",
        Mode = "horizontal",
        Controls = new List<FormControl>
        {
            new FormControlSwitch { Label = "暗黑模式", Name = "darkMode" },
            new FormControlSelect { Label = "语言", Name = "language", Options = new[] { "中文", "英文" } },
            new FormControlNumber { Label = "每页显示数", Name = "pageSize", Min = 10, Max = 100 }
        }
    };
    
    return Ok(form.ToAmisSchema());
}
```

## 7. 设置管理API参考

组件提供了全面的API支持各种设置管理操作：

### 全局设置管理
- `GetGlobalSettingAsync(string module, string key)` - 获取全局设置
- `GetGlobalSettingAsync<T>(string module, string key)` - 获取类型化全局设置
- `GetAllGlobalSettingsAsync(string module)` - 获取模块的所有全局设置
- `SetGlobalSettingAsync(string module, string key, string value, string? reason)` - 设置全局设置
- `SetGlobalSettingAsync<T>(string module, string key, T value, string? reason)` - 设置类型化全局设置
- `BatchSetGlobalSettingsAsync(string module, Dictionary<string, string> settings, string? reason)` - 批量设置全局设置

### 用户设置管理
- `GetUserSettingAsync(string module, string key, string userId)` - 获取用户设置
- `GetUserSettingAsync<T>(string module, string key, string userId)` - 获取类型化用户设置
- `GetAllUserSettingsAsync(string module, string userId)` - 获取用户的所有设置
- `SetUserSettingAsync(string module, string key, string value, string userId, string? reason)` - 设置用户设置
- `SetUserSettingAsync<T>(string module, string key, T value, string userId, string? reason)` - 设置类型化用户设置
- `BatchSetUserSettingsAsync(string module, Dictionary<string, string> settings, string userId, string? reason)` - 批量设置用户设置
- `ResetUserSettingToDefaultAsync(string module, string? key, string userId)` - 重置用户设置为全局默认值

### 设置定义管理
- `GetSettingDefinitionAsync(string module, string key)` - 获取设置定义
- `GetAllSettingDefinitionsAsync(string module)` - 获取模块的所有设置定义
- `CreateOrUpdateSettingDefinitionAsync(SettingItem settingItem)` - 创建或更新设置定义
- `DeleteSettingDefinitionAsync(string module, string key)` - 删除设置定义

### 设置历史和导入导出
- `GetSettingHistoryAsync(string module, string key)` - 获取设置历史
- `ExportSettingsAsync(string module)` - 导出设置
- `ImportSettingsAsync(string module, string settingsJson)` - 导入设置

## 8. 总结

CodeSpirit.Settings组件提供了一套完整的设置管理解决方案，支持多级设置、类型化访问、版本历史等功能，可以满足各种应用场景下的配置管理需求。通过本组件，开发人员可以方便地实现集中化配置管理，提高应用的可配置性和用户体验。 