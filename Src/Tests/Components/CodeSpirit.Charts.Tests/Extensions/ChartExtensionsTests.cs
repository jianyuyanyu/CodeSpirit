using System.Text.Json;
using CodeSpirit.Charts.Analysis;
using CodeSpirit.Charts.Extensions;
using CodeSpirit.Charts.Models;
using CodeSpirit.Charts.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace CodeSpirit.Charts.Tests.Extensions
{
    [TestClass]
    public class ChartExtensionsTests
    {
        private ServiceCollection _services;
        private ServiceProvider _serviceProvider;
        
        [TestInitialize]
        public void Setup()
        {
            _services = new ServiceCollection();
            
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
        
        [TestMethod]
        public void AddCodeSpiritCharts_ShouldRegisterAllRequiredServices()
        {
            // 安排
            var services = new ServiceCollection();
            
            // 执行
            services.AddCodeSpiritCharts();
            var serviceProvider = services.BuildServiceProvider();
            
            // 断言
            Assert.IsNotNull(serviceProvider.GetService<IDataAnalyzer>());
            Assert.IsNotNull(serviceProvider.GetService<IChartRecommender>());
            Assert.IsNotNull(serviceProvider.GetService<IEChartConfigGenerator>());
        }
        
        [TestMethod]
        public void ChartResult_WithValidConfig_ShouldReturnJsonResult()
        {
            // 安排
            var mockController = new Mock<ControllerBase>();
            var mockHttpContext = new Mock<HttpContext>();
            
            mockHttpContext.Setup(c => c.RequestServices).Returns(_serviceProvider);
            mockController.Setup(c => c.HttpContext).Returns(mockHttpContext.Object);
            
            var jsonResult = new JsonResult(new { success = true });
            mockController.Setup(c => c.Json(It.IsAny<object>())).Returns(jsonResult);
            
            var config = new ChartConfig { Title = "测试图表" };
            var data = new { name = "测试数据" };
            
            // 执行
            var result = mockController.Object.ChartResult(config, data);
            
            // 断言
            Assert.IsNotNull(result);
            mockController.Verify(c => c.Json(It.IsAny<object>()), Times.Once);
        }
        
        [TestMethod]
        public void AutoChartResult_ShouldUseRecommender_AndReturnJsonResult()
        {
            // 安排
            var mockController = new Mock<ControllerBase>();
            var mockHttpContext = new Mock<HttpContext>();
            
            mockHttpContext.Setup(c => c.RequestServices).Returns(_serviceProvider);
            mockController.Setup(c => c.HttpContext).Returns(mockHttpContext.Object);
            
            var jsonResult = new JsonResult(new { success = true });
            mockController.Setup(c => c.Json(It.IsAny<object>())).Returns(jsonResult);
            
            var data = new { name = "测试数据" };
            
            // 执行
            var result = mockController.Object.AutoChartResult(data);
            
            // 断言
            Assert.IsNotNull(result);
            mockController.Verify(c => c.Json(It.IsAny<object>()), Times.Once);
        }
        
        [TestMethod]
        public void ChartRecommendations_ShouldReturnMultipleOptions_AsJsonResult()
        {
            // 安排
            var mockController = new Mock<ControllerBase>();
            var mockHttpContext = new Mock<HttpContext>();
            
            mockHttpContext.Setup(c => c.RequestServices).Returns(_serviceProvider);
            mockController.Setup(c => c.HttpContext).Returns(mockHttpContext.Object);
            
            var jsonResult = new JsonResult(new { success = true });
            mockController.Setup(c => c.Json(It.IsAny<object>())).Returns(jsonResult);
            
            var data = new { name = "测试数据" };
            
            // 执行
            var result = mockController.Object.ChartRecommendations(data, 3);
            
            // 断言
            Assert.IsNotNull(result);
            mockController.Verify(c => c.Json(It.IsAny<object>()), Times.Once);
        }
    }
} 