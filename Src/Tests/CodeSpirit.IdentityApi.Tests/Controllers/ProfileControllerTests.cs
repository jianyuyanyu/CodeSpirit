using CodeSpirit.Authorization;
using CodeSpirit.Core;
using CodeSpirit.IdentityApi.Controllers;
using CodeSpirit.IdentityApi.Dtos.Profile;
using CodeSpirit.IdentityApi.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace CodeSpirit.IdentityApi.Tests.Controllers
{
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
        public async Task GetProfile_WhenNotAuthenticated_ReturnsUnauthorized()
        {
            // Arrange
            _mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(false);
            _mockCurrentUser.Setup(x => x.Id).Returns((long?)null);

            // Act
            var result = await _controller.GetProfile();

            // Assert
            var actionResult = Assert.IsType<ActionResult<ApiResponse<ProfileDto>>>(result);
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(actionResult.Result);
            var response = Assert.IsType<ApiResponse<ProfileDto>>(unauthorizedResult.Value);
            Assert.Equal(401, response.Status);
            Assert.Equal("未登录或登录已过期", response.Msg);
            Assert.Null(response.Data);
        }

        [Fact]
        public async Task GetProfile_WhenUserNotFound_ReturnsNotFound()
        {
            // Arrange
            long userId = 1;
            _mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);
            _mockCurrentUser.Setup(x => x.Id).Returns(userId);
            _mockUserService.Setup(x => x.GetAsync(userId)).ReturnsAsync((UserDto)null);

            // Act
            var result = await _controller.GetProfile();

            // Assert
            var actionResult = Assert.IsType<ActionResult<ApiResponse<ProfileDto>>>(result);
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(actionResult.Result);
            var response = Assert.IsType<ApiResponse<ProfileDto>>(notFoundResult.Value);
            Assert.Equal(404, response.Status);
            Assert.Equal("用户不存在", response.Msg);
            Assert.Null(response.Data);
        }

        [Fact]
        public async Task GetProfile_WhenUserExists_ReturnsSuccessResponse()
        {
            // Arrange
            long userId = 1;
            var userDto = new UserDto
            {
                Id = userId,
                Name = "测试用户",
                UserName = "testuser",
                Email = "test@example.com",
                AvatarUrl = "https://example.com/avatar.jpg",
                PhoneNumber = "13800138000"
            };

            var roles = new string[] { "Admin", "User" };
            var permissions = new List<System.Security.Claims.Claim>
            {
                new System.Security.Claims.Claim("permissions", "users.create"),
                new System.Security.Claims.Claim("permissions", "users.read")
            };

            _mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);
            _mockCurrentUser.Setup(x => x.Id).Returns(userId);
            _mockCurrentUser.Setup(x => x.Roles).Returns(roles);
            _mockCurrentUser.Setup(x => x.Claims).Returns(permissions);
            _mockUserService.Setup(x => x.GetAsync(userId)).ReturnsAsync(userDto);

            // Act
            var result = await _controller.GetProfile();

            // Assert
            var actionResult = Assert.IsType<ActionResult<ApiResponse<ProfileDto>>>(result);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var response = Assert.IsType<ApiResponse<ProfileDto>>(okResult.Value);
            Assert.Equal(0, response.Status);
            Assert.NotNull(response.Data);
            Assert.Equal(userId, response.Data.Id);
            Assert.Equal(userDto.Name, response.Data.Name);
            Assert.Equal(userDto.UserName, response.Data.UserName);
            Assert.Equal(userDto.Email, response.Data.Email);
            Assert.Equal(userDto.AvatarUrl, response.Data.AvatarUrl);
            Assert.Equal(userDto.PhoneNumber, response.Data.PhoneNumber);
            Assert.Equal(roles, response.Data.Roles);
            Assert.Equal(2, response.Data.Permissions.Count());
        }
    }
} 