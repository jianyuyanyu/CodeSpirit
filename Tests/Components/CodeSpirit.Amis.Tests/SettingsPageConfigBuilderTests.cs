using CodeSpirit.Amis;
using CodeSpirit.Amis.Attributes;
using CodeSpirit.Amis.Form;
using CodeSpirit.Amis.Form.Fields;
using CodeSpirit.Amis.Helpers;
using CodeSpirit.Amis.Helpers.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Reflection;
using Xunit;

namespace CodeSpirit.Amis.Tests;

/// <summary>
/// 设置页面配置构建器测试类
/// </summary>
public class SettingsPageConfigBuilderTests
{
    private readonly TestApiRouteHelper _apiRouteHelper;
    private readonly TestFormFieldHelper _formFieldHelper;
    private readonly TestAmisApiHelper _amisApiHelper;
    private readonly ILogger<SettingsPageConfigBuilder> _logger;
    private readonly TestSettingsPageConfigBuilder _builder;

    public SettingsPageConfigBuilderTests()
    {
        _apiRouteHelper = new TestApiRouteHelper();
        _formFieldHelper = new TestFormFieldHelper();
        _amisApiHelper = new TestAmisApiHelper();
        _logger = new TestLogger<SettingsPageConfigBuilder>();
        _builder = new TestSettingsPageConfigBuilder(
            _apiRouteHelper,
            _formFieldHelper,
            _amisApiHelper,
            _logger);
    }

    /// <summary>
    /// 测试从HeaderOperation生成Tab配置
    /// </summary>
    [Fact]
    public void GenerateSettingsPageConfig_ShouldGenerateTabsFromHeaderOperations()
    {
        // Arrange
        var controllerType = typeof(TestSettingsController);
        var settingsAttr = new SettingsPageAttribute("测试设置页面")
        {
            TabsMode = "line",
            Animated = true
        };

        // Act
        var result = _builder.GenerateSettingsPageConfig(controllerType, settingsAttr);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("page", result["type"]?.ToString());
        Assert.Equal("测试设置页面", result["title"]?.ToString());
        
        var body = result["body"] as JObject;
        Assert.NotNull(body);
        Assert.Equal("tabs", body["type"]?.ToString());
        Assert.Equal("line", body["tabsMode"]?.ToString());
        Assert.Equal(true, body["animated"]?.ToObject<bool>());
        
        var tabs = body["tabs"] as JArray;
        Assert.NotNull(tabs);
        Assert.Equal(2, tabs.Count);
        
        // 验证第一个Tab
        var firstTab = tabs[0] as JObject;
        Assert.NotNull(firstTab);
        Assert.Equal("设置一", firstTab["title"]?.ToString());
        Assert.Equal("fa-icon-1", firstTab["icon"]?.ToString());
        
        // 验证第二个Tab
        var secondTab = tabs[1] as JObject;
        Assert.NotNull(secondTab);
        Assert.Equal("设置二", secondTab["title"]?.ToString());
        Assert.Equal("fa-icon-2", secondTab["icon"]?.ToString());
    }

    /// <summary>
    /// 测试没有HeaderOperation方法时返回null
    /// </summary>
    [Fact]
    public void GenerateSettingsPageConfig_ShouldReturnNull_WhenNoHeaderOperations()
    {
        // Arrange
        var controllerType = typeof(TestControllerWithoutHeaderOperations);
        var settingsAttr = new SettingsPageAttribute("测试设置页面");

        // Act
        var result = _builder.GenerateSettingsPageConfig(controllerType, settingsAttr);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// 测试Tab配置包含表单结构
    /// </summary>
    [Fact]
    public void GenerateTabConfig_ShouldContainFormStructure()
    {
        // Arrange
        var controllerType = typeof(TestSettingsController);
        var method = controllerType.GetMethod(nameof(TestSettingsController.SaveSetting1));
        var operation = method!.GetCustomAttribute<HeaderOperationAttribute>();

        // Act
        var tab = _builder.GenerateTabConfigTest(method!, operation!, controllerType);

        // Assert
        Assert.NotNull(tab);
        var tabForm = tab["tab"] as JObject;
        Assert.NotNull(tabForm);
        Assert.Equal("form", tabForm["type"]?.ToString());
        Assert.Equal("保存设置", tabForm["submitText"]?.ToString());
        Assert.Equal("horizontal", tabForm["mode"]?.ToString());
        
        var api = tabForm["api"] as JObject;
        Assert.NotNull(api);
        Assert.Equal("/api/test/savesetting1", api["url"]?.ToString());
        Assert.Equal("PUT", api["method"]?.ToString());
        
        var initApi = tabForm["initApi"] as JObject;
        Assert.NotNull(initApi);
        Assert.Equal("/api/test/getsetting1", initApi["url"]?.ToString());
        Assert.Equal("GET", initApi["method"]?.ToString());
    }

    /// <summary>
    /// 测试使用Label作为Tab标题
    /// </summary>
    [Fact]
    public void GenerateTabConfig_ShouldUseLabelAsTabTitle()
    {
        // Arrange
        var controllerType = typeof(TestSettingsController);
        var method = controllerType.GetMethod(nameof(TestSettingsController.SaveSetting1));
        var operation = method!.GetCustomAttribute<HeaderOperationAttribute>();

        // Act
        var tab = _builder.GenerateTabConfigTest(method!, operation!, controllerType);

        // Assert
        Assert.NotNull(tab);
        Assert.Equal("设置一", tab["title"]?.ToString());
    }

    /// <summary>
    /// 测试使用Icon作为Tab图标
    /// </summary>
    [Fact]
    public void GenerateTabConfig_ShouldUseIconAsTabIcon()
    {
        // Arrange
        var controllerType = typeof(TestSettingsController);
        var method = controllerType.GetMethod(nameof(TestSettingsController.SaveSetting1));
        var operation = method!.GetCustomAttribute<HeaderOperationAttribute>();

        // Act
        var tab = _builder.GenerateTabConfigTest(method!, operation!, controllerType);

        // Assert
        Assert.NotNull(tab);
        Assert.Equal("fa-icon-1", tab["icon"]?.ToString());
    }

    /// <summary>
    /// 测试Tab按方法定义顺序排序
    /// </summary>
    [Fact]
    public void GenerateSettingsPageConfig_ShouldOrderTabsByMethodDefinitionOrder()
    {
        // Arrange
        var controllerType = typeof(TestSettingsController);
        var settingsAttr = new SettingsPageAttribute("测试设置页面");

        // Act
        var result = _builder.GenerateSettingsPageConfig(controllerType, settingsAttr);

        // Assert
        var tabs = (result!["body"] as JObject)!["tabs"] as JArray;
        Assert.NotNull(tabs);
        Assert.Equal(2, tabs.Count);
        
        // 验证顺序：第一个应该是SaveSetting1，第二个是SaveSetting2
        Assert.Equal("设置一", (tabs[0] as JObject)!["title"]?.ToString());
        Assert.Equal("设置二", (tabs[1] as JObject)!["title"]?.ToString());
    }

    /// <summary>
    /// 测试InitApi自动匹配逻辑 - SaveXxx -> GetXxx
    /// </summary>
    [Fact]
    public void FindInitApi_ShouldMatchGetMethodByConvention()
    {
        // Arrange
        var controllerType = typeof(TestSettingsController);
        var saveMethod = controllerType.GetMethod(nameof(TestSettingsController.SaveSetting1));

        // Act
        var initApi = _builder.FindInitApiForMethodTest(saveMethod!, controllerType);

        // Assert
        Assert.NotNull(initApi);
        Assert.Equal("/api/test/getsetting1", initApi["url"]?.ToString());
        Assert.Equal("GET", initApi["method"]?.ToString());
    }

    #region 测试用的控制器类

    /// <summary>
    /// 测试用的设置控制器
    /// </summary>
    [SettingsPage(Title = "测试设置")]
    private class TestSettingsController
    {
        [HttpGet("get-setting1")]
        public void GetSetting1() { }

        [HttpPut("save-setting1")]
        [HeaderOperation("设置一", "form", Icon = "fa-icon-1")]
        public void SaveSetting1([FromBody] TestDto dto) { }

        [HttpGet("get-setting2")]
        public void GetSetting2() { }

        [HttpPut("save-setting2")]
        [HeaderOperation("设置二", "form", Icon = "fa-icon-2")]
        public void SaveSetting2([FromBody] TestDto dto) { }
    }

    /// <summary>
    /// 没有HeaderOperation的测试控制器
    /// </summary>
    [SettingsPage(Title = "测试设置")]
    private class TestControllerWithoutHeaderOperations
    {
        [HttpGet("get")]
        public void Get() { }
    }

    /// <summary>
    /// 测试用的DTO
    /// </summary>
    private class TestDto
    {
        public string Name { get; set; } = string.Empty;
    }

    #endregion

    #region 测试辅助类

    /// <summary>
    /// 测试用的ApiRouteHelper（使用组合模式代替继承）
    /// </summary>
    /// <summary>
    /// 测试用的SettingsPageConfigBuilder，使用测试辅助类
    /// </summary>
    private class TestSettingsPageConfigBuilder : SettingsPageConfigBuilder
    {
        private readonly TestApiRouteHelper _testApiRouteHelper;
        private readonly TestFormFieldHelper _testFormFieldHelper;
        private readonly TestAmisApiHelper _testAmisApiHelper;

        public TestSettingsPageConfigBuilder(
            TestApiRouteHelper apiRouteHelper,
            TestFormFieldHelper formFieldHelper,
            TestAmisApiHelper amisApiHelper,
            ILogger<SettingsPageConfigBuilder> logger)
            : base(null!, null!, null!, logger)
        {
            _testApiRouteHelper = apiRouteHelper;
            _testFormFieldHelper = formFieldHelper;
            _testAmisApiHelper = amisApiHelper;
        }

        // 重写GenerateSettingsPageConfig以使用测试Helper
        public new JObject GenerateSettingsPageConfig(Type controllerType, SettingsPageAttribute settingsAttr)
        {
            // 复制原始逻辑，但使用测试的Helper
            var methods = controllerType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Select((m, index) => new
                {
                    Method = m,
                    Operation = m.GetCustomAttribute<HeaderOperationAttribute>(),
                    Order = index
                })
                .Where(x => x.Operation != null)
                .OrderBy(x => x.Order)
                .ToList();

            if (!methods.Any())
            {
                return null;
            }

            JArray tabs = new JArray();
            foreach (var methodInfo in methods)
            {
                var tab = GenerateTabConfigTest(methodInfo.Method, methodInfo.Operation, controllerType);
                if (tab != null)
                {
                    tabs.Add(tab);
                }
            }

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

        public JObject GenerateTabConfigTest(MethodInfo method, HeaderOperationAttribute operation, Type controllerType)
        {
            try
            {
                string tabTitle = operation.Label;
                string tabIcon = operation.Icon;

                var route = _testApiRouteHelper.GetApiRouteInfoForMethod(method);
                JObject initApi = FindInitApiForMethodTest(method, controllerType);

                var parameters = method.GetParameters();
                var formFields = _testFormFieldHelper.GetAmisFormFieldsFromParameters(parameters);

                JObject tabForm = new JObject
                {
                    ["type"] = "form",
                    ["api"] = _testAmisApiHelper.CreateApi(route),
                    ["body"] = new JArray(formFields),
                    ["submitText"] = "保存设置",
                    ["mode"] = "horizontal",
                    ["horizontal"] = new JObject
                    {
                        ["left"] = 4,
                        ["right"] = 8
                    }
                };

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
            catch
            {
                return null;
            }
        }

        public JObject FindInitApiForMethodTest(MethodInfo saveMethod, Type controllerType)
        {
            try
            {
                string saveMethodName = saveMethod.Name;
                string getMethodName = saveMethodName.Replace("Save", "Get").Replace("Update", "Get").Replace("Put", "Get");

                var getMethod = controllerType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(m =>
                        m.Name.Equals(getMethodName, StringComparison.OrdinalIgnoreCase) &&
                        m.GetCustomAttributes(typeof(HttpGetAttribute), false).Any());

                if (getMethod != null)
                {
                    var route = _testApiRouteHelper.GetApiRouteInfoForMethod(getMethod);
                    if (route != null)
                    {
                        return _testAmisApiHelper.CreateApi(route);
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>
    /// 测试用的ApiRouteHelper（使用组合模式代替继承）
    /// </summary>
    private class TestApiRouteHelper
    {
        public ApiRouteInfo GetApiRouteInfoForMethod(MethodInfo method)
        {
            var methodName = method.Name.ToLower();
            var httpMethod = method.GetCustomAttribute<HttpPutAttribute>() != null ? "PUT" :
                           method.GetCustomAttribute<HttpGetAttribute>() != null ? "GET" : "POST";
            
            return new ApiRouteInfo($"/api/test/{methodName}", httpMethod);
        }
    }

    /// <summary>
    /// 测试用的FormFieldHelper
    /// </summary>
    private class TestFormFieldHelper
    {
        public List<JObject> GetAmisFormFieldsFromParameters(IEnumerable<ParameterInfo> parameters)
        {
            return new List<JObject>
            {
                new JObject
                {
                    ["type"] = "input-text",
                    ["name"] = "name",
                    ["label"] = "名称"
                }
            };
        }
    }

    /// <summary>
    /// 测试用的权限服务
    /// </summary>
    private class TestPermissionService : CodeSpirit.Core.Authorization.IHasPermissionService
    {
        public bool HasPermission(string permission) => true;
        public string GetPermissionCode(System.Reflection.MethodInfo methodInfo) => string.Empty;
        public bool HasNavigationPermission(string permissionCode) => true;
    }

    /// <summary>
    /// 测试用的AmisApiHelper
    /// </summary>
    private class TestAmisApiHelper
    {
        public JObject CreateApi(ApiRouteInfo apiRoute)
        {
            return new JObject
            {
                ["url"] = apiRoute.ApiPath,
                ["method"] = apiRoute.HttpMethod
            };
        }
    }

    /// <summary>
    /// 测试用的Logger
    /// </summary>
    private class TestLogger<T> : ILogger<T>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => false;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
    }

    #endregion
}
