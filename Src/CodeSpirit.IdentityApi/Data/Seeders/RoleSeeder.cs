using CodeSpirit.Core.IdGenerator;
using CodeSpirit.IdentityApi.Data.Models;
using Microsoft.AspNetCore.Identity;

namespace CodeSpirit.IdentityApi.Data.Seeders
{
    public class RoleSeeder
    {
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly ILogger<RoleSeeder> _logger;
        private readonly IIdGenerator _idGenerator;

        public RoleSeeder(
            RoleManager<ApplicationRole> roleManager,
            ILogger<RoleSeeder> logger,
            IIdGenerator idGenerator)
        {
            _roleManager = roleManager;
            _logger = logger;
            _idGenerator = idGenerator;
        }

        public async Task SeedRolesAsync(List<ApplicationRole> roles)
        {
            foreach (ApplicationRole role in roles)
            {
                bool roleExists = await _roleManager.RoleExistsAsync(role.Name);
                if (!roleExists)
                {
                    IdentityResult result = await _roleManager.CreateAsync(role);
                    if (result.Succeeded)
                    {
                        _logger.LogInformation($"角色 '{role.Name}' 创建成功。");
                    }
                    else
                    {
                        _logger.LogError($"创建角色 '{role.Name}' 失败。错误：");
                        foreach (IdentityError error in result.Errors)
                        {
                            _logger.LogError($" - {error.Description}");
                        }
                    }
                }
                else
                {
                    _logger.LogInformation($"角色 '{role.Name}' 已存在，跳过创建。");
                }
            }
        }

        public List<ApplicationRole> GetRoles()
        {
            return
            [
                new ApplicationRole { Id = _idGenerator.NewId(), Name = "Admin", Description = "系统管理员，拥有所有权限。" },
                new ApplicationRole { Id = _idGenerator.NewId(), Name = "项目经理", Description = "项目经理，负责项目管理和团队协调。" },
                new ApplicationRole { Id = _idGenerator.NewId(), Name = "开发人员", Description = "开发人员，负责编码和实现功能。" },
                new ApplicationRole { Id = _idGenerator.NewId(), Name = "测试人员", Description = "测试人员，负责软件测试和质量保证。" },
                new ApplicationRole { Id = _idGenerator.NewId(), Name = "技术支持", Description = "支持人员，提供技术支持和客户服务。" },
                new ApplicationRole { Id = _idGenerator.NewId(), Name = "人力资源", Description = "人力资源，管理员工信息和招聘流程。" },
                new ApplicationRole { Id = _idGenerator.NewId(), Name = "财务人员", Description = "财务人员，负责财务管理和预算控制。" },
                new ApplicationRole { Id = _idGenerator.NewId(), Name = "销售人员", Description = "销售人员，负责销售和市场推广。" },
                new ApplicationRole { Id = _idGenerator.NewId(), Name = "市场营销", Description = "市场营销，负责市场分析和营销策略。" },
                new ApplicationRole { Id = _idGenerator.NewId(), Name = "访客", Description = "访客，具有最低权限的用户。" }
            ];
        }
    }
}

