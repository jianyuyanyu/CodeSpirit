using CodeSpirit.Charts.Core.Abstractions;
using CodeSpirit.Charts.Providers.ECharts;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using System.Collections.Generic;
using Xunit;

namespace CodeSpirit.Charts.Tests.Providers
{
    /// <summary>
    /// 饼图数据处理测试
    /// </summary>
    public class PieChartDataProcessingTests
    {
        private readonly Mock<ILogger<EChartsProvider>> _loggerMock;
        private readonly EChartsProvider _provider;

        public PieChartDataProcessingTests()
        {
            _loggerMock = new Mock<ILogger<EChartsProvider>>();
            _provider = new EChartsProvider(_loggerMock.Object);
        }

        [Fact]
        public void GeneratePieChart_WithUserStatisticsData_ProcessesDataCorrectly()
        {
            // 准备：模拟用户性别分布数据
            var genderDistribution = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object> { ["Gender"] = "Unknown", ["Count"] = 4 },
                new Dictionary<string, object> { ["Gender"] = "Male", ["Count"] = 2 },
                new Dictionary<string, object> { ["Gender"] = "Female", ["Count"] = 1 }
            };

            var options = new Dictionary<string, object>
            {
                ["title"] = "用户性别分布",
                ["categoryField"] = "Gender",
                ["valueField"] = "Count"
            };

            // 执行
            var result = _provider.GenerateChartConfig("pie", genderDistribution, options);

            // 断言
            var chartConfig = Assert.IsType<Dictionary<string, object>>(result);
            
            // 验证标题
            Assert.True(chartConfig.ContainsKey("title"));
            var title = Assert.IsType<Dictionary<string, object>>(chartConfig["title"]);
            Assert.Equal("用户性别分布", title["text"]);
            
            // 验证图例
            Assert.True(chartConfig.ContainsKey("legend"));
            var legend = Assert.IsType<Dictionary<string, object>>(chartConfig["legend"]);
            Assert.True(legend.ContainsKey("data"));
            var legendData = Assert.IsType<object[]>(legend["data"]);
            Assert.Equal(3, legendData.Length);
            Assert.Contains("Unknown", legendData);
            Assert.Contains("Male", legendData);
            Assert.Contains("Female", legendData);
            
            // 验证系列数据
            Assert.True(chartConfig.ContainsKey("series"));
            var series = Assert.IsType<object[]>(chartConfig["series"]);
            Assert.Single(series);
            
            var seriesItem = Assert.IsType<Dictionary<string, object>>(series[0]);
            Assert.Equal("pie", seriesItem["type"]);
            
            var seriesData = Assert.IsType<object[]>(seriesItem["data"]);
            Assert.Equal(3, seriesData.Length);
            
            // 验证数据项
            var firstItem = Assert.IsType<Dictionary<string, object>>(seriesData[0]);
            Assert.Equal("Unknown", firstItem["name"]);
            Assert.Equal(4, firstItem["value"]);
            
            var secondItem = Assert.IsType<Dictionary<string, object>>(seriesData[1]);
            Assert.Equal("Male", secondItem["name"]);
            Assert.Equal(2, secondItem["value"]);
            
            var thirdItem = Assert.IsType<Dictionary<string, object>>(seriesData[2]);
            Assert.Equal("Female", thirdItem["name"]);
            Assert.Equal(1, thirdItem["value"]);
        }

        [Fact]
        public void GeneratePieChart_WithJsonStringData_ProcessesDataCorrectly()
        {
            // 准备：模拟JSON字符串格式的数据
            string jsonData = "[{\"Gender\":\"Unknown\",\"Count\":4},{\"Gender\":\"Male\",\"Count\":2},{\"Gender\":\"Female\",\"Count\":1}]";
            
            var options = new Dictionary<string, object>
            {
                ["categoryField"] = "Gender",
                ["valueField"] = "Count"
            };

            // 执行
            var result = _provider.GenerateChartConfig("pie", jsonData, options);

            // 断言
            var chartConfig = Assert.IsType<Dictionary<string, object>>(result);
            
            // 验证系列数据
            Assert.True(chartConfig.ContainsKey("series"));
            var series = Assert.IsType<object[]>(chartConfig["series"]);
            var seriesItem = Assert.IsType<Dictionary<string, object>>(series[0]);
            var seriesData = Assert.IsType<object[]>(seriesItem["data"]);
            Assert.Equal(3, seriesData.Length);
            
            // 验证图例数据
            var legend = Assert.IsType<Dictionary<string, object>>(chartConfig["legend"]);
            var legendData = Assert.IsType<object[]>(legend["data"]);
            Assert.Equal(3, legendData.Length);
            Assert.Contains("Unknown", legendData);
            Assert.Contains("Male", legendData);
            Assert.Contains("Female", legendData);
        }

        [Fact]
        public void GeneratePieChart_WithEmptyData_ReturnsDefaultConfig()
        {
            // 准备
            var emptyData = new List<object>();
            var options = new Dictionary<string, object>
            {
                ["categoryField"] = "Gender",
                ["valueField"] = "Count"
            };

            // 执行
            var result = _provider.GenerateChartConfig("pie", emptyData, options);

            // 断言
            var chartConfig = Assert.IsType<Dictionary<string, object>>(result);
            
            // 验证系列数据
            Assert.True(chartConfig.ContainsKey("series"));
            var series = Assert.IsType<object[]>(chartConfig["series"]);
            Assert.Single(series);
            
            var seriesItem = Assert.IsType<Dictionary<string, object>>(series[0]);
            Assert.Equal("pie", seriesItem["type"]);
            
            var seriesData = Assert.IsType<object[]>(seriesItem["data"]);
            // 应该有一个默认数据项，通常是"暂无数据"
            Assert.Single(seriesData);
            
            var defaultItem = Assert.IsType<Dictionary<string, object>>(seriesData[0]);
            Assert.Equal("暂无数据", defaultItem["name"]);
        }
        
        [Fact]
        public void GeneratePieChart_WithAnonymousTypeData_ProcessesDataCorrectly()
        {
            // 准备：使用匿名类型创建数据
            var data = new 
            { 
                Items = new[]
                {
                    new { Category = "类别1", Value = 10 },
                    new { Category = "类别2", Value = 20 },
                    new { Category = "类别3", Value = 30 }
                }
            };
            
            var options = new Dictionary<string, object>
            {
                ["categoryField"] = "Category",
                ["valueField"] = "Value"
            };

            // 执行
            var result = _provider.GenerateChartConfig("pie", data, options);

            // 断言
            var chartConfig = Assert.IsType<Dictionary<string, object>>(result);
            
            // 验证系列数据
            Assert.True(chartConfig.ContainsKey("series"));
            var series = Assert.IsType<object[]>(chartConfig["series"]);
            Assert.Single(series);
            
            var seriesItem = Assert.IsType<Dictionary<string, object>>(series[0]);
            Assert.Equal("pie", seriesItem["type"]);
            
            var seriesData = Assert.IsType<object[]>(seriesItem["data"]);
            Assert.Equal(3, seriesData.Length);
            
            // 验证数据项
            var firstItem = Assert.IsType<Dictionary<string, object>>(seriesData[0]);
            Assert.Equal("类别1", firstItem["name"]);
            Assert.Equal(10, firstItem["value"]);
            
            var secondItem = Assert.IsType<Dictionary<string, object>>(seriesData[1]);
            Assert.Equal("类别2", secondItem["name"]);
            Assert.Equal(20, secondItem["value"]);
            
            var thirdItem = Assert.IsType<Dictionary<string, object>>(seriesData[2]);
            Assert.Equal("类别3", thirdItem["name"]);
            Assert.Equal(30, thirdItem["value"]);
            
            // 验证图例数据
            var legend = Assert.IsType<Dictionary<string, object>>(chartConfig["legend"]);
            var legendData = Assert.IsType<object[]>(legend["data"]);
            Assert.Equal(3, legendData.Length);
            Assert.Contains("类别1", legendData);
            Assert.Contains("类别2", legendData);
            Assert.Contains("类别3", legendData);
        }
    }
} 