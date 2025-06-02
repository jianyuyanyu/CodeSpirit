using CodeSpirit.Core;
using CodeSpirit.IdentityApi.Data.Seeders;
using CodeSpirit.IdentityApi.Tests.TestBase;
using Microsoft.EntityFrameworkCore;

namespace CodeSpirit.IdentityApi.Tests.Seeders
{
    /// <summary>
    /// 核心种子数据测试
    /// 专注于验证最基本的功能
    /// </summary>
    public class CoreSeederTests : SeederTestBase
    {
        [Fact]
        public void GetSystemRoles_应该返回正确数量的角色()
        {
            // Act
            var systemRoles = RoleSeederService.GetSystemRoles();

            // Assert
            Assert.NotNull(systemRoles);
            Assert.Equal(3, systemRoles.Count);
            
            var roleNames = systemRoles.Select(r => r.Name).ToList();
            Assert.Contains("SystemAdmin", roleNames);
            Assert.Contains("TenantOperator", roleNames);
            Assert.Contains("SystemAuditor", roleNames);
        }

        [Fact]
        public void GetBusinessRoles_应该返回正确数量的角色()
        {
            // Act
            var businessRoles = RoleSeederService.GetBusinessRoles();

            // Assert
            Assert.NotNull(businessRoles);
            Assert.True(businessRoles.Count >= 1);
            
            var adminRole = businessRoles.FirstOrDefault(r => r.Name == "Admin");
            Assert.NotNull(adminRole);
            Assert.Equal(TenantConstants.DefaultTenantId, adminRole.TenantId);
        }

        [Fact]
        public void GetSystemUsers_应该返回系统管理员()
        {
            // Act
            var systemUsers = UserSeederService.GetSystemUsers();

            // Assert
            Assert.NotNull(systemUsers);
            Assert.Single(systemUsers);

            var systemAdmin = systemUsers.First();
            Assert.Equal("systemadmin", systemAdmin.UserName);
            Assert.Equal("systemadmin@system.local", systemAdmin.Email);
            Assert.Equal(TenantConstants.SystemTenantId, systemAdmin.TenantId);
        }

        [Fact]
        public void GetBusinessUsers_应该返回业务管理员()
        {
            // Act
            var businessUsers = UserSeederService.GetBusinessUsers();

            // Assert
            Assert.NotNull(businessUsers);
            Assert.Single(businessUsers);

            var admin = businessUsers.First();
            Assert.Equal("admin", admin.UserName);
            Assert.Equal("admin@example.com", admin.Email);
            Assert.Equal(TenantConstants.DefaultTenantId, admin.TenantId);
        }

        [Fact]
        public async Task EnsureRoleExistsAsync_创建新角色_应该成功()
        {
            // Arrange
            var roleName = "TestRole";
            var description = "测试角色";
            var tenantId = TenantConstants.SystemTenantId;

            // Act
            var role = await RoleSeederService.EnsureRoleExistsAsync(roleName, description, tenantId);

            // Assert
            Assert.NotNull(role);
            Assert.Equal(roleName, role.Name);
            Assert.Equal(description, role.Description);
            Assert.Equal(tenantId, role.TenantId);
        }

        [Fact]
        public async Task EnsureUserExistsAsync_创建新用户_应该成功()
        {
            // Arrange
            var userName = "testuser";
            var email = "testuser@test.com";
            var displayName = "测试用户";
            var password = "TestPassword123!";
            var tenantId = TenantConstants.SystemTenantId;

            // Act
            var user = await UserSeederService.EnsureUserExistsAsync(
                userName, email, displayName, password, tenantId);

            // Assert
            Assert.NotNull(user);
            Assert.Equal(userName, user.UserName);
            Assert.Equal(email, user.Email);
            Assert.Equal(displayName, user.Name);
            Assert.Equal(tenantId, user.TenantId);
        }

        [Fact]
        public async Task 租户创建_应该成功()
        {
            // Arrange
            var tenantId = "test-tenant";
            var name = "测试租户";

            // Act
            var tenant = await CreateTenantAsync(tenantId, name);

            // Assert
            Assert.NotNull(tenant);
            Assert.Equal(tenantId, tenant.TenantId);
            Assert.Equal(name, tenant.Name);
        }

        [Fact]
        public async Task 多租户隔离_相同名称实体应该可以在不同租户中共存()
        {
            // Arrange
            var roleName = "Manager";
            var userName = "manager";

            // Act - 在系统租户中创建
            var systemRole = await RoleSeederService.EnsureRoleExistsAsync(
                roleName, "系统管理者", TenantConstants.SystemTenantId);
            var systemUser = await UserSeederService.EnsureUserExistsAsync(
                userName, "manager@system.local", "系统管理者", "Password123!", TenantConstants.SystemTenantId);

            // Act - 在默认租户中创建相同名称的实体
            var businessRole = await RoleSeederService.EnsureRoleExistsAsync(
                roleName, "业务管理者", TenantConstants.DefaultTenantId);
            var businessUser = await UserSeederService.EnsureUserExistsAsync(
                userName, "manager@business.local", "业务管理者", "Password123!", TenantConstants.DefaultTenantId);

            // Assert - 验证实体独立存在
            Assert.NotNull(systemRole);
            Assert.NotNull(businessRole);
            Assert.NotNull(systemUser);
            Assert.NotNull(businessUser);

            Assert.NotEqual(systemRole.Id, businessRole.Id);
            Assert.NotEqual(systemUser.Id, businessUser.Id);

            Assert.Equal(TenantConstants.SystemTenantId, systemRole.TenantId);
            Assert.Equal(TenantConstants.DefaultTenantId, businessRole.TenantId);
            Assert.Equal(TenantConstants.SystemTenantId, systemUser.TenantId);
            Assert.Equal(TenantConstants.DefaultTenantId, businessUser.TenantId);
        }

        [Fact]
        public async Task 数据持久化_应该能够正确创建对象()
        {
            // Arrange
            var roleName = "PersistentRole";
            var userName = "persistentuser";

            // Act - 创建数据
            var role = await RoleSeederService.EnsureRoleExistsAsync(
                roleName, "持久化角色", TenantConstants.SystemTenantId);
            var user = await UserSeederService.EnsureUserExistsAsync(
                userName, "persistent@test.com", "持久化用户", "Password123!", TenantConstants.SystemTenantId);

            // Assert - 验证对象创建成功
            Assert.NotNull(role);
            Assert.NotNull(user);
            Assert.Equal(roleName, role.Name);
            Assert.Equal(userName, user.UserName);
            Assert.Equal(TenantConstants.SystemTenantId, role.TenantId);
            Assert.Equal(TenantConstants.SystemTenantId, user.TenantId);
            Assert.True(role.Id > 0); // 验证ID已分配
            Assert.True(user.Id > 0); // 验证ID已分配
        }
    }
} 