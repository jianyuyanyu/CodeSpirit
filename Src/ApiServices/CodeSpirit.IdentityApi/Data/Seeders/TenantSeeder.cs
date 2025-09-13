using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.MultiTenant.Models;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using CodeSpirit.Core.IdGenerator;
using CodeSpirit.Core;
using Microsoft.AspNetCore.Identity;

namespace CodeSpirit.IdentityApi.Data.Seeders
{
    /// <summary>
    /// 租户种子数据服务
    /// </summary>
    [DisplayName("租户种子数据服务")]
    public class TenantSeeder : IScopedDependency
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TenantSeeder> _logger;
        private readonly IRoleSeederService _roleSeederService;
        private readonly IUserSeederService _userSeederService;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="context">数据库上下文</param>
        /// <param name="logger">日志记录器</param>
        /// <param name="roleSeederService">角色种子数据服务</param>
        /// <param name="userSeederService">用户种子数据服务</param>
        public TenantSeeder(
            ApplicationDbContext context,
            ILogger<TenantSeeder> logger,
            IRoleSeederService roleSeederService,
            IUserSeederService userSeederService)
        {
            _context = context;
            _logger = logger;
            _roleSeederService = roleSeederService;
            _userSeederService = userSeederService;
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

                // 使用数据库事务确保数据一致性
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // 1. 确保默认租户存在
                    await EnsureDefaultTenantAsync();

                    // 2. 确保系统租户存在
                    await EnsureSystemTenantAsync();

                    // 3. 迁移现有用户数据
                    await MigrateExistingUsersAsync();

                    // 4. 迁移现有角色数据
                    await MigrateExistingRolesAsync();

                    // 5. 创建系统角色和用户（使用统一服务）
                    await CreateSystemRolesAndUsersAsync();

                    // 检查是否有需要保存的更改
                    var hasChanges = _context.ChangeTracker.HasChanges();
                    if (hasChanges)
                    {
                        _logger.LogInformation("检测到数据变更，正在保存到数据库...");

                        try
                        {
                            await _context.SaveChangesAsync();
                            await transaction.CommitAsync();
                            _logger.LogInformation("租户种子数据保存完成");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "保存租户种子数据时发生错误，回滚事务: {Message}", ex.Message);
                            await transaction.RollbackAsync();

                            // 如果是重复键约束错误，说明数据已存在，不需要抛出异常
                            if (ex.Message.Contains("duplicate key") || ex.Message.Contains("重复键"))
                            {
                                _logger.LogWarning("检测到重复键约束，可能数据已存在，忽略此错误");
                                return;
                            }

                            throw;
                        }
                    }
                    else
                    {
                        await transaction.CommitAsync();
                        _logger.LogInformation("没有数据变更，跳过保存操作");
                    }

                    // 等待一小段时间，确保事务完全提交到数据库
                    await Task.Delay(500);

                    _logger.LogInformation("租户种子数据初始化完成");
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "租户种子数据初始化失败: {Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// 创建系统角色和用户
        /// </summary>
        /// <returns></returns>
        private async Task CreateSystemRolesAndUsersAsync()
        {
            try
            {
                _logger.LogInformation("开始创建系统角色和用户...");

                // 先清理 ChangeTracker，避免之前的实体影响
                _context.ChangeTracker.Clear();

                // 详细检查数据库状态
                await DiagnoseExistingDataAsync();

                // 检查是否已经存在系统数据，避免重复创建
                var existingSystemRoles = await _context.Roles
                    .IgnoreQueryFilters() // 忽略软删除过滤器
                    .Where(r => r.TenantId == TenantConstants.SystemTenantId)
                    .ToListAsync();

                var existingSystemUsers = await _context.Users
                    .IgnoreQueryFilters() // 忽略软删除过滤器
                    .Where(u => u.TenantId == TenantConstants.SystemTenantId)
                    .ToListAsync();

                _logger.LogInformation("现有系统角色数量: {Count}", existingSystemRoles.Count);
                _logger.LogInformation("现有系统用户数量: {Count}", existingSystemUsers.Count);

                // 如果系统数据已存在，跳过创建
                if (existingSystemRoles.Any() && existingSystemUsers.Any())
                {
                    _logger.LogInformation("系统角色和用户已存在，跳过创建");
                    return;
                }

                // 分别创建角色和用户，避免批量操作导致的事务问题
                var systemRoles = _roleSeederService.GetSystemRoles();
                var createdRoles = new List<ApplicationRole>();

                foreach (var roleDefinition in systemRoles)
                {
                    try
                    {
                        var role = await _roleSeederService.EnsureRoleExistsAsync(
                            roleDefinition.Name,
                            roleDefinition.Description,
                            roleDefinition.TenantId);
                        createdRoles.Add(role);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "创建角色 {RoleName} 时发生错误，可能已存在: {Message}",
                            roleDefinition.Name, ex.Message);

                        // 尝试查找已存在的角色
                        var existingRole = await _context.Roles
                            .FirstOrDefaultAsync(r => r.TenantId == roleDefinition.TenantId &&
                                                     (r.Name == roleDefinition.Name ||
                                                      r.NormalizedName == roleDefinition.Name.ToUpper()));
                        if (existingRole != null)
                        {
                            createdRoles.Add(existingRole);
                        }
                    }
                }

                _logger.LogInformation("系统角色创建完成，共处理 {Count} 个角色", createdRoles.Count);

                // 创建系统用户
                var systemUsers = _userSeederService.GetSystemUsers();
                var createdUsers = new List<ApplicationUser>();

                foreach (var userDefinition in systemUsers)
                {
                    try
                    {
                        var user = await _userSeederService.EnsureUserExistsAsync(
                            userDefinition.UserName,
                            userDefinition.Email,
                            userDefinition.DisplayName,
                            userDefinition.Password,
                            userDefinition.TenantId);
                        createdUsers.Add(user);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "创建用户 {UserName} 时发生错误，可能已存在: {Message}",
                            userDefinition.UserName, ex.Message);

                        // 尝试查找已存在的用户
                        var existingUser = await _context.Users
                            .FirstOrDefaultAsync(u => u.TenantId == userDefinition.TenantId &&
                                                     (u.UserName == userDefinition.UserName ||
                                                      u.NormalizedUserName == userDefinition.UserName.ToUpper()));
                        if (existingUser != null)
                        {
                            createdUsers.Add(existingUser);
                        }
                    }
                }

                _logger.LogInformation("系统用户创建完成，共处理 {Count} 个用户", createdUsers.Count);

                // 分配角色给用户
                foreach (var userDefinition in systemUsers)
                {
                    var user = createdUsers.FirstOrDefault(u => u.UserName == userDefinition.UserName);
                    if (user != null)
                    {
                        foreach (var roleName in userDefinition.Roles)
                        {
                            var role = createdRoles.FirstOrDefault(r => r.Name == roleName);
                            if (role != null)
                            {
                                try
                                {
                                    await _userSeederService.EnsureUserRoleExistsAsync(user.Id, role.Id, userDefinition.TenantId);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning(ex, "为用户 {UserName} 分配角色 {RoleName} 时发生错误: {Message}",
                                        user.UserName, roleName, ex.Message);
                                }
                            }
                        }
                    }
                }

                _logger.LogInformation("系统角色和用户创建完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建系统角色和用户时发生错误: {Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// 确保默认租户存在
        /// </summary>
        /// <returns></returns>
        private async Task EnsureDefaultTenantAsync()
        {
            var existingTenant = await _context.Tenants
                .FirstOrDefaultAsync(t => t.TenantId == TenantConstants.DefaultTenantId);

            if (existingTenant == null)
            {
                _logger.LogInformation("创建默认租户...");

                var defaultTenant = new TenantInfo
                {
                    Id = Guid.NewGuid().ToString(),
                    TenantId = TenantConstants.DefaultTenantId,
                    Name = TenantConstants.DefaultTenantName,
                    DisplayName = TenantConstants.DefaultTenantDisplayName,
                    Description = TenantConstants.DefaultTenantDescription,
                    Strategy = TenantStrategy.SharedDatabase,
                    IsActive = true,
                    Configuration = "{}",
                    ThemeConfig = "{}",
                    MaxUsers = 10000,
                    StorageLimit = 102400L, // 100GB
                    ExpiresAt = null, // 默认租户永不过期
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = 1L, // 系统管理员
                    IsDeleted = false
                };

                _context.Tenants.Add(defaultTenant);
                _logger.LogInformation("默认租户创建完成: {TenantId}, 过期时间: {ExpiresAt}",
                    TenantConstants.DefaultTenantId, defaultTenant.ExpiresAt?.ToString() ?? "永不过期");
            }
            else
            {
                _logger.LogInformation("默认租户已存在: {TenantId}", TenantConstants.DefaultTenantId);
            }
        }

        /// <summary>
        /// 确保系统租户存在
        /// </summary>
        /// <returns></returns>
        private async Task EnsureSystemTenantAsync()
        {
            try
            {
                _logger.LogInformation("开始检查系统租户是否存在...");

                var existingTenant = await _context.Tenants
                    .FirstOrDefaultAsync(t => t.TenantId == TenantConstants.SystemTenantId);

                if (existingTenant == null)
                {
                    _logger.LogInformation("创建系统租户...");

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
                        StorageLimit = 102400L, // 100GB
                        ExpiresAt = null, // 系统租户永不过期
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = 1L, // 系统管理员
                        IsDeleted = false
                    };

                    _context.Tenants.Add(systemTenant);
                    _logger.LogInformation("系统租户创建完成: {TenantId}, Id: {Id}, 过期时间: {ExpiresAt}",
                        TenantConstants.SystemTenantId, systemTenant.Id, systemTenant.ExpiresAt?.ToString() ?? "永不过期");

                    // 立即检查是否已添加到上下文
                    var addedEntry = _context.Entry(systemTenant);
                    _logger.LogInformation("系统租户在上下文中的状态: {State}", addedEntry.State);
                }
                else
                {
                    _logger.LogInformation("系统租户已存在: {TenantId}, Id: {Id}, Name: {Name}, 过期时间: {ExpiresAt}",
                        existingTenant.TenantId, existingTenant.Id, existingTenant.Name,
                        existingTenant.ExpiresAt?.ToString() ?? "永不过期");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "确保系统租户存在时发生错误: {Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// 迁移现有用户数据
        /// </summary>
        /// <returns></returns>
        private async Task MigrateExistingUsersAsync()
        {
            // 查找没有租户ID的用户
            var usersWithoutTenant = await _context.Users
                .Where(u => string.IsNullOrEmpty(u.TenantId))
                .ToListAsync();

            if (usersWithoutTenant.Any())
            {
                _logger.LogInformation("开始迁移 {Count} 个用户到默认租户...", usersWithoutTenant.Count);

                foreach (var user in usersWithoutTenant)
                {
                    user.TenantId = TenantConstants.DefaultTenantId;
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
                    role.TenantId = TenantConstants.DefaultTenantId;

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
        /// 诊断现有数据状态
        /// </summary>
        /// <returns></returns>
        private async Task DiagnoseExistingDataAsync()
        {
            try
            {
                _logger.LogInformation("开始诊断现有数据状态...");

                // 查看所有系统租户相关的角色
                var systemRoles = await _context.Roles
                    .IgnoreQueryFilters() // 忽略软删除过滤器
                    .Where(r => r.TenantId == TenantConstants.SystemTenantId)
                    .ToListAsync();

                _logger.LogInformation("系统租户中现有角色数量: {Count}", systemRoles.Count);
                foreach (var role in systemRoles)
                {
                    _logger.LogInformation("角色 - ID: {Id}, Name: {Name}, NormalizedName: {NormalizedName}, TenantId: {TenantId}",
                        role.Id, role.Name, role.NormalizedName, role.TenantId);
                }

                // 查看所有系统租户相关的用户
                var systemUsers = await _context.Users
                    .IgnoreQueryFilters() // 忽略软删除过滤器
                    .Where(u => u.TenantId == TenantConstants.SystemTenantId)
                    .ToListAsync();

                _logger.LogInformation("系统租户中现有用户数量: {Count}", systemUsers.Count);
                foreach (var user in systemUsers)
                {
                    _logger.LogInformation("用户 - ID: {Id}, UserName: {UserName}, NormalizedUserName: {NormalizedUserName}, Email: {Email}, TenantId: {TenantId}",
                        user.Id, user.UserName, user.NormalizedUserName, user.Email, user.TenantId);
                }

                // 查看任何可能冲突的角色（基于NormalizedName）
                var conflictingRoles = await _context.Roles
                    .IgnoreQueryFilters() // 忽略软删除过滤器
                    .Where(r => r.NormalizedName == "SYSTEMADMIN" || r.NormalizedName == "TENANTOPERATOR" || r.NormalizedName == "SYSTEMAUDITOR")
                    .ToListAsync();

                _logger.LogInformation("可能冲突的角色数量: {Count}", conflictingRoles.Count);
                foreach (var role in conflictingRoles)
                {
                    _logger.LogWarning("冲突角色 - ID: {Id}, Name: {Name}, NormalizedName: {NormalizedName}, TenantId: {TenantId}",
                        role.Id, role.Name, role.NormalizedName, role.TenantId);
                }

                // 查看任何可能冲突的用户（基于NormalizedUserName）
                var conflictingUsers = await _context.Users
                    .IgnoreQueryFilters() // 忽略软删除过滤器
                    .Where(u => u.NormalizedUserName == "SYSTEMADMIN")
                    .ToListAsync();

                _logger.LogInformation("可能冲突的用户数量: {Count}", conflictingUsers.Count);
                foreach (var user in conflictingUsers)
                {
                    _logger.LogWarning("冲突用户 - ID: {Id}, UserName: {UserName}, NormalizedUserName: {NormalizedUserName}, TenantId: {TenantId}",
                        user.Id, user.UserName, user.NormalizedUserName, user.TenantId);
                }

                _logger.LogInformation("数据状态诊断完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "诊断现有数据状态时发生错误: {Message}", ex.Message);
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
                    .FirstOrDefaultAsync(t => t.TenantId == TenantConstants.DefaultTenantId);

                if (defaultTenant == null)
                {
                    _logger.LogWarning("默认租户在验证时不存在，这可能是首次启动的正常情况。将在下次验证时检查。");
                    // 尝试重新查询，有时候是缓存或上下文问题
                    await Task.Delay(500);
                    defaultTenant = await _context.Tenants
                        .AsNoTracking()
                        .FirstOrDefaultAsync(t => t.TenantId == TenantConstants.DefaultTenantId);
                    
                    if (defaultTenant == null)
                    {
                        _logger.LogWarning("二次验证默认租户仍不存在，可能需要手动创建或检查数据库连接");
                    }
                    else
                    {
                        _logger.LogInformation("二次验证默认租户成功: {TenantName}", defaultTenant.Name);
                    }
                }
                else
                {
                    _logger.LogInformation("默认租户验证通过: {TenantName}", defaultTenant.Name);
                }

                // 检查系统租户是否存在
                var systemTenant = await _context.Tenants
                    .FirstOrDefaultAsync(t => t.TenantId == TenantConstants.SystemTenantId);

                if (systemTenant == null)
                {
                    _logger.LogWarning("系统租户在验证时不存在，这可能是首次启动的正常情况。将在下次验证时检查。");
                    // 尝试重新查询，有时候是缓存或上下文问题
                    await Task.Delay(500);
                    systemTenant = await _context.Tenants
                        .AsNoTracking()
                        .FirstOrDefaultAsync(t => t.TenantId == TenantConstants.SystemTenantId);
                    
                    if (systemTenant == null)
                    {
                        _logger.LogWarning("二次验证系统租户仍不存在，可能需要手动创建或检查数据库连接");
                    }
                    else
                    {
                        _logger.LogInformation("二次验证系统租户成功: {TenantName}", systemTenant.Name);
                    }
                }
                else
                {
                    _logger.LogInformation("系统租户验证通过: {TenantName}", systemTenant.Name);
                }

                // 统计信息
                var totalUsers = await _context.Users.CountAsync();
                var totalRoles = await _context.Roles.CountAsync();
                var totalTenants = await _context.Tenants.CountAsync();

                _logger.LogInformation("数据迁移验证完成 - 用户: {Users}, 角色: {Roles}, 租户: {Tenants}",
                    totalUsers, totalRoles, totalTenants);

                // 列出所有租户
                var allTenants = await _context.Tenants
                    .Select(t => new { t.TenantId, t.Name, t.IsActive, t.ExpiresAt })
                    .ToListAsync();

                _logger.LogInformation("当前所有租户:");
                foreach (var tenant in allTenants)
                {
                    _logger.LogInformation("- 租户ID: {TenantId}, 名称: {Name}, 状态: {IsActive}, 过期时间: {ExpiresAt}",
                        tenant.TenantId, tenant.Name, tenant.IsActive ? "激活" : "禁用",
                        tenant.ExpiresAt?.ToString() ?? "永不过期");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "数据迁移验证失败: {Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// 诊断租户状态
        /// </summary>
        /// <returns></returns>
        public async Task DiagnoseTenantStatusAsync()
        {
            try
            {
                _logger.LogInformation("开始诊断租户状态...");

                // 检查数据库连接
                var canConnect = await _context.Database.CanConnectAsync();
                _logger.LogInformation("数据库连接状态: {CanConnect}", canConnect ? "正常" : "失败");

                if (!canConnect)
                {
                    _logger.LogError("无法连接到数据库!");
                    return;
                }

                // 检查Tenants表是否存在
                await _context.Database.GetDbConnection().OpenAsync();
                _logger.LogInformation("Tenants表检查...");

                // 查询所有租户
                var allTenants = await _context.Tenants.ToListAsync();
                _logger.LogInformation("数据库中共有 {Count} 个租户", allTenants.Count);

                foreach (var tenant in allTenants)
                {
                    _logger.LogInformation("租户详情 - ID: {Id}, 租户ID: {TenantId}, 名称: {Name}, 创建时间: {CreatedAt}, 是否激活: {IsActive}",
                        tenant.Id, tenant.TenantId, tenant.Name, tenant.CreatedAt, tenant.IsActive);
                }

                // 检查是否存在system租户
                var systemTenantQuery = _context.Tenants.Where(t => t.TenantId == TenantConstants.SystemTenantId);
                var systemTenant = await systemTenantQuery.FirstOrDefaultAsync();
                if (systemTenant == null)
                {
                    _logger.LogError("系统租户确实不存在!");

                    // 尝试强制创建
                    _logger.LogInformation("尝试立即创建系统租户...");
                    await ForceCreateSystemTenantAsync();
                }
                else
                {
                    _logger.LogInformation("系统租户存在: {SystemTenant}",
                        Newtonsoft.Json.JsonConvert.SerializeObject(systemTenant, Newtonsoft.Json.Formatting.Indented));
                }

                _logger.LogInformation("租户状态诊断完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "诊断租户状态失败: {Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// 强制创建系统租户
        /// </summary>
        /// <returns></returns>
        private async Task ForceCreateSystemTenantAsync()
        {
            try
            {
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
                    ExpiresAt = null, // 系统租户永不过期
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = 1L,
                    IsDeleted = false
                };

                _context.Tenants.Add(systemTenant);
                await _context.SaveChangesAsync();

                _logger.LogInformation("强制创建系统租户成功: {TenantId}, ID: {Id}, 过期时间: {ExpiresAt}",
                    systemTenant.TenantId, systemTenant.Id, systemTenant.ExpiresAt?.ToString() ?? "永不过期");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "强制创建系统租户失败: {Message}", ex.Message);
                throw;
            }
        }
    }
}