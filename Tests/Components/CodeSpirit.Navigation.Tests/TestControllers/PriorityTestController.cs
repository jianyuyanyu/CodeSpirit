using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Enums;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace CodeSpirit.Navigation.Tests.TestControllers
{
    /// <summary>
    /// 演示优先级逻辑的测试控制器集合
    /// </summary>
    
    /// <summary>
    /// 场景1：只有 ModuleAttribute - 应该自动创建导航节点
    /// </summary>
    [Module("PriorityTest", "优先级测试模块", Icon = "fa-solid fa-flask")]
    public class OnlyModuleAttributeController : ControllerBase
    {
        /// <summary>
        /// 测试动作
        /// </summary>
        [Description("只有模块属性的测试动作")]
        public IActionResult Index()
        {
            return Ok("Only ModuleAttribute");
        }
    }

    /// <summary>
    /// 场景2：ModuleAttribute + NavigationAttribute - NavigationAttribute 应该优先
    /// </summary>
    [Module("PriorityTest", "优先级测试模块", Icon = "fa-solid fa-flask")]
    [Navigation(
        Title = "优先级控制器", // 应该覆盖 ModuleAttribute 的 DisplayName
        Icon = "fa-solid fa-star", // 应该覆盖 ModuleAttribute 的 Icon
        Order = 1,
        Permission = "priority_test_navigation",
        PlatformType = PlatformType.System,
        Group = "Testing",
        Tags = new[] { "priority", "test" },
        Priority = 5,
        Badge = "NAV",
        BadgeType = "primary"
    )]
    public class BothAttributesController : ControllerBase
    {
        /// <summary>
        /// NavigationAttribute 优先的测试动作
        /// </summary>
        [Navigation(
            Title = "优先级测试动作",
            Icon = "fa-solid fa-check",
            Order = 1
        )]
        public IActionResult Index()
        {
            return Ok("NavigationAttribute Priority");
        }
    }

    /// <summary>
    /// 场景3：隐藏的 NavigationAttribute - 应该回退到 ModuleAttribute
    /// </summary>
    [Module("PriorityTest", "优先级测试模块", Icon = "fa-solid fa-flask")]
    [Navigation(Hidden = true)] // NavigationAttribute 被隐藏
    public class HiddenNavigationController : ControllerBase
    {
        /// <summary>
        /// 回退到模块属性的测试动作
        /// </summary>
        public IActionResult Index()
        {
            return Ok("Fallback to ModuleAttribute");
        }
    }

    /// <summary>
    /// 场景4：完全没有导航属性 - 不应该出现在导航中
    /// </summary>
    public class NoAttributesController : ControllerBase
    {
        /// <summary>
        /// 没有任何导航属性的动作
        /// </summary>
        public IActionResult Index()
        {
            return Ok("No Navigation");
        }
    }
} 