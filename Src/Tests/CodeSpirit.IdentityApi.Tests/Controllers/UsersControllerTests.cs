using CodeSpirit.Core;
using CodeSpirit.Core.Dtos;
using CodeSpirit.IdentityApi.Controllers;
using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.IdentityApi.Dtos.User;
using CodeSpirit.IdentityApi.Services;
using CodeSpirit.IdentityApi.Tests.TestBase;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc.Abstractions;
using CodeSpirit.Shared.Filters;

namespace CodeSpirit.IdentityApi.Tests.Controllers
{
    /// <summary>
    /// 用户控制器测试类
    /// </summary>
    public class UsersControllerTests : ControllerTestBase
    {
        private readonly Mock<IUserService> _mockUserService;
        private readonly Mock<IAuthService> _mockAuthService;
        private readonly UsersController _controller;

        public UsersControllerTests()
        {
            _mockUserService = new Mock<IUserService>();
            _mockAuthService = new Mock<IAuthService>();
            _controller = new UsersController(_mockUserService.Object, _mockAuthService.Object);
        }

        /// <summary>
        /// 测试目的：验证获取用户列表接口能正确返回分页数据
        /// 预期结果：返回成功响应，包含正确的分页用户数据
        /// </summary>
        [Fact]
        public async Task GetUsers_ReturnsSuccessResponse()
        {
            // Arrange
            var queryDto = new UserQueryDto { Page = 1, PerPage = 10 };
            var expectedUsers = new PageList<UserDto>
            {
                Items = new List<UserDto>
                {
                    new UserDto { Id = 1, UserName = "user1", Email = "user1@example.com" },
                    new UserDto { Id = 2, UserName = "user2", Email = "user2@example.com" }
                },
                Total = 2
            };

            _mockUserService.Setup(x => x.GetUsersAsync(queryDto))
                .ReturnsAsync(expectedUsers);

            // Act
            var result = await _controller.GetUsers(queryDto);

            // Assert
            AssertSuccessResponse(result, expectedUsers);
            AssertPaginationResponse(expectedUsers, 2, 2);
        }

        /// <summary>
        /// 测试目的：验证创建用户接口在输入有效数据时能正确创建用户
        /// 预期结果：返回CreatedAtAction结果，包含新创建的用户数据
        /// </summary>
        [Fact]
        public async Task CreateUser_WithValidData_ReturnsSuccessResponse()
        {
            // Arrange
            var createDto = new CreateUserDto
            {
                UserName = "newuser",
                Email = "newuser@example.com",
                Name = "New User",
                Roles = new List<string> { "User" }
            };

            var expectedUser = new UserDto
            {
                Id = 1,
                UserName = createDto.UserName,
                Email = createDto.Email,
                Name = createDto.Name
            };

            _mockUserService.Setup(x => x.CreateAsync(createDto))
                .ReturnsAsync(expectedUser);

            // Act
            var result = await _controller.CreateUser(createDto);

            // Assert
            AssertCreatedAtActionResult(result, expectedUser);
        }

        /// <summary>
        /// 测试目的：验证创建用户接口在用户名无效时返回验证错误
        /// 测试数据：null、空字符串、空白字符串
        /// 预期结果：返回400错误，包含用户名验证错误信息
        /// </summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task CreateUser_WithInvalidUsername_ReturnsValidationError(string username)
        {
            // Arrange
            var createDto = new CreateUserDto
            {
                UserName = username,
                Email = "newuser@example.com",
                Name = "New User",
                Roles = new List<string> { "User" }
            };

            // 执行模型验证
            var validationResult = ValidateModel(_controller, createDto, "UserName", "用户名不能为空。");

            // 验证结果
            if (validationResult != null)
            {
                AssertModelValidationError(validationResult, "用户名不能为空");
                return;
            }

            // 如果验证通过，继续执行控制器方法
            var result = await _controller.CreateUser(createDto);
            AssertErrorResponse(result, 400, "用户名不能为空");
        }

        /// <summary>
        /// 测试目的：验证创建用户接口在用户名已存在时返回错误
        /// 预期结果：返回400错误，包含用户名重复的错误信息
        /// </summary>
        [Fact]
        public async Task CreateUser_WithExistingUsername_ReturnsDuplicateError()
        {
            // Arrange
            var createDto = new CreateUserDto
            {
                UserName = "existinguser",
                Email = "newuser@example.com",
                Name = "New User",
                Roles = new List<string> { "User" }
            };

            _mockUserService.Setup(x => x.CreateAsync(createDto))
                .ThrowsAsync(new AppServiceException(400, "用户名已存在"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<AppServiceException>(
                async () => await _controller.CreateUser(createDto)
            );
            
            Assert.Equal(400, exception.Code);
            Assert.Equal("用户名已存在", exception.Message);
        }

        /// <summary>
        /// 测试目的：验证创建用户接口在邮箱格式无效时返回验证错误
        /// 预期结果：返回400错误，包含邮箱格式错误信息
        /// </summary>
        [Fact]
        public async Task CreateUser_WithInvalidEmail_ReturnsValidationError()
        {
            // Arrange
            var createDto = new CreateUserDto
            {
                UserName = "newuser",
                Email = "invalid-email",
                Name = "New User",
                Roles = new List<string> { "User" }
            };

            _mockUserService.Setup(x => x.CreateAsync(createDto))
                .ThrowsAsync(new AppServiceException(400, "邮箱格式不正确"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<AppServiceException>(
                async () => await _controller.CreateUser(createDto)
            );
            
            Assert.Equal(400, exception.Code);
            Assert.Equal("邮箱格式不正确", exception.Message);
        }

        /// <summary>
        /// 测试目的：验证创建用户接口在角色列表为空时的处理
        /// 预期结果：成功创建用户，返回CreatedAtAction结果
        /// </summary>
        [Fact]
        public async Task CreateUser_WithEmptyRoles_ReturnsSuccessResponse()
        {
            // Arrange
            var createDto = new CreateUserDto
            {
                UserName = "newuser",
                Email = "newuser@example.com",
                Name = "New User",
                Roles = new List<string>()
            };

            var expectedUser = new UserDto
            {
                Id = 1,
                UserName = createDto.UserName,
                Email = createDto.Email,
                Name = createDto.Name
            };

            _mockUserService.Setup(x => x.CreateAsync(createDto))
                .ReturnsAsync(expectedUser);

            // Act
            var result = await _controller.CreateUser(createDto);

            // Assert
            AssertCreatedAtActionResult(result, expectedUser);
        }

        /// <summary>
        /// 测试目的：验证更新用户接口能正确更新用户信息
        /// 预期结果：返回成功响应
        /// </summary>
        [Fact]
        public async Task UpdateUser_ReturnsSuccessResponse()
        {
            // Arrange
            long userId = 1;
            var updateDto = new UpdateUserDto
            {
                Name = "Updated User",
                PhoneNumber = "1234567890",
                IsActive = true,
                Roles = new List<string> { "User" }
            };

            _mockUserService.Setup(x => x.UpdateUserAsync(userId, updateDto))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.UpdateUser(userId, updateDto);

            // Assert
            AssertBasicSuccessResponse(result);
            _mockUserService.Verify(x => x.UpdateUserAsync(userId, updateDto), Times.Once);
        }

        /// <summary>
        /// 测试目的：验证删除用户接口能正确删除用户
        /// 预期结果：返回成功响应，并验证服务方法被调用
        /// </summary>
        [Fact]
        public async Task DeleteUser_ReturnsSuccessResponse()
        {
            // Arrange
            long userId = 1;
            _mockUserService.Setup(x => x.DeleteAsync(userId))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.DeleteUser(userId);

            // Assert
            AssertBasicSuccessResponse(result);
            _mockUserService.Verify(x => x.DeleteAsync(userId), Times.Once);
        }

        /// <summary>
        /// 测试目的：验证获取用户详情接口能正确返回用户信息
        /// 预期结果：返回成功响应，包含正确的用户数据
        /// </summary>
        [Fact]
        public async Task Detail_WithValidId_ReturnsUser()
        {
            // Arrange
            long userId = 1;
            var expectedUser = new UserDto
            {
                Id = userId,
                UserName = "testuser",
                Email = "test@example.com",
                Name = "Test User"
            };

            _mockUserService.Setup(x => x.GetAsync(userId))
                .ReturnsAsync(expectedUser);

            // Act
            var result = await _controller.Detail(userId);

            // Assert
            AssertSuccessResponse(result, expectedUser);
        }

        [Fact]
        public async Task SetActiveStatus_ReturnsSuccessResponse()
        {
            // Arrange
            long userId = 1;
            bool isActive = true;
            _mockUserService.Setup(x => x.SetActiveStatusAsync(userId, isActive))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.SetActiveStatus(userId, isActive);

            // Assert
            var actionResult = Assert.IsType<ActionResult<ApiResponse>>(result);
            var response = Assert.IsType<ApiResponse>(((ObjectResult)actionResult.Result).Value);
            Assert.Equal(0, response.Status);
            Assert.Equal("用户已激活成功！", response.Msg);
            _mockUserService.Verify(x => x.SetActiveStatusAsync(userId, isActive), Times.Once);
        }
    }
}