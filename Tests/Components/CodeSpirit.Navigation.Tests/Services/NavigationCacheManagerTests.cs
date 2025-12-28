using CodeSpirit.Navigation.Extensions;
using CodeSpirit.Navigation.Models;
using CodeSpirit.Navigation.Services;
using Microsoft.Extensions.Caching.Distributed;
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

            var json = JsonConvert.SerializeObject(cacheData);
            var bytes = Encoding.UTF8.GetBytes(json);

            _cacheMock.Setup(x => x.GetAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(bytes);

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
            _cacheMock.Setup(x => x.GetAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte[])null);

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

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(cacheData);
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);

            _cacheMock.Setup(x => x.GetAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(bytes);

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
            _cacheMock.Setup(x => x.GetAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte[])null);

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

            byte[] capturedBytes = null;
            _cacheMock.Setup(x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()))
                .Callback<string, byte[], DistributedCacheEntryOptions, CancellationToken>(
                    (key, value, options, token) => capturedBytes = value)
                .Returns(Task.CompletedTask);

            // 执行
            await _cacheManager.SetCachedNavigationAsync(nodes);

            // 断言
            Assert.NotNull(capturedBytes);
            var json = Encoding.UTF8.GetString(capturedBytes);
            var cacheData = JsonConvert.DeserializeObject<NavigationCacheData>(json);
            
            Assert.NotNull(cacheData);
            Assert.NotNull(cacheData.Version);
            Assert.NotEmpty(cacheData.Version);
            Assert.Single(cacheData.Nodes);
            Assert.True(cacheData.UpdatedAt <= DateTime.UtcNow.AddSeconds(1));
            Assert.True(cacheData.UpdatedAt >= DateTime.UtcNow.AddSeconds(-1));
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

            byte[] capturedBytes1 = null;
            byte[] capturedBytes2 = null;

            _cacheMock.Setup(x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()))
                .Callback<string, byte[], DistributedCacheEntryOptions, CancellationToken>(
                    (key, value, options, token) => 
                    {
                        if (capturedBytes1 == null)
                            capturedBytes1 = value;
                        else
                            capturedBytes2 = value;
                    })
                .Returns(Task.CompletedTask);

            // 执行
            await _cacheManager.SetCachedNavigationAsync(nodes1);
            await _cacheManager.SetCachedNavigationAsync(nodes2);

            // 断言
            var json1 = Encoding.UTF8.GetString(capturedBytes1);
            var json2 = Encoding.UTF8.GetString(capturedBytes2);
            var cacheData1 = JsonConvert.DeserializeObject<NavigationCacheData>(json1);
            var cacheData2 = JsonConvert.DeserializeObject<NavigationCacheData>(json2);

            Assert.Equal(cacheData1.Version, cacheData2.Version);
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

            byte[] capturedBytes1 = null;
            byte[] capturedBytes2 = null;

            _cacheMock.Setup(x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()))
                .Callback<string, byte[], DistributedCacheEntryOptions, CancellationToken>(
                    (key, value, options, token) => 
                    {
                        if (capturedBytes1 == null)
                            capturedBytes1 = value;
                        else
                            capturedBytes2 = value;
                    })
                .Returns(Task.CompletedTask);

            // 执行
            await _cacheManager.SetCachedNavigationAsync(nodes1);
            await _cacheManager.SetCachedNavigationAsync(nodes2);

            // 断言
            var json1 = Encoding.UTF8.GetString(capturedBytes1);
            var json2 = Encoding.UTF8.GetString(capturedBytes2);
            var cacheData1 = JsonConvert.DeserializeObject<NavigationCacheData>(json1);
            var cacheData2 = JsonConvert.DeserializeObject<NavigationCacheData>(json2);

            Assert.NotEqual(cacheData1.Version, cacheData2.Version);
        }

        /// <summary>
        /// 测试：空节点列表应生成特殊版本标识
        /// </summary>
        [Fact]
        public async Task SetCachedNavigationAsync_EmptyNodes_ShouldGenerateSpecialVersion()
        {
            // 安排
            var nodes = new List<NavigationNode>();

            byte[] capturedBytes = null;
            _cacheMock.Setup(x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()))
                .Callback<string, byte[], DistributedCacheEntryOptions, CancellationToken>(
                    (key, value, options, token) => capturedBytes = value)
                .Returns(Task.CompletedTask);

            // 执行
            await _cacheManager.SetCachedNavigationAsync(nodes);

            // 断言
            Assert.NotNull(capturedBytes);
            var json = Encoding.UTF8.GetString(capturedBytes);
            var cacheData = JsonConvert.DeserializeObject<NavigationCacheData>(json);
            
            Assert.NotNull(cacheData);
            Assert.Equal("empty", cacheData.Version);
        }

        /// <summary>
        /// 测试：向后兼容 - 旧格式缓存应自动迁移
        /// </summary>
        [Fact]
        public async Task GetCachedNavigationAsync_WithOldFormat_ShouldMigrateToNewFormat()
        {
            // 安排
            var oldNodes = new List<NavigationNode>
            {
                new NavigationNode("test", "Test", "/test")
            };

            // 先返回旧格式（直接序列化节点列表）
            var oldJson = JsonConvert.SerializeObject(oldNodes);
            var oldBytes = Encoding.UTF8.GetBytes(oldJson);

            // GetCachedNavigationDataAsync会先调用GetAsync，返回null（新格式不存在）
            // 然后GetCachedNavigationAsync会再次调用GetAsync读取旧格式
            _cacheMock.SetupSequence(x => x.GetAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte[])null)  // GetCachedNavigationDataAsync返回null（新格式不存在）
                .ReturnsAsync(oldBytes);  // GetCachedNavigationAsync读取旧格式

            _cacheMock.Setup(x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // 执行
            var result = await _cacheManager.GetCachedNavigationAsync();

            // 断言
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("test", result[0].Name);
            
            // 验证自动迁移到新格式
            _cacheMock.Verify(x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
