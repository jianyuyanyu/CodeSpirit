using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Enums;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace CodeSpirit.Navigation.Tests.TestControllers
{
    /// <summary>
    /// 测试模块控制器
    /// </summary>
    [Module("TestModule", "测试模块", Icon = "fa-solid fa-test")]
    [Navigation(
        Title = "测试管理",
        Icon = "fa-solid fa-clipboard-check",
        Order = 1,
        PlatformType = PlatformType.Both,
        Group = "Management",
        Tags = new[] { "test", "management" },
        Priority = 10,
        Badge = "NEW",
        BadgeType = "success")]
    public class TestModuleController : ControllerBase
    {
        /// <summary>
        /// 测试列表页面
        /// </summary>
        [Navigation(
            Title = "测试列表",
            Icon = "fa-solid fa-list",
            Order = 1,
            PlatformType = PlatformType.Both,
            Group = "Management",
            Tags = new[] { "list" },
            RequireAuth = true)]
        [DisplayName("测试列表")]
        [Description("显示所有测试项目的列表")]
        public IActionResult Index()
        {
            return Ok();
        }

        /// <summary>
        /// 创建测试
        /// </summary>
        [Navigation(
            Title = "创建测试",
            Icon = "fa-solid fa-plus",
            Order = 2,
            PlatformType = PlatformType.System,
            Group = "Management",
            Tags = new[] { "create" },
            RequireAuth = true,
            Permission = "test_create",
            IsExperimental = true,
            MinVersion = "1.0.0",
            MaxVersion = "2.0.0",
            SupportedDevices = new[] { "desktop", "tablet" },
            Priority = 5,
            Shortcut = "Ctrl+N",
            Badge = "BETA",
            BadgeType = "warning",
            MetaDataJson = "{\"category\": \"admin\", \"level\": \"advanced\"}")]
        public IActionResult Create()
        {
            return Ok();
        }

        /// <summary>
        /// 编辑测试（隐藏菜单项）
        /// </summary>
        [Navigation(
            Title = "编辑测试",
            Hidden = true)]
        public IActionResult Edit(int id)
        {
            return Ok();
        }

        /// <summary>
        /// 删除测试（仅租户平台）
        /// </summary>
        [Navigation(
            Title = "删除测试",
            Icon = "fa-solid fa-trash",
            Order = 3,
            PlatformType = PlatformType.Tenant,
            RequireAuth = true,
            Permission = "test_delete")]
        public IActionResult Delete(int id)
        {
            return Ok();
        }

        /// <summary>
        /// 外部链接测试
        /// </summary>
        [Navigation(
            Title = "外部文档",
            Icon = "fa-solid fa-external-link",
            Order = 4,
            IsExternal = true,
            Target = "_blank",
            PlatformType = PlatformType.Both)]
        public IActionResult ExternalDoc()
        {
            return Ok();
        }
    }

    /// <summary>
    /// 另一个测试模块控制器
    /// </summary>
    [Module("AnotherModule", "另一个模块", Icon = "fa-solid fa-cog")]
    [Navigation(
        Title = "设置管理",
        Icon = "fa-solid fa-cog",
        Order = 2,
        PlatformType = PlatformType.System,
        Group = "Settings")]
    public class AnotherModuleController : ControllerBase
    {
        /// <summary>
        /// 系统设置
        /// </summary>
        [Navigation(
            Title = "系统设置",
            Icon = "fa-solid fa-gear",
            Order = 1,
            PlatformType = PlatformType.System,
            RequireAuth = true)]
        public IActionResult SystemSettings()
        {
            return Ok();
        }
    }
} 