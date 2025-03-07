using System.Collections.Generic;
using System.Threading.Tasks;
using CodeSpirit.Charts.Analysis;
using CodeSpirit.Charts.Extensions;
using CodeSpirit.Charts.Models;
using CodeSpirit.Charts.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace CodeSpirit.Charts.Tests.Samples
{
    /// <summary>
    /// 图表API控制器示例
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ChartsApiController : ControllerBase
    {
        private readonly IChartRecommender _recommender;
        private readonly IEChartConfigGenerator _echartGenerator;
        private readonly IMemoryCache _memoryCache;
        private readonly IHttpContextAccessor _httpContextAccessor;
        
        public ChartsApiController(
            IChartRecommender recommender, 
            IEChartConfigGenerator echartGenerator,
            IMemoryCache memoryCache,
            IHttpContextAccessor httpContextAccessor)
        {
            _recommender = recommender;
            _echartGenerator = echartGenerator;
            _memoryCache = memoryCache;
            _httpContextAccessor = httpContextAccessor;
        }
        
        /// <summary>
        /// 获取销售数据图表配置
        /// </summary>
        [HttpGet("sales")]
        public IActionResult GetSalesChart([FromQuery] string chartType = "auto")
        {
            // 示例销售数据
            var salesData = new[]
            {
                new { Month = "一月", Sales = 120, Cost = 80, Profit = 40 },
                new { Month = "二月", Sales = 132, Cost = 90, Profit = 42 },
                new { Month = "三月", Sales = 101, Cost = 70, Profit = 31 },
                new { Month = "四月", Sales = 134, Cost = 85, Profit = 49 },
                new { Month = "五月", Sales = 90, Cost = 65, Profit = 25 },
                new { Month = "六月", Sales = 230, Cost = 150, Profit = 80 },
                new { Month = "七月", Sales = 210, Cost = 140, Profit = 70 }
            };
            
            if (chartType == "auto")
            {
                // 使用自动推荐的图表类型
                return this.AutoChartResult(salesData);
            }
            
            // 使用指定的图表类型
            ChartType? type = chartType.ToLower() switch
            {
                "bar" => ChartType.Bar,
                "line" => ChartType.Line,
                "pie" => ChartType.Pie,
                _ => null
            };
            
            return this.AutoChartResult(salesData, type);
        }
        
        /// <summary>
        /// 获取性能指标图表配置
        /// </summary>
        [HttpGet("performance")]
        public IActionResult GetPerformanceChart([FromQuery] string chartType = "auto")
        {
            // 示例性能数据
            var performanceData = new[]
            {
                new { Metric = "CPU使用率", Current = 45, Threshold = 80, Historical = 60 },
                new { Metric = "内存使用率", Current = 65, Threshold = 75, Historical = 70 },
                new { Metric = "磁盘IO", Current = 30, Threshold = 60, Historical = 45 },
                new { Metric = "网络流量", Current = 80, Threshold = 90, Historical = 75 },
                new { Metric = "响应时间", Current = 120, Threshold = 200, Historical = 150 }
            };
            
            if (chartType == "auto")
            {
                // 使用自动推荐的图表类型
                return this.AutoChartResult(performanceData);
            }
            
            // 使用指定的图表类型
            ChartType? type = chartType.ToLower() switch
            {
                "bar" => ChartType.Bar,
                "radar" => ChartType.Radar,
                "line" => ChartType.Line,
                _ => null
            };
            
            return this.AutoChartResult(performanceData, type);
        }
        
        /// <summary>
        /// 获取地区分布图表配置
        /// </summary>
        [HttpGet("distribution")]
        public IActionResult GetDistributionChart([FromQuery] string chartType = "auto")
        {
            // 示例分布数据
            var distributionData = new[]
            {
                new { Region = "华东", Sales = 4300, Customers = 1200, Stores = 35 },
                new { Region = "华南", Sales = 3200, Customers = 900, Stores = 28 },
                new { Region = "华北", Sales = 5100, Customers = 1500, Stores = 40 },
                new { Region = "西南", Sales = 2800, Customers = 800, Stores = 25 },
                new { Region = "西北", Sales = 1800, Customers = 500, Stores = 15 },
                new { Region = "东北", Sales = 2300, Customers = 600, Stores = 20 }
            };
            
            if (chartType == "auto")
            {
                // 使用自动推荐的图表类型
                return this.AutoChartResult(distributionData);
            }
            
            // 使用指定的图表类型
            ChartType? type = chartType.ToLower() switch
            {
                "bar" => ChartType.Bar,
                "pie" => ChartType.Pie,
                _ => null
            };
            
            return this.AutoChartResult(distributionData, type);
        }
        
        /// <summary>
        /// 获取推荐的图表类型
        /// </summary>
        [HttpGet("recommend")]
        public IActionResult GetRecommendedChartTypes([FromQuery] string dataType, [FromQuery] int maxCount = 3)
        {
            object data;
            
            // 根据数据类型选择示例数据
            switch (dataType.ToLower())
            {
                case "sales":
                    data = new[]
                    {
                        new { Month = "一月", Sales = 120, Cost = 80, Profit = 40 },
                        new { Month = "二月", Sales = 132, Cost = 90, Profit = 42 }
                    };
                    break;
                    
                case "performance":
                    data = new[]
                    {
                        new { Metric = "CPU使用率", Current = 45, Threshold = 80 },
                        new { Metric = "内存使用率", Current = 65, Threshold = 75 }
                    };
                    break;
                    
                case "distribution":
                    data = new[]
                    {
                        new { Region = "华东", Sales = 4300, Customers = 1200 },
                        new { Region = "华南", Sales = 3200, Customers = 900 }
                    };
                    break;
                    
                default:
                    return BadRequest("不支持的数据类型");
            }
            
            // 使用控制器扩展方法获取推荐的图表类型
            return this.ChartRecommendations(data, maxCount);
        }
        
        /// <summary>
        /// 使用手动配置的图表
        /// </summary>
        [HttpGet("custom")]
        public IActionResult GetCustomChart()
        {
            // 示例数据
            var data = new[]
            {
                new { Category = "A类", Value1 = 100, Value2 = 60 },
                new { Category = "B类", Value1 = 80, Value2 = 70 },
                new { Category = "C类", Value1 = 120, Value2 = 90 },
                new { Category = "D类", Value1 = 70, Value2 = 50 }
            };
            
            // 手动构建图表配置
            var config = new ChartConfig
            {
                Title = "自定义图表",
                Subtitle = "手动配置示例",
                Type = ChartType.Bar,
                XAxis = new AxisConfig { Type = "category", Name = "类别" },
                YAxis = new AxisConfig { Type = "value", Name = "数值" },
                Series = new List<SeriesConfig>
                {
                    new SeriesConfig { Name = "指标1", Type = "bar" },
                    new SeriesConfig { Name = "指标2", Type = "bar" }
                },
                Legend = new LegendConfig 
                { 
                    Data = new List<string> { "指标1", "指标2" },
                    Orient = "horizontal",
                    Position = "top"
                },
                Toolbox = new ToolboxConfig { Show = true }
            };
            
            // 返回图表配置和数据
            return this.ChartResult(config, data);
        }
        
        /// <summary>
        /// 直接获取ECharts配置JSON
        /// </summary>
        [HttpGet("echarts-config")]
        public IActionResult GetEChartsConfig()
        {
            // 示例数据
            var data = new[]
            {
                new { Name = "产品A", Sales = 1000, Target = 1200 },
                new { Name = "产品B", Sales = 1200, Target = 1000 },
                new { Name = "产品C", Sales = 800, Target = 1000 },
                new { Name = "产品D", Sales = 1500, Target = 1300 },
                new { Name = "产品E", Sales = 900, Target = 1100 }
            };
            
            // 使用图表推荐器生成配置
            var config = _recommender.GenerateChartConfig(data);
            
            // 直接返回ECharts配置对象
            var echartConfig = _echartGenerator.GenerateCompleteEChartConfig(config, data);
            return Ok(echartConfig);
        }
    }
} 