/*
using CodeSpirit.Charts;
using CodeSpirit.Charts.Core.Abstractions;
using CodeSpirit.Charts.Extensions;
using CodeSpirit.Charts.Services;
using CodeSpirit.IdentityApi.Controllers;
using CodeSpirit.IdentityApi.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace CodeSpirit.Charts.Tests.Integration;

public class UserStatisticsControllerTests
{
    private readonly Mock<IUserService> _userServiceMock;
    private readonly IChartService _chartService;
    private readonly Mock<ILogger<UserStatisticsController>> _loggerMock;
    private readonly ServiceProvider _serviceProvider;
    private readonly UserStatisticsController _controller;

    public UserStatisticsControllerTests()
    {
        _userServiceMock = new Mock<IUserService>();
        _loggerMock = new Mock<ILogger<UserStatisticsController>>();

        // 创建服务提供者
        var services = new ServiceCollection();
        services.AddSingleton<IChartService, ChartService>();
        services.AddSingleton<IEChartConfigGenerator, EChartConfigGenerator>();

        // 注册图表提供者
        services.AddSingleton<IChartProvider, CodeSpirit.Charts.Providers.ECharts.EChartsProvider>();

        _serviceProvider = services.BuildServiceProvider();
        _chartService = _serviceProvider.GetRequiredService<IChartService>();

        // 创建控制器
        _controller = new UserStatisticsController(
            _userServiceMock.Object,
            _chartService,
            _serviceProvider.GetRequiredService<IEChartConfigGenerator>(),
            _loggerMock.Object);

        // 设置HttpContext
        var httpContext = new DefaultHttpContext
        {
            RequestServices = _serviceProvider
        };
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    [Fact]
    public async Task GetGenderDistributionAsync_ReturnsCorrectPieChartConfig()
    {
        // 安排
        var genderDistribution = new List<object>
        {
            new Dictionary<string, object> { ["Gender"] = "Unknown", ["Count"] = 4 },
            new Dictionary<string, object> { ["Gender"] = "Male", ["Count"] = 2 },
            new Dictionary<string, object> { ["Gender"] = "Female", ["Count"] = 1 }
        };

        _userServiceMock.Setup(x => x.GetUserGenderDistributionAsync())
            .ReturnsAsync(genderDistribution);

        // 行动
        var result = await _controller.GetGenderDistributionAsync();

        // 断言
        var okResult = Assert.IsType<OkObjectResult>(result);
        var chartConfig = okResult.Value;

        // 将配置转换为JSON以便于检查
        var jsonConfig = JsonConvert.SerializeObject(chartConfig);
        
        // 验证配置包含重要的部分
        Assert.Contains("\"type\":\"pie\"", jsonConfig);
        Assert.Contains("\"categoryField\":\"Gender\"", jsonConfig);
        Assert.Contains("\"valueField\":\"Count\"", jsonConfig);
        
        // 验证数据已正确处理
        Assert.Contains("\"Unknown\"", jsonConfig);
        Assert.Contains("\"Male\"", jsonConfig);
        Assert.Contains("\"Female\"", jsonConfig);
    }
    
    [Fact]
    public async Task GetInactiveUsersAsync_ReturnsCorrectLineChartConfig()
    {
        // 安排
        var inactiveUsers = new List<object>
        {
            new Dictionary<string, object> { ["InactiveDays"] = "30天以上", ["UserCount"] = 6 },
            new Dictionary<string, object> { ["InactiveDays"] = "60天以上", ["UserCount"] = 3 },
            new Dictionary<string, object> { ["InactiveDays"] = "90天以上", ["UserCount"] = 3 },
            new Dictionary<string, object> { ["InactiveDays"] = "120天以上", ["UserCount"] = 3 },
            new Dictionary<string, object> { ["InactiveDays"] = "150天以上", ["UserCount"] = 3 }
        };

        _userServiceMock.Setup(x => x.GetInactiveUsersStatisticsAsync(It.IsAny<int>()))
            .ReturnsAsync(inactiveUsers);

        // 行动
        var result = await _controller.GetInactiveUsersAsync();

        // 断言
        var okResult = Assert.IsType<OkObjectResult>(result);
        var chartConfig = okResult.Value;

        // 将配置转换为JSON以便于检查
        var jsonConfig = JsonConvert.SerializeObject(chartConfig);
        
        // 验证配置包含重要的部分
        Assert.Contains("\"type\":\"line\"", jsonConfig);
        Assert.Contains("\"xField\":\"InactiveDays\"", jsonConfig);
        Assert.Contains("\"yField\":\"UserCount\"", jsonConfig);
        
        // 验证X轴数据已正确处理
        Assert.Contains("\"30天以上\"", jsonConfig);
        Assert.Contains("\"60天以上\"", jsonConfig);
        Assert.Contains("\"90天以上\"", jsonConfig);
        Assert.Contains("\"120天以上\"", jsonConfig);
        Assert.Contains("\"150天以上\"", jsonConfig);
        
        // 验证Y轴数据已正确处理 - 验证数值出现在数据中
        Assert.Contains("6", jsonConfig);
        Assert.Contains("3", jsonConfig);
    }
}
*/ 