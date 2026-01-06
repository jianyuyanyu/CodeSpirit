using CodeSpirit.Amis.Attributes;
using CodeSpirit.Amis.Enums;
using CodeSpirit.Amis.Helpers.Dtos;
using CodeSpirit.Amis.Tabs;
using Newtonsoft.Json.Linq;
using System.Reflection;

namespace CodeSpirit.Amis.Helpers;

/// <summary>
/// Tab帮助类，用于生成页面顶部Tab配置
/// </summary>
public class TabsHelper
{
    private readonly AmisApiHelper _amisApiHelper;
    private readonly ApiRouteHelper _apiRouteHelper;

    /// <summary>
    /// 初始化TabsHelper
    /// </summary>
    /// <param name="amisApiHelper">AMIS API帮助类</param>
    /// <param name="apiRouteHelper">API路由帮助类</param>
    public TabsHelper(AmisApiHelper amisApiHelper, ApiRouteHelper apiRouteHelper)
    {
        _amisApiHelper = amisApiHelper;
        _apiRouteHelper = apiRouteHelper;
    }

    /// <summary>
    /// 检查是否需要生成Tabs配置
    /// </summary>
    /// <param name="queryDtoType">查询DTO类型</param>
    /// <returns>是否需要生成Tabs</returns>
    public bool ShouldGenerateTabs(Type? queryDtoType)
    {
        if (queryDtoType == null) return false;

        // 检查查询DTO类上是否有PageTabsAttribute特性
        return queryDtoType.GetCustomAttribute<PageTabsAttribute>() != null;
    }

    /// <summary>
    /// 生成Tabs配置
    /// </summary>
    /// <param name="queryDtoType">查询DTO类型</param>
    /// <param name="crudConfig">CRUD配置对象</param>
    /// <param name="crudName">CRUD组件名称</param>
    /// <returns>Tabs配置的JSON对象，如果不需要生成则返回null</returns>
    public JObject? GenerateTabsConfig(Type? queryDtoType, JObject crudConfig, string crudName)
    {
        if (queryDtoType == null) return null;

        var tabsAttr = queryDtoType.GetCustomAttribute<PageTabsAttribute>();
        if (tabsAttr == null) return null;

        // 如果有ConfigType，使用强类型配置
        if (tabsAttr.ConfigType != null)
        {
            return GenerateTabsConfigFromTypedConfig(queryDtoType, tabsAttr, crudConfig, crudName);
        }

        // 获取所有PageTabItemAttribute特性
        var tabItems = queryDtoType.GetCustomAttributes<PageTabItemAttribute>()
            .OrderBy(x => x.Order)
            .ThenBy(x => x.Key)
            .ToList();

        if (!tabItems.Any()) return null;

        // 构建Tab列表
        JArray tabs = new JArray();
        foreach (var tabItem in tabItems)
        {
            var tab = BuildTabConfig(tabItem, tabsAttr, crudConfig, crudName);
            if (tab != null)
            {
                tabs.Add(tab);
            }
        }

        if (!tabs.Any()) return null;

        // 构建Tabs配置
        JObject tabsConfig = new JObject
        {
            ["type"] = "tabs",
            ["tabsMode"] = tabsAttr.TabsMode.ToAmisString(),
            ["tabs"] = tabs
        };

        // 设置默认激活的Tab - AMIS使用 activeKey
        if (!string.IsNullOrWhiteSpace(tabsAttr.DefaultTab))
        {
            tabsConfig["activeKey"] = tabsAttr.DefaultTab;
        }
        else if (tabItems.Any())
        {
            // 如果没有指定默认Tab，使用第一个Tab
            tabsConfig["activeKey"] = tabItems[0].Key;
        }

        return tabsConfig;
    }

    /// <summary>
    /// 从强类型配置生成Tabs配置
    /// </summary>
    private JObject? GenerateTabsConfigFromTypedConfig(Type queryDtoType, PageTabsAttribute tabsAttr, JObject crudConfig, string crudName)
    {
        var configType = tabsAttr.ConfigType;
        if (configType == null) return null;

        // 创建配置实例
        var configInstance = Activator.CreateInstance(configType);
        if (configInstance == null) return null;

        // 调用GetConfiguration方法
        var getConfigMethod = configType.BaseType?.GetMethod("GetConfiguration", 
            BindingFlags.Instance | BindingFlags.NonPublic);
        if (getConfigMethod == null) return null;

        var configuration = getConfigMethod.Invoke(configInstance, null);
        if (configuration == null) return null;

        // 获取配置属性
        var configurationType = configuration.GetType();
        var countApiProp = configurationType.GetProperty("CountApi");
        var tabsModeProp = configurationType.GetProperty("TabsMode");
        var defaultTabProp = configurationType.GetProperty("DefaultTab");
        var showBadgeProp = configurationType.GetProperty("ShowBadge");
        var tabItemsProp = configurationType.GetProperty("TabItems");

        if (tabItemsProp == null) return null;

        var tabItems = tabItemsProp.GetValue(configuration) as System.Collections.IEnumerable;
        if (tabItems == null) return null;

        var tabItemsList = tabItems.Cast<object>().ToList();
        if (!tabItemsList.Any()) return null;

        // 构建Tab列表
        JArray tabs = new JArray();
        foreach (var tabItem in tabItemsList)
        {
            var tab = BuildTabConfigFromTyped(tabItem, crudConfig, crudName);
            if (tab != null)
            {
                tabs.Add(tab);
            }
        }

        if (!tabs.Any()) return null;

        // 构建Tabs配置
        JObject tabsConfig = new JObject
        {
            ["type"] = "tabs",
            ["tabsMode"] = tabsModeProp?.GetValue(configuration)?.ToString() ?? "line",
            ["tabs"] = tabs
        };

        // 设置默认激活的Tab
        var defaultTab = defaultTabProp?.GetValue(configuration)?.ToString();
        if (!string.IsNullOrWhiteSpace(defaultTab))
        {
            tabsConfig["activeKey"] = defaultTab;
        }
        else if (tabItemsList.Any())
        {
            var firstTabKeyProp = tabItemsList[0].GetType().GetProperty("Key");
            var firstTabKey = firstTabKeyProp?.GetValue(tabItemsList[0])?.ToString();
            if (!string.IsNullOrWhiteSpace(firstTabKey))
            {
                tabsConfig["activeKey"] = firstTabKey;
            }
        }

        return tabsConfig;
    }

    /// <summary>
    /// 从强类型Tab配置构建Tab
    /// </summary>
    private JObject? BuildTabConfigFromTyped(object tabItem, JObject crudConfig, string crudName)
    {
        var tabItemType = tabItem.GetType();
        var keyProp = tabItemType.GetProperty("Key");
        var titleProp = tabItemType.GetProperty("Title");
        var iconProp = tabItemType.GetProperty("Icon");
        var badgeLevelProp = tabItemType.GetProperty("BadgeLevel");
        var getFilterJsonMethod = tabItemType.GetMethod("GetFilterJson");
        var getCountKeyMethod = tabItemType.GetMethod("GetCountKey");

        var key = keyProp?.GetValue(tabItem)?.ToString();
        var title = titleProp?.GetValue(tabItem)?.ToString();

        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(title))
        {
            return null;
        }

        // 创建Tab的CRUD配置副本
        JObject tabCrudConfig = new JObject(crudConfig);

        // 获取Filter JSON
        var filterJson = getFilterJsonMethod?.Invoke(tabItem, null)?.ToString();
        if (!string.IsNullOrWhiteSpace(filterJson) && filterJson != "{}")
        {
            try
            {
                JObject filterObj = JObject.Parse(filterJson);
                
                // 获取现有的api配置
                var existingApi = tabCrudConfig["api"];
                string apiUrl = existingApi?.ToString() ?? "";
                
                if (existingApi is JObject apiObj && apiObj["url"] != null)
                {
                    apiUrl = apiObj["url"]?.ToString() ?? "";
                }
                
                // 构建新的api配置
                JObject apiConfig = new JObject();
                
                if (existingApi is JObject existingApiObj)
                {
                    foreach (var prop in existingApiObj.Properties())
                    {
                        if (prop.Name != "data" && prop.Name != "query")
                        {
                            apiConfig[prop.Name] = prop.Value;
                        }
                    }
                }
                else
                {
                    apiConfig["method"] = "GET";
                    apiConfig["url"] = apiUrl;
                }
                
                apiConfig["data"] = filterObj;
                tabCrudConfig["api"] = apiConfig;
            }
            catch
            {
                // JSON解析失败，忽略
            }
        }

        // 构建Tab对象
        JObject tab = new JObject
        {
            ["hash"] = key,
            ["tab"] = tabCrudConfig
        };

        // 处理标题
        var icon = iconProp?.GetValue(tabItem)?.ToString();
        var badgeLevel = badgeLevelProp?.GetValue(tabItem)?.ToString() ?? "";
        var countKey = getCountKeyMethod?.Invoke(tabItem, null)?.ToString();

        string titleTemplate = title;
        if (!string.IsNullOrWhiteSpace(icon))
        {
            titleTemplate = $"<i class='{icon}' style='margin-right: 4px;'></i>{titleTemplate}";
        }

        if (!string.IsNullOrWhiteSpace(countKey))
        {
            var badgeColorClass = GetBadgeColorClass(badgeLevel);
            titleTemplate += $" <span class='{badgeColorClass}'>(${{{countKey}}})</span>";
        }

        tab["title"] = new JObject
        {
            ["type"] = "tpl",
            ["tpl"] = titleTemplate
        };

        return tab;
    }


    /// <summary>
    /// 构建单个Tab配置
    /// </summary>
    /// <param name="tabItem">Tab项特性</param>
    /// <param name="tabsAttr">Tabs容器特性</param>
    /// <param name="crudConfig">CRUD配置对象</param>
    /// <param name="crudName">CRUD组件名称</param>
    /// <returns>Tab配置对象</returns>
    private JObject BuildTabConfig(PageTabItemAttribute tabItem, PageTabsAttribute tabsAttr, JObject crudConfig, string crudName)
    {
        if (string.IsNullOrWhiteSpace(tabItem.Key) || string.IsNullOrWhiteSpace(tabItem.Title))
        {
            return null;
        }

        // 创建Tab的CRUD配置副本
        JObject tabCrudConfig = new JObject(crudConfig);

        if (!string.IsNullOrWhiteSpace(tabItem.Filter))
        {
            try
            {
                JObject filterObj = JObject.Parse(tabItem.Filter);
                
                // 获取现有的api配置
                var existingApi = tabCrudConfig["api"];
                string apiUrl = existingApi?.ToString() ?? "";
                
                if (existingApi is JObject apiObj && apiObj["url"] != null)
                {
                    apiUrl = apiObj["url"]?.ToString() ?? "";
                }
                
                // 构建新的api配置，将filter条件作为固定的请求参数
                JObject apiConfig = new JObject();
                
                if (existingApi is JObject existingApiObj)
                {
                    // 保留现有api配置
                    foreach (var prop in existingApiObj.Properties())
                    {
                        if (prop.Name != "data" && prop.Name != "query")
                        {
                            apiConfig[prop.Name] = prop.Value;
                        }
                    }
                }
                else
                {
                    apiConfig["method"] = "GET";
                    apiConfig["url"] = apiUrl;
                }
                
                // 将filter条件添加到data参数中（这些参数会作为query string发送）
                apiConfig["data"] = filterObj;
                
                tabCrudConfig["api"] = apiConfig;
            }
            catch
            {
                // JSON解析失败，忽略filter配置
            }
        }
        // 构建Tab对象
        JObject tab = new JObject
        {
            ["hash"] = tabItem.Key,
            ["tab"] = tabCrudConfig
        };

        // 处理标题和数字
        if (tabsAttr.ShowBadge && !string.IsNullOrWhiteSpace(tabsAttr.CountApi))
        {
            // Badge变量名：将key转换为驼峰式后加上Count，如 "on_sale" -> "onSaleCount"
            string badgeVarName = $"{ConvertKeyToCamelCase(tabItem.Key)}Count";
            
            // 构建标题模板：标题 (数字)
            string titleTemplate = tabItem.Title;
            
            // 如果有图标，添加到标题前面
            if (!string.IsNullOrWhiteSpace(tabItem.Icon))
            {
                titleTemplate = $"<i class='{tabItem.Icon}' style='margin-right: 4px;'></i>{titleTemplate}";
            }
            
            // 添加数字显示（使用括号包裹，根据 BadgeLevel 应用 AMIS 样式类）
            var badgeColorClass = GetBadgeColorClass(tabItem.BadgeLevel.ToAmisString());
            titleTemplate += $" <span class='{badgeColorClass}'>(${{{badgeVarName}}})</span>";
            
            // 使用 tpl 渲染标题
            tab["title"] = new JObject
            {
                ["type"] = "tpl",
                ["tpl"] = titleTemplate
            };
        }
        else
        {
            // 不显示数字时，使用简单的字符串title
            tab["title"] = tabItem.Title;
            
            // 添加图标
            if (!string.IsNullOrWhiteSpace(tabItem.Icon))
            {
                tab["icon"] = tabItem.Icon;
            }
        }

        return tab;
    }

    /// <summary>
    /// 获取 PageTabsAttribute 配置
    /// </summary>
    /// <param name="queryDtoType">查询DTO类型</param>
    /// <returns>PageTabsAttribute 实例，如果不存在则返回 null</returns>
    public PageTabsAttribute? GetPageTabsAttribute(Type? queryDtoType)
    {
        if (queryDtoType == null) return null;

        return queryDtoType.GetCustomAttribute<PageTabsAttribute>();
    }

    /// <summary>
    /// 获取CountApi路径（支持强类型配置）
    /// </summary>
    public string? GetCountApiPath(Type? queryDtoType)
    {
        if (queryDtoType == null) return null;

        var tabsAttr = queryDtoType.GetCustomAttribute<PageTabsAttribute>();
        if (tabsAttr == null) return null;

        // 如果有ConfigType，从配置类获取CountApi
        if (tabsAttr.ConfigType != null)
        {
            var configInstance = Activator.CreateInstance(tabsAttr.ConfigType);
            var getConfigMethod = tabsAttr.ConfigType.BaseType?.GetMethod("GetConfiguration",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var configuration = getConfigMethod?.Invoke(configInstance, null);
            if (configuration != null)
            {
                var countApiProp = configuration.GetType().GetProperty("CountApi");
                return countApiProp?.GetValue(configuration)?.ToString();
            }
        }

        // 回退到直接配置
        return tabsAttr.CountApi;
    }

    /// <summary>
    /// 将下划线分隔的key转换为驼峰式命名
    /// 例如: "on_sale" -> "onSale", "low_stock" -> "lowStock"
    /// </summary>
    /// <param name="key">原始key</param>
    /// <returns>驼峰式命名的key</returns>
    private string ConvertKeyToCamelCase(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return key;
        }

        // 如果不包含下划线，直接返回（可能已经是驼峰式）
        if (!key.Contains('_'))
        {
            return key;
        }

        // 分割并处理每个部分
        var parts = key.Split('_', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return key;
        }

        // 第一个部分保持小写，其余部分首字母大写
        var result = parts[0].ToLowerInvariant();
        for (int i = 1; i < parts.Length; i++)
        {
            if (parts[i].Length > 0)
            {
                result += char.ToUpperInvariant(parts[i][0]) + parts[i].Substring(1).ToLowerInvariant();
            }
        }

        return result;
    }

    /// <summary>
    /// 根据 BadgeLevel 获取对应的 AMIS 文本颜色样式类
    /// </summary>
    /// <param name="badgeLevel">Badge级别（小写字符串）</param>
    /// <returns>AMIS 文本颜色样式类名</returns>
    private string GetBadgeColorClass(string badgeLevel)
    {
        return badgeLevel?.ToLowerInvariant() switch
        {
            "info" => "text-info",         // 信息样式
            "success" => "text-success",   // 成功样式
            "warning" => "text-warning",   // 警告样式
            "danger" => "text-danger",     // 危险样式
            _ => "text-muted"              // 静音样式（默认）
        };
    }

    /// <summary>
    /// 创建CountApi配置（用于页面initApi）
    /// </summary>
    /// <param name="queryDtoType">查询DTO类型</param>
    /// <returns>API配置对象，如果不需要则返回null</returns>
    public JObject? CreateCountApiConfig(Type? queryDtoType)
    {
        var countApi = GetCountApiPath(queryDtoType);
        if (string.IsNullOrWhiteSpace(countApi))
        {
            return null;
        }

        // 如果CountApi是相对路径，需要转换为完整路径
        string apiPath = countApi;
        if (!apiPath.StartsWith("http://") && !apiPath.StartsWith("https://"))
        {
            // 如果以 / 开头，说明是绝对路径（相对于根路径），直接使用
            // 否则是相对路径，需要拼接基础路径
            if (!apiPath.StartsWith("/"))
            {
                string baseApi = _apiRouteHelper.GetRootApi();
                if (!string.IsNullOrEmpty(baseApi))
                {
                    apiPath = $"{baseApi}/{apiPath}";
                }
            }
        }

        return new JObject
        {
            ["url"] = apiPath,
            ["method"] = "GET"
        };
    }
}

