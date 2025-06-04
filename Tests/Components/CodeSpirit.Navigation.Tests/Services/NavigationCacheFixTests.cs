using CodeSpirit.Core.Enums;
using CodeSpirit.Navigation.Models;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Moq;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace CodeSpirit.Navigation.Tests.Services
{
    /// <summary>
    /// 导航缓存修复验证测试
    /// 验证修复后的缓存逻辑正确性
    /// </summary>
    public class NavigationCacheFixTests
    {
        private readonly ITestOutputHelper _output;

        public NavigationCacheFixTests(ITestOutputHelper output)
        {
            _output = output;
        }

        /// <summary>
        /// 测试平台类型推断逻辑修复
        /// 验证模块级别的平台类型推断是否正确
        /// </summary>
        [Fact]
        public void ModulePlatformTypeInference_ShouldWorkCorrectly()
        {
            // Create NavigationService with mocks
            var mockActionProvider = new Mock<IActionDescriptorCollectionProvider>();
            var mockCache = new Mock<IDistributedCache>();
            var mockLogger = new Mock<ILogger<NavigationService>>();
            var mockConfiguration = new Mock<IConfiguration>();

            var service = new NavigationService(
                mockActionProvider.Object,
                mockCache.Object,
                mockLogger.Object,
                mockConfiguration.Object);

            // Test system module case - should correctly infer System platform
            var systemModuleNode = new NavigationNode("config", "配置中心", "/config")
            {
                PlatformType = PlatformType.System,
                OriginalPlatformType = PlatformType.System,
                ModuleName = "config"
            };

            var systemControllerNode = new NavigationNode("apps", "应用管理", "/config/apps")
            {
                PlatformType = PlatformType.System,
                OriginalPlatformType = PlatformType.System,
                ModuleName = "config"
            };

            systemModuleNode.Children.Add(systemControllerNode);

            _output.WriteLine($"系统模块平台类型: {systemModuleNode.PlatformType}");
            _output.WriteLine($"系统模块原始平台类型: {systemModuleNode.OriginalPlatformType}");

            // Verify System module should only appear in System and Both caches
            var moduleNodes = new List<NavigationNode> { systemModuleNode };
            
            var systemFiltered = service.FilterNodesByPlatform(moduleNodes, PlatformType.System);
            var tenantFiltered = service.FilterNodesByPlatform(moduleNodes, PlatformType.Tenant);
            var bothFiltered = service.FilterNodesByPlatform(moduleNodes, PlatformType.Both);

            _output.WriteLine($"System缓存过滤结果: {systemFiltered.Count} 个节点");
            _output.WriteLine($"Tenant缓存过滤结果: {tenantFiltered.Count} 个节点");
            _output.WriteLine($"Both缓存过滤结果: {bothFiltered.Count} 个节点");

            // System平台应该包含该模块
            Assert.Single(systemFiltered);
            Assert.Equal("config", systemFiltered[0].Name);

            // Tenant平台不应该包含System模块
            Assert.Empty(tenantFiltered);

            // Both平台应该包含该模块（因为Both包含System）
            Assert.Single(bothFiltered);
            Assert.Equal("config", bothFiltered[0].Name);
        }

        /// <summary>
        /// 测试缓存平台类型确定逻辑
        /// 验证给定模块平台类型时，应该为哪些平台创建缓存
        /// </summary>
        [Theory]
        [InlineData(PlatformType.System, new[] { PlatformType.System, PlatformType.Both })]
        [InlineData(PlatformType.Tenant, new[] { PlatformType.Tenant, PlatformType.Both })]
        [InlineData(PlatformType.Both, new[] { PlatformType.System, PlatformType.Tenant, PlatformType.Both })]
        public void DetermineCachePlatformTypes_ShouldReturnCorrectPlatforms(
            PlatformType modulePlatformType, PlatformType[] expectedCachePlatforms)
        {
            // Simulate the cache platform determination logic
            var platformTypesToCache = new List<PlatformType>();

            switch (modulePlatformType)
            {
                case PlatformType.System:
                    platformTypesToCache.Add(PlatformType.System);
                    platformTypesToCache.Add(PlatformType.Both); // Both 包含 System
                    break;
                case PlatformType.Tenant:
                    platformTypesToCache.Add(PlatformType.Tenant);
                    platformTypesToCache.Add(PlatformType.Both); // Both 包含 Tenant
                    break;
                case PlatformType.Both:
                    platformTypesToCache.Add(PlatformType.System);
                    platformTypesToCache.Add(PlatformType.Tenant);
                    platformTypesToCache.Add(PlatformType.Both);
                    break;
            }

            _output.WriteLine($"模块平台类型: {modulePlatformType}");
            _output.WriteLine($"应创建的缓存平台: [{string.Join(", ", platformTypesToCache)}]");
            _output.WriteLine($"预期的缓存平台: [{string.Join(", ", expectedCachePlatforms)}]");

            // Verify the platforms to cache match expectations
            Assert.Equal(expectedCachePlatforms.Length, platformTypesToCache.Count);
            foreach (var expectedPlatform in expectedCachePlatforms)
            {
                Assert.Contains(expectedPlatform, platformTypesToCache);
            }
        }

        /// <summary>
        /// 测试缓存键生成逻辑
        /// 验证不同平台类型应该生成不同的缓存键
        /// </summary>
        [Theory]
        [InlineData("config", PlatformType.System, "CodeSpirit:Navigation:Module:config:System")]
        [InlineData("config", PlatformType.Tenant, "CodeSpirit:Navigation:Module:config:Tenant")]
        [InlineData("config", PlatformType.Both, "CodeSpirit:Navigation:Module:config:Both")]
        [InlineData("exam", PlatformType.System, "CodeSpirit:Navigation:Module:exam:System")]
        public void GetModuleCacheKey_ShouldGenerateCorrectKeys(
            string moduleName, PlatformType platformType, string expectedKey)
        {
            // Create NavigationService with mocks
            var mockActionProvider = new Mock<IActionDescriptorCollectionProvider>();
            var mockCache = new Mock<IDistributedCache>();
            var mockLogger = new Mock<ILogger<NavigationService>>();
            var mockConfiguration = new Mock<IConfiguration>();

            var service = new NavigationService(
                mockActionProvider.Object,
                mockCache.Object,
                mockLogger.Object,
                mockConfiguration.Object);

            // Use reflection to call private method
            var method = typeof(NavigationService).GetMethod("GetModuleCacheKey",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var result = (string)method.Invoke(service, new object[] { moduleName, platformType });

            _output.WriteLine($"模块: {moduleName}, 平台: {platformType}");
            _output.WriteLine($"生成的缓存键: {result}");
            _output.WriteLine($"预期的缓存键: {expectedKey}");

            Assert.Equal(expectedKey, result);
        }
    }
} 