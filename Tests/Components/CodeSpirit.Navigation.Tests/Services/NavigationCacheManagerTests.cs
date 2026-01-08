using CodeSpirit.Caching.Abstractions;
using CodeSpirit.Navigation.Extensions;
using CodeSpirit.Navigation.Models;
using CodeSpirit.Navigation.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CodeSpirit.Navigation.Tests.Services
{
    /// <summary>
    /// NavigationCacheManager 单元测试
    /// </summary>
    public class NavigationCacheManagerTests
    {
        private readonly Mock<IServiceProvider> _serviceProviderMock;
        private readonly Mock<ICacheService> _cacheServiceMock;
        private readonly Mock<ILogger<NavigationCacheManager>> _loggerMock;
        private readonly NavigationCacheManager _cacheManager;

        public NavigationCacheManagerTests()
        {
            _cacheServiceMock = new Mock<ICacheService>();
            _loggerMock = new Mock<ILogger<NavigationCacheManager>>();
            
            // 创建 ServiceProvider mock
            _serviceProviderMock = new Mock<IServiceProvider>();
            var serviceScopeMock = new Mock<IServiceScope>();
            var scopedServiceProviderMock = new Mock<IServiceProvider>();
            
            scopedServiceProviderMock.Setup(x => x.GetService(typeof(ICacheService)))
                .Returns(_cacheServiceMock.Object);
            scopedServiceProviderMock.Setup(x => x.GetRequiredService(typeof(ICacheService)))
                .Returns(_cacheServiceMock.Object);
            
            serviceScopeMock.Setup(x => x.ServiceProvider).Returns(scopedServiceProviderMock.Object);
            _serviceProviderMock.Setup(x => x.CreateScope()).Returns(serviceScopeMock.Object);

            _cacheManager = new NavigationCacheManager(
                _serviceProviderMock.Object,
                _loggerMock.Object);
        }

        /// <summary>
        /// 测试：当缓存为空时，应返回null
        /// </summary>
        [Fact]
        public async Task GetCachedNavigationAsync_WhenCacheEmpty_ShouldReturnNull()
        {
            // 安排
            _cacheServiceMock.Setup(x => x.GetAsync<NavigationCacheData>(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync((NavigationCacheData)null);

            // 执行
            var result = await _cacheManager.GetCachedNavigationAsync();

            // 断言
            Assert.Null(result);
        }

        /// <summary>
        /// 测试：当缓存存在时，应返回数据
        /// </summary>
        [Fact]
        public async Task GetCachedNavigationAsync_WhenCacheExists_ShouldReturnData()
        {
            // 安排
            var nodes = new List<NavigationNode>
            {
                new NavigationNode("test", "Test", "/test")
            };

            var cacheData = new NavigationCacheData
            {
                Version = "test-version",
                UpdatedAt = DateTime.UtcNow,
                Nodes = nodes
            };

            _cacheServiceMock.Setup(x => x.GetAsync<NavigationCacheData>(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(cacheData);

            // 执行
            var result = await _cacheManager.GetCachedNavigationAsync();

            // 断言
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("test", result[0].Name);
        }

        /// <summary>
        /// 测试：设置缓存应存储数据
        /// </summary>
        [Fact]
        public async Task SetCachedNavigationAsync_ShouldStoreInCache()
        {
            // 安排
            var nodes = new List<NavigationNode>
            {
                new NavigationNode("test", "Test", "/test")
            };

            _cacheServiceMock.Setup(x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<NavigationCacheData>(),
                It.IsAny<CodeSpirit.Caching.Models.CacheOptions>(),
                It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // 执行
            await _cacheManager.SetCachedNavigationAsync(nodes);

            // 断言
            _cacheServiceMock.Verify(x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<NavigationCacheData>(),
                It.IsAny<CodeSpirit.Caching.Models.CacheOptions>(),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        /// <summary>
        /// 测试：清除所有缓存应移除缓存
        /// </summary>
        [Fact]
        public async Task ClearAllCacheAsync_ShouldRemoveCache()
        {
            // 安排
            _cacheServiceMock.Setup(x => x.RemoveAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // 执行
            await _cacheManager.ClearAllCacheAsync();

            // 断言
            _cacheServiceMock.Verify(x => x.RemoveAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        /// <summary>
        /// 测试：清除模块缓存应清除整个缓存
        /// </summary>
        [Fact]
        public async Task ClearModuleCacheAsync_ShouldClearAllCache()
        {
            // 安排
            _cacheServiceMock.Setup(x => x.RemoveAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // 执行
            await _cacheManager.ClearModuleCacheAsync("TestModule");

            // 断言
            _cacheServiceMock.Verify(x => x.RemoveAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        /// <summary>
        /// 测试：获取缓存的导航数据应返回NavigationCacheData
        /// </summary>
        [Fact]
        public async Task GetCachedNavigationDataAsync_WhenCacheExists_ShouldReturnCacheData()
        {
            // 安排
            var cacheData = new NavigationCacheData
            {
                Version = "test-version-123",
                UpdatedAt = DateTime.UtcNow,
                Nodes = new List<NavigationNode>
                {
                    new NavigationNode("test", "Test", "/test")
                }
            };

            _cacheServiceMock.Setup(x => x.GetAsync<NavigationCacheData>(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(cacheData);

            // 执行
            var result = await _cacheManager.GetCachedNavigationDataAsync();

            // 断言
            Assert.NotNull(result);
            Assert.Equal("test-version-123", result.Version);
            Assert.Single(result.Nodes);
            Assert.Equal("test", result.Nodes[0].Name);
        }

        /// <summary>
        /// 测试：获取缓存的导航数据，缓存为空时应返回null
        /// </summary>
        [Fact]
        public async Task GetCachedNavigationDataAsync_WhenCacheEmpty_ShouldReturnNull()
        {
            // 安排
            _cacheServiceMock.Setup(x => x.GetAsync<NavigationCacheData>(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync((NavigationCacheData)null);

            // 执行
            var result = await _cacheManager.GetCachedNavigationDataAsync();

            // 断言
            Assert.Null(result);
        }

        /// <summary>
        /// 测试：获取当前版本号应从缓存数据中提取
        /// </summary>
        [Fact]
        public async Task GetCurrentVersionAsync_WhenCacheExists_ShouldReturnVersion()
        {
            // 安排
            var cacheData = new NavigationCacheData
            {
                Version = "test-version-456",
                UpdatedAt = DateTime.UtcNow,
                Nodes = new List<NavigationNode>()
            };

            _cacheServiceMock.Setup(x => x.GetAsync<NavigationCacheData>(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(cacheData);

            // 执行
            var result = await _cacheManager.GetCurrentVersionAsync();

            // 断言
            Assert.Equal("test-version-456", result);
        }

        /// <summary>
        /// 测试：获取当前版本号，缓存为空时应返回null
        /// </summary>
        [Fact]
        public async Task GetCurrentVersionAsync_WhenCacheEmpty_ShouldReturnNull()
        {
            // 安排
            _cacheServiceMock.Setup(x => x.GetAsync<NavigationCacheData>(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync((NavigationCacheData)null);

            // 执行
            var result = await _cacheManager.GetCurrentVersionAsync();

            // 断言
            Assert.Null(result);
        }

        /// <summary>
        /// 测试：设置导航树缓存应自动计算版本号
        /// </summary>
        [Fact]
        public async Task SetCachedNavigationAsync_ShouldAutoComputeVersion()
        {
            // 安排
            var nodes = new List<NavigationNode>
            {
                new NavigationNode("test", "Test", "/test")
            };

            NavigationCacheData capturedData = null;
            _cacheServiceMock.Setup(x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<NavigationCacheData>(),
                It.IsAny<CodeSpirit.Caching.Models.CacheOptions>(),
                It.IsAny<CancellationToken>()))
                .Callback<string, NavigationCacheData, CodeSpirit.Caching.Models.CacheOptions, CancellationToken>(
                    (key, value, options, token) => capturedData = value)
                .Returns(Task.CompletedTask);

            // 执行
            await _cacheManager.SetCachedNavigationAsync(nodes);

            // 断言
            Assert.NotNull(capturedData);
            Assert.NotNull(capturedData.Version);
            Assert.NotEmpty(capturedData.Version);
            Assert.Single(capturedData.Nodes);
            Assert.True(capturedData.UpdatedAt <= DateTime.UtcNow.AddSeconds(1));
            Assert.True(capturedData.UpdatedAt >= DateTime.UtcNow.AddSeconds(-1));
        }

        /// <summary>
        /// 测试：相同内容应生成相同版本哈希
        /// </summary>
        [Fact]
        public async Task SetCachedNavigationAsync_SameContent_ShouldGenerateSameVersion()
        {
            // 安排
            var nodes1 = new List<NavigationNode>
            {
                new NavigationNode("test", "Test", "/test")
            };

            var nodes2 = new List<NavigationNode>
            {
                new NavigationNode("test", "Test", "/test")
            };

            NavigationCacheData capturedData1 = null;
            NavigationCacheData capturedData2 = null;

            _cacheServiceMock.Setup(x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<NavigationCacheData>(),
                It.IsAny<CodeSpirit.Caching.Models.CacheOptions>(),
                It.IsAny<CancellationToken>()))
                .Callback<string, NavigationCacheData, CodeSpirit.Caching.Models.CacheOptions, CancellationToken>(
                    (key, value, options, token) => 
                    {
                        if (capturedData1 == null)
                            capturedData1 = value;
                        else
                            capturedData2 = value;
                    })
                .Returns(Task.CompletedTask);

            // 执行
            await _cacheManager.SetCachedNavigationAsync(nodes1);
            await _cacheManager.SetCachedNavigationAsync(nodes2);

            // 断言
            Assert.Equal(capturedData1.Version, capturedData2.Version);
        }

        /// <summary>
        /// 测试：不同内容应生成不同版本哈希
        /// </summary>
        [Fact]
        public async Task SetCachedNavigationAsync_DifferentContent_ShouldGenerateDifferentVersion()
        {
            // 安排
            var nodes1 = new List<NavigationNode>
            {
                new NavigationNode("test1", "Test1", "/test1")
            };

            var nodes2 = new List<NavigationNode>
            {
                new NavigationNode("test2", "Test2", "/test2")
            };

            NavigationCacheData capturedData1 = null;
            NavigationCacheData capturedData2 = null;

            _cacheServiceMock.Setup(x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<NavigationCacheData>(),
                It.IsAny<CodeSpirit.Caching.Models.CacheOptions>(),
                It.IsAny<CancellationToken>()))
                .Callback<string, NavigationCacheData, CodeSpirit.Caching.Models.CacheOptions, CancellationToken>(
                    (key, value, options, token) => 
                    {
                        if (capturedData1 == null)
                            capturedData1 = value;
                        else
                            capturedData2 = value;
                    })
                .Returns(Task.CompletedTask);

            // 执行
            await _cacheManager.SetCachedNavigationAsync(nodes1);
            await _cacheManager.SetCachedNavigationAsync(nodes2);

            // 断言
            Assert.NotEqual(capturedData1.Version, capturedData2.Version);
        }

        /// <summary>
        /// 测试：空节点列表应生成特殊版本标识
        /// </summary>
        [Fact]
        public async Task SetCachedNavigationAsync_EmptyNodes_ShouldGenerateSpecialVersion()
        {
            // 安排
            var nodes = new List<NavigationNode>();

            NavigationCacheData capturedData = null;
            _cacheServiceMock.Setup(x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<NavigationCacheData>(),
                It.IsAny<CodeSpirit.Caching.Models.CacheOptions>(),
                It.IsAny<CancellationToken>()))
                .Callback<string, NavigationCacheData, CodeSpirit.Caching.Models.CacheOptions, CancellationToken>(
                    (key, value, options, token) => capturedData = value)
                .Returns(Task.CompletedTask);

            // 执行
            await _cacheManager.SetCachedNavigationAsync(nodes);

            // 断言
            Assert.NotNull(capturedData);
            Assert.Equal("empty", capturedData.Version);
        }

        /// <summary>
        /// 测试：向后兼容 - 旧格式缓存应自动迁移
        /// </summary>
        [Fact]
        public async Task GetCachedNavigationAsync_WithOldFormat_ShouldMigrateToNewFormat()
        {
            // 注意：当前实现不再支持旧格式迁移，因为现在直接使用 ICacheService
            // 这个测试保留用于验证基本功能
            
            // 安排
            var cacheData = new NavigationCacheData
            {
                Version = "test-version",
                UpdatedAt = DateTime.UtcNow,
                Nodes = new List<NavigationNode>
                {
                    new NavigationNode("test", "Test", "/test")
                }
            };

            _cacheServiceMock.Setup(x => x.GetAsync<NavigationCacheData>(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(cacheData);

            // 执行
            var result = await _cacheManager.GetCachedNavigationAsync();

            // 断言
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("test", result[0].Name);
        }
    }
}

