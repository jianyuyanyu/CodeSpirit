using CodeSpirit.Core;
using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.IdentityApi.Tests.TestBase;
using CodeSpirit.MultiTenant.Models;
using Microsoft.EntityFrameworkCore;

namespace CodeSpirit.IdentityApi.Tests.Seeders
{
    /// <summary>
    /// 租户种子数据服务测试
    /// </summary>
    public class TenantSeederTests : SeederTestBase
    {
        [Fact]
        public async Task SeedAsync_空数据库_应该创建完整的初始数据()
        {
            // Act
            await TenantSeeder.SeedAsync();

            // Assert - 验证租户创建
            var systemTenant = await GetTenantAsync(TenantConstants.SystemTenantId);
            var defaultTenant = await GetTenantAsync(TenantConstants.DefaultTenantId);
            
            Assert.NotNull(systemTenant);
            Assert.NotNull(defaultTenant);
            Assert.Equal(TenantConstants.SystemTenantName, systemTenant.Name);
            Assert.Equal(TenantConstants.DefaultTenantName, defaultTenant.Name);

            // Assert - 验证系统角色创建
            var systemAdminRole = await GetRoleAsync("SystemAdmin", TenantConstants.SystemTenantId);
            var tenantOperatorRole = await GetRoleAsync("TenantOperator", TenantConstants.SystemTenantId);
            var systemAuditorRole = await GetRoleAsync("SystemAuditor", TenantConstants.SystemTenantId);
            
            Assert.NotNull(systemAdminRole);
            Assert.NotNull(tenantOperatorRole);
            Assert.NotNull(systemAuditorRole);

            // Assert - 验证系统用户创建
            var systemAdminUser = await GetUserAsync("systemadmin", TenantConstants.SystemTenantId);
            Assert.NotNull(systemAdminUser);
            Assert.NotNull(systemAdminUser.PasswordHash);
            Assert.True(systemAdminUser.IsActive);

            // Assert - 验证用户角色关联
            var userRole = await GetUserRoleAsync(systemAdminUser.Id, systemAdminRole.Id);
            Assert.NotNull(userRole);
            Assert.Equal(TenantConstants.SystemTenantId, userRole.TenantId);
        }

        [Fact]
        public async Task SeedAsync_重复运行_应该幂等()
        {
            // Act - 第一次运行
            await TenantSeeder.SeedAsync();
            
            // 获取第一次运行的结果
            var firstRunTenantCount = await DbContext.Tenants.CountAsync();
            var firstRunRoleCount = await DbContext.Roles.CountAsync();
            var firstRunUserCount = await DbContext.Users.CountAsync();
            var firstRunUserRoleCount = await DbContext.UserRoles.CountAsync();

            // Act - 第二次运行
            await TenantSeeder.SeedAsync();

            // Assert - 验证数据没有重复
            var secondRunTenantCount = await DbContext.Tenants.CountAsync();
            var secondRunRoleCount = await DbContext.Roles.CountAsync();
            var secondRunUserCount = await DbContext.Users.CountAsync();
            var secondRunUserRoleCount = await DbContext.UserRoles.CountAsync();

            Assert.Equal(firstRunTenantCount, secondRunTenantCount);
            Assert.Equal(firstRunRoleCount, secondRunRoleCount);
            Assert.Equal(firstRunUserCount, secondRunUserCount);
            Assert.Equal(firstRunUserRoleCount, secondRunUserRoleCount);
        }

        [Fact]
        public async Task SeedAsync_已有旧数据_应该正确迁移()
        {
            // Arrange - 创建一些没有租户ID的旧数据
            var oldRole = new ApplicationRole
            {
                Id = 1L,
                Name = "OldRole",
                NormalizedName = "OLDROLE",
                Description = "旧角色",
                TenantId = null, // 没有租户ID
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = 1L
            };

            var oldUser = new ApplicationUser
            {
                Id = 1L,
                UserName = "olduser",
                NormalizedUserName = "OLDUSER",
                Email = "olduser@example.com",
                Name = "旧用户",
                TenantId = null, // 没有租户ID
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = 1L
            };

            DbContext.Roles.Add(oldRole);
            DbContext.Users.Add(oldUser);
            await DbContext.SaveChangesAsync();

            // Act
            await TenantSeeder.SeedAsync();

            // Assert - 验证旧数据已迁移到默认租户
            var migratedRole = await DbContext.Roles.FindAsync(1L);
            var migratedUser = await DbContext.Users.FindAsync(1L);

            Assert.NotNull(migratedRole);
            Assert.NotNull(migratedUser);
            Assert.Equal(TenantConstants.DefaultTenantId, migratedRole.TenantId);
            Assert.Equal(TenantConstants.DefaultTenantId, migratedUser.TenantId);

            // Assert - 验证新的系统数据也已创建
            var systemAdminUser = await GetUserAsync("systemadmin", TenantConstants.SystemTenantId);
            Assert.NotNull(systemAdminUser);
        }

        [Fact]
        public async Task ValidateMigrationAsync_数据迁移完成后_应该通过验证()
        {
            // Arrange
            await TenantSeeder.SeedAsync();

            // Act & Assert - 应该不抛出异常
            await TenantSeeder.ValidateMigrationAsync();

            // 额外验证 - 检查是否还有没有租户ID的数据
            var usersWithoutTenant = await DbContext.Users
                .CountAsync(u => string.IsNullOrEmpty(u.TenantId));
            var rolesWithoutTenant = await DbContext.Roles
                .CountAsync(r => string.IsNullOrEmpty(r.TenantId));

            Assert.Equal(0, usersWithoutTenant);
            Assert.Equal(0, rolesWithoutTenant);
        }

        [Fact]
        public async Task SeedAsync_存在冲突数据_应该正确清理()
        {
            // Arrange - 创建冲突的数据（错误租户的系统角色和用户）
            var conflictingRole = new ApplicationRole
            {
                Id = 1001L,
                Name = "SystemAdmin",
                NormalizedName = "SYSTEMADMIN",
                Description = "冲突的系统管理员角色",
                TenantId = TenantConstants.DefaultTenantId, // 错误的租户ID
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = 1L
            };

            var conflictingUser = new ApplicationUser
            {
                Id = 1001L,
                UserName = "systemadmin",
                NormalizedUserName = "SYSTEMADMIN",
                Email = "systemadmin@wrong.local",
                Name = "冲突的系统管理员",
                TenantId = TenantConstants.DefaultTenantId, // 错误的租户ID
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = 1L
            };

            DbContext.Roles.Add(conflictingRole);
            DbContext.Users.Add(conflictingUser);
            await DbContext.SaveChangesAsync();

            // Act
            await TenantSeeder.SeedAsync();

            // Assert - 验证冲突数据已被清理
            var remainingConflictingRole = await DbContext.Roles.FindAsync(1001L);
            var remainingConflictingUser = await DbContext.Users.FindAsync(1001L);

            Assert.Null(remainingConflictingRole);
            Assert.Null(remainingConflictingUser);

            // Assert - 验证正确的系统数据已创建
            var correctSystemRole = await GetRoleAsync("SystemAdmin", TenantConstants.SystemTenantId);
            var correctSystemUser = await GetUserAsync("systemadmin", TenantConstants.SystemTenantId);

            Assert.NotNull(correctSystemRole);
            Assert.NotNull(correctSystemUser);
            Assert.Equal(TenantConstants.SystemTenantId, correctSystemRole.TenantId);
            Assert.Equal(TenantConstants.SystemTenantId, correctSystemUser.TenantId);
        }

        [Fact]
        public async Task DiagnoseTenantStatusAsync_运行后_应该提供详细诊断信息()
        {
            // Arrange
            await TenantSeeder.SeedAsync();

            // Act & Assert - 应该不抛出异常
            await TenantSeeder.DiagnoseTenantStatusAsync();

            // 验证诊断后数据完整性
            var tenantCount = await DbContext.Tenants.CountAsync();
            var systemTenant = await GetTenantAsync(TenantConstants.SystemTenantId);
            var defaultTenant = await GetTenantAsync(TenantConstants.DefaultTenantId);

            Assert.True(tenantCount >= 2);
            Assert.NotNull(systemTenant);
            Assert.NotNull(defaultTenant);
        }

        [Fact]
        public async Task SeedAsync_创建用户角色关联_应该正确设置()
        {
            // Act
            await TenantSeeder.SeedAsync();

            // Assert
            var systemAdminUser = await GetUserAsync("systemadmin", TenantConstants.SystemTenantId);
            var systemAdminRole = await GetRoleAsync("SystemAdmin", TenantConstants.SystemTenantId);

            Assert.NotNull(systemAdminUser);
            Assert.NotNull(systemAdminRole);

            // 验证用户角色关联
            var userRole = await GetUserRoleAsync(systemAdminUser.Id, systemAdminRole.Id);
            Assert.NotNull(userRole);
            Assert.Equal(TenantConstants.SystemTenantId, userRole.TenantId);
            Assert.True(userRole.CreatedAt > DateTime.MinValue);
        }

        [Fact]
        public async Task SeedAsync_租户配置_应该正确设置()
        {
            // Act
            await TenantSeeder.SeedAsync();

            // Assert
            var systemTenant = await GetTenantAsync(TenantConstants.SystemTenantId);
            var defaultTenant = await GetTenantAsync(TenantConstants.DefaultTenantId);

            // 验证系统租户配置
            Assert.NotNull(systemTenant);
            Assert.True(systemTenant.IsActive);
            Assert.Equal(TenantStrategy.SharedDatabase, systemTenant.Strategy);
            Assert.Equal(10000, systemTenant.MaxUsers);
            Assert.Equal(102400L, systemTenant.StorageLimit);
            Assert.Null(systemTenant.ExpiresAt); // 系统租户永不过期
            Assert.Equal("{}", systemTenant.Configuration);
            Assert.Equal("{}", systemTenant.ThemeConfig);

            // 验证默认租户配置
            Assert.NotNull(defaultTenant);
            Assert.True(defaultTenant.IsActive);
            Assert.Equal(TenantStrategy.SharedDatabase, defaultTenant.Strategy);
            Assert.Equal(10000, defaultTenant.MaxUsers);
            Assert.Equal(102400L, defaultTenant.StorageLimit);
            Assert.Null(defaultTenant.ExpiresAt); // 默认租户永不过期
        }

        [Fact]
        public async Task SeedAsync_数据审计字段_应该正确设置()
        {
            // Act
            await TenantSeeder.SeedAsync();

            // Assert
            var systemTenant = await GetTenantAsync(TenantConstants.SystemTenantId);
            var systemAdminRole = await GetRoleAsync("SystemAdmin", TenantConstants.SystemTenantId);
            var systemAdminUser = await GetUserAsync("systemadmin", TenantConstants.SystemTenantId);

            // 验证租户审计字段
            Assert.NotNull(systemTenant);
            Assert.True(systemTenant.CreatedAt > DateTime.MinValue);
            Assert.Equal(1L, systemTenant.CreatedBy);
            Assert.False(systemTenant.IsDeleted);

            // 验证角色审计字段
            Assert.NotNull(systemAdminRole);
            Assert.True(systemAdminRole.CreatedAt > DateTime.MinValue);
            Assert.Equal(1L, systemAdminRole.CreatedBy);
            Assert.False(systemAdminRole.IsDeleted);

            // 验证用户审计字段
            Assert.NotNull(systemAdminUser);
            Assert.True(systemAdminUser.CreatedAt > DateTime.MinValue);
            Assert.Equal(1L, systemAdminUser.CreatedBy);
            Assert.False(systemAdminUser.IsDeleted);
        }
    }
} 