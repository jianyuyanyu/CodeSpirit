using CodeSpirit.Core;
using CodeSpirit.IdentityApi.Tests.TestBase;
using Microsoft.EntityFrameworkCore;

namespace CodeSpirit.IdentityApi.Tests.Seeders
{
    /// <summary>
    /// SeederService 集成测试
    /// </summary>
    public class SeederServiceIntegrationTests : SeederTestBase
    {
        [Fact]
        public async Task SeedAsync_完整的数据初始化流程_应该成功()
        {
            // Act
            await SeederService.SeedAsync();

            // Assert - 验证租户数据
            var systemTenant = await GetTenantAsync(TenantConstants.SystemTenantId);
            var defaultTenant = await GetTenantAsync(TenantConstants.DefaultTenantId);
            
            Assert.NotNull(systemTenant);
            Assert.NotNull(defaultTenant);

            // Assert - 验证系统角色
            var systemRoles = await DbContext.Roles
                .Where(r => r.TenantId == TenantConstants.SystemTenantId)
                .ToListAsync();
            
            Assert.NotEmpty(systemRoles);
            Assert.Contains(systemRoles, r => r.Name == "SystemAdmin");
            Assert.Contains(systemRoles, r => r.Name == "TenantOperator");
            Assert.Contains(systemRoles, r => r.Name == "SystemAuditor");

            // Assert - 验证业务角色
            var businessRoles = await DbContext.Roles
                .Where(r => r.TenantId == TenantConstants.DefaultTenantId)
                .ToListAsync();
            
            Assert.NotEmpty(businessRoles);
            Assert.Contains(businessRoles, r => r.Name == "Admin");

            // Assert - 验证系统用户
            var systemUsers = await DbContext.Users
                .Where(u => u.TenantId == TenantConstants.SystemTenantId)
                .ToListAsync();
            
            Assert.NotEmpty(systemUsers);
            var systemAdmin = systemUsers.FirstOrDefault(u => u.UserName == "systemadmin");
            Assert.NotNull(systemAdmin);
            Assert.NotNull(systemAdmin.PasswordHash);

            // Assert - 验证业务用户
            var businessUsers = await DbContext.Users
                .Where(u => u.TenantId == TenantConstants.DefaultTenantId)
                .ToListAsync();
            
            Assert.NotEmpty(businessUsers);
            var admin = businessUsers.FirstOrDefault(u => u.UserName == "admin");
            Assert.NotNull(admin);

            // Assert - 验证用户角色关联
            var systemAdminRole = systemRoles.First(r => r.Name == "SystemAdmin");
            var systemUserRole = await GetUserRoleAsync(systemAdmin.Id, systemAdminRole.Id);
            Assert.NotNull(systemUserRole);

            var adminRole = businessRoles.First(r => r.Name == "Admin");
            var businessUserRole = await GetUserRoleAsync(admin.Id, adminRole.Id);
            Assert.NotNull(businessUserRole);
        }

        [Fact]
        public async Task SeedAsync_重复运行完整流程_应该幂等()
        {
            // Act - 第一次运行
            await SeederService.SeedAsync();
            
            var firstRunStats = await GetDatabaseStats();

            // Act - 第二次运行
            await SeederService.SeedAsync();
            
            var secondRunStats = await GetDatabaseStats();

            // Assert - 验证数据没有重复
            Assert.Equal(firstRunStats.TenantCount, secondRunStats.TenantCount);
            Assert.Equal(firstRunStats.RoleCount, secondRunStats.RoleCount);
            Assert.Equal(firstRunStats.UserCount, secondRunStats.UserCount);
            Assert.Equal(firstRunStats.UserRoleCount, secondRunStats.UserRoleCount);
        }

        [Fact]
        public async Task SeedAsync_包含数据迁移场景_应该正确处理()
        {
            // Arrange - 创建一些旧数据
            await CreateLegacyDataAsync();

            // Act
            await SeederService.SeedAsync();

            // Assert - 验证旧数据已迁移
            var legacyRole = await DbContext.Roles
                .FirstOrDefaultAsync(r => r.Name == "LegacyRole");
            var legacyUser = await DbContext.Users
                .FirstOrDefaultAsync(u => u.UserName == "legacyuser");

            Assert.NotNull(legacyRole);
            Assert.NotNull(legacyUser);
            Assert.Equal(TenantConstants.DefaultTenantId, legacyRole.TenantId);
            Assert.Equal(TenantConstants.DefaultTenantId, legacyUser.TenantId);

            // Assert - 验证新数据也已创建
            var systemAdmin = await GetUserAsync("systemadmin", TenantConstants.SystemTenantId);
            Assert.NotNull(systemAdmin);
        }

        [Fact]
        public async Task SeedAsync_验证数据完整性_所有关联应该正确()
        {
            // Act
            await SeederService.SeedAsync();

            // Assert - 验证系统管理员的完整关联
            var systemAdmin = await GetUserAsync("systemadmin", TenantConstants.SystemTenantId);
            var systemAdminRole = await GetRoleAsync("SystemAdmin", TenantConstants.SystemTenantId);
            
            Assert.NotNull(systemAdmin);
            Assert.NotNull(systemAdminRole);

            var userRole = await GetUserRoleAsync(systemAdmin.Id, systemAdminRole.Id);
            Assert.NotNull(userRole);
            Assert.Equal(TenantConstants.SystemTenantId, userRole.TenantId);

            // Assert - 验证业务管理员的完整关联
            var admin = await GetUserAsync("admin", TenantConstants.DefaultTenantId);
            var adminRole = await GetRoleAsync("Admin", TenantConstants.DefaultTenantId);
            
            Assert.NotNull(admin);
            Assert.NotNull(adminRole);

            var businessUserRole = await GetUserRoleAsync(admin.Id, adminRole.Id);
            Assert.NotNull(businessUserRole);
            Assert.Equal(TenantConstants.DefaultTenantId, businessUserRole.TenantId);
        }

        [Fact]
        public async Task SeedAsync_验证密码设置_所有用户都应该有密码()
        {
            // Act
            await SeederService.SeedAsync();

            // Assert
            var allUsers = await DbContext.Users.ToListAsync();
            
            Assert.NotEmpty(allUsers);
            Assert.All(allUsers, user =>
            {
                Assert.NotNull(user.PasswordHash);
                Assert.NotEmpty(user.PasswordHash);
            });
        }

        [Fact]
        public async Task SeedAsync_验证租户隔离_不同租户数据应该分离()
        {
            // Act
            await SeederService.SeedAsync();

            // Assert - 验证系统租户数据
            var systemRoles = await DbContext.Roles
                .Where(r => r.TenantId == TenantConstants.SystemTenantId)
                .Select(r => r.Name)
                .ToListAsync();
            
            var systemUsers = await DbContext.Users
                .Where(u => u.TenantId == TenantConstants.SystemTenantId)
                .Select(u => u.UserName)
                .ToListAsync();

            Assert.Contains("SystemAdmin", systemRoles);
            Assert.Contains("TenantOperator", systemRoles);
            Assert.Contains("SystemAuditor", systemRoles);
            Assert.Contains("systemadmin", systemUsers);

            // Assert - 验证默认租户数据
            var defaultRoles = await DbContext.Roles
                .Where(r => r.TenantId == TenantConstants.DefaultTenantId)
                .Select(r => r.Name)
                .ToListAsync();
            
            var defaultUsers = await DbContext.Users
                .Where(u => u.TenantId == TenantConstants.DefaultTenantId)
                .Select(u => u.UserName)
                .ToListAsync();

            Assert.Contains("Admin", defaultRoles);
            Assert.Contains("admin", defaultUsers);

            // Assert - 验证没有交叉污染
            Assert.DoesNotContain("SystemAdmin", defaultRoles);
            Assert.DoesNotContain("Admin", systemRoles);
            Assert.DoesNotContain("systemadmin", defaultUsers);
            Assert.DoesNotContain("admin", systemUsers);
        }

        [Fact]
        public async Task SeedAsync_性能测试_应该在合理时间内完成()
        {
            // Arrange
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act
            await SeederService.SeedAsync();

            // Assert
            stopwatch.Stop();
            
            // 数据初始化应该在10秒内完成（对于内存数据库）
            Assert.True(stopwatch.ElapsedMilliseconds < 10000, 
                $"数据初始化耗时过长: {stopwatch.ElapsedMilliseconds}ms");

            // 验证数据完整性
            var stats = await GetDatabaseStats();
            Assert.True(stats.TenantCount >= 2);
            Assert.True(stats.RoleCount >= 4); // 至少3个系统角色 + 1个业务角色
            Assert.True(stats.UserCount >= 2); // 至少1个系统用户 + 1个业务用户
            Assert.True(stats.UserRoleCount >= 2); // 至少2个用户角色关联
        }

        /// <summary>
        /// 获取数据库统计信息
        /// </summary>
        private async Task<DatabaseStats> GetDatabaseStats()
        {
            return new DatabaseStats
            {
                TenantCount = await DbContext.Tenants.CountAsync(),
                RoleCount = await DbContext.Roles.CountAsync(),
                UserCount = await DbContext.Users.CountAsync(),
                UserRoleCount = await DbContext.UserRoles.CountAsync()
            };
        }

        /// <summary>
        /// 创建遗留数据用于测试迁移
        /// </summary>
        private async Task CreateLegacyDataAsync()
        {
            var legacyRole = new CodeSpirit.IdentityApi.Data.Models.ApplicationRole
            {
                Id = 9999L,
                Name = "LegacyRole",
                NormalizedName = "LEGACYROLE",
                Description = "遗留角色",
                TenantId = null, // 没有租户ID
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = 1L
            };

            var legacyUser = new CodeSpirit.IdentityApi.Data.Models.ApplicationUser
            {
                Id = 9999L,
                UserName = "legacyuser",
                NormalizedUserName = "LEGACYUSER",
                Email = "legacyuser@example.com",
                Name = "遗留用户",
                TenantId = null, // 没有租户ID
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = 1L
            };

            DbContext.Roles.Add(legacyRole);
            DbContext.Users.Add(legacyUser);
            await DbContext.SaveChangesAsync();
        }

        /// <summary>
        /// 数据库统计信息
        /// </summary>
        private class DatabaseStats
        {
            public int TenantCount { get; set; }
            public int RoleCount { get; set; }
            public int UserCount { get; set; }
            public int UserRoleCount { get; set; }
        }
    }
} 