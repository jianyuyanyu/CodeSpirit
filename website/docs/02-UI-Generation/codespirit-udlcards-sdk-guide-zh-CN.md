# CodeSpirit UDL Cards C# SDK使用说明

## 概述

CodeSpirit UDL Cards SDK 是一个专门为后端开发设计的卡片生成器，用于生成与前端 Amis Cards V2.0 兼容的配置。通过这个SDK，后端开发者可以使用流畅的C# API来创建各种类型的卡片和仪表板。

## 展示效果

https://localhost:7120/amis-cards/demo/monitor-dashboard-api.html

![屏幕截图_29-6-2025_23333_localhost](/img/屏幕截图_29-6-2025_23333_localhost.jpeg)

### 🎯 核心特性

- **🔒 类型安全**：完全基于强类型的 C# 模型，编译时检查
- **🎨 流畅API**：提供直观易用的建构器模式
- **🧩 模块化设计**：支持自定义卡片类型扩展
- **✅ 配置验证**：内置配置验证和异常处理机制
- **🛡️ 权限控制**：集成权限和角色验证
- **🎭 主题支持**：支持多种内置和自定义主题
- **📊 丰富组件**：支持统计、图表、表格、信息展示等多种卡片类型

### 📊 支持的卡片类型

| 卡片类型 | 描述 | 适用场景 |
|---------|------|---------|
| **StatCard** | 统计卡片 | 数值展示、趋势分析、进度指示 |
| **ChartCard** | 图表卡片 | 基于 ECharts 的各种图表类型 |
| **TableCard** | 表格卡片 | 功能完整的数据表格展示 |
| **InfoCard** | 信息卡片 | 静态信息和内容展示 |
| **InfoGridCard** | 信息网格 | 网格化信息布局展示 |

## 🚀 快速开始

### 1. 项目依赖

确保你的项目使用 .NET 10 并引用必要的包：

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="9.0.3" />
    <PackageReference Include="Microsoft.Extensions.Options" Version="9.0.3" />
    <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
    <PackageReference Include="FluentValidation" Version="11.11.0" />
  </ItemGroup>
</Project>
```

### 2. 服务注册

在 `Program.cs` 中注册 UdlCards 服务：

```csharp
using CodeSpirit.UdlCards.Extensions;

var builder = WebApplication.CreateBuilder(args);

// 方式1：使用配置文件
builder.Services.AddUdlCards(builder.Configuration);

// 方式2：使用委托配置
builder.Services.AddUdlCards(options =>
{
    options.DefaultTheme = "primary";
    options.EnablePermissionControl = true;
    options.StrictMode = false; // 重要：控制单个卡片失败时的行为
    options.ApiBaseUrl = "https://api.example.com";
    options.EnableCaching = true;
    options.CacheExpirationMinutes = 15;
});

var app = builder.Build();
```

### 3. 配置文件设置

在 `appsettings.json` 中添加配置：

```json
{
  "UdlCards": {
    "DefaultTheme": "primary",
    "EnableCaching": true,
    "CacheExpirationMinutes": 15,
    "MaxCardsPerPage": 10,
    "DefaultRefreshInterval": 30000,
    "StrictMode": false,
    "EnablePermissionControl": true,
    "DebugMode": false,
    "ApiBaseUrl": "https://api.example.com"
  }
}
```

### 4. 基本使用示例

```csharp
using CodeSpirit.UdlCards.Core;
using CodeSpirit.UdlCards.Models;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly UdlCardsGenerator _generator;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(UdlCardsGenerator generator, ILogger<DashboardController> logger)
    {
        _generator = generator;
        _logger = logger;
    }

    [HttpGet("stats")]
    public ActionResult<Dictionary<string, object>> GetStatsCard()
    {
        try
        {
            var statCard = new StatCardConfig
            {
                Title = "用户总数",
                Data = new StatDataConfig
                {
                    Value = 1248,
                    Label = "注册用户",
                    Unit = "人",
                    Formatter = "number",
                    ShowSeparator = true
                },
                Icon = new StatIconConfig
                {
                    Name = "fa-users",
                    Color = "#1890ff",
                    Position = "left"
                }
            };

            var amisConfig = _generator.GenerateCard(statCard);
            return Ok(amisConfig);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成统计卡片失败");
            return StatusCode(500, "卡片生成失败");
        }
    }
}
```

## 📋 详细配置指南

### StatCard - 统计卡片

统计卡片用于展示数值型数据，支持趋势、进度条和动画效果。

```csharp
var statCard = new StatCardConfig
{
    Title = "销售业绩",
    Description = "本月销售数据统计",
    Data = new StatDataConfig
    {
        Value = 89356.7m,
        Label = "本月销售额",
        Unit = "元",
        Prefix = "¥",
        Formatter = "currency",
        DecimalPlaces = 2,
        ShowSeparator = true,
        ApiUrl = "/api/stats/sales", // 动态数据源
        FieldMapping = new Dictionary<string, string>
        {
            ["value"] = "amount",
            ["label"] = "description"
        }
    },
    Icon = new StatIconConfig
    {
        Name = "fa-dollar-sign",
        Position = "left", // left, right, top, bottom
        Size = "lg", // xs, sm, md, lg, xl
        Color = "#52c41a",
        BackgroundColor = "#f6ffed",
        ShowBorder = false,
        BorderColor = "#d9f7be"
    },
    Trend = new StatTrendConfig
    {
        Direction = "up", // up, down, stable
        Value = 15.3m,
        IsPercentage = true,
        Text = "较上月增长",
        Colors = new StatTrendColorConfig
        {
            Up = "#52c41a",
            Down = "#ff4d4f",
            Stable = "#faad14"
        }
    },
    Progress = new StatProgressConfig
    {
        Target = 100000,
        Show = true,
        Height = 6,
        ShowText = true,
        Color = "#52c41a",
        BackgroundColor = "#f0f0f0"
    },
    Animation = new StatAnimationConfig
    {
        EnableValueAnimation = true,
        Duration = 2000,
        Easing = "ease-out",
        Delay = 0
    }
};
```

### ChartCard - 图表卡片

图表卡片基于 ECharts，支持多种图表类型和丰富的配置选项。

```csharp
var chartCard = new ChartCardConfig
{
    Title = "用户增长趋势",
    Description = "最近12个月用户注册趋势",
    Chart = new ChartConfig
    {
        Type = "line", // line, bar, pie, area, scatter, radar, gauge
        Height = 350,
        Width = "100%",
        Theme = "default",
        Responsive = true,
        Options = new Dictionary<string, object>
        {
            ["grid"] = new { left = "3%", right = "4%", bottom = "3%", containLabel = true },
            ["xAxis"] = new { type = "category", boundaryGap = false },
            ["yAxis"] = new { type = "value" },
            ["tooltip"] = new { trigger = "axis" }
        },
        Animation = new ChartAnimationConfig
        {
            Enabled = true,
            Duration = 1000,
            Easing = "cubicOut",
            Delay = 0
        },
        Toolbox = new ChartToolboxConfig
        {
            Show = true,
            Position = "top",
            Tools = new List<string> { "saveAsImage", "dataView", "refresh" }
        }
    },
    Data = new ChartDataConfig
    {
        ApiUrl = "/api/charts/user-growth",
        FieldMapping = new ChartFieldMapping
        {
            XField = "date",
            YField = "count",
            SeriesField = "type",
            ValueField = "value",
            LabelField = "label"
        },
        RefreshInterval = 30000,
        Filters = new List<ChartDataFilter>
        {
            new()
            {
                Field = "status",
                Operator = "eq", // eq, ne, gt, gte, lt, lte, in, nin, contains
                Value = "active"
            }
        }
    }
};
```

### TableCard - 表格卡片

表格卡片提供完整的数据表格功能，包括搜索、排序、分页等。

```csharp
var tableCard = new TableCardConfig
{
    Title = "用户列表",
    Description = "系统用户管理",
    Table = new TableConfig
    {
        Columns = new List<TableColumn>
        {
            new() 
            { 
                Name = "id", 
                Label = "ID", 
                Type = "text", 
                Width = "80px",
                Sortable = false
            },
            new() 
            { 
                Name = "name", 
                Label = "姓名", 
                Type = "text", 
                Sortable = true,
                Searchable = true
            },
            new() 
            { 
                Name = "email", 
                Label = "邮箱", 
                Type = "text", 
                Searchable = true
            },
            new() 
            { 
                Name = "status", 
                Label = "状态", 
                Type = "status",
                Mapping = new Dictionary<string, object>
                {
                    ["active"] = new { label = "活跃", status = "success" },
                    ["inactive"] = new { label = "停用", status = "danger" }
                }
            },
            new()
            {
                Name = "createdAt",
                Label = "创建时间",
                Type = "datetime",
                Format = "YYYY-MM-DD HH:mm:ss"
            }
        },
        ShowIndex = true,
        ShowSelection = true,
        Pagination = new TablePaginationConfig
        {
            Enabled = true,
            PageSize = 20,
            PageSizeOptions = new List<int> { 10, 20, 50, 100 }
        }
    },
    Data = new TableDataConfig
    {
        ApiUrl = "/api/users",
        PageSize = 20
    }
};
```

### InfoGridCard - 信息网格卡片

信息网格卡片用于展示网格化的信息项，适合系统监控和状态展示。

```csharp
var infoGridCard = new InfoGridCardConfig
{
    Title = "系统状态",
    Description = "服务器运行状态监控",
    Grid = new InfoGridConfig
    {
        Columns = 4,
        Gap = "16px",
        Responsive = true,
        ResponsiveColumns = new Dictionary<string, int>
        {
            ["xs"] = 2,
            ["sm"] = 3,
            ["md"] = 4,
            ["lg"] = 4,
            ["xl"] = 4
        }
    },
    Items = new List<InfoGridItem>
    {
        new()
        {
            Title = "CPU使用率",
            Value = "65%",
            Icon = new InfoGridIconConfig 
            { 
                Name = "fa-microchip", 
                Color = "#52c41a" 
            },
            Theme = "success"
        },
        new()
        {
            Title = "内存使用",
            Value = "8.2GB / 16GB",
            Icon = new InfoGridIconConfig 
            { 
                Name = "fa-memory", 
                Color = "#faad14" 
            },
            Theme = "warning"
        },
        new()
        {
            Title = "磁盘空间",
            Value = "156GB / 500GB",
            Icon = new InfoGridIconConfig 
            { 
                Name = "fa-hdd", 
                Color = "#1890ff" 
            },
            Theme = "info"
        },
        new()
        {
            Title = "网络流量",
            Value = "1.2GB/s",
            Icon = new InfoGridIconConfig 
            { 
                Name = "fa-network-wired", 
                Color = "#ff4d4f" 
            },
            Theme = "danger"
        }
    }
};
```

### 创建复杂仪表板

使用仪表板配置可以创建包含多个分区的复杂页面：

```csharp
[HttpGet("dashboard")]
public ActionResult<UdlDashboardConfig> GetDashboard()
{
    try
    {
        var dashboard = new UdlDashboardConfig
        {
            Title = "智慧管理平台",
            Description = "系统运营数据总览",
            Theme = "primary",
            ShowRefresh = true,
            RefreshInterval = 60,
            Sections = new List<UdlDashboardSection>
            {
                new()
                {
                    Title = "核心指标",
                    Description = "关键业务数据",
                    Cards = new List<Dictionary<string, object>>
                    {
                        _generator.GenerateCard(new StatCardConfig { /* ... */ }),
                        _generator.GenerateCard(new StatCardConfig { /* ... */ }),
                        _generator.GenerateCard(new StatCardConfig { /* ... */ })
                    },
                    PageConfig = new UdlPageConfig
                    {
                        Layout = "grid",
                        LayoutConfig = new UdlLayoutConfig 
                        { 
                            Type = "grid", 
                            Columns = 3,
                            Gap = "16px"
                        }
                    }
                },
                new()
                {
                    Title = "数据分析",
                    Description = "趋势和统计分析",
                    Cards = new List<Dictionary<string, object>>
                    {
                        _generator.GenerateCard(new ChartCardConfig { /* ... */ }),
                        _generator.GenerateCard(new TableCardConfig { /* ... */ })
                    }
                }
            }
        };

        var result = _generator.GenerateDashboard(dashboard);
        return Ok(result);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "生成仪表板失败");
        return StatusCode(500, "仪表板生成失败");
    }
}
```

## 🔧 高级功能

### 权限控制

所有卡片都支持基于角色和权限的访问控制，支持两种配置方式：

```csharp
var card = new StatCardConfig
{
    Title = "敏感数据",
    
    // 方式1：使用 Permission 对象（推荐）
    Permission = new UdlPermissionConfig
    {
        RequiredPermissions = new List<string> { "view.statistics", "view.financial" },
        RequiredRoles = new List<string> { "admin", "manager" },
        OnDenied = "hide" // hide, disable, readonly
    },
    
    // 方式2：直接使用属性
    Permissions = new List<string> { "view.statistics" },
    Roles = new List<string> { "admin", "manager" },
    
    // 显示条件表达式
    VisibleOn = "${user.level >= 3}"
};
```

### 主题定制

支持内置主题和自定义主题：

```csharp
// 使用内置主题
var card = new StatCardConfig
{
    Theme = "primary", // primary, success, warning, danger, info, dark
    ThemeConfig = new UdlCardTheme 
    { 
        Name = "primary" 
    }
};

// 自定义主题
var customCard = new StatCardConfig
{
    ThemeConfig = new UdlCardTheme
    {
        Name = "custom",
        PrimaryColor = "#722ed1",
        BackgroundColor = "#f9f0ff",
        BorderColor = "#d3adf7",
        TextColor = "#722ed1"
    }
};
```

### 数据刷新配置

支持自动和手动数据刷新：

```csharp
var card = new StatCardConfig
{
    Refresh = new UdlRefreshConfig
    {
        Auto = true,
        Interval = 30000, // 30秒
        ShowButton = true,
        ShowLoading = true
    }
};
```

### 配置验证

每个构建器都内置了配置验证功能：

```csharp
[HttpPost("validate")]
public ActionResult ValidateCardConfig([FromBody] StatCardConfig cardConfig)
{
    var builder = new StatCardBuilder();
    
    if (!builder.Validate(cardConfig))
    {
        return BadRequest("卡片配置验证失败：数据值不能为空");
    }
    
    return Ok("配置验证成功");
}
```

### 自定义卡片类型

创建自定义卡片类型和构建器：

```csharp
// 1. 定义自定义卡片配置
public class CustomCardConfig : UdlCardConfig
{
    public override string Type => "custom";
    public string CustomProperty { get; set; } = string.Empty;
    public Dictionary<string, object> CustomData { get; set; } = new();
}

// 2. 实现自定义构建器
public class CustomCardBuilder : IUdlCardBuilder<CustomCardConfig>, IUdlCardBuilderBase
{
    public string CardType => "custom";

    public Dictionary<string, object> Build(CustomCardConfig cardConfig)
    {
        return new Dictionary<string, object>
        {
            ["type"] = "custom",
            ["id"] = cardConfig.Id,
            ["title"] = cardConfig.Title,
            ["customProperty"] = cardConfig.CustomProperty,
            ["customData"] = cardConfig.CustomData,
            ["className"] = "custom-card"
        };
    }

    public bool Validate(CustomCardConfig cardConfig)
    {
        return !string.IsNullOrEmpty(cardConfig.CustomProperty);
    }

    // IUdlCardBuilderBase 接口实现
    Dictionary<string, object> IUdlCardBuilderBase.Build(UdlCardConfig cardConfig)
    {
        if (cardConfig is not CustomCardConfig config)
            throw new ArgumentException($"期望 {nameof(CustomCardConfig)}，实际 {cardConfig.GetType().Name}");
        return Build(config);
    }

    bool IUdlCardBuilderBase.Validate(UdlCardConfig cardConfig)
    {
        return cardConfig is CustomCardConfig config && Validate(config);
    }
}

// 3. 注册自定义构建器
services.AddUdlCardBuilder<CustomCardConfig, CustomCardBuilder>();
```

## 🛠️ 异常处理和日志

UdlCardsGenerator 内置了完整的异常处理和日志记录：

```csharp
[HttpGet("cards/{cardType}")]
public ActionResult<Dictionary<string, object>> GetCard(string cardType)
{
    try
    {
        var cardConfig = CreateCardConfig(cardType);
        var amisConfig = _generator.GenerateCard(cardConfig);
        return Ok(amisConfig);
    }
    catch (NotSupportedException ex)
    {
        // 不支持的卡片类型
        _logger.LogWarning("不支持的卡片类型: {CardType}", cardType);
        return BadRequest($"不支持的卡片类型: {cardType}");
    }
    catch (ArgumentException ex)
    {
        // 配置参数错误
        _logger.LogError(ex, "卡片配置参数错误: {CardType}", cardType);
        return BadRequest($"配置错误: {ex.Message}");
    }
    catch (Exception ex)
    {
        // 其他未知错误
        _logger.LogError(ex, "生成卡片时发生未知错误: {CardType}", cardType);
        return StatusCode(500, "卡片生成失败");
    }
}
```

### 严格模式控制

通过 `StrictMode` 配置控制错误处理行为：

```csharp
// StrictMode = false (默认)：单个卡片失败不影响整个页面
var pageConfig = _generator.GeneratePage(cards);

// StrictMode = true：任何卡片失败都会抛出异常
services.Configure<UdlCardsOptions>(options =>
{
    options.StrictMode = true; // 严格模式
});
```

## 🎨 与前端集成

生成的配置可以直接在前端使用：

```javascript
// 前端接收后端生成的配置
async function loadDashboard() {
    try {
        const response = await fetch('/api/dashboard/overview');
        const config = await response.json();
        
        // 使用 Amis Cards 渲染
        const amisCards = new AmisCards.Core();
        amisCards.renderPage('#dashboard-container', config);
    } catch (error) {
        console.error('加载仪表板失败:', error);
    }
}

// 监听卡片事件
document.addEventListener('amis-card-refresh', (event) => {
    const { cardId, cardType } = event.detail;
    console.log(`卡片 ${cardId} (${cardType}) 刷新完成`);
});
```

## 📈 性能优化

### 启用缓存

```csharp
services.AddUdlCards(options =>
{
    options.EnableCaching = true;
    options.CacheExpirationMinutes = 15;
});
```

### 分页控制

```csharp
services.AddUdlCards(options =>
{
    options.MaxCardsPerPage = 10; // 限制每页最大卡片数
});
```

### 异步处理

对于大量卡片的场景，建议使用异步处理：

```csharp
[HttpGet("dashboard/async")]
public async Task<ActionResult<UdlDashboardConfig>> GetDashboardAsync()
{
    var tasks = cards.Select(async card => 
    {
        await Task.Delay(10); // 模拟异步处理
        return _generator.GenerateCard(card);
    });
    
    var results = await Task.WhenAll(tasks);
    return Ok(new { cards = results });
}
```

## 🔍 最佳实践

### 1. 配置验证
```csharp
// 始终验证配置
if (!builder.Validate(cardConfig))
{
    throw new ArgumentException("卡片配置无效");
}
```

### 2. 错误处理
```csharp
// 使用try-catch包装所有生成操作
try
{
    var config = _generator.GenerateCard(cardConfig);
    return Ok(config);
}
catch (Exception ex)
{
    _logger.LogError(ex, "生成卡片失败");
    return StatusCode(500, "生成失败");
}
```

### 3. 权限检查
```csharp
// 在控制器级别进行权限预检查
[Authorize("ViewDashboard")]
public ActionResult GetDashboard()
{
    // 卡片级别的权限在前端处理
}
```

### 4. 主题一致性
```csharp
// 在应用中保持主题一致性
services.AddUdlCards(options =>
{
    options.DefaultTheme = "primary"; // 全局默认主题
});
```

### 5. 性能监控
```csharp
// 监控生成性能
var stopwatch = Stopwatch.StartNew();
var config = _generator.GenerateCard(cardConfig);
stopwatch.Stop();

if (stopwatch.ElapsedMilliseconds > 1000)
{
    _logger.LogWarning("卡片生成耗时过长: {ElapsedMs}ms", 
        stopwatch.ElapsedMilliseconds);
}
```

## 🆘 故障排除

### 常见问题

1. **不支持的卡片类型**
   ```csharp
   // 确保卡片类型正确
   public override string Type => "stat"; // 必须与构建器的 CardType 匹配
   ```

2. **配置验证失败**
   ```csharp
   // 检查必填字段
   Data = new StatDataConfig
   {
       Value = 0 // 不能为 null
   }
   ```

3. **权限配置无效**
   ```csharp
   // 确保权限服务已注册
   services.AddUdlCards(options =>
   {
       options.EnablePermissionControl = true;
   });
   ```

4. **主题不生效**
   ```csharp
   // 使用正确的主题名称
   Theme = "primary" // 不是 "Primary"
   ```

### 调试模式

启用调试模式获取详细日志：

```csharp
services.AddUdlCards(options =>
{
    options.DebugMode = true;
});
```

## 📚 API 参考

### 核心类

- `UdlCardsGenerator` - 主要的卡片生成器
- `UdlCardConfig` - 所有卡片的基础配置类
- `IUdlCardBuilder<T>` - 泛型构建器接口
- `IUdlCardBuilderBase` - 非泛型构建器接口

### 配置选项类

- `UdlCardsOptions` - 全局配置选项
- `UdlPageConfig` - 页面配置
- `UdlDashboardConfig` - 仪表板配置
- `UdlLayoutConfig` - 布局配置

### 卡片配置类

- `StatCardConfig` - 统计卡片配置
- `ChartCardConfig` - 图表卡片配置
- `TableCardConfig` - 表格卡片配置
- `InfoCardConfig` - 信息卡片配置
- `InfoGridCardConfig` - 信息网格卡片配置

## 🔄 版本信息

- **当前版本**: 1.0.0
- **目标框架**: .NET 10.0
- **兼容性**: Amis Cards V2.0+
- **最后更新**: 2025年

如有问题或建议，请提交 Issue 或 Pull Request。 