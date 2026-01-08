using CodeSpirit.ConfigCenter.Models;
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

        // 定义所有系统服务
        var systemApps = new List<App>
        {
            new App
            {
                Id = "config",
                Name = "配置中心",
                Secret = Guid.NewGuid().ToString("N"),
                Description = "系统配置中心服务",
                Enabled = true,
                AutoPublish = true,
                Tag = "系统",
                IsAutoRegistered = true,
                InheritancedAppId = publicApp.Id
            },
            new App
            {
                Id = "identity",
                Name = "用户中心",
                Secret = Guid.NewGuid().ToString("N"),
                Description = "系统身份认证服务",
                Enabled = true,
                AutoPublish = true,
                Tag = "系统",
                IsAutoRegistered = true,
                InheritancedAppId = publicApp.Id
            },
            new App
            {
                Id = "messaging",
                Name = "消息服务",
                Secret = Guid.NewGuid().ToString("N"),
                Description = "系统消息服务",
                Enabled = true,
                AutoPublish = true,
                Tag = "系统",
                IsAutoRegistered = true,
                InheritancedAppId = publicApp.Id
            },
            new App
            {
                Id = "exam",
                Name = "考试系统",
                Secret = Guid.NewGuid().ToString("N"),
                Description = "在线考试服务",
                Enabled = true,
                AutoPublish = true,
                Tag = "业务",
                IsAutoRegistered = true,
                InheritancedAppId = publicApp.Id
            },
            new App
            {
                Id = "file",
                Name = "文件存储",
                Secret = Guid.NewGuid().ToString("N"),
                Description = "文件存储服务",
                Enabled = true,
                AutoPublish = true,
                Tag = "系统",
                IsAutoRegistered = true,
                InheritancedAppId = publicApp.Id
            },
            new App
            {
                Id = "survey",
                Name = "问卷调查",
                Secret = Guid.NewGuid().ToString("N"),
                Description = "问卷调查服务",
                Enabled = true,
                AutoPublish = true,
                Tag = "业务",
                IsAutoRegistered = true,
                InheritancedAppId = publicApp.Id
            },
            new App
            {
                Id = "approval",
                Name = "审批流程",
                Secret = Guid.NewGuid().ToString("N"),
                Description = "审批流程服务",
                Enabled = true,
                AutoPublish = true,
                Tag = "业务",
                IsAutoRegistered = true,
                InheritancedAppId = publicApp.Id
            },
            new App
            {
                Id = "pathfinder",
                Name = "目标管理",
                Secret = Guid.NewGuid().ToString("N"),
                Description = "AI目标管理服务",
                Enabled = true,
                AutoPublish = true,
                Tag = "业务",
                IsAutoRegistered = true,
                InheritancedAppId = publicApp.Id
            },
            new App
            {
                Id = "webfrontend",
                Name = "Web前端",
                Secret = Guid.NewGuid().ToString("N"),
                Description = "Web前端应用",
                Enabled = true,
                AutoPublish = true,
                Tag = "前端",
                IsAutoRegistered = true,
                InheritancedAppId = publicApp.Id
            }
        };

        // 批量创建应用
        foreach (var app in systemApps)
        {
            if (!await _dbContext.Apps.AnyAsync(a => a.Id == app.Id))
            {
                await _dbContext.Apps.AddAsync(app);
                _logger.LogInformation("创建系统应用：{AppId} - {AppName}", app.Id, app.Name);
            }
            else
            {
                _logger.LogInformation("系统应用已存在：{AppId} - {AppName}", app.Id, app.Name);
            }
        }

        // 保存所有应用
        await _dbContext.SaveChangesAsync();
    }

    private async Task SeedSystemConfigsAsync()
    {
        // 获取公共应用
        var publicApp = await _dbContext.Apps.FirstOrDefaultAsync(a => a.Id == "public");
        // 获取Identity应用
        var identityApp = await _dbContext.Apps.FirstOrDefaultAsync(a => a.Id == "identity");
        
        if (publicApp != null)
        {
            await SeedPublicAppConfigsAsync(publicApp);
        }

        if (identityApp != null)
        {
            await SeedIdentityAppConfigsAsync(identityApp);
        }
    }

    /// <summary>
    /// 初始化公共应用配置（所有服务共享）
    /// </summary>
    private async Task SeedPublicAppConfigsAsync(App publicApp)
    {
        // ⚠️ 注意：以下配置不应放入配置中心（由 Aspire 服务发现提供）：
        // - ConnectionStrings:* - 由 Aspire 服务发现提供
        // - Services:* - 由 Aspire 服务发现提供
        // - GreptimeDB:Url 等服务端点 - 由 Aspire 服务端点提供
        // 
        // 💡 敏感配置（如 ApiKey）留空，启动后由用户在配置中心管理 UI 中编辑

        // JWT 配置（所有应用共享）- 注意：SecretKey 应通过 Aspire secrets 覆盖
        var jwtConfig = new
        {
            SecretKey = "ECBF8FA013844D77AE041A6800D7FF8F",  // 默认值，生产环境应通过 Aspire secrets 覆盖
            Issuer = "codespirit.com",
            Audience = "CodeSpirit",
            ExpirationMinutes = 60,
            RefreshTokenExpirationDays = 7
        };

        // LLM 配置 - ApiKey 留空，启动后由用户在配置中心管理 UI 中编辑
        var llmConfig = new
        {
            ApiKey = "",  // 请在配置中心管理 UI 中设置
            ApiBaseUrl = "https://dashscope.aliyuncs.com/compatible-mode/v1",
            ModelName = "qwen-flash",
            TimeoutSeconds = 120,
            MaxTokens = 2048,
            UseProxy = false,
            ProxyAddress = ""
        };

        // AI 表单填充 LLM 配置 - ApiKey 留空，启动后由用户在配置中心管理 UI 中编辑
        var aiFormFillLlmConfig = new
        {
            ApiKey = "",  // 请在配置中心管理 UI 中设置
            ApiBaseUrl = "https://dashscope.aliyuncs.com/compatible-mode/v1",
            ModelName = "qwen3-max-preview",
            DisableThinking = true,
            ResponseFormatType = "json_object",
            Temperature = 0.1,
            TopP = 0.9,
            EnableStreaming = true
        };

        // 审计配置（非服务端点部分）
        var auditConfig = new
        {
            StorageProvider = "GreptimeDB",
            GreptimeDB = new
            {
                Database = "audit_logs",
                TableName = "audit_logs"
                // 注意：Url 不在此配置，由 Aspire 服务端点提供
            },
            SensitiveData = new
            {
                SensitiveFieldPatterns = new[]
                {
                    "password",
                    "secret",
                    "token",
                    "apiKey",
                    "key",
                    "auth",
                    "credential"
                }
            }
        };

        // 公共应用配置项列表
        var publicConfigs = new List<ConfigItem>
        {
            new ConfigItem
            {
                AppId = publicApp.Id,
                Key = "Logging",
                Value = JsonConvert.SerializeObject(new
                {
                    LogLevel = new
                    {
                        Default = "Information",
                        MicrosoftAspNetCore = "Warning"
                    }
                }),
                Group = "系统配置",
                Description = "日志级别配置",
                ValueType = ConfigValueType.Json,
                Status = ConfigStatus.Released
            },
            new ConfigItem
            {
                AppId = publicApp.Id,
                Key = "Jwt",
                Value = JsonConvert.SerializeObject(jwtConfig),
                Group = "安全配置",
                Description = "JWT 认证配置（SecretKey 应通过 Aspire secrets 覆盖）",
                ValueType = ConfigValueType.Json,
                Status = ConfigStatus.Released
            },
            new ConfigItem
            {
                AppId = publicApp.Id,
                Key = "LLM",
                Value = JsonConvert.SerializeObject(llmConfig),
                Group = "AI配置",
                Description = "LLM 大语言模型配置，⚠️ 请在管理界面设置 ApiKey",
                ValueType = ConfigValueType.Json,
                Status = ConfigStatus.Released
            },
            new ConfigItem
            {
                AppId = publicApp.Id,
                Key = "AiFormFillLLM",
                Value = JsonConvert.SerializeObject(aiFormFillLlmConfig),
                Group = "AI配置",
                Description = "AI 表单填充 LLM 配置，⚠️ 请在管理界面设置 ApiKey",
                ValueType = ConfigValueType.Json,
                Status = ConfigStatus.Released
            },
            new ConfigItem
            {
                AppId = publicApp.Id,
                Key = "Audit",
                Value = JsonConvert.SerializeObject(auditConfig),
                Group = "系统配置",
                Description = "审计日志配置（GreptimeDB URL 由 Aspire 服务端点提供）",
                ValueType = ConfigValueType.Json,
                Status = ConfigStatus.Released
            }
        };

        // 添加公共配置项
        foreach (var config in publicConfigs)
        {
            if (!await _dbContext.Configs.AnyAsync(c => 
                c.AppId == config.AppId && 
                c.Key == config.Key))
            {
                await _dbContext.Configs.AddAsync(config);
                _logger.LogInformation("为应用 {AppName} 创建配置：{ConfigKey}", 
                    publicApp.Name, config.Key);
            }
        }
    }

    /// <summary>
    /// 初始化 Identity 应用特有配置
    /// </summary>
    private async Task SeedIdentityAppConfigsAsync(App identityApp)
    {
        // 用户密码配置
        var passwordConfig = new
        {
            RequireDigit = true,
            RequireLowercase = true,
            RequireNonAlphanumeric = false,
            RequireUppercase = true,
            RequiredLength = 6
        };

        // 用户锁定配置
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

        // Identity 应用特有配置
        var identityConfig = new ConfigItem
        {
            AppId = identityApp.Id,
            Key = "User",
            Value = JsonConvert.SerializeObject(userConfig),
            Group = "用户配置",
            Description = "用户配置，包含密码策略和锁定策略",
            ValueType = ConfigValueType.Json,
            Status = ConfigStatus.Released
        };

        if (!await _dbContext.Configs.AnyAsync(c => 
            c.AppId == identityConfig.AppId && 
            c.Key == identityConfig.Key))
        {
            await _dbContext.Configs.AddAsync(identityConfig);
            _logger.LogInformation("为应用 {AppName} 创建配置：{ConfigKey}", 
                identityApp.Name, identityConfig.Key);
        }
    }
} 