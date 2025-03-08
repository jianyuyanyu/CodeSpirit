using CodeSpirit.Charts.Analysis;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Xunit;
using Newtonsoft.Json.Linq;

namespace CodeSpirit.Charts.Tests.Analysis
{
    public class DataAnalyzerTests
    {
        private readonly DataAnalyzer _dataAnalyzer;
        private readonly Mock<ILogger<DataAnalyzer>> _loggerMock;

        public DataAnalyzerTests()
        {
            _loggerMock = new Mock<ILogger<DataAnalyzer>>();
            _dataAnalyzer = new DataAnalyzer(_loggerMock.Object);
        }

        [Fact]
        public void AnalyzeDataStructure_WithArrayData_ReturnsCorrectStructure()
        {
            // 准备测试数据
            var testData = new[]
            {
                new { month = "2023-01", sales = 1000, profit = 300 },
                new { month = "2023-02", sales = 1200, profit = 350 },
                new { month = "2023-03", sales = 900, profit = 270 }
            };

            // 执行测试
            var result = _dataAnalyzer.AnalyzeDataStructure(testData);

            // 断言
            Assert.NotNull(result);
            Assert.Equal(3, result.RowCount);
            Assert.Contains("month", result.DimensionFields);
            Assert.Contains("sales", result.MetricFields);
            Assert.Contains("profit", result.MetricFields);
        }

        [Fact]
        public void ExtractDataFeatures_WithTimeSeriesData_DetectsTimeSeries()
        {
            // 准备测试数据
            var testData = new[]
            {
                new { date = "2023-01-01", value = 100 },
                new { date = "2023-01-02", value = 120 },
                new { date = "2023-01-03", value = 110 },
                new { date = "2023-01-04", value = 130 },
                new { date = "2023-01-05", value = 150 }
            };

            // 执行测试
            var result = _dataAnalyzer.ExtractDataFeatures(testData);

            // 断言
            Assert.NotNull(result);
            Assert.True(result.IsTimeSeries);
            Assert.True(result.HasTrend);
            Assert.True(result.MetricStatistics.ContainsKey("value"));
        }

        [Fact]
        public void DetectCorrelations_WithCorrelatedData_ReturnsCorrelations()
        {
            // 准备测试数据
            var testData = new[]
            {
                new { x = 10, y = 20, z = 5 },
                new { x = 20, y = 40, z = 4 },
                new { x = 30, y = 60, z = 3 },
                new { x = 40, y = 80, z = 2 },
                new { x = 50, y = 100, z = 1 }
            };

            // 执行测试
            var result = _dataAnalyzer.DetectCorrelations(testData);

            // 断言
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            // 检查x和y应该有强相关性
            var xyCorrelation = result.FirstOrDefault(c => 
                (c.Field1 == "x" && c.Field2 == "y") || 
                (c.Field1 == "y" && c.Field2 == "x"));
            Assert.NotNull(xyCorrelation);
            Assert.True(Math.Abs(xyCorrelation.Coefficient) > 0.9);
        }

        [Fact]
        public void IdentifyPatterns_WithTrendData_ReturnsTrendPattern()
        {
            // 准备测试数据
            var testData = new[]
            {
                new { month = "2023-01", value = 100 },
                new { month = "2023-02", value = 110 },
                new { month = "2023-03", value = 120 },
                new { month = "2023-04", value = 130 },
                new { month = "2023-05", value = 140 }
            };

            // 执行测试
            var result = _dataAnalyzer.IdentifyPatterns(testData);

            // 断言
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            var trendPattern = result.FirstOrDefault(p => p.Type == "TimeTrend");
            Assert.NotNull(trendPattern);
            Assert.True(trendPattern.Confidence > 0.7);
        }

        [Fact]
        public void ExtractNumericValues_ReturnsCorrectValues()
        {
            // 准备测试数据
            var json = @"[
                { ""name"": ""Product A"", ""value"": 100 },
                { ""name"": ""Product B"", ""value"": 200 },
                { ""name"": ""Product C"", ""value"": 300 }
            ]";
            var jsonArray = JArray.Parse(json);

            // 使用反射调用私有方法
            var methodInfo = typeof(DataAnalyzer).GetMethod("ExtractNumericValues", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = methodInfo.Invoke(_dataAnalyzer, new object[] { jsonArray, "value" }) as List<double>;

            // 断言
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            Assert.Equal(100, result[0]);
            Assert.Equal(200, result[1]);
            Assert.Equal(300, result[2]);
        }

        [Fact]
        public void AnalyzeDataStructure_WithUserGrowthData_ReturnsCorrectStructure()
        {
            // 准备测试数据 - 用户增长统计数据
            var dailyGrowth = new[]
            {
                new { Date = new DateTime(2025, 3, 4), UserCount = 119 },
                new { Date = new DateTime(2025, 3, 5), UserCount = 367 }
            };

            // 执行测试
            var result = _dataAnalyzer.AnalyzeDataStructure(dailyGrowth);

            // 断言
            Assert.NotNull(result);
            Assert.Equal(2, result.RowCount);
            Assert.Contains("Date", result.DimensionFields);
            Assert.Contains("UserCount", result.MetricFields);
            Assert.Equal(typeof(DateTime), result.FieldTypes["Date"]);
            Assert.Equal(typeof(long), result.FieldTypes["UserCount"]);
        }

        [Fact]
        public void ExtractDataFeatures_WithUserGrowthData_DetectsGrowthTrend()
        {
            // 准备测试数据 - 模拟连续几天的用户增长
            var dailyGrowth = new[]
            {
                new { Date = new DateTime(2025, 3, 1), UserCount = 100 },
                new { Date = new DateTime(2025, 3, 2), UserCount = 150 },
                new { Date = new DateTime(2025, 3, 3), UserCount = 210 },
                new { Date = new DateTime(2025, 3, 4), UserCount = 280 },
                new { Date = new DateTime(2025, 3, 5), UserCount = 367 }
            };

            // 执行测试
            var result = _dataAnalyzer.ExtractDataFeatures(dailyGrowth);

            // 断言
            Assert.NotNull(result);
            Assert.True(result.IsTimeSeries);  // 应该检测到这是时间序列数据
            Assert.True(result.HasTrend);      // 应该检测到上升趋势
            Assert.True(result.MetricStatistics.ContainsKey("UserCount"));
            
            var stats = result.MetricStatistics["UserCount"];
            Assert.Equal(100, stats.Min);
            Assert.Equal(367, stats.Max);
            Assert.True(stats.Average > 200);  // 平均值应该在这个范围
        }

        [Fact]
        public void DetectCorrelations_WithActiveUsersAndGrowth_DetectsCorrelation()
        {
            // 准备测试数据 - 用户增长和活跃用户数
            var userData = new[]
            {
                new { Date = new DateTime(2025, 3, 1), UserCount = 100, ActiveUserCount = 80 },
                new { Date = new DateTime(2025, 3, 2), UserCount = 150, ActiveUserCount = 100 },
                new { Date = new DateTime(2025, 3, 3), UserCount = 210, ActiveUserCount = 130 },
                new { Date = new DateTime(2025, 3, 4), UserCount = 280, ActiveUserCount = 190 },
                new { Date = new DateTime(2025, 3, 5), UserCount = 367, ActiveUserCount = 240 }
            };

            // 执行测试
            var result = _dataAnalyzer.DetectCorrelations(userData);

            // 断言
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            
            // 查找 UserCount 和 ActiveUserCount 之间的相关性
            var correlation = result.FirstOrDefault(c => 
                (c.Field1 == "UserCount" && c.Field2 == "ActiveUserCount") ||
                (c.Field1 == "ActiveUserCount" && c.Field2 == "UserCount"));
            
            Assert.NotNull(correlation);
            Assert.True(correlation.Coefficient > 0.8); // 应该有很强的正相关性
            Assert.Contains("强", correlation.Strength); // 中文描述应该包含"强"字
        }

        [Fact]
        public void IdentifyPatterns_WithUserGrowthData_DetectsGrowthPattern()
        {
            // 准备测试数据 - 用户增长数据，呈指数增长趋势
            var dailyGrowth = new[]
            {
                new { Date = new DateTime(2025, 3, 1), UserCount = 100 },
                new { Date = new DateTime(2025, 3, 2), UserCount = 150 },
                new { Date = new DateTime(2025, 3, 3), UserCount = 210 },
                new { Date = new DateTime(2025, 3, 4), UserCount = 280 },
                new { Date = new DateTime(2025, 3, 5), UserCount = 367 }
            };

            // 执行测试
            var result = _dataAnalyzer.IdentifyPatterns(dailyGrowth);

            // 断言
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            
            // 应该至少有一个趋势模式
            var trendPattern = result.FirstOrDefault(p => p.Type.Contains("趋势") || p.Type.Contains("Trend"));
            Assert.NotNull(trendPattern);
            Assert.Contains("UserCount", trendPattern.RelatedFields);
            Assert.True(trendPattern.Confidence > 0.7); // 应该有很高的可信度
        }
    }
} 