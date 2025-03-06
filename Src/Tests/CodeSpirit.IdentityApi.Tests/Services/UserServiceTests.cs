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
using CodeSpirit.IdentityApi.Tests.TestBase;

namespace CodeSpirit.IdentityApi.Tests.Services
{
    public class UserServiceTests : ServiceTestBase
    {
        private readonly UserService _userService;
        private readonly Mock<IIdGenerator> _mockIdGenerator;

        public UserServiceTests()
            : base()
        {
            // 设置额外依赖
            _mockIdGenerator = new Mock<IIdGenerator>();
            
            // 初始化UserService
            _userService = new UserService(
                UserRepository,
                MockMapper.Object,
                UserManager,
                RoleManager,
                MockUserServiceLogger.Object,
                MockHttpContextAccessor.Object,
                _mockIdGenerator.Object,
                MockCurrentUser.Object
            );
            
            // 准备测试数据
            SeedTestData();
        }
        
        /// <summary>
        /// 准备用户测试数据
        /// </summary>
        protected override void SeedTestData()
        {
            var users = new List<ApplicationUser>
            {
                new ApplicationUser
                {
                    Id = 1,
                    UserName = "testuser",
                    Email = "test@example.com",
                    IsActive = true,
                    Name = "Test User",
                    LastLoginTime = DateTimeOffset.Now.AddDays(-1)
                },
                new ApplicationUser
                {
                    Id = 2,
                    UserName = "testuser2",
                    Email = "test2@example.com",
                    IsActive = true,
                    Name = "Test User 2",
                    LastLoginTime = DateTimeOffset.Now.AddDays(-2)
                }
            };
            
            SeedUsers(users.ToArray());
            
            // 配置Mapper模拟
            MockMapper.Setup(x => x.Map<UserDto>(It.IsAny<ApplicationUser>()))
                .Returns<ApplicationUser>(user => new UserDto
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    Name = user.Name,
                    IsActive = user.IsActive,
                    LastLoginTime = user.LastLoginTime
                });
                
            // 不再模拟 UserManager，使用真实实现
            
            // 为测试用户设置密码（使用真实的 UserManager）
            var passwordHasher = new PasswordHasher<ApplicationUser>();
            var user1 = users[0];
            user1.PasswordHash = passwordHasher.HashPassword(user1, "testpassword");
            UserManager.UpdateAsync(user1).Wait();
            
            var user2 = users[1];
            user2.PasswordHash = passwordHasher.HashPassword(user2, "testpassword");
            UserManager.UpdateAsync(user2).Wait();
        }

        /// <summary>
        /// 在每个测试方法执行前自动清理数据库上下文
        /// </summary>
        protected void Setup()
        {
            ClearDbContext();
        }

        [Fact]
        public async Task GetUsersAsync_ReturnsPagedList()
        {
            // Arrange
            Setup();
            var queryDto = new UserQueryDto { Page = 1, PerPage = 10 };

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
            Setup();
            long userId = 1;
            var roles = new List<string> { "Admin" };
            
            // Act
            await _userService.AssignRolesAsync(userId, roles);
            
            // Assert
            var user = await UserManager.FindByIdAsync(userId.ToString());
            Assert.NotNull(user);
            
            var userRoles = await UserManager.GetRolesAsync(user);
            Assert.Contains("Admin", userRoles);
        }

        [Fact]
        public async Task SetActiveStatusAsync_UpdatesUserStatus()
        {
            // Arrange
            Setup();
            long userId = 1;
            bool isActive = false;
            
            // Act
            await _userService.SetActiveStatusAsync(userId, isActive);
            
            // Assert
            var user = await UserManager.FindByIdAsync(userId.ToString());
            Assert.NotNull(user);
            Assert.False(user.IsActive);
            
            // 恢复原始状态
            await _userService.SetActiveStatusAsync(userId, true);
        }

        [Fact]
        public async Task UnlockUserAsync_UnlocksUser()
        {
            // Arrange
            Setup();
            long userId = 1;
            
            // 先锁定用户
            var user = await UserManager.FindByIdAsync(userId.ToString());
            await UserManager.SetLockoutEndDateAsync(user, DateTimeOffset.Now.AddDays(1));
            
            // Act
            await _userService.UnlockUserAsync(userId);
            
            // Assert
            user = await UserManager.FindByIdAsync(userId.ToString());
            Assert.NotNull(user);
            Assert.Null(user.LockoutEnd);
        }

        [Fact]
        public async Task GetUsersByIdsAsync_ReturnsMatchingUsers()
        {
            // Arrange
            Setup();
            var userIds = new List<long> { 1, 2 };
            
            // Act
            var users = await _userService.GetUsersByIdsAsync(userIds);
            
            // Assert
            Assert.NotNull(users);
            Assert.Equal(2, users.Count);
            Assert.Contains(users, u => u.Id == 1);
            Assert.Contains(users, u => u.Id == 2);
        }
    }
} 