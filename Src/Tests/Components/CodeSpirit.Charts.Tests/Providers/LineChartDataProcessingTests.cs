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
    /// 折线图数据处理测试
    /// </summary>
    public class LineChartDataProcessingTests
    {
        private readonly Mock<ILogger<EChartsProvider>> _loggerMock;
        private readonly EChartsProvider _provider;

        public LineChartDataProcessingTests()
        {
            _loggerMock = new Mock<ILogger<EChartsProvider>>();
            _provider = new EChartsProvider(_loggerMock.Object);
        }

        [Fact]
        public void GenerateLineChart_WithInactiveUserStatisticsData_ProcessesDataCorrectly()
        {
            // 准备：模拟未登录用户统计数据
            var inactiveUsers = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object> { ["InactiveDays"] = "30天以上", ["UserCount"] = 6 },
                new Dictionary<string, object> { ["InactiveDays"] = "60天以上", ["UserCount"] = 3 },
                new Dictionary<string, object> { ["InactiveDays"] = "90天以上", ["UserCount"] = 3 },
                new Dictionary<string, object> { ["InactiveDays"] = "120天以上", ["UserCount"] = 3 },
                new Dictionary<string, object> { ["InactiveDays"] = "150天以上", ["UserCount"] = 3 }
            };

            var options = new Dictionary<string, object>
            {
                ["title"] = "长期未登录用户",
                ["xField"] = "InactiveDays",
                ["yField"] = "UserCount"
            };

            // 执行
            var result = _provider.GenerateChartConfig("line", inactiveUsers, options);

            // 断言
            var chartConfig = Assert.IsType<Dictionary<string, object>>(result);
            
            // 验证基本结构
            Assert.True(chartConfig.ContainsKey("xAxis"));
            Assert.True(chartConfig.ContainsKey("yAxis"));
            Assert.True(chartConfig.ContainsKey("series"));
            
            // 验证 X 轴数据
            var xAxis = Assert.IsType<Dictionary<string, object>>(chartConfig["xAxis"]);
            Assert.Equal("category", xAxis["type"]);
            
            var xAxisData = Assert.IsType<object[]>(xAxis["data"]);
            Assert.Equal(5, xAxisData.Length);
            Assert.Contains("30天以上", xAxisData);
            Assert.Contains("60天以上", xAxisData);
            Assert.Contains("90天以上", xAxisData);
            Assert.Contains("120天以上", xAxisData);
            Assert.Contains("150天以上", xAxisData);
            
            // 验证 Y 轴类型
            var yAxis = Assert.IsType<Dictionary<string, object>>(chartConfig["yAxis"]);
            Assert.Equal("value", yAxis["type"]);
            
            // 验证系列数据
            var series = Assert.IsAssignableFrom<object[]>(chartConfig["series"]);
            Assert.Single(series);
            
            var seriesItem = Assert.IsType<Dictionary<string, object>>(series[0]);
            Assert.Equal("line", seriesItem["type"]);
            
            var seriesData = Assert.IsType<object[]>(seriesItem["data"]);
            Assert.Equal(5, seriesData.Length);
            Assert.Contains(6, seriesData);
            Assert.Contains(3, seriesData);
        }

        [Fact]
        public void GenerateLineChart_WithJsonStringData_ProcessesDataCorrectly()
        {
            // 准备：模拟JSON字符串格式的数据
            string jsonData = "[{\"InactiveDays\":\"30天以上\",\"UserCount\":6},{\"InactiveDays\":\"60天以上\",\"UserCount\":3},{\"InactiveDays\":\"90天以上\",\"UserCount\":3},{\"InactiveDays\":\"120天以上\",\"UserCount\":3},{\"InactiveDays\":\"150天以上\",\"UserCount\":3}]";
            
            var options = new Dictionary<string, object>
            {
                ["xField"] = "InactiveDays",
                ["yField"] = "UserCount"
            };

            // 执行
            var result = _provider.GenerateChartConfig("line", jsonData, options);

            // 断言
            var chartConfig = Assert.IsType<Dictionary<string, object>>(result);
            
            // 验证X轴和系列数据
            var xAxis = Assert.IsType<Dictionary<string, object>>(chartConfig["xAxis"]);
            var xAxisData = Assert.IsType<object[]>(xAxis["data"]);
            Assert.Equal(5, xAxisData.Length);
            
            var series = Assert.IsAssignableFrom<object[]>(chartConfig["series"]);
            var seriesItem = Assert.IsType<Dictionary<string, object>>(series[0]);
            var seriesData = Assert.IsType<object[]>(seriesItem["data"]);
            Assert.Equal(5, seriesData.Length);
        }

        [Fact]
        public void GenerateLineChart_WithEmptyData_ReturnsEmptyConfig()
        {
            // 准备
            var emptyData = new List<object>();
            var options = new Dictionary<string, object>
            {
                ["xField"] = "InactiveDays",
                ["yField"] = "UserCount"
            };

            // 执行
            var result = _provider.GenerateChartConfig("line", emptyData, options);

            // 断言
            var chartConfig = Assert.IsType<Dictionary<string, object>>(result);
            
            // 验证X轴和系列数据
            var xAxis = Assert.IsType<Dictionary<string, object>>(chartConfig["xAxis"]);
            var xAxisData = Assert.IsType<object[]>(xAxis["data"]);
            Assert.Empty(xAxisData);
            
            var series = Assert.IsAssignableFrom<object[]>(chartConfig["series"]);
            Assert.Single(series);
            var seriesItem = Assert.IsType<Dictionary<string, object>>(series[0]);
            Assert.Equal("line", seriesItem["type"]);
            var seriesData = Assert.IsType<object[]>(seriesItem["data"]);
            Assert.Empty(seriesData);
        }
    }
} 