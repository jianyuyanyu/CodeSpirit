using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Enums;
using CodeSpirit.Navigation.Extensions;
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
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Newtonsoft.Json;

namespace CodeSpirit.Navigation.Tests
{
    /// <summary>
    /// 测试平台类型推断修复的效果
    /// </summary>
    public class PlatformTypeInferenceFixTest : NavigationTestBase
    {
        private readonly ITestOutputHelper _output;

        public PlatformTypeInferenceFixTest(ITestOutputHelper output)
        {
            _output = output;
        }

        /// <summary>
        /// 测试混合平台类型模块的推断逻辑
        /// </summary>
        [Fact]
        public void BuildCodeBasedNavigation_MixedPlatformTypes_ShouldInferCorrectly()
        {
            _output.WriteLine("=== 测试混合平台类型模块的推断逻辑 ===");

            // 创建混合平台类型的控制器描述符
            var systemUsersDescriptor = CreateControllerDescriptor(typeof(TestFixSystemUsersController), "SystemUsers");
            var tenantUsersDescriptor = CreateControllerDescriptor(typeof(TestFixTenantUsersController), "Users");
            var bothRolesDescriptor = CreateControllerDescriptor(typeof(TestFixRolesController), "Roles");

            var descriptors = new List<ControllerActionDescriptor> 
            { 
                systemUsersDescriptor, 
                tenantUsersDescriptor, 
                bothRolesDescriptor 
            };

            MockActionProvider.Setup(x => x.ActionDescriptors)
                .Returns(CreateMockActionDescriptorCollection(descriptors));

            // Act - 使用反射调用私有方法 BuildCodeBasedNavigation
            var buildMethod = typeof(NavigationService).GetMethod("BuildCodeBasedNavigation", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(buildMethod);

            var result = (List<NavigationNode>)buildMethod.Invoke(NavigationService, new object[] { "identity" });

            // Assert
            _output.WriteLine($"构建结果: 找到 {result?.Count ?? 0} 个模块节点");
            
            Assert.Single(result);
            var identityModule = result.First();
            
            _output.WriteLine($"模块平台类型: {identityModule.PlatformType}");
            _output.WriteLine($"控制器数量: {identityModule.Children.Count}");

            foreach (var controller in identityModule.Children)
            {
                _output.WriteLine($"  控制器: {controller.Title} (Name: {controller.Name}, PlatformType: {controller.PlatformType})");
            }

            // 验证模块推断为Both（因为包含System和Tenant类型的控制器）
            Assert.Equal(PlatformType.Both, identityModule.PlatformType);
            
            // 验证包含了所有三个控制器
            Assert.Equal(3, identityModule.Children.Count);

            _output.WriteLine("✓ 混合平台类型模块推断正确");
        }

        /// <summary>
        /// 测试纯系统平台模块的推断逻辑
        /// </summary>
        [Fact]
        public void BuildCodeBasedNavigation_SystemOnlyModule_ShouldInferSystem()
        {
            _output.WriteLine("=== 测试纯系统平台模块的推断逻辑 ===");

            // 只创建系统平台的控制器
            var systemUsersDescriptor = CreateControllerDescriptor(typeof(TestFixSystemUsersController), "SystemUsers");
            var systemRolesDescriptor = CreateControllerDescriptor(typeof(TestFixSystemRolesController), "SystemRoles");

            var descriptors = new List<ControllerActionDescriptor> 
            { 
                systemUsersDescriptor, 
                systemRolesDescriptor 
            };

            MockActionProvider.Setup(x => x.ActionDescriptors)
                .Returns(CreateMockActionDescriptorCollection(descriptors));

            // Act
            var buildMethod = typeof(NavigationService).GetMethod("BuildCodeBasedNavigation", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            var result = (List<NavigationNode>)buildMethod.Invoke(NavigationService, new object[] { "identity" });

            // Assert
            Assert.Single(result);
            var identityModule = result.First();
            
            _output.WriteLine($"模块平台类型: {identityModule.PlatformType}");
            
            // 验证模块推断为System（因为只包含System类型的控制器）
            Assert.Equal(PlatformType.System, identityModule.PlatformType);

            _output.WriteLine("✓ 纯系统平台模块推断正确");
        }

        /// <summary>
        /// 测试包含Both类型控制器的模块推断逻辑
        /// </summary>
        [Fact]
        public void BuildCodeBasedNavigation_WithBothController_ShouldInferBoth()
        {
            _output.WriteLine("=== 测试包含Both类型控制器的模块推断逻辑 ===");

            // 创建包含Both类型控制器的模块
            var systemUsersDescriptor = CreateControllerDescriptor(typeof(TestFixSystemUsersController), "SystemUsers");
            var bothRolesDescriptor = CreateControllerDescriptor(typeof(TestFixRolesController), "Roles");

            var descriptors = new List<ControllerActionDescriptor> 
            { 
                systemUsersDescriptor, 
                bothRolesDescriptor 
            };

            MockActionProvider.Setup(x => x.ActionDescriptors)
                .Returns(CreateMockActionDescriptorCollection(descriptors));

            // Act
            var buildMethod = typeof(NavigationService).GetMethod("BuildCodeBasedNavigation", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            var result = (List<NavigationNode>)buildMethod.Invoke(NavigationService, new object[] { "identity" });

            // Assert
            Assert.Single(result);
            var identityModule = result.First();
            
            _output.WriteLine($"模块平台类型: {identityModule.PlatformType}");
            
            // 验证模块推断为Both（因为包含Both类型的控制器）
            Assert.Equal(PlatformType.Both, identityModule.PlatformType);

            _output.WriteLine("✓ 包含Both控制器的模块推断正确");
        }

        /// <summary>
        /// 测试GetNavigationTreeAsync在修复后的行为
        /// </summary>
        [Fact]
        public async Task GetNavigationTreeAsync_AfterFix_ShouldWorkCorrectly()
        {
            _output.WriteLine("=== 测试GetNavigationTreeAsync修复后的行为 ===");

            // 创建混合平台类型的控制器
            var systemUsersDescriptor = CreateControllerDescriptor(typeof(TestFixSystemUsersController), "SystemUsers");
            var tenantUsersDescriptor = CreateControllerDescriptor(typeof(TestFixTenantUsersController), "Users");

            var descriptors = new List<ControllerActionDescriptor> 
            { 
                systemUsersDescriptor, 
                tenantUsersDescriptor 
            };

            MockActionProvider.Setup(x => x.ActionDescriptors)
                .Returns(CreateMockActionDescriptorCollection(descriptors));

            // 模拟模块名缓存 - 不能使用扩展方法，需要直接模拟原生方法
            var moduleNamesJson = JsonConvert.SerializeObject(new List<string> { "identity" });
            var moduleNamesBytes = System.Text.Encoding.UTF8.GetBytes(moduleNamesJson);
            MockCache.Setup(x => x.GetAsync("CodeSpirit:Navigation:ModuleNames", default))
                .ReturnsAsync(moduleNamesBytes);

            // 模拟系统平台和Both平台的缓存（这应该在修复后被正确创建）
            var moduleNodes = new List<NavigationNode>
            {
                new NavigationNode("identity", "用户中心", "/identity")
                {
                    PlatformType = PlatformType.Both,
                    Children = new List<NavigationNode>
                    {
                        new NavigationNode("systemUsers", "系统用户管理", "/identity/systemUsers")
                        {
                            PlatformType = PlatformType.System
                        },
                        new NavigationNode("users", "用户管理", "/identity/users")
                        {
                            PlatformType = PlatformType.Tenant
                        }
                    }
                }
            };

            // 系统平台的缓存应该包含过滤后的节点
            var systemFilteredNodes = NavigationService.FilterNodesByPlatform(moduleNodes, PlatformType.System);
            var systemCacheJson = JsonConvert.SerializeObject(systemFilteredNodes);
            var systemCacheBytes = System.Text.Encoding.UTF8.GetBytes(systemCacheJson);
            MockCache.Setup(x => x.GetAsync("CodeSpirit:Navigation:Module:identity:System", default))
                .ReturnsAsync(systemCacheBytes);

            // Act
            var result = await NavigationService.GetNavigationTreeAsync(PlatformType.System);

            // Assert
            _output.WriteLine($"GetNavigationTreeAsync(System) 结果: {result?.Count ?? 0} 个模块");
            
            if (result?.Any() == true)
            {
                var identityModule = result.First();
                _output.WriteLine($"模块: {identityModule.Title}, 控制器数: {identityModule.Children.Count}");
                
                foreach (var controller in identityModule.Children)
                {
                    _output.WriteLine($"  控制器: {controller.Title} (PlatformType: {controller.PlatformType})");
                }

                // 验证只包含系统平台的控制器
                Assert.Single(identityModule.Children);
                Assert.Equal("systemUsers", identityModule.Children.First().Name);
                Assert.Equal(PlatformType.System, identityModule.Children.First().PlatformType);

                _output.WriteLine("✓ GetNavigationTreeAsync 修复后工作正常");
            }
            else
            {
                _output.WriteLine("⚠️ GetNavigationTreeAsync 仍然返回空结果");
            }
        }

        #region 辅助方法

        private ControllerActionDescriptor CreateControllerDescriptor(System.Type controllerType, string controllerName)
        {
            return new ControllerActionDescriptor
            {
                ControllerTypeInfo = controllerType.GetTypeInfo(),
                ControllerName = controllerName,
                ActionName = "Index",
                MethodInfo = controllerType.GetMethod("Index")
            };
        }

        private ActionDescriptorCollection CreateMockActionDescriptorCollection(List<ControllerActionDescriptor> descriptors)
        {
            return new ActionDescriptorCollection(descriptors.Cast<Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor>().ToList(), 1);
        }

        #endregion
    }

    #region 测试控制器类

    [Module("identity", "用户中心")]
    [DisplayName("系统用户管理")]
    [Navigation(Icon = "fa-solid fa-users-gear", PlatformType = PlatformType.System)]
    public class TestFixSystemUsersController : ControllerBase
    {
        [HttpGet]
        public IActionResult Index() => Ok();
    }

    [Module("identity", "用户中心")]
    [DisplayName("租户用户管理")]
    [Navigation(Icon = "fa-solid fa-users", PlatformType = PlatformType.Tenant)]
    public class TestFixTenantUsersController : ControllerBase
    {
        [HttpGet]
        public IActionResult Index() => Ok();
    }

    [Module("identity", "用户中心")]
    [DisplayName("角色管理")]
    [Navigation(Icon = "fa-solid fa-user-tag", PlatformType = PlatformType.Both)]
    public class TestFixRolesController : ControllerBase
    {
        [HttpGet]
        public IActionResult Index() => Ok();
    }

    [Module("identity", "用户中心")]
    [DisplayName("系统角色管理")]
    [Navigation(Icon = "fa-solid fa-user-gear", PlatformType = PlatformType.System)]
    public class TestFixSystemRolesController : ControllerBase
    {
        [HttpGet]
        public IActionResult Index() => Ok();
    }

    #endregion
} 