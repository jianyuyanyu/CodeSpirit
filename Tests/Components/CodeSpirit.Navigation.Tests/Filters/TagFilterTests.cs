using CodeSpirit.Navigation.Models;
using CodeSpirit.Navigation.Services;
using CodeSpirit.Navigation.Services.Filters;

namespace CodeSpirit.Navigation.Tests.Filters
{
    /// <summary>
    /// TagFilter 单元测试
    /// </summary>
    public class TagFilterTests
    {
        private readonly TagFilter _filter;

        public TagFilterTests()
        {
            _filter = new TagFilter();
        }

        /// <summary>
        /// 测试：没有用户标签时应包含所有节点
        /// </summary>
        [Fact]
        public void ShouldInclude_WhenNoUserTags_ShouldReturnTrue()
        {
            // 安排
            var node = new NavigationNode("test", "Test", "/test")
            {
                Tags = new[] { "tag1", "tag2" }
            };

            var context = new NavigationFilterContext
            {
                UserTags = null
            };

            // 执行
            var result = _filter.ShouldInclude(node, context);

            // 断言
            Assert.True(result);
        }

        /// <summary>
        /// 测试：节点标签与用户标签有交集时应包含节点
        /// </summary>
        [Fact]
        public void ShouldInclude_WhenTagsIntersect_ShouldReturnTrue()
        {
            // 安排
            var node = new NavigationNode("test", "Test", "/test")
            {
                Tags = new[] { "tag1", "tag2", "tag3" }
            };

            var context = new NavigationFilterContext
            {
                UserTags = new[] { "tag2", "tag4" }
            };

            // 执行
            var result = _filter.ShouldInclude(node, context);

            // 断言
            Assert.True(result);
        }

        /// <summary>
        /// 测试：节点标签与用户标签无交集时应排除节点
        /// </summary>
        [Fact]
        public void ShouldInclude_WhenTagsDoNotIntersect_ShouldReturnFalse()
        {
            // 安排
            var node = new NavigationNode("test", "Test", "/test")
            {
                Tags = new[] { "tag1", "tag2" }
            };

            var context = new NavigationFilterContext
            {
                UserTags = new[] { "tag3", "tag4" }
            };

            // 执行
            var result = _filter.ShouldInclude(node, context);

            // 断言
            Assert.False(result);
        }

        /// <summary>
        /// 测试：节点没有标签时应包含节点
        /// </summary>
        [Fact]
        public void ShouldInclude_WhenNodeHasNoTags_ShouldReturnTrue()
        {
            // 安排
            var node = new NavigationNode("test", "Test", "/test")
            {
                Tags = null
            };

            var context = new NavigationFilterContext
            {
                UserTags = new[] { "tag1", "tag2" }
            };

            // 执行
            var result = _filter.ShouldInclude(node, context);

            // 断言
            Assert.True(result);
        }

        /// <summary>
        /// 测试：优先级应为8
        /// </summary>
        [Fact]
        public void Priority_ShouldBe8()
        {
            // 断言
            Assert.Equal(8, _filter.Priority);
        }
    }
}
