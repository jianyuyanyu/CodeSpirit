using CodeSpirit.Navigation.Models;
using CodeSpirit.Navigation.Services;
using CodeSpirit.Navigation.Services.Filters;

namespace CodeSpirit.Navigation.Tests.Filters
{
    /// <summary>
    /// VersionFilter 单元测试
    /// </summary>
    public class VersionFilterTests
    {
        private readonly VersionFilter _filter;

        public VersionFilterTests()
        {
            _filter = new VersionFilter();
        }

        /// <summary>
        /// 测试：没有版本约束时应包含节点
        /// </summary>
        [Fact]
        public void ShouldInclude_WhenNoVersionConstraint_ShouldReturnTrue()
        {
            // 安排
            var node = new NavigationNode("test", "Test", "/test");
            var context = new NavigationFilterContext
            {
                CurrentVersion = null
            };

            // 执行
            var result = _filter.ShouldInclude(node, context);

            // 断言
            Assert.True(result);
        }

        /// <summary>
        /// 测试：版本在范围内时应包含节点
        /// </summary>
        [Theory]
        [InlineData("1.0.0", "1.0.0", "2.0.0", true)]
        [InlineData("1.5.0", "1.0.0", "2.0.0", true)]
        [InlineData("2.0.0", "1.0.0", "2.0.0", true)]
        [InlineData("0.9.0", "1.0.0", "2.0.0", false)]
        [InlineData("2.1.0", "1.0.0", "2.0.0", false)]
        public void ShouldInclude_WithVersionRange_ReturnsExpected(
            string currentVersion,
            string minVersion,
            string maxVersion,
            bool expected)
        {
            // 安排
            var node = new NavigationNode("test", "Test", "/test")
            {
                MinVersion = minVersion,
                MaxVersion = maxVersion
            };

            var context = new NavigationFilterContext
            {
                CurrentVersion = currentVersion
            };

            // 执行
            var result = _filter.ShouldInclude(node, context);

            // 断言
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// 测试：只有最小版本时应正确检查
        /// </summary>
        [Fact]
        public void ShouldInclude_WithOnlyMinVersion_ShouldCheckMinVersion()
        {
            // 安排
            var node = new NavigationNode("test", "Test", "/test")
            {
                MinVersion = "1.0.0"
            };

            var context = new NavigationFilterContext
            {
                CurrentVersion = "0.9.0"
            };

            // 执行
            var result = _filter.ShouldInclude(node, context);

            // 断言
            Assert.False(result);
        }

        /// <summary>
        /// 测试：优先级应为4
        /// </summary>
        [Fact]
        public void Priority_ShouldBe4()
        {
            // 断言
            Assert.Equal(4, _filter.Priority);
        }
    }
}
