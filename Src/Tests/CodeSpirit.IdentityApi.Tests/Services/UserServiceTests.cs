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
        private readonly IIdGenerator _idGenerator;

        public UserServiceTests()
            : base()
        {
            // 设置额外依赖
            _idGenerator = new SnowflakeIdGenerator();
            
            // 初始化UserService
            _userService = new UserService(
                UserRepository,
                Mapper,
                UserManager,
                RoleManager,
                MockUserServiceLogger.Object,
                MockHttpContextAccessor.Object,
                _idGenerator,
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
                    LastLoginTime = DateTimeOffset.Now.AddDays(-1),
                    SecurityStamp = Guid.NewGuid().ToString(),
                    LockoutEnabled = true, // 启用锁定功能
                    LockoutEnd = DateTimeOffset.Now.AddDays(1) // 锁定用户
                },
                new ApplicationUser
                {
                    Id = 2,
                    UserName = "testuser2",
                    Email = "test2@example.com",
                    IsActive = true,
                    Name = "Test User 2",
                    LastLoginTime = DateTimeOffset.Now.AddDays(-2),
                    SecurityStamp = Guid.NewGuid().ToString()
                }
            };
            
            SeedUsers(users.ToArray());
            
            // 使用真实映射，不再模拟Mapper
            
            // 为测试用户设置密码（使用真实的 UserManager）
            var passwordHasher = new PasswordHasher<ApplicationUser>();
            
            foreach (var user in users)
            {
                var dbUser = UserManager.FindByIdAsync(user.Id.ToString()).Result;
                dbUser.PasswordHash = passwordHasher.HashPassword(dbUser, "testpassword");
                UserManager.UpdateAsync(dbUser).Wait();
            }
            
            // 添加测试角色
            if (!RoleManager.RoleExistsAsync("TestRole").Result)
            {
                var role = new ApplicationRole
                {
                    Name = "TestRole",
                    Description = "Test Role Description"
                };
                RoleManager.CreateAsync(role).Wait();
            }
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
            var userId = 1L;
            var roles = new List<string> { "TestRole" };
            
            // Act
            await _userService.AssignRolesAsync(userId, roles);
            
            // Assert
            var user = await UserManager.FindByIdAsync(userId.ToString());
            var userRoles = await UserManager.GetRolesAsync(user);
            Assert.Contains("TestRole", userRoles);
        }

        [Fact]
        public async Task SetActiveStatusAsync_UpdatesUserStatus()
        {
            // Arrange
            Setup();
            var userId = 1L;
            
            // Act
            await _userService.SetActiveStatusAsync(userId, false);
            
            // Assert
            var user = await UserManager.FindByIdAsync(userId.ToString());
            Assert.False(user.IsActive);
        }

        [Fact]
        public async Task UnlockUserAsync_UnlocksUser()
        {
            // Arrange
            Setup();
            var userId = 1L;
            
            // 确保用户被锁定
            var user = await UserManager.FindByIdAsync(userId.ToString());
            Assert.NotNull(user.LockoutEnd); // 确认用户被锁定
            
            // Act
            await _userService.UnlockUserAsync(userId);
            
            // Assert
            user = await UserManager.FindByIdAsync(userId.ToString());
            Assert.Null(user.LockoutEnd); // 确认锁定已解除
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