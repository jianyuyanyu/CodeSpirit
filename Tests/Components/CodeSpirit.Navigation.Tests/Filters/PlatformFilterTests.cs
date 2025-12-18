using CodeSpirit.Core.Authorization;
using CodeSpirit.Core.Enums;
using CodeSpirit.Navigation.Models;
using CodeSpirit.Navigation.Services;
using CodeSpirit.Navigation.Services.Filters;
using Moq;

namespace CodeSpirit.Navigation.Tests.Filters
{
    /// <summary>
    /// PlatformFilter 单元测试
    /// </summary>
    public class PlatformFilterTests
    {
        private readonly PlatformFilter _filter;

        public PlatformFilterTests()
        {
            _filter = new PlatformFilter();
        }

        /// <summary>
        /// 测试：平台类型匹配的各种情况
        /// </summary>
        [Theory]
        [InlineData(PlatformType.System, PlatformType.System, true)]
        [InlineData(PlatformType.Tenant, PlatformType.Tenant, true)]
        [InlineData(PlatformType.Both, PlatformType.System, true)]
        [InlineData(PlatformType.Both, PlatformType.Tenant, true)]
        [InlineData(PlatformType.Both, PlatformType.Both, true)]
        [InlineData(PlatformType.System, PlatformType.Tenant, false)]
        [InlineData(PlatformType.Tenant, PlatformType.System, false)]
        public void ShouldInclude_WithPlatformTypes_ReturnsExpected(
            PlatformType nodePlatform,
            PlatformType contextPlatform,
            bool expected)
        {
            // 安排
            var node = new NavigationNode("test", "Test", "/test")
            {
                PlatformType = nodePlatform
            };

            var context = new NavigationFilterContext
            {
                PlatformType = contextPlatform
            };

            // 执行
            var result = _filter.ShouldInclude(node, context);

            // 断言
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// 测试：优先级应为1
        /// </summary>
        [Fact]
        public void Priority_ShouldBe1()
        {
            // 断言
            Assert.Equal(1, _filter.Priority);
        }
    }
}
