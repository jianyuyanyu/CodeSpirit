using CodeSpirit.IdentityApi.Data;
using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.IdentityApi.Data.Models.RoleManagementApiIdentity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

public class RolePermissionAssigner
{
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<RolePermissionAssigner> _logger;

    public RolePermissionAssigner(
        RoleManager<ApplicationRole> roleManager,
        ApplicationDbContext dbContext,
        ILogger<RolePermissionAssigner> logger)
    {
        _roleManager = roleManager;
        _dbContext = dbContext;
        _logger = logger;
    }

    public List<Permission> GetPermissionsForRole(string roleName, List<Permission> allPermissions)
    {
        return roleName switch
        {
            "Administrator" => allPermissions,  // 所有权限
            "Manager" => allPermissions.Where(p =>
                p.Name == "view_users" || p.Name == "edit_users" ||
                p.Name == "add_users" || p.Name == "delete_users" ||
                p.Name == "view_reports" || p.Name == "export_reports" ||
                p.Name == "view_data" || p.Name == "export_data").ToList(),
            "Developer" => allPermissions.Where(p =>
                p.Name == "view_users" || p.Name == "edit_users" ||
                p.Name == "add_users" || p.Name == "view_data" ||
                p.Name == "edit_data").ToList(),
            "Tester" => allPermissions.Where(p =>
                p.Name == "view_users" || p.Name == "view_data").ToList(),
            "Support" => allPermissions.Where(p =>
                p.Name == "view_users" || p.Name == "edit_users" ||
                p.Name == "view_data").ToList(),
            "HR" => allPermissions.Where(p =>
                p.Name == "view_users" || p.Name == "manage_users").ToList(),
            "Finance" => allPermissions.Where(p =>
                p.Name == "view_data" || p.Name == "export_data" ||
                p.Name == "import_data").ToList(),
            "Sales" => allPermissions.Where(p =>
                p.Name == "view_users" || p.Name == "add_users" ||
                p.Name == "export_data").ToList(),
            "Marketing" => allPermissions.Where(p =>
                p.Name == "view_reports" || p.Name == "view_data").ToList(),
            "Guest" => allPermissions.Where(p =>
                p.Name == "view_users").ToList(),
            _ => []
        };
    }

    public async Task AssignPermissionsToRolesAsync(List<ApplicationRole> roles)
    {
        List<Permission> permissions = await _dbContext.Permissions.ToListAsync();
        List<RolePermission> allRolePermissions = await _dbContext.RolePermissions
            .Include(rp => rp.Permission)
            .ToListAsync();

        if (allRolePermissions.Any())
        {
            _logger.LogInformation("角色权限已存在，跳过权限分配。");
            return;
        }

        foreach (ApplicationRole role in roles)
        {
            ApplicationRole existingRole = await _roleManager.Roles
                .FirstOrDefaultAsync(r => r.Name == role.Name);

            if (existingRole != null)
            {
                List<Permission> rolePermissions = GetPermissionsForRole(existingRole.Name, permissions);

                if (existingRole.RolePermissions == null)
                {
                    existingRole.RolePermissions = [];
                }

                foreach (Permission permission in rolePermissions)
                {
                    if (!existingRole.RolePermissions.Any(rp => rp.PermissionId == permission.Id))
                    {
                        if (permissions.Any(p => p.Id == permission.Id))
                        {
                            existingRole.RolePermissions.Add(new RolePermission()
                            {
                                Permission = permission,
                                Role = existingRole
                            });
                        }
                        else
                        {
                            _logger.LogWarning($"权限 '{permission.Name}' 不存在，无法分配给角色 '{existingRole.Name}'。");
                        }
                    }
                }

                IdentityResult updateResult = await _roleManager.UpdateAsync(existingRole);
                if (updateResult.Succeeded)
                {
                    _logger.LogInformation($"权限已分配给角色 '{existingRole.Name}'。");
                }
                else
                {
                    _logger.LogError($"分配权限给角色 '{existingRole.Name}' 失败。错误：");
                    foreach (IdentityError error in updateResult.Errors)
                    {
                        _logger.LogError($" - {error.Description}");
                    }
                }
            }
            else
            {
                _logger.LogWarning($"角色 '{role.Name}' 未找到，跳过权限分配。");
            }
        }
    }
}



