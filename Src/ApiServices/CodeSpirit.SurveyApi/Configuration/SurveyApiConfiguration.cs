using CodeSpirit.Aggregator;
using CodeSpirit.AiFormFill;
using CodeSpirit.Audit.Extensions;
using CodeSpirit.Charts.Extensions;
using CodeSpirit.SurveyApi.Services.Interfaces;
using CodeSpirit.SurveyApi.Services.Implementations;
using CodeSpirit.MultiTenant.Extensions;
using CodeSpirit.Settings.Extensions;
using CodeSpirit.Shared.Data;
using CodeSpirit.Shared.DistributedLock;
using CodeSpirit.Shared.EventBus.Extensions;
using CodeSpirit.Shared.Extensions;
using CodeSpirit.Shared.Repositories;
using CodeSpirit.Shared.Startup;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using CodeSpirit.LLM;

namespace CodeSpirit.SurveyApi.Configuration;

/// <summary>
/// 问卷系统API服务配置
/// </summary>
public class SurveyApiConfiguration : BaseApiConfiguration
{
    /// <summary>
    /// 服务名称，用于Aspire服务发现
    /// </summary>
    public override string ServiceName => "survey";
    
    /// <summary>
    /// 数据库连接字符串键名
    /// </summary>
    public override string ConnectionStringKey => "survey-api";
    
    /// <summary>
    /// 配置问卷系统特定服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // 调用基类方法以初始化路径前缀配置
        base.ConfigureServices(services, configuration);
        
        // 配置标准数据库服务（多数据库支持、仓储模式）
        this.ConfigureStandardDatabaseServices<SurveyDbContext, MySqlSurveyDbContext, SqlServerSurveyDbContext>(
            services, configuration);
        
        // 配置标准基础设施服务（事件总线、HTTP客户端）+ 可选组件（多租户、设置管理）
        this.ConfigureStandardInfrastructureServices(services, configuration, (s, c) =>
        {
            s.AddCodeSpiritMultiTenant(c);
            s.AddSettingsManagerWithDatabase(c);
        });
        
        // 添加Redis分布式锁服务
        AddRedisDistributedLock(services);
        
        // 注册Charts服务
        AddChartServices(services);
        
        // 注册LLM服务
        services.AddLLMServices();
        
        // 注册问卷系统特定服务
        AddSurveyServices(services);
        
        // 配置控制器和审计元数据过滤器
        ConfigureControllersWithAudit(services, configuration);
    }
    
    /// <summary>
    /// 配置控制器和审计元数据过滤器
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    private static void ConfigureControllersWithAudit(IServiceCollection services, IConfiguration configuration)
    {
        // 审计元数据过滤器将通过AddAuditMetadataFilter自动注册
        
        // 添加审计元数据过滤器到控制器
        services.AddControllers().AddAuditMetadataFilter();
    }
    
    /// <summary>
    /// 配置问卷系统特定中间件
    /// </summary>
    /// <param name="app">应用程序构建器</param>
    /// <returns>异步任务</returns>
    public override async Task ConfigureMiddlewareAsync(WebApplication app)
    {
        // 配置标准中间件（聚合器）+ 可选组件（多租户、AI表单填充）
        await this.ConfigureStandardMiddlewareAsync(app, a =>
        {
            a.UseCodeSpiritMultiTenant();
            a.UseAiFormFillEndpoints();
        });
        
        // 初始化设置管理
        await app.UseSettingsManagerAsync();
    }
    
    /// <summary>
    /// 问卷系统数据库初始化
    /// </summary>
    /// <param name="app">应用程序构建器</param>
    /// <returns>异步任务</returns>
    public override async Task InitializeDatabaseAsync(WebApplication app)
    {
        // 使用标准数据库初始化方法
        await this.InitializeStandardDatabaseAsync<SurveyDbContext, MySqlSurveyDbContext, SqlServerSurveyDbContext>(
            app, "SurveyApi");
    }
    
    /// <summary>
    /// 添加Redis分布式锁服务
    /// </summary>
    /// <param name="services">服务集合</param>
    private static void AddRedisDistributedLock(IServiceCollection services)
    {
        services.AddRedisDistributedLock(options =>
        {
            options.KeyPrefix = "CodeSpirit:Survey:Lock:";
            options.DefaultLockTimeout = TimeSpan.FromMinutes(5);
            options.DefaultAcquireTimeout = TimeSpan.FromSeconds(10);
            options.RetryInterval = TimeSpan.FromMilliseconds(100);
        });
    }
    
    /// <summary>
    /// 添加图表服务
    /// </summary>
    /// <param name="services">服务集合</param>
    private static void AddChartServices(IServiceCollection services)
    {
        // 注册Charts服务 - 即使Redis不可用，Chart服务也应该可以使用
        try
        {
            services.AddChartServices(options =>
            {
                options.EnableCache = true;
                options.CacheExpiration = 30;
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"警告: 注册Charts服务时出错: {ex.Message}，但应用程序将继续启动");
        }
    }
    
    /// <summary>
    /// 添加问卷系统特定服务
    /// </summary>
    /// <param name="services">服务集合</param>
    private static void AddSurveyServices(IServiceCollection services)
    {
        // 注册问卷相关服务
        services.AddScoped<ISurveyService, SurveyService>();
        services.AddScoped<ISurveyDraftService, SurveyDraftService>();
        services.AddScoped<ISurveySettingsService, SurveySettingsService>();
        services.AddScoped<IQuestionService, QuestionService>();
        services.AddScoped<ISurveyLLMGeneratorService, SurveyLLMGeneratorService>();
        services.AddScoped<ISurveyCategoryService, SurveyCategoryService>();
        services.AddScoped<IAppSurveyService, AppSurveyService>();
        services.AddScoped<IResponseService, ResponseService>();
        
        // 添加AI表单填充服务（包含自动端点功能）
        services.AddAiFormFillEndpoints();
        
        // AutoMapper已在CommonApiServices中配置，会自动扫描当前程序集中的Profile
    }
}
