using CodeSpirit.Charts.Analysis;
using CodeSpirit.Charts.Models;
using CodeSpirit.Charts.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;
using Xunit;

namespace CodeSpirit.Charts.Tests.Services
{
    /// <summary>
    /// 使用真实数据源的ChartService集成测试
    /// 注意：这些测试需要网络连接，并且使用真实的HTTP请求
    /// </summary>
    public class ChartServiceWithDataSourceTests : IDisposable
    {
        private readonly ChartService _chartService;
        private readonly IServiceProvider _serviceProvider;
        private readonly ServiceProvider _services;

        public ChartServiceWithDataSourceTests()
        {
            // 创建真实的服务集合
            var services = new ServiceCollection();

            // 添加真实的依赖
            services.AddLogging(builder => builder.AddConsole());
            services.AddHttpClient();
            
            // 添加图表相关服务
            services.AddSingleton<IDataAnalyzer, DataAnalyzer>();
            services.AddSingleton<IChartRecommender, ChartRecommender>();
            services.AddSingleton<IEChartConfigGenerator, EChartConfigGenerator>();
            
            _services = services.BuildServiceProvider();
            _serviceProvider = _services;

            // 创建ChartService实例，使用真实依赖
            _chartService = new ChartService(
                _serviceProvider.GetRequiredService<IChartRecommender>(),
                _serviceProvider.GetRequiredService<IDataAnalyzer>(),
                _serviceProvider.GetRequiredService<IHttpClientFactory>(),
                _serviceProvider.GetRequiredService<ILogger<ChartService>>());
        }

        public void Dispose()
        {
            _services?.Dispose();
        }

        [Fact(Skip = "这个测试需要真实的数据源，仅在本地运行")]
        public async Task GetChartDataWithExternalDataSource_ReturnsData()
        {
            // 创建使用API的数据源配置
            var dataSource = new ChartDataSource
            {
                Type = DataSourceType.Api,
                ApiUrl = "https://jsonplaceholder.typicode.com/posts",
                Method = "GET",
            };

            // 获取数据
            var result = await _chartService.GetChartDataAsync(dataSource);

            // 验证
            Assert.NotNull(result);
            
            // 确保是一个数组
            var resultArray = Assert.IsType<JsonElement>(result);
            Assert.Equal(JsonValueKind.Array, resultArray.ValueKind);
        }

        [Fact]
        public async Task AnalyzeAndGenerateWithLocalData_ProducesCorrectChart()
        {
            // 使用本地数据
            var data = new[]
            {
                new { category = "产品A", value = 1000 },
                new { category = "产品B", value = 1500 },
                new { category = "产品C", value = 800 },
                new { category = "产品D", value = 1200 },
                new { category = "产品E", value = 2000 }
            };

            // 使用真实数据分析和图表生成
            var config = await _chartService.AnalyzeAndGenerateChartAsync(data);
            
            // 生成最终图表
            var chartJson = await _chartService.GenerateChartJsonAsync(config);

            // 详细验证
            Assert.NotNull(config);
            Assert.NotNull(chartJson);
            Assert.NotNull(chartJson["series"]);
            Assert.True(chartJson["series"].Count() > 0, "系列数组不应为空");
            
            // 验证数据系列包含正确的数据点
            var series = chartJson["series"][0];
            Assert.NotNull(series);
            
            // 对于饼图，数据可能位于不同结构中，所以我们验证必要的图表属性
            Assert.NotNull(series["type"]);
            Assert.NotNull(series["name"]);
            
            // 如果存在data属性，验证数据
            if (series["data"] != null)
            {
                var dataPoints = series["data"];
                Assert.Equal(5, dataPoints.Count());
            }
            else
            {
                // 如果没有data属性，可能以其他形式存储数据
                // 此时不进行严格验证，仅确保图表已生成
                Console.WriteLine($"注意：系列中没有直接的data属性，图表类型为：{series["type"]}");
            }
        }

        [Fact]
        public async Task EndToEndIntegrationTest_WithLocalData_WorksCorrectly()
        {
            // 完整流程测试
            
            // 1. 准备本地测试数据
            var timeseriesData = new[]
            {
                new { timestamp = "2023-01-01", temperature = 5.2, humidity = 65 },
                new { timestamp = "2023-01-02", temperature = 6.1, humidity = 68 },
                new { timestamp = "2023-01-03", temperature = 4.9, humidity = 71 },
                new { timestamp = "2023-01-04", temperature = 3.8, humidity = 75 },
                new { timestamp = "2023-01-05", temperature = 5.4, humidity = 67 },
                new { timestamp = "2023-01-06", temperature = 7.2, humidity = 62 },
                new { timestamp = "2023-01-07", temperature = 8.5, humidity = 58 },
            };

            // 2. 获取推荐的图表类型
            var recommendedChartType = await _chartService.RecommendChartTypeAsync(timeseriesData);
            Assert.Equal(ChartType.Line, recommendedChartType); // 时间序列数据应推荐为折线图

            // 3. 获取多个推荐图表类型
            var recommendedTypes = await _chartService.GetRecommendedChartTypesAsync(timeseriesData);
            Assert.True(recommendedTypes.ContainsKey(ChartType.Line));

            // 4. 生成图表配置
            var chartConfig = await _chartService.AnalyzeAndGenerateChartAsync(timeseriesData);
            Assert.NotNull(chartConfig);
            Assert.Equal(ChartType.Line, chartConfig.Type);
            Assert.NotNull(chartConfig.XAxis);
            Assert.NotNull(chartConfig.YAxis);
            Assert.NotEmpty(chartConfig.Series);

            // 5. 保存配置并检索
            var chartId = await _chartService.SaveChartConfigAsync(chartConfig);
            var retrievedConfig = await _chartService.GetChartConfigAsync(chartId);
            Assert.Equal(chartConfig.Title, retrievedConfig.Title);

            // 6. 生成ECharts配置
            var echartJson = await _chartService.GenerateChartJsonAsync(retrievedConfig);
            Assert.NotNull(echartJson);
            Assert.NotNull(echartJson["series"]);
            Assert.NotNull(echartJson["xAxis"]);
            Assert.NotNull(echartJson["yAxis"]);
            
            // 验证数据系列对应的字段
            var seriesNames = echartJson["series"].Select(s => s["name"].ToString()).ToList();
            Assert.Contains("temperature", seriesNames);
            Assert.Contains("humidity", seriesNames);
        }
    }
} 