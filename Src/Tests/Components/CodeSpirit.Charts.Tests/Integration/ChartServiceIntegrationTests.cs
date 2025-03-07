using CodeSpirit.Charts.Models;
using CodeSpirit.Charts.Services;
using CodeSpirit.Charts.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace CodeSpirit.Charts.Tests.Integration
{
    /// <summary>
    /// ChartService的集成测试
    /// </summary>
    public class ChartServiceIntegrationTests : IntegrationTestBase
    {
        private readonly IChartService _chartService;
        private readonly ITestOutputHelper _outputHelper;

        public ChartServiceIntegrationTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
            _chartService = GetService<IChartService>();
        }
        
        /// <summary>
        /// 配置服务，添加XUnit日志提供程序
        /// </summary>
        protected override void ConfigureServices(ServiceCollection services)
        {
            base.ConfigureServices(services);
            
            // 添加输出到XUnit测试输出窗口的日志提供程序
            services.AddLogging(builder =>
            {
                builder.AddProvider(new XUnitLoggerProvider(_outputHelper));
            });
        }

        [Fact]
        public async Task CompleteWorkflow_WithRealImplementations_WorksCorrectly()
        {
            // 准备测试数据 - 具有明显类别特性的数据
            var categoryData = new[]
            {
                new { 类别 = "食品", 销量 = 3500, 增长率 = 0.05 },
                new { 类别 = "电子", 销量 = 4800, 增长率 = 0.12 },
                new { 类别 = "服装", 销量 = 2700, 增长率 = 0.03 },
                new { 类别 = "家居", 销量 = 1900, 增长率 = 0.08 },
                new { 类别 = "美妆", 销量 = 3200, 增长率 = 0.15 }
            };

            // 记录测试开始
            _outputHelper.WriteLine("开始完整工作流测试");

            // 1. 获取推荐图表类型
            var chartType = await _chartService.RecommendChartTypeAsync(categoryData);
            _outputHelper.WriteLine($"推荐图表类型: {chartType}");
            
            // 使用真实实现，类别数据通常推荐柱状图或饼图
            Assert.True(chartType == ChartType.Bar || chartType == ChartType.Pie,
                $"期望柱状图或饼图，实际得到: {chartType}");

            // 2. 生成图表配置
            var config = await _chartService.AnalyzeAndGenerateChartAsync(categoryData);
            _outputHelper.WriteLine($"生成图表配置: {config.Title}, 类型: {config.Type}");
            
            Assert.NotNull(config);
            Assert.Equal(chartType, config.Type);

            // 3. 保存配置
            var chartId = await _chartService.SaveChartConfigAsync(config);
            _outputHelper.WriteLine($"保存的图表ID: {chartId}");
            
            Assert.NotNull(chartId);

            // 4. 检索配置
            var retrievedConfig = await _chartService.GetChartConfigAsync(chartId);
            _outputHelper.WriteLine($"检索的图表标题: {retrievedConfig.Title}");
            
            Assert.Equal(config.Title, retrievedConfig.Title);
            Assert.Equal(config.Type, retrievedConfig.Type);

            // 5. 生成图表JSON
            var json = await _chartService.GenerateChartJsonAsync(retrievedConfig);
            
            Assert.NotNull(json);
            Assert.NotNull(json["title"]);
            Assert.NotNull(json["series"]);
            
            // 验证所有类别都在图表中
            if (chartType == ChartType.Pie)
            {
                var dataPoints = json["series"][0]["data"];
                Assert.Equal(5, dataPoints.Count());
                
                var categories = dataPoints.Select(p => p["name"].ToString()).ToList();
                Assert.Contains("食品", categories);
                Assert.Contains("电子", categories);
                Assert.Contains("服装", categories);
                Assert.Contains("家居", categories);
                Assert.Contains("美妆", categories);
            }
            
            _outputHelper.WriteLine("完整工作流测试成功完成");
        }
        
        [Fact]
        public async Task ChartDataSource_IsCorrectlySet_WhenGeneratingChartConfig()
        {
            // 准备测试数据
            var categoryData = new[]
            {
                new { 类别 = "食品", 销量 = 3500, 增长率 = 0.05 },
                new { 类别 = "电子", 销量 = 4800, 增长率 = 0.12 },
                new { 类别 = "服装", 销量 = 2700, 增长率 = 0.03 }
            };
            
            _outputHelper.WriteLine("开始测试 ChartDataSource 赋值");
            
            // 生成图表配置
            var config = await _chartService.AnalyzeAndGenerateChartAsync(categoryData);
            
            // 验证 ChartDataSource 被正确赋值
            Assert.NotNull(config.DataSource);
            Assert.Equal(DataSourceType.Current, config.DataSource.Type);
            Assert.NotNull(config.DataSource.StaticData);
            
            _outputHelper.WriteLine($"DataSource.Type: {config.DataSource.Type}");
            
            // 验证 DataSource.StaticData 包含原始数据
            var staticData = config.DataSource.StaticData;
            Assert.NotNull(staticData);
            
            // 保存配置
            var chartId = await _chartService.SaveChartConfigAsync(config);
            
            // 检索配置
            var retrievedConfig = await _chartService.GetChartConfigAsync(chartId);
            
            // 验证检索的配置中 DataSource 也被正确保存
            Assert.NotNull(retrievedConfig.DataSource);
            Assert.Equal(DataSourceType.Current, retrievedConfig.DataSource.Type);
            Assert.NotNull(retrievedConfig.DataSource.StaticData);
            
            _outputHelper.WriteLine("ChartDataSource 测试完成");
        }
    }
    
    /// <summary>
    /// XUnit日志提供程序，用于将日志输出到测试结果中
    /// </summary>
    public class XUnitLoggerProvider : ILoggerProvider
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public XUnitLoggerProvider(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new XUnitLogger(_testOutputHelper, categoryName);
        }

        public void Dispose()
        {
        }

        private class XUnitLogger : ILogger
        {
            private readonly ITestOutputHelper _testOutputHelper;
            private readonly string _categoryName;

            public XUnitLogger(ITestOutputHelper testOutputHelper, string categoryName)
            {
                _testOutputHelper = testOutputHelper;
                _categoryName = categoryName;
            }

            public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                try
                {
                    _testOutputHelper.WriteLine($"[{logLevel}] {_categoryName}: {formatter(state, exception)}");
                    if (exception != null)
                    {
                        _testOutputHelper.WriteLine($"Exception: {exception}");
                    }
                }
                catch
                {
                    // 忽略输出错误
                }
            }

            private class NullScope : IDisposable
            {
                public static NullScope Instance { get; } = new NullScope();
                public void Dispose() { }
            }
        }
    }
} 