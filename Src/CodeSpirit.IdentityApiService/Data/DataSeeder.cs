// Data/DataSeeder.cs
using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.IdentityApi.Data.Models.RoleManagementApiIdentity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;

namespace CodeSpirit.IdentityApi.Data
{
    public static class DataSeeder
    {
        public static async Task SeedRolesAndPermissionsAsync(IServiceProvider serviceProvider, ILogger<ApplicationDbContext> logger)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();
            await dbContext.Database.MigrateAsync();

            var roles = GetRoles();
            var permissions = GetPermissions();

            // 创建角色
            await CreateRolesAsync(roleManager, roles, logger);
            logger.LogInformation("角色创建完毕！");

            // 创建顶级权限
            await CreatePermissionsAsync(dbContext, permissions, logger);
            logger.LogInformation("权限创建完毕！");

            // 更新子权限的 ParentId
            await SetPermissionParentsAsync(dbContext, permissions, logger);
            logger.LogInformation("子权限更新完毕！");

            // 分配权限给角色
            await AssignPermissionsToRolesAsync(roleManager, dbContext, roles, permissions, logger);
            logger.LogInformation("权限分配完毕！");

            // 创建管理员用户
            await SeedAdminUserAsync(serviceProvider, logger, roleManager, userManager);
            logger.LogInformation("管理员创建完毕！");

            // 创建随机用户
            await SeedRandomUsersAsync(serviceProvider, 20, logger, roleManager, userManager);
            logger.LogInformation("随机用户创建完毕！");

            await dbContext.SaveChangesAsync();
        }

        private static List<ApplicationRole> GetRoles()
        {
            return new List<ApplicationRole>
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
        }

        private static List<Permission> GetPermissions()
        {
            return new List<Permission>
            {
                new Permission { Name = "UserManagement", Description = "用户管理权限" },
                new Permission { Name = "RoleManagement", Description = "角色管理权限" },
                new Permission { Name = "ReportManagement", Description = "报告管理权限" },
                new Permission { Name = "DataManagement", Description = "数据管理权限" },
                new Permission { Name = "SystemSettings", Description = "系统设置权限" },
                new Permission { Name = "view_users", Description = "查看用户" },
                new Permission { Name = "edit_users", Description = "编辑用户" },
                new Permission { Name = "add_users", Description = "新增用户" },
                new Permission { Name = "delete_users", Description = "删除用户" },
                new Permission { Name = "view_roles", Description = "查看角色" },
                new Permission { Name = "edit_roles", Description = "编辑角色" },
                new Permission { Name = "add_roles", Description = "新增角色" },
                new Permission { Name = "delete_roles", Description = "删除角色" },
                new Permission { Name = "view_reports", Description = "查看报告" },
                new Permission { Name = "export_reports", Description = "导出报告" },
                new Permission { Name = "view_data", Description = "查看数据" },
                new Permission { Name = "edit_data", Description = "编辑数据" },
                new Permission { Name = "import_data", Description = "导入数据" },
                new Permission { Name = "export_data", Description = "导出数据" },
                new Permission { Name = "configure_settings", Description = "配置设置" },
                new Permission { Name = "manage_system", Description = "管理系统" }
            };
        }

        private static async Task CreateRolesAsync(RoleManager<ApplicationRole> roleManager, List<ApplicationRole> roles, ILogger<ApplicationDbContext> logger)
        {
            foreach (var role in roles)
            {
                var roleExists = await roleManager.RoleExistsAsync(role.Name);
                if (!roleExists)
                {
                    var result = await roleManager.CreateAsync(role);
                    if (result.Succeeded)
                    {
                        logger.LogInformation($"角色 '{role.Name}' 创建成功。");
                    }
                    else
                    {
                        logger.LogError($"创建角色 '{role.Name}' 失败。错误：");
                        foreach (var error in result.Errors)
                        {
                            logger.LogError($" - {error.Description}");
                        }
                    }
                }
                else
                {
                    logger.LogInformation($"角色 '{role.Name}' 已存在，跳过创建。");
                }
            }
        }

        private static async Task CreatePermissionsAsync(ApplicationDbContext dbContext, List<Permission> permissions, ILogger<ApplicationDbContext> logger)
        {
            foreach (var permission in permissions.Where(p => p.ParentId == null))
            {
                if (!dbContext.Permissions.Any(p => p.Name == permission.Name))
                {
                    dbContext.Permissions.Add(permission);
                    logger.LogInformation($"权限 '{permission.Name}' 添加到数据库。");
                }
                else
                {
                    logger.LogInformation($"权限 '{permission.Name}' 已存在，跳过添加。");
                }
            }

            await dbContext.SaveChangesAsync();
        }

        private static async Task SetPermissionParentsAsync(ApplicationDbContext dbContext, List<Permission> permissions, ILogger<ApplicationDbContext> logger)
        {
            // 使用 ToLookup 代替 ToDictionary 以支持重复的 Name 键
            var permissionLookup = dbContext.Permissions.ToLookup(p => p.Name, p => p.Id);

            // 如果 permissionLookup 为空，返回
            if (permissionLookup.Count == 0)
            {
                logger.LogWarning("没有找到任何权限数据");
                return;
            }

            // 将所有权限按名称存入一个字典，以减少数据库查询次数
            var existingPermissions = dbContext.Permissions.ToList();

            // 遍历没有父权限的权限
            foreach (var permission in permissions.Where(p => p.ParentId == null))
            {
                // 计算下划线的位置，确保安全地处理
                var underscoreIndex = permission.Name.IndexOf('_');
                if (underscoreIndex > 0)
                {
                    // 找到所有与当前权限名称匹配的子权限
                    var children = permissions.Where(p => p.Name.StartsWith(permission.Name.Substring(0, underscoreIndex))).ToList();

                    // 遍历子权限
                    foreach (var child in children)
                    {
                        var existingPermission = existingPermissions.FirstOrDefault(p => p.Name == child.Name);
                        // 检查是否在已加载的权限字典中存在该子权限
                        if (existingPermission != null)
                        {
                            // 设置父权限
                            existingPermission.ParentId = permissionLookup[permission.Name].FirstOrDefault(); // 获取第一个匹配的父权限 ID
                        }
                        else
                        {
                            logger.LogWarning($"未找到子权限: {child.Name}");
                        }
                    }
                }
                else
                {
                    logger.LogWarning($"权限名称 {permission.Name} 不包含有效的下划线，无法确定父权限");
                }
            }

            // 保存所有更改
            await dbContext.SaveChangesAsync();
        }

        private static async Task AssignPermissionsToRolesAsync(
    RoleManager<ApplicationRole> roleManager,
    ApplicationDbContext dbContext,
    List<ApplicationRole> roles,
    List<Permission> permissions,
    ILogger<ApplicationDbContext> logger)
        {
            // 加载所有角色和权限关联关系一次，避免多次查询数据库
            var allRolePermissions = await dbContext.RolePermissions
                .Include(rp => rp.Permission)
                .ToListAsync();

            foreach (var role in roles)
            {
                // 获取角色
                var existingRole = await roleManager.Roles
                    .FirstOrDefaultAsync(r => r.Name == role.Name);

                if (existingRole != null)
                {
                    // 获取该角色对应的权限列表
                    var rolePermissions = GetPermissionsForRole(existingRole.Name, permissions);

                    // 确保 RolePermissions 已初始化
                    if (existingRole.RolePermissions == null)
                    {
                        existingRole.RolePermissions = new List<RolePermission>();
                    }

                    // 遍历角色需要的权限
                    foreach (var permission in rolePermissions)
                    {
                        // 如果该角色还没有该权限，则添加
                        if (!existingRole.RolePermissions.Any(rp => rp.PermissionId == permission.Id))
                        {
                            // 检查是否存在对应的权限
                            if (permissions.Any(p => p.Id == permission.Id))
                            {
                                existingRole.RolePermissions.Add(new RolePermission()
                                {
                                    IsAllowed = true,
                                    Permission = permission,
                                    Role = existingRole
                                });
                            }
                            else
                            {
                                logger.LogWarning($"权限 '{permission.Name}' 不存在，无法分配给角色 '{existingRole.Name}'。");
                            }
                        }
                    }

                    // 更新角色
                    var updateResult = await roleManager.UpdateAsync(existingRole);
                    if (updateResult.Succeeded)
                    {
                        logger.LogInformation($"权限已分配给角色 '{existingRole.Name}'。");
                    }
                    else
                    {
                        logger.LogError($"分配权限给角色 '{existingRole.Name}' 失败。错误：");
                        foreach (var error in updateResult.Errors)
                        {
                            logger.LogError($" - {error.Description}");
                        }
                    }
                }
                else
                {
                    logger.LogWarning($"角色 '{role.Name}' 未找到，跳过权限分配。");
                }
            }
        }

        private static async Task SeedAdminUserAsync(IServiceProvider serviceProvider, ILogger<ApplicationDbContext> logger, RoleManager<ApplicationRole> roleManager, UserManager<ApplicationUser> userManager)
        {
            // 查找管理员用户
            var adminUser = await userManager.FindByNameAsync("admin");

            if (adminUser == null)
            {
                // 如果没有找到，创建管理员用户
                adminUser = new ApplicationUser
                {
                    Id = Guid.NewGuid().ToString("N"),
                    UserName = "admin",
                    Email = "admin@example.com",
                    EmailConfirmed = true,
                    Name = "Admin", // 给管理员用户指定一个默认姓名
                    IsActive = true, // 设置用户为启用状态
                    Gender = Gender.Unknown // 默认性别
                };

                var result = await userManager.CreateAsync(adminUser, "Admin@123"); // 注意：密码应通过配置管理
                if (result.Succeeded)
                {
                    logger.LogInformation("管理员用户创建成功。");
                }
                else
                {
                    logger.LogError("创建管理员用户失败：");
                    foreach (var error in result.Errors)
                    {
                        logger.LogError($" - {error.Description}");
                    }
                    return; // 如果用户创建失败，后续操作不再执行
                }
            }
            else
            {
                logger.LogInformation("管理员用户已存在，跳过创建。");
            }

            // 检查管理员角色是否存在
            if (!await roleManager.RoleExistsAsync("Administrator"))
            {
                // 如果角色不存在，则创建角色
                var createRoleResult = await roleManager.CreateAsync(new ApplicationRole() { Name = "Administrator" });
                if (createRoleResult.Succeeded)
                {
                    logger.LogInformation("管理员角色创建成功。");
                }
                else
                {
                    logger.LogError("创建管理员角色失败：");
                    foreach (var error in createRoleResult.Errors)
                    {
                        logger.LogError($" - {error.Description}");
                    }
                    return; // 如果角色创建失败，后续操作不再执行
                }
            }

            // 检查管理员用户是否已拥有角色
            if (!await userManager.IsInRoleAsync(adminUser, "Administrator"))
            {
                var addToRoleResult = await userManager.AddToRoleAsync(adminUser, "Administrator");
                if (addToRoleResult.Succeeded)
                {
                    logger.LogInformation("管理员用户已分配 'Administrator' 角色。");
                }
                else
                {
                    logger.LogError("分配 'Administrator' 角色失败：");
                    foreach (var error in addToRoleResult.Errors)
                    {
                        logger.LogError($" - {error.Description}");
                    }
                }
            }
            else
            {
                logger.LogInformation("管理员用户已存在 'Administrator' 角色，跳过角色分配。");
            }
        }

        private static List<Permission> GetPermissionsForRole(string roleName, List<Permission> allPermissions)
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
                _ => new List<Permission>()
            };
        }

        private static async Task SeedRandomUsersAsync(IServiceProvider serviceProvider, int userCount, ILogger<ApplicationDbContext> logger, RoleManager<ApplicationRole> roleManager, UserManager<ApplicationUser> userManager)
        {
            var random = new Random();
            var genderValues = Enum.GetValues(typeof(Gender)).Cast<Gender>().ToArray(); // 获取所有性别值

            // 获取一个默认角色（如果存在）
            var defaultRole = await roleManager.FindByNameAsync("User");
            if (defaultRole == null)
            {
                logger.LogWarning("未找到默认角色 'User'，将跳过角色分配！");
            }

            for (int i = 0; i < userCount; i++)
            {
                var userName = $"user{random.Next(1000, 9999)}";
                // 使用 DiceBear Avatars API 生成随机头像 URL
                var avatarStyle = random.Next(0, 3); // 随机选择风格，0 -> Identicon, 1 -> Bottts, 2 -> Avataaars
                var avatarUrl = avatarStyle switch
                {
                    0 => $"https://avatars.dicebear.com/api/identicon/{userName}.svg",
                    1 => $"https://avatars.dicebear.com/api/bottts/{userName}.svg",
                    2 => $"https://avatars.dicebear.com/api/avataaars/{userName}.svg",
                    _ => $"https://avatars.dicebear.com/api/identicon/{userName}.svg",
                };

                // 随机生成 LastLoginTime（过去三个月内）
                var lastLoginTime = GetRandomDate(DateTime.Now.AddMonths(-3), DateTime.Now);
                var createTime = GetRandomDate(DateTime.Now.AddMonths(-1), DateTime.Now);

                var user = new ApplicationUser
                {
                    Id = Guid.NewGuid().ToString("N"),
                    UserName = userName,
                    Email = $"{userName}@example.com",
                    LastLoginTime = lastLoginTime, // 随机生成 LastLoginTime
                    EmailConfirmed = true,
                    Name = $"User {random.Next(1000, 9999)}", // 随机生成用户姓名
                    Gender = genderValues[random.Next(genderValues.Length)], // 随机分配性别
                    IsActive = random.Next(1, 10) % 2 == 0,
                    AvatarUrl = avatarUrl,
                    CreationTime = createTime.DateTime
                };

                if (i > 5)
                {
                    user.LockoutEnd = DateTimeOffset.Now.AddHours(5);
                }

                // 创建用户
                var result = await userManager.CreateAsync(user, "Password@123");
                if (result.Succeeded)
                {
                    logger.LogInformation($"用户 {user.UserName} 创建成功。");

                    // 如果有默认角色，将用户添加到该角色中
                    if (defaultRole != null)
                    {
                        var addToRoleResult = await userManager.AddToRoleAsync(user, defaultRole.Name);
                        if (addToRoleResult.Succeeded)
                        {
                            logger.LogInformation($"用户 {user.UserName} 被分配到角色 '{defaultRole.Name}'。");
                        }
                        else
                        {
                            logger.LogError($"分配角色 '{defaultRole.Name}' 给用户 {user.UserName} 失败：");
                            foreach (var error in addToRoleResult.Errors)
                            {
                                logger.LogError($" - {error.Description}");
                            }
                        }
                    }
                }
                else
                {
                    logger.LogError($"创建用户 {user.UserName} 失败：");
                    foreach (var error in result.Errors)
                    {
                        logger.LogError($" - {error.Description}");
                    }
                }
            }
        }

        // 随机生成一个日期在指定时间范围内
        private static DateTimeOffset GetRandomDate(DateTime startDate, DateTime endDate)
        {
            var random = new Random();
            var range = endDate - startDate;
            var randomTimeSpan = new TimeSpan((long)(random.NextDouble() * range.Ticks));
            return startDate + randomTimeSpan;
        }
    }
}
