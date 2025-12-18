using CodeSpirit.Core.Authorization;
using CodeSpirit.Core.Enums;
using CodeSpirit.Navigation.Models;
using CodeSpirit.Navigation.Services;
using CodeSpirit.Navigation.Services.Filters;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;
using System.Linq;

namespace CodeSpirit.Navigation.Tests.Services
{
    /// <summary>
    /// NavigationFilterService 单元测试
    /// </summary>
    public class NavigationFilterServiceTests
    {
        private readonly Mock<ILogger<NavigationFilterService>> _loggerMock;
        private readonly NavigationFilterService _filterService;

        public NavigationFilterServiceTests()
        {
            _loggerMock = new Mock<ILogger<NavigationFilterService>>();

            var filters = new List<INavigationFilter>
            {
                new PlatformFilter(),
                new PermissionFilter(),
                new AuthenticationFilter()
            };

            _filterService = new NavigationFilterService(filters, _loggerMock.Object);
        }

        /// <summary>
        /// 测试：使用平台过滤器应正确过滤
        /// </summary>
        [Fact]
        public void FilterNodes_WithPlatformFilter_ShouldFilterCorrectly()
        {
            // 安排
            var nodes = new List<NavigationNode>
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

            var context = new NavigationFilterContext
            {
                PlatformType = PlatformType.System,
                IsAuthenticated = true // 确保认证过滤器通过
            };

            // 执行
            var result = _filterService.FilterNodes(nodes, context);

            // 断言
            Assert.Single(result);
            Assert.Equal("system", result[0].Name);
        }

        /// <summary>
        /// 测试：有子节点的父节点即使不匹配也应包含（如果子节点匹配）
        /// </summary>
        [Fact]
        public void FilterNodes_WithChildNodes_ShouldIncludeParentIfChildMatches()
        {
            // 安排
            var nodes = new List<NavigationNode>
            {
                new NavigationNode("parent", "Parent", "/parent")
                {
                    PlatformType = PlatformType.Tenant, // 父节点不匹配
                    RequireAuth = false, // 确保认证过滤器通过
                    Children = new List<NavigationNode>
                    {
                        new NavigationNode("child", "Child", "/child")
                        {
                            PlatformType = PlatformType.System, // 子节点匹配
                            RequireAuth = false
                        }
                    }
                }
            };

            var context = new NavigationFilterContext
            {
                PlatformType = PlatformType.System,
                IsAuthenticated = true // 确保认证过滤器通过
            };

            // 执行
            var result = _filterService.FilterNodes(nodes, context);

            // 断言
            Assert.Single(result); // 父节点应该被包含（因为子节点匹配）
            Assert.Equal("parent", result[0].Name);
            Assert.Single(result[0].Children); // 子节点也应该存在
        }

        /// <summary>
        /// 测试：注册自定义过滤器应添加过滤器
        /// </summary>
        [Fact]
        public void RegisterFilter_ShouldAddCustomFilter()
        {
            // 安排
            var customFilter = new Mock<INavigationFilter>();
            customFilter.Setup(f => f.Priority).Returns(10);
            customFilter.Setup(f => f.ShouldInclude(It.IsAny<NavigationNode>(), It.IsAny<NavigationFilterContext>()))
                .Returns(true);

            // 执行
            _filterService.RegisterFilter(customFilter.Object);

            // 断言
            // 验证日志被调用
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Registered custom filter")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        /// <summary>
        /// 测试：空节点列表应返回空列表
        /// </summary>
        [Fact]
        public void FilterNodes_WithEmptyList_ShouldReturnEmptyList()
        {
            // 安排
            var nodes = new List<NavigationNode>();
            var context = new NavigationFilterContext();

            // 执行
            var result = _filterService.FilterNodes(nodes, context);

            // 断言
            Assert.Empty(result);
        }

        /// <summary>
        /// 测试：null节点列表应返回空列表
        /// </summary>
        [Fact]
        public void FilterNodes_WithNullList_ShouldReturnEmptyList()
        {
            // 安排
            List<NavigationNode> nodes = null;
            var context = new NavigationFilterContext();

            // 执行
            var result = _filterService.FilterNodes(nodes, context);

            // 断言
            Assert.Empty(result);
        }

        /// <summary>
        /// 测试：多个过滤器应全部应用
        /// </summary>
        [Fact]
        public void FilterNodes_WithMultipleFilters_ShouldApplyAll()
        {
            // 安排
            var nodes = new List<NavigationNode>
            {
                new NavigationNode("test", "Test", "/test")
                {
                    PlatformType = PlatformType.System,
                    RequireAuth = true
                }
            };

            var context = new NavigationFilterContext
            {
                PlatformType = PlatformType.System,
                IsAuthenticated = false // 未认证
            };

            // 执行
            var result = _filterService.FilterNodes(nodes, context);

            // 断言
            // 平台过滤通过，但认证过滤失败，所以应该被排除
            Assert.Empty(result);
        }

        /// <summary>
        /// 测试：过滤器异常时应包含节点（容错机制）
        /// </summary>
        [Fact]
        public void FilterNodes_WhenFilterThrows_ShouldIncludeNode()
        {
            // 安排
            var failingFilter = new Mock<INavigationFilter>();
            failingFilter.Setup(f => f.Priority).Returns(1);
            failingFilter.Setup(f => f.ShouldInclude(It.IsAny<NavigationNode>(), It.IsAny<NavigationFilterContext>()))
                .Throws(new Exception("Filter error"));

            var filters = new List<INavigationFilter> { failingFilter.Object };
            var service = new NavigationFilterService(filters, _loggerMock.Object);

            var nodes = new List<NavigationNode>
            {
                new NavigationNode("test", "Test", "/test")
            };

            var context = new NavigationFilterContext();

            // 执行
            var result = service.FilterNodes(nodes, context);

            // 断言
            // 过滤器异常时，默认包含节点（容错机制）
            Assert.Single(result);
        }
    }
}
