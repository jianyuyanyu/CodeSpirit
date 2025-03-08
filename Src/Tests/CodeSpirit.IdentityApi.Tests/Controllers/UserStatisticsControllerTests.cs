using CodeSpirit.IdentityApi.Controllers;
using CodeSpirit.Charts.Models;
using CodeSpirit.Charts.Attributes;
using System;
using System.Reflection;
using Xunit;

namespace CodeSpirit.IdentityApi.Tests.Controllers
{
    public class UserStatisticsControllerTests
    {
        [Fact]
        public void GetUserGrowthStatisticsAsync_ShouldHaveCorrectChartTitle()
        {
            // 获取方法信息
            var methodInfo = typeof(UserStatisticsController).GetMethod(nameof(UserStatisticsController.GetUserGrowthStatisticsAsync));
            
            // 断言
            Assert.NotNull(methodInfo);
            
            // 验证方法上的特性是否正确
            var chartAttribute = methodInfo.GetCustomAttribute<ChartAttribute>();
            Assert.NotNull(chartAttribute);
            Assert.Equal("用户增长趋势", chartAttribute.Title);
            
            // 验证图表类型特性
            var chartTypeAttribute = methodInfo.GetCustomAttribute<ChartTypeAttribute>();
            Assert.NotNull(chartTypeAttribute);
            Assert.Equal(ChartType.Line, chartTypeAttribute.Type);
            
            // 验证图表数据特性
            var chartDataAttribute = methodInfo.GetCustomAttribute<ChartDataAttribute>();
            Assert.NotNull(chartDataAttribute);
            Assert.Equal("Date", chartDataAttribute.DimensionField);
            Assert.Contains("UserCount", chartDataAttribute.MetricFields);
        }
    }
} 