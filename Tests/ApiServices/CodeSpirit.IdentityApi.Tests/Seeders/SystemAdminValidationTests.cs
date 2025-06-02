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
    /// 系统管理员验证测试
    /// </summary>
    public class SystemAdminValidationTests : IDisposable
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly ApplicationDbContext _context;
        private readonly DataSeederValidator _validator;

        public SystemAdminValidationTests()
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
        public async Task ValidateSystemAdminAsync_当系统数据完整时_应该返回True()
        {
            // Arrange - 创建完整的系统数据
            await CreateCompleteSystemDataAsync();

            // Act
            var result = await _validator.ValidateSystemAdminAsync();

            // Assert
            Assert.True(result, "系统管理员数据应该验证通过");
        }

        [Fact]
        public async Task ValidateSystemAdminAsync_当缺少用户角色关联时_应该返回False()
        {
            // Arrange - 创建没有角色关联的系统数据
            await CreateSystemDataWithoutUserRoleAsync();

            // Act
            var result = await _validator.ValidateSystemAdminAsync();

            // Assert
            Assert.False(result, "缺少用户角色关联时应该验证失败");
        }

        [Fact]
        public async Task FixSystemAdminRoleBindingAsync_当缺少角色绑定时_应该成功修复()
        {
            // Arrange - 创建没有角色关联的系统数据
            await CreateSystemDataWithoutUserRoleAsync();

            // Act
            var fixResult = await _validator.FixSystemAdminRoleBindingAsync();
            var validateResult = await _validator.ValidateSystemAdminAsync();

            // Assert
            Assert.True(fixResult, "修复操作应该成功");
            Assert.True(validateResult, "修复后验证应该通过");
        }

        /// <summary>
        /// 创建完整的系统数据
        /// </summary>
        private async Task CreateCompleteSystemDataAsync()
        {
            // 创建系统租户
            var systemTenant = new TenantInfo
            {
                Id = Guid.NewGuid().ToString(),
                TenantId = TenantConstants.SystemTenantId,
                Name = TenantConstants.SystemTenantName,
                DisplayName = TenantConstants.SystemTenantDisplayName,
                Description = TenantConstants.SystemTenantDescription,
                Strategy = TenantStrategy.SharedDatabase,
                IsActive = true,
                Configuration = "{}",
                ThemeConfig = "{}",
                MaxUsers = 10000,
                StorageLimit = 102400L,
                ExpiresAt = null,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = 1L,
                IsDeleted = false
            };
            _context.Tenants.Add(systemTenant);

            // 创建系统管理员角色
            var adminRole = new ApplicationRole
            {
                Id = 1L,
                TenantId = TenantConstants.SystemTenantId,
                Name = "Admin",
                NormalizedName = "ADMIN",
                Description = "系统管理员角色",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = 1L,
                IsDeleted = false
            };
            _context.Roles.Add(adminRole);

            // 创建系统管理员用户
            var systemAdminUser = new ApplicationUser
            {
                Id = 1L,
                TenantId = TenantConstants.SystemTenantId,
                UserName = "systemadmin",
                NormalizedUserName = "SYSTEMADMIN",
                Email = "systemadmin@system.local",
                NormalizedEmail = "SYSTEMADMIN@SYSTEM.LOCAL",
                EmailConfirmed = true,
                Name = "系统管理员",
                IsActive = true,
                PasswordHash = "hashedpassword", // 模拟已设置的密码
                CreatedAt = DateTime.UtcNow,
                CreatedBy = 1L,
                IsDeleted = false,
                SecurityStamp = Guid.NewGuid().ToString(),
                ConcurrencyStamp = Guid.NewGuid().ToString()
            };
            _context.Users.Add(systemAdminUser);

            // 创建用户角色关联
            var userRole = new ApplicationUserRole
            {
                UserId = systemAdminUser.Id,
                RoleId = adminRole.Id,
                TenantId = TenantConstants.SystemTenantId,
                CreatedAt = DateTime.UtcNow
            };
            _context.UserRoles.Add(userRole);

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// 创建没有用户角色关联的系统数据
        /// </summary>
        private async Task CreateSystemDataWithoutUserRoleAsync()
        {
            // 创建系统租户
            var systemTenant = new TenantInfo
            {
                Id = Guid.NewGuid().ToString(),
                TenantId = TenantConstants.SystemTenantId,
                Name = TenantConstants.SystemTenantName,
                DisplayName = TenantConstants.SystemTenantDisplayName,
                Description = TenantConstants.SystemTenantDescription,
                Strategy = TenantStrategy.SharedDatabase,
                IsActive = true,
                Configuration = "{}",
                ThemeConfig = "{}",
                MaxUsers = 10000,
                StorageLimit = 102400L,
                ExpiresAt = null,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = 1L,
                IsDeleted = false
            };
            _context.Tenants.Add(systemTenant);

            // 创建系统管理员角色
            var adminRole = new ApplicationRole
            {
                Id = 1L,
                TenantId = TenantConstants.SystemTenantId,
                Name = "Admin",
                NormalizedName = "ADMIN",
                Description = "系统管理员角色",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = 1L,
                IsDeleted = false
            };
            _context.Roles.Add(adminRole);

            // 创建系统管理员用户（但不创建用户角色关联）
            var systemAdminUser = new ApplicationUser
            {
                Id = 1L,
                TenantId = TenantConstants.SystemTenantId,
                UserName = "systemadmin",
                NormalizedUserName = "SYSTEMADMIN",
                Email = "systemadmin@system.local",
                NormalizedEmail = "SYSTEMADMIN@SYSTEM.LOCAL",
                EmailConfirmed = true,
                Name = "系统管理员",
                IsActive = true,
                PasswordHash = "hashedpassword", // 模拟已设置的密码
                CreatedAt = DateTime.UtcNow,
                CreatedBy = 1L,
                IsDeleted = false,
                SecurityStamp = Guid.NewGuid().ToString(),
                ConcurrencyStamp = Guid.NewGuid().ToString()
            };
            _context.Users.Add(systemAdminUser);

            // 注意：这里没有创建用户角色关联

            await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context?.Dispose();
            _serviceProvider?.Dispose();
        }
    }
} 