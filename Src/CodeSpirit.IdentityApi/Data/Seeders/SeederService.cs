using CodeSpirit.IdentityApi.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
namespace CodeSpirit.IdentityApi.Data.Seeders;
public class SeederService: IScopedDependency
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SeederService> _logger;

    public SeederService(IServiceProvider serviceProvider, ILogger<SeederService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        using (IServiceScope scope = _serviceProvider.CreateScope())
        {
            RoleManager<ApplicationRole> roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
            UserManager<ApplicationUser> userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            try
            {
                // 临时设置一个系统用户ID用于审计字段
                dbContext.UserId = -1;  // 使用-1作为系统用户ID

                // 应用迁移
                await dbContext.Database.MigrateAsync();

                // 初始化各个 Seeder
                RoleSeeder roleSeeder = scope.ServiceProvider.GetRequiredService<RoleSeeder>();
                UserSeeder userSeeder = scope.ServiceProvider.GetRequiredService<UserSeeder>();

                // 获取角色和权限数据
                List<ApplicationRole> roles = roleSeeder.GetRoles();

                // 创建角色
                await roleSeeder.SeedRolesAsync(roles);
                _logger.LogInformation("角色创建完毕！");

                // 创建管理员用户
                await userSeeder.SeedAdminUserAsync();
                _logger.LogInformation("管理员创建完毕！");

                // 创建随机用户
                await userSeeder.SeedRandomUsersAsync(20, roleManager);
                _logger.LogInformation("随机用户创建完毕！");

                // 保存更改
                await dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "数据初始化失败：{Message}", ex.Message);
            }
        }
    }
}



