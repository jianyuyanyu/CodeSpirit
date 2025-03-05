using CodeSpirit.Core;
using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.IdentityApi.Dtos.User;
using CodeSpirit.IdentityApi.Services;
using Microsoft.AspNetCore.Identity;
using Moq;
using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using AutoMapper;
using CodeSpirit.Shared.Repositories;
using CodeSpirit.Shared.Services;
using CodeSpirit.Core.IdGenerator;

namespace CodeSpirit.IdentityApi.Tests.Services
{
    public class UserServiceTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly Mock<RoleManager<ApplicationRole>> _mockRoleManager;
        private readonly Mock<IRepository<ApplicationUser>> _mockRepository;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<UserService>> _mockLogger;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly Mock<IIdGenerator> _mockIdGenerator;
        private readonly Mock<ICurrentUser> _mockCurrentUser;
        private readonly UserService _userService;

        public UserServiceTests()
        {
            // 设置UserManager Mock
            var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
            _mockUserManager = new Mock<UserManager<ApplicationUser>>(
                userStoreMock.Object, null, null, null, null, null, null, null, null);

            // 设置RoleManager Mock
            var roleStoreMock = new Mock<IRoleStore<ApplicationRole>>();
            _mockRoleManager = new Mock<RoleManager<ApplicationRole>>(
                roleStoreMock.Object, null, null, null, null);

            // 设置其他依赖项
            _mockRepository = new Mock<IRepository<ApplicationUser>>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<UserService>>();
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            _mockIdGenerator = new Mock<IIdGenerator>();
            _mockCurrentUser = new Mock<ICurrentUser>();

            // 初始化UserService
            _userService = new UserService(
                _mockRepository.Object,
                _mockMapper.Object,
                _mockUserManager.Object,
                _mockRoleManager.Object,
                _mockLogger.Object,
                _mockHttpContextAccessor.Object,
                _mockIdGenerator.Object,
                _mockCurrentUser.Object
            );
        }

        [Fact]
        public async Task GetUsersAsync_ReturnsPagedList()
        {
            // Arrange
            var queryDto = new UserQueryDto { Page = 1, PerPage = 10 };
            var users = new List<ApplicationUser>
            {
                new ApplicationUser { Id = 1, UserName = "user1", Email = "user1@example.com" },
                new ApplicationUser { Id = 2, UserName = "user2", Email = "user2@example.com" }
            };

            _mockRepository.Setup(x => x.CreateQuery())
                .Returns(users.AsQueryable());

            var userDtos = users.Select(u => new UserDto 
            { 
                Id = u.Id, 
                UserName = u.UserName, 
                Email = u.Email 
            }).ToList();

            _mockMapper.Setup(x => x.Map<List<UserDto>>(It.IsAny<List<ApplicationUser>>()))
                .Returns(userDtos);

            // Act
            var result = await _userService.GetUsersAsync(queryDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Total);
            Assert.Equal(2, result.Items.Count);
        }

        [Fact]
        public async Task AssignRolesAsync_AddsRolesToUser()
        {
            // Arrange
            long userId = 1;
            var roles = new List<string> { "Admin", "User" };
            var user = new ApplicationUser { Id = userId, UserName = "testuser" };

            _mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString()))
                .ReturnsAsync(user);
            _mockUserManager.Setup(x => x.AddToRolesAsync(user, roles))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            await _userService.AssignRolesAsync(userId, roles);

            // Assert
            _mockUserManager.Verify(x => x.AddToRolesAsync(user, roles), Times.Once);
        }

        [Fact]
        public async Task SetActiveStatusAsync_UpdatesUserStatus()
        {
            // Arrange
            long userId = 1;
            var user = new ApplicationUser { Id = userId, UserName = "testuser" };

            _mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString()))
                .ReturnsAsync(user);
            _mockUserManager.Setup(x => x.UpdateAsync(user))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            await _userService.SetActiveStatusAsync(userId, true);

            // Assert
            Assert.True(user.IsActive);
            _mockUserManager.Verify(x => x.UpdateAsync(user), Times.Once);
        }

        [Fact]
        public async Task UnlockUserAsync_UnlocksUser()
        {
            // Arrange
            long userId = 1;
            var user = new ApplicationUser { Id = userId, UserName = "testuser" };

            _mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString()))
                .ReturnsAsync(user);
            _mockUserManager.Setup(x => x.SetLockoutEndDateAsync(user, null))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            await _userService.UnlockUserAsync(userId);

            // Assert
            _mockUserManager.Verify(x => x.SetLockoutEndDateAsync(user, null), Times.Once);
        }

        [Fact]
        public async Task GetUsersByIdsAsync_ReturnsMatchingUsers()
        {
            // Arrange
            var userIds = new List<long> { 1, 2 };
            var users = new List<ApplicationUser>
            {
                new ApplicationUser { Id = 1, UserName = "user1" },
                new ApplicationUser { Id = 2, UserName = "user2" }
            };

            _mockRepository.Setup(x => x.CreateQuery())
                .Returns(users.AsQueryable());

            // Act
            var result = await _userService.GetUsersByIdsAsync(userIds);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal(userIds[0], result[0].Id);
            Assert.Equal(userIds[1], result[1].Id);
        }
    }
} 