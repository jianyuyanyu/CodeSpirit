using CodeSpirit.Core.IdGenerator;
using CodeSpirit.IdentityApi.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CodeSpirit.Core;

namespace CodeSpirit.IdentityApi.Data.Seeders
{
    /// <summary>
    /// 统一的角色种子数据服务
    /// </summary>
    public class UnifiedRoleSeederService : IRoleSeederService, IScopedDependency
    {
        private readonly ApplicationDbContext _context;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly ILogger<UnifiedRoleSeederService> _logger;
        private readonly IIdGenerator _idGenerator;
        private readonly IHostEnvironment _hostEnvironment;

        /// <summary>
        /// 构造函数
        /// </summary>
        public UnifiedRoleSeederService(
            ApplicationDbContext context,
            RoleManager<ApplicationRole> roleManager,
            ILogger<UnifiedRoleSeederService> logger,
            IIdGenerator idGenerator,
            IHostEnvironment hostEnvironment)
        {
            _context = context;
            _roleManager = roleManager;
            _logger = logger;
            _idGenerator = idGenerator;
            _hostEnvironment = hostEnvironment;
        }

        /// <summary>
        /// 确保角色存在
        /// </summary>
        public async Task<ApplicationRole> EnsureRoleExistsAsync(string roleName, string description, string tenantId)
        {
            // 查找现有角色
            var existingRole = await _context.Roles
                .FirstOrDefaultAsync(r => r.TenantId == tenantId &&
                                         (r.Name == roleName || r.NormalizedName == roleName.ToUpper()));

            if (existingRole == null)
            {
                _logger.LogInformation("创建角色: {RoleName} (租户: {TenantId})", roleName, tenantId);

                var newRole = new ApplicationRole
                {
                    Id = _idGenerator.NewId(),
                    TenantId = tenantId,
                    Name = roleName,
                    NormalizedName = roleName.ToUpper(),
                    Description = description,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = 1L,
                    IsDeleted = false
                };

                // 对于系统角色，使用DbContext直接操作
                // 对于业务角色，使用RoleManager
                if (tenantId == TenantConstants.SystemTenantId)
                {
                    _context.Roles.Add(newRole);
                }
                else
                {
                    var result = await _roleManager.CreateAsync(newRole);
                    if (!result.Succeeded)
                    {
                        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                        throw new InvalidOperationException($"创建角色失败: {errors}");
                    }
                }

                _logger.LogInformation("角色创建完成: {RoleName}, ID: {RoleId}", roleName, newRole.Id);
                return newRole;
            }
            else
            {
                _logger.LogInformation("角色已存在: {RoleName}, ID: {RoleId}", existingRole.Name, existingRole.Id);
                return existingRole;
            }
        }

        /// <summary>
        /// 批量创建角色
        /// </summary>
        public async Task<List<ApplicationRole>> CreateRolesBatchAsync(List<RoleDefinition> roles)
        {
            var createdRoles = new List<ApplicationRole>();

            foreach (var roleDefinition in roles)
            {
                var role = await EnsureRoleExistsAsync(roleDefinition.Name, roleDefinition.Description, roleDefinition.TenantId);
                createdRoles.Add(role);
            }

            return createdRoles;
        }

        /// <summary>
        /// 获取预定义的系统角色
        /// </summary>
        public List<RoleDefinition> GetSystemRoles()
        {
            return new List<RoleDefinition>
            {
                new()
                {
                    Name = "Admin",
                    Description = "系统管理员角色，拥有所有系统级权限",
                    TenantId = TenantConstants.SystemTenantId,
                    IsSystemRole = true
                },
                new()
                {
                    Name = "TenantOperator",
                    Description = "负责租户管理的运营人员",
                    TenantId = TenantConstants.SystemTenantId,
                    IsSystemRole = true
                },
                new()
                {
                    Name = "SystemAuditor",
                    Description = "系统审计和监控专员，拥有只读审计权限",
                    TenantId = TenantConstants.SystemTenantId,
                    IsSystemRole = true
                }
            };
        }

        /// <summary>
        /// 获取预定义的业务角色
        /// </summary>
        public List<RoleDefinition> GetBusinessRoles()
        {
            var roles = new List<RoleDefinition>
            {
                new()
                {
                    Name = "Admin",
                    Description = "系统管理员，拥有所有权限。",
                    TenantId = TenantConstants.DefaultTenantId,
                    IsSystemRole = false
                }
            };

            // 只在开发环境下添加额外的业务角色
            if (_hostEnvironment.IsDevelopment())
            {
                roles.AddRange(new[]
                {
                    new RoleDefinition
                    {
                        Name = "项目经理",
                        Description = "项目经理，负责项目管理和团队协调。",
                        TenantId = TenantConstants.DefaultTenantId,
                        IsSystemRole = false
                    },
                    new RoleDefinition
                    {
                        Name = "开发人员",
                        Description = "开发人员，负责编码和实现功能。",
                        TenantId = TenantConstants.DefaultTenantId,
                        IsSystemRole = false
                    },
                    new RoleDefinition
                    {
                        Name = "测试人员",
                        Description = "测试人员，负责软件测试和质量保证。",
                        TenantId = TenantConstants.DefaultTenantId,
                        IsSystemRole = false
                    },
                    new RoleDefinition
                    {
                        Name = "技术支持",
                        Description = "支持人员，提供技术支持和客户服务。",
                        TenantId = TenantConstants.DefaultTenantId,
                        IsSystemRole = false
                    },
                    new RoleDefinition
                    {
                        Name = "人力资源",
                        Description = "人力资源，管理员工信息和招聘流程。",
                        TenantId = TenantConstants.DefaultTenantId,
                        IsSystemRole = false
                    },
                    new RoleDefinition
                    {
                        Name = "财务人员",
                        Description = "财务人员，负责财务管理和预算控制。",
                        TenantId = TenantConstants.DefaultTenantId,
                        IsSystemRole = false
                    },
                    new RoleDefinition
                    {
                        Name = "销售人员",
                        Description = "销售人员，负责销售和市场推广。",
                        TenantId = TenantConstants.DefaultTenantId,
                        IsSystemRole = false
                    },
                    new RoleDefinition
                    {
                        Name = "市场营销",
                        Description = "市场营销，负责市场分析和营销策略。",
                        TenantId = TenantConstants.DefaultTenantId,
                        IsSystemRole = false
                    },
                    new RoleDefinition
                    {
                        Name = "访客",
                        Description = "访客，具有最低权限的用户。",
                        TenantId = TenantConstants.DefaultTenantId,
                        IsSystemRole = false
                    }
                });
            }

            return roles;
        }
    }
} 