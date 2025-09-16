using CodeSpirit.ApprovalApi.Configuration;
using CodeSpirit.ApprovalApi.Data;
using CodeSpirit.ApprovalApi.Events;
using CodeSpirit.Shared.EventBus.Interfaces;
using CodeSpirit.Shared.EventBus.Extensions;
using Microsoft.EntityFrameworkCore;

namespace CodeSpirit.ApprovalApi.Services;

/// <summary>
/// 审批模块服务注册扩展
/// </summary>
public static class ApprovalServiceCollectionExtensions
{
    /// <summary>
    /// 添加审批服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddApprovalServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 注册配置
        services.Configure<ApprovalOptions>(configuration.GetSection("Approval"));

        // 注册核心服务
        services.AddScoped<IApprovalInstanceService, ApprovalInstanceService>();
        services.AddScoped<IApprovalTaskService, ApprovalTaskService>();
        services.AddScoped<IWorkflowDefinitionService, WorkflowDefinitionService>();
        services.AddScoped<IWorkflowEngine, WorkflowEngine>();
        services.AddScoped<IConditionEngine, ConditionEngine>();
        services.AddScoped<IApprovalLogService, ApprovalLogService>();

        // 注册智能审批服务
        services.AddScoped<IIntelligentApprovalService, IntelligentApprovalService>();

        // 数据库上下文已在ApprovalApiConfiguration中通过DatabaseMigrationHelper.ConfigureMultiDatabaseDbContext配置
        // 避免重复注册

        // 集成缓存
        services.AddMemoryCache();
        
        var redisConnectionString = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnectionString))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
            });
        }

        // 集成AutoMapper
        services.AddAutoMapper(typeof(ApprovalServiceCollectionExtensions).Assembly);

        return services;
    }

    /// <summary>
    /// 添加审批事件处理器
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddApprovalEventHandlers(this IServiceCollection services)
    {
        // 注册租户感知事件处理器
        services.AddTenantAwareEventHandler<ApprovalStartedEvent, ApprovalStartedEventHandler>();
        services.AddTenantAwareEventHandler<ApprovalCompletedEvent, ApprovalCompletedEventHandler>();
        services.AddTenantAwareEventHandler<TaskAssignedEvent, TaskAssignedEventHandler>();
        services.AddTenantAwareEventHandler<TaskCompletedEvent, TaskCompletedEventHandler>();
        services.AddTenantAwareEventHandler<BusinessDataStatusChangedEvent, BusinessDataStatusChangedEventHandler>();

        return services;
    }
}
