using CodeSpirit.Navigation.Models;
using CodeSpirit.Navigation.Services;
using CodeSpirit.Navigation.Services.Filters;

namespace CodeSpirit.Navigation.Tests.Filters
{
    /// <summary>
    /// DeviceFilter 单元测试
    /// </summary>
    public class DeviceFilterTests
    {
        private readonly DeviceFilter _filter;

        public DeviceFilterTests()
        {
            _filter = new DeviceFilter();
        }

        /// <summary>
        /// 测试：没有设备限制时应包含节点
        /// </summary>
        [Fact]
        public void ShouldInclude_WhenNoDeviceRestriction_ShouldReturnTrue()
        {
            // 安排
            var node = new NavigationNode("test", "Test", "/test")
            {
                SupportedDevices = null
            };

            var context = new NavigationFilterContext
            {
                DeviceType = "desktop"
            };

            // 执行
            var result = _filter.ShouldInclude(node, context);

            // 断言
            Assert.True(result);
        }

        /// <summary>
        /// 测试：设备类型匹配时应包含节点
        /// </summary>
        [Fact]
        public void ShouldInclude_WhenDeviceTypeMatches_ShouldReturnTrue()
        {
            // 安排
            var node = new NavigationNode("test", "Test", "/test")
            {
                SupportedDevices = new[] { "desktop", "tablet", "mobile" }
            };

            var context = new NavigationFilterContext
            {
                DeviceType = "desktop"
            };

            // 执行
            var result = _filter.ShouldInclude(node, context);

            // 断言
            Assert.True(result);
        }

        /// <summary>
        /// 测试：设备类型不匹配时应排除节点
        /// </summary>
        [Fact]
        public void ShouldInclude_WhenDeviceTypeDoesNotMatch_ShouldReturnFalse()
        {
            // 安排
            var node = new NavigationNode("test", "Test", "/test")
            {
                SupportedDevices = new[] { "desktop", "tablet" }
            };

            var context = new NavigationFilterContext
            {
                DeviceType = "mobile"
            };

            // 执行
            var result = _filter.ShouldInclude(node, context);

            // 断言
            Assert.False(result);
        }

        /// <summary>
        /// 测试：没有指定设备类型时应包含节点
        /// </summary>
        [Fact]
        public void ShouldInclude_WhenNoDeviceTypeSpecified_ShouldReturnTrue()
        {
            // 安排
            var node = new NavigationNode("test", "Test", "/test")
            {
                SupportedDevices = new[] { "desktop" }
            };

            var context = new NavigationFilterContext
            {
                DeviceType = null
            };

            // 执行
            var result = _filter.ShouldInclude(node, context);

            // 断言
            Assert.True(result);
        }

        /// <summary>
        /// 测试：优先级应为5
        /// </summary>
        [Fact]
        public void Priority_ShouldBe5()
        {
            // 断言
            Assert.Equal(5, _filter.Priority);
        }
    }
}
