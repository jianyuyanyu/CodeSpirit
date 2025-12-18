using CodeSpirit.Navigation.Models;
using CodeSpirit.Navigation.Services;
using CodeSpirit.Navigation.Services.Filters;

namespace CodeSpirit.Navigation.Tests.Filters
{
    /// <summary>
    /// AuthenticationFilter 单元测试
    /// </summary>
    public class AuthenticationFilterTests
    {
        private readonly AuthenticationFilter _filter;

        public AuthenticationFilterTests()
        {
            _filter = new AuthenticationFilter();
        }

        /// <summary>
        /// 测试：节点需要认证且用户已认证时应包含节点
        /// </summary>
        [Fact]
        public void ShouldInclude_WhenNodeRequiresAuthAndUserAuthenticated_ShouldReturnTrue()
        {
            // 安排
            var node = new NavigationNode("test", "Test", "/test")
            {
                RequireAuth = true
            };

            var context = new NavigationFilterContext
            {
                IsAuthenticated = true
            };

            // 执行
            var result = _filter.ShouldInclude(node, context);

            // 断言
            Assert.True(result);
        }

        /// <summary>
        /// 测试：节点需要认证但用户未认证时应排除节点
        /// </summary>
        [Fact]
        public void ShouldInclude_WhenNodeRequiresAuthButUserNotAuthenticated_ShouldReturnFalse()
        {
            // 安排
            var node = new NavigationNode("test", "Test", "/test")
            {
                RequireAuth = true
            };

            var context = new NavigationFilterContext
            {
                IsAuthenticated = false
            };

            // 执行
            var result = _filter.ShouldInclude(node, context);

            // 断言
            Assert.False(result);
        }

        /// <summary>
        /// 测试：节点不需要认证时应包含节点
        /// </summary>
        [Fact]
        public void ShouldInclude_WhenNodeDoesNotRequireAuth_ShouldReturnTrue()
        {
            // 安排
            var node = new NavigationNode("test", "Test", "/test")
            {
                RequireAuth = false
            };

            var context = new NavigationFilterContext
            {
                IsAuthenticated = false
            };

            // 执行
            var result = _filter.ShouldInclude(node, context);

            // 断言
            Assert.True(result);
        }

        /// <summary>
        /// 测试：优先级应为3
        /// </summary>
        [Fact]
        public void Priority_ShouldBe3()
        {
            // 断言
            Assert.Equal(3, _filter.Priority);
        }
    }
}
