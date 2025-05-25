# CodeSpirit.Authorization 权限组件详解

## 概述

CodeSpirit.Authorization是框架的核心权限管理组件，实现了基于角色的访问控制（RBAC）和基于属性的访问控制（ABAC）的混合权限模型。该组件提供了灵活、细粒度的权限控制机制，支持动态权限验证、权限树管理和多层级权限继承。

## 权限模型架构

```mermaid
graph TB
    subgraph "权限主体 (Subjects)"
        User[用户]
        Role[角色]
        Group[用户组]
    end
    
    subgraph "权限对象 (Objects)"
        Resource[资源]
        Action[操作]
        Controller[控制器]
        Method[方法]
    end
    
    subgraph "权限规则 (Rules)"
        Permission[权限]
        Policy[策略]
        Attribute[属性]
    end
    
    subgraph "权限验证 (Verification)"
        Handler[授权处理器]
        Requirement[权限要求]
        Context[上下文]
    end
    
    User --> Role
    Role --> Permission
    Permission --> Resource
    Permission --> Action
    Handler --> Requirement
    Handler --> Context
    Context --> User
    Context --> Resource
    
    classDef subject fill:#e1f5fe
    classDef object fill:#f3e5f5
    classDef rule fill:#e8f5e8
    classDef verification fill:#fff3e0
    
    class User,Role,Group subject
    class Resource,Action,Controller,Method object
    class Permission,Policy,Attribute rule
    class Handler,Requirement,Context verification
```

## 核心组件设计

### 1. 权限节点 (PermissionNode)

权限节点是权限系统的基础数据结构，用于描述权限树中的一个节点（既可以表示控制器，也可以表示动作）。

```csharp
/// <summary>
/// 权限节点类，用于描述权限树中的一个节点（既可以表示控制器，也可以表示动作）。
/// 新增 RequestMethod 属性，用于记录动作所支持的 HTTP 请求方法。
/// </summary>
public class PermissionNode
{
    /// <summary>
    /// 节点名称（控制器名称或动作名称，唯一）
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 显示名称
    /// </summary>
    public string DisplayName { get; set; }

    /// <summary>
    /// 节点描述（可以通过 DisplayNameAttribute 或 PermissionAttribute 指定）
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// 父节点名称，如果是动作则指向所属控制器；如果为空，则表示根节点
    /// </summary>
    public string Parent { get; set; }

    /// <summary>
    /// 请求路径（仅对动作节点有效，通过 RouteAttribute 获取）
    /// </summary>
    public string Path { get; set; }

    /// <summary>
    /// 请求方法（例如 GET、POST、PUT、DELETE 等，仅对动作节点有效）
    /// </summary>
    public string RequestMethod { get; set; }

    /// <summary>
    /// 子节点集合
    /// </summary>
    public List<PermissionNode> Children { get; set; } = [];

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="name">节点名称</param>
    /// <param name="description">节点描述</param>
    /// <param name="parent">父节点名称</param>
    /// <param name="path">请求路径</param>
    /// <param name="requestMethod">请求方法</param>
    /// <param name="displayName">显示名称</param>
    public PermissionNode(string name, string description, string parent = "", string path = "", string requestMethod = "", string displayName = null)
    {
        Name = name;
        Description = description;
        Parent = parent;
        Path = path;
        RequestMethod = requestMethod;
        DisplayName = displayName;
    }
}
```

### 2. 权限服务接口 (IPermissionService)

权限服务是权限管理的核心接口，提供权限查询、验证和管理功能。

```csharp
/// <summary>
/// 权限服务接口：用于管理和查询应用的权限
/// </summary>
public interface IPermissionService
{
    /// <summary>
    /// 获取权限树，即所有控制器及其下属动作组成的节点集合
    /// </summary>
    /// <returns>权限树根节点列表</returns>
    List<PermissionNode> GetPermissionTree();

    /// <summary>
    /// 检查权限
    /// </summary>
    /// <param name="permissionName">权限名称</param>
    /// <param name="userPermissions">用户权限集合</param>
    /// <returns>是否有权限</returns>
    bool HasPermission(string permissionName, ISet<string> userPermissions);

    /// <summary>
    /// 初始化权限树
    /// </summary>
    Task InitializePermissionTree();
}
```

### 3. 权限要求 (PermissionRequirement)

权限要求定义了访问资源所需的权限条件。

```csharp
/// <summary>
/// 权限要求
/// </summary>
public class PermissionRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// 权限名称
    /// </summary>
    public string Permission { get; }

    /// <summary>
    /// 是否允许匿名访问
    /// </summary>
    public bool AllowAnonymous { get; set; }

    /// <summary>
    /// 权限策略
    /// </summary>
    public string Policy { get; set; }

    /// <summary>
    /// 属性要求
    /// </summary>
    public Dictionary<string, object> AttributeRequirements { get; set; } = new Dictionary<string, object>();

    public PermissionRequirement(string permission)
    {
        Permission = permission ?? throw new ArgumentNullException(nameof(permission));
    }

    public PermissionRequirement(string permission, string policy) : this(permission)
    {
        Policy = policy;
    }
}
```

### 4. 权限授权处理器 (RolePermissionAuthorizationHandler)

授权处理器负责执行具体的权限验证逻辑。

```csharp
/// <summary>
/// 角色权限授权处理器
/// </summary>
public class RolePermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IPermissionService _permissionService;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<RolePermissionAuthorizationHandler> _logger;
    private readonly IMemoryCache _cache;

    public RolePermissionAuthorizationHandler(
        IPermissionService permissionService,
        ICurrentUser currentUser,
        ILogger<RolePermissionAuthorizationHandler> logger,
        IMemoryCache cache)
    {
        _permissionService = permissionService;
        _currentUser = currentUser;
        _logger = logger;
        _cache = cache;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        try
        {
            // 检查是否允许匿名访问
            if (requirement.AllowAnonymous)
            {
                context.Succeed(requirement);
                return;
            }

            // 检查用户是否已认证
            if (!_currentUser.IsAuthenticated)
            {
                _logger.LogWarning("用户未认证，权限验证失败。权限: {Permission}", requirement.Permission);
                context.Fail();
                return;
            }

            // 超级管理员直接通过
            if (_currentUser.IsInRole("SuperAdmin"))
            {
                context.Succeed(requirement);
                return;
            }

            // 获取用户权限（使用缓存）
            var cacheKey = $"user_permissions_{_currentUser.Id}";
            var userPermissions = await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
                return await _permissionService.GetUserPermissionsAsync(_currentUser.Id.Value);
            });

            // 创建授权上下文
            var authContext = new AuthorizationContext
            {
                User = _currentUser,
                HttpContext = context.Resource as HttpContext,
                Requirements = requirement.AttributeRequirements
            };

            // 验证权限
            var hasPermission = _permissionService.HasPermission(
                requirement.Permission, 
                userPermissions, 
                authContext);

            if (hasPermission)
            {
                _logger.LogDebug("用户 {UserId} 权限验证成功。权限: {Permission}", 
                    _currentUser.Id, requirement.Permission);
                context.Succeed(requirement);
            }
            else
            {
                _logger.LogWarning("用户 {UserId} 权限验证失败。权限: {Permission}", 
                    _currentUser.Id, requirement.Permission);
                context.Fail();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "权限验证过程中发生异常。权限: {Permission}", requirement.Permission);
            context.Fail();
        }
    }
}

/// <summary>
/// 授权上下文
/// </summary>
public class AuthorizationContext
{
    /// <summary>
    /// 当前用户
    /// </summary>
    public ICurrentUser User { get; set; }

    /// <summary>
    /// HTTP上下文
    /// </summary>
    public HttpContext HttpContext { get; set; }

    /// <summary>
    /// 属性要求
    /// </summary>
    public Dictionary<string, object> Requirements { get; set; } = new Dictionary<string, object>();

    /// <summary>
    /// 资源信息
    /// </summary>
    public object Resource { get; set; }

    /// <summary>
    /// 操作信息
    /// </summary>
    public string Action { get; set; }
}
```

## 权限特性系统

### 1. 权限要求特性 (RequirePermissionAttribute)

用于标记控制器或方法需要的权限。

```csharp
/// <summary>
/// 权限要求特性
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequirePermissionAttribute : Attribute, IAuthorizationRequirement
{
    /// <summary>
    /// 权限名称
    /// </summary>
    public string Permission { get; }

    /// <summary>
    /// 权限策略
    /// </summary>
    public string Policy { get; set; }

    /// <summary>
    /// 是否允许匿名访问
    /// </summary>
    public bool AllowAnonymous { get; set; }

    /// <summary>
    /// 权限描述
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// 权限组
    /// </summary>
    public string Group { get; set; }

    public RequirePermissionAttribute(string permission)
    {
        Permission = permission ?? throw new ArgumentNullException(nameof(permission));
    }
}

/// <summary>
/// 操作权限特性
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class OperationAttribute : Attribute
{
    /// <summary>
    /// 操作名称
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 操作描述
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// 操作类型
    /// </summary>
    public OperationType Type { get; set; }

    /// <summary>
    /// 是否记录审计日志
    /// </summary>
    public bool AuditLog { get; set; } = true;

    public OperationAttribute(string name, OperationType type = OperationType.Query)
    {
        Name = name;
        Type = type;
    }
}

/// <summary>
/// 操作类型枚举
/// </summary>
public enum OperationType
{
    /// <summary>
    /// 查询操作
    /// </summary>
    Query = 1,

    /// <summary>
    /// 创建操作
    /// </summary>
    Create = 2,

    /// <summary>
    /// 更新操作
    /// </summary>
    Update = 3,

    /// <summary>
    /// 删除操作
    /// </summary>
    Delete = 4,

    /// <summary>
    /// 导入操作
    /// </summary>
    Import = 5,

    /// <summary>
    /// 导出操作
    /// </summary>
    Export = 6,

    /// <summary>
    /// 自定义操作
    /// </summary>
    Custom = 99
}
```

### 2. 页面权限特性 (PageAttribute)

用于标记页面级别的权限控制。

```csharp
/// <summary>
/// 页面权限特性
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class PageAttribute : Attribute
{
    /// <summary>
    /// 页面名称
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 页面标题
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// 页面描述
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// 页面图标
    /// </summary>
    public string Icon { get; set; }

    /// <summary>
    /// 页面路径
    /// </summary>
    public string Path { get; set; }

    /// <summary>
    /// 父级页面
    /// </summary>
    public string Parent { get; set; }

    /// <summary>
    /// 排序顺序
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// 是否在菜单中显示
    /// </summary>
    public bool ShowInMenu { get; set; } = true;

    /// <summary>
    /// 是否需要权限验证
    /// </summary>
    public bool RequireAuth { get; set; } = true;

    public PageAttribute(string name)
    {
        Name = name;
    }
}
```

## 权限服务实现

### 1. 权限服务实现类

```csharp
/// <summary>
/// 权限服务实现
/// </summary>
public class PermissionService : IPermissionService, IScopedDependency
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMemoryCache _cache;
    private readonly ILogger<PermissionService> _logger;
    private readonly IConfiguration _configuration;
    private static readonly object _lockObject = new object();
    private static List<PermissionNode> _permissionTree;

    public PermissionService(
        IServiceProvider serviceProvider,
        IMemoryCache cache,
        ILogger<PermissionService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _cache = cache;
        _logger = logger;
        _configuration = configuration;
    }

    public List<PermissionNode> GetPermissionTree()
    {
        if (_permissionTree == null)
        {
            lock (_lockObject)
            {
                if (_permissionTree == null)
                {
                    _permissionTree = BuildPermissionTree();
                }
            }
        }
        return _permissionTree;
    }

    public bool HasPermission(string permissionName, ISet<string> userPermissions)
    {
        if (string.IsNullOrEmpty(permissionName))
            return true;

        // 检查直接权限
        if (userPermissions.Contains(permissionName))
            return true;

        // 检查通配符权限
        if (CheckWildcardPermissions(permissionName, userPermissions))
            return true;

        // 检查继承权限
        if (CheckInheritedPermissions(permissionName, userPermissions))
            return true;

        // 检查属性权限（ABAC）
        if (CheckAttributeBasedPermissions(permissionName, userPermissions))
            return true;

        return false;
    }

    public async Task InitializePermissionTree()
    {
        lock (_lockObject)
        {
            _permissionTree = BuildPermissionTree();
        }
        
        _logger.LogInformation("权限树初始化完成，共 {Count} 个权限节点", 
            CountPermissionNodes(_permissionTree));
    }

    private List<PermissionNode> BuildPermissionTree()
    {
        var nodes = new List<PermissionNode>();
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.FullName.StartsWith("CodeSpirit"));

        foreach (var assembly in assemblies)
        {
            var controllers = assembly.GetTypes()
                .Where(t => t.IsSubclassOf(typeof(ControllerBase)) && !t.IsAbstract);

            foreach (var controller in controllers)
            {
                var controllerNode = CreateControllerNode(controller);
                if (controllerNode != null)
                {
                    nodes.Add(controllerNode);
                }
            }
        }

        return BuildHierarchy(nodes);
    }

    private PermissionNode CreateControllerNode(Type controllerType)
    {
        var controllerName = controllerType.Name.Replace("Controller", "");
        var pageAttr = controllerType.GetCustomAttribute<PageAttribute>();
        var requirePermissionAttr = controllerType.GetCustomAttribute<RequirePermissionAttribute>();

        var node = new PermissionNode
        {
            Name = controllerName,
            DisplayName = pageAttr?.Title ?? controllerName,
            Description = pageAttr?.Description ?? $"{controllerName}管理",
            Type = PermissionType.Controller,
            ControllerName = controllerName,
            Level = 1,
            Order = pageAttr?.Order ?? 0
        };

        // 添加操作权限
        var methods = controllerType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.IsPublic && !m.IsSpecialName && m.DeclaringType == controllerType);

        foreach (var method in methods)
        {
            var actionNode = CreateActionNode(method, controllerName);
            if (actionNode != null)
            {
                node.Children.Add(actionNode);
            }
        }

        return node;
    }

    private PermissionNode CreateActionNode(MethodInfo method, string controllerName)
    {
        var operationAttr = method.GetCustomAttribute<OperationAttribute>();
        var requirePermissionAttr = method.GetCustomAttribute<RequirePermissionAttribute>();
        
        if (operationAttr == null && requirePermissionAttr == null)
            return null;

        var actionName = method.Name;
        var permissionName = requirePermissionAttr?.Permission ?? $"{controllerName}.{actionName}";

        var node = new PermissionNode
        {
            Name = permissionName,
            DisplayName = operationAttr?.Name ?? actionName,
            Description = operationAttr?.Description ?? $"{actionName}操作",
            Type = PermissionType.Action,
            ControllerName = controllerName,
            ActionName = actionName,
            ParentName = controllerName,
            Level = 2,
            HttpMethod = GetHttpMethod(method)
        };

        return node;
    }

    private string GetHttpMethod(MethodInfo method)
    {
        var httpMethodAttrs = method.GetCustomAttributes()
            .Where(attr => attr.GetType().Name.StartsWith("Http") && attr.GetType().Name.EndsWith("Attribute"));

        foreach (var attr in httpMethodAttrs)
        {
            var typeName = attr.GetType().Name;
            return typeName.Replace("Http", "").Replace("Attribute", "").ToUpper();
        }

        return "GET"; // 默认GET方法
    }

    private List<PermissionNode> BuildHierarchy(List<PermissionNode> nodes)
    {
        var nodeDict = nodes.ToDictionary(n => n.Name, n => n);
        var rootNodes = new List<PermissionNode>();

        foreach (var node in nodes)
        {
            if (string.IsNullOrEmpty(node.ParentName))
            {
                rootNodes.Add(node);
            }
            else if (nodeDict.TryGetValue(node.ParentName, out var parent))
            {
                parent.Children.Add(node);
            }
        }

        return rootNodes.OrderBy(n => n.Order).ThenBy(n => n.Name).ToList();
    }

    private bool CheckWildcardPermissions(string permission, ISet<string> userPermissions)
    {
        // 检查通配符权限，如 "User.*" 可以匹配 "User.Create", "User.Update" 等
        var parts = permission.Split('.');
        for (int i = parts.Length - 1; i >= 0; i--)
        {
            var wildcardPermission = string.Join(".", parts.Take(i + 1)) + ".*";
            if (userPermissions.Contains(wildcardPermission))
                return true;
        }

        return false;
    }

    private bool CheckInheritedPermissions(string permission, ISet<string> userPermissions)
    {
        // 检查继承权限，如拥有父级权限自动拥有子级权限
        var parts = permission.Split('.');
        for (int i = parts.Length - 1; i > 0; i--)
        {
            var parentPermission = string.Join(".", parts.Take(i));
            if (userPermissions.Contains(parentPermission))
                return true;
        }

        return false;
    }

    private bool CheckAttributeBasedPermissions(string permission, ISet<string> userPermissions)
    {
        // ABAC权限检查逻辑
        // 这里可以根据用户属性、资源属性、环境属性等进行复杂的权限判断
        
        // 示例：检查数据权限
        if (userPermissions.Contains("DataScope"))
        {
            var dataScope = userPermissions.FirstOrDefault(p => p.StartsWith("DataScope"));
            if (dataScope != null)
            {
                var userDataScope = userPermissions.FirstOrDefault(p => p.StartsWith("UserDataScope"));
                if (userDataScope != null)
                {
                    return CheckDataScopePermission(dataScope, userDataScope);
                }
            }
        }

        return false;
    }

    private bool CheckDataScopePermission(string requiredScope, string userScope)
    {
        // 数据权限检查逻辑
        // 1: 全部数据权限
        // 2: 部门数据权限
        // 3: 个人数据权限
        
        if (userScope == "1") // 全部数据权限
            return true;
            
        if (userScope == "2") // 部门数据权限
        {
            // 检查是否为同一部门
            var userDeptId = userPermissions.FirstOrDefault(p => p.StartsWith("DeptId"));
            var resourceDeptId = requiredScope.Split(':')[1];
            return userDeptId == resourceDeptId;
        }
        
        if (userScope == "3") // 个人数据权限
        {
            // 检查是否为本人数据
            var userId = userPermissions.FirstOrDefault(p => p.StartsWith("UserId"));
            var resourceUserId = requiredScope.Split(':')[1];
            return userId == resourceUserId;
        }

        return false;
    }

    private int CountPermissionNodes(List<PermissionNode> nodes)
    {
        int count = nodes.Count;
        foreach (var node in nodes)
        {
            count += CountPermissionNodes(node.Children);
        }
        return count;
    }
}
```

## 权限中间件

### 1. 权限验证中间件

```csharp
/// <summary>
/// 权限验证中间件
/// </summary>
public class PermissionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PermissionMiddleware> _logger;

    public PermissionMiddleware(RequestDelegate next, ILogger<PermissionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IPermissionService permissionService, ICurrentUser currentUser)
    {
        // 获取当前请求的控制器和操作
        var endpoint = context.GetEndpoint();
        if (endpoint == null)
        {
            await _next(context);
            return;
        }

        // 检查是否需要权限验证
        var requirePermissionAttrs = endpoint.Metadata.GetOrderedMetadata<RequirePermissionAttribute>();
        if (!requirePermissionAttrs.Any())
        {
            await _next(context);
            return;
        }

        // 检查是否允许匿名访问
        var allowAnonymous = endpoint.Metadata.GetMetadata<AllowAnonymousAttribute>() != null ||
                           requirePermissionAttrs.Any(attr => attr.AllowAnonymous);
        
        if (allowAnonymous)
        {
            await _next(context);
            return;
        }

        // 验证用户权限
        foreach (var attr in requirePermissionAttrs)
        {
            if (!await permissionService.HasPermissionAsync(currentUser.Id.Value, attr.Permission))
            {
                _logger.LogWarning("用户 {UserId} 访问 {Path} 权限不足，需要权限: {Permission}", 
                    currentUser.Id, context.Request.Path, attr.Permission);
                
                context.Response.StatusCode = 403;
                await context.Response.WriteAsync("权限不足");
                return;
            }
        }

        await _next(context);
    }
}
```

## 权限配置和扩展

### 1. 权限配置扩展

```csharp
/// <summary>
/// 权限配置扩展
/// </summary>
public static class AuthorizationExtensions
{
    /// <summary>
    /// 添加权限服务
    /// </summary>
    public static IServiceCollection AddPermissionServices(this IServiceCollection services)
    {
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IAuthorizationHandler, RolePermissionAuthorizationHandler>();
        
        services.AddAuthorization(options =>
        {
            // 添加默认权限策略
            options.DefaultPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();

            // 添加自定义权限策略
            options.AddPolicy("RequirePermission", policy =>
                policy.Requirements.Add(new PermissionRequirement("default")));
        });

        return services;
    }

    /// <summary>
    /// 使用权限中间件
    /// </summary>
    public static IApplicationBuilder UsePermissionMiddleware(this IApplicationBuilder app)
    {
        return app.UseMiddleware<PermissionMiddleware>();
    }

    /// <summary>
    /// 初始化权限系统
    /// </summary>
    public static async Task InitializePermissionSystemAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();
        await permissionService.InitializePermissionTree();
    }
}
```

### 2. 权限策略配置

```csharp
/// <summary>
/// 权限策略配置
/// </summary>
public class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallbackPolicyProvider;

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        _fallbackPolicyProvider = new DefaultAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
    {
        return _fallbackPolicyProvider.GetDefaultPolicyAsync();
    }

    public Task<AuthorizationPolicy> GetFallbackPolicyAsync()
    {
        return _fallbackPolicyProvider.GetFallbackPolicyAsync();
    }

    public Task<AuthorizationPolicy> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith("Permission:"))
        {
            var permission = policyName.Substring("Permission:".Length);
            var policy = new AuthorizationPolicyBuilder()
                .AddRequirements(new PermissionRequirement(permission))
                .Build();
            return Task.FromResult(policy);
        }

        return _fallbackPolicyProvider.GetPolicyAsync(policyName);
    }
}
```

## 使用示例

### 1. 控制器权限配置

```csharp
/// <summary>
/// 用户管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Page("UserManagement", Title = "用户管理", Description = "系统用户管理", Icon = "user", Order = 1)]
[RequirePermission("User.View")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// 获取用户列表
    /// </summary>
    [HttpGet]
    [Operation("查询用户", OperationType.Query)]
    public async Task<IActionResult> GetUsers([FromQuery] UserQueryDto query)
    {
        var result = await _userService.GetUsersAsync(query);
        return Ok(result);
    }

    /// <summary>
    /// 创建用户
    /// </summary>
    [HttpPost]
    [RequirePermission("User.Create")]
    [Operation("创建用户", OperationType.Create)]
    public async Task<IActionResult> CreateUser(CreateUserDto dto)
    {
        var result = await _userService.CreateUserAsync(dto);
        return Ok(result);
    }

    /// <summary>
    /// 更新用户
    /// </summary>
    [HttpPut("{id}")]
    [RequirePermission("User.Update")]
    [Operation("更新用户", OperationType.Update)]
    public async Task<IActionResult> UpdateUser(long id, UpdateUserDto dto)
    {
        var result = await _userService.UpdateUserAsync(id, dto);
        return Ok(result);
    }

    /// <summary>
    /// 删除用户
    /// </summary>
    [HttpDelete("{id}")]
    [RequirePermission("User.Delete")]
    [Operation("删除用户", OperationType.Delete)]
    public async Task<IActionResult> DeleteUser(long id)
    {
        var result = await _userService.DeleteUserAsync(id);
        return Ok(result);
    }
}
```

### 2. 服务层权限检查

```csharp
/// <summary>
/// 用户服务实现
/// </summary>
public class UserService : IUserService
{
    private readonly IPermissionService _permissionService;
    private readonly ICurrentUser _currentUser;

    public UserService(IPermissionService permissionService, ICurrentUser currentUser)
    {
        _permissionService = permissionService;
        _currentUser = currentUser;
    }

    public async Task<ApiResponse<UserDto>> GetUserAsync(long id)
    {
        // 检查数据权限
        if (!await CheckDataPermissionAsync(id))
        {
            return ApiResponse<UserDto>.Error(403, "无权访问该用户数据");
        }

        // 业务逻辑...
    }

    private async Task<bool> CheckDataPermissionAsync(long userId)
    {
        var userDataScope = _currentUser.GetClaimValue("DataScope");
        
        switch (userDataScope)
        {
            case "1": // 全部数据权限
                return true;
            case "2": // 部门数据权限
                return await CheckDepartmentPermissionAsync(userId);
            case "3": // 个人数据权限
                return userId == _currentUser.Id;
            default:
                return false;
        }
    }

    private async Task<bool> CheckDepartmentPermissionAsync(long userId)
    {
        // 检查是否为同一部门
        // 实现部门权限检查逻辑
        return true;
    }
}
```

## 最佳实践

### 1. 权限设计原则

1. **最小权限原则**：用户只获得完成工作所需的最小权限
2. **权限分离**：将不同类型的权限分开管理
3. **权限继承**：合理设计权限层次结构
4. **权限审计**：记录所有权限变更操作

### 2. 性能优化

1. **权限缓存**：使用缓存减少数据库查询
2. **批量验证**：一次性验证多个权限
3. **懒加载**：按需加载权限数据
4. **权限预计算**：预先计算用户的有效权限

### 3. 安全考虑

1. **权限验证**：在多个层次进行权限验证
2. **权限日志**：记录权限验证失败的情况
3. **权限更新**：及时更新用户权限缓存
4. **权限审查**：定期审查用户权限分配

## 总结

CodeSpirit.Authorization权限组件提供了：

1. **灵活的权限模型**：支持RBAC和ABAC混合模型
2. **细粒度控制**：支持到方法级别的权限控制
3. **动态权限验证**：支持运行时权限验证
4. **权限树管理**：支持层次化权限组织
5. **高性能缓存**：优化权限验证性能
6. **扩展性设计**：支持自定义权限策略

该组件为CodeSpirit框架提供了强大而灵活的权限管理能力，确保了系统的安全性和可控性。 