using CodeSpirit.Core;
using CodeSpirit.IdentityApi.Controllers;
using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.IdentityApi.Dtos.Auth;
using CodeSpirit.IdentityApi.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace CodeSpirit.IdentityApi.Tests.Controllers
{
    public class AuthControllerTests
    {
        private readonly Mock<IAuthService> _mockAuthService;
        private readonly Mock<SignInManager<ApplicationUser>> _mockSignInManager;
        private readonly AuthController _controller;

        public AuthControllerTests()
        {
            _mockAuthService = new Mock<IAuthService>();
            
            // 设置 SignInManager 的 Mock
            var mockUserManager = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(),
                null, null, null, null, null, null, null, null);
            _mockSignInManager = new Mock<SignInManager<ApplicationUser>>(
                mockUserManager.Object,
                Mock.Of<Microsoft.AspNetCore.Http.IHttpContextAccessor>(),
                Mock.Of<IUserClaimsPrincipalFactory<ApplicationUser>>(),
                null, null, null, null);

            _controller = new AuthController(_mockAuthService.Object, _mockSignInManager.Object);
        }

        [Fact]
        public async Task Login_WithValidCredentials_ReturnsSuccessResponse()
        {
            // Arrange
            var loginModel = new LoginModel { UserName = "testuser", Password = "testpass" };
            var expectedToken = "test-token";
            var expectedUser = new UserDto();

            _mockAuthService.Setup(x => x.LoginAsync(loginModel.UserName, loginModel.Password))
                .ReturnsAsync((true, "登录成功", expectedToken, expectedUser));

            // Act
            var result = await _controller.Login(loginModel);

            // Assert
            var actionResult = Assert.IsType<ActionResult<ApiResponse<LoginResult>>>(result);
            var response = Assert.IsType<ApiResponse<LoginResult>>(((ObjectResult)actionResult.Result).Value);
            Assert.Equal(0, response.Status);
            Assert.Equal("登录成功！", response.Msg);
            Assert.Equal(expectedToken, response.Data.Token);
        }

        [Fact]
        public async Task Login_WithInvalidCredentials_ReturnsBadResponse()
        {
            // Arrange
            var loginModel = new LoginModel { UserName = "testuser", Password = "wrongpass" };
            var errorMessage = "用户名或密码错误";

            _mockAuthService.Setup(x => x.LoginAsync(loginModel.UserName, loginModel.Password))
                .ReturnsAsync((false, errorMessage, null, null));

            // Act
            var result = await _controller.Login(loginModel);

            // Assert
            var actionResult = Assert.IsType<ActionResult<ApiResponse<LoginResult>>>(result);
            var response = Assert.IsType<ApiResponse<LoginResult>>(((ObjectResult)actionResult.Result).Value);
            Assert.NotEqual(0, response.Status);
            Assert.Equal(errorMessage, response.Msg);
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
            var actionResult = Assert.IsType<ActionResult<ApiResponse>>(result);
            var response = Assert.IsType<ApiResponse>(((ObjectResult)actionResult.Result).Value);
            Assert.Equal(0, response.Status);
            Assert.Equal("退出登录成功!", response.Msg);
        }
    }
} 