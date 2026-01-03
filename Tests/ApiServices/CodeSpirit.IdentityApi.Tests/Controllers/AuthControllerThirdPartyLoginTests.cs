using CodeSpirit.Core;
using CodeSpirit.IdentityApi.Controllers;
using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.IdentityApi.Dtos.Auth;
using CodeSpirit.IdentityApi.Dtos.User;
using CodeSpirit.IdentityApi.Models;
using CodeSpirit.IdentityApi.Services;
using CodeSpirit.MultiTenant.Abstractions;
using CodeSpirit.MultiTenant.Models;
using CodeSpirit.Shared.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CodeSpirit.IdentityApi.Tests.Controllers;

/// <summary>
/// AuthController第三方登录测试
/// </summary>
public class AuthControllerThirdPartyLoginTests
{
    private readonly Mock<IAuthService> _mockAuthService;
    private readonly Mock<SignInManager<ApplicationUser>> _mockSignInManager;
    private readonly Mock<ILogger<AuthController>> _mockLogger;
    private readonly Mock<IClientIpService> _mockClientIpService;
    private readonly Mock<ICurrentUser> _mockCurrentUser;
    private readonly Mock<ITenantStore> _mockTenantStore;
    private readonly AuthController _controller;

    public AuthControllerThirdPartyLoginTests()
    {
        _mockAuthService = new Mock<IAuthService>();
        _mockLogger = new Mock<ILogger<AuthController>>();
        _mockClientIpService = new Mock<IClientIpService>();
        _mockCurrentUser = new Mock<ICurrentUser>();
        _mockTenantStore = new Mock<ITenantStore>();

        // 设置 SignInManager 的 Mock
        var mockUserManager = new Mock<UserManager<ApplicationUser>>(
            Mock.Of<IUserStore<ApplicationUser>>(),
            null, null, null, null, null, null, null, null);
        _mockSignInManager = new Mock<SignInManager<ApplicationUser>>(
            mockUserManager.Object,
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<ApplicationUser>>(),
            null, null, null, null);

        // 设置 ClientIpService 的默认行为
        _mockClientIpService.Setup(x => x.GetClientIpAddress(It.IsAny<HttpContext>()))
            .Returns("127.0.0.1");

        _controller = new AuthController(
            _mockAuthService.Object,
            _mockSignInManager.Object,
            _mockLogger.Object,
            _mockClientIpService.Object,
            _mockCurrentUser.Object,
            _mockTenantStore.Object);

        // 设置HttpContext
        var controllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        _controller.ControllerContext = controllerContext;
    }

    [Fact]
    public async Task ThirdPartyLogin_有效请求_应该返回成功()
    {
        // Arrange
        var tenantId = "test_tenant";
        var model = new ThirdPartyLoginModel
        {
            PlatformType = ThirdPartyPlatformType.WeChatMiniProgram,
            Credential = "test_code",
            TenantId = tenantId
        };

        var tenantInfo = new TenantInfo
        {
            Id = tenantId,
            TenantId = tenantId,
            Name = "Test Tenant",
            IsActive = true
        };

        var userDto = new UserDto { Id = 1, UserName = "test_user" };
        var authResult = AuthResultDto.CreateSuccess("test_token", "test_refresh_token", userDto);

        _mockTenantStore
            .Setup(s => s.GetTenantAsync(tenantId))
            .ReturnsAsync(tenantInfo);

        _mockAuthService
            .Setup(s => s.ThirdPartyLoginAsync(model, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(authResult);

        // Act
        var result = await _controller.ThirdPartyLogin(model);

        // Assert
        var okResult = Assert.IsType<ActionResult<ApiResponse<AuthTokenResponse>>>(result);
        var response = Assert.IsType<OkObjectResult>(okResult.Result);
        var apiResponse = Assert.IsType<ApiResponse<AuthTokenResponse>>(response.Value);
        Assert.Equal(0, apiResponse.Status); // Status为0表示成功
        Assert.NotNull(apiResponse.Data);
        if (apiResponse.Data != null)
        {
            Assert.NotNull(apiResponse.Data.TenantInfo);
        }
        Assert.Equal(tenantId, apiResponse.Data.TenantInfo.TenantId);
        Assert.Equal("Test Tenant", apiResponse.Data.TenantInfo.TenantName);
    }

    [Fact]
    public async Task ThirdPartyLogin_租户不存在_应该返回错误()
    {
        // Arrange
        var tenantId = "non_existent_tenant";
        var model = new ThirdPartyLoginModel
        {
            PlatformType = ThirdPartyPlatformType.WeChatMiniProgram,
            Credential = "test_code",
            TenantId = tenantId
        };

        _mockTenantStore
            .Setup(s => s.GetTenantAsync(tenantId))
            .ReturnsAsync((ITenantInfo)null);

        // Act
        var result = await _controller.ThirdPartyLogin(model);

        // Assert
        var badResult = Assert.IsType<ActionResult<ApiResponse<AuthTokenResponse>>>(result);
        var response = Assert.IsType<ObjectResult>(badResult.Result);
        var apiResponse = Assert.IsType<ApiResponse<AuthTokenResponse>>(response.Value);
        Assert.NotEqual(0, apiResponse.Status); // Status非0表示失败
        Assert.Contains("租户不存在", apiResponse.Msg);
    }

    [Fact]
    public async Task ThirdPartyLogin_租户已禁用_应该返回错误()
    {
        // Arrange
        var tenantId = "disabled_tenant";
        var model = new ThirdPartyLoginModel
        {
            PlatformType = ThirdPartyPlatformType.WeChatMiniProgram,
            Credential = "test_code",
            TenantId = tenantId
        };

        var tenantInfo = new TenantInfo
        {
            Id = tenantId,
            TenantId = tenantId,
            Name = "Disabled Tenant",
            IsActive = false // 已禁用
        };

        _mockTenantStore
            .Setup(s => s.GetTenantAsync(tenantId))
            .ReturnsAsync(tenantInfo);

        // Act
        var result = await _controller.ThirdPartyLogin(model);

        // Assert
        var badResult = Assert.IsType<ActionResult<ApiResponse<AuthTokenResponse>>>(result);
        var response = Assert.IsType<ObjectResult>(badResult.Result);
        var apiResponse = Assert.IsType<ApiResponse<AuthTokenResponse>>(response.Value);
        Assert.NotEqual(0, apiResponse.Status); // Status非0表示失败
        Assert.Contains("租户不存在", apiResponse.Msg);
    }

    [Fact]
    public async Task WeChatLogin_有效请求_应该返回成功()
    {
        // Arrange
        var tenantId = "test_tenant";
        var model = new WeChatLoginModel
        {
            Code = "test_code",
            TenantId = tenantId
        };

        var tenantInfo = new TenantInfo
        {
            Id = tenantId,
            TenantId = tenantId,
            Name = "Test Tenant",
            IsActive = true
        };

        var userDto = new UserDto { Id = 1, UserName = "test_user" };
        var authResult = AuthResultDto.CreateSuccess("test_token", "test_refresh_token", userDto);

        _mockTenantStore
            .Setup(s => s.GetTenantAsync(tenantId))
            .ReturnsAsync(tenantInfo);

        _mockAuthService
            .Setup(s => s.WeChatLoginAsync(model, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(authResult);

        // Act
        var result = await _controller.WeChatLogin(model);

        // Assert
        var okResult = Assert.IsType<ActionResult<ApiResponse<AuthTokenResponse>>>(result);
        var response = Assert.IsType<OkObjectResult>(okResult.Result);
        var apiResponse = Assert.IsType<ApiResponse<AuthTokenResponse>>(response.Value);
        Assert.Equal(0, apiResponse.Status); // Status为0表示成功
        Assert.NotNull(apiResponse.Data);
        if (apiResponse.Data != null)
        {
            Assert.NotNull(apiResponse.Data.TenantInfo);
        }
    }

    [Fact]
    public async Task WeChatLogin_登录失败_应该返回错误()
    {
        // Arrange
        var tenantId = "test_tenant";
        var model = new WeChatLoginModel
        {
            Code = "invalid_code",
            TenantId = tenantId
        };

        var tenantInfo = new TenantInfo
        {
            Id = tenantId,
            TenantId = tenantId,
            Name = "Test Tenant",
            IsActive = true
        };

        var authResult = AuthResultDto.CreateFailure("登录失败");

        _mockTenantStore
            .Setup(s => s.GetTenantAsync(tenantId))
            .ReturnsAsync(tenantInfo);

        _mockAuthService
            .Setup(s => s.WeChatLoginAsync(model, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(authResult);

        // Act
        var result = await _controller.WeChatLogin(model);

        // Assert
        var badResult = Assert.IsType<ActionResult<ApiResponse<AuthTokenResponse>>>(result);
        var response = Assert.IsType<ObjectResult>(badResult.Result);
        var apiResponse = Assert.IsType<ApiResponse<AuthTokenResponse>>(response.Value);
        Assert.NotEqual(0, apiResponse.Status); // Status非0表示失败
        Assert.Contains("登录失败", apiResponse.Msg);
    }
}

