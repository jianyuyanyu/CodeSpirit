# CodeSpirit.Settings设置管理组件使用指南

## 1. 组件介绍

CodeSpirit.Settings 是码灵框架中的**业务级设置管理组件**，提供了一套完整的多级设置管理解决方案。该组件支持全局设置、用户个性化设置、租户设置、组织设置等多种设置范围，可以方便地对应用程序的业务配置进行集中管理，同时保留配置历史记录以便审计和回滚。

> **💡 组件定位**：Settings 组件专注于**业务级、多级设置**（用户偏好、租户配置等），与专注于**框架级、应用级配置**（JWT、LLM 等）的 [配置中心](./config-center-architecture-overview-zh-CN.md) 互补。详见 [Settings 组件 vs 配置中心](#9-settings-组件-vs-配置中心)。

![ScreenShot_2026-01-06_205544_708](../../Res/ScreenShot_2026-01-06_205544_708.png)

### 1.1 主要功能

- **多级设置管理**：支持全局设置、用户设置、租户设置、模块设置、组织设置和角色设置，不同级别的设置可以继承和覆盖

- **设置项类型丰富**：支持字符串、数值、布尔值、日期时间、JSON、单选、多选等多种数据类型

- **历史记录追踪**：记录设置变更历史，支持版本追踪和审计

- **批量操作**：支持批量设置导入导出，便于环境迁移和备份

- **类型化访问**：提供 `[SettingsDto]` 特性和泛型方法，实现类型安全的设置访问

- **灵活继承**：租户设置可继承全局设置，用户设置可继承租户设置，支持多级继承和覆盖

- **易于集成**：提供简单的 API 设计，通过 `ISettingsService` 快速集成到现有应用

  ![image-20260106224425135](../../Res/image-20260106224425135.png)

### 1.2 与配置中心的区别

Settings 组件与配置中心（ConfigCenter）**互补而非重复**：

- **Settings 组件**：管理**业务级、多级设置**，如用户偏好、租户配置、模块参数等，不同用户/租户有不同的值
- **配置中心**：管理**框架级、应用级配置**，如 JWT、LLM、数据库连接等，所有服务实例共享相同的值

> 详细对比请参考 [Settings 组件 vs 配置中心](#8-settings-组件-vs-配置中心) 章节

## 2. 快速入门

### 2.1 基本用法

#### 获取设置值

```csharp
// 注入设置服务
private readonly ISettingsService _settingsService;

public MyService(ISettingsService settingsService)
{
    _settingsService = settingsService;
}

// 方式一：传统方式（需要手动传入 module 和 key）
var value = await _settingsService.GetGlobalSettingAsync("System", "Theme");
var userValue = await _settingsService.GetUserSettingAsync("System", "Theme", userId);
var config = await _settingsService.GetGlobalSettingAsync<AppConfig>("System", "Configuration");

// 方式二：使用 SettingsDto 特性（推荐，避免字符串拼写错误）
// 1. 定义 DTO 并添加特性
[SettingsDto("System", "Theme")]
public class ThemeSettingsDto
{
    public string Theme { get; set; } = "Light";
}

// 2. 使用简化 API（自动从特性获取 module/key）
var themeSettings = await _settingsService.GetGlobalSettingAsync<ThemeSettingsDto>();
var userThemeSettings = await _settingsService.GetUserSettingAsync<ThemeSettingsDto>(userId);
var tenantThemeSettings = await _settingsService.GetTenantSettingAsync<ThemeSettingsDto>(tenantId);
```

#### 设置值

```csharp
// 方式一：传统方式（需要手动传入 module 和 key）
await _settingsService.SetGlobalSettingAsync("System", "Theme", "Dark", "更新默认主题");
await _settingsService.SetUserSettingAsync("System", "Theme", "Light", userId, "用户偏好设置");

var config = new AppConfig { /* 配置内容 */ };
await _settingsService.SetGlobalSettingAsync("System", "Configuration", config);

// 方式二：使用 SettingsDto 特性（推荐）
[SettingsDto("System", "Theme")]
public class ThemeSettingsDto
{
    public string Theme { get; set; } = "Light";
}

var themeSettings = new ThemeSettingsDto { Theme = "Dark" };
await _settingsService.SetGlobalSettingAsync(themeSettings, "更新默认主题");
await _settingsService.SetTenantSettingAsync(themeSettings, tenantId, "租户主题设置");
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
- **租户(Tenant)**：租户级别的设置

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

### 4.3 租户设置管理

租户设置允许为每个租户配置独立的设置值，当租户设置不存在时，会自动继承全局设置。

```csharp
// 获取租户设置（如果租户设置不存在，返回全局设置）
var tenantTheme = await _settingsService.GetTenantSettingAsync("System", "Theme", tenantId);

// 设置租户设置
await _settingsService.SetTenantSettingAsync("System", "Theme", "Dark", tenantId, "租户主题设置");

// 获取租户所有设置（合并全局设置和租户设置）
var allTenantSettings = await _settingsService.GetAllTenantSettingsAsync("System", tenantId);

// 批量设置租户设置
var settings = new Dictionary<string, string>
{
    { "Theme", "Dark" },
    { "Language", "zh-CN" }
};
await _settingsService.BatchSetTenantSettingsAsync("System", settings, tenantId, "批量更新租户设置");

// 重置租户设置为全局默认值
await _settingsService.ResetTenantSettingToDefaultAsync("System", "Theme", tenantId);
```

**租户设置与全局设置的关系：**
- 租户设置优先于全局设置
- 如果租户设置不存在，自动返回全局设置值
- 重置租户设置后，将恢复为全局设置值
- 租户设置完全隔离，不同租户之间的设置互不影响

### 4.4 设置导入导出

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

### 5.3 使用 SettingsDto 特性（推荐）

为设置 DTO 添加 `[SettingsDto]` 特性，可以简化 API 调用并避免模块名/配置键字符串不一致的问题：

```csharp
// 定义设置 DTO
[SettingsDto("ThirdPartyLogin", "WeChat")]
public class WeChatLoginSettingsDto
{
    public string AppId { get; set; } = string.Empty;
    public string AppSecret { get; set; } = string.Empty;
    public bool Enabled { get; set; } = false;
}

// 读取设置（无需手动传入 module/key）
var settings = await _settingsService.GetTenantSettingAsync<WeChatLoginSettingsDto>(tenantId);

// 保存设置
await _settingsService.SetTenantSettingAsync(dto, tenantId, "更新微信配置");
```

**优势：**
- ✅ 类型安全：编译时检查，IDE 智能提示
- ✅ 集中管理：配置键在 DTO 类上定义，修改只需改一处
- ✅ 避免不一致：消除模块名/配置键字符串拼写错误
- ✅ 性能优化：反射结果自动缓存，每个类型只反射一次

### 5.4 定义常量（传统方式）

如果使用传统方式，为常用设置键定义常量，避免硬编码：

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

## 7. 设置管理 API 参考

组件提供了全面的 API 支持各种设置管理操作：

### 全局设置管理
- `GetGlobalSettingAsync(string module, string key)` - 获取全局设置
- `GetGlobalSettingAsync<T>(string module, string key)` - 获取类型化全局设置
- `GetGlobalSettingAsync<T>()` - 获取类型化全局设置（从 DTO 特性自动获取 module/key，需标记 `[SettingsDto]`）
- `GetAllGlobalSettingsAsync(string module)` - 获取模块的所有全局设置
- `SetGlobalSettingAsync(string module, string key, string value, string? reason)` - 设置全局设置
- `SetGlobalSettingAsync<T>(string module, string key, T value, string? reason)` - 设置类型化全局设置
- `SetGlobalSettingAsync<T>(T value, string? reason)` - 设置类型化全局设置（从 DTO 特性自动获取 module/key，需标记 `[SettingsDto]`）
- `BatchSetGlobalSettingsAsync(string module, Dictionary<string, string> settings, string? reason)` - 批量设置全局设置

### 用户设置管理
- `GetUserSettingAsync(string module, string key, string userId)` - 获取用户设置
- `GetUserSettingAsync<T>(string module, string key, string userId)` - 获取类型化用户设置
- `GetUserSettingAsync<T>(string userId)` - 获取类型化用户设置（从 DTO 特性自动获取 module/key，需标记 `[SettingsDto]`）
- `GetAllUserSettingsAsync(string module, string userId)` - 获取用户的所有设置
- `SetUserSettingAsync(string module, string key, string value, string userId, string? reason)` - 设置用户设置
- `SetUserSettingAsync<T>(string module, string key, T value, string userId, string? reason)` - 设置类型化用户设置
- `SetUserSettingAsync<T>(T value, string userId, string? reason)` - 设置类型化用户设置（从 DTO 特性自动获取 module/key，需标记 `[SettingsDto]`）
- `BatchSetUserSettingsAsync(string module, Dictionary<string, string> settings, string userId, string? reason)` - 批量设置用户设置
- `ResetUserSettingToDefaultAsync(string module, string? key, string userId)` - 重置用户设置为全局默认值

### 租户设置管理
- `GetTenantSettingAsync(string module, string key, string tenantId)` - 获取租户设置
- `GetTenantSettingAsync<T>(string module, string key, string tenantId)` - 获取类型化租户设置
- `GetTenantSettingAsync<T>(string tenantId)` - 获取类型化租户设置（从 DTO 特性自动获取 module/key，需标记 `[SettingsDto]`）
- `GetAllTenantSettingsAsync(string module, string tenantId)` - 获取租户的所有设置
- `SetTenantSettingAsync(string module, string key, string value, string tenantId, string? reason)` - 设置租户设置
- `SetTenantSettingAsync<T>(string module, string key, T value, string tenantId, string? reason)` - 设置类型化租户设置
- `SetTenantSettingAsync<T>(T value, string tenantId, string? reason)` - 设置类型化租户设置（从 DTO 特性自动获取 module/key，需标记 `[SettingsDto]`）
- `BatchSetTenantSettingsAsync(string module, Dictionary<string, string> settings, string tenantId, string? reason)` - 批量设置租户设置
- `ResetTenantSettingToDefaultAsync(string module, string? key, string tenantId)` - 重置租户设置为全局默认值

### 设置定义管理
- `GetSettingDefinitionAsync(string module, string key)` - 获取设置定义
- `GetAllSettingDefinitionsAsync(string module)` - 获取模块的所有设置定义
- `CreateOrUpdateSettingDefinitionAsync(SettingItem settingItem)` - 创建或更新设置定义
- `DeleteSettingDefinitionAsync(string module, string key)` - 删除设置定义

### 设置历史和导入导出
- `GetSettingHistoryAsync(string module, string key)` - 获取设置历史
- `ExportSettingsAsync(string module)` - 导出设置
- `ImportSettingsAsync(string module, string settingsJson)` - 导入设置

## 8. Settings 组件 vs 配置中心

CodeSpirit 提供了两套配置管理方案，分别服务于不同的场景。理解它们的区别对于正确使用至关重要。

### 核心区别

| 维度 | **Settings 组件（本组件）** | **配置中心（ConfigCenter）** |
|------|---------------------------|----------------------------|
| **定位** | 业务级设置管理 | 框架级配置管理 |
| **配置类型** | 动态业务设置 | 静态基础配置 |
| **典型配置** | • 用户主题偏好<br>• 租户功能开关<br>• 模块参数配置<br>• 组织规则设置<br>• 角色权限配置 | • JWT 认证参数<br>• LLM API 密钥<br>• 数据库连接<br>• 审计日志配置<br>• 日志级别 |
| **配置粒度** | 多级（全局/用户/租户/组织/角色/模块） | 应用级（按 AppId） |
| **变更频率** | 高（用户/租户可自行配置） | 低（管理员操作） |
| **推送机制** | ❌ 按需读取，无推送 | ✅ SSE 实时推送到所有实例 |
| **集成方式** | 通过 `ISettingsService` 调用 | 集成到 `IConfiguration` |
| **自动热更新** | ❌ 需主动重新读取 | ✅ 配置变更自动生效 |
| **访问权限** | 用户/租户可自行配置 | 仅管理员（系统平台） |
| **作用范围** | 影响用户体验和业务逻辑 | 影响整个应用运行 |

### 如何选择

#### ✅ 使用 Settings 组件（本组件）：
**业务级动态设置**，不同用户/租户有不同的设置值

**典型场景**：
- 用户个性化设置：主题、语言、时区
- 租户自定义配置：Logo、品牌色、公司信息、功能开关
- 模块参数配置：每页显示数、审批流程配置
- 组织规则设置：考勤规则、报销标准
- 角色权限配置：默认权限、数据权限

**代码示例**：
```csharp
// 获取租户的微信登录配置（不同租户不同配置）
[SettingsDto("ThirdPartyLogin", "WeChat")]
public class WeChatLoginSettingsDto
{
    public string AppId { get; set; } = string.Empty;
    public string AppSecret { get; set; } = string.Empty;
    public bool Enabled { get; set; } = false;
}

var wechatConfig = await _settingsService.GetTenantSettingAsync<WeChatLoginSettingsDto>(tenantId);
```

#### ✅ 使用配置中心：
**框架级基础配置**，影响应用启动和运行，需要实时推送到所有服务实例

**典型场景**：
- JWT 认证配置（SecretKey、Issuer、Audience）
- LLM API 配置（ApiKey、ModelName、Temperature）
- 数据库连接字符串
- 审计日志配置（GreptimeDB 连接）
- 缓存配置（Redis 连接）
- 消息队列配置（RabbitMQ 连接）

**代码示例**：
```csharp
// 从配置中心读取（已自动加载到 IConfiguration）
var apiKey = _configuration["LLM:ApiKey"];
```

### 协同使用示例

在实际项目中，两者通常协同使用：

```csharp
// 场景：AI 功能的使用

// 1. 配置中心：LLM 全局配置（所有租户共享同一个 LLM 服务）
var llmApiKey = _configuration["LLM:ApiKey"];  // 从配置中心读取
var llmModelName = _configuration["LLM:ModelName"];

// 2. Settings 组件：租户级 AI 功能开关（租户可以选择启用/禁用 AI 功能）
[SettingsDto("AI", "Features")]
public class AiFeaturesSettingsDto
{
    public bool EnableAiFormFill { get; set; } = true;      // 启用 AI 表单填充
    public bool EnableSmartApproval { get; set; } = true;   // 启用智能审批
    public bool EnableAiCards { get; set; } = false;        // 启用 AI 卡片
}

var aiFeaturesConfig = await _settingsService.GetTenantSettingAsync<AiFeaturesSettingsDto>(tenantId);

// 3. 使用：先检查租户是否启用 AI 功能，再使用配置中心的 LLM 配置调用 API
if (aiFeaturesConfig.EnableAiFormFill)
{
    // 调用 LLM API（使用配置中心的全局配置）
    var llmClient = new LLMClient(llmApiKey, llmModelName);
    // ...
}
```

### 快速判断

**一个简单的判断标准**：

- 📌 **所有服务实例都使用相同的值** → 使用配置中心
- 📌 **不同用户/租户/组织有不同的值** → 使用 Settings 组件

**另一个判断标准**：

- 📌 **配置变更需要实时推送到所有实例** → 使用配置中心
- 📌 **配置变更只影响当前用户/租户** → 使用 Settings 组件

> **💡 详细对比**：完整的对比说明请参考 [配置中心架构概览 - 配置中心 vs Settings 组件](./config-center-architecture-overview-zh-CN.md#配置中心-vs-settings-组件)

---

## 9. 总结

CodeSpirit.Settings 组件提供了一套完整的**业务级、多级设置**管理解决方案，支持多级设置、类型化访问、版本历史等功能，可以满足各种应用场景下的业务配置管理需求。

**核心特点**：
- ✅ **多级设置**：支持全局、用户、租户、组织、角色、模块等多种设置范围
- ✅ **类型化访问**：通过 `[SettingsDto]` 特性提供类型安全的设置访问
- ✅ **灵活继承**：租户/用户设置可继承全局默认值
- ✅ **历史追踪**：完整的设置变更历史记录
- ✅ **易于集成**：简单的 API 设计，方便业务代码调用

**适用场景**：
- 用户个性化设置
- 租户自定义配置
- 模块参数配置
- 组织规则设置
- 角色权限配置

通过 Settings 组件与配置中心的协同使用，CodeSpirit 提供了完整的配置管理体系，既满足框架级配置的实时推送需求，又满足业务级设置的灵活配置需求。 