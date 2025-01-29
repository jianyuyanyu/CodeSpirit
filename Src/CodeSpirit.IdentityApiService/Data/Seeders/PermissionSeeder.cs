using CodeSpirit.IdentityApi.Data;
using CodeSpirit.IdentityApi.Data.Models.RoleManagementApiIdentity.Models;
using Microsoft.EntityFrameworkCore;

public class PermissionSeeder
{
    /// <summary>
    /// 权限种子模型，包含权限名称、描述和可选的父权限名称。
    /// </summary>
    public class PermissionSeedModel
    {
        /// <summary>
        /// 权限名称，唯一且必填。
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 权限描述。
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 父权限名称，用于建立权限的层级关系。
        /// 可选，如果为空则表示这是一个父权限。
        /// </summary>
        public string ParentName { get; set; }
    }

    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<PermissionSeeder> _logger;

    public PermissionSeeder(ApplicationDbContext dbContext, ILogger<PermissionSeeder> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// 获取权限列表，包括父权限和子权限。
    /// 子权限通过 ParentName 属性关联到父权限。
    /// </summary>
    /// <returns>权限列表</returns>
    public List<PermissionSeedModel> GetPermissions()
    {
        return new List<PermissionSeedModel>
            {
                // 父权限
                new PermissionSeedModel { Name = "UserManagement", Description = "用户管理权限" },
                new PermissionSeedModel { Name = "RoleManagement", Description = "角色管理权限" },
                new PermissionSeedModel { Name = "ReportManagement", Description = "报告管理权限" },
                new PermissionSeedModel { Name = "DataManagement", Description = "数据管理权限" },
                new PermissionSeedModel { Name = "SystemSettings", Description = "系统设置权限" },

                // 子权限 - 用户管理
                new PermissionSeedModel { Name = "view_users", Description = "查看用户", ParentName = "UserManagement" },
                new PermissionSeedModel { Name = "edit_users", Description = "编辑用户", ParentName = "UserManagement" },
                new PermissionSeedModel { Name = "add_users", Description = "新增用户", ParentName = "UserManagement" },
                new PermissionSeedModel { Name = "delete_users", Description = "删除用户", ParentName = "UserManagement" },

                // 子权限 - 角色管理
                new PermissionSeedModel { Name = "view_roles", Description = "查看角色", ParentName = "RoleManagement" },
                new PermissionSeedModel { Name = "edit_roles", Description = "编辑角色", ParentName = "RoleManagement" },
                new PermissionSeedModel { Name = "add_roles", Description = "新增角色", ParentName = "RoleManagement" },
                new PermissionSeedModel { Name = "delete_roles", Description = "删除角色", ParentName = "RoleManagement" },

                // 子权限 - 报告管理
                new PermissionSeedModel { Name = "view_reports", Description = "查看报告", ParentName = "ReportManagement" },
                new PermissionSeedModel { Name = "export_reports", Description = "导出报告", ParentName = "ReportManagement" },

                // 子权限 - 数据管理
                new PermissionSeedModel { Name = "view_data", Description = "查看数据", ParentName = "DataManagement" },
                new PermissionSeedModel { Name = "edit_data", Description = "编辑数据", ParentName = "DataManagement" },
                new PermissionSeedModel { Name = "import_data", Description = "导入数据", ParentName = "DataManagement" },
                new PermissionSeedModel { Name = "export_data", Description = "导出数据", ParentName = "DataManagement" },

                // 子权限 - 系统设置
                new PermissionSeedModel { Name = "configure_settings", Description = "配置设置", ParentName = "SystemSettings" },
                new PermissionSeedModel { Name = "manage_system", Description = "管理系统", ParentName = "SystemSettings" }
            };
    }

    /// <summary>
    /// 种子权限到数据库，包括父权限和子权限。
    /// </summary>
    /// <returns>异步任务</returns>
    public async Task SeedPermissionsAsync()
    {
        var permissions = GetPermissions();

        // 分离父权限和子权限
        var parentPermissions = permissions.Where(p => string.IsNullOrEmpty(p.ParentName)).ToList();
        var childPermissions = permissions.Where(p => !string.IsNullOrEmpty(p.ParentName)).ToList();

        // 使用事务确保种子操作的原子性
        using var transaction = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            // 添加或更新父权限
            foreach (var parent in parentPermissions)
            {
                var existingParent = await _dbContext.Permissions
                    .FirstOrDefaultAsync(p => p.Name == parent.Name);

                if (existingParent == null)
                {
                    var newParent = new Permission
                    {
                        Name = parent.Name,
                        Description = parent.Description,
                        IsAllowed = true // 默认允许
                    };
                    _dbContext.Permissions.Add(newParent);
                    _logger.LogInformation($"权限 '{parent.Name}' 已添加到数据库。");
                }
                else
                {
                    // 可选：更新描述或其他属性
                    if (existingParent.Description != parent.Description)
                    {
                        existingParent.Description = parent.Description;
                        _logger.LogInformation($"权限 '{parent.Name}' 的描述已更新。");
                    }
                    else
                    {
                        _logger.LogInformation($"权限 '{parent.Name}' 已存在，跳过添加。");
                    }
                }
            }

            await _dbContext.SaveChangesAsync();

            // 获取父权限的 ID 映射
            var parentPermissionDict = await _dbContext.Permissions
                .Where(p => parentPermissions.Select(pp => pp.Name).Contains(p.Name))
                .ToDictionaryAsync(p => p.Name, p => p.Id);

            // 添加或更新子权限
            foreach (var child in childPermissions)
            {
                var existingChild = await _dbContext.Permissions
                    .FirstOrDefaultAsync(p => p.Name == child.Name);

                if (existingChild == null)
                {
                    if (parentPermissionDict.TryGetValue(child.ParentName, out var parentId))
                    {
                        var newChild = new Permission
                        {
                            Name = child.Name,
                            Description = child.Description,
                            ParentId = parentId,
                            IsAllowed = true // 默认允许
                        };
                        _dbContext.Permissions.Add(newChild);
                        _logger.LogInformation($"权限 '{child.Name}' 已添加到数据库。");
                    }
                    else
                    {
                        _logger.LogWarning($"未找到父权限 '{child.ParentName}'，无法添加子权限 '{child.Name}'。");
                    }
                }
                else
                {
                    // 可选：验证并更新 ParentId 和其他属性
                    bool updated = false;

                    if (existingChild.ParentId != parentPermissionDict.GetValueOrDefault(child.ParentName))
                    {
                        existingChild.ParentId = parentPermissionDict[child.ParentName];
                        updated = true;
                    }

                    if (existingChild.Description != child.Description)
                    {
                        existingChild.Description = child.Description;
                        updated = true;
                    }

                    if (updated)
                    {
                        _logger.LogInformation($"权限 '{child.Name}' 已更新。");
                    }
                    else
                    {
                        _logger.LogInformation($"权限 '{child.Name}' 已存在，跳过添加。");
                    }
                }
            }

            await _dbContext.SaveChangesAsync();

            // 提交事务
            await transaction.CommitAsync();
            _logger.LogInformation("所有权限已成功种子到数据库。");
        }
        catch (Exception ex)
        {
            // 回滚事务
            await transaction.RollbackAsync();
            _logger.LogError(ex, "种子权限时发生错误，事务已回滚。");
            throw; // 可选：根据需要重新抛出异常

        }
    }
}



