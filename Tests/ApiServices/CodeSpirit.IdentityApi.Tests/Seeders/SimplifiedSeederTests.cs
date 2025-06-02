using CodeSpirit.Core;
using CodeSpirit.IdentityApi.Data.Seeders;
using CodeSpirit.IdentityApi.Tests.TestBase;
using Microsoft.EntityFrameworkCore;

namespace CodeSpirit.IdentityApi.Tests.Seeders
{
    /// <summary>
    /// 简化的种子数据测试
    /// 专注于验证核心功能
    /// </summary>
    public class SimplifiedSeederTests : SeederTestBase
    {
        [Fact]
        public async Task GetSystemRoles_应该返回预定义角色()
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

            // 验证所有角色都是系统角色
            Assert.All(systemRoles, role => 
            {
                Assert.Equal(TenantConstants.SystemTenantId, role.TenantId);
                Assert.True(role.IsSystemRole);
            });
        }

        [Fact]
        public async Task GetBusinessRoles_应该返回预定义角色()
        {
            // Act
            var businessRoles = RoleSeederService.GetBusinessRoles();

            // Assert
            Assert.NotNull(businessRoles);
            Assert.True(businessRoles.Count >= 1);

            var adminRole = businessRoles.FirstOrDefault(r => r.Name == "Admin");
            Assert.NotNull(adminRole);
            Assert.Equal(TenantConstants.DefaultTenantId, adminRole.TenantId);
            Assert.False(adminRole.IsSystemRole);
        }

        [Fact]
        public async Task GetSystemUsers_应该返回预定义用户()
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
        public async Task GetBusinessUsers_应该返回预定义用户()
        {
            // Act
            var businessUsers = UserSeederService.GetBusinessUsers();

            // Assert
            Assert.NotNull(businessUsers);
            Assert.Single(businessUsers);

            var adminUser = businessUsers.First();
            Assert.Equal("admin", adminUser.UserName);
            Assert.Equal("admin@example.com", adminUser.Email);
            Assert.Equal(TenantConstants.DefaultTenantId, adminUser.TenantId);
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
            Assert.True(role.IsActive);
        }

        [Fact]
        public async Task EnsureUserExistsAsync_创建新用户_应该成功()
        {
            // Arrange
            var userName = "testuser";
            var email = "testuser@test.local";
            var displayName = "测试用户";
            var password = "TestPassword123!";
            var tenantId = TenantConstants.SystemTenantId;

            // Act
            var user = await UserSeederService.EnsureUserExistsAsync(userName, email, displayName, password, tenantId);

            // Assert
            Assert.NotNull(user);
            Assert.Equal(userName, user.UserName);
            Assert.Equal(email, user.Email);
            Assert.Equal(tenantId, user.TenantId);
            Assert.True(user.IsActive);
        }

        [Fact]
        public async Task CreateTenant_应该成功创建租户()
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
            Assert.True(tenant.IsActive);
        }

        [Fact]
        public async Task 数据库操作_应该支持基本CRUD()
        {
            // Arrange - 创建租户
            var tenant = await CreateTenantAsync("crud-test", "CRUD测试");

            // Act & Assert - 验证租户已创建
            var savedTenant = await GetTenantAsync("crud-test");
            Assert.NotNull(savedTenant);
            Assert.Equal("CRUD测试", savedTenant.Name);

            // Act - 创建角色
            var role = await RoleSeederService.EnsureRoleExistsAsync("TestRole", "测试角色", "crud-test");
            Assert.NotNull(role);

            // Act - 创建用户
            var user = await UserSeederService.EnsureUserExistsAsync("testuser", "test@test.com", "测试用户", "Password123!", "crud-test");
            Assert.NotNull(user);

            // 保存更改
            await DbContext.SaveChangesAsync();

            // Assert - 验证数据库中的数据
            var dbRole = await GetRoleAsync("TestRole", "crud-test");
            var dbUser = await GetUserAsync("testuser", "crud-test");
            
            Assert.NotNull(dbRole);
            Assert.NotNull(dbUser);
            Assert.Equal(role.Id, dbRole.Id);
            Assert.Equal(user.Id, dbUser.Id);
        }

        [Fact]
        public async Task 多租户隔离_应该正确工作()
        {
            // Arrange
            var roleName = "Admin";
            var userName = "admin";

            // Act - 在不同租户中创建相同名称的角色和用户
            var systemRole = await RoleSeederService.EnsureRoleExistsAsync(roleName, "系统管理员", TenantConstants.SystemTenantId);
            var businessRole = await RoleSeederService.EnsureRoleExistsAsync(roleName, "业务管理员", TenantConstants.DefaultTenantId);

            var systemUser = await UserSeederService.EnsureUserExistsAsync(userName, "admin@system.local", "系统管理员", "Password123!", TenantConstants.SystemTenantId);
            var businessUser = await UserSeederService.EnsureUserExistsAsync(userName, "admin@business.local", "业务管理员", "Password123!", TenantConstants.DefaultTenantId);

            // Assert - 验证不同租户的实体是独立的
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
    }
} 