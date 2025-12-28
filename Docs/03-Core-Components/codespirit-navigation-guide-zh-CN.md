# CodeSpirit.Navigation 导航组件

## 概述

`CodeSpirit.Navigation` 是 CodeSpirit 框架的核心导航组件，提供智能化的导航树构建、权限过滤和缓存管理功能。支持基于代码特性和配置文件的双重导航定义方式，实现了高度灵活和可扩展的导航管理解决方案。

**当前版本：2.2.0** - 支持多平台、元数据丰富、上下文感知的高级导航框架

> **架构特性**：采用职责分离的服务模式，过滤器责任链模式统一过滤逻辑，优化的单一缓存策略，智能版本控制机制。

### 🎯 核心亮点

- **智能属性优先级策略**：`NavigationAttribute` 优先于 `ModuleAttribute`，提供从简单到复杂的渐进式开发体验
- **多平台支持**：系统平台、租户平台的独立管理和过滤
- **权限深度集成**：自动权限生成和过滤，支持多级权限检查
- **高性能缓存**：单一缓存 + 内存过滤策略，内存占用降低 66%
- **智能版本控制**：基于内容哈希的版本管理，支持HTTP ETag标准缓存，自动检测导航变化
- **扩展属性丰富**：15+ 个导航属性，支持分组、标签、版本约束、设备适配等
- **架构优化**：职责分离的服务模式，8个内置过滤器，支持动态注册自定义过滤器

## 主要特性

### 核心功能 ✅
- **智能导航构建**：自动扫描控制器和动作方法的特性，构建导航树
- **配置文件支持**：支持通过 `appsettings.json` 定义导航结构
- **权限集成**：集成权限系统，自动过滤用户无权访问的导航项
- **分布式缓存**：使用分布式缓存提升导航树查询性能
- **版本控制**：基于内容哈希的版本管理，支持HTTP ETag标准缓存
- **模块化设计**：支持多模块导航管理
- **层次结构**：支持多级导航菜单结构
- **外部链接**：支持外部链接和 iframe 嵌入
- **图标支持**：支持 FontAwesome 等图标库
- **优先级策略**：`NavigationAttribute` 优先于 `ModuleAttribute`，提供精细化控制

### 2.2.0 新特性 🎉
- **版本控制机制**：基于内容哈希的版本管理系统，支持HTTP ETag标准缓存
- **自动失效检测**：导航内容变化时自动更新版本，无需手动清除缓存
- **单对象封装**：版本号和导航数据封装在一起，仅占用1个Redis键，避免缓存膨胀
- **客户端智能缓存**：支持ETag标准，客户端可自动判断是否需要更新导航数据

### 2.1.0 新特性 🎉
- **架构优化**：职责分离的服务模式，提升代码可维护性
- **过滤器体系**：责任链模式的过滤器体系，8个内置过滤器，支持动态注册
- **缓存优化**：单一缓存 + 内存过滤策略，内存占用降低 66%
- **测试覆盖**：完整的单元测试覆盖（108个测试用例）
- **多服务支持**：支持多个 API 服务独立初始化，自动合并导航模块

### 2.0.0 新特性 🎉
- **多平台支持**：`PlatformType` 枚举（None, System, Tenant, Both）
- **扩展属性**：15+ 个新的导航属性（分组、标签、元数据、版本约束等）
- **高级过滤**：基于上下文的多维度过滤（平台、权限、版本、设备等）
- **深拷贝支持**：NavigationNode 深拷贝方法
- **版本约束**：内置版本比较逻辑，支持语义化版本
- **设备类型**：支持按设备类型过滤导航项
- **实验性功能**：开发环境实验性功能支持
- **徽章系统**：导航项徽章显示支持
- **智能属性合并**：`NavigationAttribute` 与 `ModuleAttribute` 的智能优先级处理

## ⚡ 5分钟快速入门

### 最简单的开始方式

```csharp
// 🚀 只需要一个 ModuleAttribute，立即拥有导航功能
[Module("UserManagement", "用户管理", Icon = "fa-solid fa-users")]
public class UserController : ControllerBase
{
    public IActionResult Index() => View();
}
```

**结果**：自动生成完整的导航节点，包含标题、图标、权限等。

### 进阶配置

```csharp
// 🎯 添加 NavigationAttribute 进行精细控制
[Module("UserManagement", "用户管理", Icon = "fa-solid fa-users")]
[Navigation(
    Title = "用户中心",           // 覆盖模块标题
    Order = 1,                   // 设置排序
    Group = "Management"         // 添加分组
)]
public class UserController : ControllerBase
{
    public IActionResult Index() => View();
}
```

**结果**：使用 NavigationAttribute 的精确配置，同时保留 ModuleAttribute 未覆盖的属性。

### 开发调试

```csharp
// 🔧 临时隐藏功能，保留配置
[Module("UserManagement", "用户管理", Icon = "fa-solid fa-users")]
[Navigation(Hidden = true)]   // 一键隐藏/显示
public class UserController : ControllerBase
{
    public IActionResult Index() => View();
}
```

**结果**：导航被隐藏，但当设置 `Hidden = false` 时，立即恢复显示并使用 ModuleAttribute 的配置。

### 智能优先级一览表

| 你的代码 | 系统行为 | 适用场景 |
|---------|----------|----------|
| 只有 `[Module]` | ✅ 自动创建导航 | 快速原型开发 |
| `[Module]` + `[Navigation]` | ✅ Navigation 优先 | 精细化控制 |
| `[Module]` + `[Navigation(Hidden=true)]` | ✅ 回退到 Module | 临时隐藏 |
| 都没有 | ❌ 不显示导航 | 内部API |

## 项目结构

```
CodeSpirit.Navigation/
├── Models/
│   ├── NavigationNode.cs           # 导航节点模型
│   └── NavigationConfiguration.cs  # 配置项模型
├── Services/
│   ├── INavigationService.cs        # 导航服务接口
│   ├── NavigationService.cs         # 导航服务主实现
│   ├── INavigationTreeBuilder.cs    # 导航树构建器接口
│   ├── NavigationTreeBuilder.cs     # 导航树构建器实现
│   ├── INavigationCacheManager.cs   # 缓存管理器接口
│   ├── NavigationCacheManager.cs    # 缓存管理器实现
│   ├── INavigationFilterService.cs  # 过滤服务接口
│   ├── NavigationFilterService.cs   # 过滤服务实现
│   └── Filters/                     # 过滤器目录
│       ├── INavigationFilter.cs     # 过滤器接口
│       ├── PlatformFilter.cs        # 平台过滤器
│       ├── PermissionFilter.cs      # 权限过滤器
│       ├── AuthenticationFilter.cs  # 认证过滤器
│       ├── VersionFilter.cs         # 版本过滤器
│       ├── DeviceFilter.cs          # 设备过滤器
│       ├── ExperimentalFilter.cs    # 实验性功能过滤器
│       ├── GroupFilter.cs           # 分组过滤器
│       └── TagFilter.cs             # 标签过滤器
├── Extensions/
│   └── ServiceCollectionExtensions.cs # 依赖注入扩展
├── README.md                        # 主文档
```

## 快速开始

### 1. 服务注册

在 `Program.cs` 中注册导航服务：

```csharp
// 注册导航服务（自动注册所有服务和过滤器）
builder.Services.AddCodeSpiritNavigation();

// 在应用启动时初始化导航树
var app = builder.Build();
await app.UseCodeSpiritNavigationAsync();
```

**服务注册说明**：
- `INavigationTreeBuilder` - 导航树构建器
- `INavigationCacheManager` - 缓存管理器
- `INavigationFilterService` - 过滤服务
- `INavigationService` - 主服务
- 8个内置过滤器（Platform, Permission, Authentication, Version, Device, Experimental, Group, Tag）

**多服务初始化说明**：
在分布式环境中，多个 API 服务（如 identity、exam、survey 等）都会调用 `UseCodeSpiritNavigationAsync()`。导航组件采用**合并策略**：
- 第一个服务初始化时，直接写入缓存
- 后续服务初始化时，检查缓存是否存在
- 如果缓存存在，将新模块合并到现有缓存中（按模块名去重）
- 如果缓存不存在，直接写入缓存

这样可以确保所有服务的导航模块都能正确累积到统一的导航树中。

### 2. 使用特性定义导航

## 🎯 智能属性优先级策略

### 策略概述

导航组件实现了智能的属性优先级策略，让开发者可以采用渐进式的方式构建导航：

1. **快速原型阶段**：仅使用 `ModuleAttribute` 快速搭建基础导航
2. **精细化阶段**：添加 `NavigationAttribute` 进行详细配置 
3. **动态控制阶段**：使用 `Hidden` 属性灵活控制显示/隐藏

### 优先级规则 📋

| 场景 | NavigationAttribute | ModuleAttribute | 结果 |
|------|-------------------|-----------------|------|
| 场景1 | ❌ 不存在 | ✅ 存在 | 使用 ModuleAttribute 创建导航 |
| 场景2 | ✅ 存在且 Hidden=false | ✅ 存在 | 使用 NavigationAttribute（覆盖） |
| 场景3 | ⚠️ 存在但 Hidden=true | ✅ 存在 | 回退到 ModuleAttribute |
| 场景4 | ❌ 不存在 | ❌ 不存在 | 控制器不出现在导航中 |

### 详细示例 📝

#### 场景1：只有 ModuleAttribute - 自动创建导航

```csharp
/// <summary>
/// 只有模块属性，系统会自动创建导航节点
/// </summary>
[Module("UserManagement", "用户管理", Icon = "fa-solid fa-users")]
public class UserController : ControllerBase
{
    // ✅ 控制器会自动出现在导航中
    // 标题：用户管理 (来自 ModuleAttribute.DisplayName)
    // 图标：fa-solid fa-users (来自 ModuleAttribute.Icon)  
    // 权限：userManagement_user (自动生成)
    // 其他属性：使用默认值

    public IActionResult Index() => View();
}
```

**生成的导航节点属性：**
```json
{
  "name": "user",
  "title": "用户管理",
  "icon": "fa-solid fa-users", 
  "permission": "userManagement_user",
  "platformType": "Both",
  "order": 0,
  "requireAuth": true,
  "supportedDevices": ["desktop", "tablet", "mobile"]
}
```

#### 场景2：NavigationAttribute 优先级覆盖

```csharp
/// <summary>
/// 同时存在两种属性，NavigationAttribute 优先
/// </summary>
[Module("UserManagement", "用户管理", Icon = "fa-solid fa-users")]
[Navigation(
    Title = "用户列表",                    // 🔄 覆盖 ModuleAttribute.DisplayName
    Icon = "fa-solid fa-user-list",        // 🔄 覆盖 ModuleAttribute.Icon
    Order = 1,                             // ➕ 新增属性
    Permission = "user_list_access",       // ➕ 自定义权限
    PlatformType = PlatformType.System,    // ➕ 平台限制
    Group = "Management",                  // ➕ 分组
    Tags = new[] { "admin", "user" },      // ➕ 标签
    Priority = 10,                         // ➕ 优先级
    Badge = "NEW",                         // ➕ 徽章
    BadgeType = "success"                  // ➕ 徽章样式
)]
public class UserController : ControllerBase
{
    // ✅ 最终使用 NavigationAttribute 的所有配置
    // 标题：用户列表 (不是"用户管理")
    // 图标：fa-solid fa-user-list (不是"fa-solid fa-users")
    
    public IActionResult Index() => View();
}
```

**最终生成的导航节点：**
```json
{
  "name": "user",
  "title": "用户列表",                    // NavigationAttribute 覆盖
  "icon": "fa-solid fa-user-list",        // NavigationAttribute 覆盖  
  "permission": "user_list_access",       // NavigationAttribute 设置
  "platformType": "System",               // NavigationAttribute 设置
  "group": "Management",                  // NavigationAttribute 设置
  "order": 1,
  "priority": 10,
  "badge": "NEW",
  "badgeType": "success"
}
```

#### 场景3：隐藏回退策略

```csharp
/// <summary>
/// NavigationAttribute 被隐藏时，回退到 ModuleAttribute
/// </summary>
[Module("UserManagement", "用户管理", Icon = "fa-solid fa-users")]
[Navigation(Hidden = true)]  // 🚫 NavigationAttribute 被隐藏
public class UserController : ControllerBase
{
    // ✅ 由于 NavigationAttribute.Hidden = true
    // 系统检查到 ModuleAttribute 存在，使用其属性创建导航
    // 标题：用户管理 (来自 ModuleAttribute)
    // 图标：fa-solid fa-users (来自 ModuleAttribute)
    
    public IActionResult Index() => View();
}
```

#### 场景4：临时隐藏和恢复

```csharp
/// <summary>
/// 开发阶段的导航控制示例
/// </summary>
[Module("ExperimentalFeature", "实验性功能", Icon = "fa-solid fa-flask")]
[Navigation(
    Hidden = false,                        // 🟢 开发环境显示
    // Hidden = true,                      // 🔴 生产环境隐藏
    IsExperimental = true,                 // 标记为实验性功能
    MinVersion = "2.1.0",                  // 版本限制
    Badge = "BETA",
    BadgeType = "warning"
)]
public class ExperimentalController : ControllerBase
{
    // 通过修改 Hidden 属性可以快速控制导航的显示/隐藏
    // 而无需删除整个 NavigationAttribute
}
```

### 属性映射对照表 📊

| NavigationAttribute | ModuleAttribute | 回退默认值 | 说明 |
|-------------------|-----------------|-----------|------|
| `Title` | `DisplayName` | `controllerName` | 显示标题 |
| `Icon` | `Icon` | `null` | 图标 |
| `Permission` | - | `moduleName_controllerName` | 权限码 |
| `Order` | - | `0` | 排序 |
| `PlatformType` | - | `Both` | 平台类型 |
| `Group` | - | `null` | 分组 |
| `Tags` | - | `[]` | 标签数组 |
| `RequireAuth` | - | `true` | 需要认证 |
| `Priority` | - | `0` | 优先级 |
| `Badge` | - | `null` | 徽章 |

### 渐进式开发工作流 🚀

#### 第一阶段：快速搭建

```csharp
// 1. 快速定义模块，立即可用
[Module("BlogManagement", "博客管理", Icon = "fa-solid fa-blog")]
public class BlogController : ControllerBase 
{
    public IActionResult Index() => View();
}
```

#### 第二阶段：基础优化

```csharp
// 2. 添加基础的 NavigationAttribute 配置
[Module("BlogManagement", "博客管理", Icon = "fa-solid fa-blog")]
[Navigation(
    Order = 2,                             // 调整排序
    Permission = "blog_management"         // 自定义权限
)]
public class BlogController : ControllerBase 
{
    public IActionResult Index() => View();
}
```

#### 第三阶段：完整配置

```csharp
// 3. 完整的导航配置
[Module("BlogManagement", "博客管理", Icon = "fa-solid fa-blog")]
[Navigation(
    Title = "博客文章",                    // 更精确的标题
    Icon = "fa-solid fa-pen-to-square",    // 更具体的图标
    Order = 2,
    Permission = "blog_management",
    PlatformType = PlatformType.Both,
    Group = "Content",                     // 内容管理分组
    Tags = new[] { "content", "cms" },     // 标签
    Priority = 5,
    Badge = "HOT",                         // 热门功能
    BadgeType = "danger",
    MinVersion = "2.0.0",                  // 版本要求
    SupportedDevices = new[] { "desktop", "tablet" }  // 设备限制
)]
public class BlogController : ControllerBase 
{
    public IActionResult Index() => View();
}
```

### 最佳实践建议 💡

#### ✅ 推荐做法

```csharp
// 1. 使用 ModuleAttribute 作为基础导航定义
[Module("OrderManagement", "订单管理", Icon = "fa-solid fa-shopping-cart")]
public class OrderController : ControllerBase { }

// 2. 仅在需要时添加 NavigationAttribute 覆盖特定属性
[Module("OrderManagement", "订单管理", Icon = "fa-solid fa-shopping-cart")]
[Navigation(Order = 1, Group = "Business")]  // 只覆盖需要的属性
public class OrderController : ControllerBase { }

// 3. 使用 Hidden 属性进行临时控制
[Module("OrderManagement", "订单管理", Icon = "fa-solid fa-shopping-cart")]
[Navigation(Hidden = true)]  // 临时隐藏，保留配置
public class OrderController : ControllerBase { }
```

#### ❌ 避免的做法

```csharp
// 不必要的属性重复
[Module("OrderManagement", "订单管理", Icon = "fa-solid fa-shopping-cart")]
[Navigation(
    Title = "订单管理",                    // ❌ 与 ModuleAttribute 重复
    Icon = "fa-solid fa-shopping-cart"     // ❌ 与 ModuleAttribute 重复
)]
public class OrderController : ControllerBase { }

// 更好的方式：只设置不同的属性
[Module("OrderManagement", "订单管理", Icon = "fa-solid fa-shopping-cart")]
[Navigation(Order = 1, Permission = "order_access")]  // ✅ 只设置差异属性
public class OrderController : ControllerBase { }
```

### 2. 使用特性定义导航

#### 模块级导航

```csharp
[Module("UserManagement", "用户管理", Icon = "fa-solid fa-users")]
public class UserController : ControllerBase
{
    // 控制器实现
}
```

#### 控制器级导航（基础用法）

```csharp
[Navigation(
    Title = "用户列表",
    Icon = "fa-solid fa-user-list", 
    Order = 1,
    Permission = "userManagement_users")]
public class UsersController : ControllerBase
{
    // 控制器实现
}
```

#### 控制器级导航（扩展用法）

```csharp
[Navigation(
    Title = "系统管理",
    Icon = "fa-solid fa-cog",
    Order = 1,
    PlatformType = PlatformType.System,     // 仅系统平台显示
    Group = "Management",                   // 分组
    Tags = new[] { "admin", "system" },     // 标签
    RequireAuth = true,                     // 需要认证
    MinVersion = "2.0.0",                   // 最小版本要求
    SupportedDevices = new[] { "desktop" }, // 支持的设备
    Priority = 10,                          // 优先级
    Badge = "NEW",                          // 徽章
    BadgeType = "success"                   // 徽章样式
)]
public class SystemController : ControllerBase
{
    // 控制器实现
}
```

#### 动作级导航

```csharp
public class UsersController : ControllerBase
{
    [Navigation(
        Title = "创建用户",
        Icon = "fa-solid fa-user-plus",
        Order = 1,
        Permission = "userManagement_users_create")]
    public IActionResult Create()
    {
        return View();
    }
}
```

### 3. 配置文件定义导航

在 `appsettings.json` 中定义导航：

```json
{
  "Navigation": {
    "Dashboard": {
      "Name": "Dashboard",
      "Title": "控制台",
      "Path": "/dashboard",
      "Icon": "fa-solid fa-gauge-high",
      "Order": 0,
      "Permission": "dashboard",
      "Children": [
        {
          "Name": "Statistics",
          "Title": "统计概览",
          "Path": "/dashboard/statistics",
          "Icon": "fa-solid fa-chart-line",
          "Order": 1,
          "Permission": "dashboard_statistics"
        }
      ]
    },
    "TestModule": {
      "Name": "test",
      "Title": "测试模块",
      "Path": "/test",
      "Icon": "fa-solid fa-test",
      "Order": 1,
      "PlatformType": "Both",
      "Group": "System",
      "Tags": ["test", "demo"],
      "MetaData": {
        "category": "system",
        "priority": "high"
      },
      "RequireAuth": true,
      "IsExperimental": false,
      "MinVersion": "1.0.0",
      "SupportedDevices": ["desktop", "tablet", "mobile"],
      "Priority": 5,
      "Badge": "DEMO",
      "BadgeType": "info"
    }
  }
}
```

### 4. 获取导航数据

```csharp
[ApiController]
public class NavigationController : ControllerBase
{
    private readonly INavigationService _navigationService;
    private readonly IHasPermissionService _permissionService;

    public NavigationController(
        INavigationService navigationService,
        IHasPermissionService permissionService)
    {
        _navigationService = navigationService;
        _permissionService = permissionService;
    }

    // 基础用法
    [HttpGet("tree")]
    public async Task<ActionResult<List<NavigationNode>>> GetNavigationTree()
    {
        var tree = await _navigationService.GetNavigationTreeAsync();
        var filteredTree = _navigationService.FilterNodesByPermission(tree, _permissionService);
        return Ok(filteredTree);
    }

    // 2.0.0 新功能：按平台获取
    [HttpGet("system")]
    public async Task<IActionResult> GetSystemNavigation()
    {
        var navigation = await _navigationService.GetNavigationTreeAsync(PlatformType.System);
        return Ok(navigation);
    }

    // 2.0.0 新功能：上下文过滤
    [HttpGet("filtered")]
    public async Task<IActionResult> GetFilteredNavigation(
        string platform = "both",
        string device = "desktop",
        string version = null,
        bool isDev = false)
    {
        var nodes = await _navigationService.GetNavigationTreeAsync();
        
        var context = new NavigationFilterContext
        {
            PlatformType = Enum.Parse<PlatformType>(platform, true),
            DeviceType = device,
            CurrentVersion = version,
            IsDevelopment = isDev,
            IsAuthenticated = User.Identity.IsAuthenticated,
            PermissionService = _permissionService,
            UserTags = GetUserTags(),
            GroupFilter = GetAllowedGroups()
        };

        var filteredNodes = _navigationService.FilterNodesByContext(nodes, context);
        return Ok(filteredNodes);
    }
}
```

## 🔥 实际应用案例

### 电商管理系统导航示例

以下是一个完整的电商管理系统导航配置示例，展示了智能属性优先级策略的实际应用：

#### 基础模块快速搭建

```csharp
// 1. 产品管理 - 只使用 ModuleAttribute，快速上线
[Module("ProductManagement", "产品管理", Icon = "fa-solid fa-box")]
public class ProductController : ControllerBase
{
    // 自动生成导航：
    // - 标题：产品管理
    // - 图标：fa-solid fa-box
    // - 权限：productManagement_product
    
    public IActionResult Index() => View();
    public IActionResult Create() => View();
}

// 2. 订单管理 - 添加基础配置
[Module("OrderManagement", "订单管理", Icon = "fa-solid fa-shopping-cart")]
[Navigation(Order = 2, Group = "Business")]
public class OrderController : ControllerBase
{
    // 使用 ModuleAttribute 的标题和图标
    // 添加排序和分组
    
    public IActionResult Index() => View();
}

// 3. 用户管理 - 完整配置
[Module("UserManagement", "用户管理", Icon = "fa-solid fa-users")]
[Navigation(
    Title = "用户中心",                     // 覆盖模块标题
    Icon = "fa-solid fa-user-gear",         // 覆盖模块图标
    Order = 1,
    Permission = "user_center_access",
    PlatformType = PlatformType.System,
    Group = "Management",
    Tags = new[] { "admin", "user", "system" },
    Priority = 10,
    Badge = "HOT",
    BadgeType = "danger"
)]
public class UserController : ControllerBase
{
    // 最终导航配置：
    // - 标题：用户中心 (NavigationAttribute覆盖)
    // - 图标：fa-solid fa-user-gear (NavigationAttribute覆盖)
    // - 其他属性来自 NavigationAttribute
    
    [Navigation(Title = "用户列表", Order = 1)]
    public IActionResult Index() => View();
    
    [Navigation(Title = "添加用户", Order = 2)]
    public IActionResult Create() => View();
}

// 4. 报表管理 - 临时隐藏
[Module("ReportManagement", "报表管理", Icon = "fa-solid fa-chart-bar")]
[Navigation(Hidden = true)]  // 功能开发中，临时隐藏
public class ReportController : ControllerBase
{
    // 虽然隐藏了 NavigationAttribute，但保留了模块信息
    // 需要时只需要设置 Hidden = false 即可恢复显示
    
    public IActionResult Index() => View();
}
```

#### 生成的导航树结构

```json
{
  "modules": [
    {
      "name": "productManagement",
      "title": "产品管理",
      "icon": "fa-solid fa-box",
      "order": 0,
      "permission": "productManagement",
      "children": [
        {
          "name": "product",
          "title": "产品管理",           // 来自 ModuleAttribute
          "icon": "fa-solid fa-box",     // 来自 ModuleAttribute
          "permission": "productManagement_product",
          "platformType": "Both",        // 默认值
          "requireAuth": true            // 默认值
        }
      ]
    },
    {
      "name": "orderManagement", 
      "title": "订单管理",
      "icon": "fa-solid fa-shopping-cart",
      "order": 0,
      "permission": "orderManagement",
      "children": [
        {
          "name": "order",
          "title": "订单管理",           // 来自 ModuleAttribute
          "icon": "fa-solid fa-shopping-cart", // 来自 ModuleAttribute
          "order": 2,                    // 来自 NavigationAttribute
          "group": "Business",           // 来自 NavigationAttribute
          "permission": "orderManagement_order"
        }
      ]
    },
    {
      "name": "userManagement",
      "title": "用户管理", 
      "icon": "fa-solid fa-users",
      "order": 0,
      "permission": "userManagement",
      "children": [
        {
          "name": "user",
          "title": "用户中心",           // NavigationAttribute 覆盖
          "icon": "fa-solid fa-user-gear", // NavigationAttribute 覆盖
          "order": 1,
          "permission": "user_center_access",
          "platformType": "System",
          "group": "Management",
          "tags": ["admin", "user", "system"],
          "priority": 10,
          "badge": "HOT",
          "badgeType": "danger"
        }
      ]
    }
    // ReportController 被隐藏，不出现在导航中
  ]
}
```

### 开发团队协作场景

#### 场景1：新功能快速迭代

```csharp
// 开发初期 - 功能原型
[Module("InventoryManagement", "库存管理", Icon = "fa-solid fa-warehouse")]
public class InventoryController : ControllerBase
{
    // 快速上线，使用默认配置
}

// 功能稳定后 - 添加精细配置
[Module("InventoryManagement", "库存管理", Icon = "fa-solid fa-warehouse")]
[Navigation(
    Title = "智能库存",               // 产品化的名称
    Order = 3,
    Group = "Operations",
    Badge = "PRO",
    BadgeType = "success"
)]
public class InventoryController : ControllerBase
{
    // 保持原有功能，只是导航展示更专业
}
```

#### 场景2：A/B测试和功能开关

```csharp
// 版本A：传统界面
[Module("Dashboard", "控制台", Icon = "fa-solid fa-gauge")]
[Navigation(
    Hidden = false,                    // 当前版本
    Badge = "V1",
    BadgeType = "info"
)]
public class DashboardController : ControllerBase { }

// 版本B：新界面
[Module("Dashboard", "控制台", Icon = "fa-solid fa-gauge")]
[Navigation(
    Title = "智能控制台",              // 新版本名称
    Hidden = true,                     // 灰度发布时隐藏
    IsExperimental = true,
    Badge = "V2",
    BadgeType = "warning",
    MinVersion = "2.1.0"
)]
public class DashboardV2Controller : ControllerBase { }
```

#### 场景3：权限分级管理

```csharp
// 基础管理员功能
[Module("SystemManagement", "系统管理", Icon = "fa-solid fa-cog")]
[Navigation(
    PlatformType = PlatformType.System,
    Group = "System",
    RequireAuth = true
)]
public class BasicSystemController : ControllerBase { }

// 高级管理员功能  
[Module("SystemManagement", "系统管理", Icon = "fa-solid fa-cog")]
[Navigation(
    Title = "高级系统管理",
    Icon = "fa-solid fa-gear",
    Permission = "advanced_system_admin",
    PlatformType = PlatformType.System,
    Group = "System",
    Priority = 100,                    // 高优先级
    Badge = "ADMIN",
    BadgeType = "danger"
)]
public class AdvancedSystemController : ControllerBase { }
```

## 扩展的 NavigationAttribute 属性详解

### 平台类型支持

```csharp
[Navigation(
    Title = "系统管理",
    PlatformType = PlatformType.System // 仅系统平台显示
)]
public class SystemController : ControllerBase { }

[Navigation(
    Title = "租户设置",
    PlatformType = PlatformType.Tenant // 仅租户平台显示
)]
public class TenantController : ControllerBase { }

[Navigation(
    Title = "通用功能",
    PlatformType = PlatformType.Both // 两个平台都显示（默认值）
)]
public class HomeController : ControllerBase { }
```

### 分组和标签

```csharp
[Navigation(
    Title = "用户管理",
    Group = "System", // 分组
    Tags = new[] { "admin", "management", "user" } // 标签
)]
public class UsersController : ControllerBase { }
```

### 元数据支持

```csharp
[Navigation(
    Title = "高级功能",
    MetaDataJson = "{\"category\": \"admin\", \"level\": \"advanced\", \"beta\": true}"
)]
public class AdvancedController : ControllerBase { }
```

### 认证和权限

```csharp
[Navigation(
    Title = "公开页面",
    RequireAuth = false // 不需要认证
)]
public class PublicController : ControllerBase { }

[Navigation(
    Title = "管理功能",
    RequireAuth = true, // 需要认证（默认值）
    Permission = "admin_access" // 自定义权限
)]
public class AdminController : ControllerBase { }
```

### 版本控制

```csharp
[Navigation(
    Title = "新功能",
    MinVersion = "2.0.0", // 最小版本要求
    MaxVersion = "3.0.0", // 最大支持版本
    IsExperimental = true // 实验性功能，仅开发环境显示
)]
public class NewFeatureController : ControllerBase { }
```

### 设备支持

```csharp
[Navigation(
    Title = "移动端功能",
    SupportedDevices = new[] { "mobile", "tablet" } // 仅移动设备显示
)]
public class MobileController : ControllerBase { }

[Navigation(
    Title = "桌面功能",
    SupportedDevices = new[] { "desktop" } // 仅桌面显示
)]
public class DesktopController : ControllerBase { }
```

### 优先级和排序

```csharp
[Navigation(
    Title = "重要功能",
    Order = 1, // 基础排序
    Priority = 10 // 高优先级（相同Order时优先显示）
)]
public class ImportantController : ControllerBase { }
```

### 快捷键和徽章

```csharp
[Navigation(
    Title = "快速操作",
    Shortcut = "Ctrl+Q", // 快捷键
    Badge = "NEW", // 徽章文本
    BadgeType = "success" // 徽章样式
)]
public class QuickController : ControllerBase
{
    [Navigation(
        Title = "热门功能",
        Badge = "HOT",
        BadgeType = "danger"
    )]
    public IActionResult Popular() => Ok();

    [Navigation(
        Title = "测试功能",
        Badge = "BETA",
        BadgeType = "warning"
    )]
    public IActionResult Beta() => Ok();
}
```

## API 文档

### INavigationService 接口

```csharp
public interface INavigationService
{
    /// <summary>
    /// 获取导航树
    /// </summary>
    /// <param name="platformType">平台类型</param>
    /// <returns>导航节点列表</returns>
    Task<List<NavigationNode>> GetNavigationTreeAsync(PlatformType platformType = PlatformType.Both);

    /// <summary>
    /// 初始化导航树
    /// </summary>
    Task InitializeNavigationTree();

    /// <summary>
    /// 清除指定模块的导航缓存
    /// </summary>
    /// <param name="moduleName">模块名称</param>
    /// <param name="platformType">平台类型</param>
    Task ClearModuleNavigationCacheAsync(string moduleName, PlatformType? platformType = null);
    
    /// <summary>
    /// 清除所有导航缓存
    /// </summary>
    Task ClearAllNavigationCacheAsync();
    
    /// <summary>
    /// 获取当前导航版本号
    /// </summary>
    /// <returns>版本哈希值，如果不存在则返回null</returns>
    Task<string> GetNavigationVersionAsync();
    
    /// <summary>
    /// 根据权限过滤导航节点
    /// </summary>
    /// <param name="nodes">导航节点列表</param>
    /// <param name="hasPermissionService">权限服务</param>
    /// <returns>过滤后的导航节点列表</returns>
    List<NavigationNode> FilterNodesByPermission(List<NavigationNode> nodes, IHasPermissionService hasPermissionService);

    /// <summary>
    /// 根据平台类型过滤导航节点
    /// </summary>
    /// <param name="nodes">导航节点列表</param>
    /// <param name="platformType">平台类型</param>
    /// <returns>过滤后的导航节点列表</returns>
    List<NavigationNode> FilterNodesByPlatform(List<NavigationNode> nodes, PlatformType platformType);

    /// <summary>
    /// 根据上下文过滤导航节点
    /// </summary>
    /// <param name="nodes">导航节点列表</param>
    /// <param name="context">过滤上下文</param>
    /// <returns>过滤后的导航节点列表</returns>
    List<NavigationNode> FilterNodesByContext(List<NavigationNode> nodes, NavigationFilterContext context);
}
```

### NavigationNode 模型

```csharp
public class NavigationNode
{
    /// <summary>导航项标识</summary>
    public string Name { get; set; }

    /// <summary>显示标题</summary>
    public string Title { get; set; }

    /// <summary>路由路径</summary>
    public string Path { get; set; }

    /// <summary>外部地址</summary>
    public string Link { get; set; }

    /// <summary>图标</summary>
    public string Icon { get; set; }

    /// <summary>排序值</summary>
    public int Order { get; set; }

    /// <summary>父级路径</summary>
    public string ParentPath { get; set; }

    /// <summary>是否隐藏</summary>
    public bool Hidden { get; set; }

    /// <summary>所需权限</summary>
    public string Permission { get; set; }

    /// <summary>描述信息</summary>
    public string Description { get; set; }

    /// <summary>是否为外部链接</summary>
    public bool IsExternal { get; set; }

    /// <summary>打开方式（_blank/_self）</summary>
    public string Target { get; set; }

    /// <summary>子节点</summary>
    public List<NavigationNode> Children { get; set; } = [];

    /// <summary>路由模板</summary>
    public string Route { get; set; }

    /// <summary>所属模块</summary>
    public string ModuleName { get; set; }

    // 2.0.0 新增属性
    /// <summary>平台类型</summary>
    public PlatformType PlatformType { get; set; } = PlatformType.Both;

    /// <summary>分组</summary>
    public string Group { get; set; }

    /// <summary>标签</summary>
    public string[] Tags { get; set; } = [];

    /// <summary>元数据JSON</summary>
    public string MetaDataJson { get; set; }

    /// <summary>是否需要认证</summary>
    public bool RequireAuth { get; set; } = true;

    /// <summary>是否为实验性功能</summary>
    public bool IsExperimental { get; set; }

    /// <summary>最小版本要求</summary>
    public string MinVersion { get; set; }

    /// <summary>最大支持版本</summary>
    public string MaxVersion { get; set; }

    /// <summary>支持的设备类型</summary>
    public string[] SupportedDevices { get; set; } = [];

    /// <summary>优先级</summary>
    public int Priority { get; set; }

    /// <summary>快捷键</summary>
    public string Shortcut { get; set; }

    /// <summary>徽章文本</summary>
    public string Badge { get; set; }

    /// <summary>徽章类型</summary>
    public string BadgeType { get; set; }

    /// <summary>
    /// 深拷贝导航节点
    /// </summary>
    /// <returns>新的导航节点实例</returns>
    public NavigationNode Clone()
    {
        return new NavigationNode(Name, Title, Path)
        {
            Link = Link,
            Icon = Icon,
            Order = Order,
            ParentPath = ParentPath,
            Hidden = Hidden,
            Permission = Permission,
            Description = Description,
            IsExternal = IsExternal,
            Target = Target,
            Route = Route,
            ModuleName = ModuleName,
            PlatformType = PlatformType,
            Group = Group,
            Tags = Tags?.ToArray() ?? [],
            MetaDataJson = MetaDataJson,
            RequireAuth = RequireAuth,
            IsExperimental = IsExperimental,
            MinVersion = MinVersion,
            MaxVersion = MaxVersion,
            SupportedDevices = SupportedDevices?.ToArray() ?? [],
            Priority = Priority,
            Shortcut = Shortcut,
            Badge = Badge,
            BadgeType = BadgeType,
            Children = [] // 子节点列表重置为空
        };
    }
}
```

### NavigationFilterContext 上下文

```csharp
public class NavigationFilterContext
{
    /// <summary>平台类型</summary>
    public PlatformType PlatformType { get; set; } = PlatformType.Both;

    /// <summary>权限服务</summary>
    public IHasPermissionService PermissionService { get; set; }

    /// <summary>当前版本</summary>
    public string CurrentVersion { get; set; }

    /// <summary>设备类型</summary>
    public string DeviceType { get; set; }

    /// <summary>是否为开发环境</summary>
    public bool IsDevelopment { get; set; }

    /// <summary>是否已认证</summary>
    public bool IsAuthenticated { get; set; }

    /// <summary>用户标签</summary>
    public string[] UserTags { get; set; } = [];

    /// <summary>分组过滤器</summary>
    public string[] GroupFilter { get; set; } = [];
}
```

## 缓存管理

### 统一缓存策略

```csharp
public class NavigationManagementController : ControllerBase
{
    private readonly INavigationService _navigationService;

    [HttpPost("cache/clear/{moduleName}")]
    public async Task<IActionResult> ClearModuleCache(string moduleName)
    {
        // 注意：platformType 参数保留以保持API兼容性，但不再使用
        // 统一清除整个缓存，下次访问时自动重建
        await _navigationService.ClearModuleNavigationCacheAsync(moduleName);
        return Ok($"已清除模块 {moduleName} 的缓存");
    }

    [HttpPost("cache/clear-all")]
    public async Task<IActionResult> ClearAllCache()
    {
        await _navigationService.ClearAllNavigationCacheAsync();
        return Ok("已清除所有导航缓存");
    }

    [HttpPost("cache/rebuild")]
    public async Task<IActionResult> RebuildCache()
    {
        await _navigationService.InitializeNavigationTree();
        return Ok("已重新构建导航缓存");
    }
}
```

### 缓存键格式

导航组件使用以下缓存键格式：

- **统一缓存键**：`CodeSpirit:Navigation:All` - 存储完整的导航树
- **缓存策略**：单一缓存 + 内存过滤，简化缓存管理，降低内存占用
- **平台过滤**：在内存中根据平台类型过滤，性能优于多缓存键方案

**优势**：
- 内存占用降低约 66%（相比多平台独立缓存）
- 缓存更新更简单，只需一次写入
- 平台过滤在内存中进行，性能更优

## 高级特性

### 1. 权限集成

导航组件与 `CodeSpirit.Authorization` 组件深度集成，支持：

- **自动权限生成**：根据模块、控制器、动作自动生成权限码
- **权限过滤**：自动过滤用户无权访问的导航项
- **递归权限检查**：子节点有权限时，父节点自动显示

```csharp
// 自动生成的权限码格式：
// 模块权限：{moduleName}
// 控制器权限：{moduleName}_{controllerName}
// 动作权限：{moduleName}_{controllerName}_{actionName}
```

### 2. 缓存策略

- **分布式缓存**：支持 Redis 等分布式缓存
- **统一缓存**：单一缓存键存储完整导航树，简化管理
- **内存过滤**：平台类型过滤在内存中进行，性能更优
- **长期缓存**：缓存期限为 365 天，滑动过期时间 90 天
- **优雅降级**：缓存异常时不影响应用正常运行
- **内存优化**：相比多平台缓存方案，内存占用降低约 66%
- **合并策略**：支持多个服务独立初始化，自动合并导航模块

#### 2.1 版本控制机制

导航组件实现了基于内容哈希的版本控制系统，支持HTTP ETag标准缓存：

**核心特性**：
- **单对象封装**：版本号和导航数据封装在一起，仅占用1个Redis键，彻底避免缓存膨胀
- **自动版本计算**：基于导航树内容的SHA256哈希自动生成版本号
- **ETag支持**：客户端可通过HTTP ETag头实现智能缓存，减少不必要的网络传输
- **自动失效检测**：导航内容变化时自动更新版本号，无需手动清除缓存

**工作流程**：
1. 导航树构建时自动计算内容哈希作为版本号
2. 版本号和导航数据一起存储到缓存中
3. 客户端请求时返回ETag响应头
4. 客户端下次请求携带If-None-Match头
5. 服务器比较版本，未变化时返回304 Not Modified

**优势**：
- ✅ **零缓存膨胀**：每次更新完全覆盖旧数据，不产生多版本
- ✅ **原子性保证**：版本和数据同步写入，天然一致
- ✅ **自动失效**：导航内容变化时版本自动更新
- ✅ **向后兼容**：支持旧格式缓存自动迁移
- ✅ **性能优化**：客户端缓存命中时直接返回304，节省带宽

**API支持**：
- `GET /api/navigation/version` - 获取当前导航版本号
- 所有导航API自动包含ETag响应头

### 3. 导航合并策略

当同时存在代码定义和配置文件定义时：

1. 优先使用代码中的导航结构
2. 配置文件的导航信息会覆盖代码中的对应属性
3. 配置文件中的额外节点会被添加到导航树中

### 4. 外部链接支持

```csharp
[Navigation(
    Title = "外部文档",
    Link = "https://docs.example.com",
    IsExternal = true,
    Target = "_blank"  // 新窗口打开
)]
```

```csharp
[Navigation(
    Title = "内嵌系统",
    Link = "https://internal.example.com",
    IsExternal = true,
    Target = "_self"   // iframe 嵌入
)]
```

### 5. 版本比较

导航服务内置版本比较功能，支持标准语义化版本和字符串比较：

```csharp
// 这些操作在 FilterNodesByContext 中自动处理
context.CurrentVersion = "1.5.0";
node.MinVersion = "1.0.0"; // 满足条件
node.MaxVersion = "2.0.0"; // 满足条件
```

## 最佳实践

### 1. 导航结构设计

```csharp
// 推荐的导航层次结构
[Module("ProductManagement", "产品管理")]
public class ProductModule { }

[Navigation(Title = "产品管理", Order = 1)]
public class ProductsController : ControllerBase
{
    [Navigation(Title = "产品列表", Order = 1)]
    public IActionResult Index() => View();

    [Navigation(Title = "创建产品", Order = 2)]
    public IActionResult Create() => View();

    [Navigation(Title = "编辑产品", Hidden = true)]
    public IActionResult Edit(int id) => View();
}
```

### 2. 权限命名规范

```csharp
// 使用有意义的权限名称
[Navigation(Permission = "product_management")]  // 模块级权限
[Navigation(Permission = "product_list")]        // 功能级权限
[Navigation(Permission = "product_create")]      // 操作级权限
```

### 3. 图标使用规范

```csharp
// 使用 FontAwesome 图标
[Navigation(Icon = "fa-solid fa-box")]           // 产品
[Navigation(Icon = "fa-solid fa-users")]         // 用户
[Navigation(Icon = "fa-solid fa-chart-line")]    // 报表
[Navigation(Icon = "fa-solid fa-cog")]           // 设置
```

### 4. 排序最佳实践

```csharp
// 使用 10 的倍数便于插入新项目
[Navigation(Order = 10)]  // 核心功能
[Navigation(Order = 20)]  // 管理功能  
[Navigation(Order = 30)]  // 报表功能
[Navigation(Order = 90)]  // 设置功能
```

### 5. 平台和设备策略

```csharp
// 合理使用平台过滤
[Navigation(
    Title = "系统监控",
    PlatformType = PlatformType.System,        // 仅系统管理员
    SupportedDevices = new[] { "desktop" }     // 仅桌面端
)]

[Navigation(
    Title = "移动应用",
    PlatformType = PlatformType.Both,
    SupportedDevices = new[] { "mobile", "tablet" }
)]
```

## 性能优化建议

1. **合理使用平台过滤**：根据实际平台类型获取导航，避免不必要的数据传输
2. **充分利用缓存**：导航数据统一缓存，平台过滤在内存中进行，提高响应速度
3. **批量清除缓存**：在更新导航配置后，及时清除相关缓存
4. **标签和分组优化**：使用有意义的标签和分组，便于过滤和管理
5. **避免深度拷贝滥用**：仅在必要时使用 `Clone()` 方法
6. **多服务初始化**：在分布式环境中，确保所有服务都正确初始化，导航模块会自动合并

## 故障排除

### 常见问题

1. **导航不显示**
   - 检查模块是否正确注册 `[Module]` 特性
   - 确认导航服务已正确注册
   - 验证权限配置是否正确

2. **平台类型不生效**
   
   检查 `NavigationAttribute` 的 `PlatformType` 设置：
   ```csharp
   // 正确
   PlatformType = PlatformType.System
   
   // 错误
   PlatformType = "System" // 这是字符串，不是枚举
   ```

3. **缓存更新延迟**
   
   导航组件已实现自动版本控制，导航内容变化时会自动更新版本。如果遇到缓存更新延迟：
   ```csharp
   // 手动清除模块缓存（注意：platformType 参数保留但不再使用）
   await navigationService.ClearModuleNavigationCacheAsync("moduleName");
   
   // 重新初始化导航树（会自动计算新版本）
   await navigationService.InitializeNavigationTree();
   ```
   
   **提示**：在2.2.0版本中，导航内容变化时会自动检测版本变化并更新缓存，通常无需手动清除。

4. **多服务初始化问题**
   
   在分布式环境中，如果多个服务初始化后只看到一个模块：
   - 检查日志中是否有 "Merging X new modules" 消息
   - 确认所有服务都正确调用了 `UseCodeSpiritNavigationAsync()`
   - 清除缓存后重新启动所有服务：
     ```csharp
     // 清除所有缓存
     await navigationService.ClearAllNavigationCacheAsync();
     // 然后重启所有服务，让它们按顺序初始化
     ```

5. **权限过滤异常**
   - 确认权限服务已正确实现
   - 检查权限码格式是否正确
   - 验证用户权限数据

6. **编译错误**
   
   确保项目引用了 `CodeSpirit.Core` 项目：
   ```xml
   <ProjectReference Include="..\..\CodeSpirit.Core\CodeSpirit.Core.csproj" />
   ```

### 日志监控

```csharp
// 启用详细日志
"Logging": {
  "LogLevel": {
    "CodeSpirit.Navigation": "Debug"
  }
}
```

## 单元测试

框架提供了完整的单元测试覆盖，包括：

### 服务测试
- **NavigationTreeBuilderTests** - 导航树构建器测试
- **NavigationCacheManagerTests** - 缓存管理器测试
- **NavigationFilterServiceTests** - 过滤服务测试
- **NavigationServiceTests** - 主服务集成测试

### 过滤器测试（8个过滤器）
- **PlatformFilterTests** - 平台类型过滤测试
- **PermissionFilterTests** - 权限过滤测试
- **AuthenticationFilterTests** - 认证过滤测试
- **VersionFilterTests** - 版本过滤测试
- **DeviceFilterTests** - 设备类型过滤测试
- **ExperimentalFilterTests** - 实验性功能过滤测试
- **GroupFilterTests** - 分组过滤测试
- **TagFilterTests** - 标签过滤测试

**测试统计**：108个测试用例，全部通过 ✅

### 版本控制测试
- **NavigationVersionControlTests** - 版本控制专项测试
  - 版本哈希确定性测试
  - 版本哈希唯一性测试
  - 复杂导航树版本计算测试
  - 序列化/反序列化测试

### 测试示例

```csharp
[Fact]
public void FilterNodesByPlatform_SystemPlatform_ShouldReturnSystemNodes()
{
    // Arrange
    var nodes = new List<NavigationNode>
    {
        new NavigationNode("system1", "系统功能", "/system1")
        {
            PlatformType = PlatformType.System
        },
        new NavigationNode("tenant1", "租户功能", "/tenant1")
        {
            PlatformType = PlatformType.Tenant
        }
    };

    // Act
    var result = _navigationService.FilterNodesByPlatform(nodes, PlatformType.System);

    // Assert
    Assert.Single(result);
    Assert.Equal("system1", result[0].Name);
}
```

运行测试：
```bash
dotnet test Tests/Components/CodeSpirit.Navigation.Tests/
```

## 变更日志

### [2.2.0] - 2025-12-28

#### 版本控制功能 🎉

**智能版本管理**
- 实现基于内容哈希的版本控制系统
- 使用SHA256算法计算导航树内容哈希作为版本标识
- 版本号和导航数据封装在单一缓存对象中，仅占用1个Redis键

**HTTP ETag支持**
- 所有导航API自动返回ETag响应头
- 客户端支持If-None-Match头进行版本对比
- 版本未变化时返回304 Not Modified，减少网络传输

**自动失效机制**
- 导航内容变化时自动检测并更新版本号
- 无需手动清除缓存，系统自动处理版本更新
- 支持版本变化日志记录，便于追踪导航变更

**向后兼容**
- 支持旧格式缓存自动迁移到新格式
- 保持现有API完全兼容，无需修改调用代码
- 平滑升级，不影响现有功能

**测试覆盖**
- 新增19个版本控制相关测试用例
- 覆盖版本计算、缓存管理、ETag支持等核心功能
- 测试总数从70个增加到108个

#### 技术改进 🔧

**缓存优化**
- 单对象封装策略，避免多版本缓存问题
- 原子性更新，版本和数据同步写入
- 零冗余设计，每次更新完全覆盖旧数据

**性能优化**
- ETag支持减少不必要的网络传输
- 版本查询API支持快速版本检查
- 懒加载计算，仅在写入缓存时计算哈希

### [2.1.0] - 2025-12-18

#### 架构重构 🎉

**服务职责分离**
- 拆分 `NavigationService` 为多个独立服务：
  - `NavigationTreeBuilder` - 导航树构建
  - `NavigationCacheManager` - 缓存管理
  - `NavigationFilterService` - 过滤服务
- 移除 Partial Class 设计，采用清晰的接口分离

**过滤器体系**
- 实现责任链模式的过滤器体系
- 8个内置过滤器：Platform, Permission, Authentication, Version, Device, Experimental, Group, Tag
- 支持动态注册自定义过滤器
- 过滤器按优先级自动排序执行

**缓存策略优化**
- 从多平台独立缓存改为单一缓存 + 内存过滤
- 缓存键：`CodeSpirit:Navigation:All`
- 内存占用降低约 66%
- 缓存更新更简单，只需一次写入

**多服务初始化支持**
- 支持多个 API 服务独立初始化
- 采用合并策略：新服务初始化时自动合并到现有缓存
- 按模块名去重，避免重复模块

**代码质量提升**
- 代码行数减少约 25%
- 职责清晰，易于测试和维护
- 完整的单元测试覆盖（70个测试用例）

#### 破坏性变更 ⚠️

**内部实现变更**
- `NavigationService.Tree.cs` 和 `NavigationService.Cache.cs` 已删除
- 缓存键格式变更：从 `CodeSpirit:Navigation:Module:{ModuleName}:{PlatformType}` 改为 `CodeSpirit:Navigation:All`
- `ClearModuleNavigationCacheAsync` 的 `platformType` 参数保留但不再使用

**迁移指南**
- 清除旧缓存：`await navigationService.ClearAllNavigationCacheAsync()`
- API 保持向后兼容，无需修改调用代码

### [2.0.0] - 2025-05-29

#### 新增功能 🎉

**智能属性优先级策略** ⭐
- 实现 `NavigationAttribute` 与 `ModuleAttribute` 的智能优先级处理
- 支持渐进式导航开发：从快速原型到精细配置
- 新增 `CreateNavigationNodeFromModuleAttribute` 方法处理模块属性回退
- 隐藏的 `NavigationAttribute` 自动回退到 `ModuleAttribute`
- 完善的属性映射和默认值处理机制

**平台类型支持**
- 新增 `PlatformType` 枚举（None, System, Tenant, Both）
- 支持按平台类型独立缓存和过滤
- 新增 `FilterNodesByPlatform` 方法

**扩展的 NavigationAttribute 属性**
- **Group**：导航项分组/分类
- **Tags**：标签数组支持
- **MetaDataJson**：JSON格式元数据
- **RequireAuth**：是否需要认证（默认true）
- **IsExperimental**：实验性功能标记
- **MinVersion/MaxVersion**：版本约束支持
- **SupportedDevices**：设备类型支持
- **Priority**：优先级（相同Order时的排序）
- **Shortcut**：快捷键支持
- **Badge/BadgeType**：徽章显示

**高级过滤功能**
- 新增 `NavigationFilterContext` 上下文过滤类
- 新增 `FilterNodesByContext` 综合过滤方法
- 支持多维度过滤：平台、权限、版本、设备、分组、标签、认证、实验性功能

**缓存优化**
- 支持按平台类型过滤导航
- 优化缓存键格式：`CodeSpirit:Navigation:All`（2.1.0）
- 支持清除缓存和重新构建

**模型增强**
- `NavigationNode` 新增深拷贝 `Clone()` 方法
- `NavigationConfigItem` 同步支持所有新属性
- 元数据支持复杂对象类型

#### 改进 🚀

**服务接口扩展**
- `INavigationService` 新增平台参数支持
- 新增上下文过滤和平台过滤方法
- 改进缓存管理接口

**导航树构建**
- 智能属性优先级策略：`NavigationAttribute` 优先于 `ModuleAttribute`
- 支持从 `ModuleAttribute` 创建默认导航节点
- 完善的属性回退和合并机制
- 支持元数据JSON解析
- 完善节点合并逻辑
- 支持所有新属性的处理

**错误处理**
- 改进缓存异常处理
- 增强日志记录
- 优雅处理单个模块异常

#### 技术改进 🔧

**版本比较**
- 内置版本比较逻辑
- 支持语义化版本和字符串比较
- 自动处理版本约束

**性能优化**
- 按平台独立缓存提高性能
- 减少不必要的数据加载
- 优化过滤算法

#### 测试覆盖 ✅

**单元测试**
- 平台类型过滤测试 (`NavigationServicePlatformTests`)
- 上下文过滤测试 (`NavigationServiceContextTests`)
- 缓存管理测试 (`NavigationServiceCacheTests`)
- 扩展属性测试 (`NavigationServiceExtendedPropertiesTests`)

**测试覆盖项目**
- 认证和权限过滤
- 版本范围验证
- 设备类型过滤
- 分组和标签过滤
- 实验性功能过滤
- 缓存异常处理
- 深拷贝功能
- 排序和优先级

#### 破坏性变更 ⚠️

**API 变更**
- `GetNavigationTreeAsync` 新增 `platformType` 参数（可选，默认 `Both`）
- `ClearModuleNavigationCacheAsync` 新增 `platformType` 参数（可选）

**缓存键变更**
- 缓存键格式从 `模块名:平台类型` 改为 `CodeSpirit:Navigation:All`（2.1.0）
- 需要清除旧缓存数据

#### 迁移指南 📋

**从 1.x 升级到 2.0**

1. **更新项目引用**
   ```xml
   <ProjectReference Include="..\..\CodeSpirit.Core\CodeSpirit.Core.csproj" />
   ```

2. **清除旧缓存**
   ```csharp
   await navigationService.ClearAllNavigationCacheAsync();
   await navigationService.InitializeNavigationTree();
   ```

3. **更新 NavigationAttribute 使用**
   ```csharp
   // 旧版本
   [Navigation(Title = "管理", Icon = "fa-cog")]
   
   // 新版本（可选扩展）
   [Navigation(
       Title = "管理",
       Icon = "fa-cog",
       PlatformType = PlatformType.System,
       Group = "Admin",
       Tags = new[] { "management" }
   )]
   ```

4. **更新服务调用**
   ```csharp
   // 旧版本
   var navigation = await navigationService.GetNavigationTreeAsync();
   
   // 新版本（向后兼容）
   var navigation = await navigationService.GetNavigationTreeAsync(PlatformType.Both);
   ```

---

### [1.0.0] - 2025-01-01

#### 初始版本
- 基础导航树构建
- 权限过滤支持
- 分布式缓存
- 配置文件支持
- 基础文档和测试

## 依赖项

- **.NET 9.0**：目标框架
- **Microsoft.AspNetCore.App**：ASP.NET Core 框架
- **Newtonsoft.Json**：JSON 序列化
- **CodeSpirit.Core**：核心组件

## 贡献指南

1. Fork 项目仓库
2. 创建功能分支
3. 提交变更
4. 创建 Pull Request

## 许可证

本项目采用 MIT 许可证，详见 LICENSE 文件。 