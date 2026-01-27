using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Hosting;
using System.Collections.Concurrent;
using System.Reflection;

namespace CodeSpirit.Audit.Middleware;

/// <summary>
/// 控制器类型注册表
/// </summary>
/// <remarks>
/// 启动时预加载所有控制器类型，提升运行时查找性能
/// </remarks>
public class ControllerTypeRegistry : IHostedService
{
    private readonly ConcurrentDictionary<string, Type> _controllerTypes = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<ControllerTypeRegistry> _logger;
    private readonly IActionDescriptorCollectionProvider? _actionDescriptorCollectionProvider;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ControllerTypeRegistry(
        ILogger<ControllerTypeRegistry> logger,
        IActionDescriptorCollectionProvider? actionDescriptorCollectionProvider = null)
    {
        _logger = logger;
        _actionDescriptorCollectionProvider = actionDescriptorCollectionProvider;
    }

    /// <summary>
    /// 启动时初始化
    /// </summary>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (_actionDescriptorCollectionProvider != null)
            {
                // 从 ActionDescriptorCollectionProvider 加载（推荐方式）
                var actionDescriptors = _actionDescriptorCollectionProvider.ActionDescriptors.Items;
                foreach (var descriptor in actionDescriptors)
                {
                    if (descriptor is Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor controllerActionDescriptor)
                    {
                        var controllerName = controllerActionDescriptor.ControllerName;
                        var controllerType = controllerActionDescriptor.ControllerTypeInfo.AsType();
                        _controllerTypes.TryAdd(controllerName, controllerType);
                    }
                }

                _logger.LogInformation("已从应用程序部件初始化控制器类型缓存，共 {Count} 个控制器", _controllerTypes.Count);
            }
            else
            {
                // 备用方式：扫描所有程序集
                var controllers = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a =>
                    {
                        try
                        {
                            return a.GetTypes();
                        }
                        catch (ReflectionTypeLoadException ex)
                        {
                            _logger.LogWarning(ex, "加载程序集 {Assembly} 的类型时发生异常", a.FullName);
                            return ex.Types.Where(t => t != null)!;
                        }
                    })
                    .Where(t => t != null && typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract);

                foreach (var controller in controllers)
                {
                    var controllerName = controller.Name.Replace("Controller", "");
                    _controllerTypes.TryAdd(controllerName, controller);
                }

                _logger.LogInformation("已扫描程序集初始化控制器类型缓存，共 {Count} 个控制器", _controllerTypes.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "初始化控制器类型缓存失败");
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// 停止服务
    /// </summary>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _controllerTypes.Clear();
        return Task.CompletedTask;
    }

    /// <summary>
    /// 查找控制器类型
    /// </summary>
    /// <param name="controllerName">控制器名称（不含Controller后缀）</param>
    /// <returns>控制器类型，如果未找到返回null</returns>
    public Type? FindControllerType(string controllerName)
    {
        if (string.IsNullOrEmpty(controllerName))
        {
            return null;
        }

        // 先尝试直接查找
        if (_controllerTypes.TryGetValue(controllerName, out var controllerType))
        {
            return controllerType;
        }

        // 尝试添加Controller后缀查找
        var controllerNameWithSuffix = controllerName.EndsWith("Controller", StringComparison.OrdinalIgnoreCase)
            ? controllerName
            : $"{controllerName}Controller";

        if (_controllerTypes.TryGetValue(controllerNameWithSuffix, out controllerType))
        {
            return controllerType;
        }

        // 尝试移除Controller后缀查找
        if (controllerName.EndsWith("Controller", StringComparison.OrdinalIgnoreCase))
        {
            var controllerNameWithoutSuffix = controllerName.Substring(0, controllerName.Length - "Controller".Length);
            if (_controllerTypes.TryGetValue(controllerNameWithoutSuffix, out controllerType))
            {
                return controllerType;
            }
        }

        return null;
    }

    /// <summary>
    /// 获取所有已注册的控制器名称
    /// </summary>
    public IEnumerable<string> GetAllControllerNames()
    {
        return _controllerTypes.Keys;
    }
}
