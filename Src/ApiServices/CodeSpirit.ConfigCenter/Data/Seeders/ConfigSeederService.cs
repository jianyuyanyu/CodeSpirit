using CodeSpirit.ConfigCenter.Models;
using CodeSpirit.ConfigCenter.Models.Enums;
using CodeSpirit.Core.IdGenerator;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

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

            // 初始化系统应用
            await SeedSystemAppsAsync();

            // 初始化系统配置
            await SeedSystemConfigsAsync();

            // 保存更改
            await _dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "数据初始化失败：{Message}", ex.Message);
            throw;
        }
    }

    private async Task SeedSystemAppsAsync()
    {
        // 创建公共应用（public）
        var publicApp = new App
        {
            Id = "public",
            Name = "公共应用",
            Secret = Guid.NewGuid().ToString("N"),
            Description = "系统基础公共应用，可被其他应用继承",
            Enabled = true,
            AutoPublish = true,
            Tag = "系统",
            IsAutoRegistered = true
        };

        if (!await _dbContext.Apps.AnyAsync(a => a.Id == publicApp.Id))
        {
            await _dbContext.Apps.AddAsync(publicApp);
            _logger.LogInformation("创建系统应用：{AppName}", publicApp.Name);
        }
        else
        {
            publicApp = await _dbContext.Apps.FirstAsync(a => a.Id == publicApp.Id);
        }

        // 创建身份认证应用（Identity），继承自public
        var identityApp = new App
        {
            Id = "identity",
            Name = "用户中心",
            Secret = Guid.NewGuid().ToString("N"),
            Description = "系统身份认证服务应用",
            Enabled = true,
            AutoPublish = true,
            Tag = "系统",
            IsAutoRegistered = true,
            InheritancedAppId = publicApp.Id  // 继承自公共应用
        };

        if (!await _dbContext.Apps.AnyAsync(a => a.Id == identityApp.Id))
        {
            await _dbContext.Apps.AddAsync(identityApp);
            _logger.LogInformation("创建系统应用：{AppName}", identityApp.Name);
            await _dbContext.SaveChangesAsync();
        }
    }

    private async Task SeedSystemConfigsAsync()
    {
        // 获取公共应用
        var publicApp = await _dbContext.Apps.FirstOrDefaultAsync(a => a.Id == "public");
        // 获取Identity应用
        var identityApp = await _dbContext.Apps.FirstOrDefaultAsync(a => a.Id == "identity");
        
        if (publicApp != null)
        {
            // 为公共应用创建日志和连接字符串配置
            var systemConfig = new
            {
                Logging = new
                {
                    LogLevel = new
                    {
                        Default = "Information",
                        MicrosoftAspNetCore = "Warning"
                    }
                },
                ConnectionStrings = new
                {
                    cache = "redis:6379,defaultDatabase=0,abortConnect=false,connectTimeout=5000,syncTimeout=5000"
                }
            };
            
            // 为所有环境创建配置
            var environments = Enum.GetValues(typeof(EnvironmentType)).Cast<EnvironmentType>();
            
            foreach (var env in environments)
            {
                var publicConfigItem = new ConfigItem
                {
                    AppId = publicApp.Id,
                    Key = "System",
                    Value = JsonConvert.SerializeObject(systemConfig),
                    Environment = env,
                    Group = "系统配置",
                    Description = "系统基础配置，包含日志级别和连接字符串",
                    ValueType = ConfigValueType.Json,
                    Status = ConfigStatus.Released
                };
                
                if (!await _dbContext.Configs.AnyAsync(c => 
                    c.AppId == publicConfigItem.AppId && 
                    c.Key == publicConfigItem.Key && 
                    c.Environment == publicConfigItem.Environment))
                {
                    await _dbContext.Configs.AddAsync(publicConfigItem);
                    _logger.LogInformation("为应用 {AppName} 创建系统配置：{ConfigKey}，环境：{Environment}", 
                        publicApp.Name, publicConfigItem.Key, env);
                }
            }
        }

        if (identityApp == null) return;

        // 为Identity应用创建JWT配置（使用JSON格式）
        var jwtConfig = new
        {
            SecretKey = "ECBF8FA013844D77AE041A6800D7FF8F",
            Issuer = "codespirit.com",
            Audience = "CodeSpirit",
            ExpirationMinutes = 60,
            RefreshTokenExpirationDays = 7
        };

        // 用户密码配置（使用JSON格式）
        var passwordConfig = new
        {
            RequireDigit = true,
            RequireLowercase = true,
            RequireNonAlphanumeric = false,
            RequireUppercase = true,
            RequiredLength = 6
        };

        // 用户锁定配置（使用JSON格式）
        var lockoutConfig = new
        {
            DefaultLockoutMinutes = 5,
            MaxFailedAttempts = 5
        };

        // 组合成用户配置
        var userConfig = new
        {
            Password = passwordConfig,
            Lockout = lockoutConfig
        };

        // 创建配置项列表
        var configs = new List<ConfigItem>
        {
            new ConfigItem
            {
                AppId = identityApp.Id,
                Key = "Jwt",
                Value = JsonConvert.SerializeObject(jwtConfig),
                Environment = EnvironmentType.Development,
                Group = "JWT配置",
                Description = "JWT配置，包含密钥、签发者、接收者、过期时间等",
                ValueType = ConfigValueType.Json,
                Status = ConfigStatus.Released
            },
            new ConfigItem
            {
                AppId = identityApp.Id,
                Key = "User",
                Value = JsonConvert.SerializeObject(userConfig),
                Environment = EnvironmentType.Development,
                Group = "用户配置",
                Description = "用户配置，包含密码策略和锁定策略",
                ValueType = ConfigValueType.Json,
                Status = ConfigStatus.Released
            }
        };

        // 添加配置项
        foreach (var config in configs)
        {
            if (!await _dbContext.Configs.AnyAsync(c => 
                c.AppId == config.AppId && 
                c.Key == config.Key && 
                c.Environment == config.Environment))
            {
                await _dbContext.Configs.AddAsync(config);
                _logger.LogInformation("为应用 {AppName} 创建系统配置：{ConfigKey}", 
                    identityApp.Name, config.Key);
            }
        }
    }
} 