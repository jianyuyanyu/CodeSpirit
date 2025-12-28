# CodeSpirit.Charts 智能图表组件使用文档

## 目录

1. [简介](#1-简介)
2. [主要功能](#2-主要功能)
3. [项目结构](#3-项目结构)
4. [快速上手](#4-快速上手)
   - 4.1 [安装和配置](#41-安装和配置)
   - 4.2 [服务注册详解](#42-服务注册详解)
   - 4.3 [使用特性标记控制器方法](#43-使用特性标记控制器方法)
   - 4.4 [实际项目示例](#44-实际项目示例)
   - 4.5 [前端自动呈现](#45-前端自动呈现)
5. [高级使用](#5-高级使用)
6. [支持的图表类型](#6-支持的图表类型)
7. [配置选项](#7-配置选项)
8. [错误处理和最佳实践](#8-错误处理和最佳实践)
9. [未来规划](#9-未来规划)
10. [总结](#10-总结)

## 1. 简介

CodeSpirit.Charts 是一个功能强大的智能图表组件，基于特性驱动、声明式配置，让您的应用轻松拥有美观、智能的数据可视化功能。组件采用 ECharts 作为底层渲染引擎，提供了丰富的图表类型和配置选项。

## 2. 主要功能

- **多图表类型支持**：折线图、柱状图、饼图、散点图、仪表盘、卡片图表等
- **特性驱动配置**：通过特性（Attribute）轻松为API方法添加图表功能
- **智能数据处理**：自动数据验证、转换和错误处理
- **空数据友好**：优雅处理空数据和异常情况
- **自动图表推荐**：基于数据特征智能推荐合适的图表类型
- **丰富的配置选项**：支持标题、主题、颜色、交互等多种配置
- **扩展性强**：支持自定义图表提供者和数据处理器

## 3. 项目结构

```
CodeSpirit.Charts/                    # 智能图表组件
├── Attributes/                       # 特性定义
│   ├── ChartAttribute.cs            # 基础图表特性
│   ├── ChartDataAttribute.cs        # 数据映射特性
│   ├── ChartTitleAttribute.cs       # 图表标题特性
│   └── ...
├── Core/                            # 核心组件
│   ├── Abstractions/                # 抽象接口
│   │   ├── IChartProvider.cs        # 图表提供者接口
│   │   ├── IChartService.cs         # 图表服务接口
│   │   ├── IDataProcessor.cs        # 数据处理器接口
│   │   └── IChartRecommender.cs     # 图表推荐器接口
│   └── Services/                    # 核心服务
│       ├── ChartService.cs          # 图表服务实现
│       └── DataProcessor.cs         # 数据处理器实现
├── Providers/                       # 图表提供者
│   └── ECharts/                     # ECharts 提供者
│       └── EChartsProvider.cs       # ECharts 图表生成器
├── Models/                          # 数据模型
│   ├── ChartConfig.cs              # 图表配置模型
│   ├── ChartType.cs                # 图表类型枚举
│   └── ...
├── Extensions/                      # 扩展方法
│   ├── ControllerExtensions.cs     # 控制器扩展方法
│   └── ServiceCollectionExtensions.cs # 服务注册扩展
├── Services/                        # 业务服务
│   ├── EChartConfigGenerator.cs    # ECharts 配置生成器
│   └── ...
└── CodeSpirit.Charts.csproj        # 项目文件
```

## 4. 快速上手

### 4.1 安装和配置

在项目中添加对CodeSpirit.Charts的引用：

```xml
<ItemGroup>
    <ProjectReference Include="..\..\Components\CodeSpirit.Charts\CodeSpirit.Charts.csproj" />
</ItemGroup>
```

在Program.cs中注册服务：

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// 添加图表服务
builder.Services.AddChartsService();

var app = builder.Build();
```

### 4.2 服务注册详解

图表组件会自动注册以下服务：

```csharp
// 核心服务
services.AddScoped<IChartService, ChartService>();
services.AddScoped<IDataProcessor, DataProcessor>();

// 图表提供者
services.AddScoped<IChartProvider, EChartsProvider>();

// 可选：图表推荐器
services.AddScoped<IChartRecommender, ChartRecommender>();
```

### 4.3 使用特性标记控制器方法

最简单的使用方式是通过特性标记控制器方法：

```csharp
/// <summary>
/// 获取用户增长趋势图的配置
/// </summary>
/// <param name="dateRange">日期范围</param>
/// <returns>图表配置</returns>
[HttpGet("usergrowth")]
[Display(Name = "用户增长趋势")]
[Chart("用户增长趋势", "展示用户随时间的增长趋势")]
[ChartType(ChartType.Line)]
[ChartData(dimensionField: "Date", metricFields: new[] { "UserCount" })]
public async Task<IActionResult> GetUserGrowthStatisticsAsync([FromQuery] DateTime[] dateRange)
{
    DateTimeOffset startDate = dateRange?.Length > 0 ? dateRange[0] : DateTimeOffset.Now.AddMonths(-1);
    DateTimeOffset endDate = dateRange?.Length > 1 ? dateRange[1] : DateTimeOffset.Now.AddDays(1);

    // 获取数据
    var dailyGrowth = await _userService.GetUserGrowthAsync(startDate, endDate);
    return this.AutoChartResult(dailyGrowth);
}
```

### 4.4 实际项目示例

以下是一个完整的审计统计控制器示例，展示了多种图表类型的使用：

```csharp
/// <summary>
/// 审计统计图表控制器
/// </summary>
[DisplayName("审计统计")]
[Navigation(Icon = "fa-solid fa-chart-bar")]
public class AuditStatisticsController : ApiControllerBase
{
    private readonly IAuditService _auditService;
    private readonly ILogger<AuditStatisticsController> _logger;

    public AuditStatisticsController(IAuditService auditService, ILogger<AuditStatisticsController> logger)
    {
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// 获取操作类型统计 - 饼图
    /// </summary>
    [HttpGet("operations")]
    [Chart("pie")]
    [ChartData(CategoryField = "OperationType", ValueField = "Count")]
    [ChartTitle("操作类型分布")]
    public async Task<IActionResult> GetOperationStatsAsync([FromQuery] DateTime[] dateRange)
    {
        var stats = await _auditService.GetOperationStatsAsync(startDate, endDate);
        var chartData = stats.Select(kvp => new { OperationType = kvp.Key, Count = kvp.Value });
        return await this.AutoChartResult(chartData);
    }

    /// <summary>
    /// 获取操作趋势 - 折线图
    /// </summary>
    [HttpGet("trend")]
    [Chart("line")]
    [ChartData(XField = "Date", YField = "Count")]
    [ChartTitle("系统操作趋势")]
    public async Task<IActionResult> GetOperationTrendAsync([FromQuery] DateTime[] dateRange)
    {
        var trend = await _auditService.GetOperationTrendAsync(startDate, endDate, 24);
        var chartData = trend.Select(kvp => new { Date = kvp.Key.ToString("yyyy-MM-dd HH:mm"), Count = kvp.Value });
        return await this.AutoChartResult(chartData);
    }

    /// <summary>
    /// 获取操作汇总 - 卡片图表
    /// </summary>
    [HttpGet("summary")]
    [Chart("card")]
    [ChartData(CategoryField = "Period", ValueField = "Count")]
    public async Task<IActionResult> GetOperationSummaryAsync()
    {
        var summaryData = new List<object>
        {
            new { Period = "今日", Count = todayCount },
            new { Period = "本周", Count = weekCount },
            new { Period = "本月", Count = monthCount }
        };
        return await this.AutoChartResult(summaryData);
    }

    /// <summary>
    /// 获取成功率 - 仪表盘
    /// </summary>
    [HttpGet("success-rate")]
    [Chart("gauge")]
    [ChartData(CategoryField = "Status", ValueField = "Percentage")]
    public async Task<IActionResult> GetSuccessRateAsync([FromQuery] DateTime[] dateRange)
    {
        // 重要：避免除零错误，防止返回 NaN
        var totalCount = trend?.Values.Sum() ?? 0;
        double successPercentage = totalCount > 0 
            ? Math.Round((double)(totalCount * 0.95) / totalCount * 100, 2)
            : 0.0;  // 空数据时返回 0%
        
        var chartData = new List<object> { new { Status = "成功", Percentage = successPercentage } };
        return await this.AutoChartResult(chartData);
    }
}
```

### 4.5 前端自动呈现

请遵循“Statistics”命名约定，CodeSpirit会自动渲染图表页面。

## 5. 高级使用

### 5.1 通过ChartConfigBuilder构建图表

可以使用`ChartConfigBuilder`类以编程方式构建图表：

```csharp
[HttpGet("custom-chart")]
public async Task<ActionResult> GetCustomChart()
{
    var data = await _dataService.GetData();
    
    var chartBuilder = new ChartConfigBuilder(
        _serviceProvider, 
        _memoryCache, 
        _httpContextAccessor,
        _recommender,
        _echartGenerator);
    
    var chartConfig = await chartBuilder
        .SetTitle("自定义图表")
        .SetSubtitle("数据分析")
        .BuildChartConfigForDataAsync(data, ChartType.Bar);
    
    return Ok(new { data, chartConfig });
}
```

### 5.2 图表自动推荐

组件支持自动分析数据并推荐合适的图表类型：

```csharp
[HttpGet("recommend-charts")]
public async Task<ActionResult> GetRecommendedCharts()
{
    var data = await _dataService.GetData();
    
    var chartBuilder = new ChartConfigBuilder(
        _serviceProvider, 
        _memoryCache, 
        _httpContextAccessor,
        _recommender,
        _echartGenerator);
    
    // 获取最多3种推荐的图表配置
    var recommendedCharts = await chartBuilder.GetRecommendedChartConfigsAsync(data, 3);
    
    return Ok(new { data, recommendedCharts });
}
```

### 5.3 使用图表特性配置选项

`ChartAttribute`提供了多种配置选项：

```csharp
[HttpGet("sales-trend")]
[Chart(
    Title = "销售趋势分析", 
    Description = "按月份显示销售趋势",
    AutoRefresh = true,
    RefreshInterval = 300,
    ShowToolbox = true,
    Theme = "dark",
    Height = 500,
    EnableExport = true
)]
public async Task<ActionResult> GetSalesTrend()
{
    var data = await _salesService.GetTrendData();
    return Ok(data);
}
```

### 5.4 数据映射特性

使用`ChartDataAttribute`标记模型属性，指定数据映射：

```csharp
public class SalesViewModel
{
    [ChartData(FieldType = ChartFieldType.Category, AxisType = "x")]
    public string Month { get; set; }
    
    [ChartData(FieldType = ChartFieldType.Value, SeriesName = "销售额")]
    public decimal Sales { get; set; }
    
    [ChartData(FieldType = ChartFieldType.Value, SeriesName = "利润")]
    public decimal Profit { get; set; }
}
```

## 6. 支持的图表类型

### 6.1 基础图表类型

| 图表类型 | 标识符 | 适用场景 | 示例 |
|---------|--------|----------|------|
| 折线图 | `line` | 趋势分析、时间序列数据 | 用户增长趋势、销售趋势 |
| 柱状图 | `bar` | 分类数据对比 | 各部门销售额、用户活跃度 |
| 饼图 | `pie` | 占比分析、构成分析 | 操作类型分布、市场份额 |
| 散点图 | `scatter` | 相关性分析、分布分析 | 价格与销量关系 |
| 雷达图 | `radar` | 多维度评估 | 员工能力评估、产品对比 |

### 6.2 专业图表类型

| 图表类型 | 标识符 | 适用场景 | 特点 |
|---------|--------|----------|------|
| 仪表盘 | `gauge` | 指标监控、进度展示 | 支持百分比、阈值设置 |
| 卡片图表 | `card` | 关键指标展示 | 简洁的数值展示 |
| 热力图 | `heatmap` | 密度分析、相关性矩阵 | 颜色深浅表示数值大小 |
| 树图 | `tree` | 层级结构展示 | 组织架构、分类体系 |
| 桑基图 | `sankey` | 流向分析 | 资金流向、用户路径 |

### 6.3 图表类型选择建议

```csharp
// 根据数据特征自动推荐图表类型
[HttpGet("auto-recommend")]
public async Task<IActionResult> GetAutoRecommendChart()
{
    var data = await _dataService.GetData();
    
    // 系统会自动分析数据特征并推荐合适的图表类型
    return await this.AutoChartResult(data);
}
```

## 7. 配置选项

### 7.1 图表基本配置

`ChartConfig`类提供了丰富的图表配置选项：

- 标题和副标题
- 图表类型和子类型
- 坐标轴配置
- 图例配置
- 系列配置
- 工具箱配置
- 交互配置
- 主题配置

### 7.2 坐标轴配置

```csharp
var config = new ChartConfig
{
    XAxis = new AxisConfig
    {
        Name = "月份",
        Type = "category",
        Data = new List<string> { "1月", "2月", "3月", "4月", "5月", "6月" }
    },
    YAxis = new AxisConfig
    {
        Name = "销售额",
        Type = "value"
    }
};
```

### 7.3 系列配置

```csharp
var config = new ChartConfig
{
    Series = new List<SeriesConfig>
    {
        new SeriesConfig
        {
            Name = "销售额",
            Type = "line",
            Data = new List<object> { 120, 132, 101, 134, 90, 230 },
            Label = new Dictionary<string, object>
            {
                { "show", true },
                { "position", "top" }
            }
        },
        new SeriesConfig
        {
            Name = "利润",
            Type = "line",
            Data = new List<object> { 220, 182, 191, 234, 290, 330 }
        }
    }
};
```

## 8. 错误处理和最佳实践

### 8.1 数据验证和错误处理

图表组件内置了完善的错误处理机制：

```csharp
[HttpGet("robust-chart")]
public async Task<IActionResult> GetRobustChart([FromQuery] DateTime[] dateRange)
{
    try
    {
        var data = await _dataService.GetData(dateRange);
        
        // 组件会自动处理以下情况：
        // 1. 空数据集合
        // 2. 数值计算异常（如除零错误）
        // 3. 数据类型转换错误
        // 4. 无效的图表配置
        
        return await this.AutoChartResult(data);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "获取图表数据时发生错误");
        
        // 返回友好的错误信息
        return BadRequest(new
        {
            error = "获取图表数据失败",
            message = ex.Message,
            suggestion = "请检查数据源是否正常，或稍后重试"
        });
    }
}
```

### 8.2 避免常见问题

#### 8.2.1 防止 NaN 值

```csharp
// ❌ 错误：可能导致除零错误
var percentage = (double)successCount / totalCount * 100;

// ✅ 正确：添加空值检查
var totalCount = data?.Sum() ?? 0;
var percentage = totalCount > 0 
    ? Math.Round((double)successCount / totalCount * 100, 2)
    : 0.0;
```

#### 8.2.2 处理空数据

```csharp
// ✅ 推荐：提供默认数据或友好提示
var chartData = stats?.Any() == true 
    ? stats.Select(s => new { Name = s.Key, Value = s.Value })
    : new[] { new { Name = "暂无数据", Value = 0 } };
```

#### 8.2.3 数据类型转换

```csharp
// ✅ 推荐：使用 Cast<object>() 确保类型兼容
var chartData = stats.Select(kvp => new 
{
    Category = kvp.Key,
    Value = kvp.Value
}).Cast<object>().ToList();
```

### 8.3 性能优化建议

1. **数据分页**：对于大数据集，考虑分页或限制数据量
2. **缓存策略**：对于计算密集的统计数据，使用适当的缓存
3. **异步处理**：使用 `async/await` 避免阻塞UI线程
4. **资源释放**：及时释放不需要的资源

### 8.4 测试建议

```csharp
[Fact]
public async Task ChartService_ShouldHandleEmptyData()
{
    // Arrange
    var emptyData = Array.Empty<object>();

    // Act
    var result = await _chartService.CreateChartConfigAsync("echarts", "bar", emptyData);

    // Assert
    Assert.NotNull(result);
    // 验证空数据时返回合适的默认配置
}

[Fact]
public async Task ChartService_ShouldNotReturnNaN()
{
    // Arrange
    var data = new[] { new { Status = "成功", Percentage = 0.0 } };

    // Act
    var result = await _chartService.CreateChartConfigAsync("echarts", "gauge", data);

    // Assert
    // 验证不包含 NaN 值
    var config = Assert.IsType<Dictionary<string, object>>(result);
    // ... 具体验证逻辑
}
```

## 9. 未来规划

1. **多图表联动**：支持图表之间的数据联动和交互
2. **AI驱动的数据洞察**：自动生成数据洞察和解释
3. **更多图表类型**：支持更多专业图表类型和定制化选项
4. **更好的移动适配**：优化移动端展示效果
5. **仪表盘及大屏支持**：支持复合仪表盘和数据大屏
6. **实时数据流**：支持WebSocket等实时数据更新
7. **导出功能增强**：支持更多格式的图表导出

## 10. 总结

CodeSpirit.Charts 智能图表组件为 .NET 应用提供了强大而易用的数据可视化能力。通过本文档，您已经了解了：

### 核心特性
- ✅ **特性驱动**：通过简单的特性标记即可为API添加图表功能
- ✅ **智能处理**：自动数据验证、转换和错误处理
- ✅ **多图表支持**：支持折线图、柱状图、饼图、仪表盘、卡片等多种图表类型
- ✅ **健壮性强**：优雅处理空数据、异常情况和边界条件

### 最佳实践
- 🔧 **数据验证**：始终检查空值和边界条件
- 🔧 **错误处理**：使用 try-catch 和友好的错误提示
- 🔧 **性能优化**：合理使用缓存和异步处理
- 🔧 **测试覆盖**：编写单元测试确保图表功能稳定

### 快速开始
1. 添加项目引用：`CodeSpirit.Charts`
2. 注册服务：`builder.Services.AddChartsService()`
3. 标记控制器方法：`[Chart("pie")]` + `[ChartData(...)]`
4. 返回数据：`return await this.AutoChartResult(data)`

### 技术支持
如果您在使用过程中遇到问题，请：
- 查看本文档的错误处理章节
- 参考项目中的单元测试示例
- 检查日志输出获取详细错误信息

CodeSpirit.Charts 将持续演进，为您的数据可视化需求提供更强大的支持！
