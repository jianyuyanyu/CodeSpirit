using CodeSpirit.Navigation.Extensions;
using CodeSpirit.Navigation.Models;
using CodeSpirit.Navigation.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CodeSpirit.Navigation.Tests.Services
{
    /// <summary>
    /// NavigationCacheManager 单元测试
    /// </summary>
    public class NavigationCacheManagerTests
    {
        private readonly Mock<IDistributedCache> _cacheMock;
        private readonly Mock<ILogger<NavigationCacheManager>> _loggerMock;
        private readonly NavigationCacheManager _cacheManager;

        public NavigationCacheManagerTests()
        {
            _cacheMock = new Mock<IDistributedCache>();
            _loggerMock = new Mock<ILogger<NavigationCacheManager>>();

            _cacheManager = new NavigationCacheManager(
                _cacheMock.Object,
                _loggerMock.Object);
        }

        /// <summary>
        /// 测试：当缓存为空时，应返回null
        /// </summary>
        [Fact]
        public async Task GetCachedNavigationAsync_WhenCacheEmpty_ShouldReturnNull()
        {
            // 安排
            _cacheMock.Setup(x => x.GetAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte[])null);

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

            var json = System.Text.Json.JsonSerializer.Serialize(nodes);
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);

            _cacheMock.Setup(x => x.GetAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(bytes);

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

            _cacheMock.Setup(x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // 执行
            await _cacheManager.SetCachedNavigationAsync(nodes);

            // 断言
            _cacheMock.Verify(x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
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
            _cacheMock.Setup(x => x.RemoveAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // 执行
            await _cacheManager.ClearAllCacheAsync();

            // 断言
            _cacheMock.Verify(x => x.RemoveAsync(
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
            _cacheMock.Setup(x => x.RemoveAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // 执行
            await _cacheManager.ClearModuleCacheAsync("TestModule");

            // 断言
            _cacheMock.Verify(x => x.RemoveAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
