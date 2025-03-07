using CodeSpirit.Charts.Analysis;
using CodeSpirit.Charts.Attributes;
using CodeSpirit.Charts.Models;
using CodeSpirit.Charts.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Reflection;
using Xunit;

namespace CodeSpirit.Charts.Tests.Services
{
    public class ChartServiceTests : IDisposable
    {
        private readonly ChartService _chartService;
        private readonly IChartRecommender _chartRecommender;
        private readonly IDataAnalyzer _dataAnalyzer;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ChartService> _logger;
        private readonly ServiceProvider _serviceProvider;

        public ChartServiceTests()
        {
            // 使用真实的ServiceCollection配置依赖项
            var services = new ServiceCollection();
            
            // 配置真实的依赖项
            _dataAnalyzer = new DataAnalyzer(NullLogger<DataAnalyzer>.Instance);
            _chartRecommender = new ChartRecommender(_dataAnalyzer, NullLogger<ChartRecommender>.Instance);
            _logger = NullLogger<ChartService>.Instance;
            
            // 注册服务
            services.AddSingleton<IDataAnalyzer>(_dataAnalyzer);
            services.AddSingleton<IChartRecommender>(_chartRecommender);
            services.AddSingleton<IEChartConfigGenerator>(provider => 
                new EChartConfigGenerator(_dataAnalyzer, NullLogger<EChartConfigGenerator>.Instance));
            
            // 添加真实的HttpClient服务
            services.AddHttpClient();
            
            // 创建服务提供者
            _serviceProvider = services.BuildServiceProvider();
            
            // 获取HttpClientFactory（真实实现）
            _httpClientFactory = _serviceProvider.GetRequiredService<IHttpClientFactory>();
            
            // 创建ChartService的实例
            _chartService = new ChartService(
                _chartRecommender,
                _dataAnalyzer,
                _httpClientFactory,
                _logger);
        }

        public void Dispose()
        {
            // 清理资源
            _serviceProvider?.Dispose();
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
        public async Task RecommendChartTypeAsync_WithTimeSeriesData_ReturnsLineChart()
        {
            // 准备时间序列测试数据
            var data = new[]
            {
                new { date = "2023-01-01", value = 100 },
                new { date = "2023-01-02", value = 120 },
                new { date = "2023-01-03", value = 140 },
                new { date = "2023-01-04", value = 130 },
                new { date = "2023-01-05", value = 150 }
            };

            // 执行测试
            var result = await _chartService.RecommendChartTypeAsync(data);

            // 断言 - 由于使用真实实现，时间序列数据应该推荐折线图
            Assert.Equal(ChartType.Line, result);
        }

        [Fact]
        public async Task RecommendChartTypeAsync_WithCategoricalData_ReturnsBarOrPieChart()
        {
            // 准备分类测试数据
            var data = new[]
            {
                new { category = "A", value = 100 },
                new { category = "B", value = 200 },
                new { category = "C", value = 300 },
                new { category = "D", value = 150 }
            };

            // 执行测试
            var result = await _chartService.RecommendChartTypeAsync(data);

            // 断言 - 由于使用真实实现，分类数据应该推荐饼图或柱状图
            Assert.True(result == ChartType.Pie || result == ChartType.Bar,
                $"Expected Pie or Bar chart, but got {result}");
        }

        [Fact]
        public async Task AnalyzeAndGenerateChartAsync_WithTimeSeriesData_GeneratesLineChartConfig()
        {
            // 准备时间序列测试数据
            var data = new[]
            {
                new { date = "2023-01-01", value = 100 },
                new { date = "2023-01-02", value = 120 },
                new { date = "2023-01-03", value = 140 }
            };

            // 执行测试
            var result = await _chartService.AnalyzeAndGenerateChartAsync(data);

            // 断言
            Assert.NotNull(result);
            Assert.Equal(ChartType.Line, result.Type);
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
        public async Task GetRecommendedChartTypesAsync_WithTimeSeriesData_ReturnsMultipleTypesIncludingLine()
        {
            // 准备时间序列测试数据
            var data = new[]
            {
                new { date = "2023-01-01", value = 100 },
                new { date = "2023-01-02", value = 120 },
                new { date = "2023-01-03", value = 140 }
            };

            // 执行测试
            var result = await _chartService.GetRecommendedChartTypesAsync(data);

            // 断言
            Assert.NotNull(result);
            Assert.True(result.Count >= 2, "应该至少推荐两种图表类型");
            Assert.True(result.ContainsKey(ChartType.Line), "时间序列数据应该包含折线图推荐");
        }

        [Fact]
        public async Task GetChartConfigAsync_WithInvalidId_ThrowsKeyNotFoundException()
        {
            // 执行测试和断言
            await Assert.ThrowsAsync<KeyNotFoundException>(async () => 
                await _chartService.GetChartConfigAsync("non-existent-id"));
        }

        [Fact]
        public async Task GenerateChartJsonAsync_WithRealData_ProducesValidConfiguration()
        {
            // 准备真实测试数据
            var salesData = new[]
            {
                new { month = "1月", sales = 1000, profit = 300 },
                new { month = "2月", sales = 1200, profit = 350 },
                new { month = "3月", sales = 900, profit = 270 },
                new { month = "4月", sales = 1500, profit = 400 },
                new { month = "5月", sales = 2000, profit = 500 }
            };

            // 使用推荐器生成配置
            var config = _chartRecommender.GenerateChartConfig(salesData);
            
            // 生成JSON
            var result = await _chartService.GenerateChartJsonAsync(config);

            // 断言
            Assert.NotNull(result);
            Assert.NotNull(result["title"]);
            Assert.NotNull(result["series"]);
            
            // 验证数据处理
            Assert.NotEmpty(config.Series);
            Assert.NotNull(config.XAxis);
        }

        [Fact]
        public async Task EndToEndTest_FromDataToChart_ProducesCompleteChartConfiguration()
        {
            // 真实完整流程测试：从数据分析到图表生成

            // 1. 准备测试数据 - 保证是时间序列数据，这样推荐的类型会是Line
            var salesData = new[]
            {
                new { date = "2023-01-01", region = "北部", product = "A", sales = 1000, profit = 300 },
                new { date = "2023-01-02", region = "北部", product = "A", sales = 1100, profit = 330 },
                new { date = "2023-01-03", region = "北部", product = "A", sales = 1200, profit = 360 },
                new { date = "2023-01-04", region = "北部", product = "A", sales = 1300, profit = 390 },
                new { date = "2023-01-05", region = "北部", product = "A", sales = 1400, profit = 420 },
            };

            // 2. 数据分析
            var structureInfo = _dataAnalyzer.AnalyzeDataStructure(salesData);
            var features = _dataAnalyzer.ExtractDataFeatures(salesData);

            // 3. 验证数据分析结果
            Assert.NotNull(structureInfo);
            Assert.NotEmpty(structureInfo.DimensionFields);
            Assert.NotEmpty(structureInfo.MetricFields);
            Assert.True(features.IsTimeSeries, "应该检测到时间序列特征");

            // 4. 获取推荐的图表类型
            var recommendedTypes = await _chartService.GetRecommendedChartTypesAsync(salesData);
            Assert.NotEmpty(recommendedTypes);
            
            // 打印推荐的图表类型及评分
            foreach (var type in recommendedTypes)
            {
                Console.WriteLine($"推荐图表类型: {type.Key}, 评分: {type.Value}");
            }

            // 5. 选择最佳图表类型
            var bestType = recommendedTypes.OrderByDescending(kv => kv.Value).First().Key;
            
            // 输出实际的推荐类型，帮助调试
            Console.WriteLine($"推荐的最佳图表类型: {bestType}");
            
            // 如果是时间序列数据，最佳推荐通常应该是折线图
            Assert.Equal(ChartType.Line, bestType);
            
            // 6. 生成图表配置
            var config = await _chartService.AnalyzeAndGenerateChartAsync(salesData);
            Assert.NotNull(config);
            
            // 验证生成的图表类型与推荐的类型匹配
            Assert.Equal(bestType, config.Type);

            // 7. 保存图表配置
            var chartId = await _chartService.SaveChartConfigAsync(config);
            Assert.NotNull(chartId);

            // 8. 检索保存的配置
            var retrievedConfig = await _chartService.GetChartConfigAsync(chartId);
            Assert.Equal(config.Title, retrievedConfig.Title);
            Assert.Equal(config.Type, retrievedConfig.Type);

            // 9. 生成最终图表JSON
            var chartJson = await _chartService.GenerateChartJsonAsync(retrievedConfig);
            Assert.NotNull(chartJson);
            Assert.NotNull(chartJson["title"]);
            Assert.NotNull(chartJson["series"]);
            
            // 10. 验证图表配置的关键组件
            Assert.NotNull(chartJson["tooltip"]);
            Assert.NotNull(chartJson["toolbox"]);
        }

        [Fact]
        public async Task AnalyzeAndGenerateChartAsync_WithMethodInfo_UsesChartAttribute()
        {
            // 准备测试数据
            var testData = new[]
            {
                new { date = "2023-01-01", value = 100 },
                new { date = "2023-01-02", value = 120 },
                new { date = "2023-01-03", value = 140 }
            };
            
            var methodInfo = GetType().GetMethod("TestMethod", BindingFlags.Instance | BindingFlags.NonPublic);
            
            // 执行测试
            var result = await _chartService.AnalyzeAndGenerateChartAsync(testData, methodInfo);
            
            // 断言
            Assert.NotNull(result);
            Assert.Equal("测试图表", result.Title); // 确认使用了TestMethod方法上的Chart特性中的标题
            Assert.Equal(ChartType.Line, result.Type);
            Assert.True(result.AutoRefresh);
            Assert.Equal(30, result.RefreshInterval);
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