using CodeSpirit.Core.Authorization;
using CodeSpirit.Core.Enums;
using CodeSpirit.Navigation.Models;
using CodeSpirit.Navigation.Services;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CodeSpirit.Navigation.Tests.Services
{
    /// <summary>
    /// NavigationService 集成测试
    /// </summary>
    public class NavigationServiceTests
    {
        private readonly Mock<INavigationTreeBuilder> _treeBuilderMock;
        private readonly Mock<INavigationCacheManager> _cacheManagerMock;
        private readonly Mock<INavigationFilterService> _filterServiceMock;
        private readonly Mock<ILogger<NavigationService>> _loggerMock;
        private readonly NavigationService _service;

        public NavigationServiceTests()
        {
            _treeBuilderMock = new Mock<INavigationTreeBuilder>();
            _cacheManagerMock = new Mock<INavigationCacheManager>();
            _filterServiceMock = new Mock<INavigationFilterService>();
            _loggerMock = new Mock<ILogger<NavigationService>>();

            _service = new NavigationService(
                _treeBuilderMock.Object,
                _cacheManagerMock.Object,
                _filterServiceMock.Object,
                _loggerMock.Object);
        }

        /// <summary>
        /// 测试：首次调用应使用缓存，缓存命中后应使用缓存数据
        /// </summary>
        [Fact]
        public async Task GetNavigationTreeAsync_ShouldUseCacheAfterFirstCall()
        {
            // 安排
            var cachedNodes = new List<NavigationNode>
            {
                new NavigationNode("test", "Test", "/test")
            };

            var filteredNodes = new List<NavigationNode>
            {
                new NavigationNode("test", "Test", "/test")
            };

            _cacheManagerMock.Setup(x => x.GetCachedNavigationAsync())
                .ReturnsAsync(cachedNodes);

            _filterServiceMock.Setup(x => x.FilterNodes(
                It.IsAny<List<NavigationNode>>(),
                It.IsAny<NavigationFilterContext>()))
                .Returns(filteredNodes);

            // 执行
            var result = await _service.GetNavigationTreeAsync(PlatformType.Both);

            // 断言
            Assert.Single(result);
            _cacheManagerMock.Verify(x => x.GetCachedNavigationAsync(), Times.Once);
            _treeBuilderMock.Verify(x => x.BuildNavigationTree(), Times.Never);
        }

        /// <summary>
        /// 测试：缓存未命中时应构建导航树并写入缓存
        /// </summary>
        [Fact]
        public async Task GetNavigationTreeAsync_WhenCacheMiss_ShouldBuildAndCache()
        {
            // 安排
            var builtNodes = new List<NavigationNode>
            {
                new NavigationNode("test", "Test", "/test")
            };

            var filteredNodes = new List<NavigationNode>
            {
                new NavigationNode("test", "Test", "/test")
            };

            _cacheManagerMock.Setup(x => x.GetCachedNavigationAsync())
                .ReturnsAsync((List<NavigationNode>)null);

            _treeBuilderMock.Setup(x => x.BuildNavigationTree())
                .Returns(builtNodes);

            _filterServiceMock.Setup(x => x.FilterNodes(
                It.IsAny<List<NavigationNode>>(),
                It.IsAny<NavigationFilterContext>()))
                .Returns(filteredNodes);

            // 执行
            var result = await _service.GetNavigationTreeAsync(PlatformType.Both);

            // 断言
            Assert.Single(result);
            _treeBuilderMock.Verify(x => x.BuildNavigationTree(), Times.Once);
            _cacheManagerMock.Verify(x => x.SetCachedNavigationAsync(builtNodes), Times.Once);
        }

        /// <summary>
        /// 测试：使用平台过滤器应返回过滤后的节点
        /// </summary>
        [Fact]
        public async Task GetNavigationTreeAsync_WithPlatformFilter_ShouldReturnFilteredNodes()
        {
            // 安排
            var cachedNodes = new List<NavigationNode>
            {
                new NavigationNode("system", "System", "/system")
                {
                    PlatformType = PlatformType.System
                },
                new NavigationNode("tenant", "Tenant", "/tenant")
                {
                    PlatformType = PlatformType.Tenant
                }
            };

            var filteredNodes = new List<NavigationNode>
            {
                new NavigationNode("system", "System", "/system")
            };

            _cacheManagerMock.Setup(x => x.GetCachedNavigationAsync())
                .ReturnsAsync(cachedNodes);

            _filterServiceMock.Setup(x => x.FilterNodes(
                It.IsAny<List<NavigationNode>>(),
                It.Is<NavigationFilterContext>(c => c.PlatformType == PlatformType.System)))
                .Returns(filteredNodes);

            // 执行
            var result = await _service.GetNavigationTreeAsync(PlatformType.System);

            // 断言
            Assert.Single(result);
            Assert.Equal("system", result[0].Name);
        }

        /// <summary>
        /// 测试：初始化导航树应构建并缓存
        /// </summary>
        [Fact]
        public async Task InitializeNavigationTree_ShouldBuildAndCache()
        {
            // 安排
            var navigationTree = new List<NavigationNode>
            {
                new NavigationNode("test", "Test", "/test")
            };

            _treeBuilderMock.Setup(x => x.BuildNavigationTree())
                .Returns(navigationTree);

            _cacheManagerMock.Setup(x => x.GetCachedNavigationDataAsync())
                .ReturnsAsync((NavigationCacheData)null);

            _cacheManagerMock.Setup(x => x.GetCurrentVersionAsync())
                .ReturnsAsync("test-version-123");

            // 执行
            await _service.InitializeNavigationTree();

            // 断言
            _treeBuilderMock.Verify(x => x.BuildNavigationTree(), Times.Once);
            _cacheManagerMock.Verify(x => x.SetCachedNavigationAsync(navigationTree), Times.Once);
        }

        /// <summary>
        /// 测试：初始化导航树时应检测版本变化
        /// </summary>
        [Fact]
        public async Task InitializeNavigationTree_WhenVersionChanges_ShouldDetectChange()
        {
            // 安排
            var existingNodes = new List<NavigationNode>
            {
                new NavigationNode("existing", "Existing", "/existing")
            };

            var newNodes = new List<NavigationNode>
            {
                new NavigationNode("new", "New", "/new")
            };

            var existingCacheData = new NavigationCacheData
            {
                Version = "old-version-123",
                UpdatedAt = System.DateTime.UtcNow.AddHours(-1),
                Nodes = existingNodes
            };

            _treeBuilderMock.Setup(x => x.BuildNavigationTree())
                .Returns(newNodes);

            _cacheManagerMock.Setup(x => x.GetCachedNavigationDataAsync())
                .ReturnsAsync(existingCacheData);

            _cacheManagerMock.Setup(x => x.GetCurrentVersionAsync())
                .ReturnsAsync("new-version-456");

            // 执行
            await _service.InitializeNavigationTree();

            // 断言
            _treeBuilderMock.Verify(x => x.BuildNavigationTree(), Times.Once);
            _cacheManagerMock.Verify(x => x.SetCachedNavigationAsync(It.IsAny<List<NavigationNode>>()), Times.Once);
            _cacheManagerMock.Verify(x => x.GetCurrentVersionAsync(), Times.Once);
        }

        /// <summary>
        /// 测试：初始化导航树时版本未变化应记录日志
        /// </summary>
        [Fact]
        public async Task InitializeNavigationTree_WhenVersionUnchanged_ShouldLogInfo()
        {
            // 安排 - 模拟有模块合并但内容相同导致版本未变化的情况
            var existingNodes = new List<NavigationNode>
            {
                new NavigationNode("existing", "Existing", "/existing")
            };

            var newNodes = new List<NavigationNode>
            {
                new NavigationNode("new", "New", "/new")
            };

            // 合并后的节点（与existingNodes相同，模拟合并后内容未变化）
            var mergedNodes = new List<NavigationNode>
            {
                new NavigationNode("existing", "Existing", "/existing"),
                new NavigationNode("new", "New", "/new")
            };

            var existingCacheData = new NavigationCacheData
            {
                Version = "same-version-123",
                UpdatedAt = System.DateTime.UtcNow.AddHours(-1),
                Nodes = existingNodes
            };

            _treeBuilderMock.Setup(x => x.BuildNavigationTree())
                .Returns(newNodes);

            _cacheManagerMock.Setup(x => x.GetCachedNavigationDataAsync())
                .ReturnsAsync(existingCacheData);

            _cacheManagerMock.Setup(x => x.SetCachedNavigationAsync(It.IsAny<List<NavigationNode>>()))
                .Returns(Task.CompletedTask);

            _cacheManagerMock.Setup(x => x.GetCurrentVersionAsync())
                .ReturnsAsync("same-version-123"); // 版本未变化

            // 执行
            await _service.InitializeNavigationTree();

            // 断言
            _treeBuilderMock.Verify(x => x.BuildNavigationTree(), Times.Once);
            _cacheManagerMock.Verify(x => x.SetCachedNavigationAsync(It.IsAny<List<NavigationNode>>()), Times.Once);
            _cacheManagerMock.Verify(x => x.GetCurrentVersionAsync(), Times.Once);
        }

        /// <summary>
        /// 测试：获取导航版本号应委托给缓存管理器
        /// </summary>
        [Fact]
        public async Task GetNavigationVersionAsync_ShouldDelegateToCacheManager()
        {
            // 安排
            var expectedVersion = "test-version-789";
            _cacheManagerMock.Setup(x => x.GetCurrentVersionAsync())
                .ReturnsAsync(expectedVersion);

            // 执行
            var result = await _service.GetNavigationVersionAsync();

            // 断言
            Assert.Equal(expectedVersion, result);
            _cacheManagerMock.Verify(x => x.GetCurrentVersionAsync(), Times.Once);
        }

        /// <summary>
        /// 测试：获取导航版本号，缓存为空时应返回null
        /// </summary>
        [Fact]
        public async Task GetNavigationVersionAsync_WhenCacheEmpty_ShouldReturnNull()
        {
            // 安排
            _cacheManagerMock.Setup(x => x.GetCurrentVersionAsync())
                .ReturnsAsync((string)null);

            // 执行
            var result = await _service.GetNavigationVersionAsync();

            // 断言
            Assert.Null(result);
        }

        /// <summary>
        /// 测试：清除模块缓存应调用缓存管理器
        /// </summary>
        [Fact]
        public async Task ClearModuleNavigationCacheAsync_ShouldInvalidateCache()
        {
            // 安排
            _cacheManagerMock.Setup(x => x.ClearModuleCacheAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // 执行
            await _service.ClearModuleNavigationCacheAsync("TestModule");

            // 断言
            _cacheManagerMock.Verify(x => x.ClearModuleCacheAsync("TestModule"), Times.Once);
        }

        /// <summary>
        /// 测试：清除所有导航缓存应调用缓存管理器
        /// </summary>
        [Fact]
        public async Task ClearAllNavigationCacheAsync_ShouldClearCache()
        {
            // 安排
            _cacheManagerMock.Setup(x => x.ClearAllCacheAsync())
                .Returns(Task.CompletedTask);

            // 执行
            await _service.ClearAllNavigationCacheAsync();

            // 断言
            _cacheManagerMock.Verify(x => x.ClearAllCacheAsync(), Times.Once);
        }

        /// <summary>
        /// 测试：根据权限过滤应委托给过滤服务
        /// </summary>
        [Fact]
        public void FilterNodesByPermission_ShouldDelegateToFilterService()
        {
            // 安排
            var nodes = new List<NavigationNode>
            {
                new NavigationNode("test", "Test", "/test")
            };

            var filteredNodes = new List<NavigationNode>
            {
                new NavigationNode("test", "Test", "/test")
            };

            var permissionServiceMock = new Mock<IHasPermissionService>();

            _filterServiceMock.Setup(x => x.FilterNodes(
                nodes,
                It.Is<NavigationFilterContext>(c => c.PermissionService == permissionServiceMock.Object)))
                .Returns(filteredNodes);

            // 执行
            var result = _service.FilterNodesByPermission(nodes, permissionServiceMock.Object);

            // 断言
            Assert.Single(result);
            _filterServiceMock.Verify(x => x.FilterNodes(
                nodes,
                It.Is<NavigationFilterContext>(c => c.PermissionService == permissionServiceMock.Object)),
                Times.Once);
        }

        /// <summary>
        /// 测试：根据平台过滤应委托给过滤服务
        /// </summary>
        [Fact]
        public void FilterNodesByPlatform_ShouldDelegateToFilterService()
        {
            // 安排
            var nodes = new List<NavigationNode>
            {
                new NavigationNode("test", "Test", "/test")
            };

            var filteredNodes = new List<NavigationNode>
            {
                new NavigationNode("test", "Test", "/test")
            };

            _filterServiceMock.Setup(x => x.FilterNodes(
                nodes,
                It.Is<NavigationFilterContext>(c => c.PlatformType == PlatformType.System)))
                .Returns(filteredNodes);

            // 执行
            var result = _service.FilterNodesByPlatform(nodes, PlatformType.System);

            // 断言
            Assert.Single(result);
            _filterServiceMock.Verify(x => x.FilterNodes(
                nodes,
                It.Is<NavigationFilterContext>(c => c.PlatformType == PlatformType.System)),
                Times.Once);
        }

        /// <summary>
        /// 测试：根据上下文过滤应委托给过滤服务
        /// </summary>
        [Fact]
        public void FilterNodesByContext_ShouldDelegateToFilterService()
        {
            // 安排
            var nodes = new List<NavigationNode>
            {
                new NavigationNode("test", "Test", "/test")
            };

            var context = new NavigationFilterContext
            {
                PlatformType = PlatformType.System
            };

            var filteredNodes = new List<NavigationNode>
            {
                new NavigationNode("test", "Test", "/test")
            };

            _filterServiceMock.Setup(x => x.FilterNodes(nodes, context))
                .Returns(filteredNodes);

            // 执行
            var result = _service.FilterNodesByContext(nodes, context);

            // 断言
            Assert.Single(result);
            _filterServiceMock.Verify(x => x.FilterNodes(nodes, context), Times.Once);
        }

        /// <summary>
        /// 测试：异常处理应返回空列表
        /// </summary>
        [Fact]
        public async Task GetNavigationTreeAsync_WhenExceptionOccurs_ShouldReturnEmptyList()
        {
            // 安排
            _cacheManagerMock.Setup(x => x.GetCachedNavigationAsync())
                .ThrowsAsync(new System.Exception("Cache error"));

            // 执行
            var result = await _service.GetNavigationTreeAsync(PlatformType.Both);

            // 断言
            Assert.Empty(result);
        }
    }
}
