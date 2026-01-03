using CodeSpirit.Core;
using CodeSpirit.IdentityApi.Controllers;
using CodeSpirit.IdentityApi.Dtos.Settings;
using CodeSpirit.Settings.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CodeSpirit.IdentityApi.Tests.Controllers;

/// <summary>
/// 第三方登录设置控制器测试
/// </summary>
public class ThirdPartyLoginSettingsControllerTests
{
    private readonly Mock<ISettingsService> _mockSettingsService;
    private readonly Mock<ICurrentUser> _mockCurrentUser;
    private readonly Mock<ILogger<ThirdPartyLoginSettingsController>> _mockLogger;
    private readonly ThirdPartyLoginSettingsController _controller;

    public ThirdPartyLoginSettingsControllerTests()
    {
        _mockSettingsService = new Mock<ISettingsService>();
        _mockCurrentUser = new Mock<ICurrentUser>();
        _mockLogger = new Mock<ILogger<ThirdPartyLoginSettingsController>>();

        _controller = new ThirdPartyLoginSettingsController(
            _mockSettingsService.Object,
            _mockCurrentUser.Object,
            _mockLogger.Object);

        // 设置HttpContext
        var controllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        _controller.ControllerContext = controllerContext;
    }

    [Fact]
    public async Task GetSettings_设置存在_应该返回设置()
    {
        // Arrange
        var tenantId = "test_tenant";
        _mockCurrentUser.Setup(u => u.TenantId).Returns(tenantId);

        var expectedSettings = new ThirdPartyLoginSettingsDto
        {
            WeChatAppId = "test_wechat_appid",
            WeChatAppSecret = "test_wechat_secret",
            AlipayAppId = "test_alipay_appid",
            AlipayAppSecret = "test_alipay_secret"
        };

        _mockSettingsService
            .Setup(s => s.GetTenantSettingAsync<ThirdPartyLoginSettingsDto>(
                "ThirdPartyLogin", "Configuration", tenantId))
            .ReturnsAsync(expectedSettings);

        // Act
        var result = await _controller.GetSettings();

        // Assert
        var okResult = Assert.IsType<ActionResult<ApiResponse<ThirdPartyLoginSettingsDto>>>(result);
        var response = Assert.IsType<OkObjectResult>(okResult.Result);
        var apiResponse = Assert.IsType<ApiResponse<ThirdPartyLoginSettingsDto>>(response.Value);
        Assert.Equal(0, apiResponse.Status); // Status为0表示成功
        Assert.NotNull(apiResponse.Data);
        Assert.Equal(expectedSettings.WeChatAppId, apiResponse.Data.WeChatAppId);
    }

    [Fact]
    public async Task GetSettings_设置不存在_应该返回默认值()
    {
        // Arrange
        var tenantId = "test_tenant";
        _mockCurrentUser.Setup(u => u.TenantId).Returns(tenantId);

        _mockSettingsService
            .Setup(s => s.GetTenantSettingAsync<ThirdPartyLoginSettingsDto>(
                "ThirdPartyLogin", "Configuration", tenantId))
            .ReturnsAsync((ThirdPartyLoginSettingsDto)null);

        // Act
        var result = await _controller.GetSettings();

        // Assert
        var okResult = Assert.IsType<ActionResult<ApiResponse<ThirdPartyLoginSettingsDto>>>(result);
        var response = Assert.IsType<OkObjectResult>(okResult.Result);
        var apiResponse = Assert.IsType<ApiResponse<ThirdPartyLoginSettingsDto>>(response.Value);
        Assert.Equal(0, apiResponse.Status); // Status为0表示成功
        Assert.NotNull(apiResponse.Data);
        Assert.Equal(string.Empty, apiResponse.Data.WeChatAppId);
    }

    [Fact]
    public async Task SaveSettings_有效设置_应该保存成功()
    {
        // Arrange
        var tenantId = "test_tenant";
        var userName = "test_user";
        _mockCurrentUser.Setup(u => u.TenantId).Returns(tenantId);
        _mockCurrentUser.Setup(u => u.UserName).Returns(userName);

        var dto = new ThirdPartyLoginSettingsDto
        {
            WeChatAppId = "new_wechat_appid",
            WeChatAppSecret = "new_wechat_secret",
            AlipayAppId = "new_alipay_appid",
            AlipayAppSecret = "new_alipay_secret"
        };

        _mockSettingsService
            .Setup(s => s.SetTenantSettingAsync(
                "ThirdPartyLogin",
                "Configuration",
                dto,
                tenantId,
                It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.SaveSettings(dto);

        // Assert
        var okResult = Assert.IsType<ActionResult<ApiResponse>>(result);
        var response = Assert.IsType<OkObjectResult>(okResult.Result);
        var apiResponse = Assert.IsType<ApiResponse>(response.Value);
        Assert.Equal(0, apiResponse.Status); // Status为0表示成功
        _mockSettingsService.Verify(s => s.SetTenantSettingAsync(
            "ThirdPartyLogin",
            "Configuration",
            dto,
            tenantId,
            It.Is<string>(r => r.Contains(userName))), Times.Once);
    }

    [Fact]
    public async Task SaveSettings_保存失败_应该返回错误()
    {
        // Arrange
        var tenantId = "test_tenant";
        _mockCurrentUser.Setup(u => u.TenantId).Returns(tenantId);

        var dto = new ThirdPartyLoginSettingsDto
        {
            WeChatAppId = "test_appid",
            WeChatAppSecret = "test_secret"
        };

        _mockSettingsService
            .Setup(s => s.SetTenantSettingAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<ThirdPartyLoginSettingsDto>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.SaveSettings(dto);

        // Assert
        var badResult = Assert.IsType<ActionResult<ApiResponse>>(result);
        var response = Assert.IsType<ObjectResult>(badResult.Result);
        var apiResponse = Assert.IsType<ApiResponse>(response.Value);
        Assert.NotEqual(0, apiResponse.Status); // Status非0表示失败
    }
}

