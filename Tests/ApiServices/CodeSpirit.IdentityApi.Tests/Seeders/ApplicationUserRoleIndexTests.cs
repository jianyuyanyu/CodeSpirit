using CodeSpirit.Core;
using CodeSpirit.IdentityApi.Data;
using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.IdentityApi.Data.Seeders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace CodeSpirit.IdentityApi.Tests.Seeders
{
    /// <summary>
    /// ApplicationUserRole索引约束测试
    /// </summary>
    public class ApplicationUserRoleIndexTests : IDisposable
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly ApplicationDbContext _context;
        private readonly DataSeederValidator _validator;

        public ApplicationUserRoleIndexTests()
        {
            var services = new ServiceCollection();
            
            // 配置内存数据库
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));
            
            // 注册日志服务
            services.AddLogging(builder => builder.AddConsole());
            
            // 注册其他必要的服务
            services.AddScoped<DataSeederValidator>();
            
            _serviceProvider = services.BuildServiceProvider();
            _context = _serviceProvider.GetRequiredService<ApplicationDbContext>();
            _validator = _serviceProvider.GetRequiredService<DataSeederValidator>();
        }

        [Fact]
        public async Task CheckDuplicateUserRolesAsync_当没有重复数据时_应该返回True()
        {
            // Arrange - 创建唯一的用户角色关联
            await CreateUniqueUserRoleDataAsync();

            // Act
            var result = await _validator.CheckDuplicateUserRolesAsync();

            // Assert
            Assert.True(result, "没有重复数据时应该返回true");
        }

        [Fact]
        public async Task CheckDuplicateUserRolesAsync_当存在重复数据时_应该返回False()
        {
            // Arrange - 创建重复的用户角色关联
            await CreateDuplicateUserRoleDataAsync();

            // Act
            var result = await _validator.CheckDuplicateUserRolesAsync();

            // Assert
            Assert.False(result, "存在重复数据时应该返回false");
        }

        [Fact]
        public async Task CleanupDuplicateUserRolesAsync_应该删除重复记录保留最早创建的()
        {
            // Arrange - 创建重复的用户角色关联
            await CreateDuplicateUserRoleDataAsync();

            // 验证初始状态有重复数据
            var initialCheck = await _validator.CheckDuplicateUserRolesAsync();
            Assert.False(initialCheck, "应该有重复数据");

            // Act - 清理重复数据
            var cleanupResult = await _validator.CleanupDuplicateUserRolesAsync();

            // Assert
            Assert.True(cleanupResult, "清理操作应该成功");

            // 验证清理后没有重复数据
            var finalCheck = await _validator.CheckDuplicateUserRolesAsync();
            Assert.True(finalCheck, "清理后应该没有重复数据");

            // 验证只保留了一条记录
            var remainingCount = await _context.UserRoles
                .Where(ur => ur.UserId == 1L && ur.RoleId == 1L && ur.TenantId == TenantConstants.SystemTenantId)
                .CountAsync();
            Assert.Equal(1, remainingCount);
        }

        [Fact]
        public async Task ValidateDatabaseConstraintsAsync_当数据完整时_应该返回True()
        {
            // Arrange - 创建完整的测试数据
            await CreateCompleteTestDataAsync();

            // Act
            var result = await _validator.ValidateDatabaseConstraintsAsync();

            // Assert
            Assert.True(result, "数据完整时约束验证应该通过");
        }

        [Fact]
        public async Task ValidateDatabaseConstraintsAsync_当有空TenantId时_应该返回False()
        {
            // Arrange - 创建有空TenantId的数据
            await CreateDataWithEmptyTenantIdAsync();

            // Act
            var result = await _validator.ValidateDatabaseConstraintsAsync();

            // Assert
            Assert.False(result, "有空TenantId时约束验证应该失败");
        }

        /// <summary>
        /// 创建唯一的用户角色关联数据
        /// </summary>
        private async Task CreateUniqueUserRoleDataAsync()
        {
            // 创建租户
            var tenant = new TenantInfo
            {
                Id = Guid.NewGuid().ToString(),
                TenantId = TenantConstants.SystemTenantId,
                Name = "System",
                DisplayName = "系统",
                Description = "系统租户",
                Strategy = TenantStrategy.SharedDatabase,
                IsActive = true,
                Configuration = "{}",
                ThemeConfig = "{}",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = 1L,
                IsDeleted = false
            };
            _context.Tenants.Add(tenant);

            // 创建用户
            var user = new ApplicationUser
            {
                Id = 1L,
                TenantId = TenantConstants.SystemTenantId,
                UserName = "testuser",
                NormalizedUserName = "TESTUSER",
                Email = "test@example.com",
                NormalizedEmail = "TEST@EXAMPLE.COM",
                EmailConfirmed = true,
                Name = "测试用户",
                IsActive = true,
                PasswordHash = "hashedpassword",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = 1L,
                IsDeleted = false,
                SecurityStamp = Guid.NewGuid().ToString(),
                ConcurrencyStamp = Guid.NewGuid().ToString()
            };
            _context.Users.Add(user);

            // 创建角色
            var role = new ApplicationRole
            {
                Id = 1L,
                TenantId = TenantConstants.SystemTenantId,
                Name = "TestRole",
                NormalizedName = "TESTROLE",
                Description = "测试角色",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = 1L,
                IsDeleted = false
            };
            _context.Roles.Add(role);

            // 创建唯一的用户角色关联
            var userRole = new ApplicationUserRole
            {
                UserId = user.Id,
                RoleId = role.Id,
                TenantId = TenantConstants.SystemTenantId,
                CreatedAt = DateTime.UtcNow
            };
            _context.UserRoles.Add(userRole);

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// 创建重复的用户角色关联数据
        /// </summary>
        private async Task CreateDuplicateUserRoleDataAsync()
        {
            await CreateUniqueUserRoleDataAsync();

            // 添加重复的用户角色关联
            var duplicateUserRole = new ApplicationUserRole
            {
                UserId = 1L,
                RoleId = 1L,
                TenantId = TenantConstants.SystemTenantId,
                CreatedAt = DateTime.UtcNow.AddMinutes(1) // 晚1分钟创建
            };
            _context.UserRoles.Add(duplicateUserRole);

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// 创建完整的测试数据
        /// </summary>
        private async Task CreateCompleteTestDataAsync()
        {
            await CreateUniqueUserRoleDataAsync();
        }

        /// <summary>
        /// 创建有空TenantId的数据
        /// </summary>
        private async Task CreateDataWithEmptyTenantIdAsync()
        {
            await CreateUniqueUserRoleDataAsync();

            // 添加TenantId为空的用户角色关联
            var invalidUserRole = new ApplicationUserRole
            {
                UserId = 1L,
                RoleId = 1L,
                TenantId = "", // 空的TenantId
                CreatedAt = DateTime.UtcNow
            };
            _context.UserRoles.Add(invalidUserRole);

            await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context?.Dispose();
            _serviceProvider?.Dispose();
        }
    }
} 