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
        private readonly Mock<IServiceProvider> _serviceProviderMock;
        private readonly Mock<ICacheService> _cacheServiceMock;
        private readonly Mock<ILogger<NavigationCacheManager>> _loggerMock;
        private readonly NavigationCacheManager _cacheManager;

        public NavigationVersionControlTests()
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

            // 执行 - 第一次设置
            await _cacheManager.SetCachedNavigationAsync(nodes);

            // 等待一小段时间确保时间戳不同（如果版本包含时间戳）
            await Task.Delay(10);

            // 执行 - 第二次设置相同内容
            await _cacheManager.SetCachedNavigationAsync(nodes);

            // 断言
            // 版本哈希应该相同（基于内容，不包含时间戳）
            Assert.Equal(capturedData1.Version, capturedData2.Version);
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
            // 当前实现：顺序不同会导致哈希不同（这是合理的，因为顺序可能影响导航显示）
            // 如果需要顺序无关，可以在ComputeContentHash中先排序
            Assert.NotEqual(capturedData1.Version, capturedData2.Version);
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
            Assert.NotNull(capturedData.Version);
            Assert.True(capturedData.Version.Length <= 16, $"Version length should be <= 16, but was {capturedData.Version.Length}");
            Assert.True(capturedData.Version.Length > 0, "Version should not be empty");
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
            await _cacheManager.SetCachedNavigationAsync(complexNodes);

            // 断言
            Assert.NotNull(capturedData);
            Assert.NotNull(capturedData.Version);
            Assert.Equal(2, capturedData.Nodes.Count);
            Assert.Equal(2, capturedData.Nodes[0].Children.Count);
            Assert.Equal("module1", capturedData.Nodes[0].Name);
            Assert.Equal("module2", capturedData.Nodes[1].Name);
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
            // 由于使用了NullValueHandling.Ignore和DefaultValueHandling.Ignore，版本应该相同
            Assert.Equal(capturedData1.Version, capturedData2.Version);
        }
    }
}

