using CodeSpirit.Charts.Analysis;
using CodeSpirit.Charts.Models;
using CodeSpirit.Charts.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using Xunit;

namespace CodeSpirit.Charts.Tests
{
    public class ChartConfigBuilderEChartsTests
    {
        private readonly Mock<IServiceProvider> _mockServiceProvider;
        private readonly Mock<IMemoryCache> _mockMemoryCache;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly Mock<IChartRecommender> _mockRecommender;
        private readonly Mock<IEChartConfigGenerator> _mockEChartGenerator;
        private readonly ChartConfigBuilder _builder;
        
        public ChartConfigBuilderEChartsTests()
        {
            _mockServiceProvider = new Mock<IServiceProvider>();
            _mockMemoryCache = new Mock<IMemoryCache>();
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            _mockRecommender = new Mock<IChartRecommender>();
            _mockEChartGenerator = new Mock<IEChartConfigGenerator>();
            
            // 设置EChart配置生成器的基本行为
            _mockEChartGenerator.Setup(g => g.GenerateEChartConfig(It.IsAny<ChartConfig>()))
                .Returns(new Dictionary<string, object>
                {
                    ["title"] = new Dictionary<string, string> { ["text"] = "测试图表" }
                });
            
            _mockEChartGenerator.Setup(g => g.GenerateEChartConfigJson(It.IsAny<ChartConfig>()))
                .Returns("{\"title\":{\"text\":\"测试图表\"}}");
            
            _mockEChartGenerator.Setup(g => g.GenerateCompleteEChartConfig(It.IsAny<ChartConfig>(), It.IsAny<object>()))
                .Returns(new Dictionary<string, object>
                {
                    ["title"] = new Dictionary<string, string> { ["text"] = "测试图表" },
                    ["series"] = new List<Dictionary<string, object>>
                    {
                        new Dictionary<string, object>
                        {
                            ["name"] = "数据系列",
                            ["data"] = new[] { 10, 20, 30 }
                        }
                    }
                });
            
            // 创建ChartConfigBuilder实例
            _builder = new ChartConfigBuilder(
                _mockServiceProvider.Object,
                _mockMemoryCache.Object,
                _mockHttpContextAccessor.Object,
                _mockRecommender.Object,
                _mockEChartGenerator.Object);
            
            // 设置基本配置
            _builder.SetTitle("测试图表");
        }
        
        [Fact]
        public void GenerateEChartConfig_ShouldCallEChartGenerator()
        {
            // 执行
            var result = _builder.GenerateEChartConfig();
            
            // 断言
            _mockEChartGenerator.Verify(g => g.GenerateEChartConfig(It.IsAny<ChartConfig>()), Times.Once);
            Assert.NotNull(result);
            Assert.IsType<Dictionary<string, object>>(result);
        }
        
        [Fact]
        public void GenerateEChartConfigJson_ShouldCallEChartGenerator()
        {
            // 执行
            var result = _builder.GenerateEChartConfigJson();
            
            // 断言
            _mockEChartGenerator.Verify(g => g.GenerateEChartConfigJson(It.IsAny<ChartConfig>()), Times.Once);
            Assert.NotNull(result);
            Assert.Contains("测试图表", result);
        }
        
        [Fact]
        public void GenerateCompleteEChartConfig_ShouldCallEChartGenerator()
        {
            // 安排
            var testData = new[]
            {
                new { category = "类别1", value = 10 },
                new { category = "类别2", value = 20 },
                new { category = "类别3", value = 30 }
            };
            
            // 执行
            var result = _builder.GenerateCompleteEChartConfig(testData);
            
            // 断言
            _mockEChartGenerator.Verify(g => g.GenerateCompleteEChartConfig(It.IsAny<ChartConfig>(), It.IsAny<object>()), Times.Once);
            Assert.NotNull(result);
            Assert.IsType<Dictionary<string, object>>(result);
        }
        
        [Fact]
        public void GenerateEChartConfigWithoutGenerator_ShouldThrowException()
        {
            // 安排 - 创建没有EChartGenerator的构建器
            var builder = new ChartConfigBuilder(
                _mockServiceProvider.Object,
                _mockMemoryCache.Object,
                _mockHttpContextAccessor.Object);
            
            // 执行 - 应抛出异常
            Assert.Throws<InvalidOperationException>(() => builder.GenerateEChartConfig());
        }
        
        [Fact]
        public void ChartConfigBuilder_FluentApi_ShouldBuildCompleteConfig()
        {
            // 安排
            var builder = new ChartConfigBuilder(
                _mockServiceProvider.Object,
                _mockMemoryCache.Object,
                _mockHttpContextAccessor.Object,
                _mockRecommender.Object,
                _mockEChartGenerator.Object);
            
            // 执行
            builder.SetTitle("销售数据");
            builder.SetSubtitle("月度销售数据");
            
            var result = builder.GenerateEChartConfig();
            
            // 断言
            _mockEChartGenerator.Verify(g => g.GenerateEChartConfig(It.IsAny<ChartConfig>()), Times.Once);
            Assert.NotNull(result);
        }
        
        [Fact]
        public void GenerateCompleteEChartConfigWithTestCase_ShouldGenerateExpectedConfig()
        {
            // 安排
            var testData = new[]
            {
                new { month = "1月", sales = 100, cost = 80 },
                new { month = "2月", sales = 150, cost = 100 },
                new { month = "3月", sales = 180, cost = 120 }
            };
            
            var builder = new ChartConfigBuilder(
                _mockServiceProvider.Object,
                _mockMemoryCache.Object,
                _mockHttpContextAccessor.Object,
                _mockRecommender.Object,
                _mockEChartGenerator.Object);
            
            builder.SetTitle("季度销售分析");
            
            // 设置模拟行为返回一个更完整的配置
            _mockEChartGenerator.Setup(g => g.GenerateCompleteEChartConfig(It.IsAny<ChartConfig>(), testData))
                .Returns(new Dictionary<string, object>
                {
                    ["title"] = new Dictionary<string, object> { ["text"] = "季度销售分析" },
                    ["xAxis"] = new Dictionary<string, object>
                    {
                        ["type"] = "category",
                        ["data"] = new[] { "1月", "2月", "3月" }
                    },
                    ["series"] = new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            ["name"] = "销售额",
                            ["type"] = "bar",
                            ["data"] = new[] { 100, 150, 180 }
                        },
                        new Dictionary<string, object>
                        {
                            ["name"] = "成本",
                            ["type"] = "bar",
                            ["data"] = new[] { 80, 100, 120 }
                        }
                    }
                });
            
            // 执行
            var result = builder.GenerateCompleteEChartConfig(testData) as Dictionary<string, object>;
            
            // 断言
            Assert.NotNull(result);
            Assert.Equal("季度销售分析", ((Dictionary<string, object>)result["title"])["text"]);
            
            var xAxis = result["xAxis"] as Dictionary<string, object>;
            Assert.NotNull(xAxis);
            
            var xAxisData = xAxis["data"] as string[];
            Assert.NotNull(xAxisData);
            Assert.Equal(3, xAxisData.Length);
            Assert.Equal("1月", xAxisData[0]);
            
            var series = result["series"] as List<object>;
            Assert.NotNull(series);
            Assert.Equal(2, series.Count);
        }
    }
} 