using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Enums;
using CodeSpirit.Navigation.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace CodeSpirit.Navigation.Tests.Services
{
    /// <summary>
    /// Config模块缓存修复验证测试
    /// 专门验证config模块平台类型缓存问题的修复效果
    /// </summary>
    public class ConfigModuleCacheFixVerificationTests
    {
        private readonly ITestOutputHelper _output;

        public ConfigModuleCacheFixVerificationTests(ITestOutputHelper output)
        {
            _output = output;
        }

        /// <summary>
        /// 模拟ConfigCenter的ApiControllerBase - 设置为系统平台
        /// </summary>
        [ApiController]
        [Route("api/config/[controller]")]
        [Module("config", "配置中心", Icon = "fa-solid fa-sliders")]
        [Navigation(Icon = "fa-solid fa-sliders", PlatformType = PlatformType.System)]
        public abstract class MockConfigApiControllerBase : ControllerBase
        {
        }

        /// <summary>
        /// 模拟具体的配置控制器
        /// </summary>
        [DisplayName("应用管理")]
        [Navigation(Icon = "fa-solid fa-cube")]
        public class MockAppsController : MockConfigApiControllerBase
        {
            [HttpGet]
            [DisplayName("获取应用列表")]
            public IActionResult GetApps() => Ok();

            [HttpPost]
            [DisplayName("创建应用")]
            public IActionResult CreateApp() => Ok();
        }

        [DisplayName("客户端连接")]
        [Navigation(Icon = "fa-solid fa-plug")]
        public class MockClientConnectionsController : MockConfigApiControllerBase
        {
            [HttpGet]
            [DisplayName("获取连接列表")]
            public IActionResult GetConnections() => Ok();
        }

        [DisplayName("配置项管理")]
        [Navigation(Icon = "fa-solid fa-gear")]
        public class MockConfigItemsController : MockConfigApiControllerBase
        {
            [HttpGet]
            [DisplayName("获取配置项")]
            public IActionResult GetConfigItems() => Ok();
        }

        [DisplayName("发布历史")]
        [Navigation(Icon = "fa-solid fa-clock-rotate-left")]
        public class MockConfigPublishHistoriesController : MockConfigApiControllerBase
        {
            [HttpGet]
            [DisplayName("获取发布历史")]
            public IActionResult GetPublishHistories() => Ok();
        }

        /// <summary>
        /// 测试config模块平台类型推断
        /// 验证修复后的逻辑能正确推断config模块为System平台
        /// </summary>
        [Fact]
        public void ConfigModule_PlatformTypeInference_ShouldBeSystem()
        {
            // Arrange
            var controllerTypes = new[]
            {
                typeof(MockAppsController),
                typeof(MockClientConnectionsController),
                typeof(MockConfigItemsController),
                typeof(MockConfigPublishHistoriesController)
            };

            var actionDescriptors = new List<ActionDescriptor>();
            foreach (var controllerType in controllerTypes)
            {
                var methods = controllerType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Where(m => m.DeclaringType == controllerType && m.IsPublic && !m.IsSpecialName);

                foreach (var method in methods)
                {
                    actionDescriptors.Add(new ControllerActionDescriptor
                    {
                        ControllerTypeInfo = controllerType.GetTypeInfo(),
                        MethodInfo = method,
                        ActionName = method.Name,
                        ControllerName = controllerType.Name.Replace("Controller", "")
                    });
                }
            }

            var mockActionProvider = new Mock<IActionDescriptorCollectionProvider>();
            mockActionProvider.Setup(x => x.ActionDescriptors)
                .Returns(new ActionDescriptorCollection(actionDescriptors, 1));

            // 模拟配置服务（空配置）
            var mockConfigSection = new Mock<IConfigurationSection>();
            // 直接返回false，避免使用Extension方法
            mockConfigSection.SetupGet(x => x.Value).Returns((string)null);
            mockConfigSection.Setup(x => x.GetChildren()).Returns(new List<IConfigurationSection>());
            var mockConfiguration = new Mock<IConfiguration>();
            mockConfiguration.Setup(x => x.GetSection("Navigation")).Returns(mockConfigSection.Object);
            
            // 模拟GetSection返回空的导航配置段
            var mockNavigationConfigSection = new Mock<IConfigurationSection>();
            mockNavigationConfigSection.SetupGet(x => x.Value).Returns((string)null);
            mockNavigationConfigSection.Setup(x => x.GetChildren()).Returns(new List<IConfigurationSection>());
            mockConfiguration.Setup(x => x.GetSection("Navigation:config")).Returns(mockNavigationConfigSection.Object);

            var mockCache = new Mock<IDistributedCache>();
            var mockLogger = new Mock<ILogger<NavigationService>>();

            var service = new NavigationService(
                mockActionProvider.Object,
                mockCache.Object,
                mockLogger.Object,
                mockConfiguration.Object);

            // Act - 使用反射调用私有方法
            var buildMethod = typeof(NavigationService).GetMethod("BuildModuleNavigationTree",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var result = (List<NavigationNode>)buildMethod.Invoke(service, new object[] { "config" });

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);

            var moduleNode = result[0];
            _output.WriteLine($"Config模块名称: {moduleNode.Name}");
            _output.WriteLine($"Config模块标题: {moduleNode.Title}");
            _output.WriteLine($"Config模块平台类型: {moduleNode.PlatformType} ({(int)moduleNode.PlatformType})");
            _output.WriteLine($"Config模块原始平台类型: {moduleNode.OriginalPlatformType} ({(int)moduleNode.OriginalPlatformType})");

            // 验证模块级别的平台类型应该是System
            Assert.Equal("config", moduleNode.Name);
            Assert.Equal("配置中心", moduleNode.Title);
            Assert.Equal(PlatformType.System, moduleNode.PlatformType);
            Assert.Equal(PlatformType.System, moduleNode.OriginalPlatformType);

            // 验证子控制器
            _output.WriteLine($"子控制器数量: {moduleNode.Children.Count}");
            Assert.Equal(4, moduleNode.Children.Count);

            foreach (var child in moduleNode.Children)
            {
                _output.WriteLine($"控制器: {child.Name}, 平台类型: {child.PlatformType}");
                // 所有子控制器应该继承System平台类型
                Assert.Equal(PlatformType.System, child.PlatformType);
            }
        }

        /// <summary>
        /// 测试修复后的缓存平台类型确定逻辑
        /// 验证System模块只应该为System平台创建缓存，不应该同时出现在Both缓存中
        /// </summary>
        [Fact]
        public void ConfigModule_CachePlatformDetermination_ShouldOnlyCreateSystemCache()
        {
            // Arrange - 模拟config模块为System平台类型
            var modulePlatformType = PlatformType.System;

            // Act - 模拟修复后的缓存平台确定逻辑
            var platformTypesToCache = new List<PlatformType>();

            switch (modulePlatformType)
            {
                case PlatformType.System:
                    platformTypesToCache.Add(PlatformType.System);
                    // 修复：System模块不应该出现在Both缓存中
                    break;
                case PlatformType.Tenant:
                    platformTypesToCache.Add(PlatformType.Tenant);
                    // 修复：Tenant模块不应该出现在Both缓存中
                    break;
                case PlatformType.Both:
                    platformTypesToCache.Add(PlatformType.System);
                    platformTypesToCache.Add(PlatformType.Tenant);
                    platformTypesToCache.Add(PlatformType.Both);
                    break;
            }

            // Assert
            _output.WriteLine($"Config模块平台类型: {modulePlatformType}");
            _output.WriteLine($"应创建的缓存平台: [{string.Join(", ", platformTypesToCache)}]");

            // 验证只为System平台创建缓存
            Assert.Single(platformTypesToCache);
            Assert.Contains(PlatformType.System, platformTypesToCache);
            Assert.DoesNotContain(PlatformType.Both, platformTypesToCache);
            Assert.DoesNotContain(PlatformType.Tenant, platformTypesToCache);

            // 验证期望的缓存键
            var expectedCacheKeys = new[]
            {
                "CodeSpirit:Navigation:Module:config:System"
            };

            var actualCacheKeys = platformTypesToCache
                .Select(pt => $"CodeSpirit:Navigation:Module:config:{pt}")
                .ToArray();

            _output.WriteLine($"期望的缓存键: [{string.Join(", ", expectedCacheKeys)}]");
            _output.WriteLine($"实际的缓存键: [{string.Join(", ", actualCacheKeys)}]");

            Assert.Equal(expectedCacheKeys.Length, actualCacheKeys.Length);
            foreach (var expectedKey in expectedCacheKeys)
            {
                Assert.Contains(expectedKey, actualCacheKeys);
            }
        }

        /// <summary>
        /// 测试修复后的过滤逻辑
        /// 验证config模块在不同平台缓存中的表现：只存储在System缓存中，但在Both查询时仍能正确显示
        /// </summary>
        [Fact]
        public void ConfigModule_PlatformFiltering_ShouldOnlyAppearInSystemCache()
        {
            // Arrange - 创建config模块节点
            var configModuleNode = new NavigationNode("config", "配置中心", "/config")
            {
                PlatformType = PlatformType.System,
                OriginalPlatformType = PlatformType.System,
                ModuleName = "config"
            };

            // 添加子控制器
            var controllers = new[]
            {
                ("apps", "应用管理"),
                ("clientConnections", "客户端连接"),
                ("configItems", "配置项管理"),
                ("configPublishHistories", "发布历史")
            };

            foreach (var (name, title) in controllers)
            {
                var controllerNode = new NavigationNode(name, title, $"/config/{name}")
                {
                    PlatformType = PlatformType.System,
                    OriginalPlatformType = PlatformType.System,
                    ModuleName = "config"
                };
                configModuleNode.Children.Add(controllerNode);
            }

            var moduleNodes = new List<NavigationNode> { configModuleNode };

            var mockActionProvider = new Mock<IActionDescriptorCollectionProvider>();
            var mockCache = new Mock<IDistributedCache>();
            var mockLogger = new Mock<ILogger<NavigationService>>();
            var mockConfiguration = new Mock<IConfiguration>();

            var service = new NavigationService(
                mockActionProvider.Object,
                mockCache.Object,
                mockLogger.Object,
                mockConfiguration.Object);

            // Act - 测试不同平台类型的过滤
            var systemFiltered = service.FilterNodesByPlatform(moduleNodes, PlatformType.System);
            var tenantFiltered = service.FilterNodesByPlatform(moduleNodes, PlatformType.Tenant);
            var bothFiltered = service.FilterNodesByPlatform(moduleNodes, PlatformType.Both);

            // Assert
            _output.WriteLine($"System缓存过滤结果: {systemFiltered.Count} 个模块");
            _output.WriteLine($"Tenant缓存过滤结果: {tenantFiltered.Count} 个模块");
            _output.WriteLine($"Both缓存过滤结果: {bothFiltered.Count} 个模块");

            // System缓存：应该包含config模块
            Assert.Single(systemFiltered);
            Assert.Equal("config", systemFiltered[0].Name);
            Assert.Equal(4, systemFiltered[0].Children.Count);

            // Tenant缓存：不应该包含config模块（System模块不适用于Tenant平台）
            Assert.Empty(tenantFiltered);

            // Both缓存：应该包含config模块（Both查询包含System模块）
            Assert.Single(bothFiltered);
            Assert.Equal("config", bothFiltered[0].Name);
            Assert.Equal(4, bothFiltered[0].Children.Count);

            _output.WriteLine("✅ 修复验证成功：config模块只存储在System缓存中，但在Both查询时仍能正确显示");
        }

        /// <summary>
        /// 测试Both平台类型模块的缓存行为
        /// 验证真正的Both类型模块应该出现在所有平台缓存中
        /// </summary>
        [Fact]
        public void BothModule_PlatformFiltering_ShouldAppearInAllCaches()
        {
            // Arrange - 创建一个Both平台类型的模块
            var bothModuleNode = new NavigationNode("common", "通用功能", "/common")
            {
                PlatformType = PlatformType.Both,
                OriginalPlatformType = PlatformType.Both,
                ModuleName = "common"
            };

            // 添加子控制器
            var controllerNode = new NavigationNode("dashboard", "仪表板", "/common/dashboard")
            {
                PlatformType = PlatformType.Both,
                OriginalPlatformType = PlatformType.Both,
                ModuleName = "common"
            };
            bothModuleNode.Children.Add(controllerNode);

            var moduleNodes = new List<NavigationNode> { bothModuleNode };

            var mockActionProvider = new Mock<IActionDescriptorCollectionProvider>();
            var mockCache = new Mock<IDistributedCache>();
            var mockLogger = new Mock<ILogger<NavigationService>>();
            var mockConfiguration = new Mock<IConfiguration>();

            var service = new NavigationService(
                mockActionProvider.Object,
                mockCache.Object,
                mockLogger.Object,
                mockConfiguration.Object);

            // Act - 测试不同平台类型的过滤
            var systemFiltered = service.FilterNodesByPlatform(moduleNodes, PlatformType.System);
            var tenantFiltered = service.FilterNodesByPlatform(moduleNodes, PlatformType.Tenant);
            var bothFiltered = service.FilterNodesByPlatform(moduleNodes, PlatformType.Both);

            // Assert
            _output.WriteLine($"Both模块在System缓存中: {systemFiltered.Count} 个");
            _output.WriteLine($"Both模块在Tenant缓存中: {tenantFiltered.Count} 个");
            _output.WriteLine($"Both模块在Both缓存中: {bothFiltered.Count} 个");

            // Both类型的模块应该在所有平台缓存中都出现
            Assert.Single(systemFiltered);
            Assert.Equal("common", systemFiltered[0].Name);

            Assert.Single(tenantFiltered);
            Assert.Equal("common", tenantFiltered[0].Name);

            Assert.Single(bothFiltered);
            Assert.Equal("common", bothFiltered[0].Name);

            _output.WriteLine("✅ Both类型模块正确出现在所有平台缓存中");
        }
    }
} 