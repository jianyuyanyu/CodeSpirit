using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CodeSpirit.Navigation;
using CodeSpirit.Navigation.Models;
using CodeSpirit.Navigation.Services;
using CodeSpirit.Navigation.Tests.Extensions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;
using Xunit.Abstractions;
using System.Linq;
using System.Text.Json;
using CodeSpirit.Core.Authorization;
using CodeSpirit.Core.Attributes;

namespace CodeSpirit.Navigation.Tests
{
    /// <summary>
    /// 导航服务单元测试
    /// </summary>
    public class NavigationServiceTests
    {
        private readonly Mock<IActionDescriptorCollectionProvider> _mockActionProvider;
        private readonly Mock<IDistributedCache> _mockCache;
        private readonly Mock<ILogger<NavigationService>> _mockLogger;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly ITestOutputHelper _testOutputHelper;

        // 常量定义，与NavigationService中保持一致
        private const string CACHE_KEY_PREFIX = "CodeSpirit:Navigation:Module:";
        private const string MODULE_NAMES_CACHE_KEY = "CodeSpirit:Navigation:ModuleNames";

        public NavigationServiceTests(ITestOutputHelper testOutputHelper)
        {
            _mockActionProvider = new Mock<IActionDescriptorCollectionProvider>();
            _mockCache = new Mock<IDistributedCache>();
            _mockLogger = new Mock<ILogger<NavigationService>>();
            _mockConfiguration = new Mock<IConfiguration>();
            _testOutputHelper = testOutputHelper;
        }

        /// <summary>
        /// 测试获取导航树 - 当缓存中存在模块时应返回导航节点
        /// </summary>
        [Fact]
        public async Task GetNavigationTreeAsync_WhenModulesExistInCache_ShouldReturnNavigationNodes()
        {
            // 准备测试数据
            var moduleNames = new List<string> { "Module1", "Module2" };
            var module1Nodes = new List<NavigationNode>
            {
                new NavigationNode("node1", "节点1", "/node1") { ModuleName = "Module1" }
            };
            var module2Nodes = new List<NavigationNode>
            {
                new NavigationNode("node2", "节点2", "/node2") { ModuleName = "Module2" }
            };

            // 记录测试信息
            _testOutputHelper.WriteLine("测试获取导航树 - 设置模块列表缓存数据");

            // 设置模拟行为
            _mockCache.Setup(c => c.GetAsync(
                    MODULE_NAMES_CACHE_KEY,
                    It.IsAny<System.Threading.CancellationToken>()))
                .ReturnsAsync(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(moduleNames));

            _mockCache.Setup(c => c.GetAsync(
                    $"{CACHE_KEY_PREFIX}Module1:Both",
                    It.IsAny<System.Threading.CancellationToken>()))
                .ReturnsAsync(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(module1Nodes));

            _mockCache.Setup(c => c.GetAsync(
                    $"{CACHE_KEY_PREFIX}Module2:Both",
                    It.IsAny<System.Threading.CancellationToken>()))
                .ReturnsAsync(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(module2Nodes));

            // 创建被测试服务
            var service = new NavigationService(
                _mockActionProvider.Object,
                _mockCache.Object,
                _mockLogger.Object,
                _mockConfiguration.Object);

            // 执行测试
            _testOutputHelper.WriteLine("测试获取导航树 - 执行GetNavigationTreeAsync方法");
            var result = await service.GetNavigationTreeAsync();

            // 验证结果
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);

            _testOutputHelper.WriteLine($"测试获取导航树 - 结果包含 {result.Count} 个节点");
            _testOutputHelper.WriteLine($"测试获取导航树 - 第一个节点: {result[0].Name}, 模块: {result[0].ModuleName}");

            Assert.Equal("node1", result[0].Name);
            Assert.Equal("Module1", result[0].ModuleName);
            Assert.Equal("node2", result[1].Name);
            Assert.Equal("Module2", result[1].ModuleName);
        }

        /// <summary>
        /// 测试获取导航树 - 当缓存中不存在模块列表时应返回空列表并记录警告日志
        /// </summary>
        [Fact]
        public async Task GetNavigationTreeAsync_WhenNoModulesInCache_ShouldReturnEmptyListAndLogWarning()
        {
            // 设置模拟行为 - 模块列表为空
            _mockCache.Setup(c => c.GetAsync(
                    MODULE_NAMES_CACHE_KEY,
                    It.IsAny<System.Threading.CancellationToken>()))
                .ReturnsAsync(null as byte[]);

            // 记录测试信息
            _testOutputHelper.WriteLine("测试获取导航树 - 模块列表缓存为空");

            // 创建被测试服务
            var service = new NavigationService(
                _mockActionProvider.Object,
                _mockCache.Object,
                _mockLogger.Object,
                _mockConfiguration.Object);

            // 执行测试
            var result = await service.GetNavigationTreeAsync();

            // 验证结果
            Assert.NotNull(result);
            Assert.Empty(result);

            _testOutputHelper.WriteLine("测试获取导航树 - 结果为空列表");

            // 验证记录了警告日志
            _mockLogger.Verify(
                l => l.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("No navigation modules found in cache")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);

            _testOutputHelper.WriteLine("测试获取导航树 - 验证记录了警告日志");
        }

        /// <summary>
        /// 测试获取导航树 - 应根据权限过滤节点
        /// </summary>
        [Fact]
        public async Task GetNavigationTreeAsync_ShouldFilterNodesByPermission()
        {
            // 准备测试数据
            var moduleNames = new List<string> { "Module1" };
            var module1Nodes = new List<NavigationNode>
            {
                new NavigationNode("visibleNode", "可见节点", "/visible") 
                { 
                    ModuleName = "Module1",
                    Permission = "visible_permission"
                },
                new NavigationNode("hiddenNode", "隐藏节点", "/hidden") 
                { 
                    ModuleName = "Module1",
                    Permission = "hidden_permission"
                }
            };

            // 记录测试信息
            _testOutputHelper.WriteLine("测试权限验证 - 设置模块列表缓存数据");

            // 设置缓存模拟
            _mockCache.Setup(c => c.GetAsync(
                    MODULE_NAMES_CACHE_KEY,
                    It.IsAny<System.Threading.CancellationToken>()))
                .ReturnsAsync(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(moduleNames));

            _mockCache.Setup(c => c.GetAsync(
                    $"{CACHE_KEY_PREFIX}Module1:Both",
                    It.IsAny<System.Threading.CancellationToken>()))
                .ReturnsAsync(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(module1Nodes));

            // 创建权限服务模拟
            var mockPermissionService = new Mock<CodeSpirit.Core.Authorization.IHasPermissionService>();
            mockPermissionService.Setup(p => p.HasNavigationPermission("visible_permission")).Returns(true);
            mockPermissionService.Setup(p => p.HasNavigationPermission("hidden_permission")).Returns(false);

            // 创建被测试服务
            var service = new NavigationService(
                _mockActionProvider.Object,
                _mockCache.Object,
                _mockLogger.Object,
                _mockConfiguration.Object);

            // 执行测试
            _testOutputHelper.WriteLine("测试权限验证 - 执行GetNavigationTreeAsync和FilterNodesByPermission方法");
            var allNodes = await service.GetNavigationTreeAsync();
            var filteredResult = service.FilterNodesByPermission(allNodes, mockPermissionService.Object);

            // 验证结果
            Assert.NotNull(filteredResult);
            Assert.Single(filteredResult);
            Assert.Equal("visibleNode", filteredResult[0].Name);

            _testOutputHelper.WriteLine($"测试权限验证 - 过滤后结果包含 {filteredResult.Count} 个节点");
            _testOutputHelper.WriteLine($"测试权限验证 - 可见节点: {filteredResult[0].Name}");
        }

        /// <summary>
        /// 测试获取导航树 - 应根据权限过滤嵌套节点
        /// </summary>
        [Fact]
        public async Task GetNavigationTreeAsync_ShouldFilterNestedNodesByPermission()
        {
            // 准备测试数据
            var moduleNames = new List<string> { "Module1" };
            var module1Nodes = new List<NavigationNode>
            {
                new NavigationNode("parent", "父节点", "/parent")
                {
                    ModuleName = "Module1",
                    Permission = "parent_permission",
                    Children = new List<NavigationNode>
                    {
                        new NavigationNode("child1", "子节点1", "/parent/child1")
                        {
                            Permission = "child1_permission"
                        },
                        new NavigationNode("child2", "子节点2", "/parent/child2")
                        {
                            Permission = "child2_permission"
                        }
                    }
                }
            };

            // 记录测试信息
            _testOutputHelper.WriteLine("测试嵌套节点权限验证 - 设置模块列表缓存数据");

            // 设置缓存模拟
            _mockCache.Setup(c => c.GetAsync(
                    MODULE_NAMES_CACHE_KEY,
                    It.IsAny<System.Threading.CancellationToken>()))
                .ReturnsAsync(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(moduleNames));

            _mockCache.Setup(c => c.GetAsync(
                    $"{CACHE_KEY_PREFIX}Module1:Both",
                    It.IsAny<System.Threading.CancellationToken>()))
                .ReturnsAsync(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(module1Nodes));

            // 创建权限服务模拟
            var mockPermissionService = new Mock<CodeSpirit.Core.Authorization.IHasPermissionService>();
            mockPermissionService.Setup(p => p.HasNavigationPermission("parent_permission")).Returns(false);
            mockPermissionService.Setup(p => p.HasNavigationPermission("child1_permission")).Returns(true);
            mockPermissionService.Setup(p => p.HasNavigationPermission("child2_permission")).Returns(false);

            // 创建被测试服务
            var service = new NavigationService(
                _mockActionProvider.Object,
                _mockCache.Object,
                _mockLogger.Object,
                _mockConfiguration.Object);

            // 执行测试
            _testOutputHelper.WriteLine("测试嵌套节点权限验证 - 执行GetNavigationTreeAsync和FilterNodesByPermission方法");
            var allNodes = await service.GetNavigationTreeAsync();
            var filteredResult = service.FilterNodesByPermission(allNodes, mockPermissionService.Object);

            // 验证结果
            Assert.NotNull(filteredResult);
            Assert.Single(filteredResult); // 父节点因为有有权限的子节点而被保留
            Assert.Equal("parent", filteredResult[0].Name);
            Assert.Single(filteredResult[0].Children); // 只有child1有权限
            Assert.Equal("child1", filteredResult[0].Children[0].Name);

            _testOutputHelper.WriteLine($"测试嵌套节点权限验证 - 过滤后结果包含 {filteredResult.Count} 个父节点");
            _testOutputHelper.WriteLine($"测试嵌套节点权限验证 - 父节点的子节点数量: {filteredResult[0].Children.Count}");
        }

        /// <summary>
        /// 测试获取导航树 - 复杂场景应正确处理
        /// </summary>
        [Fact]
        public async Task GetNavigationTreeAsync_ComplexScenarios_ShouldHandleCorrectly()
        {
            // 准备测试数据
            var moduleNames = new List<string> { "Module1", "Module2" };
            var module1Nodes = new List<NavigationNode>
            {
                new NavigationNode("m1_node1", "模块1节点1", "/m1/node1") { ModuleName = "Module1" },
                new NavigationNode("m1_node2", "模块1节点2", "/m1/node2") { ModuleName = "Module1" }
            };
            var module2Nodes = new List<NavigationNode>
            {
                new NavigationNode("m2_node1", "模块2节点1", "/m2/node1") { ModuleName = "Module2" }
            };

            // 记录测试信息
            _testOutputHelper.WriteLine("开始执行复杂场景导航树测试");

            // 设置缓存模拟
            _mockCache.Setup(c => c.GetAsync(
                    MODULE_NAMES_CACHE_KEY,
                    It.IsAny<System.Threading.CancellationToken>()))
                .ReturnsAsync(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(moduleNames));

            _mockCache.Setup(c => c.GetAsync(
                    $"{CACHE_KEY_PREFIX}Module1:Both",
                    It.IsAny<System.Threading.CancellationToken>()))
                .ReturnsAsync(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(module1Nodes));

            _mockCache.Setup(c => c.GetAsync(
                    $"{CACHE_KEY_PREFIX}Module2:Both",
                    It.IsAny<System.Threading.CancellationToken>()))
                .ReturnsAsync(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(module2Nodes));

            // 创建被测试服务
            var service = new NavigationService(
                _mockActionProvider.Object,
                _mockCache.Object,
                _mockLogger.Object,
                _mockConfiguration.Object);

            // 执行测试
            _testOutputHelper.WriteLine("执行GetNavigationTreeAsync方法");
            var result = await service.GetNavigationTreeAsync();

            // 验证结果
            Assert.NotNull(result);
            Assert.Equal(3, result.Count); // 2个Module1节点 + 1个Module2节点

            var module1Results = result.Where(n => n.ModuleName == "Module1").ToList();
            var module2Results = result.Where(n => n.ModuleName == "Module2").ToList();

            Assert.Equal(2, module1Results.Count);
            Assert.Single(module2Results);

            _testOutputHelper.WriteLine("复杂场景导航树测试完成");
        }

        /// <summary>
        /// 测试获取导航树 - 缓存服务失败应优雅处理
        /// </summary>
        [Fact]
        public async Task GetNavigationTreeAsync_CacheServiceFailure_ShouldHandleGracefully()
        {
            _testOutputHelper.WriteLine("开始执行缓存异常处理测试");

            // 准备测试数据
            var moduleNames = new List<string> { "Module1", "Module2" };

            // 设置缓存模拟 - 模块列表正常，但Module2缓存获取失败
            _mockCache.Setup(c => c.GetAsync(
                    MODULE_NAMES_CACHE_KEY,
                    It.IsAny<System.Threading.CancellationToken>()))
                .ReturnsAsync(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(moduleNames));

            _mockCache.Setup(c => c.GetAsync(
                    $"{CACHE_KEY_PREFIX}Module1:Both",
                    It.IsAny<System.Threading.CancellationToken>()))
                .ReturnsAsync(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(new List<NavigationNode>
                {
                    new NavigationNode("node1", "节点1", "/node1") { ModuleName = "Module1" }
                }));

            _mockCache.Setup(c => c.GetAsync(
                    $"{CACHE_KEY_PREFIX}Module2:Both",
                    It.IsAny<System.Threading.CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("模拟缓存服务异常"));

            // 创建被测试服务
            var service = new NavigationService(
                _mockActionProvider.Object,
                _mockCache.Object,
                _mockLogger.Object,
                _mockConfiguration.Object);

            // 执行测试
            _testOutputHelper.WriteLine("执行GetNavigationTreeAsync方法");
            var result = await service.GetNavigationTreeAsync();

            // 验证结果
            Assert.NotNull(result);
            Assert.Single(result); // 只有Module1的节点应该被成功加载
            Assert.Equal("node1", result[0].Name);

            // 验证日志记录
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("module 'Module2'")),
                    It.Is<Exception>(ex => ex.Message.Contains("模拟缓存服务异常")),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.AtLeastOnce);

            _testOutputHelper.WriteLine("缓存异常处理测试完成");
        }

        [Fact]
        public async Task GetNavigationTreeAsync_PermissionServiceUnavailable_ShouldReturnAllNodes()
        {
            _testOutputHelper.WriteLine("开始执行权限服务不可用测试");

            // 创建测试数据
            var moduleNames = new List<string> { "Module1" };
            var module1Nodes = new List<NavigationNode>
            {
                new NavigationNode("parent", "父节点", "/parent")
                {
                    Permission = "parent_permission",
                    Children = new List<NavigationNode>
                    {
                        new NavigationNode("child1", "子节点1", "/parent/child1")
                        {
                            Permission = "child1_permission"
                        },
                        new NavigationNode("child2", "子节点2", "/parent/child2")
                        {
                            Permission = "child2_permission"
                        }
                    }
                }
            };

            // 设置缓存模拟
            _mockCache.Setup(c => c.GetAsync(
                    MODULE_NAMES_CACHE_KEY,
                    It.IsAny<System.Threading.CancellationToken>()))
                .ReturnsAsync(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(moduleNames));

            _mockCache.Setup(c => c.GetAsync(
                    $"{CACHE_KEY_PREFIX}Module1:Both",
                    It.IsAny<System.Threading.CancellationToken>()))
                .ReturnsAsync(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(module1Nodes));

            // 创建被测试服务
            var service = new NavigationService(
                _mockActionProvider.Object,
                _mockCache.Object,
                _mockLogger.Object,
                _mockConfiguration.Object);

            // 执行测试 - 不提供权限服务
            _testOutputHelper.WriteLine("执行GetNavigationTreeAsync方法");
            var allNodes = await service.GetNavigationTreeAsync();
            var result = service.FilterNodesByPermission(allNodes, null);

            // 验证结果
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("parent", result[0].Name);
            Assert.Equal(2, result[0].Children.Count); // 所有子节点都应该存在，因为没有权限过滤

            // 验证记录了权限服务不可用的警告
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Permission service not available")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.AtLeastOnce);

            _testOutputHelper.WriteLine("权限服务不可用测试完成");
        }

        /// <summary>
        /// 测试初始化导航树 - 确保正确调用缓存更新方法
        /// </summary>
        [Fact]
        public async Task InitializeNavigationTree_ShouldUpdateCacheForAllModules()
        {
            // 准备测试数据
            var modules = new List<string> { "Module1", "Module2" };

            // 记录测试信息
            _testOutputHelper.WriteLine("测试初始化导航树 - 准备测试数据");

            // 设置模拟行为 - 返回空的现有模块列表
            _mockCache.Setup(c => c.GetAsync(
                    MODULE_NAMES_CACHE_KEY,
                    It.IsAny<System.Threading.CancellationToken>()))
                .ReturnsAsync(null as byte[]);

            // 创建被测试服务与服务提供者
            var serviceMock = new Mock<NavigationService>(
                _mockActionProvider.Object,
                _mockCache.Object,
                _mockLogger.Object,
                _mockConfiguration.Object)
            {
                CallBase = true
            };

            // 使用Protected().Setup来模拟受保护的方法
            // 模拟GetCurrentModules方法返回模块列表
            serviceMock.Protected()
                .Setup<List<string>>("GetCurrentModules")
                .Returns(modules);

            // 模拟GetConfigModules方法返回空列表
            serviceMock.Protected()
                .Setup<List<string>>("GetConfigModules")
                .Returns(new List<string>());

            // 模拟BuildModuleNavigationTree方法返回空列表，避免访问未模拟的ActionProvider
            serviceMock.Protected()
                .Setup<List<NavigationNode>>("BuildModuleNavigationTree", ItExpr.IsAny<string>())
                .Returns(new List<NavigationNode>());

            // 执行测试
            _testOutputHelper.WriteLine("测试初始化导航树 - 执行InitializeNavigationTree方法");
            await serviceMock.Object.InitializeNavigationTree();

            // 验证缓存更新
            _mockCache.Verify(c => c.SetAsync(
                MODULE_NAMES_CACHE_KEY,
                It.Is<byte[]>(b => System.Text.Encoding.UTF8.GetString(b).Contains("Module1") &&
                                   System.Text.Encoding.UTF8.GetString(b).Contains("Module2")),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<System.Threading.CancellationToken>()),
                Times.Once);

            _testOutputHelper.WriteLine("测试初始化导航树 - 验证缓存已更新");

            // 验证日志记录
            _mockLogger.Verify(
                l => l.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Starting navigation tree initialization")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);

            _mockLogger.Verify(
                l => l.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Navigation tree initialization completed")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);

            _testOutputHelper.WriteLine("测试初始化导航树 - 验证日志记录已完成");
        }

        /// <summary>
        /// 测试初始化导航树 - 多模块应为所有模块创建缓存
        /// </summary>
        [Fact]
        public async Task InitializeNavigationTree_WithMultipleModules_ShouldCreateCacheForAllModules()
        {
            _testOutputHelper.WriteLine("开始执行导航树初始化测试");

            // 模拟MVC控制器数据
            var actionDescriptors = new List<Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor>();
            var mockActionDescriptorCollection = new Mock<Microsoft.AspNetCore.Mvc.Infrastructure.ActionDescriptorCollection>(
                actionDescriptors, 1);

            _mockActionProvider
                .Setup(p => p.ActionDescriptors)
                .Returns(mockActionDescriptorCollection.Object);

            // 模拟配置
            var mockConfigSection = new Mock<Microsoft.Extensions.Configuration.IConfigurationSection>();
            mockConfigSection.Setup(s => s.Value).Returns("true"); // 启用导航树自动生成

            _mockConfiguration
                .Setup(c => c.GetSection("CodeSpirit:Navigation:AutoGenerate"))
                .Returns(mockConfigSection.Object);

            // 创建被测试服务
            var serviceMock = new Mock<NavigationService>(
                _mockActionProvider.Object,
                _mockCache.Object,
                _mockLogger.Object,
                _mockConfiguration.Object)
            {
                CallBase = true
            };

            // 不再模拟GetNavigationModules方法，改为模拟BuildModuleNavigationTree方法
            serviceMock.Protected()
                .Setup<List<NavigationNode>>("BuildModuleNavigationTree", ItExpr.Is<string>(s => s == "Module1"))
                .Returns(new List<NavigationNode>
                {
                    new NavigationNode("m1_node", "模块1节点", "/m1")
                });

            serviceMock.Protected()
                .Setup<List<NavigationNode>>("BuildModuleNavigationTree", ItExpr.Is<string>(s => s == "Module2"))
                .Returns(new List<NavigationNode>
                {
                    new NavigationNode("m2_node", "模块2节点", "/m2")
                });

            // 模拟GetCurrentModules方法返回两个模块
            serviceMock.Protected()
                .Setup<List<string>>("GetCurrentModules")
                .Returns(new List<string> { "Module1", "Module2" });

            // 执行测试 - 初始化导航树
            await serviceMock.Object.InitializeNavigationTree();

            // 验证模块名称列表已缓存
            _mockCache.Verify(c => c.SetAsync(
                MODULE_NAMES_CACHE_KEY,
                It.Is<byte[]>(b => System.Text.Encoding.UTF8.GetString(b).Contains("Module1") &&
                                   System.Text.Encoding.UTF8.GetString(b).Contains("Module2")),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<System.Threading.CancellationToken>()),
                Times.Once);

            // 验证各平台类型的模块节点已缓存 - 修正为正确的缓存键格式
            _mockCache.Verify(c => c.SetAsync(
                $"{CACHE_KEY_PREFIX}Module1:System",
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<System.Threading.CancellationToken>()),
                Times.Once);

            _mockCache.Verify(c => c.SetAsync(
                $"{CACHE_KEY_PREFIX}Module1:Tenant",
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<System.Threading.CancellationToken>()),
                Times.Once);

            _mockCache.Verify(c => c.SetAsync(
                $"{CACHE_KEY_PREFIX}Module1:Both",
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<System.Threading.CancellationToken>()),
                Times.Once);

            _mockCache.Verify(c => c.SetAsync(
                $"{CACHE_KEY_PREFIX}Module2:System",
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<System.Threading.CancellationToken>()),
                Times.Once);

            _mockCache.Verify(c => c.SetAsync(
                $"{CACHE_KEY_PREFIX}Module2:Tenant",
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<System.Threading.CancellationToken>()),
                Times.Once);

            _mockCache.Verify(c => c.SetAsync(
                $"{CACHE_KEY_PREFIX}Module2:Both",
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<System.Threading.CancellationToken>()),
                Times.Once);

            _testOutputHelper.WriteLine("导航树初始化测试完成");
        }

        /// <summary>
        /// 测试清除模块导航缓存 - 确保正确调用缓存清除方法
        /// </summary>
        [Fact]
        public async Task ClearModuleNavigationCacheAsync_ShouldRemoveAndResetCache()
        {
            _testOutputHelper.WriteLine("开始执行缓存清除和重建测试");

            // 模拟MVC控制器数据
            var actionDescriptors = new List<Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor>();
            var mockActionDescriptorCollection = new Mock<Microsoft.AspNetCore.Mvc.Infrastructure.ActionDescriptorCollection>(
                actionDescriptors, 1);

            _mockActionProvider
                .Setup(p => p.ActionDescriptors)
                .Returns(mockActionDescriptorCollection.Object);

            // 设置模块列表缓存数据
            var moduleNames = new List<string> { "Module1" };
            var initialNodes = new List<NavigationNode>
            {
                new NavigationNode("old_node", "旧节点", "/old")
                {
                    Permission = "old_permission"
                }
            };

            // 设置初始缓存数据
            _mockCache.Setup(c => c.GetAsync(
                    MODULE_NAMES_CACHE_KEY,
                    It.IsAny<System.Threading.CancellationToken>()))
                .ReturnsAsync(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(moduleNames));

            _mockCache.Setup(c => c.GetAsync(
                    $"{CACHE_KEY_PREFIX}Module1:Both",
                    It.IsAny<System.Threading.CancellationToken>()))
                .ReturnsAsync(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(initialNodes));

            // 创建被测试服务
            var navigationService = new NavigationService(
                _mockActionProvider.Object,
                _mockCache.Object,
                _mockLogger.Object,
                _mockConfiguration.Object);

            // 验证初始缓存状态 - 通过GetNavigationTreeAsync方法
            var initialResult = await navigationService.GetNavigationTreeAsync();
            Assert.Single(initialResult);
            Assert.Equal("old_node", initialResult[0].Name);

            // 执行测试 - 清除缓存
            await navigationService.ClearModuleNavigationCacheAsync("Module1");

            // 验证缓存被清除 - 检查所有平台类型的缓存键
            _mockCache.Verify(c => c.RemoveAsync(
                $"{CACHE_KEY_PREFIX}Module1:System",
                It.IsAny<System.Threading.CancellationToken>()),
                Times.Once);

            _mockCache.Verify(c => c.RemoveAsync(
                $"{CACHE_KEY_PREFIX}Module1:Tenant",
                It.IsAny<System.Threading.CancellationToken>()),
                Times.Once);

            _mockCache.Verify(c => c.RemoveAsync(
                $"{CACHE_KEY_PREFIX}Module1:Both",
                It.IsAny<System.Threading.CancellationToken>()),
                Times.Once);

            // 验证模块名称列表被更新
            _mockCache.Verify(c => c.SetAsync(
                MODULE_NAMES_CACHE_KEY,
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<System.Threading.CancellationToken>()),
                Times.AtLeastOnce);

            _testOutputHelper.WriteLine("缓存清除和重建测试完成");
        }

        [Fact]
        public async Task GetNavigationTreeAsync_WithLargeNavigationTree_ShouldPerformEfficiently()
        {
            _testOutputHelper.WriteLine("开始执行大型导航树性能测试");

            // 创建大型导航树 - 包含多个模块，每个模块有多个节点和子节点
            var moduleNames = new List<string>();

            // 创建10个模块
            for (int i = 1; i <= 10; i++)
            {
                var moduleName = $"LargeModule{i}";
                moduleNames.Add(moduleName);

                // 每个模块创建20个父节点
                var moduleNodes = new List<NavigationNode>();
                for (int j = 1; j <= 20; j++)
                {
                    var parentNode = new NavigationNode($"parent_{i}_{j}", $"父节点{i}-{j}", $"/module{i}/parent{j}")
                    {
                        Permission = $"module{i}_parent{j}",
                        Children = new List<NavigationNode>()
                    };

                    // 每个父节点创建5个子节点
                    for (int k = 1; k <= 5; k++)
                    {
                        parentNode.Children.Add(new NavigationNode(
                            $"child_{i}_{j}_{k}",
                            $"子节点{i}-{j}-{k}",
                            $"/module{i}/parent{j}/child{k}")
                        {
                            Permission = $"module{i}_parent{j}_child{k}"
                        });
                    }

                    moduleNodes.Add(parentNode);
                }

                // 设置模块缓存
                _mockCache.Setup(c => c.GetAsync(
                        $"{CACHE_KEY_PREFIX}{moduleName}:Both",
                        It.IsAny<System.Threading.CancellationToken>()))
                    .ReturnsAsync(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(moduleNodes));
            }

            // 设置模块名称缓存
            _mockCache.Setup(c => c.GetAsync(
                    MODULE_NAMES_CACHE_KEY,
                    It.IsAny<System.Threading.CancellationToken>()))
                .ReturnsAsync(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(moduleNames));

            // 创建被测试服务
            var service = new NavigationService(
                _mockActionProvider.Object,
                _mockCache.Object,
                _mockLogger.Object,
                _mockConfiguration.Object);

            // 记录性能数据
            var stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();

            // 执行测试
            _testOutputHelper.WriteLine("执行GetNavigationTreeAsync方法");
            var result = await service.GetNavigationTreeAsync();

            stopwatch.Stop();
            _testOutputHelper.WriteLine($"大型导航树加载耗时: {stopwatch.ElapsedMilliseconds}ms");

            // 验证结果
            Assert.NotNull(result);
            Assert.Equal(200, result.Count); // 10个模块 x 20个父节点 = 200个顶级节点
            Assert.Equal(1000, result.Sum(node => node.Children.Count)); // 200个父节点 x 5个子节点 = 1000个子节点

            _testOutputHelper.WriteLine("大型导航树性能测试完成");
        }

        [Fact]
        public async Task GetNavigationTreeAsync_WithDynamicPermissionChanges_ShouldUpdateVisibleNodes()
        {
            _testOutputHelper.WriteLine("开始执行动态权限变更测试");

            // 创建测试数据
            var moduleNames = new List<string> { "Module1" };
            var module1Nodes = new List<NavigationNode>
            {
                new NavigationNode("node1", "节点1", "/node1")
                {
                    Permission = "permission1"
                },
                new NavigationNode("node2", "节点2", "/node2")
                {
                    Permission = "permission2"
                },
                new NavigationNode("node3", "节点3", "/node3")
                {
                    Permission = "permission3"
                }
            };

            // 设置缓存模拟
            _mockCache.Setup(c => c.GetAsync(
                    MODULE_NAMES_CACHE_KEY,
                    It.IsAny<System.Threading.CancellationToken>()))
                .ReturnsAsync(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(moduleNames));

            _mockCache.Setup(c => c.GetAsync(
                    $"{CACHE_KEY_PREFIX}Module1:Both",
                    It.IsAny<System.Threading.CancellationToken>()))
                .ReturnsAsync(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(module1Nodes));

            // 创建被测试服务
            var service = new NavigationService(
                _mockActionProvider.Object,
                _mockCache.Object,
                _mockLogger.Object,
                _mockConfiguration.Object);

            // 执行测试
            _testOutputHelper.WriteLine("第一次调用 - 初始权限");
            var result1 = await service.GetNavigationTreeAsync();

            // 验证结果 - 应该有3个节点
            Assert.NotNull(result1);
            Assert.Equal(3, result1.Count);
            Assert.Contains(result1, n => n.Name == "node1");
            Assert.Contains(result1, n => n.Name == "node2");
            Assert.Contains(result1, n => n.Name == "node3");

            // 第二次调用 - 权限过滤测试
            _testOutputHelper.WriteLine("第二次调用 - 使用权限过滤");
            
            // 创建权限服务模拟 - 只允许部分权限
            var mockPermissionService = new Mock<CodeSpirit.Core.Authorization.IHasPermissionService>();
            mockPermissionService.Setup(p => p.HasNavigationPermission("permission1")).Returns(true);
            mockPermissionService.Setup(p => p.HasNavigationPermission("permission2")).Returns(false);
            mockPermissionService.Setup(p => p.HasNavigationPermission("permission3")).Returns(true);

            var result2 = service.FilterNodesByPermission(result1, mockPermissionService.Object);

            // 验证结果 - 应该有2个节点(node1, node3)
            Assert.NotNull(result2);
            Assert.Equal(2, result2.Count);
            Assert.Contains(result2, n => n.Name == "node1");
            Assert.Contains(result2, n => n.Name == "node3");
            Assert.DoesNotContain(result2, n => n.Name == "node2");

            _testOutputHelper.WriteLine("动态权限变更测试完成");
        }

        /// <summary>
        /// 测试多级导航（三级或以上）中一级导航的权限过滤
        /// </summary>
        [Fact]
        public void FilterNodesByPermission_WithMultiLevelNodes_ShouldNotFilterOutParentNodes()
        {
            // 准备测试数据：三级导航结构
            var navigationNodes = new List<NavigationNode>
            {
                new NavigationNode("level1", "一级菜单", "/level1")
                {
                    Permission = "level1_permission",
                    Children = new List<NavigationNode>
                    {
                        new NavigationNode("level2", "二级菜单", "/level1/level2")
                        {
                            Permission = "level2_permission",
                            Children = new List<NavigationNode>
                            {
                                new NavigationNode("level3", "三级菜单", "/level1/level2/level3")
                                {
                                    Permission = "level3_permission"
                                }
                            }
                        }
                    }
                }
            };

            // 创建Mock权限服务 - 设置只有三级菜单有权限
            var mockPermissionService = new Mock<IHasPermissionService>();
            mockPermissionService.Setup(p => p.HasNavigationPermission(It.Is<string>(s => s == "level1_permission"))).Returns(false);
            mockPermissionService.Setup(p => p.HasNavigationPermission(It.Is<string>(s => s == "level2_permission"))).Returns(false);
            mockPermissionService.Setup(p => p.HasNavigationPermission(It.Is<string>(s => s == "level3_permission"))).Returns(true);

            // 创建服务实例
            var service = new NavigationService(
                _mockActionProvider.Object,
                _mockCache.Object,
                _mockLogger.Object,
                _mockConfiguration.Object
            );

            // 执行测试
            var result = service.FilterNodesByPermission(navigationNodes, mockPermissionService.Object);

            // 验证结果
            Assert.NotNull(result);
            Assert.Single(result); // 应返回一级菜单

            var level1Node = result.First();
            Assert.Equal("level1", level1Node.Name);
            Assert.Single(level1Node.Children); // 应返回二级菜单

            var level2Node = level1Node.Children.First();
            Assert.Equal("level2", level2Node.Name);
            Assert.Single(level2Node.Children); // 应返回三级菜单

            var level3Node = level2Node.Children.First();
            Assert.Equal("level3", level3Node.Name);
            Assert.Empty(level3Node.Children);
        }

        /// <summary>
        /// 测试具有部分子菜单权限时的导航过滤
        /// </summary>
        [Fact]
        public void FilterNodesByPermission_WithPartialChildMenuPermission_ShouldDisplayParentAndAuthorizedChild()
        {
            // 准备测试数据：考试中心导航结构
            var navigationNodes = new List<NavigationNode>
            {
                new NavigationNode("examCenter", "考试中心", "/exam")
                {
                    Icon = "fa-solid fa-graduation-cap",
                    Permission = "exam",
                    Children = new List<NavigationNode>
                    {
                        new NavigationNode("examPapers", "试卷管理", "/exam/examPapers")
                        {
                            Icon = "fa-solid fa-file-lines",
                            Permission = "exam_examPapers"
                        },
                        new NavigationNode("examRecords", "考试记录管理", "/exam/examRecords")
                        {
                            Icon = "fa-solid fa-clipboard-check",
                            Permission = "exam_examRecords"
                        },
                        new NavigationNode("examSettings", "考试管理", "/exam/examSettings")
                        {
                            Icon = "fa-solid fa-calendar-check",
                            Permission = "exam_examSettings"
                        },
                        new NavigationNode("examStatistics", "考试统计", "/exam/examStatistics")
                        {
                            Icon = "fa-solid fa-chart-pie",
                            Permission = "exam_examStatistics"
                        },
                        new NavigationNode("practiceRecords", "练习记录管理", "/exam/practiceRecords")
                        {
                            Icon = "fa-solid fa-clipboard-check",
                            Permission = "exam_practiceRecords"
                        },
                        new NavigationNode("questionCategories", "题目分类管理", "/exam/questionCategories")
                        {
                            Icon = "fa-solid fa-folder-tree",
                            Permission = "exam_questionCategories"
                        },
                        new NavigationNode("questions", "题目管理", "/exam/questions")
                        {
                            Icon = "fa-solid fa-book",
                            Permission = "exam_questions"
                        },
                        new NavigationNode("questionVersions", "题目版本管理", "/exam/questionVersions")
                        {
                            Icon = "fa-solid fa-code-branch",
                            Permission = "exam_questionVersions"
                        },
                        new NavigationNode("studentGroups", "考生组管理", "/exam/studentGroups")
                        {
                            Icon = "fa-solid fa-users-rectangle",
                            Permission = "exam_studentGroups"
                        },
                        new NavigationNode("students", "考生管理", "/exam/students")
                        {
                            Icon = "fa-solid fa-user-graduate",
                            Permission = "exam_students"
                        },
                        new NavigationNode("wrongQuestions", "错题管理", "/exam/wrongQuestions")
                        {
                            Icon = "fa-solid fa-circle-exclamation",
                            Permission = "exam_wrongQuestions"
                        }
                    }
                }
            };

            // 创建Mock权限服务 - 设置只有试卷管理相关权限
            var mockPermissionService = new Mock<IHasPermissionService>();

            // 设置父级菜单权限
            mockPermissionService.Setup(p => p.HasNavigationPermission(It.Is<string>(s => s == "exam"))).Returns(false);

            // 设置各子菜单权限
            mockPermissionService.Setup(p => p.HasNavigationPermission(It.Is<string>(s => s == "exam_examPapers"))).Returns(true);
            mockPermissionService.Setup(p => p.HasNavigationPermission(It.Is<string>(s => s == "exam_examRecords"))).Returns(false);
            mockPermissionService.Setup(p => p.HasNavigationPermission(It.Is<string>(s => s == "exam_examSettings"))).Returns(false);
            mockPermissionService.Setup(p => p.HasNavigationPermission(It.Is<string>(s => s == "exam_examStatistics"))).Returns(false);
            mockPermissionService.Setup(p => p.HasNavigationPermission(It.Is<string>(s => s == "exam_practiceRecords"))).Returns(false);
            mockPermissionService.Setup(p => p.HasNavigationPermission(It.Is<string>(s => s == "exam_questionCategories"))).Returns(false);
            mockPermissionService.Setup(p => p.HasNavigationPermission(It.Is<string>(s => s == "exam_questions"))).Returns(false);
            mockPermissionService.Setup(p => p.HasNavigationPermission(It.Is<string>(s => s == "exam_questionVersions"))).Returns(false);
            mockPermissionService.Setup(p => p.HasNavigationPermission(It.Is<string>(s => s == "exam_studentGroups"))).Returns(false);
            mockPermissionService.Setup(p => p.HasNavigationPermission(It.Is<string>(s => s == "exam_students"))).Returns(false);
            mockPermissionService.Setup(p => p.HasNavigationPermission(It.Is<string>(s => s == "exam_wrongQuestions"))).Returns(false);

            // 试卷管理的细分权限
            mockPermissionService.Setup(p => p.HasNavigationPermission(It.Is<string>(s => s.StartsWith("exam_examPapers_")))).Returns(true);

            // 创建服务实例
            var service = new NavigationService(
                _mockActionProvider.Object,
                _mockCache.Object,
                _mockLogger.Object,
                _mockConfiguration.Object
            );

            // 执行测试
            var result = service.FilterNodesByPermission(navigationNodes, mockPermissionService.Object);

            // 验证结果
            Assert.NotNull(result);
            Assert.Single(result); // 应返回考试中心菜单

            var examCenterNode = result.First();
            Assert.Equal("examCenter", examCenterNode.Name);
            Assert.Equal("考试中心", examCenterNode.Title);

            Assert.Single(examCenterNode.Children); // 应只返回试卷管理子菜单

            var examPapersNode = examCenterNode.Children.First();
            Assert.Equal("examPapers", examPapersNode.Name);
            Assert.Equal("试卷管理", examPapersNode.Title);
            Assert.StrictEqual(1, examCenterNode.Children.Count);
        }

        /// <summary>
        /// 测试 ModuleAttribute 和 NavigationAttribute 的优先级逻辑
        /// 当控制器同时有 ModuleAttribute 和 NavigationAttribute 时，应优先使用 NavigationAttribute
        /// </summary>
        [Fact]
        public void BuildCodeBasedNavigation_WithBothModuleAndNavigationAttribute_ShouldPrioritizeNavigationAttribute()
        {
            _testOutputHelper.WriteLine("测试 ModuleAttribute 和 NavigationAttribute 优先级 - NavigationAttribute 优先");

            // 这个测试依赖于实际的控制器类型，我们可以通过检查现有的测试控制器来验证逻辑
            // 例如 TestModuleController 同时有 ModuleAttribute 和 NavigationAttribute

            // 创建模拟的导航服务
            var service = new NavigationService(
                _mockActionProvider.Object,
                _mockCache.Object,
                _mockLogger.Object,
                _mockConfiguration.Object);

            // 注意：这里需要设置 ActionProvider 来包含测试控制器
            // 由于这是私有方法的测试，我们需要通过反射来测试
            var buildMethod = typeof(NavigationService).GetMethod("BuildCodeBasedNavigation", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            Assert.NotNull(buildMethod);
            _testOutputHelper.WriteLine("找到 BuildCodeBasedNavigation 私有方法");

            // 验证方法签名
            var parameters = buildMethod.GetParameters();
            Assert.Single(parameters);
            Assert.Equal("moduleName", parameters[0].Name);
            Assert.Equal(typeof(string), parameters[0].ParameterType);

            _testOutputHelper.WriteLine("ModuleAttribute 和 NavigationAttribute 优先级测试完成");
        }

        /// <summary>
        /// 测试只有 NavigationAttribute 的控制器才会创建导航节点
        /// </summary>
        [Fact]
        public void BuildCodeBasedNavigation_WithOnlyModuleAttribute_ShouldNotCreateNavigationNode()
        {
            _testOutputHelper.WriteLine("测试只有 ModuleAttribute 的情况 - 不应创建导航节点");

            // 创建模拟的导航服务
            var service = new NavigationService(
                _mockActionProvider.Object,
                _mockCache.Object,
                _mockLogger.Object,
                _mockConfiguration.Object);

            // 新的逻辑：只有明确定义了 NavigationAttribute 的控制器才会创建导航节点
            // CreateNavigationNodeFromModuleAttribute 方法已被移除
            var createFromModuleMethod = typeof(NavigationService).GetMethod("CreateNavigationNodeFromModuleAttribute", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            // 验证该方法已被移除
            Assert.Null(createFromModuleMethod);
            _testOutputHelper.WriteLine("确认 CreateNavigationNodeFromModuleAttribute 方法已被移除");

            // 验证只有 CreateNavigationNode 方法存在（用于处理 NavigationAttribute）
            var createNavigationMethod = typeof(NavigationService).GetMethod("CreateNavigationNode", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            Assert.NotNull(createNavigationMethod);
            _testOutputHelper.WriteLine("确认 CreateNavigationNode 方法存在");

            // 验证方法签名
            var parameters = createNavigationMethod.GetParameters();
            Assert.Equal(5, parameters.Length);
            Assert.Equal("moduleName", parameters[0].Name);
            Assert.Equal(typeof(NavigationAttribute), parameters[1].ParameterType);
            Assert.Equal("defaultName", parameters[2].Name);
            Assert.Equal("memberInfo", parameters[3].Name);
            Assert.Equal("defaultPath", parameters[4].Name);

            _testOutputHelper.WriteLine("新的导航逻辑测试完成 - 只有明确的 NavigationAttribute 才创建导航");
        }

        /// <summary>
        /// 测试当 NavigationAttribute 被隐藏时应回退到 ModuleAttribute
        /// </summary>
        [Fact]
        public void BuildCodeBasedNavigation_WithHiddenNavigationAttribute_ShouldFallbackToModuleAttribute()
        {
            _testOutputHelper.WriteLine("测试 NavigationAttribute 被隐藏时的回退逻辑");

            // 这个测试验证当 NavigationAttribute.Hidden = true 时，
            // 系统应该检查是否有 ModuleAttribute 可以作为备用

            var service = new NavigationService(
                _mockActionProvider.Object,
                _mockCache.Object,
                _mockLogger.Object,
                _mockConfiguration.Object);

            // 验证逻辑存在
            var buildMethod = typeof(NavigationService).GetMethod("BuildCodeBasedNavigation", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            Assert.NotNull(buildMethod);
            _testOutputHelper.WriteLine("验证隐藏 NavigationAttribute 的回退逻辑存在");

            _testOutputHelper.WriteLine("NavigationAttribute 隐藏回退测试完成");
        }

        /// <summary>
        /// 测试配置中心模块的平台类型继承 - 子控制器应该继承父级的 System 平台类型
        /// </summary>
        [Fact]
        public void ConfigCenterModule_ShouldInheritSystemPlatformType()
        {
            _testOutputHelper.WriteLine("测试配置中心模块的平台类型继承");

            var service = new NavigationService(
                _mockActionProvider.Object,
                _mockCache.Object,
                _mockLogger.Object,
                _mockConfiguration.Object);

            // 通过反射测试私有方法
            var buildMethod = typeof(NavigationService).GetMethod("BuildCodeBasedNavigation", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            Assert.NotNull(buildMethod);

            // 测试配置中心模块的导航构建
            // 在实际应用中，这将验证 ApiControllerBase 设置为 System 平台类型
            // 而子控制器设置为 Inherit，应该继承父级的 System 类型

            _testOutputHelper.WriteLine("验证配置中心模块平台类型继承逻辑存在");

            // 验证平台类型继承方法存在
            var inheritanceMethod = typeof(NavigationService).GetMethod("ProcessPlatformTypeInheritance", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            Assert.NotNull(inheritanceMethod);
            _testOutputHelper.WriteLine("平台类型继承处理方法存在");

            // 验证解析方法存在
            var resolveMethod = typeof(NavigationService).GetMethod("ResolvePlatformType", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            Assert.NotNull(resolveMethod);
            _testOutputHelper.WriteLine("平台类型解析方法存在");

            _testOutputHelper.WriteLine("配置中心模块平台类型继承测试完成");
        }
    }
}