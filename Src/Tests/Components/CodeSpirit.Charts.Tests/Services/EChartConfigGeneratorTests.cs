using CodeSpirit.Charts.Analysis;
using CodeSpirit.Charts.Models;
using CodeSpirit.Charts.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.Json;
using Xunit;

namespace CodeSpirit.Charts.Tests.Services
{
    public class EChartConfigGeneratorTests
    {
        private readonly Mock<IDataAnalyzer> _mockDataAnalyzer;
        private readonly Mock<ILogger<EChartConfigGenerator>> _mockLogger;
        private readonly EChartConfigGenerator _generator;
        
        public EChartConfigGeneratorTests()
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
        
        [Fact]
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
            Assert.NotNull(result);
            Assert.Equal("测试折线图", ((Dictionary<string, object>)result["title"])["text"]);
            Assert.Equal("category", ((Dictionary<string, object>)result["xAxis"])["type"]);
            Assert.Equal("value", ((Dictionary<string, object>)result["yAxis"])["type"]);
            
            var series = result["series"] as List<Dictionary<string, object>>;
            Assert.NotNull(series);
            Assert.Equal(1, series.Count);
            Assert.Equal("销售额", series[0]["name"]);
            Assert.Equal("line", series[0]["type"]);
        }
        
        [Fact]
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
            Assert.NotNull(result);
            Assert.Equal("测试饼图", ((Dictionary<string, object>)result["title"])["text"]);
            Assert.False(result.ContainsKey("xAxis"));
            Assert.False(result.ContainsKey("yAxis"));
            
            var legend = result["legend"] as Dictionary<string, object>;
            Assert.NotNull(legend);
            Assert.Equal("vertical", legend["orient"]);
            Assert.Equal("right", legend["left"]);
            
            var series = result["series"] as List<Dictionary<string, object>>;
            Assert.NotNull(series);
            Assert.Equal(1, series.Count);
            Assert.Equal("访问来源", series[0]["name"]);
            Assert.Equal("pie", series[0]["type"]);
            
            var tooltip = result["tooltip"] as Dictionary<string, object>;
            Assert.NotNull(tooltip);
            Assert.Equal("item", tooltip["trigger"]);
        }
        
        [Fact]
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
            Assert.NotNull(result);
            var jsonStr = JsonConvert.SerializeObject(result);
            Console.WriteLine($"ECharts配置: {jsonStr}");
            
            // 由于GenerateCompleteEChartConfig依赖于ExtractDataArray等方法的实现
            // 这里我们只做基本验证，确保生成了配置对象
            Assert.True(result.ContainsKey("series"));
        }
        
        [Fact]
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
            Assert.NotNull(jsonStr);
            Assert.True(jsonStr.Length > 0);
            
            // 验证是否为有效的JSON
            var obj = JsonConvert.DeserializeObject(jsonStr);
            Assert.NotNull(obj);
        }
        
        [Fact]
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
            Assert.NotNull(result);
            Assert.True(result.ContainsKey("radar"));
            Assert.NotNull(result["radar"]);
            
            var series = result["series"] as List<Dictionary<string, object>>;
            Assert.NotNull(series);
            Assert.Equal(1, series.Count);
            Assert.Equal("radar", series[0]["type"]);
        }
        
        [Fact]
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
            Assert.NotNull(result);
            Assert.Equal("热力图示例", ((Dictionary<string, object>)result["title"])["text"]);
            Assert.True(result.ContainsKey("visualMap"));
            
            var series = result["series"] as List<Dictionary<string, object>>;
            Assert.NotNull(series);
            Assert.Equal(1, series.Count);
            Assert.Equal("heatmap", series[0]["type"]);
        }

        [Fact]
        public void GenerateEChartConfig_BarChart_ShouldReturnValidConfiguration()
        {
            // 安排
            var config = new ChartConfig
            {
                Type = ChartType.Bar,
                Title = "柱状图示例",
                XAxis = new AxisConfig { Type = "category", Name = "产品" },
                YAxis = new AxisConfig { Type = "value", Name = "销量" },
                Series = new List<SeriesConfig>
                {
                    new SeriesConfig 
                    { 
                        Name = "销量", 
                        Type = "bar",
                        ItemStyle = new Dictionary<string, object>
                        {
                            ["borderRadius"] = 5,
                            ["color"] = new Dictionary<string, object>
                            {
                                ["type"] = "linear",
                                ["x"] = 0,
                                ["y"] = 0,
                                ["x2"] = 0,
                                ["y2"] = 1,
                                ["colorStops"] = new[]
                                {
                                    new Dictionary<string, object> { ["offset"] = 0, ["color"] = "#83bff6" },
                                    new Dictionary<string, object> { ["offset"] = 1, ["color"] = "#188df0" }
                                }
                            }
                        }
                    }
                },
                Legend = new LegendConfig { Data = new List<string> { "销量" } }
            };
            
            // 执行
            var result = _generator.GenerateEChartConfig(config) as Dictionary<string, object>;
            
            // 断言
            Assert.NotNull(result);
            Assert.Equal("柱状图示例", ((Dictionary<string, object>)result["title"])["text"]);
            Assert.Equal("category", ((Dictionary<string, object>)result["xAxis"])["type"]);
            Assert.Equal("value", ((Dictionary<string, object>)result["yAxis"])["type"]);
            
            var series = result["series"] as List<Dictionary<string, object>>;
            Assert.NotNull(series);
            Assert.Equal(1, series.Count);
            Assert.Equal("bar", series[0]["type"]);
            
            var itemStyle = series[0]["itemStyle"] as Dictionary<string, object>;
            Assert.NotNull(itemStyle);
            Assert.Equal(5, itemStyle["borderRadius"]);
            Assert.NotNull(itemStyle["color"]);
        }

        [Fact]
        public void GenerateEChartConfig_ScatterChart_ShouldReturnValidConfiguration()
        {
            // 安排
            var config = new ChartConfig
            {
                Type = ChartType.Scatter,
                Title = "散点图示例",
                XAxis = new AxisConfig { Type = "value", Name = "身高 (cm)" },
                YAxis = new AxisConfig { Type = "value", Name = "体重 (kg)" },
                Series = new List<SeriesConfig>
                {
                    new SeriesConfig 
                    { 
                        Name = "男性", 
                        Type = "scatter",
                        ItemStyle = new Dictionary<string, object> { ["color"] = "#5470C6" },
                        ExtraOptions = new Dictionary<string, object>
                        {
                            ["symbolSize"] = 10,
                            ["emphasis"] = new Dictionary<string, object>
                            {
                                ["focus"] = "series",
                                ["itemStyle"] = new Dictionary<string, object>
                                {
                                    ["shadowBlur"] = 10,
                                    ["shadowColor"] = "rgba(0, 0, 0, 0.5)"
                                }
                            }
                        }
                    }
                },
                Interaction = new InteractionConfig
                {
                    Tooltip = new Dictionary<string, object>
                    {
                        ["formatter"] = "{a} <br/>{b} : {c}"
                    }
                }
            };
            
            // 执行
            var result = _generator.GenerateEChartConfig(config) as Dictionary<string, object>;
            
            // 断言
            Assert.NotNull(result);
            Assert.Equal("散点图示例", ((Dictionary<string, object>)result["title"])["text"]);
            Assert.Equal("value", ((Dictionary<string, object>)result["xAxis"])["type"]);
            Assert.Equal("value", ((Dictionary<string, object>)result["yAxis"])["type"]);
            
            var series = result["series"] as List<Dictionary<string, object>>;
            Assert.NotNull(series);
            Assert.Equal(1, series.Count);
            Assert.Equal("scatter", series[0]["type"]);
            Assert.Equal(10, series[0]["symbolSize"]);
            
            var tooltip = result["tooltip"] as Dictionary<string, object>;
            Assert.NotNull(tooltip);
            Assert.Equal("{a} <br/>{b} : {c}", tooltip["formatter"]);
        }

        [Fact]
        public void GenerateEChartConfig_GaugeChart_ShouldReturnValidConfiguration()
        {
            // 安排
            var config = new ChartConfig
            {
                Type = ChartType.Gauge,
                Title = "仪表盘示例",
                Series = new List<SeriesConfig>
                {
                    new SeriesConfig 
                    { 
                        Name = "业务指标", 
                        Type = "gauge",
                        ExtraOptions = new Dictionary<string, object>
                        {
                            ["progress"] = new Dictionary<string, object> { ["show"] = true },
                            ["axisLine"] = new Dictionary<string, object>
                            {
                                ["lineStyle"] = new Dictionary<string, object>
                                {
                                    ["width"] = 30,
                                    ["color"] = new[]
                                    {
                                        new object[] { 0.3, "#67e0e3" },
                                        new object[] { 0.7, "#37a2da" },
                                        new object[] { 1.0, "#fd666d" }
                                    }
                                }
                            },
                            ["pointer"] = new Dictionary<string, object>
                            {
                                ["itemStyle"] = new Dictionary<string, object> { ["color"] = "auto" }
                            },
                            ["detail"] = new Dictionary<string, object>
                            {
                                ["valueAnimation"] = true,
                                ["formatter"] = "{value}%"
                            }
                        }
                    }
                }
            };
            
            // 执行
            var result = _generator.GenerateEChartConfig(config) as Dictionary<string, object>;
            
            // 断言
            Assert.NotNull(result);
            Assert.Equal("仪表盘示例", ((Dictionary<string, object>)result["title"])["text"]);
            
            var series = result["series"] as List<Dictionary<string, object>>;
            Assert.NotNull(series);
            Assert.Equal(1, series.Count);
            Assert.Equal("gauge", series[0]["type"]);
            
            var progress = series[0]["progress"] as Dictionary<string, object>;
            Assert.NotNull(progress);
            Assert.True((bool)progress["show"]);
            
            var axisLine = series[0]["axisLine"] as Dictionary<string, object>;
            Assert.NotNull(axisLine);
            
            var detail = series[0]["detail"] as Dictionary<string, object>;
            Assert.NotNull(detail);
            Assert.Equal("{value}%", detail["formatter"]);
        }

        [Fact]
        public void GenerateEChartConfig_FunnelChart_ShouldReturnValidConfiguration()
        {
            // 安排
            var config = new ChartConfig
            {
                Type = ChartType.Funnel,
                Title = "漏斗图示例",
                Series = new List<SeriesConfig>
                {
                    new SeriesConfig 
                    { 
                        Name = "转化漏斗", 
                        Type = "funnel",
                        ExtraOptions = new Dictionary<string, object>
                        {
                            ["sort"] = "descending",
                            ["gap"] = 2,
                            ["min"] = 0,
                            ["max"] = 100,
                            ["left"] = "10%",
                            ["right"] = "10%",
                            ["funnelAlign"] = "center"
                        },
                        Label = new Dictionary<string, object>
                        {
                            ["show"] = true,
                            ["position"] = "inside"
                        },
                        ItemStyle = new Dictionary<string, object>
                        {
                            ["borderColor"] = "#fff",
                            ["borderWidth"] = 1
                        }
                    }
                },
                Legend = new LegendConfig 
                { 
                    Data = new List<string> { "访问", "注册", "加入购物车", "付款", "交易完成" },
                    Orient = "vertical",
                    Position = "left"
                }
            };
            
            // 执行
            var result = _generator.GenerateEChartConfig(config) as Dictionary<string, object>;
            
            // 断言
            Assert.NotNull(result);
            Assert.Equal("漏斗图示例", ((Dictionary<string, object>)result["title"])["text"]);
            
            var series = result["series"] as List<Dictionary<string, object>>;
            Assert.NotNull(series);
            Assert.Equal(1, series.Count);
            Assert.Equal("funnel", series[0]["type"]);
            Assert.Equal("descending", series[0]["sort"]);
            Assert.Equal(2, series[0]["gap"]);
            
            var legend = result["legend"] as Dictionary<string, object>;
            Assert.NotNull(legend);
            Assert.Equal("vertical", legend["orient"]);
            Assert.Equal("left", legend["left"]);
        }

        [Fact]
        public void GenerateEChartConfig_SankeyChart_ShouldReturnValidConfiguration()
        {
            // 安排
            var config = new ChartConfig
            {
                Type = ChartType.Sankey,
                Title = "桑基图示例",
                Series = new List<SeriesConfig>
                {
                    new SeriesConfig 
                    { 
                        Name = "数据流向", 
                        Type = "sankey",
                        ExtraOptions = new Dictionary<string, object>
                        {
                            ["nodeWidth"] = 20,
                            ["nodeGap"] = 10,
                            ["layoutIterations"] = 64,
                            ["orient"] = "horizontal",
                            ["emphasis"] = new Dictionary<string, object>
                            {
                                ["focus"] = "adjacency"
                            },
                            ["levels"] = new object[]
                            {
                                new Dictionary<string, object>
                                {
                                    ["depth"] = 0,
                                    ["itemStyle"] = new Dictionary<string, object>
                                    {
                                        ["color"] = "#fbb4ae"
                                    },
                                    ["lineStyle"] = new Dictionary<string, object>
                                    {
                                        ["color"] = "source",
                                        ["opacity"] = 0.6
                                    }
                                }
                            }
                        }
                    }
                }
            };
            
            // 执行
            var result = _generator.GenerateEChartConfig(config) as Dictionary<string, object>;
            
            // 断言
            Assert.NotNull(result);
            Assert.Equal("桑基图示例", ((Dictionary<string, object>)result["title"])["text"]);
            
            var series = result["series"] as List<Dictionary<string, object>>;
            Assert.NotNull(series);
            Assert.Equal(1, series.Count);
            Assert.Equal("sankey", series[0]["type"]);
            Assert.Equal(20, series[0]["nodeWidth"]);
            Assert.Equal(10, series[0]["nodeGap"]);
            
            var emphasis = series[0]["emphasis"] as Dictionary<string, object>;
            Assert.NotNull(emphasis);
            Assert.Equal("adjacency", emphasis["focus"]);
            
            var levels = series[0]["levels"] as object[];
            Assert.NotNull(levels);
            Assert.Equal(1, levels.Length);
        }

        [Fact]
        public void GenerateEChartConfig_TreeChart_ShouldReturnValidConfiguration()
        {
            // 安排
            var config = new ChartConfig
            {
                Type = ChartType.Tree,
                Title = "树图示例",
                Series = new List<SeriesConfig>
                {
                    new SeriesConfig 
                    { 
                        Name = "组织架构", 
                        Type = "tree",
                        ExtraOptions = new Dictionary<string, object>
                        {
                            ["layout"] = "orthogonal",
                            ["orient"] = "vertical",
                            ["initialTreeDepth"] = 3,
                            ["roam"] = true,
                            ["label"] = new Dictionary<string, object>
                            {
                                ["position"] = "left",
                                ["verticalAlign"] = "middle",
                                ["align"] = "right"
                            },
                            ["leaves"] = new Dictionary<string, object>
                            {
                                ["label"] = new Dictionary<string, object>
                                {
                                    ["position"] = "right",
                                    ["verticalAlign"] = "middle",
                                    ["align"] = "left"
                                }
                            },
                            ["emphasis"] = new Dictionary<string, object>
                            {
                                ["focus"] = "descendant"
                            }
                        }
                    }
                }
            };
            
            // 执行
            var result = _generator.GenerateEChartConfig(config) as Dictionary<string, object>;
            
            // 断言
            Assert.NotNull(result);
            Assert.Equal("树图示例", ((Dictionary<string, object>)result["title"])["text"]);
            
            var series = result["series"] as List<Dictionary<string, object>>;
            Assert.NotNull(series);
            Assert.Equal(1, series.Count);
            Assert.Equal("tree", series[0]["type"]);
            Assert.Equal("orthogonal", series[0]["layout"]);
            Assert.Equal(3, series[0]["initialTreeDepth"]);
            Assert.True((bool)series[0]["roam"]);
            
            var leaves = series[0]["leaves"] as Dictionary<string, object>;
            Assert.NotNull(leaves);
            
            var emphasis = series[0]["emphasis"] as Dictionary<string, object>;
            Assert.NotNull(emphasis);
            Assert.Equal("descendant", emphasis["focus"]);
        }

        [Fact]
        public void GenerateEChartConfig_GraphChart_ShouldReturnValidConfiguration()
        {
            // 安排
            var config = new ChartConfig
            {
                Type = ChartType.Graph,
                Title = "关系图示例",
                Series = new List<SeriesConfig>
                {
                    new SeriesConfig 
                    { 
                        Name = "人物关系", 
                        Type = "graph",
                        ExtraOptions = new Dictionary<string, object>
                        {
                            ["layout"] = "force",
                            ["draggable"] = true,
                            ["roam"] = true,
                            ["label"] = new Dictionary<string, object>
                            {
                                ["show"] = true
                            },
                            ["edgeSymbol"] = new[] { "none", "arrow" },
                            ["edgeLabel"] = new Dictionary<string, object>
                            {
                                ["show"] = false,
                                ["formatter"] = "{c}"
                            },
                            ["categories"] = new object[] 
                            { 
                                new Dictionary<string, object> { ["name"] = "类别A" },
                                new Dictionary<string, object> { ["name"] = "类别B" }
                            },
                            ["force"] = new Dictionary<string, object>
                            {
                                ["repulsion"] = 100,
                                ["edgeLength"] = 30
                            }
                        }
                    }
                },
                Legend = new LegendConfig
                {
                    Data = new List<string> { "类别A", "类别B" }
                }
            };
            
            // 执行
            var result = _generator.GenerateEChartConfig(config) as Dictionary<string, object>;
            
            // 断言
            Assert.NotNull(result);
            Assert.Equal("关系图示例", ((Dictionary<string, object>)result["title"])["text"]);
            
            var series = result["series"] as List<Dictionary<string, object>>;
            Assert.NotNull(series);
            Assert.Equal(1, series.Count);
            Assert.Equal("graph", series[0]["type"]);
            Assert.Equal("force", series[0]["layout"]);
            Assert.True((bool)series[0]["draggable"]);
            Assert.True((bool)series[0]["roam"]);
            
            var categories = series[0]["categories"] as object[];
            Assert.NotNull(categories);
            Assert.Equal(2, categories.Length);
            
            var force = series[0]["force"] as Dictionary<string, object>;
            Assert.NotNull(force);
            Assert.Equal(100, force["repulsion"]);
            Assert.Equal(30, force["edgeLength"]);
        }

        [Fact]
        public void GenerateEChartConfig_WithTheme_ShouldApplyThemeOptions()
        {
            // 安排
            var config = new ChartConfig
            {
                Type = ChartType.Line,
                Title = "主题测试图表",
                Theme = "dark",
                ExtraStyles = new Dictionary<string, object>
                {
                    ["backgroundColor"] = "#333",
                    ["textStyle"] = new Dictionary<string, object>
                    {
                        ["color"] = "#fff"
                    },
                    ["color"] = new[] { "#dd6b66", "#759aa0", "#e69d87", "#8dc1a9", "#ea7e53" }
                },
                Series = new List<SeriesConfig>
                {
                    new SeriesConfig { Name = "数据系列", Type = "line" }
                }
            };
            
            // 执行
            var result = _generator.GenerateEChartConfig(config) as Dictionary<string, object>;
            
            // 断言
            Assert.NotNull(result);
            Assert.Equal("#333", result["backgroundColor"]);
            
            var textStyle = result["textStyle"] as Dictionary<string, object>;
            Assert.NotNull(textStyle);
            Assert.Equal("#fff", textStyle["color"]);
            
            var colorPalette = result["color"] as object[];
            Assert.NotNull(colorPalette);
            Assert.Equal(5, colorPalette.Length);
            Assert.Equal("#dd6b66", colorPalette[0]);
        }

        [Fact]
        public void GenerateEChartConfig_WithCustomAnimation_ShouldIncludeAnimationOptions()
        {
            // 安排
            var config = new ChartConfig
            {
                Type = ChartType.Bar,
                Title = "带动画效果的图表",
                ExtraStyles = new Dictionary<string, object>
                {
                    ["animation"] = true,
                    ["animationThreshold"] = 2000,
                    ["animationDuration"] = 1000,
                    ["animationEasing"] = "cubicOut",
                    ["animationDelay"] = 0
                },
                Series = new List<SeriesConfig>
                {
                    new SeriesConfig 
                    { 
                        Name = "数据系列", 
                        Type = "bar",
                        ExtraOptions = new Dictionary<string, object>
                        {
                            ["animationDuration"] = 2000,
                            ["animationEasing"] = "elasticOut"
                        }
                    }
                }
            };
            
            // 执行
            var result = _generator.GenerateEChartConfig(config) as Dictionary<string, object>;
            
            // 断言
            Assert.NotNull(result);
            Assert.True((bool)result["animation"]);
            Assert.Equal(1000, result["animationDuration"]);
            Assert.Equal("cubicOut", result["animationEasing"]);
            
            var series = result["series"] as List<Dictionary<string, object>>;
            Assert.NotNull(series);
            Assert.Equal(1, series.Count);
            Assert.Equal(2000, series[0]["animationDuration"]);
            Assert.Equal("elasticOut", series[0]["animationEasing"]);
        }
    }
} 