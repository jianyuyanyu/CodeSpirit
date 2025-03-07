using CodeSpirit.Charts.Analysis;
using CodeSpirit.Charts.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CodeSpirit.Charts.Tests.Analysis
{
    public class ChartRecommenderTests
    {
        private readonly ChartRecommender _chartRecommender;
        private readonly Mock<IDataAnalyzer> _dataAnalyzerMock;
        private readonly Mock<ILogger<ChartRecommender>> _loggerMock;

        public ChartRecommenderTests()
        {
            _dataAnalyzerMock = new Mock<IDataAnalyzer>();
            _loggerMock = new Mock<ILogger<ChartRecommender>>();
            _chartRecommender = new ChartRecommender(_dataAnalyzerMock.Object, _loggerMock.Object);
        }

        [Fact]
        public void RecommendChartType_WithTimeSeriesData_ReturnsLineChart()
        {
            // 准备测试数据
            var testData = new[]
            {
                new { date = "2023-01-01", value = 100 },
                new { date = "2023-01-02", value = 120 }
            };

            // 设置模拟数据分析结果
            var structureInfo = new DataStructureInfo
            {
                RowCount = 2,
                DimensionFields = new List<string> { "date" },
                MetricFields = new List<string> { "value" }
            };

            var features = new DataFeatures
            {
                IsTimeSeries = true,
                HasTrend = true,
                IsContinuous = true
            };

            _dataAnalyzerMock.Setup(x => x.AnalyzeDataStructure(It.IsAny<object>()))
                .Returns(structureInfo);
            
            _dataAnalyzerMock.Setup(x => x.ExtractDataFeatures(It.IsAny<object>()))
                .Returns(features);

            // 执行测试
            var result = _chartRecommender.RecommendChartType(testData);

            // 断言
            Assert.Equal(ChartType.Line, result);
        }

        [Fact]
        public void RecommendChartType_WithCategoricalSingleMetric_ReturnsPieChart()
        {
            // 准备测试数据
            var testData = new[]
            {
                new { category = "A", value = 100 },
                new { category = "B", value = 200 },
                new { category = "C", value = 300 }
            };

            // 设置模拟数据分析结果
            var structureInfo = new DataStructureInfo
            {
                RowCount = 3,
                DimensionFields = new List<string> { "category" },
                MetricFields = new List<string> { "value" }
            };

            var features = new DataFeatures
            {
                IsTimeSeries = false,
                HasTrend = false,
                IsCategorical = true,
                IsContinuous = true
            };

            _dataAnalyzerMock.Setup(x => x.AnalyzeDataStructure(It.IsAny<object>()))
                .Returns(structureInfo);
            
            _dataAnalyzerMock.Setup(x => x.ExtractDataFeatures(It.IsAny<object>()))
                .Returns(features);

            // 执行测试
            var result = _chartRecommender.RecommendChartType(testData);

            // 断言
            Assert.Equal(ChartType.Pie, result);
        }

        [Fact]
        public void RecommendChartType_WithMultipleMetrics_ReturnsBarChart()
        {
            // 准备测试数据
            var testData = new[]
            {
                new { category = "A", value1 = 100, value2 = 50 },
                new { category = "B", value1 = 200, value2 = 100 },
                new { category = "C", value1 = 300, value2 = 150 }
            };

            // 设置模拟数据分析结果
            var structureInfo = new DataStructureInfo
            {
                RowCount = 3,
                DimensionFields = new List<string> { "category" },
                MetricFields = new List<string> { "value1", "value2" }
            };

            var features = new DataFeatures
            {
                IsTimeSeries = false,
                HasTrend = false,
                IsCategorical = true,
                IsContinuous = true
            };

            _dataAnalyzerMock.Setup(x => x.AnalyzeDataStructure(It.IsAny<object>()))
                .Returns(structureInfo);
            
            _dataAnalyzerMock.Setup(x => x.ExtractDataFeatures(It.IsAny<object>()))
                .Returns(features);

            // 执行测试
            var result = _chartRecommender.RecommendChartType(testData);

            // 断言
            Assert.Equal(ChartType.Bar, result);
        }

        [Fact]
        public void GenerateChartConfig_CreatesCorrectConfig()
        {
            // 准备测试数据
            var testData = new[]
            {
                new { month = "2023-01", sales = 1000 },
                new { month = "2023-02", sales = 1200 }
            };

            // 设置模拟数据分析结果
            var structureInfo = new DataStructureInfo
            {
                RowCount = 2,
                DimensionFields = new List<string> { "month" },
                MetricFields = new List<string> { "sales" }
            };

            var features = new DataFeatures
            {
                IsTimeSeries = true,
                HasTrend = true,
                IsContinuous = true
            };

            _dataAnalyzerMock.Setup(x => x.AnalyzeDataStructure(It.IsAny<object>()))
                .Returns(structureInfo);
            
            _dataAnalyzerMock.Setup(x => x.ExtractDataFeatures(It.IsAny<object>()))
                .Returns(features);

            // 执行测试
            var result = _chartRecommender.GenerateChartConfig(testData, ChartType.Line);

            // 断言
            Assert.NotNull(result);
            Assert.Equal(ChartType.Line, result.Type);
            Assert.NotNull(result.DataSource);
            Assert.Equal(DataSourceType.Current, result.DataSource.Type);
            Assert.Same(testData, result.DataSource.StaticData);
            Assert.NotNull(result.XAxis);
            Assert.NotNull(result.YAxis);
            Assert.NotEmpty(result.Series);
        }

        [Fact]
        public void RecommendChartTypes_ReturnsMultipleTypes()
        {
            // 准备测试数据
            var testData = new[]
            {
                new { category = "A", value = 100 },
                new { category = "B", value = 200 }
            };

            // 设置模拟数据分析结果
            var structureInfo = new DataStructureInfo
            {
                RowCount = 2,
                DimensionFields = new List<string> { "category" },
                MetricFields = new List<string> { "value" }
            };

            var features = new DataFeatures
            {
                IsTimeSeries = false,
                HasTrend = false,
                IsCategorical = true,
                IsContinuous = true
            };

            _dataAnalyzerMock.Setup(x => x.AnalyzeDataStructure(It.IsAny<object>()))
                .Returns(structureInfo);
            
            _dataAnalyzerMock.Setup(x => x.ExtractDataFeatures(It.IsAny<object>()))
                .Returns(features);

            // 执行测试
            var result = _chartRecommender.RecommendChartTypes(testData, 3);

            // 断言
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            // 检查评分是否合理
            foreach (var score in result.Values)
            {
                Assert.InRange(score, 0, 1);
            }
        }

        [Fact]
        public void OptimizeChartConfig_EnhancesConfiguration()
        {
            // 准备测试数据
            var config = new ChartConfig
            {
                Type = ChartType.Line,
                Title = "Test Chart",
                XAxis = new AxisConfig { Type = "category" },
                YAxis = new AxisConfig { Type = "value" },
                Series = new List<SeriesConfig>
                {
                    new SeriesConfig { Type = "line", Name = "Sales" }
                }
            };

            var structureInfo = new DataStructureInfo
            {
                DimensionFields = new List<string> { "month" },
                MetricFields = new List<string> { "sales" }
            };

            var features = new DataFeatures
            {
                IsTimeSeries = true,
                HasTrend = true
            };

            _dataAnalyzerMock.Setup(x => x.AnalyzeDataStructure(It.IsAny<object>()))
                .Returns(structureInfo);
            
            _dataAnalyzerMock.Setup(x => x.ExtractDataFeatures(It.IsAny<object>()))
                .Returns(features);

            // 执行测试
            var result = _chartRecommender.OptimizeChartConfig(config, new object());

            // 断言
            Assert.NotNull(result);
            Assert.Equal(ChartType.Line, result.Type);
            Assert.Equal("Test Chart", result.Title);
            Assert.NotNull(result.Interaction);
        }
    }
} 