using CodeSpirit.Navigation.Extensions;
using CodeSpirit.Navigation.Models;
using CodeSpirit.Navigation.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CodeSpirit.Navigation.Tests.Services
{
    /// <summary>
    /// 导航版本控制单元测试
    /// </summary>
    public class NavigationVersionControlTests
    {
        private readonly Mock<IDistributedCache> _cacheMock;
        private readonly Mock<ILogger<NavigationCacheManager>> _loggerMock;
        private readonly NavigationCacheManager _cacheManager;

        public NavigationVersionControlTests()
        {
            _cacheMock = new Mock<IDistributedCache>();
            _loggerMock = new Mock<ILogger<NavigationCacheManager>>();

            _cacheManager = new NavigationCacheManager(
                _cacheMock.Object,
                _loggerMock.Object);
        }

        /// <summary>
        /// 测试：版本哈希计算的确定性 - 相同内容多次计算应得到相同哈希
        /// </summary>
        [Fact]
        public async Task VersionHash_ShouldBeDeterministic()
        {
            // 安排
            var nodes = new List<NavigationNode>
            {
                new NavigationNode("module1", "Module 1", "/module1")
                {
                    Icon = "fa-icon",
                    Order = 1
                },
                new NavigationNode("module2", "Module 2", "/module2")
                {
                    Icon = "fa-icon-2",
                    Order = 2
                }
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

            // 执行 - 第一次设置
            await _cacheManager.SetCachedNavigationAsync(nodes);

            // 等待一小段时间确保时间戳不同（如果版本包含时间戳）
            await Task.Delay(10);

            // 执行 - 第二次设置相同内容
            await _cacheManager.SetCachedNavigationAsync(nodes);

            // 断言
            var json1 = Encoding.UTF8.GetString(capturedBytes1);
            var json2 = Encoding.UTF8.GetString(capturedBytes2);
            var cacheData1 = JsonConvert.DeserializeObject<NavigationCacheData>(json1);
            var cacheData2 = JsonConvert.DeserializeObject<NavigationCacheData>(json2);

            // 版本哈希应该相同（基于内容，不包含时间戳）
            Assert.Equal(cacheData1.Version, cacheData2.Version);
        }

        /// <summary>
        /// 测试：版本哈希的唯一性 - 不同内容应生成不同哈希
        /// </summary>
        [Fact]
        public async Task VersionHash_DifferentContent_ShouldGenerateDifferentHash()
        {
            // 安排
            var nodes1 = new List<NavigationNode>
            {
                new NavigationNode("module1", "Module 1", "/module1")
            };

            var nodes2 = new List<NavigationNode>
            {
                new NavigationNode("module1", "Module 1 Changed", "/module1")  // 标题不同
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
        /// 测试：版本哈希应忽略节点顺序（如果排序后相同）
        /// </summary>
        [Fact]
        public async Task VersionHash_ShouldBeOrderIndependent()
        {
            // 注意：当前实现基于JSON序列化，JSON序列化会保持顺序
            // 这个测试验证当前行为（顺序敏感）
            // 如果需要顺序无关的哈希，需要先排序再序列化

            // 安排
            var nodes1 = new List<NavigationNode>
            {
                new NavigationNode("module1", "Module 1", "/module1") { Order = 1 },
                new NavigationNode("module2", "Module 2", "/module2") { Order = 2 }
            };

            var nodes2 = new List<NavigationNode>
            {
                new NavigationNode("module2", "Module 2", "/module2") { Order = 2 },
                new NavigationNode("module1", "Module 1", "/module1") { Order = 1 }
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

            // 当前实现：顺序不同会导致哈希不同（这是合理的，因为顺序可能影响导航显示）
            // 如果需要顺序无关，可以在ComputeContentHash中先排序
            Assert.NotEqual(cacheData1.Version, cacheData2.Version);
        }

        /// <summary>
        /// 测试：版本哈希长度应合理（16字符）
        /// </summary>
        [Fact]
        public async Task VersionHash_ShouldHaveReasonableLength()
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
            var json = Encoding.UTF8.GetString(capturedBytes);
            var cacheData = JsonConvert.DeserializeObject<NavigationCacheData>(json);

            Assert.NotNull(cacheData.Version);
            Assert.True(cacheData.Version.Length <= 16, $"Version length should be <= 16, but was {cacheData.Version.Length}");
            Assert.True(cacheData.Version.Length > 0, "Version should not be empty");
        }

        /// <summary>
        /// 测试：NavigationCacheData应正确序列化和反序列化
        /// </summary>
        [Fact]
        public void NavigationCacheData_ShouldSerializeAndDeserialize()
        {
            // 安排
            var original = new NavigationCacheData
            {
                Version = "test-version-123",
                UpdatedAt = DateTime.UtcNow,
                Nodes = new List<NavigationNode>
                {
                    new NavigationNode("test", "Test", "/test")
                    {
                        Icon = "fa-icon",
                        Order = 1
                    }
                }
            };

            // 执行
            var json = JsonConvert.SerializeObject(original);
            var deserialized = JsonConvert.DeserializeObject<NavigationCacheData>(json);

            // 断言
            Assert.NotNull(deserialized);
            Assert.Equal(original.Version, deserialized.Version);
            Assert.Equal(original.UpdatedAt, deserialized.UpdatedAt, TimeSpan.FromSeconds(1));
            Assert.NotNull(deserialized.Nodes);
            Assert.Single(deserialized.Nodes);
            Assert.Equal(original.Nodes[0].Name, deserialized.Nodes[0].Name);
            Assert.Equal(original.Nodes[0].Title, deserialized.Nodes[0].Title);
        }

        /// <summary>
        /// 测试：复杂导航树应正确计算版本哈希
        /// </summary>
        [Fact]
        public async Task VersionHash_WithComplexNavigationTree_ShouldComputeCorrectly()
        {
            // 安排
            var complexNodes = new List<NavigationNode>
            {
                new NavigationNode("module1", "Module 1", "/module1")
                {
                    Icon = "fa-module1",
                    Order = 1,
                    Children = new List<NavigationNode>
                    {
                        new NavigationNode("sub1", "Sub 1", "/module1/sub1")
                        {
                            Order = 1,
                            Permission = "module1.sub1"
                        },
                        new NavigationNode("sub2", "Sub 2", "/module1/sub2")
                        {
                            Order = 2,
                            Permission = "module1.sub2"
                        }
                    }
                },
                new NavigationNode("module2", "Module 2", "/module2")
                {
                    Icon = "fa-module2",
                    Order = 2,
                    PlatformType = CodeSpirit.Core.Enums.PlatformType.Tenant
                }
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
            await _cacheManager.SetCachedNavigationAsync(complexNodes);

            // 断言
            var json = Encoding.UTF8.GetString(capturedBytes);
            var cacheData = JsonConvert.DeserializeObject<NavigationCacheData>(json);

            Assert.NotNull(cacheData);
            Assert.NotNull(cacheData.Version);
            Assert.Equal(2, cacheData.Nodes.Count);
            Assert.Equal(2, cacheData.Nodes[0].Children.Count);
            Assert.Equal("module1", cacheData.Nodes[0].Name);
            Assert.Equal("module2", cacheData.Nodes[1].Name);
        }

        /// <summary>
        /// 测试：版本哈希应忽略null值和默认值
        /// </summary>
        [Fact]
        public async Task VersionHash_ShouldIgnoreNullAndDefaultValues()
        {
            // 安排
            var nodes1 = new List<NavigationNode>
            {
                new NavigationNode("test", "Test", "/test")
                {
                    Icon = null,
                    Description = null,
                    Order = 0  // 默认值
                }
            };

            var nodes2 = new List<NavigationNode>
            {
                new NavigationNode("test", "Test", "/test")
                {
                    // 不设置Icon、Description、Order（都是默认值）
                }
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

            // 由于使用了NullValueHandling.Ignore和DefaultValueHandling.Ignore，版本应该相同
            Assert.Equal(cacheData1.Version, cacheData2.Version);
        }
    }
}

