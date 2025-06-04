using CodeSpirit.Core.Enums;
using CodeSpirit.Navigation.Models;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Moq;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace CodeSpirit.Navigation.Tests.Services
{
    /// <summary>
    /// 缓存问题测试
    /// 验证config模块平台类型缓存问题
    /// </summary>
    public class CachingIssueTests
    {
        private readonly ITestOutputHelper _output;

        public CachingIssueTests(ITestOutputHelper output)
        {
            _output = output;
        }

        /// <summary>
        /// 测试平台类型过滤功能
        /// 验证System平台的模块在Tenant缓存中应该为空
        /// </summary>
        [Fact]
        public void FilterNodesByPlatform_SystemModule_ShouldNotAppearInTenantCache()
        {
            // Arrange
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
            var moduleNodes = new List<NavigationNode> { systemModuleNode };

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

            // Act - 测试不同平台类型的过滤
            var systemFiltered = service.FilterNodesByPlatform(moduleNodes, PlatformType.System);
            var tenantFiltered = service.FilterNodesByPlatform(moduleNodes, PlatformType.Tenant);
            var bothFiltered = service.FilterNodesByPlatform(moduleNodes, PlatformType.Both);

            // Assert
            _output.WriteLine($"System过滤结果: {systemFiltered.Count} 个节点");
            _output.WriteLine($"Tenant过滤结果: {tenantFiltered.Count} 个节点");
            _output.WriteLine($"Both过滤结果: {bothFiltered.Count} 个节点");

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
        /// 测试位运算过滤逻辑
        /// 验证PlatformType枚举的位运算是否正确工作
        /// </summary>
        [Theory]
        [InlineData(PlatformType.System, PlatformType.System, true)]
        [InlineData(PlatformType.System, PlatformType.Tenant, false)]
        [InlineData(PlatformType.System, PlatformType.Both, true)]  // Both包含System
        [InlineData(PlatformType.Tenant, PlatformType.System, false)]
        [InlineData(PlatformType.Tenant, PlatformType.Tenant, true)]
        [InlineData(PlatformType.Tenant, PlatformType.Both, true)]  // Both包含Tenant
        [InlineData(PlatformType.Both, PlatformType.System, true)]
        [InlineData(PlatformType.Both, PlatformType.Tenant, true)]
        [InlineData(PlatformType.Both, PlatformType.Both, true)]
        public void PlatformType_BitwiseAndOperation_ShouldWorkCorrectly(
            PlatformType nodePlatform, PlatformType filterPlatform, bool expectedResult)
        {
            // Act
            var result = (nodePlatform & filterPlatform) != 0;

            // Assert
            _output.WriteLine($"节点平台: {nodePlatform} ({(int)nodePlatform})");
            _output.WriteLine($"过滤平台: {filterPlatform} ({(int)filterPlatform})");
            _output.WriteLine($"位运算结果: {nodePlatform} & {filterPlatform} = {(int)(nodePlatform & filterPlatform)}");
            _output.WriteLine($"预期结果: {expectedResult}, 实际结果: {result}");

            Assert.Equal(expectedResult, result);
        }
    }
} 