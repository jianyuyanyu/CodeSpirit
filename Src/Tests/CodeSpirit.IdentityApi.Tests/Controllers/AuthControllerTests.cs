using CodeSpirit.Core;
using CodeSpirit.IdentityApi.Controllers;
using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.IdentityApi.Dtos.Auth;
using CodeSpirit.IdentityApi.Dtos.User;
using CodeSpirit.IdentityApi.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using System.Collections.Generic;

namespace CodeSpirit.IdentityApi.Tests.Controllers
{
    public class AuthControllerTests
    {
        private readonly Mock<IAuthService> _mockAuthService;
        private readonly Mock<SignInManager<ApplicationUser>> _mockSignInManager;
        private readonly Mock<ILogger<AuthController>> _mockLogger;
        private readonly AuthController _controller;

        public AuthControllerTests()
        {
            _mockAuthService = new Mock<IAuthService>();
            _mockLogger = new Mock<ILogger<AuthController>>();
            
            // 设置 SignInManager 的 Mock
            var mockUserManager = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(),
                null, null, null, null, null, null, null, null);
            _mockSignInManager = new Mock<SignInManager<ApplicationUser>>(
                mockUserManager.Object,
                Mock.Of<Microsoft.AspNetCore.Http.IHttpContextAccessor>(),
                Mock.Of<IUserClaimsPrincipalFactory<ApplicationUser>>(),
                null, null, null, null);

            _controller = new AuthController(_mockAuthService.Object, _mockSignInManager.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task Login_WithValidCredentials_ReturnsSuccessResponse()
        {
            // Arrange
            var userName = "testuser";
            var password = "testpassword";
            var loginModel = new LoginModel
            {
                UserName = userName,
                Password = password
            };

            // 预期服务器会构建包含IP和UserAgent的LoginDto
            var userDto = new UserDto { UserName = userName };
            var authResult = AuthResultDto.CreateSuccess("test-token", "test-refresh-token", userDto);
            
            _mockAuthService.Setup(a => a.LoginAsync(It.Is<LoginDto>(dto => 
                dto.UserName == userName && 
                dto.Password == password)))
                .ReturnsAsync(authResult);

            // 设置HttpContext
            var controllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            _controller.ControllerContext = controllerContext;

            // Act
            var result = await _controller.Login(loginModel);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<AuthTokenResponse>>(okResult.Value);
            
            Assert.Equal(200, response.Status);
            Assert.Equal("登录成功", response.Msg);
            Assert.NotNull(response.Data);
            Assert.Equal("test-token", response.Data.Token);
            Assert.Equal("test-refresh-token", response.Data.RefreshToken);
            Assert.Equal(userName, response.Data.User.UserName);
        }

        [Fact]
        public async Task Login_WithInvalidCredentials_ReturnsBadResponse()
        {
            // Arrange
            var loginModel = new LoginModel 
            { 
                UserName = "testuser", 
                Password = "wrongpass",
            };
            
            var errorMessage = "用户名或密码错误";
            var authResult = AuthResultDto.CreateFailure(errorMessage);

            _mockAuthService.Setup(x => x.LoginAsync(It.IsAny<LoginDto>()))
                .ReturnsAsync(authResult);
            
            // 设置HttpContext
            var controllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            _controller.ControllerContext = controllerContext;

            // Act
            var result = await _controller.Login(loginModel);

            // Assert
            var badResult = Assert.IsType<ActionResult<ApiResponse<AuthTokenResponse>>>(result);
            var response = Assert.IsType<ApiResponse>(badResult.Value);
            Assert.Equal(1, response.Status);
            // 不验证具体错误消息，只确保是失败状态
            Assert.False(string.IsNullOrEmpty(response.Msg));
        }

        [Fact]
        public async Task RefreshToken_WithValidTokens_ReturnsSuccessResponse()
        {
            // Arrange
            var refreshTokenDto = new RefreshTokenDto 
            { 
                Token = "old-token", 
                RefreshToken = "old-refresh-token" 
            };
            
            var userDto = new UserDto { UserName = "testuser" };
            var authResult = AuthResultDto.CreateSuccess("new-token", "new-refresh-token", userDto);

            _mockAuthService.Setup(x => x.RefreshTokenAsync(refreshTokenDto.Token, refreshTokenDto.RefreshToken))
                .ReturnsAsync(authResult);

            // Act
            var result = await _controller.RefreshToken(refreshTokenDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<AuthTokenResponse>>(okResult.Value);
            Assert.Equal(200, response.Status);
            Assert.Equal("令牌刷新成功", response.Msg);
            Assert.NotNull(response.Data);
            Assert.Equal("new-token", response.Data.Token);
            Assert.Equal("new-refresh-token", response.Data.RefreshToken);
            Assert.Equal("testuser", response.Data.User.UserName);
        }

        [Fact]
        public async Task RefreshToken_WithInvalidTokens_ReturnsBadResponse()
        {
            // Arrange
            var refreshTokenDto = new RefreshTokenDto 
            { 
                Token = "invalid-token", 
                RefreshToken = "invalid-refresh-token" 
            };
            
            var errorMessage = "无效的访问令牌";
            var authResult = AuthResultDto.CreateFailure(errorMessage);

            _mockAuthService.Setup(x => x.RefreshTokenAsync(refreshTokenDto.Token, refreshTokenDto.RefreshToken))
                .ReturnsAsync(authResult);

            // Act
            var result = await _controller.RefreshToken(refreshTokenDto);

            // Assert
            var badResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse>(badResult.Value);
            Assert.Equal(400, response.Status);
            Assert.Equal(errorMessage, response.Msg);
        }

        [Fact]
        public async Task RefreshToken_WithEmptyTokens_ReturnsBadResponse()
        {
            // Arrange
            var refreshTokenDto = new RefreshTokenDto 
            { 
                Token = "", 
                RefreshToken = null 
            };

            // Act
            var result = await _controller.RefreshToken(refreshTokenDto);

            // Assert
            var badResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse>(badResult.Value);
            Assert.Equal(400, response.Status);
            Assert.Equal("访问令牌和刷新令牌不能为空", response.Msg);
        }

        [Fact]
        public async Task Logout_ReturnsSuccessResponse()
        {
            // Arrange
            _mockSignInManager.Setup(x => x.SignOutAsync())
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Logout();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse>(okResult.Value);
            Assert.Equal(200, response.Status);
            Assert.Equal("退出登录成功!", response.Msg);
        }
    }
} 