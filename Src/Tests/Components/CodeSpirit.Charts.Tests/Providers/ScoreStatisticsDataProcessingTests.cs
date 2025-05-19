using CodeSpirit.Charts.Core.Abstractions;
using CodeSpirit.Charts.Core.Services;
using CodeSpirit.Charts.Providers.ECharts;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;
using Xunit;

namespace CodeSpirit.Charts.Tests.Providers
{
    /// <summary>
    /// 成绩统计数据处理测试
    /// </summary>
    public class ScoreStatisticsDataProcessingTests
    {
        private readonly Mock<ILogger<EChartsProvider>> _loggerMock;
        private readonly Mock<ILogger<DataProcessor>> _dataProcessorLoggerMock;
        private readonly EChartsProvider _provider;
        private readonly DataProcessor _dataProcessor;

        public ScoreStatisticsDataProcessingTests()
        {
            _loggerMock = new Mock<ILogger<EChartsProvider>>();
            _dataProcessorLoggerMock = new Mock<ILogger<DataProcessor>>();
            _provider = new EChartsProvider(_loggerMock.Object);
            _dataProcessor = new DataProcessor(_dataProcessorLoggerMock.Object);
        }

        [Fact]
        public void ValidateLineChartData_WithCategoryValueFormattedData_ReturnsTrue()
        {
            // 准备：模拟统计数据
            var statisticsData = new List<object>
            {
                new { Category = "考试人数", Value = 100 },
                new { Category = "平均分", Value = 76.5 },
                new { Category = "最高分", Value = 98 },
                new { Category = "最低分", Value = 45 },
                new { Category = "及格人数", Value = 80 },
                new { Category = "及格率(%)", Value = 80.0 }
            };

            // 执行：验证数据格式是否符合折线图要求
            var result = _dataProcessor.ValidateDataForChartTypeAsync(statisticsData, "line").Result;

            // 断言：数据验证应该通过
            Assert.True(result.IsValid);
            Assert.Null(result.ErrorMessage);
        }

        [Fact]
        public void GenerateLineChart_WithScoreStatisticsData_ProcessesDataCorrectly()
        {
            // 准备：模拟成绩统计数据
            var statisticsData = new List<object>
            {
                new { Category = "考试人数", Value = 100 },
                new { Category = "平均分", Value = 76.5 },
                new { Category = "最高分", Value = 98 },
                new { Category = "最低分", Value = 45 },
                new { Category = "及格人数", Value = 80 },
                new { Category = "及格率(%)", Value = 80.0 }
            };

            var options = new Dictionary<string, object>
            {
                ["title"] = "考试成绩统计",
                ["xField"] = "Category",
                ["yField"] = "Value"
            };

            // 执行
            var result = _provider.GenerateChartConfig("line", statisticsData, options);

            // 断言
            var chartConfig = Assert.IsType<Dictionary<string, object>>(result);
            
            // 验证标题
            Assert.True(chartConfig.ContainsKey("title"));
            var title = Assert.IsType<Dictionary<string, object>>(chartConfig["title"]);
            Assert.Equal("考试成绩统计", title["text"]);
            
            // 验证X轴数据
            Assert.True(chartConfig.ContainsKey("xAxis"));
            var xAxis = Assert.IsType<Dictionary<string, object>>(chartConfig["xAxis"]);
            Assert.Equal("category", xAxis["type"]);
            
            var xAxisData = Assert.IsType<object[]>(xAxis["data"]);
            Assert.Equal(6, xAxisData.Length);
            Assert.Contains("考试人数", xAxisData);
            Assert.Contains("平均分", xAxisData);
            Assert.Contains("最高分", xAxisData);
            Assert.Contains("最低分", xAxisData);
            Assert.Contains("及格人数", xAxisData);
            Assert.Contains("及格率(%)", xAxisData);
            
            // 验证Y轴类型
            Assert.True(chartConfig.ContainsKey("yAxis"));
            var yAxis = Assert.IsType<Dictionary<string, object>>(chartConfig["yAxis"]);
            Assert.Equal("value", yAxis["type"]);
            
            // 验证系列数据
            Assert.True(chartConfig.ContainsKey("series"));
            var series = Assert.IsType<object[]>(chartConfig["series"]);
            Assert.Single(series);
            
            var seriesItem = Assert.IsType<Dictionary<string, object>>(series[0]);
            Assert.Equal("line", seriesItem["type"]);
            
            var seriesData = Assert.IsType<object[]>(seriesItem["data"]);
            Assert.Equal(6, seriesData.Length);
            Assert.Contains(100.0, seriesData);
            Assert.Contains(76.5, seriesData);
            Assert.Contains(98.0, seriesData);
            Assert.Contains(45.0, seriesData);
            Assert.Contains(80.0, seriesData);
            Assert.Contains(80.0, seriesData); // 及格率
        }
    }
} 