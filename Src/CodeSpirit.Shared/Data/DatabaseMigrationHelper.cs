using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pomelo.EntityFrameworkCore.MySql;

namespace CodeSpirit.Shared.Data;

/// <summary>
/// 数据库迁移帮助类
/// 提供通用的数据库迁移应用方法
/// </summary>
public static class DatabaseMigrationHelper
{
    /// <summary>
    /// 应用数据库迁移
    /// </summary>
    /// <param name="services">服务提供者</param>
    /// <param name="configuration">配置</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="apiName">API名称，用于日志记录</param>
    /// <typeparam name="TMySqlContext">MySQL DbContext类型</typeparam>
    /// <typeparam name="TSqlServerContext">SQL Server DbContext类型</typeparam>
    public static async Task ApplyDatabaseMigrationsAsync<TMySqlContext, TSqlServerContext>(
        IServiceProvider services,
        IConfiguration configuration,
        ILogger logger,
        string apiName)
        where TMySqlContext : DbContext
        where TSqlServerContext : DbContext
    {
        var databaseType = configuration.GetValue<string>("DatabaseType") ?? 
                          configuration.GetValue<string>("Database:Provider") ?? "MySql";
        logger.LogInformation("开始应用 {ApiName} {DatabaseType} 数据库迁移...", apiName, databaseType);

        try
        {
            DbContext contextForMigration;

            if (!typeof(TMySqlContext).Name.Contains("MySql"))
                throw new InvalidOperationException($"{nameof(TMySqlContext)} 的命名必须包含 MySql 字符串！");
            if (!typeof(TSqlServerContext).Name.Contains("SqlServer"))
                throw new InvalidOperationException($"{nameof(TSqlServerContext)} 的命名必须包含 SqlServer 字符串！");

            if (databaseType.Equals("MySql", StringComparison.OrdinalIgnoreCase))
            {
                contextForMigration = services.GetRequiredService<TMySqlContext>();
            }
            else if (databaseType.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                contextForMigration = services.GetRequiredService<TSqlServerContext>();
            }
            else
            {
                throw new InvalidOperationException($"不支持的数据库类型: {databaseType}");
            }

            // 检查数据库是否存在
            var canConnect = await contextForMigration.Database.CanConnectAsync();
            if (!canConnect)
            {
                logger.LogWarning("{ApiName} 无法连接到数据库，尝试创建数据库...", apiName);
            }

            // 检查是否有待应用的迁移
            var pendingMigrations = await contextForMigration.Database.GetPendingMigrationsAsync();
            var pendingMigrationsList = pendingMigrations.ToList();

            if (pendingMigrationsList.Any())
            {
                logger.LogInformation("{ApiName} 发现 {Count} 个待应用的迁移: {Migrations}",
                    apiName, pendingMigrationsList.Count, string.Join(", ", pendingMigrationsList));

                // 应用迁移
                await contextForMigration.Database.MigrateAsync();
                logger.LogInformation("{ApiName} {DatabaseType} 数据库迁移应用完成", apiName, databaseType);
            }
            else
            {
                logger.LogInformation("{ApiName} {DatabaseType} 数据库已是最新状态，无需迁移", apiName, databaseType);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{ApiName} 数据库迁移应用失败: {Message}", apiName, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// 配置多数据库DbContext服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置</param>
    /// <param name="connectionStringName">连接字符串名称，默认为"DefaultConnection"</param>
    /// <typeparam name="TApplicationContext">应用程序DbContext类型</typeparam>
    /// <typeparam name="TMySqlContext">MySQL DbContext类型</typeparam>
    /// <typeparam name="TSqlServerContext">SQL Server DbContext类型</typeparam>
    public static void ConfigureMultiDatabaseDbContext<TApplicationContext, TMySqlContext, TSqlServerContext>(
        IServiceCollection services,
        IConfiguration configuration,
        string connectionStringName = "DefaultConnection",
        bool isRegisterDbContext = true)
        where TApplicationContext : DbContext
        where TMySqlContext : DbContext
        where TSqlServerContext : DbContext
    {
        var connectionString = configuration.GetConnectionString(connectionStringName);
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException($"未找到连接字符串: {connectionStringName}");
        }

        var databaseType = configuration.GetValue<string>("DatabaseType") ?? 
                          configuration.GetValue<string>("Database:Provider") ?? "MySql";
        Console.WriteLine($"配置多数据库DbContext，数据库类型: {databaseType}");

        if (!typeof(TMySqlContext).Name.Contains("MySql"))
            throw new InvalidOperationException($"{nameof(TMySqlContext)} 的命名必须包含 MySql 字符串！");
        if (!typeof(TSqlServerContext).Name.Contains("SqlServer"))
            throw new InvalidOperationException($"{nameof(TSqlServerContext)} 的命名必须包含 SqlServer 字符串！");

        if (databaseType.Equals("MySql", StringComparison.OrdinalIgnoreCase))
        {
            // 只注册MySQL特定的DbContext
            services.AddDbContext<TMySqlContext>(options =>
            {
                options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
                // 为迁移专用上下文禁用自动迁移检测
                options.EnableServiceProviderCaching(false);
            });

            Console.WriteLine("已配置MySql数据库");
        }
        else if (databaseType.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
        {
            // 只注册SQL Server特定的DbContext
            services.AddDbContext<TSqlServerContext>(options =>
            {
                options.UseSqlServer(connectionString);
                // 为迁移专用上下文禁用自动迁移检测
                options.EnableServiceProviderCaching(false);
            });

            Console.WriteLine("已配置SqlServer数据库");
        }
        else
        {
            throw new InvalidOperationException($"不支持的数据库类型: {databaseType}");
        }

        // 注册DbContext基类解析，根据数据库类型选择正确的实现
        services.AddScoped<TApplicationContext>(provider =>
        {
            var config = provider.GetRequiredService<IConfiguration>();
            var databaseType = config.GetValue<string>("DatabaseType") ?? 
                              config.GetValue<string>("Database:Provider") ?? "MySql";
            
            if (databaseType.Equals("MySql", StringComparison.OrdinalIgnoreCase))
            {
                var mysqlContext = provider.GetRequiredService<TMySqlContext>();
                return (TApplicationContext)(object)mysqlContext;
            }
            else if (databaseType.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                var sqlServerContext = provider.GetRequiredService<TSqlServerContext>();
                return (TApplicationContext)(object)sqlServerContext;
            }
            else
            {
                throw new InvalidOperationException($"不支持的数据库类型: {databaseType}");
            }
        });

        if (isRegisterDbContext)
        {
            services.AddScoped<DbContext>(provider =>
                provider.GetRequiredService<TApplicationContext>());
        }
    }

    /// <summary>
    /// 安全应用数据库迁移，处理可能的冲突
    /// </summary>
    /// <param name="services">服务提供者</param>
    /// <param name="configuration">配置</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="apiName">API名称，用于日志记录</param>
    /// <typeparam name="TMySqlContext">MySQL DbContext类型</typeparam>
    /// <typeparam name="TSqlServerContext">SQL Server DbContext类型</typeparam>
    public static async Task SafeApplyDatabaseMigrationsAsync<TMySqlContext, TSqlServerContext>(
        IServiceProvider services,
        IConfiguration configuration,
        ILogger logger,
        string apiName)
        where TMySqlContext : DbContext
        where TSqlServerContext : DbContext
    {
        var databaseType = configuration.GetValue<string>("DatabaseType") ?? 
                          configuration.GetValue<string>("Database:Provider") ?? "MySql";
        logger.LogInformation("开始安全应用 {ApiName} {DatabaseType} 数据库迁移...", apiName, databaseType);

        try
        {
            DbContext contextForMigration;

            if (databaseType.Equals("MySql", StringComparison.OrdinalIgnoreCase))
            {
                contextForMigration = services.GetRequiredService<TMySqlContext>();
            }
            else if (databaseType.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                contextForMigration = services.GetRequiredService<TSqlServerContext>();
            }
            else
            {
                throw new InvalidOperationException($"不支持的数据库类型: {databaseType}");
            }

            // 检查数据库是否存在
            var canConnect = await contextForMigration.Database.CanConnectAsync();
            if (!canConnect)
            {
                logger.LogWarning("{ApiName} 无法连接到数据库，尝试创建数据库...", apiName);
                await contextForMigration.Database.EnsureCreatedAsync();
                return;
            }

            // 检查是否有待应用的迁移
            var pendingMigrations = await contextForMigration.Database.GetPendingMigrationsAsync();
            var pendingMigrationsList = pendingMigrations.ToList();

            if (!pendingMigrationsList.Any())
            {
                logger.LogInformation("{ApiName} {DatabaseType} 数据库已是最新状态，无需迁移", apiName, databaseType);
                return;
            }

            logger.LogInformation("{ApiName} 发现 {Count} 个待应用的迁移: {Migrations}",
                apiName, pendingMigrationsList.Count, string.Join(", ", pendingMigrationsList));

            // 逐个应用迁移以便更好地处理错误
            foreach (var migration in pendingMigrationsList)
            {
                try
                {
                    logger.LogInformation("{ApiName} 正在应用迁移: {Migration}", apiName, migration);
                    await contextForMigration.Database.MigrateAsync();
                    break; // MigrateAsync 会应用所有待应用的迁移，所以只需要调用一次
                }
                catch (Exception migrationEx)
                {
                    // 检查是否是表已存在的错误
                    if (migrationEx.Message.Contains("already an object named") ||
                        migrationEx.Message.Contains("Table") && migrationEx.Message.Contains("already exists"))
                    {
                        logger.LogWarning("{ApiName} 迁移失败，但可能是因为表已存在。正在尝试标记迁移为已应用: {Migration}",
                            apiName, migration);

                        // 这里可以添加逻辑来手动标记迁移为已应用
                        // 但这需要直接操作 __EFMigrationsHistory 表，比较危险
                        // 暂时重新抛出异常，让开发者手动处理
                        throw;
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            logger.LogInformation("{ApiName} {DatabaseType} 数据库迁移应用完成", apiName, databaseType);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{ApiName} 数据库迁移应用失败: {Message}", apiName, ex.Message);
            throw;
        }
    }
}
