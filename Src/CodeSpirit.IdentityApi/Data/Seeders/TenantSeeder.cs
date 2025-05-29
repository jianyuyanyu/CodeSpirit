using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.MultiTenant.Models;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;

namespace CodeSpirit.IdentityApi.Data.Seeders
{
    /// <summary>
    /// 租户种子数据服务
    /// </summary>
    [DisplayName("租户种子数据服务")]
    public class TenantSeeder
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TenantSeeder> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="context">数据库上下文</param>
        /// <param name="logger">日志记录器</param>
        public TenantSeeder(ApplicationDbContext context, ILogger<TenantSeeder> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// 执行租户种子数据初始化
        /// </summary>
        /// <returns></returns>
        public async Task SeedAsync()
        {
            try
            {
                _logger.LogInformation("开始执行租户种子数据初始化...");

                // 1. 确保默认租户存在
                await EnsureDefaultTenantAsync();

                // 2. 迁移现有用户数据
                await MigrateExistingUsersAsync();

                // 3. 迁移现有角色数据
                await MigrateExistingRolesAsync();

                // 4. 保存更改
                await _context.SaveChangesAsync();

                _logger.LogInformation("租户种子数据初始化完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "租户种子数据初始化失败: {Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// 确保默认租户存在
        /// </summary>
        /// <returns></returns>
        private async Task EnsureDefaultTenantAsync()
        {
            const string defaultTenantId = "default";
            
            var existingTenant = await _context.Tenants
                .FirstOrDefaultAsync(t => t.TenantId == defaultTenantId);

            if (existingTenant == null)
            {
                _logger.LogInformation("创建默认租户...");

                var defaultTenant = new TenantInfo
                {
                    Id = Guid.NewGuid().ToString(),
                    TenantId = defaultTenantId,
                    Name = "默认租户",
                    DisplayName = "默认租户",
                    Description = "系统默认租户，用于迁移现有数据",
                    Strategy = TenantStrategy.SharedDatabase,
                    IsActive = true,
                    Configuration = "{}",
                    ThemeConfig = "{}",
                    MaxUsers = 10000,
                    StorageLimit = 102400L, // 100GB
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = 1L, // 系统管理员
                    IsDeleted = false
                };

                _context.Tenants.Add(defaultTenant);
                _logger.LogInformation("默认租户创建完成: {TenantId}", defaultTenantId);
            }
            else
            {
                _logger.LogInformation("默认租户已存在: {TenantId}", defaultTenantId);
            }
        }

        /// <summary>
        /// 迁移现有用户数据
        /// </summary>
        /// <returns></returns>
        private async Task MigrateExistingUsersAsync()
        {
            const string defaultTenantId = "default";

            // 查找没有租户ID的用户
            var usersWithoutTenant = await _context.Users
                .Where(u => string.IsNullOrEmpty(u.TenantId))
                .ToListAsync();

            if (usersWithoutTenant.Any())
            {
                _logger.LogInformation("开始迁移 {Count} 个用户到默认租户...", usersWithoutTenant.Count);

                foreach (var user in usersWithoutTenant)
                {
                    user.TenantId = defaultTenantId;
                    _logger.LogDebug("用户 {UserId} ({UserName}) 已分配到默认租户", user.Id, user.UserName);
                }

                _logger.LogInformation("用户数据迁移完成");
            }
            else
            {
                _logger.LogInformation("没有需要迁移的用户数据");
            }
        }

        /// <summary>
        /// 迁移现有角色数据
        /// </summary>
        /// <returns></returns>
        private async Task MigrateExistingRolesAsync()
        {
            const string defaultTenantId = "default";

            // 查找没有租户ID的角色
            var rolesWithoutTenant = await _context.Roles
                .Where(r => string.IsNullOrEmpty(r.TenantId))
                .ToListAsync();

            if (rolesWithoutTenant.Any())
            {
                _logger.LogInformation("开始迁移 {Count} 个角色到默认租户...", rolesWithoutTenant.Count);

                var currentTime = DateTime.UtcNow;
                foreach (var role in rolesWithoutTenant)
                {
                    role.TenantId = defaultTenantId;
                    
                    // 设置审计信息（如果还没有设置）
                    if (role.CreatedAt == default)
                    {
                        role.CreatedAt = currentTime;
                        role.CreatedBy = 1L; // 系统管理员
                    }
                    
                    // 确保角色是激活状态
                    role.IsActive = true;
                    
                    _logger.LogDebug("角色 {RoleId} ({RoleName}) 已分配到默认租户", role.Id, role.Name);
                }

                _logger.LogInformation("角色数据迁移完成");
            }
            else
            {
                _logger.LogInformation("没有需要迁移的角色数据");
            }
        }

        /// <summary>
        /// 验证数据迁移结果
        /// </summary>
        /// <returns></returns>
        public async Task ValidateMigrationAsync()
        {
            try
            {
                _logger.LogInformation("开始验证数据迁移结果...");

                // 检查是否还有没有租户ID的用户
                var usersWithoutTenant = await _context.Users
                    .CountAsync(u => string.IsNullOrEmpty(u.TenantId));

                if (usersWithoutTenant > 0)
                {
                    _logger.LogWarning("发现 {Count} 个用户没有租户ID", usersWithoutTenant);
                }

                // 检查是否还有没有租户ID的角色
                var rolesWithoutTenant = await _context.Roles
                    .CountAsync(r => string.IsNullOrEmpty(r.TenantId));

                if (rolesWithoutTenant > 0)
                {
                    _logger.LogWarning("发现 {Count} 个角色没有租户ID", rolesWithoutTenant);
                }

                // 检查默认租户是否存在
                var defaultTenant = await _context.Tenants
                    .FirstOrDefaultAsync(t => t.TenantId == "default");

                if (defaultTenant == null)
                {
                    _logger.LogError("默认租户不存在！");
                }
                else
                {
                    _logger.LogInformation("默认租户验证通过: {TenantName}", defaultTenant.Name);
                }

                // 统计信息
                var totalUsers = await _context.Users.CountAsync();
                var totalRoles = await _context.Roles.CountAsync();
                var totalTenants = await _context.Tenants.CountAsync();

                _logger.LogInformation("数据迁移验证完成 - 用户: {Users}, 角色: {Roles}, 租户: {Tenants}", 
                    totalUsers, totalRoles, totalTenants);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "数据迁移验证失败: {Message}", ex.Message);
                throw;
            }
        }
    }
} 