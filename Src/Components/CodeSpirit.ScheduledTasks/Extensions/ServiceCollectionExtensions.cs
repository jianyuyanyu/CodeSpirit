using CodeSpirit.ScheduledTasks.Background;
using CodeSpirit.ScheduledTasks.Configuration;
using CodeSpirit.ScheduledTasks.Services;
using Microsoft.Extensions.Configuration;

namespace CodeSpirit.ScheduledTasks.Extensions;

/// <summary>
/// 服务集合扩展方法
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 添加CodeSpirit定时任务服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置</param>
    /// <param name="serviceName">服务名称</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddCodeSpiritScheduledTasks(
        this IServiceCollection services, 
        IConfiguration configuration, 
        string serviceName)
    {
        return services.AddCodeSpiritScheduledTasks(configuration.GetSection(ScheduledTasksOptions.SectionName), serviceName);
    }

    /// <summary>
    /// 添加CodeSpirit定时任务服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configurationSection">配置节</param>
    /// <param name="serviceName">服务名称</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddCodeSpiritScheduledTasks(
        this IServiceCollection services, 
        IConfigurationSection configurationSection, 
        string serviceName)
    {
        // 配置选项
        services.Configure<ScheduledTasksOptions>(configurationSection);
        
        // 验证配置
        services.AddSingleton<IValidateOptions<ScheduledTasksOptions>, ScheduledTasksOptionsValidator>();

        // 注册核心服务
        services.AddScoped<IScheduledTaskService, ScheduledTaskService>();
        services.AddScoped<IScheduledTaskQueryService>(provider => 
            provider.GetRequiredService<IScheduledTaskService>() as IScheduledTaskQueryService 
            ?? throw new InvalidOperationException("ScheduledTaskService must implement IScheduledTaskQueryService"));
        services.AddScoped<ITaskExecutor, TaskExecutor>();
        
        // 注册任务处理器注册表
        services.AddSingleton<ITaskHandlerRegistry, TaskHandlerRegistry>();

        // 注册后台服务
        services.AddHostedService<TaskHandlerRegistrationService>();
        services.AddHostedService<ScheduledTaskBackgroundService>();

        return services;
    }

    /// <summary>
    /// 添加CodeSpirit定时任务服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configureOptions">配置选项委托</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddCodeSpiritScheduledTasks(
        this IServiceCollection services, 
        Action<ScheduledTasksOptions>? configureOptions = null)
    {
        // 配置选项
        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }
        else
        {
            services.Configure<ScheduledTasksOptions>(options => { }); // 使用默认配置
        }

        // 验证配置
        services.AddSingleton<IValidateOptions<ScheduledTasksOptions>, ScheduledTasksOptionsValidator>();

        // 注册核心服务
        services.AddScoped<IScheduledTaskService, ScheduledTaskService>();
        services.AddScoped<IScheduledTaskQueryService>(provider => 
            provider.GetRequiredService<IScheduledTaskService>() as IScheduledTaskQueryService 
            ?? throw new InvalidOperationException("ScheduledTaskService must implement IScheduledTaskQueryService"));
        services.AddScoped<ITaskExecutor, TaskExecutor>();
        
        // 注册任务处理器注册表
        services.AddSingleton<ITaskHandlerRegistry, TaskHandlerRegistry>();

        // 注册后台服务
        services.AddHostedService<TaskHandlerRegistrationService>();
        services.AddHostedService<ScheduledTaskBackgroundService>();

        return services;
    }

    /// <summary>
    /// 添加任务处理器
    /// </summary>
    /// <typeparam name="THandler">处理器类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <param name="lifetime">服务生命周期</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddTaskHandler<THandler>(
        this IServiceCollection services, 
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where THandler : class, ITaskHandler
    {
        services.Add(new ServiceDescriptor(typeof(THandler), typeof(THandler), lifetime));
        services.Add(new ServiceDescriptor(typeof(ITaskHandler), provider => provider.GetRequiredService<THandler>(), lifetime));
        
        return services;
    }

    /// <summary>
    /// 添加任务处理器
    /// </summary>
    /// <typeparam name="THandler">处理器类型</typeparam>
    /// <typeparam name="TImplementation">实现类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <param name="lifetime">服务生命周期</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddTaskHandler<THandler, TImplementation>(
        this IServiceCollection services, 
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where THandler : class, ITaskHandler
        where TImplementation : class, THandler
    {
        services.Add(new ServiceDescriptor(typeof(THandler), typeof(TImplementation), lifetime));
        services.Add(new ServiceDescriptor(typeof(ITaskHandler), provider => provider.GetRequiredService<THandler>(), lifetime));
        
        return services;
    }

    /// <summary>
    /// 添加任务处理器工厂
    /// </summary>
    /// <typeparam name="THandler">处理器类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <param name="factory">处理器工厂</param>
    /// <param name="lifetime">服务生命周期</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddTaskHandler<THandler>(
        this IServiceCollection services, 
        Func<IServiceProvider, THandler> factory,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where THandler : class, ITaskHandler
    {
        services.Add(new ServiceDescriptor(typeof(THandler), factory, lifetime));
        services.Add(new ServiceDescriptor(typeof(ITaskHandler), provider => provider.GetRequiredService<THandler>(), lifetime));
        
        return services;
    }
}

/// <summary>
/// 定时任务配置选项验证器
/// </summary>
internal class ScheduledTasksOptionsValidator : IValidateOptions<ScheduledTasksOptions>
{
    /// <summary>
    /// 验证选项
    /// </summary>
    /// <param name="name">选项名称</param>
    /// <param name="options">选项实例</param>
    /// <returns>验证结果</returns>
    public ValidateOptionsResult Validate(string? name, ScheduledTasksOptions options)
    {
        if (!options.Validate())
        {
            return ValidateOptionsResult.Fail("定时任务配置验证失败：请检查超时时间、并发数等配置项");
        }

        // 验证预定义任务
        var errors = new List<string>();
        foreach (var task in options.Tasks)
        {
            if (string.IsNullOrWhiteSpace(task.Id))
            {
                errors.Add($"任务ID不能为空");
                continue;
            }

            if (string.IsNullOrWhiteSpace(task.Name))
            {
                errors.Add($"任务 {task.Id} 的名称不能为空");
            }

            if (string.IsNullOrWhiteSpace(task.HandlerType))
            {
                errors.Add($"任务 {task.Id} 的处理器类型不能为空");
            }

            // 验证任务类型特定配置
            switch (task.Type)
            {
                case Models.TaskType.Cron:
                    if (string.IsNullOrWhiteSpace(task.CronExpression))
                    {
                        errors.Add($"Cron任务 {task.Id} 必须指定Cron表达式");
                    }
                    else if (!Helpers.CronHelper.IsValidCronExpression(task.CronExpression))
                    {
                        errors.Add($"任务 {task.Id} 的Cron表达式无效: {task.CronExpression}");
                    }
                    break;

                case Models.TaskType.Delay:
                    if (!task.DelayTime.HasValue || task.DelayTime.Value <= TimeSpan.Zero)
                    {
                        errors.Add($"延迟任务 {task.Id} 必须指定有效的延迟时间");
                    }
                    break;

                case Models.TaskType.OneTime:
                    if (!task.ExecuteAt.HasValue)
                    {
                        errors.Add($"一次性任务 {task.Id} 必须指定执行时间");
                    }
                    break;
            }
        }

        // 检查任务ID重复
        var duplicateIds = options.Tasks
            .GroupBy(t => t.Id)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateIds.Any())
        {
            errors.Add($"发现重复的任务ID: {string.Join(", ", duplicateIds)}");
        }

        if (errors.Any())
        {
            return ValidateOptionsResult.Fail(string.Join("; ", errors));
        }

        return ValidateOptionsResult.Success;
    }
}
