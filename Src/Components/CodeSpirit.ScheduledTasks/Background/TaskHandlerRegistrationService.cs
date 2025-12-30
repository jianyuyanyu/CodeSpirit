using CodeSpirit.ScheduledTasks.Configuration;
using CodeSpirit.ScheduledTasks.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace CodeSpirit.ScheduledTasks.Background;

/// <summary>
/// 任务处理器自动注册服务
/// 在应用启动时扫描并注册所有已注册的ITaskHandler实现
/// </summary>
public class TaskHandlerRegistrationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ScheduledTasksOptions _options;
    private readonly ILogger<TaskHandlerRegistrationService> _logger;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    public TaskHandlerRegistrationService(
        IServiceProvider serviceProvider,
        IOptions<ScheduledTasksOptions> options,
        ILogger<TaskHandlerRegistrationService> logger)
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
        _logger = logger;
    }
    
    /// <summary>
    /// 执行后台服务
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // 等待应用完全启动
            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            
            if (string.IsNullOrEmpty(_options.ServiceName))
            {
                _logger.LogWarning("ServiceName 未配置，跳过任务处理器注册");
                return;
            }
            
            _logger.LogInformation("开始注册任务处理器 - ServiceName: {ServiceName}", _options.ServiceName);
            
            // 扫描所有已注册的ITaskHandler实现
            var handlerTypes = ScanRegisteredHandlers();
            
            if (!handlerTypes.Any())
            {
                _logger.LogWarning("未找到任何已注册的任务处理器 - ServiceName: {ServiceName}", _options.ServiceName);
                return;
            }
            
            // 注册到注册表
            using var scope = _serviceProvider.CreateScope();
            var registry = scope.ServiceProvider.GetRequiredService<ITaskHandlerRegistry>();
            await registry.RegisterHandlersAsync(_options.ServiceName, handlerTypes, stoppingToken);
            
            // 注册配置文件中定义的任务
            await RegisterConfiguredTasksAsync(registry, stoppingToken);
            
            _logger.LogInformation("任务处理器注册完成 - ServiceName: {ServiceName}, HandlerCount: {Count}", 
                _options.ServiceName, handlerTypes.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "任务处理器注册失败");
        }
    }
    
    /// <summary>
    /// 扫描所有已注册的ITaskHandler实现
    /// </summary>
    private List<string> ScanRegisteredHandlers()
    {
        var handlerTypes = new List<string>();
        
        try
        {
            // 从服务容器中获取所有ITaskHandler的注册信息
            var serviceCollection = new ServiceCollection();
            // 注意：这里我们需要通过反射扫描已注册的服务
            
            // 方法：扫描当前程序集中所有实现ITaskHandler的类型
            var entryAssembly = Assembly.GetEntryAssembly();
            if (entryAssembly != null)
            {
                var handlerTypeInfos = entryAssembly.GetTypes()
                    .Where(t => typeof(ITaskHandler).IsAssignableFrom(t) 
                        && !t.IsInterface 
                        && !t.IsAbstract)
                    .ToList();
                
                foreach (var handlerType in handlerTypeInfos)
                {
                    var fullName = handlerType.FullName;
                    if (!string.IsNullOrEmpty(fullName))
                    {
                        handlerTypes.Add(fullName);
                        _logger.LogDebug("发现任务处理器: {HandlerType}", fullName);
                    }
                }
            }
            
            // 也扫描所有已加载的程序集
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var handlerTypeInfos = assembly.GetTypes()
                        .Where(t => typeof(ITaskHandler).IsAssignableFrom(t) 
                            && !t.IsInterface 
                            && !t.IsAbstract
                            && !handlerTypes.Contains(t.FullName ?? string.Empty))
                        .ToList();
                    
                    foreach (var handlerType in handlerTypeInfos)
                    {
                        var fullName = handlerType.FullName;
                        if (!string.IsNullOrEmpty(fullName))
                        {
                            handlerTypes.Add(fullName);
                            _logger.LogDebug("发现任务处理器: {HandlerType}", fullName);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "扫描程序集 {AssemblyName} 时发生异常", assembly.FullName);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "扫描任务处理器时发生异常");
        }
        
        return handlerTypes;
    }
    
    /// <summary>
    /// 注册配置文件中定义的任务
    /// </summary>
    private async Task RegisterConfiguredTasksAsync(ITaskHandlerRegistry registry, CancellationToken cancellationToken)
    {
        try
        {
            foreach (var task in _options.Tasks)
            {
                if (!string.IsNullOrEmpty(task.HandlerType) && !string.IsNullOrEmpty(task.Id))
                {
                    await registry.RegisterTaskServiceAsync(task.Id, _options.ServiceName, cancellationToken);
                    _logger.LogDebug("注册配置任务所属服务 - TaskId: {TaskId}, ServiceName: {ServiceName}", 
                        task.Id, _options.ServiceName);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "注册配置任务时发生异常");
        }
    }
}

