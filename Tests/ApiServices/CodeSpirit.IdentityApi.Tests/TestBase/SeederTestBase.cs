using CodeSpirit.Core;
using CodeSpirit.Core.IdGenerator;
using CodeSpirit.IdentityApi.Data;
using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.IdentityApi.Data.Seeders;
using CodeSpirit.MultiTenant.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;

namespace CodeSpirit.IdentityApi.Tests.TestBase
{
    /// <summary>
    /// 种子数据测试基类
    /// </summary>
    public abstract class SeederTestBase : IDisposable
    {
        protected ApplicationDbContext DbContext { get; private set; }
        protected ServiceProvider ServiceProvider { get; private set; }
        protected IRoleSeederService RoleSeederService { get; private set; }
        protected IUserSeederService UserSeederService { get; private set; }
        protected TenantSeeder TenantSeeder { get; private set; }
        protected SeederService SeederService { get; private set; }

        public SeederTestBase()
        {
            SetupServices();
        }

        /// <summary>
        /// 设置测试服务
        /// </summary>
        private void SetupServices()
        {
            var services = new ServiceCollection();

            // 配置内存数据库
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString());
            });

            // 配置Identity
            services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                // 简化密码要求用于测试
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequiredLength = 1;
                options.User.RequireUniqueEmail = false;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

            // 注册必要的服务
            services.AddLogging(builder => builder.AddConsole());
            services.AddScoped<IIdGenerator, SnowflakeIdGenerator>();
            
            // Mock ICurrentUser
            var mockCurrentUser = new Mock<ICurrentUser>();
            mockCurrentUser.Setup(x => x.Id).Returns(1L);
            mockCurrentUser.Setup(x => x.UserName).Returns("testuser");
            mockCurrentUser.Setup(x => x.TenantId).Returns(TenantConstants.SystemTenantId);
            mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);
            mockCurrentUser.Setup(x => x.Roles).Returns(new[] { "SystemAdmin" });
            mockCurrentUser.Setup(x => x.Permissions).Returns(new HashSet<string>());
            mockCurrentUser.Setup(x => x.Claims).Returns(new List<System.Security.Claims.Claim>());
            services.AddSingleton(mockCurrentUser.Object);
            
            // Mock IHostEnvironment
            var mockHostEnvironment = new Mock<IHostEnvironment>();
            mockHostEnvironment.Setup(x => x.EnvironmentName).Returns(Environments.Development);
            services.AddSingleton(mockHostEnvironment.Object);

            // 注册种子数据服务
            services.AddScoped<IRoleSeederService, UnifiedRoleSeederService>();
            services.AddScoped<IUserSeederService, UnifiedUserSeederService>();
            services.AddScoped<TenantSeeder>();
            services.AddScoped<SeederService>();

            ServiceProvider = services.BuildServiceProvider();

            // 获取服务实例
            DbContext = ServiceProvider.GetRequiredService<ApplicationDbContext>();
            RoleSeederService = ServiceProvider.GetRequiredService<IRoleSeederService>();
            UserSeederService = ServiceProvider.GetRequiredService<IUserSeederService>();
            TenantSeeder = ServiceProvider.GetRequiredService<TenantSeeder>();
            SeederService = ServiceProvider.GetRequiredService<SeederService>();

            // 确保数据库已创建
            DbContext.Database.EnsureCreated();
        }

        /// <summary>
        /// 创建租户信息
        /// </summary>
        /// <param name="tenantId">租户ID</param>
        /// <param name="name">租户名称</param>
        /// <returns></returns>
        protected async Task<TenantInfo> CreateTenantAsync(string tenantId, string name)
        {
            var tenant = new TenantInfo
            {
                Id = Guid.NewGuid().ToString(),
                TenantId = tenantId,
                Name = name,
                DisplayName = name,
                Description = $"{name}描述",
                Strategy = TenantStrategy.SharedDatabase,
                IsActive = true,
                Configuration = "{}",
                ThemeConfig = "{}",
                MaxUsers = 1000,
                StorageLimit = 10240L,
                ExpiresAt = null,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = 1L,
                IsDeleted = false
            };

            DbContext.Tenants.Add(tenant);
            await DbContext.SaveChangesAsync();
            return tenant;
        }

        /// <summary>
        /// 验证角色是否存在
        /// </summary>
        /// <param name="roleName">角色名称</param>
        /// <param name="tenantId">租户ID</param>
        /// <returns></returns>
        protected async Task<ApplicationRole?> GetRoleAsync(string roleName, string tenantId)
        {
            return await DbContext.Roles
                .FirstOrDefaultAsync(r => r.Name == roleName && r.TenantId == tenantId);
        }

        /// <summary>
        /// 验证用户是否存在
        /// </summary>
        /// <param name="userName">用户名</param>
        /// <param name="tenantId">租户ID</param>
        /// <returns></returns>
        protected async Task<ApplicationUser?> GetUserAsync(string userName, string tenantId)
        {
            return await DbContext.Users
                .FirstOrDefaultAsync(u => u.UserName == userName && u.TenantId == tenantId);
        }

        /// <summary>
        /// 验证用户角色关联是否存在
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="roleId">角色ID</param>
        /// <returns></returns>
        protected async Task<ApplicationUserRole?> GetUserRoleAsync(long userId, long roleId)
        {
            return await DbContext.UserRoles
                .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);
        }

        /// <summary>
        /// 获取租户信息
        /// </summary>
        /// <param name="tenantId">租户ID</param>
        /// <returns></returns>
        protected async Task<TenantInfo?> GetTenantAsync(string tenantId)
        {
            return await DbContext.Tenants
                .FirstOrDefaultAsync(t => t.TenantId == tenantId);
        }

        /// <summary>
        /// 清理数据库
        /// </summary>
        protected async Task ClearDatabaseAsync()
        {
            DbContext.UserRoles.RemoveRange(DbContext.UserRoles);
            DbContext.Users.RemoveRange(DbContext.Users);
            DbContext.Roles.RemoveRange(DbContext.Roles);
            DbContext.Tenants.RemoveRange(DbContext.Tenants);
            await DbContext.SaveChangesAsync();
        }

        public void Dispose()
        {
            DbContext?.Dispose();
            ServiceProvider?.Dispose();
        }
    }
} 