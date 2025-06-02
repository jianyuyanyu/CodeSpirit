using CodeSpirit.Core;
using CodeSpirit.IdentityApi.Data.Seeders;
using CodeSpirit.IdentityApi.Tests.TestBase;
using Microsoft.EntityFrameworkCore;

namespace CodeSpirit.IdentityApi.Tests.Seeders
{
    /// <summary>
    /// 统一用户种子数据服务测试
    /// </summary>
    public class UnifiedUserSeederServiceTests : SeederTestBase
    {
        [Fact]
        public async Task GetSystemUsers_应该返回预定义的系统用户()
        {
            // Act
            var systemUsers = UserSeederService.GetSystemUsers();

            // Assert
            Assert.NotNull(systemUsers);
            Assert.Equal(1, systemUsers.Count);

            var systemAdmin = systemUsers.First();
            Assert.Equal("systemadmin", systemAdmin.UserName);
            Assert.Equal("systemadmin@system.local", systemAdmin.Email);
            Assert.Equal("系统管理员", systemAdmin.DisplayName);
            Assert.Equal(TenantConstants.SystemTenantId, systemAdmin.TenantId);
            Assert.True(systemAdmin.IsSystemUser);
            Assert.Contains("SystemAdmin", systemAdmin.Roles);
        }

        [Fact]
        public async Task GetBusinessUsers_应该返回预定义的业务用户()
        {
            // Act
            var businessUsers = UserSeederService.GetBusinessUsers();

            // Assert
            Assert.NotNull(businessUsers);
            Assert.True(businessUsers.Count >= 1);

            var adminUser = businessUsers.FirstOrDefault(u => u.UserName == "admin");
            Assert.NotNull(adminUser);
            Assert.Equal("admin@example.com", adminUser.Email);
            Assert.Equal(TenantConstants.DefaultTenantId, adminUser.TenantId);
            Assert.False(adminUser.IsSystemUser);
            Assert.Contains("Admin", adminUser.Roles);
        }

        [Fact]
        public async Task EnsureUserExistsAsync_创建新的系统用户_应该成功()
        {
            // Arrange
            var userName = "testsystemuser";
            var email = "testsystemuser@system.local";
            var displayName = "测试系统用户";
            var password = "TestPassword123!";
            var tenantId = TenantConstants.SystemTenantId;

            // Act
            var user = await UserSeederService.EnsureUserExistsAsync(
                userName, email, displayName, password, tenantId);

            // Assert
            Assert.NotNull(user);
            Assert.Equal(userName, user.UserName);
            Assert.Equal(userName.ToUpper(), user.NormalizedUserName);
            Assert.Equal(email, user.Email);
            Assert.Equal(email.ToUpper(), user.NormalizedEmail);
            Assert.Equal(displayName, user.Name);
            Assert.Equal(tenantId, user.TenantId);
            Assert.True(user.IsActive);
            Assert.True(user.EmailConfirmed);
            Assert.NotNull(user.PasswordHash);

            // 验证数据库中是否存在
            var dbUser = await GetUserAsync(userName, tenantId);
            Assert.NotNull(dbUser);
            Assert.Equal(user.Id, dbUser.Id);
        }

        [Fact]
        public async Task EnsureUserExistsAsync_创建新的业务用户_应该成功()
        {
            // Arrange
            var userName = "testbusinessuser";
            var email = "testbusinessuser@example.com";
            var displayName = "测试业务用户";
            var password = "TestPassword123!";
            var tenantId = TenantConstants.DefaultTenantId;

            // Act
            var user = await UserSeederService.EnsureUserExistsAsync(
                userName, email, displayName, password, tenantId);

            // Assert
            Assert.NotNull(user);
            Assert.Equal(userName, user.UserName);
            Assert.Equal(email, user.Email);
            Assert.Equal(displayName, user.Name);
            Assert.Equal(tenantId, user.TenantId);
            Assert.True(user.IsActive);
            Assert.True(user.EmailConfirmed);

            // 验证数据库中是否存在
            var dbUser = await GetUserAsync(userName, tenantId);
            Assert.NotNull(dbUser);
            Assert.Equal(user.Id, dbUser.Id);
        }

        [Fact]
        public async Task EnsureUserExistsAsync_用户已存在_应该返回现有用户()
        {
            // Arrange
            var userName = "existinguser";
            var email = "existinguser@example.com";
            var displayName = "现有用户";
            var password = "TestPassword123!";
            var tenantId = TenantConstants.SystemTenantId;

            // 首次创建
            var firstUser = await UserSeederService.EnsureUserExistsAsync(
                userName, email, displayName, password, tenantId);

            // Act - 再次调用
            var secondUser = await UserSeederService.EnsureUserExistsAsync(
                userName, email, displayName, password, tenantId);

            // Assert
            Assert.NotNull(secondUser);
            Assert.Equal(firstUser.Id, secondUser.Id);
            Assert.Equal(firstUser.UserName, secondUser.UserName);

            // 验证数据库中只有一个用户
            var usersInDb = await DbContext.Users
                .Where(u => u.UserName == userName && u.TenantId == tenantId)
                .CountAsync();
            Assert.Equal(1, usersInDb);
        }

        [Fact]
        public async Task CreateUsersBatchAsync_批量创建系统用户_应该成功()
        {
            // Arrange
            var systemUsers = UserSeederService.GetSystemUsers();

            // Act
            var createdUsers = await UserSeederService.CreateUsersBatchAsync(systemUsers);

            // Assert
            Assert.NotNull(createdUsers);
            Assert.Equal(systemUsers.Count, createdUsers.Count);

            // 验证每个用户都已创建
            foreach (var expectedUser in systemUsers)
            {
                var createdUser = createdUsers.FirstOrDefault(u => u.UserName == expectedUser.UserName);
                Assert.NotNull(createdUser);
                Assert.Equal(expectedUser.TenantId, createdUser.TenantId);

                // 验证数据库中存在
                var dbUser = await GetUserAsync(expectedUser.UserName, expectedUser.TenantId);
                Assert.NotNull(dbUser);
            }
        }

        [Fact]
        public async Task CreateUsersBatchAsync_批量创建业务用户_应该成功()
        {
            // Arrange
            var businessUsers = UserSeederService.GetBusinessUsers();

            // Act
            var createdUsers = await UserSeederService.CreateUsersBatchAsync(businessUsers);

            // Assert
            Assert.NotNull(createdUsers);
            Assert.Equal(businessUsers.Count, createdUsers.Count);

            // 验证每个用户都已创建
            foreach (var expectedUser in businessUsers)
            {
                var createdUser = createdUsers.FirstOrDefault(u => u.UserName == expectedUser.UserName);
                Assert.NotNull(createdUser);
                Assert.Equal(expectedUser.TenantId, createdUser.TenantId);

                // 验证数据库中存在
                var dbUser = await GetUserAsync(expectedUser.UserName, expectedUser.TenantId);
                Assert.NotNull(dbUser);
            }
        }

        [Fact]
        public async Task EnsureUserRoleExistsAsync_创建用户角色关联_应该成功()
        {
            // Arrange
            var userId = 100L;
            var roleId = 200L;
            var tenantId = TenantConstants.SystemTenantId;

            // Act
            await UserSeederService.EnsureUserRoleExistsAsync(userId, roleId, tenantId);

            // Assert
            var userRole = await GetUserRoleAsync(userId, roleId);
            Assert.NotNull(userRole);
            Assert.Equal(userId, userRole.UserId);
            Assert.Equal(roleId, userRole.RoleId);
            Assert.Equal(tenantId, userRole.TenantId);
        }

        [Fact]
        public async Task EnsureUserRoleExistsAsync_重复创建用户角色关联_应该幂等()
        {
            // Arrange
            var userId = 100L;
            var roleId = 200L;
            var tenantId = TenantConstants.SystemTenantId;

            // Act - 第一次创建
            await UserSeederService.EnsureUserRoleExistsAsync(userId, roleId, tenantId);
            
            // Act - 第二次创建
            await UserSeederService.EnsureUserRoleExistsAsync(userId, roleId, tenantId);

            // Assert
            var userRoles = await DbContext.UserRoles
                .Where(ur => ur.UserId == userId && ur.RoleId == roleId)
                .CountAsync();
            Assert.Equal(1, userRoles);
        }

        [Fact]
        public async Task CreateUsersBatchAsync_重复运行_应该幂等()
        {
            // Arrange
            var systemUsers = UserSeederService.GetSystemUsers();

            // Act - 第一次运行
            var firstRun = await UserSeederService.CreateUsersBatchAsync(systemUsers);
            
            // Act - 第二次运行
            var secondRun = await UserSeederService.CreateUsersBatchAsync(systemUsers);

            // Assert
            Assert.Equal(firstRun.Count, secondRun.Count);

            // 验证数据库中没有重复数据
            foreach (var userDefinition in systemUsers)
            {
                var usersCount = await DbContext.Users
                    .Where(u => u.UserName == userDefinition.UserName && u.TenantId == userDefinition.TenantId)
                    .CountAsync();
                    
                Assert.Equal(1, usersCount);
            }
        }

        [Fact]
        public async Task EnsureUserExistsAsync_不同租户相同用户名_应该可以共存()
        {
            // Arrange
            var userName = "admin";
            var email1 = "admin@system.local";
            var email2 = "admin@business.local";
            var displayName = "管理员";
            var password = "TestPassword123!";

            // Act
            var systemUser = await UserSeederService.EnsureUserExistsAsync(
                userName, email1, displayName, password, TenantConstants.SystemTenantId);
            var businessUser = await UserSeederService.EnsureUserExistsAsync(
                userName, email2, displayName, password, TenantConstants.DefaultTenantId);

            // Assert
            Assert.NotNull(systemUser);
            Assert.NotNull(businessUser);
            Assert.NotEqual(systemUser.Id, businessUser.Id);
            Assert.Equal(TenantConstants.SystemTenantId, systemUser.TenantId);
            Assert.Equal(TenantConstants.DefaultTenantId, businessUser.TenantId);

            // 验证数据库中存在两个不同的用户
            var totalUsers = await DbContext.Users
                .Where(u => u.UserName == userName)
                .CountAsync();
            Assert.Equal(2, totalUsers);
        }

        [Fact]
        public async Task EnsureUserExistsAsync_用户没有密码_应该设置密码()
        {
            // Arrange
            var userName = "userWithoutPassword";
            var email = "userWithoutPassword@example.com";
            var displayName = "无密码用户";
            var password = "NewPassword123!";
            var tenantId = TenantConstants.SystemTenantId;

            // 先创建一个没有密码的用户
            var user = new CodeSpirit.IdentityApi.Data.Models.ApplicationUser
            {
                Id = 999L,
                UserName = userName,
                Email = email,
                Name = displayName,
                TenantId = tenantId,
                IsActive = true,
                PasswordHash = null // 没有密码
            };
            DbContext.Users.Add(user);
            await DbContext.SaveChangesAsync();

            // Act
            var updatedUser = await UserSeederService.EnsureUserExistsAsync(
                userName, email, displayName, password, tenantId);

            // Assert
            Assert.NotNull(updatedUser);
            Assert.NotNull(updatedUser.PasswordHash);
            Assert.NotEmpty(updatedUser.PasswordHash);
        }
    }
} 