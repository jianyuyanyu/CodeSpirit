using CodeSpirit.ConfigCenter.Models;
using CodeSpirit.ConfigCenter.Models.Enums;
using CodeSpirit.Core.IdGenerator;
using Microsoft.EntityFrameworkCore;

namespace CodeSpirit.ConfigCenter.Data.Seeders;

public class ConfigSeederService : IScopedDependency
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ConfigSeederService> _logger;
    private readonly IIdGenerator _idGenerator;
    private readonly ConfigDbContext _dbContext;

    public ConfigSeederService(
        IServiceProvider serviceProvider,
        ILogger<ConfigSeederService> logger,
        IIdGenerator idGenerator,
        ConfigDbContext dbContext)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _idGenerator = idGenerator;
        _dbContext = dbContext;
    }

    public async Task SeedAsync()
    {
        try
        {
            // 临时设置一个系统用户ID用于审计字段
            _dbContext.UserId = -1;  // 使用-1作为系统用户ID

            // 应用迁移
            await _dbContext.Database.MigrateAsync();

            // 初始化示例应用
            await SeedSampleAppsAsync();

            // 初始化示例配置
            await SeedSampleConfigsAsync();

            // 保存更改
            await _dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "数据初始化失败：{Message}", ex.Message);
            throw;
        }
    }

    private async Task SeedSampleAppsAsync()
    {
        var sampleApps = new List<App>
        {
            new App
            {
                Id = _idGenerator.NewId().ToString(),
                Name = "示例应用1",
                Secret = Guid.NewGuid().ToString("N"),
                Description = "用于演示的示例应用1",
                Enabled = true,
                AutoPublish = true,
                Tag = "示例",
                IsAutoRegistered = false
            },
            new App
            {
                Id = _idGenerator.NewId().ToString(),
                Name = "示例应用2",
                Secret = Guid.NewGuid().ToString("N"),
                Description = "用于演示的示例应用2",
                Enabled = true,
                AutoPublish = false,
                Tag = "示例",
                IsAutoRegistered = false
            }
        };

        foreach (var app in sampleApps)
        {
            if (!await _dbContext.Apps.AnyAsync(a => a.Name == app.Name))
            {
                await _dbContext.Apps.AddAsync(app);
                _logger.LogInformation("创建示例应用：{AppName}", app.Name);
            }
        }
    }

    private async Task SeedSampleConfigsAsync()
    {
        var apps = await _dbContext.Apps.ToListAsync();
        if (!apps.Any()) return;

        foreach (var app in apps)
        {
            var sampleConfigs = new List<ConfigItem>
            {
                new ConfigItem
                {
                    AppId = app.Id,
                    Key = "SampleString",
                    Value = "示例字符串值",
                    Environment = EnvironmentType.Development,
                    Group = "基础配置",
                    Description = "字符串类型配置示例",
                    ValueType = ConfigValueType.String,
                    IsEnabled = true,
                    Status = ConfigStatus.Released
                },
                new ConfigItem
                {
                    AppId = app.Id,
                    Key = "SampleJson",
                    Value = "{\"name\": \"示例JSON\", \"value\": 123}",
                    Environment = EnvironmentType.Development,
                    Group = "基础配置",
                    Description = "JSON类型配置示例",
                    ValueType = ConfigValueType.Json,
                    IsEnabled = true,
                    Status = ConfigStatus.Released
                }
            };

            foreach (var config in sampleConfigs)
            {
                if (!await _dbContext.Configs.AnyAsync(c => 
                    c.AppId == config.AppId && 
                    c.Key == config.Key && 
                    c.Environment == config.Environment))
                {
                    await _dbContext.Configs.AddAsync(config);
                    _logger.LogInformation("为应用 {AppName} 创建示例配置：{ConfigKey}", 
                        app.Name, config.Key);
                }
            }
        }
    }
} 