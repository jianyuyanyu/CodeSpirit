# CodeSpirit UDL Cards SDK

## 概述

CodeSpirit UDL Cards SDK 是一个专门为后端开发设计的卡片生成器，用于生成与前端 Amis Cards V2.0 兼容的配置。通过这个SDK，后端开发者可以使用流畅的C# API来创建各种类型的卡片和仪表板。

## 特性

### 🎯 核心功能
- **类型安全**：完全基于强类型的C#模型
- **流畅API**：提供直观易用的建构器模式
- **模块化设计**：支持自定义卡片类型扩展
- **配置验证**：内置配置验证机制
- **权限控制**：集成权限和角色验证
- **主题支持**：支持多种内置和自定义主题

### 📊 支持的卡片类型
- **统计卡片 (Stat)** - 数值展示、趋势分析、进度指示
- **图表卡片 (Chart)** - 基于ECharts的各种图表类型
- **表格卡片 (Table)** - 功能完整的数据表格
- **信息卡片 (Info)** - 静态信息展示
- **信息网格 (InfoGrid)** - 网格化信息布局

## 快速开始

### 1. 安装和配置

在 `Startup.cs` 或 `Program.cs` 中注册服务：

```csharp
// 使用配置文件
services.AddUdlCards(configuration);

// 或使用委托配置
services.AddUdlCards(options =>
{
    options.DefaultTheme = "primary";
    options.EnablePermissionControl = true;
    options.ApiBaseUrl = "https://api.example.com";
});
```

### 2. 基本使用

```csharp
public class DashboardController : ControllerBase
{
    private readonly UdlCardsGenerator _generator;

    public DashboardController(UdlCardsGenerator generator)
    {
        _generator = generator;
    }

    [HttpGet("stats")]
    public ActionResult<ApiResponse> GetStatsCard()
    {
        var statCard = new StatCardConfig
        {
            Title = "用户总数",
            Data = new StatDataConfig
            {
                Value = 1248,
                Label = "注册用户",
                Unit = "人",
                Formatter = "number"
            },
            Icon = new StatIconConfig
            {
                Name = "fa-users",
                Color = "#1890ff"
            },
            Theme = new UdlCardTheme { Name = "primary" }
        };

        var amisConfig = _generator.GenerateCard(statCard);
        return ApiResponse.Success(amisConfig);
    }
}
```

## 详细使用指南

### 统计卡片 (StatCard)

统计卡片用于展示数值型数据，支持趋势、进度条和动画效果。

```csharp
var statCard = new StatCardConfig
{
    Title = "销售业绩",
    Data = new StatDataConfig
    {
        Value = 89356.7m,
        Label = "本月销售额",
        Unit = "元",
        Formatter = "currency",
        DecimalPlaces = 2,
        ShowSeparator = true,
        ApiUrl = "/api/stats/sales" // 动态数据源
    },
    Icon = new StatIconConfig
    {
        Name = "fa-dollar-sign",
        Position = "left",
        Size = "lg",
        Color = "#52c41a",
        BackgroundColor = "#f6ffed"
    },
    Trend = new StatTrendConfig
    {
        Direction = "up",
        Value = 15.3m,
        IsPercentage = true,
        Text = "较上月增长"
    },
    Progress = new StatProgressConfig
    {
        Target = 100000,
        Show = true,
        ShowText = true,
        Color = "#52c41a"
    }
};
```

### 图表卡片 (ChartCard)

图表卡片基于ECharts，支持多种图表类型和丰富的配置选项。

```csharp
var chartCard = new ChartCardConfig
{
    Title = "用户增长趋势",
    Chart = new ChartConfig
    {
        Type = "line",
        Height = 350,
        Theme = "dark",
        Responsive = true,
        Options = new Dictionary<string, object>
        {
            ["grid"] = new { left = "3%", right = "4%", bottom = "3%" },
            ["xAxis"] = new { type = "category" },
            ["yAxis"] = new { type = "value" }
        }
    },
    Data = new ChartDataConfig
    {
        ApiUrl = "/api/charts/user-growth",
        FieldMapping = new ChartFieldMapping
        {
            XField = "date",
            YField = "count",
            SeriesField = "type"
        },
        RefreshInterval = 30000
    }
};
```

### 表格卡片 (TableCard)

表格卡片提供完整的数据表格功能，包括搜索、排序、分页等。

```csharp
var tableCard = new TableCardConfig
{
    Title = "用户列表",
    Table = new TableConfig
    {
        Columns = new List<TableColumn>
        {
            new() { Name = "id", Label = "ID", Type = "text", Width = "80px" },
            new() { Name = "name", Label = "姓名", Type = "text", Sortable = true },
            new() { Name = "email", Label = "邮箱", Type = "text", Searchable = true },
            new() { Name = "status", Label = "状态", Type = "status", 
                   Mapping = new Dictionary<string, object>
                   {
                       ["active"] = new { label = "活跃", status = "success" },
                       ["inactive"] = new { label = "停用", status = "danger" }
                   }
            }
        },
        ShowIndex = true,
        ShowSelection = true,
        Pagination = new TablePaginationConfig
        {
            Enabled = true,
            PageSizeOptions = new List<int> { 10, 20, 50 }
        }
    },
    Data = new TableDataConfig
    {
        ApiUrl = "/api/users",
        PageSize = 20
    }
};
```

### 信息网格卡片 (InfoGridCard)

信息网格卡片用于展示网格化的信息项，适合系统监控和状态展示。

```csharp
var infoGridCard = new InfoGridCardConfig
{
    Title = "系统状态",
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
            ["lg"] = 4
        }
    },
    Items = new List<InfoGridItem>
    {
        new()
        {
            Title = "CPU使用率",
            Value = "65%",
            Icon = new InfoGridIconConfig { Name = "fa-microchip", Color = "#52c41a" },
            Theme = "success"
        },
        new()
        {
            Title = "内存使用",
            Value = "8.2GB / 16GB",
            Icon = new InfoGridIconConfig { Name = "fa-memory", Color = "#faad14" },
            Theme = "warning"
        }
    }
};
```

### 创建仪表板

使用仪表板配置可以创建包含多个分区的复杂页面：

```csharp
var dashboard = new UdlDashboardConfig
{
    Title = "智慧管理平台",
    Description = "系统运营数据总览",
    Sections = new List<UdlDashboardSection>
    {
        new()
        {
            Title = "核心指标",
            Cards = new List<UdlCardConfig>
            {
                // 统计卡片列表
            },
            PageConfig = new UdlPageConfig
            {
                Layout = new UdlLayoutConfig { Type = "grid", Columns = 3 }
            }
        },
        new()
        {
            Title = "数据分析",
            Cards = new List<UdlCardConfig>
            {
                // 图表卡片列表
            }
        }
    }
};

var amisConfig = _generator.GenerateDashboard(dashboard);
```

## 高级功能

### 权限控制

所有卡片都支持基于角色和权限的访问控制：

```csharp
var card = new StatCardConfig
{
    // ... 基本配置
    Permission = new UdlPermissionConfig
    {
        RequiredPermissions = new List<string> { "view.statistics" },
        RequiredRoles = new List<string> { "admin", "manager" },
        OnDenied = "hide" // hide, disable, readonly
    }
};
```

### 主题定制

支持内置主题和自定义主题：

```csharp
// 使用内置主题
Theme = new UdlCardTheme { Name = "primary" }

// 自定义主题
Theme = new UdlCardTheme
{
    Name = "custom",
    PrimaryColor = "#722ed1",
    BackgroundColor = "#f9f0ff",
    BorderColor = "#d3adf7",
    TextColor = "#722ed1"
}
```

### 数据刷新

支持自动和手动数据刷新：

```csharp
Refresh = new UdlRefreshConfig
{
    Auto = true,
    Interval = 30000, // 30秒
    ShowButton = true,
    ShowLoading = true
}
```

### 自定义建构器

可以创建自定义卡片类型：

```csharp
public class CustomCardBuilder : IUdlCardBuilder
{
    public string CardType => "custom";

    public JObject Build(UdlCardConfig cardConfig)
    {
        // 实现自定义卡片逻辑
        return new JObject
        {
            ["type"] = "custom",
            // ... 其他配置
        };
    }

    public ValidationResult Validate(UdlCardConfig cardConfig)
    {
        // 实现验证逻辑
        return ValidationResult.Success();
    }
}

// 注册自定义建构器
services.AddUdlCardBuilder<CustomCardBuilder>();
```

## 配置选项

在 `appsettings.json` 中配置SDK选项：

```json
{
  "UdlCards": {
    "StrictMode": false,
    "DefaultTheme": "primary",
    "EnablePermissionControl": true,
    "DebugMode": false,
    "ApiBaseUrl": "https://api.example.com",
    "DefaultPageConfig": {
      "Layout": {
        "Type": "grid",
        "Columns": 3,
        "Gap": "16px"
      }
    },
    "Cache": {
      "Enabled": true,
      "ExpirationMinutes": 30
    }
  }
}
```

## 与前端集成

生成的配置可以直接在前端使用：

```javascript
// 前端接收后端生成的配置
fetch('/api/dashboard/overview')
  .then(response => response.json())
  .then(config => {
    // 使用 Amis Cards 渲染
    const amisCards = new AmisCards.Core();
    amisCards.renderPage('#container', config.data);
  });
```

## 最佳实践

1. **配置验证**：始终验证卡片配置的正确性
2. **权限控制**：合理使用权限配置保护敏感数据
3. **性能优化**：启用缓存和合理设置刷新间隔
4. **主题一致性**：在应用中保持主题的一致性
5. **错误处理**：妥善处理配置生成过程中的异常
