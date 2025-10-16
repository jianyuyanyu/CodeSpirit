#nullable enable
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Routing;
using CodeSpirit.Shared.Configuration;

namespace CodeSpirit.Shared.Routing;

/// <summary>
/// 路径前缀控制器路由约定
/// </summary>
/// <remarks>
/// 通过IControllerModelConvention在应用启动时修改控制器路由模板，
/// 为所有控制器添加可配置的路径前缀，实现零运行时性能开销
/// </remarks>
public class PathPrefixControllerConvention : IControllerModelConvention
{
    private readonly PathPrefixOptions _options;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="options">路径前缀配置选项</param>
    public PathPrefixControllerConvention(PathPrefixOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// 应用路由约定到控制器模型
    /// </summary>
    /// <param name="controller">控制器模型</param>
    public void Apply(ControllerModel controller)
    {
        // 如果未启用路径前缀功能，直接返回
        if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.Prefix))
        {
            return;
        }

        var formattedPrefix = _options.GetFormattedPrefix();
        if (string.IsNullOrEmpty(formattedPrefix))
        {
            return;
        }

        // 检查是否为健康检查控制器，如果不应用前缀则跳过
        if (!_options.ApplyToHealthChecks && IsHealthCheckController(controller))
        {
            return;
        }

        // 为控制器的所有选择器添加路径前缀
        foreach (var selector in controller.Selectors)
        {
            ApplyPrefixToSelector(selector, formattedPrefix, controller.ControllerName);
        }

        // 为控制器的所有动作的选择器添加路径前缀
        foreach (var action in controller.Actions)
        {
            foreach (var selector in action.Selectors)
            {
                ApplyPrefixToSelector(selector, formattedPrefix, $"{controller.ControllerName}.{action.ActionName}");
            }
        }

        // 路径前缀已成功应用到控制器
    }

    /// <summary>
    /// 为选择器应用路径前缀
    /// </summary>
    /// <param name="selector">动作选择器模型</param>
    /// <param name="prefix">格式化的前缀</param>
    /// <param name="logContext">日志上下文（用于调试）</param>
    private void ApplyPrefixToSelector(SelectorModel selector, string prefix, string logContext)
    {
        if (selector.AttributeRouteModel != null)
        {
            // 处理特性路由
            var originalTemplate = selector.AttributeRouteModel.Template;
            var newTemplate = CombineRouteTemplates(prefix, originalTemplate);
            
            selector.AttributeRouteModel = new AttributeRouteModel
            {
                Template = newTemplate,
                Order = selector.AttributeRouteModel.Order,
                Name = selector.AttributeRouteModel.Name
            };

            // 特性路由已更新
        }
        else
        {
            // 处理约定路由，创建新的特性路由模型
            var template = prefix.TrimStart('/');
            selector.AttributeRouteModel = new AttributeRouteModel
            {
                Template = template
            };

            // 新的路由模板已创建
        }
    }

    /// <summary>
    /// 组合路由模板
    /// </summary>
    /// <param name="prefix">前缀</param>
    /// <param name="template">原始模板</param>
    /// <returns>组合后的路由模板</returns>
    private static string CombineRouteTemplates(string prefix, string? template)
    {
        var cleanPrefix = prefix.Trim('/');
        var cleanTemplate = template?.Trim('/') ?? string.Empty;

        if (string.IsNullOrEmpty(cleanTemplate))
        {
            return cleanPrefix;
        }

        return string.IsNullOrEmpty(cleanPrefix) ? cleanTemplate : $"{cleanPrefix}/{cleanTemplate}";
    }

    /// <summary>
    /// 检查是否为健康检查控制器
    /// </summary>
    /// <param name="controller">控制器模型</param>
    /// <returns>如果是健康检查控制器返回true</returns>
    private static bool IsHealthCheckController(ControllerModel controller)
    {
        // 检查控制器名称是否包含健康检查相关关键词
        var controllerName = controller.ControllerName.ToLowerInvariant();
        var healthCheckKeywords = new[] { "health", "healthcheck", "ping", "status" };
        
        return healthCheckKeywords.Any(keyword => controllerName.Contains(keyword));
    }
}
