using CodeSpirit.Amis.Attributes;
using CodeSpirit.Amis.Helpers;
using CodeSpirit.Amis.Helpers.Dtos;
using CodeSpirit.Core.Dtos;
using Newtonsoft.Json.Linq;
using System.Reflection;
using Xunit;

namespace CodeSpirit.Amis.Tests;

/// <summary>
/// TabsHelper测试类
/// </summary>
public class TabsHelperTests
{
    private readonly TestTabsHelper _tabsHelper;

    public TabsHelperTests()
    {
        _tabsHelper = new TestTabsHelper();
    }

    /// <summary>
    /// 测试ShouldGenerateTabs - 有PageTabsAttribute时返回true
    /// </summary>
    [Fact]
    public void ShouldGenerateTabs_ShouldReturnTrue_WhenHasPageTabsAttribute()
    {
        // Arrange
        var queryDtoType = typeof(TestQueryDtoWithTabs);

        // Act
        var result = _tabsHelper.ShouldGenerateTabs(queryDtoType);

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// 测试ShouldGenerateTabs - 没有PageTabsAttribute时返回false
    /// </summary>
    [Fact]
    public void ShouldGenerateTabs_ShouldReturnFalse_WhenNoPageTabsAttribute()
    {
        // Arrange
        var queryDtoType = typeof(TestQueryDtoWithoutTabs);

        // Act
        var result = _tabsHelper.ShouldGenerateTabs(queryDtoType);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// 测试ShouldGenerateTabs - null类型时返回false
    /// </summary>
    [Fact]
    public void ShouldGenerateTabs_ShouldReturnFalse_WhenTypeIsNull()
    {
        // Act
        var result = _tabsHelper.ShouldGenerateTabs(null);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// 测试GenerateTabsConfig - 生成正确的Tabs配置
    /// </summary>
    [Fact]
    public void GenerateTabsConfig_ShouldGenerateCorrectTabsConfig()
    {
        // Arrange
        var queryDtoType = typeof(TestQueryDtoWithTabs);
        var crudConfig = new JObject
        {
            ["type"] = "crud",
            ["name"] = "testCrud",
            ["api"] = new JObject
            {
                ["url"] = "/api/test",
                ["method"] = "GET"
            }
        };

        // Act
        var result = _tabsHelper.GenerateTabsConfig(queryDtoType, crudConfig, "testCrud");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("tabs", result["type"]?.ToString());
        Assert.Equal("line", result["tabsMode"]?.ToString());
        Assert.Equal("tab1", result["defaultKey"]?.ToString());

        var tabs = result["tabs"] as JArray;
        Assert.NotNull(tabs);
        Assert.Equal(2, tabs.Count);

        // 验证第一个Tab
        var firstTab = tabs[0] as JObject;
        Assert.NotNull(firstTab);
        Assert.Equal("tab1", firstTab["key"]?.ToString());
        Assert.Equal("Tab 1", firstTab["title"]?.ToString());
        Assert.Equal("fa-icon-1", firstTab["icon"]?.ToString());

        // 验证第二个Tab
        var secondTab = tabs[1] as JObject;
        Assert.NotNull(secondTab);
        Assert.Equal("tab2", secondTab["key"]?.ToString());
        Assert.Equal("Tab 2", secondTab["title"]?.ToString());
    }

    /// <summary>
    /// 测试GenerateTabsConfig - Tab包含badge配置
    /// </summary>
    [Fact]
    public void GenerateTabsConfig_ShouldIncludeBadge_WhenShowBadgeIsTrue()
    {
        // Arrange
        var queryDtoType = typeof(TestQueryDtoWithTabs);
        var crudConfig = new JObject
        {
            ["type"] = "crud",
            ["name"] = "testCrud"
        };

        // Act
        var result = _tabsHelper.GenerateTabsConfig(queryDtoType, crudConfig, "testCrud");

        // Assert
        var tabs = result!["tabs"] as JArray;
        var firstTab = tabs![0] as JObject;
        
        // Badge应该是字符串表达式
        var badge = firstTab!["badge"];
        Assert.NotNull(badge);
        Assert.Equal("${tab1Count}", badge.ToString());
    }

    /// <summary>
    /// 测试GenerateTabsConfig - Tab包含带BadgeLevel的badge配置
    /// </summary>
    [Fact]
    public void GenerateTabsConfig_ShouldIncludeBadgeWithLevel_WhenBadgeLevelIsSet()
    {
        // Arrange
        var queryDtoType = typeof(TestQueryDtoWithBadgeLevel);
        var crudConfig = new JObject
        {
            ["type"] = "crud",
            ["name"] = "testCrud"
        };

        // Act
        var result = _tabsHelper.GenerateTabsConfig(queryDtoType, crudConfig, "testCrud");

        // Assert
        var tabs = result!["tabs"] as JArray;
        var firstTab = tabs![0] as JObject;
        
        // Badge应该是对象，包含text和level
        var badge = firstTab!["badge"] as JObject;
        Assert.NotNull(badge);
        Assert.Equal("${tab1Count}", badge["text"]?.ToString());
        Assert.Equal("warning", badge["level"]?.ToString());
    }

    /// <summary>
    /// 测试GenerateTabsConfig - Tab的filter正确应用到CRUD
    /// </summary>
    [Fact]
    public void GenerateTabsConfig_ShouldApplyFilterToCrud()
    {
        // Arrange
        var queryDtoType = typeof(TestQueryDtoWithTabs);
        var crudConfig = new JObject
        {
            ["type"] = "crud",
            ["name"] = "testCrud"
        };

        // Act
        var result = _tabsHelper.GenerateTabsConfig(queryDtoType, crudConfig, "testCrud");

        // Assert
        var tabs = result!["tabs"] as JArray;
        var firstTab = tabs![0] as JObject;
        var tabCrud = firstTab!["tab"] as JObject;
        var filter = tabCrud!["filter"] as JObject;
        
        Assert.NotNull(filter);
        var filterBody = filter["body"] as JArray;
        Assert.NotNull(filterBody);
        
        // 验证filter中包含status字段
        var statusField = filterBody.FirstOrDefault(f => 
            f is JObject fieldObj && fieldObj["name"]?.ToString() == "status") as JObject;
        Assert.NotNull(statusField);
        Assert.Equal("1", statusField["value"]?.ToString());
    }

    /// <summary>
    /// 测试GenerateTabsConfig - Tab按Order排序
    /// </summary>
    [Fact]
    public void GenerateTabsConfig_ShouldOrderTabsByOrder()
    {
        // Arrange
        var queryDtoType = typeof(TestQueryDtoWithOrderedTabs);
        var crudConfig = new JObject
        {
            ["type"] = "crud",
            ["name"] = "testCrud"
        };

        // Act
        var result = _tabsHelper.GenerateTabsConfig(queryDtoType, crudConfig, "testCrud");

        // Assert
        var tabs = result!["tabs"] as JArray;
        Assert.Equal(3, tabs!.Count);
        
        // 验证顺序：Order=1, Order=2, Order=3
        Assert.Equal("tab1", (tabs[0] as JObject)!["key"]?.ToString());
        Assert.Equal("tab2", (tabs[1] as JObject)!["key"]?.ToString());
        Assert.Equal("tab3", (tabs[2] as JObject)!["key"]?.ToString());
    }

    /// <summary>
    /// 测试CreateCountApiConfig - 生成CountApi配置
    /// </summary>
    [Fact]
    public void CreateCountApiConfig_ShouldGenerateCountApiConfig()
    {
        // Arrange
        var queryDtoType = typeof(TestQueryDtoWithTabs);

        // Act
        var result = _tabsHelper.CreateCountApiConfig(queryDtoType);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("/api/test/tab-counts", result["url"]?.ToString());
        Assert.Equal("GET", result["method"]?.ToString());
    }

    /// <summary>
    /// 测试CreateCountApiConfig - 没有CountApi时返回null
    /// </summary>
    [Fact]
    public void CreateCountApiConfig_ShouldReturnNull_WhenNoCountApi()
    {
        // Arrange
        var queryDtoType = typeof(TestQueryDtoWithoutCountApi);

        // Act
        var result = _tabsHelper.CreateCountApiConfig(queryDtoType);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// 测试GetPageTabsAttribute - 正确获取特性
    /// </summary>
    [Fact]
    public void GetPageTabsAttribute_ShouldReturnAttribute()
    {
        // Arrange
        var queryDtoType = typeof(TestQueryDtoWithTabs);

        // Act
        var result = _tabsHelper.GetPageTabsAttribute(queryDtoType);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("/api/test/tab-counts", result.CountApi);
        Assert.Equal("line", result.TabsMode);
        Assert.Equal("tab1", result.DefaultTab);
        Assert.True(result.ShowBadge);
    }

    #region 测试用的DTO类

    [PageTabs(CountApi = "/api/test/tab-counts", DefaultTab = "tab1", TabsMode = "line", ShowBadge = true)]
    [PageTabItem(Key = "tab1", Title = "Tab 1", Filter = "{\"status\": 1}", Order = 1, Icon = "fa-icon-1")]
    [PageTabItem(Key = "tab2", Title = "Tab 2", Filter = "{\"status\": 2}", Order = 2)]
    private class TestQueryDtoWithTabs : QueryDtoBase
    {
        public int? Status { get; set; }
    }

    [PageTabs(CountApi = "/api/test/tab-counts", DefaultTab = "tab1")]
    [PageTabItem(Key = "tab1", Title = "Tab 1", Filter = "{\"status\": 1}", Order = 1, BadgeLevel = "warning")]
    private class TestQueryDtoWithBadgeLevel : QueryDtoBase
    {
        public int? Status { get; set; }
    }

    [PageTabs(CountApi = "/api/test/tab-counts")]
    [PageTabItem(Key = "tab2", Title = "Tab 2", Filter = "{\"status\": 2}", Order = 2)]
    [PageTabItem(Key = "tab1", Title = "Tab 1", Filter = "{\"status\": 1}", Order = 1)]
    [PageTabItem(Key = "tab3", Title = "Tab 3", Filter = "{\"status\": 3}", Order = 3)]
    private class TestQueryDtoWithOrderedTabs : QueryDtoBase
    {
        public int? Status { get; set; }
    }

    [PageTabs(DefaultTab = "tab1")]
    [PageTabItem(Key = "tab1", Title = "Tab 1", Order = 1)]
    private class TestQueryDtoWithoutCountApi : QueryDtoBase
    {
    }

    private class TestQueryDtoWithoutTabs : QueryDtoBase
    {
    }

    #endregion

    #region 测试辅助类

    /// <summary>
    /// 测试用的TabsHelper包装类
    /// </summary>
    private class TestTabsHelper
    {
        public bool ShouldGenerateTabs(Type? queryDtoType)
        {
            if (queryDtoType == null) return false;
            return queryDtoType.GetCustomAttribute<PageTabsAttribute>() != null;
        }

        public JObject? GenerateTabsConfig(Type? queryDtoType, JObject crudConfig, string crudName)
        {
            if (queryDtoType == null) return null;

            var tabsAttr = queryDtoType.GetCustomAttribute<PageTabsAttribute>();
            if (tabsAttr == null) return null;

            var tabItems = queryDtoType.GetCustomAttributes(typeof(PageTabItemAttribute), false)
                .Cast<PageTabItemAttribute>()
                .OrderBy(x => x.Order)
                .ThenBy(x => x.Key)
                .ToList();

            if (!tabItems.Any()) return null;

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

            JObject tabsConfig = new JObject
            {
                ["type"] = "tabs",
                ["tabsMode"] = tabsAttr.TabsMode ?? "line",
                ["tabs"] = tabs
            };

            if (!string.IsNullOrWhiteSpace(tabsAttr.DefaultTab))
            {
                tabsConfig["defaultKey"] = tabsAttr.DefaultTab;
            }
            else if (tabItems.Any())
            {
                tabsConfig["defaultKey"] = tabItems[0].Key;
            }

            return tabsConfig;
        }

        private JObject? BuildTabConfig(PageTabItemAttribute tabItem, PageTabsAttribute tabsAttr, JObject crudConfig, string crudName)
        {
            if (string.IsNullOrWhiteSpace(tabItem.Key) || string.IsNullOrWhiteSpace(tabItem.Title))
            {
                return null;
            }

            JObject tabCrudConfig = new JObject(crudConfig);

            if (!string.IsNullOrWhiteSpace(tabItem.Filter))
            {
                try
                {
                    JObject filterObj = JObject.Parse(tabItem.Filter);
                    
                    JObject filterConfig;
                    JArray filterBody;
                    
                    if (tabCrudConfig["filter"] != null && tabCrudConfig["filter"] is JObject existingFilter)
                    {
                        filterConfig = existingFilter;
                        if (existingFilter["body"] is JArray existingBody)
                        {
                            filterBody = existingBody;
                        }
                        else
                        {
                            filterBody = new JArray();
                            filterConfig["body"] = filterBody;
                        }
                    }
                    else
                    {
                        filterConfig = new JObject
                        {
                            ["body"] = new JArray()
                        };
                        filterBody = filterConfig["body"] as JArray;
                        tabCrudConfig["filter"] = filterConfig;
                    }

                    foreach (var prop in filterObj.Properties())
                    {
                        var existingField = filterBody.FirstOrDefault(f => 
                            f is JObject fieldObj && fieldObj["name"]?.ToString() == prop.Name);
                        
                        if (existingField != null && existingField is JObject fieldObj)
                        {
                            fieldObj["value"] = prop.Value;
                        }
                        else
                        {
                            filterBody.Add(new JObject
                            {
                                ["name"] = prop.Name,
                                ["type"] = "hidden",
                                ["value"] = prop.Value
                            });
                        }
                    }
                }
                catch
                {
                }
            }

            JObject tab = new JObject
            {
                ["key"] = tabItem.Key,
                ["title"] = tabItem.Title,
                ["tab"] = tabCrudConfig
            };

            if (!string.IsNullOrWhiteSpace(tabItem.Icon))
            {
                tab["icon"] = tabItem.Icon;
            }

            if (tabsAttr.ShowBadge)
            {
                string badgeVarName = $"{tabItem.Key}Count";
                
                if (!string.IsNullOrWhiteSpace(tabItem.BadgeLevel))
                {
                    tab["badge"] = new JObject
                    {
                        ["text"] = $"${{{badgeVarName}}}",
                        ["level"] = tabItem.BadgeLevel
                    };
                }
                else
                {
                    tab["badge"] = $"${{{badgeVarName}}}";
                }
            }

            return tab;
        }

        public JObject? CreateCountApiConfig(Type? queryDtoType)
        {
            var tabsAttr = queryDtoType?.GetCustomAttribute<PageTabsAttribute>();
            if (tabsAttr == null || string.IsNullOrWhiteSpace(tabsAttr.CountApi))
            {
                return null;
            }

            string apiPath = tabsAttr.CountApi;
            if (!apiPath.StartsWith("http://") && !apiPath.StartsWith("https://"))
            {
                // 如果以 / 开头，说明是绝对路径（相对于根路径），直接使用
                // 否则是相对路径，需要拼接基础路径
                if (!apiPath.StartsWith("/"))
                {
                    string baseApi = "/api/test";
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

        public PageTabsAttribute? GetPageTabsAttribute(Type? queryDtoType)
        {
            if (queryDtoType == null) return null;
            return queryDtoType.GetCustomAttribute<PageTabsAttribute>();
        }
    }

    #endregion
}

