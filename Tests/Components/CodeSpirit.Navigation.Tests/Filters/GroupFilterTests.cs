using CodeSpirit.Navigation.Models;
using CodeSpirit.Navigation.Services;
using CodeSpirit.Navigation.Services.Filters;

namespace CodeSpirit.Navigation.Tests.Filters
{
    /// <summary>
    /// GroupFilter 单元测试
    /// </summary>
    public class GroupFilterTests
    {
        private readonly GroupFilter _filter;

        public GroupFilterTests()
        {
            _filter = new GroupFilter();
        }

        /// <summary>
        /// 测试：没有分组过滤器时应包含所有节点
        /// </summary>
        [Fact]
        public void ShouldInclude_WhenNoGroupFilter_ShouldReturnTrue()
        {
            // 安排
            var node = new NavigationNode("test", "Test", "/test")
            {
                Group = "group1"
            };

            var context = new NavigationFilterContext
            {
                GroupFilter = null
            };

            // 执行
            var result = _filter.ShouldInclude(node, context);

            // 断言
            Assert.True(result);
        }

        /// <summary>
        /// 测试：节点分组在过滤器中时应包含节点
        /// </summary>
        [Fact]
        public void ShouldInclude_WhenNodeGroupInFilter_ShouldReturnTrue()
        {
            // 安排
            var node = new NavigationNode("test", "Test", "/test")
            {
                Group = "group1"
            };

            var context = new NavigationFilterContext
            {
                GroupFilter = new[] { "group1", "group2" }
            };

            // 执行
            var result = _filter.ShouldInclude(node, context);

            // 断言
            Assert.True(result);
        }

        /// <summary>
        /// 测试：节点分组不在过滤器中时应排除节点
        /// </summary>
        [Fact]
        public void ShouldInclude_WhenNodeGroupNotInFilter_ShouldReturnFalse()
        {
            // 安排
            var node = new NavigationNode("test", "Test", "/test")
            {
                Group = "group3"
            };

            var context = new NavigationFilterContext
            {
                GroupFilter = new[] { "group1", "group2" }
            };

            // 执行
            var result = _filter.ShouldInclude(node, context);

            // 断言
            Assert.False(result);
        }

        /// <summary>
        /// 测试：节点没有分组时应包含节点
        /// </summary>
        [Fact]
        public void ShouldInclude_WhenNodeHasNoGroup_ShouldReturnTrue()
        {
            // 安排
            var node = new NavigationNode("test", "Test", "/test")
            {
                Group = null
            };

            var context = new NavigationFilterContext
            {
                GroupFilter = new[] { "group1", "group2" }
            };

            // 执行
            var result = _filter.ShouldInclude(node, context);

            // 断言
            Assert.True(result);
        }

        /// <summary>
        /// 测试：优先级应为7
        /// </summary>
        [Fact]
        public void Priority_ShouldBe7()
        {
            // 断言
            Assert.Equal(7, _filter.Priority);
        }
    }
}
