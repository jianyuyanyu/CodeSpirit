using CodeSpirit.Navigation.Models;
using CodeSpirit.Navigation.Services;
using CodeSpirit.Navigation.Services.Filters;

namespace CodeSpirit.Navigation.Tests.Filters
{
    /// <summary>
    /// ExperimentalFilter 单元测试
    /// </summary>
    public class ExperimentalFilterTests
    {
        private readonly ExperimentalFilter _filter;

        public ExperimentalFilterTests()
        {
            _filter = new ExperimentalFilter();
        }

        /// <summary>
        /// 测试：实验性功能在开发环境应包含节点
        /// </summary>
        [Fact]
        public void ShouldInclude_WhenExperimentalAndDevelopment_ShouldReturnTrue()
        {
            // 安排
            var node = new NavigationNode("test", "Test", "/test")
            {
                IsExperimental = true
            };

            var context = new NavigationFilterContext
            {
                IsDevelopment = true
            };

            // 执行
            var result = _filter.ShouldInclude(node, context);

            // 断言
            Assert.True(result);
        }

        /// <summary>
        /// 测试：实验性功能在生产环境应排除节点
        /// </summary>
        [Fact]
        public void ShouldInclude_WhenExperimentalAndNotDevelopment_ShouldReturnFalse()
        {
            // 安排
            var node = new NavigationNode("test", "Test", "/test")
            {
                IsExperimental = true
            };

            var context = new NavigationFilterContext
            {
                IsDevelopment = false
            };

            // 执行
            var result = _filter.ShouldInclude(node, context);

            // 断言
            Assert.False(result);
        }

        /// <summary>
        /// 测试：非实验性功能应包含节点
        /// </summary>
        [Fact]
        public void ShouldInclude_WhenNotExperimental_ShouldReturnTrue()
        {
            // 安排
            var node = new NavigationNode("test", "Test", "/test")
            {
                IsExperimental = false
            };

            var context = new NavigationFilterContext
            {
                IsDevelopment = false
            };

            // 执行
            var result = _filter.ShouldInclude(node, context);

            // 断言
            Assert.True(result);
        }

        /// <summary>
        /// 测试：优先级应为6
        /// </summary>
        [Fact]
        public void Priority_ShouldBe6()
        {
            // 断言
            Assert.Equal(6, _filter.Priority);
        }
    }
}
