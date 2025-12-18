using CodeSpirit.Core.Authorization;
using CodeSpirit.Navigation.Models;
using CodeSpirit.Navigation.Services;
using CodeSpirit.Navigation.Services.Filters;
using Moq;

namespace CodeSpirit.Navigation.Tests.Filters
{
    /// <summary>
    /// PermissionFilter 单元测试
    /// </summary>
    public class PermissionFilterTests
    {
        private readonly PermissionFilter _filter;

        public PermissionFilterTests()
        {
            _filter = new PermissionFilter();
        }

        /// <summary>
        /// 测试：没有权限要求时应包含节点
        /// </summary>
        [Fact]
        public void ShouldInclude_WhenNoPermissionRequired_ShouldReturnTrue()
        {
            // 安排
            var node = new NavigationNode("test", "Test", "/test")
            {
                Permission = null
            };

            var context = new NavigationFilterContext();

            // 执行
            var result = _filter.ShouldInclude(node, context);

            // 断言
            Assert.True(result);
        }

        /// <summary>
        /// 测试：有权限要求且用户有权限时应包含节点
        /// </summary>
        [Fact]
        public void ShouldInclude_WhenUserHasPermission_ShouldReturnTrue()
        {
            // 安排
            var node = new NavigationNode("test", "Test", "/test")
            {
                Permission = "test_permission"
            };

            var permissionServiceMock = new Mock<IHasPermissionService>();
            permissionServiceMock.Setup(x => x.HasNavigationPermission("test_permission"))
                .Returns(true);

            var context = new NavigationFilterContext
            {
                PermissionService = permissionServiceMock.Object
            };

            // 执行
            var result = _filter.ShouldInclude(node, context);

            // 断言
            Assert.True(result);
        }

        /// <summary>
        /// 测试：有权限要求但用户没有权限时应排除节点
        /// </summary>
        [Fact]
        public void ShouldInclude_WhenUserLacksPermission_ShouldReturnFalse()
        {
            // 安排
            var node = new NavigationNode("test", "Test", "/test")
            {
                Permission = "test_permission"
            };

            var permissionServiceMock = new Mock<IHasPermissionService>();
            permissionServiceMock.Setup(x => x.HasNavigationPermission("test_permission"))
                .Returns(false);

            var context = new NavigationFilterContext
            {
                PermissionService = permissionServiceMock.Object
            };

            // 执行
            var result = _filter.ShouldInclude(node, context);

            // 断言
            Assert.False(result);
        }

        /// <summary>
        /// 测试：没有权限服务时应包含节点（由调用方决定）
        /// </summary>
        [Fact]
        public void ShouldInclude_WhenNoPermissionService_ShouldReturnTrue()
        {
            // 安排
            var node = new NavigationNode("test", "Test", "/test")
            {
                Permission = "test_permission"
            };

            var context = new NavigationFilterContext
            {
                PermissionService = null
            };

            // 执行
            var result = _filter.ShouldInclude(node, context);

            // 断言
            Assert.True(result);
        }

        /// <summary>
        /// 测试：优先级应为2
        /// </summary>
        [Fact]
        public void Priority_ShouldBe2()
        {
            // 断言
            Assert.Equal(2, _filter.Priority);
        }
    }
}
