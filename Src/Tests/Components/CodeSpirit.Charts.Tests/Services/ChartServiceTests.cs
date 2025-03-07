using CodeSpirit.Charts.Analysis;
using CodeSpirit.Charts.Attributes;
using CodeSpirit.Charts.Models;
using CodeSpirit.Charts.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json.Linq;
using System.Reflection;
using Xunit;

namespace CodeSpirit.Charts.Tests.Services
{
    public class ChartServiceTests
    {
        private readonly ChartService _chartService;
        private readonly Mock<IChartRecommender> _chartRecommenderMock;
        private readonly Mock<IDataAnalyzer> _dataAnalyzerMock;
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
        private readonly Mock<ILogger<ChartService>> _loggerMock;

        public ChartServiceTests()
        {
            _chartRecommenderMock = new Mock<IChartRecommender>();
            _dataAnalyzerMock = new Mock<IDataAnalyzer>();
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _loggerMock = new Mock<ILogger<ChartService>>();

            _chartService = new ChartService(
                _chartRecommenderMock.Object,
                _dataAnalyzerMock.Object,
                _httpClientFactoryMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task GenerateChartConfigAsync_WithChartAttribute_ReturnsConfig()
        {
            // 准备测试数据
            var methodInfo = GetType().GetMethod("TestMethod", BindingFlags.Instance | BindingFlags.NonPublic);

            // 执行测试
            var result = await _chartService.GenerateChartConfigAsync(methodInfo);

            // 断言
            Assert.NotNull(result);
            Assert.Equal("测试图表", result.Title);
            Assert.Equal(ChartType.Line, result.Type);
            Assert.True(result.AutoRefresh);
            Assert.Equal(30, result.RefreshInterval);
            Assert.NotNull(result.Toolbox);
        }

        [Fact]
        public async Task RecommendChartTypeAsync_CallsRecommender()
        {
            // 准备测试数据
            var data = new[] { new { x = 1, y = 2 } };
            _chartRecommenderMock.Setup(x => x.RecommendChartType(It.IsAny<object>()))
                .Returns(ChartType.Bar);

            // 执行测试
            var result = await _chartService.RecommendChartTypeAsync(data);

            // 断言
            Assert.Equal(ChartType.Bar, result);
            _chartRecommenderMock.Verify(x => x.RecommendChartType(It.IsAny<object>()), Times.Once);
        }

        [Fact]
        public async Task AnalyzeAndGenerateChartAsync_CallsRecommenderAndGenerates()
        {
            // 准备测试数据
            var data = new[] { new { x = 1, y = 2 } };
            var expectedConfig = new ChartConfig { Title = "Generated Chart" };
            
            _chartRecommenderMock.Setup(x => x.GenerateChartConfig(It.IsAny<object>(), null))
                .Returns(expectedConfig);

            // 执行测试
            var result = await _chartService.AnalyzeAndGenerateChartAsync(data);

            // 断言
            Assert.Same(expectedConfig, result);
            _chartRecommenderMock.Verify(x => x.GenerateChartConfig(It.IsAny<object>(), null), Times.Once);
        }

        [Fact]
        public async Task SaveAndGetChartConfigAsync_WorksCorrectly()
        {
            // 准备测试数据
            var config = new ChartConfig
            {
                Title = "Test Chart",
                Type = ChartType.Pie
            };

            // 保存配置
            var id = await _chartService.SaveChartConfigAsync(config);

            // 断言保存结果
            Assert.NotNull(id);
            Assert.Equal(config.Id, id);

            // 获取配置
            var retrievedConfig = await _chartService.GetChartConfigAsync(id);

            // 断言检索结果
            Assert.NotNull(retrievedConfig);
            Assert.Equal(config.Title, retrievedConfig.Title);
            Assert.Equal(config.Type, retrievedConfig.Type);
        }

        [Fact]
        public async Task GenerateChartJsonAsync_CreatesValidJson()
        {
            // 准备测试数据
            var config = new ChartConfig
            {
                Title = "Test Chart",
                Type = ChartType.Line,
                XAxis = new AxisConfig { Type = "category" },
                YAxis = new AxisConfig { Type = "value" },
                Series = new List<SeriesConfig>
                {
                    new SeriesConfig { Type = "line", Name = "Series 1" }
                }
            };

            // 执行测试
            var result = await _chartService.GenerateChartJsonAsync(config);

            // 断言
            Assert.NotNull(result);
            Assert.Equal("Test Chart", result["title"]["text"].ToString());
            Assert.NotNull(result["xAxis"]);
            Assert.NotNull(result["yAxis"]);
            Assert.NotNull(result["series"]);
            Assert.Equal(1, result["series"].Count());
        }

        [Fact]
        public async Task GetRecommendedChartTypesAsync_ReturnsMultipleTypes()
        {
            // 准备测试数据
            var data = new[] { new { x = 1, y = 2 } };
            var expectedRecommendations = new Dictionary<ChartType, double>
            {
                { ChartType.Bar, 0.8 },
                { ChartType.Pie, 0.6 },
                { ChartType.Line, 0.4 }
            };
            
            _chartRecommenderMock.Setup(x => x.RecommendChartTypes(It.IsAny<object>(), It.IsAny<int>()))
                .Returns(expectedRecommendations);

            // 执行测试
            var result = await _chartService.GetRecommendedChartTypesAsync(data);

            // 断言
            Assert.NotNull(result);
            Assert.Equal(expectedRecommendations.Count, result.Count);
            Assert.Equal(expectedRecommendations[ChartType.Bar], result[ChartType.Bar]);
            Assert.Equal(expectedRecommendations[ChartType.Pie], result[ChartType.Pie]);
            Assert.Equal(expectedRecommendations[ChartType.Line], result[ChartType.Line]);
        }

        [Fact]
        public async Task GetChartConfigAsync_WithInvalidId_ThrowsKeyNotFoundException()
        {
            // 执行测试和断言
            await Assert.ThrowsAsync<KeyNotFoundException>(async () => 
                await _chartService.GetChartConfigAsync("non-existent-id"));
        }

        [Chart("测试图表", "这是一个测试图表", AutoRefresh = true, RefreshInterval = 30)]
        [ChartType(ChartType.Line)]
        [ChartData(DimensionField = "month", MetricFields = new[] { "sales" })]
        private void TestMethod() 
        {
            // 测试方法
        }
    }
} 