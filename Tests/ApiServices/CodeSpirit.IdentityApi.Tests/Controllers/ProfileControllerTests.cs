using CodeSpirit.Authorization;
using CodeSpirit.Core;
using CodeSpirit.IdentityApi.Controllers;
using CodeSpirit.IdentityApi.Dtos.Profile;
using CodeSpirit.IdentityApi.Dtos.User;
using CodeSpirit.IdentityApi.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using Xunit;

namespace CodeSpirit.IdentityApi.Tests.Controllers;

/// <summary>
/// ProfileController测试类
/// </summary>
public class ProfileControllerTests
{
    private readonly Mock<IUserService> _mockUserService;
    private readonly Mock<ICurrentUser> _mockCurrentUser;
    private readonly ProfileController _controller;

    public ProfileControllerTests()
    {
        _mockUserService = new Mock<IUserService>();
        _mockCurrentUser = new Mock<ICurrentUser>();
        _controller = new ProfileController(_mockUserService.Object, _mockCurrentUser.Object);
    }

    [Fact]
    public async Task GetProfile_系统管理员登录_应该返回正确的个人资料()
    {
        // Arrange
        var systemAdminId = 1L;
        var systemAdminUser = new UserDto
        {
            Id = systemAdminId,
            Name = "系统管理员",
            UserName = "systemadmin",
            Email = "systemadmin@system.local",
            AvatarUrl = null,
            PhoneNumber = null
        };

        _mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);
        _mockCurrentUser.Setup(x => x.Id).Returns(systemAdminId);
        _mockCurrentUser.Setup(x => x.Roles).Returns(new[] { "SystemAdmin" });
        _mockCurrentUser.Setup(x => x.Claims).Returns(new List<Claim>
        {
            new Claim("permissions", "SystemManagement"),
            new Claim("permissions", "UserManagement")
        });

        _mockUserService.Setup(x => x.GetUserByIdIgnoreFiltersAsync(systemAdminId))
            .ReturnsAsync(systemAdminUser);

        // Act
        var result = await _controller.GetProfile();

        // Assert
        Assert.IsType<ActionResult<ApiResponse<ProfileDto>>>(result);
        var actionResult = result.Result as OkObjectResult;
        Assert.NotNull(actionResult);
        
        var apiResponse = actionResult.Value as ApiResponse<ProfileDto>;
        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);
        
        var profile = apiResponse.Data;
        Assert.Equal(systemAdminId, profile.Id);
        Assert.Equal("系统管理员", profile.Name);
        Assert.Equal("systemadmin", profile.UserName);
        Assert.Equal("systemadmin@system.local", profile.Email);
        Assert.Contains("SystemAdmin", profile.Roles);
        Assert.Contains("SystemManagement", profile.Permissions);
        Assert.Contains("UserManagement", profile.Permissions);

        // 验证调用了正确的方法（忽略过滤器的方法）
        _mockUserService.Verify(x => x.GetUserByIdIgnoreFiltersAsync(systemAdminId), Times.Once);
    }

    [Fact]
    public async Task GetProfile_未认证用户_应该返回401()
    {
        // Arrange
        _mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(false);
        _mockCurrentUser.Setup(x => x.Id).Returns((long?)null);

        // Act
        var result = await _controller.GetProfile();

        // Assert
        Assert.IsType<ActionResult<ApiResponse<ProfileDto>>>(result);
        var actionResult = result.Result as UnauthorizedObjectResult;
        Assert.NotNull(actionResult);
        
        var apiResponse = actionResult.Value as ApiResponse<ProfileDto>;
        Assert.NotNull(apiResponse);
        Assert.False(apiResponse.Success);
        Assert.Equal(401, apiResponse.Code);
        Assert.Equal("未登录或登录已过期", apiResponse.Message);
    }

    [Fact]
    public async Task GetProfile_用户不存在_应该返回404()
    {
        // Arrange
        var userId = 999L;
        _mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);
        _mockCurrentUser.Setup(x => x.Id).Returns(userId);
        _mockCurrentUser.Setup(x => x.Roles).Returns(new[] { "User" });
        _mockCurrentUser.Setup(x => x.Claims).Returns(new List<Claim>());

        _mockUserService.Setup(x => x.GetUserByIdIgnoreFiltersAsync(userId))
            .ReturnsAsync((UserDto)null);

        // Act
        var result = await _controller.GetProfile();

        // Assert
        Assert.IsType<ActionResult<ApiResponse<ProfileDto>>>(result);
        var actionResult = result.Result as NotFoundObjectResult;
        Assert.NotNull(actionResult);
        
        var apiResponse = actionResult.Value as ApiResponse<ProfileDto>;
        Assert.NotNull(apiResponse);
        Assert.False(apiResponse.Success);
        Assert.Equal(404, apiResponse.Code);
        Assert.Equal("用户不存在", apiResponse.Message);
    }

    [Fact]
    public async Task GetProfile_普通业务用户_应该返回正确的个人资料()
    {
        // Arrange
        var businessUserId = 2L;
        var businessUser = new UserDto
        {
            Id = businessUserId,
            Name = "业务管理员",
            UserName = "admin",
            Email = "admin@example.com",
            AvatarUrl = "/images/avatar.jpg",
            PhoneNumber = "13800138000"
        };

        _mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);
        _mockCurrentUser.Setup(x => x.Id).Returns(businessUserId);
        _mockCurrentUser.Setup(x => x.Roles).Returns(new[] { "Admin" });
        _mockCurrentUser.Setup(x => x.Claims).Returns(new List<Claim>
        {
            new Claim("permissions", "UserManagement"),
            new Claim("permissions", "RoleManagement")
        });

        _mockUserService.Setup(x => x.GetUserByIdIgnoreFiltersAsync(businessUserId))
            .ReturnsAsync(businessUser);

        // Act
        var result = await _controller.GetProfile();

        // Assert
        Assert.IsType<ActionResult<ApiResponse<ProfileDto>>>(result);
        var actionResult = result.Result as OkObjectResult;
        Assert.NotNull(actionResult);
        
        var apiResponse = actionResult.Value as ApiResponse<ProfileDto>;
        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);
        
        var profile = apiResponse.Data;
        Assert.Equal(businessUserId, profile.Id);
        Assert.Equal("业务管理员", profile.Name);
        Assert.Equal("admin", profile.UserName);
        Assert.Equal("admin@example.com", profile.Email);
        Assert.Equal("/images/avatar.jpg", profile.AvatarUrl);
        Assert.Equal("13800138000", profile.PhoneNumber);
        Assert.Contains("Admin", profile.Roles);
        Assert.Contains("UserManagement", profile.Permissions);
        Assert.Contains("RoleManagement", profile.Permissions);

        // 验证调用了正确的方法（忽略过滤器的方法）
        _mockUserService.Verify(x => x.GetUserByIdIgnoreFiltersAsync(businessUserId), Times.Once);
    }
} 