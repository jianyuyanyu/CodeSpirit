using System.Text.Json;
using CodeSpirit.Charts.Analysis;
using CodeSpirit.Charts.Extensions;
using CodeSpirit.Charts.Models;
using CodeSpirit.Charts.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CodeSpirit.Charts.Tests.Extensions
{
    // 自定义测试控制器
    internal class TestController : ControllerBase
    {
        public TestController(HttpContext httpContext)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }
    }

    public class ChartExtensionsTests
    {
        private readonly ServiceCollection _services;
        private readonly ServiceProvider _serviceProvider;
        
        public ChartExtensionsTests()
        {
            _services = new ServiceCollection();
            
            // 添加日志服务
            _services.AddLogging();
            
            // 添加模拟服务
            var mockDataAnalyzer = new Mock<IDataAnalyzer>();
            var mockChartRecommender = new Mock<IChartRecommender>();
            var mockEChartGenerator = new Mock<IEChartConfigGenerator>();
            
            // 配置模拟服务的基本行为
            mockEChartGenerator.Setup(g => g.GenerateCompleteEChartConfig(It.IsAny<ChartConfig>(), It.IsAny<object>()))
                .Returns(new Dictionary<string, object>
                {
                    ["title"] = new Dictionary<string, string> { ["text"] = "测试图表" }
                });
            
            mockChartRecommender.Setup(r => r.GenerateChartConfig(It.IsAny<object>(), It.IsAny<ChartType?>()))
                .Returns(new ChartConfig { Title = "推荐图表" });
            
            mockChartRecommender.Setup(r => r.RecommendChartTypes(It.IsAny<object>(), It.IsAny<int>()))
                .Returns(new Dictionary<ChartType, double>
                {
                    { ChartType.Bar, 0.8 },
                    { ChartType.Line, 0.6 },
                    { ChartType.Pie, 0.4 }
                });
            
            // 注册模拟服务
            _services.AddSingleton(mockDataAnalyzer.Object);
            _services.AddSingleton(mockChartRecommender.Object);
            _services.AddSingleton(mockEChartGenerator.Object);
            
            _serviceProvider = _services.BuildServiceProvider();
        }
        
        [Fact]
        public void AddCodeSpiritCharts_ShouldRegisterAllRequiredServices()
        {
            // 安排
            var services = new ServiceCollection();
            
            // 确保日志服务可用
            services.AddLogging();
            
            // 执行
            CodeSpirit.Charts.Extensions.ChartExtensions.AddCodeSpiritCharts(services);
            var serviceProvider = services.BuildServiceProvider();
            
            // 断言
            Assert.NotNull(serviceProvider.GetService<IDataAnalyzer>());
            Assert.NotNull(serviceProvider.GetService<IChartRecommender>());
            Assert.NotNull(serviceProvider.GetService<IEChartConfigGenerator>());
        }
        
        [Fact]
        public void ChartResult_WithValidConfig_ShouldReturnJsonResult()
        {
            // 安排
            var httpContext = new DefaultHttpContext
            {
                RequestServices = _serviceProvider
            };
            var controller = new TestController(httpContext);
            
            var config = new ChartConfig { Title = "测试图表" };
            var data = new { name = "测试数据" };
            
            // 执行
            var result = controller.ChartResult(config, data);
            
            // 断言
            Assert.NotNull(result);
            Assert.IsType<JsonResult>(result);
        }
        
        [Fact]
        public void AutoChartResult_ShouldUseRecommender_AndReturnJsonResult()
        {
            // 安排
            var httpContext = new DefaultHttpContext
            {
                RequestServices = _serviceProvider
            };
            var controller = new TestController(httpContext);
            
            var data = new { name = "测试数据" };
            
            // 执行
            var result = controller.AutoChartResult(data);
            
            // 断言
            Assert.NotNull(result);
            Assert.IsType<JsonResult>(result);
        }
        
        [Fact]
        public void ChartRecommendations_ShouldReturnMultipleOptions_AsJsonResult()
        {
            // 安排
            var httpContext = new DefaultHttpContext
            {
                RequestServices = _serviceProvider
            };
            var controller = new TestController(httpContext);
            
            var data = new { name = "测试数据" };
            
            // 执行
            var result = controller.ChartRecommendations(data, 3);
            
            // 断言
            Assert.NotNull(result);
            Assert.IsType<JsonResult>(result);
        }
    }
} 