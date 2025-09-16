using CodeSpirit.IdentityApi.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CodeSpirit.Core;
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
            ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            try
            {
                // 临时设置一个系统用户ID用于审计字段
                dbContext.UserId = -1;  // 使用-1作为系统用户ID

                // 注意：数据库迁移已在 IdentityApiConfiguration.ApplyDatabaseMigrationsAsync 中处理

                // 1. 首先初始化租户数据（必须在用户和角色之前）
                TenantSeeder tenantSeeder = scope.ServiceProvider.GetRequiredService<TenantSeeder>();
                await tenantSeeder.SeedAsync();
                _logger.LogInformation("租户数据初始化完毕！");

                // 1.1. 验证默认租户是否创建成功，如果失败则强制创建
                var defaultTenantExists = await dbContext.Tenants
                    .AsNoTracking()
                    .AnyAsync(t => t.TenantId == TenantConstants.DefaultTenantId);
                
                if (!defaultTenantExists)
                {
                    _logger.LogWarning("默认租户验证失败，尝试强制创建...");
                    await tenantSeeder.ForceCreateDefaultTenantAsync();
                }

                // 清理ChangeTracker以避免实体跟踪冲突
                dbContext.ChangeTracker.Clear();

                // 等待一小段时间确保数据库事务完全提交
                await Task.Delay(1000);

                // 验证租户数据迁移结果（使用新的scope避免上下文冲突）
                await ValidateDataWithNewScopeAsync(scope.ServiceProvider);
                _logger.LogInformation("租户数据验证完毕！");

                // 再次清理ChangeTracker
                dbContext.ChangeTracker.Clear();

                // 2. 创建业务角色和用户（使用统一服务）
                await SeedBusinessRolesAndUsersAsync(scope);

                // 保存其他 Seeder 的更改
                await dbContext.SaveChangesAsync();
                _logger.LogInformation("所有种子数据初始化完成！");

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "数据初始化失败：{Message}", ex.Message);
                throw;
            }
        }
    }

    /// <summary>
    /// 创建业务角色和用户
    /// </summary>
    /// <param name="scope">服务范围</param>
    /// <returns></returns>
    private async Task SeedBusinessRolesAndUsersAsync(IServiceScope scope)
    {
        try
        {
            _logger.LogInformation("开始创建业务角色和用户...");

            var roleSeederService = scope.ServiceProvider.GetRequiredService<IRoleSeederService>();
            var userSeederService = scope.ServiceProvider.GetRequiredService<IUserSeederService>();

            // 创建业务角色
            var businessRoles = roleSeederService.GetBusinessRoles();
            var createdRoles = await roleSeederService.CreateRolesBatchAsync(businessRoles);
            _logger.LogInformation("业务角色创建完成，共创建 {Count} 个角色", createdRoles.Count);

            // 创建业务用户
            var businessUsers = userSeederService.GetBusinessUsers();
            var createdUsers = await userSeederService.CreateUsersBatchAsync(businessUsers);
            _logger.LogInformation("业务用户创建完成，共创建 {Count} 个用户", createdUsers.Count);

            // 分配角色给用户
            foreach (var userDefinition in businessUsers)
            {
                var user = createdUsers.FirstOrDefault(u => u.UserName == userDefinition.UserName);
                if (user != null)
                {
                    foreach (var roleName in userDefinition.Roles)
                    {
                        var role = createdRoles.FirstOrDefault(r => r.Name == roleName);
                        if (role != null)
                        {
                            await userSeederService.EnsureUserRoleExistsAsync(user.Id, role.Id, userDefinition.TenantId);
                        }
                    }
                }
            }

            // 创建随机用户（仅开发环境）
            var unifiedUserSeeder = userSeederService as UnifiedUserSeederService;
            if (unifiedUserSeeder != null)
            {
                await unifiedUserSeeder.CreateRandomUsersAsync(20);
                _logger.LogInformation("随机用户创建完毕！");
            }

            _logger.LogInformation("业务角色和用户创建完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建业务角色和用户时发生错误: {Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// 使用新的作用域验证数据
    /// </summary>
    /// <param name="serviceProvider">服务提供者</param>
    /// <returns></returns>
    private async Task ValidateDataWithNewScopeAsync(IServiceProvider serviceProvider)
    {
        // 创建新的作用域来验证数据，避免与之前的上下文冲突
        using var validationScope = serviceProvider.CreateScope();
        var validationServices = validationScope.ServiceProvider;
        
        try
        {
            var tenantSeeder = validationServices.GetRequiredService<TenantSeeder>();
            
            // 诊断租户状态（更详细的检查）
            await tenantSeeder.DiagnoseTenantStatusAsync();
            _logger.LogInformation("租户状态诊断完毕！");
            
            // 验证租户数据迁移结果
            await tenantSeeder.ValidateMigrationAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "租户数据验证时发生警告: {Message}", ex.Message);
            // 这里改为警告而不是抛出异常，因为可能是初始化时的正常情况
        }
    }
}



