using CodeSpirit.Core.Enums;
using CodeSpirit.Navigation.Models;
using CodeSpirit.Navigation.Tests.TestBase;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Extensions.Configuration;

namespace CodeSpirit.Navigation.Tests.Services
{
    /// <summary>
    /// 导航服务缓存管理测试
    /// </summary>
    public class NavigationServiceCacheTests : NavigationTestBase
    {
        [Fact]
        public async Task GetNavigationTreeAsync_WithCache_ShouldReturnCachedData()
        {
            // Arrange
            var cachedNodes = new List<NavigationNode>
            {
                new NavigationNode("cached1", "缓存节点1", "/cached1")
            };

            var moduleNames = new List<string> { "TestModule" };

            // 设置模块名称缓存
            MockCache.Setup(x => x.GetAsync("CodeSpirit:Navigation:ModuleNames", It.IsAny<CancellationToken>()))
                .ReturnsAsync(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(moduleNames));

            // 设置模块导航缓存
            MockCache.Setup(x => x.GetAsync("CodeSpirit:Navigation:Module:TestModule:Both", It.IsAny<CancellationToken>()))
                .ReturnsAsync(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(cachedNodes));

            // Act
            var result = await NavigationService.GetNavigationTreeAsync(PlatformType.Both);

            // Assert
            Assert.Single(result);
            Assert.Equal("cached1", result[0].Name);
        }

        [Fact]
        public async Task GetNavigationTreeAsync_NoModuleNamesCache_ShouldReturnEmpty()
        {
            // Arrange
            MockCache.Setup(x => x.GetAsync("CodeSpirit:Navigation:ModuleNames", It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte[])null);

            // Act
            var result = await NavigationService.GetNavigationTreeAsync();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetNavigationTreeAsync_PlatformSpecific_ShouldUsePlatformSpecificCacheKey()
        {
            // Arrange
            var moduleNames = new List<string> { "TestModule" };
            var systemNodes = new List<NavigationNode>
            {
                new NavigationNode("system1", "系统节点", "/system1")
            };

            MockCache.Setup(x => x.GetAsync("CodeSpirit:Navigation:ModuleNames", It.IsAny<CancellationToken>()))
                .ReturnsAsync(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(moduleNames));

            MockCache.Setup(x => x.GetAsync("CodeSpirit:Navigation:Module:TestModule:System", It.IsAny<CancellationToken>()))
                .ReturnsAsync(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(systemNodes));

            // Act
            var result = await NavigationService.GetNavigationTreeAsync(PlatformType.System);

            // Assert
            Assert.Single(result);
            Assert.Equal("system1", result[0].Name);

            // 验证使用了正确的缓存键
            MockCache.Verify(x => x.GetAsync("CodeSpirit:Navigation:Module:TestModule:System", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ClearModuleNavigationCacheAsync_WithSpecificPlatform_ShouldClearOnlySpecificPlatform()
        {
            // Act
            await NavigationService.ClearModuleNavigationCacheAsync("TestModule", PlatformType.System);

            // Assert
            MockCache.Verify(x => x.RemoveAsync("CodeSpirit:Navigation:Module:TestModule:System", It.IsAny<CancellationToken>()), Times.Once);
            MockCache.Verify(x => x.RemoveAsync(It.Is<string>(key => key.Contains("Tenant")), It.IsAny<CancellationToken>()), Times.Never);
            MockCache.Verify(x => x.RemoveAsync(It.Is<string>(key => key.Contains("Both")), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ClearModuleNavigationCacheAsync_WithoutPlatform_ShouldClearAllPlatforms()
        {
            // Arrange
            var moduleNames = new List<string> { "TestModule", "AnotherModule" };
            MockCache.Setup(x => x.GetAsync("CodeSpirit:Navigation:ModuleNames", It.IsAny<CancellationToken>()))
                .ReturnsAsync(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(moduleNames));

            // Act
            await NavigationService.ClearModuleNavigationCacheAsync("TestModule");

            // Assert
            MockCache.Verify(x => x.RemoveAsync("CodeSpirit:Navigation:Module:TestModule:System", It.IsAny<CancellationToken>()), Times.Once);
            MockCache.Verify(x => x.RemoveAsync("CodeSpirit:Navigation:Module:TestModule:Tenant", It.IsAny<CancellationToken>()), Times.Once);
            MockCache.Verify(x => x.RemoveAsync("CodeSpirit:Navigation:Module:TestModule:Both", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ClearAllNavigationCacheAsync_ShouldClearAllCaches()
        {
            // Arrange
            var moduleNames = new List<string> { "Module1", "Module2" };
            MockCache.Setup(x => x.GetAsync("CodeSpirit:Navigation:ModuleNames", It.IsAny<CancellationToken>()))
                .ReturnsAsync(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(moduleNames));

            // Act
            await NavigationService.ClearAllNavigationCacheAsync();

            // Assert
            // 应该清除模块名称缓存
            MockCache.Verify(x => x.RemoveAsync("CodeSpirit:Navigation:ModuleNames", It.IsAny<CancellationToken>()), Times.Once);

            // 应该清除每个模块的缓存（调用了 ClearModuleNavigationCacheAsync）
            MockCache.Verify(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.AtLeast(6)); // 2个模块 * 3个平台 = 6次
        }

        [Fact]
        public async Task InitializeNavigationTree_ShouldSetupCacheForAllPlatforms()
        {
            // Arrange
            var existingModules = new List<string>();
            MockCache.Setup(x => x.GetAsync("CodeSpirit:Navigation:ModuleNames", It.IsAny<CancellationToken>()))
                .ReturnsAsync(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(existingModules));

            // 设置配置模拟以避免配置绑定错误
            var configSection = new Mock<IConfigurationSection>();
            configSection.Setup(x => x.GetChildren()).Returns(new List<IConfigurationSection>());
            MockConfiguration.Setup(x => x.GetSection(It.IsAny<string>())).Returns(configSection.Object);

            // Act
            await NavigationService.InitializeNavigationTree();

            // Assert
            // 应该设置模块名称缓存
            MockCache.Verify(x => x.SetAsync(
                "CodeSpirit:Navigation:ModuleNames",
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()), Times.Once);

            // 应该为每个发现的模块和每个平台设置缓存
            MockCache.Verify(x => x.SetAsync(
                It.Is<string>(key => key.StartsWith("CodeSpirit:Navigation:Module:") && key.Contains(":")),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()), Times.AtLeast(1));
        }

        [Fact]
        public async Task GetNavigationTreeAsync_CacheException_ShouldLogAndContinue()
        {
            // Arrange
            var moduleNames = new List<string> { "TestModule" };
            MockCache.Setup(x => x.GetAsync("CodeSpirit:Navigation:ModuleNames", It.IsAny<CancellationToken>()))
                .ReturnsAsync(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(moduleNames));

            // 设置获取模块缓存时抛出异常
            MockCache.Setup(x => x.GetAsync("CodeSpirit:Navigation:Module:TestModule:Both", It.IsAny<CancellationToken>()))
                .ThrowsAsync(new System.Exception("Cache error"));

            // Act
            var result = await NavigationService.GetNavigationTreeAsync();

            // Assert
            // 应该返回空结果而不是抛出异常
            Assert.Empty(result);

            // 应该记录错误日志
            MockLogger.Verify(
                x => x.Log(
                    Microsoft.Extensions.Logging.LogLevel.Error,
                    It.IsAny<Microsoft.Extensions.Logging.EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to retrieve navigation for module")),
                    It.IsAny<System.Exception>(),
                    It.IsAny<System.Func<It.IsAnyType, System.Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task GetNavigationTreeAsync_ModuleNamesCacheException_ShouldLogAndReturnEmpty()
        {
            // Arrange
            MockCache.Setup(x => x.GetAsync("CodeSpirit:Navigation:ModuleNames", It.IsAny<CancellationToken>()))
                .ThrowsAsync(new System.Exception("Cache error"));

            // Act
            var result = await NavigationService.GetNavigationTreeAsync();

            // Assert
            Assert.Empty(result);

            // 应该记录错误日志
            MockLogger.Verify(
                x => x.Log(
                    Microsoft.Extensions.Logging.LogLevel.Error,
                    It.IsAny<Microsoft.Extensions.Logging.EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to retrieve navigation module list")),
                    It.IsAny<System.Exception>(),
                    It.IsAny<System.Func<It.IsAnyType, System.Exception, string>>()),
                Times.Once);
        }
    }
} 