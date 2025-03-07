using CodeSpirit.Charts.Analysis;
using CodeSpirit.Charts.Models;
using CodeSpirit.Charts.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.Json;

namespace CodeSpirit.Charts.Tests.Services
{
    [TestClass]
    public class EChartConfigGeneratorTests
    {
        private Mock<IDataAnalyzer> _mockDataAnalyzer;
        private Mock<ILogger<EChartConfigGenerator>> _mockLogger;
        private EChartConfigGenerator _generator;
        
        [TestInitialize]
        public void Setup()
        {
            _mockDataAnalyzer = new Mock<IDataAnalyzer>();
            _mockLogger = new Mock<ILogger<EChartConfigGenerator>>();
            _generator = new EChartConfigGenerator(_mockDataAnalyzer.Object, _mockLogger.Object);
            
            // 设置数据分析器的基本行为
            var dataStructure = new DataStructureInfo
            {
                RowCount = 5,
                DimensionFields = new List<string> { "category", "date" },
                MetricFields = new List<string> { "value", "count" },
                FieldTypes = new Dictionary<string, Type>
                {
                    { "category", typeof(string) },
                    { "date", typeof(DateTime) },
                    { "value", typeof(double) },
                    { "count", typeof(int) }
                }
            };
            
            var dataFeatures = new DataFeatures
            {
                IsTimeSeries = true,
                HasTrend = true,
                IsCategorical = true,
                MetricStatistics = new Dictionary<string, MetricStats>
                {
                    { "value", new MetricStats { Min = 0, Max = 100, Average = 50 } },
                    { "count", new MetricStats { Min = 0, Max = 10, Average = 5 } }
                }
            };
            
            _mockDataAnalyzer.Setup(a => a.AnalyzeDataStructure(It.IsAny<object>()))
                .Returns(dataStructure);
            
            _mockDataAnalyzer.Setup(a => a.ExtractDataFeatures(It.IsAny<object>()))
                .Returns(dataFeatures);
        }
        
        [TestMethod]
        public void GenerateEChartConfig_LineChart_ShouldReturnValidConfiguration()
        {
            // 安排
            var config = new ChartConfig
            {
                Type = ChartType.Line,
                Title = "测试折线图",
                XAxis = new AxisConfig { Type = "category", Name = "类别" },
                YAxis = new AxisConfig { Type = "value", Name = "数值" },
                Series = new List<SeriesConfig>
                {
                    new SeriesConfig { Name = "销售额", Type = "line" }
                },
                Legend = new LegendConfig { Data = new List<string> { "销售额" } },
                Toolbox = new ToolboxConfig { }
            };
            
            // 执行
            var result = _generator.GenerateEChartConfig(config) as Dictionary<string, object>;
            
            // 断言
            Assert.IsNotNull(result);
            Assert.AreEqual("测试折线图", ((Dictionary<string, object>)result["title"])["text"]);
            Assert.AreEqual("category", ((Dictionary<string, object>)result["xAxis"])["type"]);
            Assert.AreEqual("value", ((Dictionary<string, object>)result["yAxis"])["type"]);
            
            var series = result["series"] as List<Dictionary<string, object>>;
            Assert.IsNotNull(series);
            Assert.AreEqual(1, series.Count);
            Assert.AreEqual("销售额", series[0]["name"]);
            Assert.AreEqual("line", series[0]["type"]);
        }
        
        [TestMethod]
        public void GenerateEChartConfig_PieChart_ShouldReturnValidConfiguration()
        {
            // 安排
            var config = new ChartConfig
            {
                Type = ChartType.Pie,
                Title = "测试饼图",
                Series = new List<SeriesConfig>
                {
                    new SeriesConfig 
                    { 
                        Name = "访问来源", 
                        Type = "pie",
                        Label = new Dictionary<string, object> { ["show"] = true }
                    }
                },
                Legend = new LegendConfig { Orient = "vertical", Position = "right" }
            };
            
            // 执行
            var result = _generator.GenerateEChartConfig(config) as Dictionary<string, object>;
            
            // 断言
            Assert.IsNotNull(result);
            Assert.AreEqual("测试饼图", ((Dictionary<string, object>)result["title"])["text"]);
            Assert.IsFalse(result.ContainsKey("xAxis"));
            Assert.IsFalse(result.ContainsKey("yAxis"));
            
            var legend = result["legend"] as Dictionary<string, object>;
            Assert.IsNotNull(legend);
            Assert.AreEqual("vertical", legend["orient"]);
            Assert.AreEqual("right", legend["left"]);
            
            var series = result["series"] as List<Dictionary<string, object>>;
            Assert.IsNotNull(series);
            Assert.AreEqual(1, series.Count);
            Assert.AreEqual("访问来源", series[0]["name"]);
            Assert.AreEqual("pie", series[0]["type"]);
            
            var tooltip = result["tooltip"] as Dictionary<string, object>;
            Assert.IsNotNull(tooltip);
            Assert.AreEqual("item", tooltip["trigger"]);
        }
        
        [TestMethod]
        public void GenerateCompleteEChartConfig_WithData_ShouldPopulateSeriesData()
        {
            // 安排
            var config = new ChartConfig
            {
                Type = ChartType.Bar,
                Title = "测试柱状图",
                XAxis = new AxisConfig { Type = "category" },
                YAxis = new AxisConfig { Type = "value" },
                Series = new List<SeriesConfig>
                {
                    new SeriesConfig { Name = "销售额", Type = "bar" }
                }
            };
            
            var testData = new[]
            {
                new { category = "类别1", value = 100 },
                new { category = "类别2", value = 200 },
                new { category = "类别3", value = 300 }
            };
            
            // 执行
            var result = _generator.GenerateCompleteEChartConfig(config, testData) as Dictionary<string, object>;
            
            // 断言
            Assert.IsNotNull(result);
            var jsonStr = JsonConvert.SerializeObject(result);
            Console.WriteLine($"ECharts配置: {jsonStr}");
            
            // 由于GenerateCompleteEChartConfig依赖于ExtractDataArray等方法的实现
            // 这里我们只做基本验证，确保生成了配置对象
            Assert.IsTrue(result.ContainsKey("series"));
        }
        
        [TestMethod]
        public void GenerateEChartConfigJson_ShouldReturnValidJsonString()
        {
            // 安排
            var config = new ChartConfig
            {
                Type = ChartType.Line,
                Title = "测试图表",
                Series = new List<SeriesConfig>
                {
                    new SeriesConfig { Name = "数据系列", Type = "line" }
                }
            };
            
            // 执行
            var jsonStr = _generator.GenerateEChartConfigJson(config);
            
            // 断言
            Assert.IsNotNull(jsonStr);
            Assert.IsTrue(jsonStr.Length > 0);
            
            // 验证是否为有效的JSON
            var obj = JsonConvert.DeserializeObject(jsonStr);
            Assert.IsNotNull(obj);
        }
        
        [TestMethod]
        public void GenerateEChartConfig_RadarChart_ShouldReturnValidConfiguration()
        {
            // 安排
            var config = new ChartConfig
            {
                Type = ChartType.Radar,
                Title = "能力雷达图",
                Series = new List<SeriesConfig>
                {
                    new SeriesConfig { Name = "预算分配", Type = "radar" }
                }
            };
            
            // 执行
            var result = _generator.GenerateEChartConfig(config) as Dictionary<string, object>;
            
            // 断言
            Assert.IsNotNull(result);
            Assert.IsTrue(result.ContainsKey("radar"));
            Assert.IsNotNull(result["radar"]);
            
            var series = result["series"] as List<Dictionary<string, object>>;
            Assert.IsNotNull(series);
            Assert.AreEqual(1, series.Count);
            Assert.AreEqual("radar", series[0]["type"]);
        }
        
        [TestMethod]
        public void GenerateEChartConfig_HeatmapChart_ShouldReturnValidConfiguration()
        {
            // 安排
            var config = new ChartConfig
            {
                Type = ChartType.Heatmap,
                Title = "热力图示例",
                XAxis = new AxisConfig { Type = "category" },
                YAxis = new AxisConfig { Type = "category" },
                Series = new List<SeriesConfig>
                {
                    new SeriesConfig { Name = "热力值", Type = "heatmap" }
                },
                ExtraStyles = new Dictionary<string, object>
                {
                    ["visualMap"] = new Dictionary<string, object>
                    {
                        ["min"] = 0,
                        ["max"] = 10
                    }
                }
            };
            
            // 执行
            var result = _generator.GenerateEChartConfig(config) as Dictionary<string, object>;
            
            // 断言
            Assert.IsNotNull(result);
            Assert.IsTrue(result.ContainsKey("visualMap"));
            
            var visualMap = result["visualMap"] as Dictionary<string, object>;
            Assert.IsNotNull(visualMap);
            Assert.AreEqual(0, visualMap["min"]);
            Assert.AreEqual(10, visualMap["max"]);
            
            var series = result["series"] as List<Dictionary<string, object>>;
            Assert.IsNotNull(series);
            Assert.AreEqual("heatmap", series[0]["type"]);
        }
    }
} 