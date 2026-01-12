using CodeSpirit.Amis.Attributes;
using CodeSpirit.Amis.StatisticsCards;
using Newtonsoft.Json.Linq;
using System.Reflection;

namespace CodeSpirit.Amis.Helpers;

/// <summary>
/// 统计卡片帮助类，用于生成统计卡片配置
/// </summary>
public class StatisticsCardsHelper
{
    /// <summary>
    /// 生成统计卡片配置
    /// </summary>
    /// <param name="controllerType">控制器类型</param>
    /// <param name="baseRoute">基础路由路径</param>
    /// <returns>统计卡片 Service 组件配置，如果控制器没有配置统计卡片则返回 null</returns>
    public JObject? GenerateStatisticsCardsConfig(Type controllerType, string baseRoute)
    {
        // 查找泛型特性 StatisticsCardsAttribute<TConfig>
        var attr = controllerType.GetCustomAttributes(typeof(Attribute), false)
            .FirstOrDefault(a => a.GetType().IsGenericType && 
                                 a.GetType().GetGenericTypeDefinition() == typeof(StatisticsCardsAttribute<>));
        
        if (attr == null) return null;
        
        // 获取配置类型并实例化
        var configTypeProperty = attr.GetType().GetProperty("ConfigType");
        if (configTypeProperty == null) return null;
        
        var configType = (Type)configTypeProperty.GetValue(attr)!;
        var config = (StatisticsCardsConfigBase)Activator.CreateInstance(configType)!;
        var configuration = config.GetConfiguration();
        
        // 生成 Amis JSON
        return GenerateServiceComponent(configuration, baseRoute);
    }
    
    /// <summary>
    /// 生成 Service 组件配置
    /// </summary>
    private JObject GenerateServiceComponent(StatisticsCardsConfiguration config, string baseRoute)
    {
        // 使用 BASE_API 模板变量构建 API 路径，BASE_API 已在页面数据中定义
        // BASE_API 格式：${ROOT_API}/${baseRoute}，例如：https://localhost:5075/api/exam/ExamRecords
        var apiPath = config.Api.TrimStart('/');
        var apiUrl = $"${{BASE_API}}/{apiPath}";
        
        var serviceConfig = new JObject
        {
            ["type"] = "service",
            ["api"] = apiUrl,
            ["className"] = "mb-4"
        };
        
        // 如果设置了刷新间隔，添加 interval 属性
        if (config.RefreshInterval > 0)
        {
            serviceConfig["interval"] = config.RefreshInterval * 1000;
        }
        
        serviceConfig["body"] = GenerateCardsGrid(config);
        
        return serviceConfig;
    }
    
    /// <summary>
    /// 生成卡片网格布局
    /// </summary>
    private JObject GenerateCardsGrid(StatisticsCardsConfiguration config)
    {
        var columnWidth = 12 / config.ColumnsCount; // Bootstrap grid (12列系统)
        var columns = config.Cards.Select(card => new JObject
        {
            ["md"] = columnWidth,
            ["body"] = GenerateCardComponent(card)
        }).ToArray();
        
        return new JObject
        {
            ["type"] = "grid",
            ["gap"] = config.Gap,
            ["columns"] = new JArray(columns)
        };
    }
    
    /// <summary>
    /// 生成单个卡片组件
    /// </summary>
    private JObject GenerateCardComponent(CardDefinition card)
    {
        return new JObject
        {
            ["type"] = "card",
            ["className"] = "statistics-card bg-white shadow-sm hover:shadow-md transition-shadow",
            ["header"] = new JObject
            {
                ["title"] = card.Title,
                ["className"] = "text-base font-semibold"
            },
            ["body"] = new JObject
            {
                ["type"] = "flex",
                ["justify"] = "space-between",
                ["alignItems"] = "center",
                ["items"] = new JArray
                {
                    new JObject
                    {
                        ["type"] = "icon",
                        ["icon"] = card.Icon,
                        ["className"] = $"text-{card.Color} text-3xl"
                    },
                    new JObject
                    {
                        ["type"] = "tpl",
                        ["tpl"] = $"<span class='text-{card.Color} text-2xl font-bold'>${{{card.Field}}}</span>"
                    }
                }
            }
        };
    }
}
