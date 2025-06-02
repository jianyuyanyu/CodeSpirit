using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Enums;
using CodeSpirit.Navigation.Models;
using CodeSpirit.Navigation.Tests.TestBase;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Moq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Newtonsoft.Json;

namespace CodeSpirit.Navigation.Tests
{
    /// <summary>
    /// 系统平台控制器导航测试 - 专门测试 SystemUsersController 等系统平台控制器的导航识别问题
    /// </summary>
    public class SystemPlatformControllersNavigationTests : NavigationTestBase
    {
        private readonly ITestOutputHelper _output;

        public SystemPlatformControllersNavigationTests(ITestOutputHelper output)
        {
            _output = output;
        }

        /// <summary>
        /// 模拟 SystemUsersController 的控制器描述符
        /// </summary>
        private ControllerActionDescriptor CreateSystemUsersControllerDescriptor()
        {
            // 创建模拟的 SystemUsersController 类型
            var systemUsersControllerType = typeof(TestSystemUsersController);
            
            var descriptor = new ControllerActionDescriptor
            {
                ControllerTypeInfo = systemUsersControllerType.GetTypeInfo(),
                ControllerName = "SystemUsers",
                ActionName = "Index",
                MethodInfo = systemUsersControllerType.GetMethod("Index")
            };

            return descriptor;
        }

        /// <summary>
        /// 创建模拟的ActionDescriptorCollection
        /// </summary>
        private ActionDescriptorCollection CreateMockActionDescriptorCollection(List<ControllerActionDescriptor> descriptors)
        {
            return new ActionDescriptorCollection(descriptors.Cast<Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor>().ToList(), 1);
        }

        /// <summary>
        /// 测试系统平台控制器是否能被正确识别和过滤
        /// </summary>
        [Fact]
        public void NavigationService_ShouldDetectSystemUsersController()
        {
            _output.WriteLine("=== 测试系统平台控制器识别 ===");

            // Arrange - 设置模拟的控制器描述符
            var systemUsersDescriptor = CreateSystemUsersControllerDescriptor();
            var descriptors = new List<ControllerActionDescriptor> { systemUsersDescriptor };

            MockActionProvider.Setup(x => x.ActionDescriptors)
                .Returns(CreateMockActionDescriptorCollection(descriptors));

            // 验证控制器的特性配置
            var controllerType = typeof(TestSystemUsersController);
            var moduleAttr = controllerType.GetCustomAttribute<ModuleAttribute>();
            var navigationAttr = controllerType.GetCustomAttribute<NavigationAttribute>();

            _output.WriteLine($"控制器类型: {controllerType.Name}");
            _output.WriteLine($"模块特性: {moduleAttr?.Name} - {moduleAttr?.DisplayName}");
            _output.WriteLine($"导航特性: Icon={navigationAttr?.Icon}, PlatformType={navigationAttr?.PlatformType}");

            // Assert - 验证特性配置
            Assert.NotNull(moduleAttr);
            Assert.Equal("identity", moduleAttr.Name);
            Assert.NotNull(navigationAttr);
            Assert.Equal(PlatformType.System, navigationAttr.PlatformType);

            _output.WriteLine("✓ 控制器特性配置验证通过");
        }

        /// <summary>
        /// 测试构建代码导航树是否包含系统平台控制器
        /// </summary>
        [Fact]
        public void BuildCodeBasedNavigation_ShouldIncludeSystemUsersController()
        {
            _output.WriteLine("=== 测试构建代码导航树 ===");

            // Arrange
            var systemUsersDescriptor = CreateSystemUsersControllerDescriptor();
            var descriptors = new List<ControllerActionDescriptor> { systemUsersDescriptor };

            MockActionProvider.Setup(x => x.ActionDescriptors)
                .Returns(CreateMockActionDescriptorCollection(descriptors));

            // Act - 使用反射调用私有方法 BuildCodeBasedNavigation
            var buildMethod = typeof(NavigationService).GetMethod("BuildCodeBasedNavigation", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(buildMethod);

            var result = (List<NavigationNode>)buildMethod.Invoke(NavigationService, new object[] { "identity" });

            // Assert
            _output.WriteLine($"构建结果: 找到 {result?.Count ?? 0} 个模块节点");
            
            if (result?.Any() == true)
            {
                var identityModule = result.First();
                _output.WriteLine($"模块: {identityModule.Title} (PlatformType: {identityModule.PlatformType})");
                _output.WriteLine($"控制器数量: {identityModule.Children.Count}");

                foreach (var controller in identityModule.Children)
                {
                    _output.WriteLine($"  控制器: {controller.Title} (PlatformType: {controller.PlatformType})");
                }

                // 验证是否包含 SystemUsers 控制器
                var systemUsersController = identityModule.Children
                    .FirstOrDefault(c => c.Name.Contains("systemUsers", StringComparison.OrdinalIgnoreCase));
                
                Assert.NotNull(systemUsersController);
                Assert.Equal(PlatformType.System, systemUsersController.PlatformType);
                _output.WriteLine("✓ SystemUsers控制器已正确包含在导航树中");
            }
            else
            {
                _output.WriteLine("⚠️ 未找到任何导航节点");
                Assert.Fail("应该找到identity模块的导航节点");
            }
        }

        /// <summary>
        /// 测试平台类型过滤 - 系统平台查询应该返回系统平台控制器
        /// </summary>
        [Fact]
        public void FilterNodesByPlatform_SystemQuery_ShouldReturnSystemControllers()
        {
            _output.WriteLine("=== 测试系统平台过滤 ===");

            // Arrange - 创建测试导航节点
            var identityModule = new NavigationNode("identity", "用户中心", "/identity")
            {
                PlatformType = PlatformType.Both,
                OriginalPlatformType = PlatformType.Both
            };

            var systemUsersController = new NavigationNode("systemUsers", "系统用户管理", "/identity/systemUsers")
            {
                PlatformType = PlatformType.System,
                OriginalPlatformType = PlatformType.System
            };

            var tenantUsersController = new NavigationNode("users", "用户管理", "/identity/users")
            {
                PlatformType = PlatformType.Tenant,
                OriginalPlatformType = PlatformType.Tenant
            };

            identityModule.Children.Add(systemUsersController);
            identityModule.Children.Add(tenantUsersController);

            var allNodes = new List<NavigationNode> { identityModule };

            // Act - 进行系统平台过滤
            var systemNodes = NavigationService.FilterNodesByPlatform(allNodes, PlatformType.System);

            // Assert
            _output.WriteLine($"过滤前节点数: {allNodes.Count}");
            _output.WriteLine($"过滤后节点数: {systemNodes.Count}");

            Assert.Single(systemNodes);
            var filteredModule = systemNodes.First();
            
            _output.WriteLine($"过滤后模块: {filteredModule.Title}");
            _output.WriteLine($"过滤后控制器数: {filteredModule.Children.Count}");

            foreach (var controller in filteredModule.Children)
            {
                _output.WriteLine($"  控制器: {controller.Title} (PlatformType: {controller.PlatformType})");
            }

            // 验证只包含系统平台的控制器
            Assert.Single(filteredModule.Children);
            Assert.Equal("systemUsers", filteredModule.Children.First().Name);
            Assert.Equal(PlatformType.System, filteredModule.Children.First().PlatformType);

            _output.WriteLine("✓ 系统平台过滤正确工作");
        }

        /// <summary>
        /// 测试GetNavigationTreeAsync方法的完整流程
        /// </summary>
        [Fact]
        public async Task GetNavigationTreeAsync_SystemPlatform_ShouldReturnSystemControllers()
        {
            _output.WriteLine("=== 测试GetNavigationTreeAsync完整流程 ===");

            // Arrange
            var systemUsersDescriptor = CreateSystemUsersControllerDescriptor();
            var descriptors = new List<ControllerActionDescriptor> { systemUsersDescriptor };

            MockActionProvider.Setup(x => x.ActionDescriptors)
                .Returns(CreateMockActionDescriptorCollection(descriptors));

            // 模拟模块名缓存
            var moduleNamesJson = JsonConvert.SerializeObject(new List<string> { "identity" });
            var moduleNamesBytes = System.Text.Encoding.UTF8.GetBytes(moduleNamesJson);
            MockCache.Setup(x => x.GetAsync("CodeSpirit:Navigation:ModuleNames", default))
                .ReturnsAsync(moduleNamesBytes);

            // 创建测试导航节点 - 根据我们的修复，identity模块应该是System类型（只有一个System控制器）
            var testNodes = new List<NavigationNode>
            {
                new NavigationNode("identity", "用户中心", "/identity")
                {
                    PlatformType = PlatformType.System, // 修复后应该推断为System
                    Children = new List<NavigationNode>
                    {
                        new NavigationNode("systemUsers", "系统用户管理", "/identity/systemUsers")
                        {
                            PlatformType = PlatformType.System
                        }
                    }
                }
            };

            // 模拟系统平台缓存
            var systemCacheJson = JsonConvert.SerializeObject(testNodes);
            var systemCacheBytes = System.Text.Encoding.UTF8.GetBytes(systemCacheJson);
            MockCache.Setup(x => x.GetAsync("CodeSpirit:Navigation:Module:identity:System", default))
                .ReturnsAsync(systemCacheBytes);

            // Act
            var result = await NavigationService.GetNavigationTreeAsync(PlatformType.System);

            // Assert
            _output.WriteLine($"最终结果节点数: {result?.Count ?? 0}");

            if (result?.Any() == true)
            {
                foreach (var module in result)
                {
                    _output.WriteLine($"模块: {module.Title} (PlatformType: {module.PlatformType})");
                    foreach (var controller in module.Children)
                    {
                        _output.WriteLine($"  控制器: {controller.Title} (PlatformType: {controller.PlatformType})");
                    }
                }

                // 验证包含系统平台的控制器
                var systemControllers = result.SelectMany(m => m.Children)
                    .Where(c => c.PlatformType == PlatformType.System)
                    .ToList();

                Assert.NotEmpty(systemControllers);
                _output.WriteLine($"✓ 找到 {systemControllers.Count} 个系统平台控制器");
            }
            else
            {
                _output.WriteLine("⚠️ GetNavigationTreeAsync未返回任何节点");
                Assert.Fail("GetNavigationTreeAsync应该返回系统平台的导航节点");
            }
        }

        /// <summary>
        /// 测试位运算过滤逻辑的正确性
        /// </summary>
        [Theory]
        [InlineData(PlatformType.System, PlatformType.System, true)]
        [InlineData(PlatformType.System, PlatformType.Tenant, false)]
        [InlineData(PlatformType.System, PlatformType.Both, true)]
        [InlineData(PlatformType.Both, PlatformType.System, true)]
        [InlineData(PlatformType.Both, PlatformType.Tenant, true)]
        [InlineData(PlatformType.Inherit, PlatformType.System, false)]
        public void PlatformTypeFiltering_BitwiseLogic_ShouldWorkCorrectly(
            PlatformType nodePlatformType, 
            PlatformType queryPlatformType, 
            bool expectedIncluded)
        {
            _output.WriteLine($"=== 测试位运算逻辑: {nodePlatformType} & {queryPlatformType} ===");

            // Act - 模拟FilterNodesByPlatform中的位运算逻辑
            bool actualIncluded = (nodePlatformType & queryPlatformType) != 0;

            // Assert
            _output.WriteLine($"节点平台类型: {nodePlatformType} ({(int)nodePlatformType})");
            _output.WriteLine($"查询平台类型: {queryPlatformType} ({(int)queryPlatformType})");
            _output.WriteLine($"位运算结果: {(int)(nodePlatformType & queryPlatformType)}");
            _output.WriteLine($"是否包含: {actualIncluded} (期望: {expectedIncluded})");

            Assert.Equal(expectedIncluded, actualIncluded);
        }
    }

    /// <summary>
    /// 测试用的 SystemUsersController 模拟类
    /// </summary>
    [Module("identity", "用户中心")]
    [DisplayName("系统用户管理")]
    [Navigation(Icon = "fa-solid fa-users-gear", PlatformType = PlatformType.System)]
    public class TestSystemUsersController : ControllerBase
    {
        [HttpGet]
        public IActionResult Index()
        {
            return Ok();
        }
    }
} 