using CodeSpirit.Charts.Analysis;
using CodeSpirit.Charts.Attributes;
using CodeSpirit.Charts.Models;
using CodeSpirit.Charts.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json.Linq;
using System.Reflection;
using Xunit;

namespace CodeSpirit.Charts.Tests
{
    public class ChartConfigBuilderTests
    {
        private readonly ChartConfigBuilder _chartConfigBuilder;
        private readonly Mock<IServiceProvider> _serviceProviderMock;
        private readonly Mock<IMemoryCache> _memoryCacheMock;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private readonly Mock<IChartService> _chartServiceMock;
        private readonly Mock<IChartRecommender> _chartRecommenderMock;

        public ChartConfigBuilderTests()
        {
            _serviceProviderMock = new Mock<IServiceProvider>();
            _memoryCacheMock = new Mock<IMemoryCache>();
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            _chartServiceMock = new Mock<IChartService>();
            _chartRecommenderMock = new Mock<IChartRecommender>();

            // 设置服务提供程序Mock
            _serviceProviderMock.Setup(x => x.GetService(typeof(IChartService)))
                .Returns(_chartServiceMock.Object);
            _serviceProviderMock.Setup(x => x.GetService(typeof(IChartRecommender)))
                .Returns(_chartRecommenderMock.Object);

            // 设置Http上下文Mock
            var httpContext = new Mock<HttpContext>();
            var request = new Mock<HttpRequest>();
            request.Setup(x => x.Scheme).Returns("http");
            request.Setup(x => x.Host).Returns(new HostString("localhost"));
            request.Setup(x => x.PathBase).Returns(new PathString(""));
            request.Setup(x => x.Path).Returns(new PathString("/api/chart/test"));
            httpContext.Setup(x => x.Request).Returns(request.Object);
            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext.Object);

            // 设置内存缓存Mock
            object cachedValue = null;
            _memoryCacheMock
                .Setup(x => x.TryGetValue(It.IsAny<object>(), out cachedValue))
                .Returns(false);

            _chartConfigBuilder = new ChartConfigBuilder(
                _serviceProviderMock.Object,
                _memoryCacheMock.Object,
                _httpContextAccessorMock.Object);
        }

        [Fact]
        public async Task BuildChartConfigForDataAsync_ReturnsJsonConfig()
        {
            // 准备测试数据
            var testData = new[]
            {
                new { category = "A", value = 100 },
                new { category = "B", value = 200 }
            };

            var chartConfig = new ChartConfig
            {
                Title = "Test Chart",
                Type = ChartType.Bar
            };

            var jsonConfig = new JObject
            {
                ["title"] = new JObject { ["text"] = "Test Chart" },
                ["type"] = "bar"
            };

            // 设置Mock
            _chartRecommenderMock.Setup(x => x.GenerateChartConfig(It.IsAny<object>(), It.IsAny<ChartType?>()))
                .Returns(chartConfig);

            _chartServiceMock.Setup(x => x.GenerateChartJsonAsync(It.IsAny<ChartConfig>()))
                .ReturnsAsync(jsonConfig);

            // 执行测试
            var result = await _chartConfigBuilder.BuildChartConfigForDataAsync(testData, ChartType.Bar);

            // 断言
            Assert.NotNull(result);
            Assert.Equal("Test Chart", result["title"]["text"].ToString());
            Assert.Equal("bar", result["type"].ToString());
        }

        [Fact]
        public async Task GetRecommendedChartConfigsAsync_ReturnsMultipleConfigs()
        {
            // 准备测试数据
            var testData = new[]
            {
                new { category = "A", value = 100 },
                new { category = "B", value = 200 }
            };

            var recommendedTypes = new Dictionary<ChartType, double>
            {
                { ChartType.Bar, 0.8 },
                { ChartType.Pie, 0.5 }
            };

            var barConfig = new ChartConfig { Type = ChartType.Bar };
            var pieConfig = new ChartConfig { Type = ChartType.Pie };

            var barJson = new JObject { ["type"] = "bar" };
            var pieJson = new JObject { ["type"] = "pie" };

            // 设置Mock
            _chartRecommenderMock.Setup(x => x.RecommendChartTypes(It.IsAny<object>(), It.IsAny<int>()))
                .Returns(recommendedTypes);

            _chartRecommenderMock.Setup(x => x.GenerateChartConfig(It.IsAny<object>(), ChartType.Bar))
                .Returns(barConfig);
            _chartRecommenderMock.Setup(x => x.GenerateChartConfig(It.IsAny<object>(), ChartType.Pie))
                .Returns(pieConfig);

            _chartServiceMock.Setup(x => x.GenerateChartJsonAsync(barConfig))
                .ReturnsAsync(barJson);
            _chartServiceMock.Setup(x => x.GenerateChartJsonAsync(pieConfig))
                .ReturnsAsync(pieJson);

            // 执行测试
            var result = await _chartConfigBuilder.GetRecommendedChartConfigsAsync(testData, 2);

            // 断言
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains(ChartType.Bar, result.Keys);
            Assert.Contains(ChartType.Pie, result.Keys);
            Assert.Equal("bar", result[ChartType.Bar]["type"].ToString());
            Assert.Equal("pie", result[ChartType.Pie]["type"].ToString());
        }

        [Fact]
        public async Task BuildChartConfigAsync_WithCachedConfig_ReturnsCachedValue()
        {
            // 准备测试数据
            var cachedJson = new JObject { ["cached"] = true };
            object cachedValue = cachedJson;

            // 设置Mock以返回缓存值
            _memoryCacheMock
                .Setup(x => x.TryGetValue(It.IsAny<object>(), out cachedValue))
                .Returns(true);

            // 创建带有Chart特性的端点Mock
            var endpoint = CreateEndpointWithChartAttribute();

            // 执行测试
            var result = await _chartConfigBuilder.BuildChartConfigAsync(endpoint);

            // 断言
            Assert.NotNull(result);
            Assert.True((bool)result["cached"]);
            
            // 验证未调用图表服务（因为使用了缓存）
            _chartServiceMock.Verify(x => x.GenerateChartConfigAsync(It.IsAny<MethodInfo>()), Times.Never);
        }

        private Endpoint CreateEndpointWithChartAttribute()
        {
            // 创建ControllerActionDescriptor Mock
            var actionDescriptor = new Mock<ControllerActionDescriptor>();
            actionDescriptor.Setup(x => x.ControllerName).Returns("Test");
            actionDescriptor.Setup(x => x.ActionName).Returns("TestAction");
            
            // 使用带Chart特性的测试方法
            var methodInfo = GetType().GetMethod("TestMethod", BindingFlags.Instance | BindingFlags.NonPublic);
            actionDescriptor.Setup(x => x.MethodInfo).Returns(methodInfo);

            // 创建Endpoint的Metadata
            var mockMetadata = new EndpointMetadataCollection(new object[] { actionDescriptor.Object });
            
            // 创建和返回Endpoint
            var endpointMock = new Mock<Endpoint>(MockBehavior.Loose);
            endpointMock.Setup(e => e.Metadata).Returns(mockMetadata);
            return endpointMock.Object;
        }

        [Chart("测试图表")]
        [ChartType(ChartType.Bar)]
        private void TestMethod() { }
    }
} 