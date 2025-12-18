# CodeSpirit.Navigation 导航组件

## 概述

`CodeSpirit.Navigation` 是 CodeSpirit 框架的核心导航组件，提供智能化的导航树构建、权限过滤和缓存管理功能。支持基于代码特性和配置文件的双重导航定义方式，实现了高度灵活和可扩展的导航管理解决方案。

**当前版本：2.1.0** - 支持多平台、元数据丰富、上下文感知的高级导航框架

> **重构完成**：架构已简化为职责分离的服务模式，采用过滤器责任链模式统一过滤逻辑，缓存策略优化为单一缓存 + 内存过滤。

## 主要特性

### 核心功能 ✅
- **智能导航构建**：自动扫描控制器和动作方法的特性，构建导航树
- **配置文件支持**：支持通过 `appsettings.json` 定义导航结构
- **权限集成**：集成权限系统，自动过滤用户无权访问的导航项
- **分布式缓存**：使用分布式缓存提升导航树查询性能
- **模块化设计**：支持多模块导航管理，每个模块独立缓存
- **层次结构**：支持多级导航菜单结构
- **外部链接**：支持外部链接和 iframe 嵌入
- **图标支持**：支持 FontAwesome 等图标库
- **优先级策略**：`NavigationAttribute` 优先于 `ModuleAttribute`，提供精细化控制

### 2.0.0 新特性 🎉
- **多平台支持**：`PlatformType` 枚举（None, System, Tenant, Both）
- **扩展属性**：15+ 个新的导航属性（分组、标签、元数据、版本约束等）
- **高级过滤**：基于上下文的多维度过滤（平台、权限、版本、设备等）
- **缓存优化**：按平台类型独立缓存，提升性能
- **深拷贝支持**：NavigationNode 深拷贝方法
- **版本约束**：内置版本比较逻辑，支持语义化版本
- **设备类型**：支持按设备类型过滤导航项
- **实验性功能**：开发环境实验性功能支持
- **徽章系统**：导航项徽章显示支持
- **智能属性合并**：`NavigationAttribute` 与 `ModuleAttribute` 的智能优先级处理

## 项目结构

```
CodeSpirit.Navigation/
├── Models/
│   ├── NavigationNode.cs           # 导航节点模型
│   └── NavigationConfiguration.cs  # 配置项模型
├── Services/
│   ├── INavigationService.cs        # 导航服务接口
│   ├── NavigationService.cs         # 导航服务主实现（重构后）
│   ├── INavigationTreeBuilder.cs    # 导航树构建器接口
│   ├── NavigationTreeBuilder.cs     # 导航树构建器实现
│   ├── INavigationCacheManager.cs   # 缓存管理器接口
│   ├── NavigationCacheManager.cs   # 缓存管理器实现
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
└── REFACTORING_*.md                 # 重构文档（参考）
```

## 快速开始

### 1. 服务注册

在 `Program.cs` 中注册导航服务：

```csharp
// 注册导航服务（重构后自动注册所有服务和过滤器）
builder.Services.AddCodeSpiritNavigation();

// 在应用启动时初始化导航树
var app = builder.Build();
await app.UseCodeSpiritNavigationAsync();
```

**重构后的服务注册**：
- `INavigationTreeBuilder` - 导航树构建器
- `INavigationCacheManager` - 缓存管理器
- `INavigationFilterService` - 过滤服务
- `INavigationService` - 主服务
- 8个内置过滤器（Platform, Permission, Authentication, Version, Device, Experimental, Group, Tag）

### 2. 使用特性定义导航

#### 属性优先级策略 🎯

导航组件实现了智能的属性优先级策略，确保灵活性和精确控制：

```csharp
// 场景1：只有 ModuleAttribute - 自动创建导航节点
[Module("UserManagement", "用户管理", Icon = "fa-solid fa-users")]
public class UserController : ControllerBase
{
    // 控制器会自动出现在导航中，使用 ModuleAttribute 的属性
    // 标题：用户管理
    // 图标：fa-solid fa-users
    // 其他属性使用默认值
}

// 场景2：ModuleAttribute + NavigationAttribute - NavigationAttribute 优先
[Module("UserManagement", "用户管理", Icon = "fa-solid fa-users")]
[Navigation(
    Title = "用户列表", // 覆盖 ModuleAttribute 的 DisplayName
    Icon = "fa-solid fa-user-list", // 覆盖 ModuleAttribute 的 Icon
    Order = 1,
    Permission = "user_management"
)]
public class UserController : ControllerBase
{
    // 最终使用 NavigationAttribute 的所有属性
    // 标题：用户列表（不是"用户管理"）
    // 图标：fa-solid fa-user-list（不是"fa-solid fa-users"）
}

// 场景3：隐藏的 NavigationAttribute - 回退到 ModuleAttribute
[Module("UserManagement", "用户管理", Icon = "fa-solid fa-users")]
[Navigation(Hidden = true)] // NavigationAttribute 被隐藏
public class UserController : ControllerBase
{
    // 由于 NavigationAttribute.Hidden = true，系统会检查 ModuleAttribute
    // 如果 ModuleAttribute 存在，使用其属性创建导航节点
    // 标题：用户管理
    // 图标：fa-solid fa-users
}

// 场景4：完全没有导航 - 不出现在导航中
public class InternalController : ControllerBase
{
    // 既没有 ModuleAttribute 也没有 NavigationAttribute
    // 该控制器不会出现在导航树中
}
```

**优先级规则总结：**
1. **NavigationAttribute** 存在且 `Hidden = false` → 使用 NavigationAttribute
2. **NavigationAttribute** 不存在或 `Hidden = true` → 检查 ModuleAttribute
3. **ModuleAttribute** 存在 → 使用 ModuleAttribute 属性创建默认导航
4. **都不存在** → 控制器不出现在导航中

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

### 统一缓存策略（重构后）

```csharp
public class NavigationManagementController : ControllerBase
{
    private readonly INavigationService _navigationService;

    [HttpPost("cache/clear/{moduleName}")]
    public async Task<IActionResult> ClearModuleCache(string moduleName)
    {
        // 注意：platformType 参数保留以保持API兼容性，但不再使用
        // 重构后统一清除整个缓存，下次访问时自动重建
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

导航组件使用以下缓存键格式（重构后）：

- **统一缓存键**：`CodeSpirit:Navigation:All` - 存储完整的导航树
- **缓存策略**：单一缓存 + 内存过滤，简化缓存管理，降低内存占用
- **平台过滤**：在内存中根据平台类型过滤，性能优于多缓存键方案

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

### 2. 缓存策略（重构后）

- **分布式缓存**：支持 Redis 等分布式缓存
- **统一缓存**：单一缓存键存储完整导航树，简化管理
- **内存过滤**：平台类型过滤在内存中进行，性能更优
- **长期缓存**：缓存期限为 365 天，滑动过期时间 90 天
- **优雅降级**：缓存异常时不影响应用正常运行
- **内存优化**：相比多平台缓存方案，内存占用降低约 66%

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

### 2. 属性优先级策略的最佳实践 🎯

```csharp
// ✅ 推荐：使用 ModuleAttribute 作为默认导航
[Module("UserManagement", "用户管理", Icon = "fa-solid fa-users")]
public class UserController : ControllerBase
{
    // 控制器会自动出现在导航中，使用模块的标题和图标
}

// ✅ 推荐：使用 NavigationAttribute 进行精细控制
[Module("UserManagement", "用户管理", Icon = "fa-solid fa-users")]
[Navigation(
    Title = "用户列表", // 更具体的标题
    Icon = "fa-solid fa-user-list", // 更具体的图标
    Order = 1,
    Permission = "user_list",
    PlatformType = PlatformType.Both
)]
public class UserController : ControllerBase { }

// ✅ 推荐：使用 Hidden 属性控制显示/隐藏
[Module("InternalModule", "内部模块", Icon = "fa-solid fa-gear")]
[Navigation(Hidden = true)] // 临时隐藏，但保留模块信息
public class InternalController : ControllerBase
{
    // 如果需要重新显示，只需要设置 Hidden = false
}

// ❌ 避免：不必要的属性重复
[Module("UserManagement", "用户管理", Icon = "fa-solid fa-users")]
[Navigation(
    Title = "用户管理", // 不必要的重复
    Icon = "fa-solid fa-users" // 不必要的重复
)]
public class UserController : ControllerBase { }

// ✅ 更好的方式：只定义不同的属性
[Module("UserManagement", "用户管理", Icon = "fa-solid fa-users")]
[Navigation(Order = 1, Permission = "user_management")] // 只设置必要的额外属性
public class UserController : ControllerBase { }
```

### 3. 渐进式导航开发

```csharp
// 第一阶段：快速开发，使用 ModuleAttribute
[Module("BlogManagement", "博客管理", Icon = "fa-solid fa-blog")]
public class BlogController : ControllerBase { }

// 第二阶段：精细化控制，添加 NavigationAttribute
[Module("BlogManagement", "博客管理", Icon = "fa-solid fa-blog")]
[Navigation(
    Order = 2,
    Permission = "blog_management",
    PlatformType = PlatformType.Both,
    Group = "Content"
)]
public class BlogController : ControllerBase { }

// 第三阶段：完整配置，添加高级属性
[Module("BlogManagement", "博客管理", Icon = "fa-solid fa-blog")]
[Navigation(
    Title = "博客文章",
    Icon = "fa-solid fa-pen-to-square",
    Order = 2,
    Permission = "blog_management",
    PlatformType = PlatformType.Both,
    Group = "Content",
    Tags = new[] { "content", "management" },
    Priority = 5,
    Badge = "BETA",
    BadgeType = "warning",
    MinVersion = "2.0.0"
)]
public class BlogController : ControllerBase { }
```

### 4. 权限命名规范

```csharp
// 使用有意义的权限名称
[Navigation(Permission = "product_management")]  // 模块级权限
[Navigation(Permission = "product_list")]        // 功能级权限
[Navigation(Permission = "product_create")]      // 操作级权限
```

### 5. 图标使用规范

```csharp
// 使用 FontAwesome 图标
[Navigation(Icon = "fa-solid fa-box")]           // 产品
[Navigation(Icon = "fa-solid fa-users")]         // 用户
[Navigation(Icon = "fa-solid fa-chart-line")]    // 报表
[Navigation(Icon = "fa-solid fa-cog")]           // 设置
```

### 6. 排序最佳实践

```csharp
// 使用 10 的倍数便于插入新项目
[Navigation(Order = 10)]  // 核心功能
[Navigation(Order = 20)]  // 管理功能  
[Navigation(Order = 30)]  // 报表功能
[Navigation(Order = 90)]  // 设置功能
```

### 7. 平台和设备策略

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
2. **充分利用缓存**：导航数据按平台独立缓存，提高响应速度
3. **批量清除缓存**：在更新导航配置后，及时清除相关缓存
4. **标签和分组优化**：使用有意义的标签和分组，便于过滤和管理
5. **避免深度拷贝滥用**：仅在必要时使用 `Clone()` 方法

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
   ```csharp
   // 手动清除模块缓存
   await navigationService.ClearModuleNavigationCacheAsync("moduleName");
   
   // 重新初始化导航树
   await navigationService.InitializeNavigationTree();
   ```

4. **权限过滤异常**
   - 确认权限服务已正确实现
   - 检查权限码格式是否正确
   - 验证用户权限数据

5. **编译错误**
   
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

框架提供了完整的单元测试覆盖（重构后），包括：

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

**测试统计**：70个测试用例，全部通过 ✅

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
- 按平台类型独立缓存（System, Tenant, Both）
- 优化缓存键格式：`模块名:平台类型`
- 支持指定平台清除缓存

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
- 缓存键格式从 `模块名` 改为 `模块名:平台类型`
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