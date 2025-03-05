using CodeSpirit.Authorization.Extensions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CodeSpirit.Authorization.Tests.Services
{
    /// <summary>
    /// PermissionService 类的单元测试
    /// </summary>
    public class PermissionServiceTests
    {
        /// <summary>
        /// 测试 HasPermission 方法，当用户拥有完全匹配的权限时，应返回 true
        /// </summary>
        [Fact]
        public void HasPermission_WhenExactPermissionExists_ReturnsTrue()
        {
            // Arrange
            var mockServiceProvider = new Mock<IServiceProvider>();
            var mockCache = new Mock<IDistributedCache>();
            var mockLogger = new Mock<ILogger<PermissionService>>();

            var permissionService = new PermissionService(
                mockServiceProvider.Object,
                mockCache.Object,
                mockLogger.Object);

            var permissionName = "module_controller_action";
            var userPermissions = new HashSet<string> { "module_controller_action" };

            // Act
            var result = permissionService.HasPermission(permissionName, userPermissions);

            // Assert
            Assert.True(result);
        }

        /// <summary>
        /// 测试 HasPermission 方法，当用户拥有父级权限时，应返回 true
        /// </summary>
        [Fact]
        public void HasPermission_WhenParentPermissionExists_ReturnsTrue()
        {
            // Arrange
            var mockServiceProvider = new Mock<IServiceProvider>();
            var mockCache = new Mock<IDistributedCache>();
            var mockLogger = new Mock<ILogger<PermissionService>>();

            var permissionService = new PermissionService(
                mockServiceProvider.Object,
                mockCache.Object,
                mockLogger.Object);

            var permissionName = "module_controller_action";

            // 用户拥有控制器级别的权限
            var userPermissions = new HashSet<string> { "module_controller" };

            // Act
            var result = permissionService.HasPermission(permissionName, userPermissions);

            // Assert
            Assert.True(result);
        }

        /// <summary>
        /// 测试 HasPermission 方法，当用户只拥有模块级权限时，应返回 true。
        /// 验证权限继承机制：模块级权限应当能够访问该模块下所有子权限。
        /// </summary>
        [Fact]
        public void HasPermission_WhenModuleLevelPermissionExists_ReturnsTrue()
        {
            // Arrange
            var mockServiceProvider = new Mock<IServiceProvider>();
            var mockCache = new Mock<IDistributedCache>();
            var mockLogger = new Mock<ILogger<PermissionService>>();

            var permissionService = new PermissionService(
                mockServiceProvider.Object,
                mockCache.Object,
                mockLogger.Object);

            // 需要验证的完整权限名称（模块_控制器_操作）
            var permissionName = "module_controller_action";

            // 用户只拥有模块级别的权限（只有"module"，没有具体控制器或操作的权限）
            // 测试目的：验证模块级权限可以授权访问该模块下的所有功能
            var userPermissions = new HashSet<string> { "module" };

            // Act
            var result = permissionService.HasPermission(permissionName, userPermissions);

            // Assert
            // 期望结果为True，因为模块级权限应当能够访问该模块下的所有权限
            Assert.True(result);
        }

        /// <summary>
        /// 测试 HasPermission 方法，当用户没有相关权限时，应返回 false
        /// </summary>
        [Fact]
        public void HasPermission_WhenNoRelevantPermissionExists_ReturnsFalse()
        {
            // Arrange
            var mockServiceProvider = new Mock<IServiceProvider>();
            var mockCache = new Mock<IDistributedCache>();
            var mockLogger = new Mock<ILogger<PermissionService>>();

            var permissionService = new PermissionService(
                mockServiceProvider.Object,
                mockCache.Object,
                mockLogger.Object);

            var permissionName = "module_controller_action";

            // 用户拥有其他权限
            var userPermissions = new HashSet<string> {
                "other_module",
                "other_controller_action",
                "module_other_action"
            };

            // Act
            var result = permissionService.HasPermission(permissionName, userPermissions);

            // Assert
            Assert.False(result);
        }

        /// <summary>
        /// 测试 HasPermission 方法，当权限名称格式不正确时，应返回 false
        /// </summary>
        [Fact]
        public void HasPermission_WhenInvalidPermissionNameFormat_ReturnsFalse()
        {
            // Arrange
            var mockServiceProvider = new Mock<IServiceProvider>();
            var mockCache = new Mock<IDistributedCache>();
            var mockLogger = new Mock<ILogger<PermissionService>>();

            var permissionService = new PermissionService(
                mockServiceProvider.Object,
                mockCache.Object,
                mockLogger.Object);

            // 无效的权限名称格式（没有下划线分隔）
            var permissionName = "invalidpermission";
            var userPermissions = new HashSet<string> { "module" };

            // Act
            var result = permissionService.HasPermission(permissionName, userPermissions);

            // Assert
            Assert.False(result);
        }

        /// <summary>
        /// 测试 HasPermission 方法，当用户权限集合为空时，应返回 false
        /// </summary>
        [Fact]
        public void HasPermission_WhenUserPermissionsEmpty_ReturnsFalse()
        {
            // Arrange
            var mockServiceProvider = new Mock<IServiceProvider>();
            var mockCache = new Mock<IDistributedCache>();
            var mockLogger = new Mock<ILogger<PermissionService>>();

            var permissionService = new PermissionService(
                mockServiceProvider.Object,
                mockCache.Object,
                mockLogger.Object);

            var permissionName = "module_controller_action";
            var userPermissions = new HashSet<string>(); // 空权限集合

            // Act
            var result = permissionService.HasPermission(permissionName, userPermissions);

            // Assert
            Assert.False(result);
        }

        /// <summary>
        /// 测试 HasPermission 方法，当权限名称为空时，应返回 false
        /// </summary>
        [Fact]
        public void HasPermission_WhenPermissionNameIsEmpty_ReturnsFalse()
        {
            // Arrange
            var mockServiceProvider = new Mock<IServiceProvider>();
            var mockCache = new Mock<IDistributedCache>();
            var mockLogger = new Mock<ILogger<PermissionService>>();

            var permissionService = new PermissionService(
                mockServiceProvider.Object,
                mockCache.Object,
                mockLogger.Object);

            var permissionName = string.Empty; // 空权限名称
            var userPermissions = new HashSet<string> { "module_controller" };

            // Act
            var result = permissionService.HasPermission(permissionName, userPermissions);

            // Assert
            Assert.False(result);
        }

        /// <summary>
        /// 测试 HasPermission 方法，当权限名称为 null 时，应返回 false
        /// </summary>
        [Fact]
        public void HasPermission_WhenPermissionNameIsNull_ReturnsFalse()
        {
            // Arrange
            var mockServiceProvider = new Mock<IServiceProvider>();
            var mockCache = new Mock<IDistributedCache>();
            var mockLogger = new Mock<ILogger<PermissionService>>();

            var permissionService = new PermissionService(
                mockServiceProvider.Object,
                mockCache.Object,
                mockLogger.Object);

            string permissionName = null; // null 权限名称
            var userPermissions = new HashSet<string> { "module_controller" };

            // Act
            var result = permissionService.HasPermission(permissionName, userPermissions);

            // Assert
            Assert.False(result);
        }

        /// <summary>
        /// 测试 HasPermission 方法，当权限层级较深且用户有中间层级权限时，应返回 true
        /// </summary>
        [Fact]
        public void HasPermission_WhenDeepLevelHierarchyWithMiddlePermission_ReturnsTrue()
        {
            // Arrange
            var mockServiceProvider = new Mock<IServiceProvider>();
            var mockCache = new Mock<IDistributedCache>();
            var mockLogger = new Mock<ILogger<PermissionService>>();

            var permissionService = new PermissionService(
                mockServiceProvider.Object,
                mockCache.Object,
                mockLogger.Object);

            // 深层次权限，例如：模块_控制器_分组_子分组_动作
            var permissionName = "module_controller_group_subgroup_action";

            // 用户拥有中间层级的权限
            var userPermissions = new HashSet<string> { "module_controller_group" };

            // Act
            var result = permissionService.HasPermission(permissionName, userPermissions);

            // Assert
            Assert.True(result);
        }

        /// <summary>
        /// 测试 HasPermission 方法，当权限有多级且用户只拥有最高层级权限时，应返回 true
        /// </summary>
        [Fact]
        public void HasPermission_WhenMultiLevelPermissionWithTopLevelAccess_ReturnsTrue()
        {
            // Arrange
            var mockServiceProvider = new Mock<IServiceProvider>();
            var mockCache = new Mock<IDistributedCache>();
            var mockLogger = new Mock<ILogger<PermissionService>>();

            var permissionService = new PermissionService(
                mockServiceProvider.Object,
                mockCache.Object,
                mockLogger.Object);

            // 多级权限名称
            var permissionName = "module_controller_group_action";

            // 用户拥有顶级权限
            var userPermissions = new HashSet<string> { "module" };

            // Act
            var result = permissionService.HasPermission(permissionName, userPermissions);

            // Assert
            Assert.True(result);
        }

        /// <summary>
        /// 测试 HasPermission 方法，当权限名称中包含多个下划线时，应正确识别层级关系
        /// </summary>
        [Fact]
        public void HasPermission_WhenPermissionHasMultipleUnderscores_HandlesCorrectly()
        {
            // Arrange
            var mockServiceProvider = new Mock<IServiceProvider>();
            var mockCache = new Mock<IDistributedCache>();
            var mockLogger = new Mock<ILogger<PermissionService>>();

            var permissionService = new PermissionService(
                mockServiceProvider.Object,
                mockCache.Object,
                mockLogger.Object);

            // 包含多个下划线的权限名称
            var permissionName = "module_controller_action_with_multiple_underscores";

            // 用户拥有控制器级别权限
            var userPermissions = new HashSet<string> { "module_controller" };

            // Act
            var result = permissionService.HasPermission(permissionName, userPermissions);

            // Assert
            Assert.True(result);
        }

        /// <summary>
        /// 测试 HasPermission 方法，当权限名称中包含特殊字符时的处理
        /// </summary>
        [Fact]
        public void HasPermission_WhenPermissionHasSpecialCharacters_HandlesCorrectly()
        {
            // Arrange
            var mockServiceProvider = new Mock<IServiceProvider>();
            var mockCache = new Mock<IDistributedCache>();
            var mockLogger = new Mock<ILogger<PermissionService>>();

            var permissionService = new PermissionService(
                mockServiceProvider.Object,
                mockCache.Object,
                mockLogger.Object);

            // 包含特殊字符的权限名称
            var permissionName = "module-name_controller-name_action-name";

            // 用户拥有直接匹配的权限
            var userPermissions = new HashSet<string> { "module-name_controller-name_action-name" };

            // Act
            var result = permissionService.HasPermission(permissionName, userPermissions);

            // Assert
            Assert.True(result);
        }

        /// <summary>
        /// 测试 HasPermission 方法，当权限名称仅为模块名且用户拥有该模块权限时，应返回 true
        /// </summary>
        [Fact]
        public void HasPermission_WhenPermissionIsModuleNameOnly_ReturnsTrue()
        {
            // Arrange
            var mockServiceProvider = new Mock<IServiceProvider>();
            var mockCache = new Mock<IDistributedCache>();
            var mockLogger = new Mock<ILogger<PermissionService>>();

            var permissionService = new PermissionService(
                mockServiceProvider.Object,
                mockCache.Object,
                mockLogger.Object);

            // 仅模块名的权限
            var permissionName = "module";

            // 用户拥有该模块权限
            var userPermissions = new HashSet<string> { "module" };

            // Act
            var result = permissionService.HasPermission(permissionName, userPermissions);

            // Assert
            Assert.True(result);
        }

        /// <summary>
        /// 测试 HasPermission 方法，当尝试检查模块权限但用户只有该模块下的某个具体权限时，应返回 false
        /// </summary>
        [Fact]
        public void HasPermission_WhenCheckingModulePermissionButUserHasOnlySpecificAction_ReturnsFalse()
        {
            // Arrange
            var mockServiceProvider = new Mock<IServiceProvider>();
            var mockCache = new Mock<IDistributedCache>();
            var mockLogger = new Mock<ILogger<PermissionService>>();

            var permissionService = new PermissionService(
                mockServiceProvider.Object,
                mockCache.Object,
                mockLogger.Object);

            // 检查模块级权限
            var permissionName = "module";

            // 用户只有模块下的具体操作权限，但没有模块级权限
            var userPermissions = new HashSet<string> { "module_controller_action" };

            // Act
            var result = permissionService.HasPermission(permissionName, userPermissions);

            // Assert
            // 期望结果为False，因为权限继承是自上而下的，不是自下而上的
            Assert.False(result);
        }

        /// <summary>
        /// 测试 HasPermission 方法，验证权限名称比较是否区分大小写
        /// </summary>
        [Fact]
        public void HasPermission_CaseSensitivity_WorksAsExpected()
        {
            // Arrange
            var mockServiceProvider = new Mock<IServiceProvider>();
            var mockCache = new Mock<IDistributedCache>();
            var mockLogger = new Mock<ILogger<PermissionService>>();

            var permissionService = new PermissionService(
                mockServiceProvider.Object,
                mockCache.Object,
                mockLogger.Object);

            var permissionName = "Module_Controller_Action";
            
            // 用户权限采用小写形式
            var userPermissions = new HashSet<string> { "module_controller_action" };

            // Act
            var result = permissionService.HasPermission(permissionName, userPermissions);

            // Assert
            // 由于默认实现是使用字符串直接比较，应该区分大小写
            Assert.False(result);
        }
    }
}