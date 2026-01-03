using CodeSpirit.Amis.Attributes;
using CodeSpirit.Amis.Form;
using CodeSpirit.Amis.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Reflection;

namespace CodeSpirit.Amis;

/// <summary>
/// 设置页面配置构建器
/// 负责生成基于Tab的设置页面AMIS配置
/// </summary>
public class SettingsPageConfigBuilder
{
    private readonly ApiRouteHelper _apiRouteHelper;
    private readonly FormFieldHelper _formFieldHelper;
    private readonly AmisApiHelper _amisApiHelper;
    private readonly ILogger<SettingsPageConfigBuilder> _logger;

    /// <summary>
    /// 初始化设置页面配置构建器
    /// </summary>
    /// <param name="apiRouteHelper">API路由帮助类</param>
    /// <param name="formFieldHelper">表单字段帮助类</param>
    /// <param name="amisApiHelper">AMIS API帮助类</param>
    /// <param name="logger">日志记录器</param>
    public SettingsPageConfigBuilder(
        ApiRouteHelper apiRouteHelper,
        FormFieldHelper formFieldHelper,
        AmisApiHelper amisApiHelper,
        ILogger<SettingsPageConfigBuilder> logger)
    {
        _apiRouteHelper = apiRouteHelper;
        _formFieldHelper = formFieldHelper;
        _amisApiHelper = amisApiHelper;
        _logger = logger;
    }

    /// <summary>
    /// 生成设置页面配置
    /// </summary>
    /// <param name="controllerType">控制器类型</param>
    /// <param name="settingsAttr">设置页面特性</param>
    /// <returns>AMIS配置</returns>
    public JObject GenerateSettingsPageConfig(Type controllerType, SettingsPageAttribute settingsAttr)
    {
        _logger.LogInformation("开始生成设置页面配置: {ControllerType}", controllerType.Name);

        // 1. 获取所有带HeaderOperationAttribute的方法，按定义顺序排列
        var methods = controllerType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Select((m, index) => new
            {
                Method = m,
                Operation = m.GetCustomAttribute<HeaderOperationAttribute>(),
                Order = index  // 使用方法定义顺序
            })
            .Where(x => x.Operation != null)
            .OrderBy(x => x.Order)  // 按定义顺序排序
            .ToList();

        if (!methods.Any())
        {
            _logger.LogWarning("控制器 {ControllerType} 没有找到带 HeaderOperationAttribute 的方法", controllerType.Name);
            return null;
        }

        _logger.LogInformation("找到 {Count} 个设置操作方法", methods.Count);

        // 2. 为每个方法生成Tab配置
        JArray tabs = new JArray();
        foreach (var methodInfo in methods)
        {
            var tab = GenerateTabConfig(methodInfo.Method, methodInfo.Operation, controllerType);
            if (tab != null)
            {
                tabs.Add(tab);
                _logger.LogInformation("生成Tab: {TabTitle}", methodInfo.Operation.Label);
            }
        }

        // 3. 构建Page配置
        return new JObject
        {
            ["type"] = "page",
            ["title"] = settingsAttr.Title,
            ["body"] = new JObject
            {
                ["type"] = "tabs",
                ["tabsMode"] = settingsAttr.TabsMode,
                ["animated"] = settingsAttr.Animated,
                ["tabs"] = tabs
            }
        };
    }

    /// <summary>
    /// 生成单个Tab配置
    /// </summary>
    /// <param name="method">方法信息</param>
    /// <param name="operation">HeaderOperation特性</param>
    /// <param name="controllerType">控制器类型</param>
    /// <returns>Tab配置对象</returns>
    private JObject GenerateTabConfig(MethodInfo method, HeaderOperationAttribute operation, Type controllerType)
    {
        try
        {
            // 使用 HeaderOperationAttribute 的 Label 作为Tab标题，Icon 作为Tab图标
            string tabTitle = operation.Label;
            string tabIcon = operation.Icon;

            // 获取方法路由
            var route = _apiRouteHelper.GetApiRouteInfoForMethod(method);

            // 查找对应的GET方法（InitApi）
            JObject initApi = FindInitApiForMethod(method, controllerType);

            // 获取表单字段（使用FormFieldHelper）
            var parameters = method.GetParameters();
            var formFields = _formFieldHelper.GetAmisFormFieldsFromParameters(parameters);

            // 构建Tab配置
            JObject tabForm = new JObject
            {
                ["type"] = "form",
                ["api"] = _amisApiHelper.CreateApi(route),
                ["body"] = new JArray(formFields),
                //["submitText"] = "保存设置",
                ["title"] = "",
                ["mode"] = "horizontal",
                ["horizontal"] = new JObject
                {
                    ["left"] = 4,
                    ["right"] = 8
                }
            };

            // 如果找到InitApi，添加到表单配置中
            if (initApi != null)
            {
                tabForm["initApi"] = initApi;
            }

            JObject tab = new JObject
            {
                ["title"] = tabTitle,
                ["tab"] = tabForm
            };

            if (!string.IsNullOrEmpty(tabIcon))
            {
                tab["icon"] = tabIcon;
            }

            return tab;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成Tab配置失败: {MethodName}", method.Name);
            return null;
        }
    }

    /// <summary>
    /// 查找对应的InitApi（GET方法）
    /// 约定：保存方法为 SaveXxx，则查找方法名为 GetXxx
    /// </summary>
    /// <param name="saveMethod">保存方法信息</param>
    /// <param name="controllerType">控制器类型</param>
    /// <returns>InitApi配置对象，如果未找到则返回null</returns>
    private JObject FindInitApiForMethod(MethodInfo saveMethod, Type controllerType)
    {
        try
        {
            // 从保存方法名推断GET方法名
            string saveMethodName = saveMethod.Name;
            string getMethodName = saveMethodName.Replace("Save", "Get").Replace("Update", "Get").Replace("Put", "Get");

            // 查找GET方法
            var getMethod = controllerType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m =>
                    m.Name.Equals(getMethodName, StringComparison.OrdinalIgnoreCase) &&
                    m.GetCustomAttributes(typeof(HttpGetAttribute), false).Any());

            if (getMethod != null)
            {
                var route = _apiRouteHelper.GetApiRouteInfoForMethod(getMethod);
                if (route != null)
                {
                    return _amisApiHelper.CreateApi(route);
                }
            }

            _logger.LogWarning("未找到对应的GET方法: {GetMethodName}", getMethodName);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查找InitApi失败");
            return null;
        }
    }
}

