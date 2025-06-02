using CodeSpirit.Core;
using CodeSpirit.IdentityApi.Data.Seeders;
using CodeSpirit.IdentityApi.Tests.TestBase;
using Microsoft.EntityFrameworkCore;

namespace CodeSpirit.IdentityApi.Tests.Seeders
{
    /// <summary>
    /// 统一角色种子数据服务测试
    /// </summary>
    public class UnifiedRoleSeederServiceTests : SeederTestBase
    {
        [Fact]
        public async Task GetSystemRoles_应该返回预定义的系统角色()
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
        public async Task GetBusinessRoles_应该返回预定义的业务角色()
        {
            // Act
            var businessRoles = RoleSeederService.GetBusinessRoles();

            // Assert
            Assert.NotNull(businessRoles);
            Assert.True(businessRoles.Count >= 1); // 至少有Admin角色

            var adminRole = businessRoles.FirstOrDefault(r => r.Name == "Admin");
            Assert.NotNull(adminRole);
            Assert.Equal(TenantConstants.DefaultTenantId, adminRole.TenantId);
            Assert.False(adminRole.IsSystemRole);
        }

        [Fact]
        public async Task EnsureRoleExistsAsync_创建新的系统角色_应该成功()
        {
            // Arrange
            var roleName = "TestSystemRole";
            var description = "测试系统角色";
            var tenantId = TenantConstants.SystemTenantId;

            // Act
            var role = await RoleSeederService.EnsureRoleExistsAsync(roleName, description, tenantId);

            // Assert
            Assert.NotNull(role);
            Assert.Equal(roleName, role.Name);
            Assert.Equal(roleName.ToUpper(), role.NormalizedName);
            Assert.Equal(description, role.Description);
            Assert.Equal(tenantId, role.TenantId);
            Assert.True(role.IsActive);

            // 验证数据库中是否存在
            var dbRole = await GetRoleAsync(roleName, tenantId);
            Assert.NotNull(dbRole);
            Assert.Equal(role.Id, dbRole.Id);
        }

        [Fact]
        public async Task EnsureRoleExistsAsync_创建新的业务角色_应该成功()
        {
            // Arrange
            var roleName = "TestBusinessRole";
            var description = "测试业务角色";
            var tenantId = TenantConstants.DefaultTenantId;

            // Act
            var role = await RoleSeederService.EnsureRoleExistsAsync(roleName, description, tenantId);

            // Assert
            Assert.NotNull(role);
            Assert.Equal(roleName, role.Name);
            Assert.Equal(roleName.ToUpper(), role.NormalizedName);
            Assert.Equal(description, role.Description);
            Assert.Equal(tenantId, role.TenantId);
            Assert.True(role.IsActive);

            // 验证数据库中是否存在
            var dbRole = await GetRoleAsync(roleName, tenantId);
            Assert.NotNull(dbRole);
            Assert.Equal(role.Id, dbRole.Id);
        }

        [Fact]
        public async Task EnsureRoleExistsAsync_角色已存在_应该返回现有角色()
        {
            // Arrange
            var roleName = "ExistingRole";
            var description = "现有角色";
            var tenantId = TenantConstants.SystemTenantId;

            // 首次创建
            var firstRole = await RoleSeederService.EnsureRoleExistsAsync(roleName, description, tenantId);

            // Act - 再次调用
            var secondRole = await RoleSeederService.EnsureRoleExistsAsync(roleName, description, tenantId);

            // Assert
            Assert.NotNull(secondRole);
            Assert.Equal(firstRole.Id, secondRole.Id);
            Assert.Equal(firstRole.Name, secondRole.Name);

            // 验证数据库中只有一个角色
            var rolesInDb = await DbContext.Roles
                .Where(r => r.Name == roleName && r.TenantId == tenantId)
                .CountAsync();
            Assert.Equal(1, rolesInDb);
        }

        [Fact]
        public async Task CreateRolesBatchAsync_批量创建系统角色_应该成功()
        {
            // Arrange
            var systemRoles = RoleSeederService.GetSystemRoles();

            // Act
            var createdRoles = await RoleSeederService.CreateRolesBatchAsync(systemRoles);

            // Assert
            Assert.NotNull(createdRoles);
            Assert.Equal(systemRoles.Count, createdRoles.Count);

            // 验证每个角色都已创建
            foreach (var expectedRole in systemRoles)
            {
                var createdRole = createdRoles.FirstOrDefault(r => r.Name == expectedRole.Name);
                Assert.NotNull(createdRole);
                Assert.Equal(expectedRole.TenantId, createdRole.TenantId);

                // 验证数据库中存在
                var dbRole = await GetRoleAsync(expectedRole.Name, expectedRole.TenantId);
                Assert.NotNull(dbRole);
            }
        }

        [Fact]
        public async Task CreateRolesBatchAsync_批量创建业务角色_应该成功()
        {
            // Arrange
            var businessRoles = RoleSeederService.GetBusinessRoles();

            // Act
            var createdRoles = await RoleSeederService.CreateRolesBatchAsync(businessRoles);

            // Assert
            Assert.NotNull(createdRoles);
            Assert.Equal(businessRoles.Count, createdRoles.Count);

            // 验证每个角色都已创建
            foreach (var expectedRole in businessRoles)
            {
                var createdRole = createdRoles.FirstOrDefault(r => r.Name == expectedRole.Name);
                Assert.NotNull(createdRole);
                Assert.Equal(expectedRole.TenantId, createdRole.TenantId);

                // 验证数据库中存在
                var dbRole = await GetRoleAsync(expectedRole.Name, expectedRole.TenantId);
                Assert.NotNull(dbRole);
            }
        }

        [Fact]
        public async Task CreateRolesBatchAsync_重复运行_应该幂等()
        {
            // Arrange
            var systemRoles = RoleSeederService.GetSystemRoles();

            // Act - 第一次运行
            var firstRun = await RoleSeederService.CreateRolesBatchAsync(systemRoles);
            
            // Act - 第二次运行
            var secondRun = await RoleSeederService.CreateRolesBatchAsync(systemRoles);

            // Assert
            Assert.Equal(firstRun.Count, secondRun.Count);

            // 验证数据库中没有重复数据
            for (int i = 0; i < systemRoles.Count; i++)
            {
                var roleName = systemRoles[i].Name;
                var tenantId = systemRoles[i].TenantId;
                
                var rolesCount = await DbContext.Roles
                    .Where(r => r.Name == roleName && r.TenantId == tenantId)
                    .CountAsync();
                    
                Assert.Equal(1, rolesCount);
            }
        }

        [Fact]
        public async Task EnsureRoleExistsAsync_不同租户相同角色名_应该可以共存()
        {
            // Arrange
            var roleName = "Admin";
            var description = "管理员";

            // Act
            var systemRole = await RoleSeederService.EnsureRoleExistsAsync(
                roleName, description, TenantConstants.SystemTenantId);
            var businessRole = await RoleSeederService.EnsureRoleExistsAsync(
                roleName, description, TenantConstants.DefaultTenantId);

            // Assert
            Assert.NotNull(systemRole);
            Assert.NotNull(businessRole);
            Assert.NotEqual(systemRole.Id, businessRole.Id);
            Assert.Equal(TenantConstants.SystemTenantId, systemRole.TenantId);
            Assert.Equal(TenantConstants.DefaultTenantId, businessRole.TenantId);

            // 验证数据库中存在两个不同的角色
            var totalRoles = await DbContext.Roles
                .Where(r => r.Name == roleName)
                .CountAsync();
            Assert.Equal(2, totalRoles);
        }
    }
} 