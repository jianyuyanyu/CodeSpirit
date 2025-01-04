// Data/DataSeeder.cs
using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.IdentityApi.Data.Models.RoleManagementApiIdentity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CodeSpirit.IdentityApi.Data
{
    public static class DataSeeder
    {
        public static async Task SeedRolesAndPermissionsAsync(IServiceProvider serviceProvider)
        {
            // 获取 RoleManager 和 ApplicationDbContext 服务
            var roleManager = serviceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
            var dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();
            await dbContext.Database.MigrateAsync();

            // 定义要创建的角色列表
            var roles = new List<ApplicationRole>
            {
                new ApplicationRole { Name = "Administrator", Description = "系统管理员，拥有所有权限。" },
                new ApplicationRole { Name = "Manager", Description = "项目经理，负责项目管理和团队协调。" },
                new ApplicationRole { Name = "Developer", Description = "开发人员，负责编码和实现功能。" },
                new ApplicationRole { Name = "Tester", Description = "测试人员，负责软件测试和质量保证。" },
                new ApplicationRole { Name = "Support", Description = "支持人员，提供技术支持和客户服务。" },
                new ApplicationRole { Name = "HR", Description = "人力资源，管理员工信息和招聘流程。" },
                new ApplicationRole { Name = "Finance", Description = "财务人员，负责财务管理和预算控制。" },
                new ApplicationRole { Name = "Sales", Description = "销售人员，负责销售和市场推广。" },
                new ApplicationRole { Name = "Marketing", Description = "市场营销，负责市场分析和营销策略。" },
                new ApplicationRole { Name = "Guest", Description = "访客，具有最低权限的用户。" }
            };

            // 创建角色
            foreach (var role in roles)
            {
                var roleExists = await roleManager.RoleExistsAsync(role.Name);
                if (!roleExists)
                {
                    var result = await roleManager.CreateAsync(role);
                    if (result.Succeeded)
                    {
                        Console.WriteLine($"角色 '{role.Name}' 创建成功。");
                    }
                    else
                    {
                        Console.WriteLine($"创建角色 '{role.Name}' 失败。错误：");
                        foreach (var error in result.Errors)
                        {
                            Console.WriteLine($" - {error.Description}");
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"角色 '{role.Name}' 已存在，跳过创建。");
                }
            }

            // 定义要创建的权限列表（支持多级权限）
            var permissions = new List<Permission>
            {
                // 顶级权限
                new Permission { Name = "UserManagement", Description = "用户管理权限" },
                new Permission { Name = "RoleManagement", Description = "角色管理权限" },
                new Permission { Name = "ReportManagement", Description = "报告管理权限" },
                new Permission { Name = "DataManagement", Description = "数据管理权限" },
                new Permission { Name = "SystemSettings", Description = "系统设置权限" },

                // 子权限
                new Permission { Name = "view_users", Description = "查看用户", ParentId = null }, // 待关联
                new Permission { Name = "edit_users", Description = "编辑用户", ParentId = null },
                new Permission { Name = "add_users", Description = "新增用户", ParentId = null },
                new Permission { Name = "delete_users", Description = "删除用户", ParentId = null },

                new Permission { Name = "view_roles", Description = "查看角色", ParentId = null },
                new Permission { Name = "edit_roles", Description = "编辑角色", ParentId = null },
                new Permission { Name = "add_roles", Description = "新增角色", ParentId = null },
                new Permission { Name = "delete_roles", Description = "删除角色", ParentId = null },

                new Permission { Name = "view_reports", Description = "查看报告", ParentId = null },
                new Permission { Name = "export_reports", Description = "导出报告", ParentId = null },

                new Permission { Name = "view_data", Description = "查看数据", ParentId = null },
                new Permission { Name = "edit_data", Description = "编辑数据", ParentId = null },
                new Permission { Name = "import_data", Description = "导入数据", ParentId = null },
                new Permission { Name = "export_data", Description = "导出数据", ParentId = null },

                new Permission { Name = "configure_settings", Description = "配置设置", ParentId = null },
                new Permission { Name = "manage_system", Description = "管理系统", ParentId = null }
            };

            // 先添加所有顶级权限
            foreach (var permission in permissions.Where(p => p.ParentId == null))
            {
                if (!dbContext.Permissions.Any(p => p.Name == permission.Name))
                {
                    dbContext.Permissions.Add(permission);
                    Console.WriteLine($"权限 '{permission.Name}' 添加到数据库。");
                }
                else
                {
                    Console.WriteLine($"权限 '{permission.Name}' 已存在，跳过添加。");
                }
            }

            await dbContext.SaveChangesAsync();

            // 获取顶级权限的 ID
            var userManagement = dbContext.Permissions.FirstOrDefault(p => p.Name == "UserManagement");
            var roleManagement = dbContext.Permissions.FirstOrDefault(p => p.Name == "RoleManagement");
            var reportManagement = dbContext.Permissions.FirstOrDefault(p => p.Name == "ReportManagement");
            var dataManagement = dbContext.Permissions.FirstOrDefault(p => p.Name == "DataManagement");
            var systemSettings = dbContext.Permissions.FirstOrDefault(p => p.Name == "SystemSettings");

            // 更新子权限的 ParentId
            foreach (var permission in permissions.Where(p => p.ParentId == null))
            {
                switch (permission.Name)
                {
                    case "UserManagement":
                        var umChildren = permissions.Where(p => p.Name.StartsWith("view_users") || p.Name.StartsWith("edit_users") || p.Name.StartsWith("add_users") || p.Name.StartsWith("delete_users")).ToList();
                        foreach (var child in umChildren)
                        {
                            var existingPermission = dbContext.Permissions.FirstOrDefault(p => p.Name == child.Name);
                            if (existingPermission != null)
                            {
                                existingPermission.ParentId = userManagement.Id;
                            }
                        }
                        break;
                    case "RoleManagement":
                        var rmChildren = permissions.Where(p => p.Name.StartsWith("view_roles") || p.Name.StartsWith("edit_roles") || p.Name.StartsWith("add_roles") || p.Name.StartsWith("delete_roles")).ToList();
                        foreach (var child in rmChildren)
                        {
                            var existingPermission = dbContext.Permissions.FirstOrDefault(p => p.Name == child.Name);
                            if (existingPermission != null)
                            {
                                existingPermission.ParentId = roleManagement.Id;
                            }
                        }
                        break;
                    case "ReportManagement":
                        var repmChildren = permissions.Where(p => p.Name.StartsWith("view_reports") || p.Name.StartsWith("export_reports")).ToList();
                        foreach (var child in repmChildren)
                        {
                            var existingPermission = dbContext.Permissions.FirstOrDefault(p => p.Name == child.Name);
                            if (existingPermission != null)
                            {
                                existingPermission.ParentId = reportManagement.Id;
                            }
                        }
                        break;
                    case "DataManagement":
                        var datmChildren = permissions.Where(p => p.Name.StartsWith("view_data") || p.Name.StartsWith("edit_data") || p.Name.StartsWith("import_data") || p.Name.StartsWith("export_data")).ToList();
                        foreach (var child in datmChildren)
                        {
                            var existingPermission = dbContext.Permissions.FirstOrDefault(p => p.Name == child.Name);
                            if (existingPermission != null)
                            {
                                existingPermission.ParentId = dataManagement.Id;
                            }
                        }
                        break;
                    case "SystemSettings":
                        var sysmChildren = permissions.Where(p => p.Name.StartsWith("configure_settings") || p.Name.StartsWith("manage_system")).ToList();
                        foreach (var child in sysmChildren)
                        {
                            var existingPermission = dbContext.Permissions.FirstOrDefault(p => p.Name == child.Name);
                            if (existingPermission != null)
                            {
                                existingPermission.ParentId = systemSettings.Id;
                            }
                        }
                        break;
                    default:
                        break;
                }
            }

            await dbContext.SaveChangesAsync();

            // 再次创建子权限（确保 ParentId 已设置）
            foreach (var permission in permissions.Where(p => p.ParentId != null))
            {
                var existingPermission = dbContext.Permissions.FirstOrDefault(p => p.Name == permission.Name);
                if (existingPermission != null && existingPermission.ParentId.HasValue)
                {
                    Console.WriteLine($"权限 '{existingPermission.Name}' 的父权限 ID 为 {existingPermission.ParentId.Value}。");
                }
            }

            // 定义要创建的子权限列表
            var childPermissions = permissions.Where(p => p.ParentId != null).ToList();

            // 确保所有子权限已添加
            foreach (var child in childPermissions)
            {
                var existingPermission = dbContext.Permissions.FirstOrDefault(p => p.Name == child.Name);
                if (existingPermission != null && existingPermission.ParentId.HasValue)
                {
                    // 关联父权限
                    var parentPermission = dbContext.Permissions.Find(existingPermission.ParentId.Value);
                    if (parentPermission != null && !parentPermission.Children.Contains(existingPermission))
                    {
                        parentPermission.Children.Add(existingPermission);
                    }
                }
            }

            await dbContext.SaveChangesAsync();

            // 分配权限给角色
            foreach (var role in roles)
            {
                var existingRole = await roleManager.Roles
                    .Include(r => r.RolePermissions)
                    .ThenInclude(p => p.Permission.Children)
                    .FirstOrDefaultAsync(r => r.Name == role.Name);

                if (existingRole != null)
                {
                    // 根据角色名称分配不同的权限
                    var rolePermissions = GetPermissionsForRole(existingRole.Name, dbContext.Permissions.ToList());

                    foreach (var permission in rolePermissions)
                    {
                        if (!existingRole.RolePermissions.Any(p => p.PermissionId == permission.Id))
                        {
                            existingRole.RolePermissions.Add(new RolePermission() { IsAllowed = true, Permission = permission, Role = existingRole });
                        }
                    }

                    var updateResult = await roleManager.UpdateAsync(existingRole);
                    if (updateResult.Succeeded)
                    {
                        Console.WriteLine($"权限已分配给角色 '{existingRole.Name}'。");
                    }
                    else
                    {
                        Console.WriteLine($"分配权限给角色 '{existingRole.Name}' 失败。错误：");
                        foreach (var error in updateResult.Errors)
                        {
                            Console.WriteLine($" - {error.Description}");
                        }
                    }
                }
            }

            await dbContext.SaveChangesAsync();

            // 调用 SeedAdminUserAsync
            await SeedAdminUserAsync(serviceProvider);
        }

        public static async Task SeedAdminUserAsync(IServiceProvider serviceProvider)
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

            // 创建管理员用户
            var adminUser = await userManager.FindByNameAsync("admin");
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = "admin",
                    Email = "admin@example.com",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(adminUser, "Admin@123"); // 确保密码符合策略
                if (result.Succeeded)
                {
                    Console.WriteLine("管理员用户创建成功。");
                }
                else
                {
                    Console.WriteLine("创建管理员用户失败：");
                    foreach (var error in result.Errors)
                    {
                        Console.WriteLine($" - {error.Description}");
                    }
                }
            }

            // 确保管理员用户拥有 Administrator 角色
            if (!await userManager.IsInRoleAsync(adminUser, "Administrator"))
            {
                var result = await userManager.AddToRoleAsync(adminUser, "Administrator");
                if (result.Succeeded)
                {
                    Console.WriteLine("管理员用户已分配 'Administrator' 角色。");
                }
                else
                {
                    Console.WriteLine("分配 'Administrator' 角色失败：");
                    foreach (var error in result.Errors)
                    {
                        Console.WriteLine($" - {error.Description}");
                    }
                }
            }
        }

        private static List<Permission> GetPermissionsForRole(string roleName, List<Permission> allPermissions)
        {
            var permissions = new List<Permission>();

            switch (roleName)
            {
                case "Administrator":
                    permissions = allPermissions; // 所有权限
                    break;
                case "Manager":
                    permissions = allPermissions.Where(p =>
                        p.Name == "view_users" || p.Name == "edit_users" ||
                        p.Name == "add_users" || p.Name == "delete_users" ||
                        p.Name == "view_reports" || p.Name == "export_reports" ||
                        p.Name == "view_data" || p.Name == "export_data").ToList();
                    break;
                case "Developer":
                    permissions = allPermissions.Where(p =>
                        p.Name == "view_users" || p.Name == "edit_users" ||
                        p.Name == "add_users" || p.Name == "view_data" ||
                        p.Name == "edit_data").ToList();
                    break;
                case "Tester":
                    permissions = allPermissions.Where(p =>
                        p.Name == "view_users" || p.Name == "view_data").ToList();
                    break;
                case "Support":
                    permissions = allPermissions.Where(p =>
                        p.Name == "view_users" || p.Name == "edit_users" ||
                        p.Name == "view_data").ToList();
                    break;
                case "HR":
                    permissions = allPermissions.Where(p =>
                        p.Name == "view_users" || p.Name == "manage_users").ToList();
                    break;
                case "Finance":
                    permissions = allPermissions.Where(p =>
                        p.Name == "view_data" || p.Name == "export_data" ||
                        p.Name == "import_data").ToList();
                    break;
                case "Sales":
                    permissions = allPermissions.Where(p =>
                        p.Name == "view_users" || p.Name == "add_users" ||
                        p.Name == "export_data").ToList();
                    break;
                case "Marketing":
                    permissions = allPermissions.Where(p =>
                        p.Name == "view_reports" || p.Name == "view_data").ToList();
                    break;
                case "Guest":
                    permissions = allPermissions.Where(p =>
                        p.Name == "view_users").ToList();
                    break;
                default:
                    break;
            }

            return permissions;
        }
    }
}
