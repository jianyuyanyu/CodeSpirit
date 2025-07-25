using CodeSpirit.Core;
using CodeSpirit.IdentityApi.Data;
using CodeSpirit.IdentityApi.Data.Seeders;
using Microsoft.EntityFrameworkCore;

namespace CodeSpirit.IdentityApi.Services
{
    /// <summary>
    /// 租户数据初始化服务实现
    /// </summary>
    public class TenantDataInitializationService : ITenantDataInitializationService, IScopedDependency
    {
        private readonly IRoleSeederService _roleSeederService;
        private readonly IUserSeederService _userSeederService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TenantDataInitializationService> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        public TenantDataInitializationService(
            IRoleSeederService roleSeederService,
            IUserSeederService userSeederService,
            ApplicationDbContext context,
            ILogger<TenantDataInitializationService> logger)
        {
            _roleSeederService = roleSeederService;
            _userSeederService = userSeederService;
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// 为新创建的租户初始化默认数据
        /// </summary>
        /// <param name="tenantId">租户ID</param>
        /// <param name="createDefaultAdmin">是否创建默认管理员</param>
        /// <returns>初始化结果</returns>
        public async Task<ApiResponse> InitializeTenantDataAsync(string tenantId, bool createDefaultAdmin = true)
        {
            try
            {
                _logger.LogInformation("开始为租户 {TenantId} 初始化默认数据...", tenantId);

                // 验证租户是否存在
                var tenant = await _context.WithoutMultiTenantFilterAsync(async () =>
                {
                    return await _context.Tenants.FirstOrDefaultAsync(t => t.TenantId == tenantId);
                });
                
                if (tenant == null)
                {
                    return new ApiResponse(404, "租户不存在");
                }

                // 1. 初始化默认角色
                var rolesResult = await InitializeTenantRolesAsync(tenantId);
                if (rolesResult.Status != 0)
                {
                    return rolesResult;
                }

                // 确保角色数据已保存到数据库
                await _context.SaveChangesAsync();

                // 2. 初始化默认用户
                var usersResult = await InitializeTenantUsersAsync(tenantId, createDefaultAdmin);
                if (usersResult.Status != 0)
                {
                    return usersResult;
                }

                _logger.LogInformation("租户 {TenantId} 数据初始化完成", tenantId);
                return new ApiResponse(0, "租户数据初始化成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "租户 {TenantId} 数据初始化失败: {Message}", tenantId, ex.Message);
                return new ApiResponse(500, $"租户数据初始化失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 为租户初始化默认角色
        /// </summary>
        /// <param name="tenantId">租户ID</param>
        /// <returns>初始化结果</returns>
        public async Task<ApiResponse> InitializeTenantRolesAsync(string tenantId)
        {
            try
            {
                _logger.LogInformation("为租户 {TenantId} 初始化默认角色...", tenantId);

                var roleNames = GetDefaultRolesForTenant(tenantId);
                var roleDefinitions = new List<RoleDefinition>();

                foreach (var roleName in roleNames)
                {
                    var description = GetRoleDescription(roleName);
                    roleDefinitions.Add(new RoleDefinition
                    {
                        Name = roleName,
                        Description = description,
                        TenantId = tenantId,
                        IsSystemRole = false
                    });
                }

                await _roleSeederService.CreateRolesBatchAsync(roleDefinitions);
                
                _logger.LogInformation("租户 {TenantId} 默认角色初始化完成，共创建 {Count} 个角色", tenantId, roleDefinitions.Count);
                return new ApiResponse(0, "默认角色初始化成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "租户 {TenantId} 角色初始化失败: {Message}", tenantId, ex.Message);
                return new ApiResponse(500, $"角色初始化失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 为租户初始化默认用户
        /// </summary>
        /// <param name="tenantId">租户ID</param>
        /// <param name="createDefaultAdmin">是否创建默认管理员</param>
        /// <returns>初始化结果</returns>
        public async Task<ApiResponse> InitializeTenantUsersAsync(string tenantId, bool createDefaultAdmin = true)
        {
            try
            {
                _logger.LogInformation("为租户 {TenantId} 初始化默认用户...", tenantId);

                var userDefinitions = new List<UserDefinition>();
                var defaultUsers = GetDefaultUsersForTenant(tenantId, createDefaultAdmin);

                foreach (var (userName, email, displayName, password, roles) in defaultUsers)
                {
                    userDefinitions.Add(new UserDefinition
                    {
                        UserName = userName,
                        Email = email,
                        DisplayName = displayName,
                        Password = password,
                        TenantId = tenantId,
                        IsSystemUser = false,
                        Roles = roles
                    });
                }

                // 创建用户
                var createdUsers = await _userSeederService.CreateUsersBatchAsync(userDefinitions);

                // 分配角色
                foreach (var userDefinition in userDefinitions)
                {
                    var user = createdUsers.FirstOrDefault(u => u.UserName == userDefinition.UserName);
                    if (user != null)
                    {
                        _logger.LogInformation("为用户 {UserName} 分配角色: {Roles}", user.UserName, string.Join(", ", userDefinition.Roles));
                        
                        foreach (var roleName in userDefinition.Roles)
                        {
                            // 查询角色（角色应该在之前的步骤中已经创建）
                            // 使用WithoutMultiTenantFilterAsync确保能够查询到指定租户的角色
                            var role = await _context.WithoutMultiTenantFilterAsync(async () =>
                            {
                                return await _context.Roles
                                    .FirstOrDefaultAsync(r => r.TenantId == tenantId && r.Name == roleName);
                            });
                            
                            if (role != null)
                            {
                                await _userSeederService.EnsureUserRoleExistsAsync(user.Id, role.Id, tenantId);
                                _logger.LogInformation("用户 {UserName} (ID: {UserId}) 已分配角色 {RoleName} (ID: {RoleId})", 
                                    user.UserName, user.Id, role.Name, role.Id);
                            }
                            else
                            {
                                _logger.LogError("无法为用户 {UserName} 分配角色 {RoleName}：角色不存在", user.UserName, roleName);
                            }
                        }
                    }
                    else
                    {
                        _logger.LogWarning("未找到用户 {UserName}，无法分配角色", userDefinition.UserName);
                    }
                }

                _logger.LogInformation("租户 {TenantId} 默认用户初始化完成，共创建 {Count} 个用户", tenantId, userDefinitions.Count);
                return new ApiResponse(0, "默认用户初始化成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "租户 {TenantId} 用户初始化失败: {Message}", tenantId, ex.Message);
                return new ApiResponse(500, $"用户初始化失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取租户的默认角色定义
        /// </summary>
        /// <param name="tenantId">租户ID</param>
        /// <returns>角色定义列表</returns>
        public List<string> GetDefaultRolesForTenant(string tenantId)
        {
            // 为每个新租户创建的默认角色
            return new List<string>
            {
                "Admin",      // 管理员角色
                "User"       // 普通用户角色
            };
        }

        /// <summary>
        /// 获取租户的默认用户定义
        /// </summary>
        /// <param name="tenantId">租户ID</param>
        /// <param name="createDefaultAdmin">是否创建默认管理员</param>
        /// <returns>用户定义列表</returns>
        public List<(string UserName, string Email, string DisplayName, string Password, List<string> Roles)> GetDefaultUsersForTenant(string tenantId, bool createDefaultAdmin = true)
        {
            var users = new List<(string, string, string, string, List<string>)>();

            if (createDefaultAdmin)
            {
                // 创建默认管理员账户
                users.Add((
                    UserName: "admin",
                    Email: $"admin@{tenantId}.local",
                    DisplayName: "系统管理员",
                    Password: "Admin@123456",
                    Roles: new List<string> { "Admin" }
                ));
            }

            // 可以根据需要添加更多默认用户
            // 例如：演示用户、测试用户等
            
            return users;
        }

        /// <summary>
        /// 获取角色描述
        /// </summary>
        /// <param name="roleName">角色名称</param>
        /// <returns>角色描述</returns>
        private string GetRoleDescription(string roleName)
        {
            return roleName switch
            {
                "Admin" => "管理员角色，拥有系统的完全访问权限",
                "User" => "普通用户角色，拥有基本的业务功能访问权限",
                "Guest" => "访客角色，拥有有限的只读访问权限",
                _ => $"{roleName}角色"
            };
        }
    }
} 