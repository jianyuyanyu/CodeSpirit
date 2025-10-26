using CodeSpirit.Authorization.Services;
using CodeSpirit.Authorization;
using CodeSpirit.Core;
using CodeSpirit.Core.Authorization;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;
using Xunit;
using System;

namespace CodeSpirit.Authorization.Tests
{
    /// <summary>
    /// HasPermissionService 的单元测试
    /// </summary>
    public class HasPermissionServiceTests
    {
        private readonly Mock<ILogger<HasPermissionService>> _mockLogger;
        private readonly Mock<IPermissionService> _mockPermissionService;
        private readonly Mock<ICurrentUser> _mockCurrentUser;
        
        // 考试系统权限列表
        private readonly HashSet<string> _examPermissions = new()
        {
            "exam_examPapers_createExamPaper",
            "exam_examPapers_deleteExamPaper",
            "exam_examPapers_publishExamPaper",
            "exam_examPapers_unpublishExamPaper",
            "exam_examPapers_generateRandomExamPaper",
            "exam_examPapers_copyExamPaper",
            "exam_examPapers_getSelectList",
            "exam_examPapers_previewExamPaper",
            "exam_examPapers_examSettings_Manager",
            "exam_examPapers_getExamQuestionsPreviewConfig",
            "exam_examPapers_getExamPaper"
        };

        public HasPermissionServiceTests()
        {
            _mockLogger = new Mock<ILogger<HasPermissionService>>();
            _mockPermissionService = new Mock<IPermissionService>();
            _mockCurrentUser = new Mock<ICurrentUser>();
        }

        /// <summary>
        /// 测试用户未认证时的权限检查
        /// </summary>
        [Fact]
        public void HasPermission_UserNotAuthenticated_ReturnsFalse()
        {
            // 安排
            _mockCurrentUser.SetupGet(u => u.IsAuthenticated).Returns(false);
            var service = new HasPermissionService(_mockLogger.Object, _mockPermissionService.Object, _mockCurrentUser.Object);

            // 执行
            bool result = service.HasPermission("exam_examPapers_createExamPaper");

            // 断言
            Assert.False(result);
        }

        /// <summary>
        /// 测试管理员用户对考试系统权限的访问
        /// </summary>
        [Fact]
        public void HasPermission_AdminUser_ReturnsTrue()
        {
            // 安排
            _mockCurrentUser.SetupGet(u => u.IsAuthenticated).Returns(true);
            _mockCurrentUser.SetupGet(u => u.Roles).Returns(new[] { "Admin" });
            var service = new HasPermissionService(_mockLogger.Object, _mockPermissionService.Object, _mockCurrentUser.Object);

            // 执行
            bool result = service.HasPermission("exam_examPapers_createExamPaper");

            // 断言
            Assert.True(result);
            
            // 验证没有调用权限服务检查
            _mockPermissionService.Verify(p => p.HasPermission(It.IsAny<string>(), It.IsAny<ISet<string>>()), Times.Never);
        }

        /// <summary>
        /// 测试普通用户拥有指定考试系统权限时的访问
        /// </summary>
        [Fact]
        public void HasPermission_RegularUserWithPermission_ReturnsTrue()
        {
            // 安排
            _mockCurrentUser.SetupGet(u => u.IsAuthenticated).Returns(true);
            _mockCurrentUser.SetupGet(u => u.Roles).Returns(new[] { "User" });
            _mockCurrentUser.SetupGet(u => u.Permissions).Returns(_examPermissions);
            _mockPermissionService.Setup(p => p.HasPermission("exam_examPapers_createExamPaper", It.IsAny<ISet<string>>()))
                .Returns(true);
            
            var service = new HasPermissionService(_mockLogger.Object, _mockPermissionService.Object, _mockCurrentUser.Object);

            // 执行
            bool result = service.HasPermission("exam_examPapers_createExamPaper");

            // 断言
            Assert.True(result);
            
            // 验证调用了权限服务检查
            _mockPermissionService.Verify(p => p.HasPermission("exam_examPapers_createExamPaper", It.IsAny<ISet<string>>()), Times.Once);
        }

        /// <summary>
        /// 测试普通用户没有指定考试系统权限时的访问
        /// </summary>
        [Fact]
        public void HasPermission_RegularUserWithoutPermission_ReturnsFalse()
        {
            // 安排
            _mockCurrentUser.SetupGet(u => u.IsAuthenticated).Returns(true);
            _mockCurrentUser.SetupGet(u => u.Roles).Returns(new[] { "User" });
            _mockCurrentUser.SetupGet(u => u.Permissions).Returns(new HashSet<string>());
            _mockPermissionService.Setup(p => p.HasPermission("exam_examPapers_createExamPaper", It.IsAny<ISet<string>>()))
                .Returns(false);
            
            var service = new HasPermissionService(_mockLogger.Object, _mockPermissionService.Object, _mockCurrentUser.Object);

            // 执行
            bool result = service.HasPermission("exam_examPapers_createExamPaper");

            // 断言
            Assert.False(result);
            
            // 验证调用了权限服务检查
            _mockPermissionService.Verify(p => p.HasPermission("exam_examPapers_createExamPaper", It.IsAny<ISet<string>>()), Times.Once);
        }

        /// <summary>
        /// 测试用户对所有考试系统权限的访问
        /// </summary>
        [Theory]
        [InlineData("exam_examPapers_createExamPaper")]
        [InlineData("exam_examPapers_deleteExamPaper")]
        [InlineData("exam_examPapers_publishExamPaper")]
        [InlineData("exam_examPapers_unpublishExamPaper")]
        [InlineData("exam_examPapers_generateRandomExamPaper")]
        [InlineData("exam_examPapers_copyExamPaper")]
        [InlineData("exam_examPapers_getSelectList")]
        [InlineData("exam_examPapers_previewExamPaper")]
        [InlineData("exam_examPapers_examSettings_Manager")]
        [InlineData("exam_examPapers_getExamQuestionsPreviewConfig")]
        [InlineData("exam_examPapers_getExamPaper")]
        public void HasPermission_AllExamPermissions_ReturnsTrue(string permissionCode)
        {
            // 安排
            _mockCurrentUser.SetupGet(u => u.IsAuthenticated).Returns(true);
            _mockCurrentUser.SetupGet(u => u.Roles).Returns(new[] { "User" });
            _mockCurrentUser.SetupGet(u => u.Permissions).Returns(_examPermissions);
            _mockPermissionService.Setup(p => p.HasPermission(It.IsAny<string>(), It.IsAny<ISet<string>>()))
                .Returns((string permission, ISet<string> permissions) => 
                    permissions.Contains(permission));
            
            var service = new HasPermissionService(_mockLogger.Object, _mockPermissionService.Object, _mockCurrentUser.Object);

            // 执行
            bool result = service.HasPermission(permissionCode);

            // 断言
            Assert.True(result);
        }

        /// <summary>
        /// 测试菜单权限检查 - 确保用户可以访问考试中心父级菜单
        /// </summary>
        [Fact]
        public void HasPermission_ExamCenterMenu_ReturnsTrue()
        {
            // 安排
            _mockCurrentUser.SetupGet(u => u.IsAuthenticated).Returns(true);
            _mockCurrentUser.SetupGet(u => u.Roles).Returns(new[] { "User" });
            _mockCurrentUser.SetupGet(u => u.Permissions).Returns(_examPermissions);
            
            // 模拟权限服务 - 如果用户有任何exam_examPapers_前缀的权限，则允许访问exam_examPapers
            _mockPermissionService.Setup(p => p.HasPermission("exam", It.IsAny<ISet<string>>()))
                .Returns((string permission, ISet<string> permissions) => 
                    permissions.Any(p => p.StartsWith("exam_")));
            
            _mockPermissionService.Setup(p => p.HasPermission("exam_examPapers", It.IsAny<ISet<string>>()))
                .Returns((string permission, ISet<string> permissions) => 
                    permissions.Any(p => p.StartsWith("exam_examPapers_")));
            
            var service = new HasPermissionService(_mockLogger.Object, _mockPermissionService.Object, _mockCurrentUser.Object);

            // 执行
            bool examCenterResult = service.HasPermission("exam");
            bool examPapersResult = service.HasPermission("exam_examPapers");

            // 断言
            Assert.True(examCenterResult, "用户应该能够访问考试中心菜单");
            Assert.True(examPapersResult, "用户应该能够访问试卷管理菜单");
        }

        /// <summary>
        /// 测试考试系统实际场景 - 用户只有部分试卷管理权限
        /// </summary>
        [Fact]
        public void HasPermission_ExamSystemRealScenario_ShouldWorkAsExpected()
        {
            // 安排 - 用户只有部分试卷管理相关权限
            var limitedPermissions = new HashSet<string>
            {
                "exam_examPapers_getExamPaper",
                "exam_examPapers_previewExamPaper",
                "exam_examPapers_getSelectList"
            };
            
            _mockCurrentUser.SetupGet(u => u.IsAuthenticated).Returns(true);
            _mockCurrentUser.SetupGet(u => u.Roles).Returns(new[] { "User" });
            _mockCurrentUser.SetupGet(u => u.Permissions).Returns(limitedPermissions);
            
            // 设置权限检查逻辑
            _mockPermissionService
                .Setup(p => p.HasPermission(It.IsAny<string>(), It.IsAny<ISet<string>>()))
                .Returns((string permission, ISet<string> permissions) => {
                    // 直接权限检查
                    if (permissions.Contains(permission))
                        return true;
                    
                    // 验证考试中心导航权限
                    if (permission == "exam" && permissions.Any(p => p.StartsWith("exam_")))
                        return true;
                    
                    // 验证试卷管理导航权限
                    if (permission == "exam_examPapers" && permissions.Any(p => p.StartsWith("exam_examPapers_")))
                        return true;
                        
                    return false;
                });
            
            var service = new HasPermissionService(_mockLogger.Object, _mockPermissionService.Object, _mockCurrentUser.Object);

            // 执行和断言
            
            // 1. 顶级导航菜单 - 考试中心
            Assert.True(service.HasPermission("exam"), "用户应该能够访问考试中心菜单");
            
            // 2. 二级导航菜单 - 试卷管理
            Assert.True(service.HasPermission("exam_examPapers"), "用户应该能够访问试卷管理菜单");
            
            // 3. 试卷管理具体操作权限
            Assert.True(service.HasPermission("exam_examPapers_getExamPaper"), "用户应该能够获取试卷");
            Assert.True(service.HasPermission("exam_examPapers_previewExamPaper"), "用户应该能够预览试卷");
            Assert.True(service.HasPermission("exam_examPapers_getSelectList"), "用户应该能够获取选择列表");
            
            // 4. 无权限的操作
            Assert.False(service.HasPermission("exam_examPapers_createExamPaper"), "用户不应该能够创建试卷");
            Assert.False(service.HasPermission("exam_examPapers_deleteExamPaper"), "用户不应该能够删除试卷");
            Assert.False(service.HasPermission("exam_examPapers_publishExamPaper"), "用户不应该能够发布试卷");
            
            // 5. 其他模块菜单
            Assert.False(service.HasPermission("exam_examRecords"), "用户不应该能够访问考试记录菜单");
            Assert.False(service.HasPermission("exam_examSettings"), "用户不应该能够访问考试设置菜单");
            
            // 验证调用权限服务的次数
            _mockPermissionService.Verify(
                p => p.HasPermission(It.IsAny<string>(), It.IsAny<ISet<string>>()),
                Times.Exactly(10));
        }

        /// <summary>
        /// 测试默认权限 - 以default_前缀开头的权限应自动允许访问
        /// </summary>
        [Fact]
        public void HasPermission_DefaultPermissionPrefix_ReturnsTrue()
        {
            // 安排
            _mockCurrentUser.SetupGet(u => u.IsAuthenticated).Returns(true);
            _mockCurrentUser.SetupGet(u => u.Roles).Returns(new[] { "User" });
            _mockCurrentUser.SetupGet(u => u.Permissions).Returns(new HashSet<string>());
            
            // 模拟权限服务 - 对于default_前缀的权限自动返回true
            _mockPermissionService
                .Setup(p => p.HasPermission(It.Is<string>(s => s.StartsWith("default_", StringComparison.OrdinalIgnoreCase)), It.IsAny<ISet<string>>()))
                .Returns(true);
            
            var service = new HasPermissionService(_mockLogger.Object, _mockPermissionService.Object, _mockCurrentUser.Object);

            // 执行
            bool result1 = service.HasPermission("default_home_index");
            bool result2 = service.HasPermission("DEFAULT_profile_get");
            
            // 断言
            Assert.True(result1, "以default_前缀的权限应自动允许访问");
            Assert.True(result2, "以DEFAULT_前缀的权限应自动允许访问（不区分大小写）");
        }

        /// <summary>
        /// 测试空权限名称或空权限集合情况
        /// </summary>
        [Fact]
        public void HasPermission_EmptyPermissionOrNullCollection_ReturnsFalse()
        {
            // 安排
            _mockCurrentUser.SetupGet(u => u.IsAuthenticated).Returns(true);
            _mockCurrentUser.SetupGet(u => u.Roles).Returns(new[] { "User" });
            _mockCurrentUser.SetupGet(u => u.Permissions).Returns(_examPermissions);
            
            // 模拟权限服务对空权限名称或空权限集合的响应
            _mockPermissionService
                .Setup(p => p.HasPermission(null, It.IsAny<ISet<string>>()))
                .Returns(false);
                
            _mockPermissionService
                .Setup(p => p.HasPermission("", It.IsAny<ISet<string>>()))
                .Returns(false);
                
            _mockPermissionService
                .Setup(p => p.HasPermission(It.IsAny<string>(), null))
                .Returns(false);
            
            var service = new HasPermissionService(_mockLogger.Object, _mockPermissionService.Object, _mockCurrentUser.Object);

            // 执行
            bool nullPermissionResult = service.HasPermission(null);
            bool emptyPermissionResult = service.HasPermission("");
            
            // 断言
            Assert.False(nullPermissionResult, "空权限名称应返回false");
            Assert.False(emptyPermissionResult, "空权限名称应返回false");
        }

        /// <summary>
        /// 测试权限继承逻辑 - 父级权限应允许访问子级权限
        /// </summary>
        [Fact]
        public void HasPermission_ParentPermissionInheritance_ShouldWorkAsExpected()
        {
            // 安排
            _mockCurrentUser.SetupGet(u => u.IsAuthenticated).Returns(true);
            _mockCurrentUser.SetupGet(u => u.Roles).Returns(new[] { "User" });
            
            // 用户只有模块级和控制器级权限
            var hierarchyPermissions = new HashSet<string>
            {
                "exam",                // 模块权限
                "identity_users"       // 控制器权限
            };
            
            _mockCurrentUser.SetupGet(u => u.Permissions).Returns(hierarchyPermissions);
            
            // 模拟权限服务 - 模拟权限继承逻辑
            _mockPermissionService
                .Setup(p => p.HasPermission(It.IsAny<string>(), It.IsAny<ISet<string>>()))
                .Returns((string permission, ISet<string> permissions) => 
                {
                    // 直接匹配
                    if (permissions.Contains(permission))
                        return true;
                        
                    // 权限层级逻辑
                    var parts = permission.Split('_');
                    if (parts.Length < 2)
                        return false;
                        
                    // 模块级权限
                    if (permissions.Contains(parts[0]))
                        return true;
                        
                    // 控制器级权限
                    if (parts.Length >= 2 && permissions.Contains($"{parts[0]}_{parts[1]}"))
                        return true;
                        
                    return false;
                });
            
            var service = new HasPermissionService(_mockLogger.Object, _mockPermissionService.Object, _mockCurrentUser.Object);

            // 执行
            // 考试模块继承测试
            bool examModuleResult = service.HasPermission("exam");
            bool examPapersControllerResult = service.HasPermission("exam_examPapers");
            bool examPapersActionResult = service.HasPermission("exam_examPapers_getExamPaper");
            
            // 身份模块继承测试
            bool identityModuleResult = service.HasPermission("identity");
            bool identityUsersControllerResult = service.HasPermission("identity_users");
            bool identityUsersActionResult = service.HasPermission("identity_users_createUser");
            bool identityRolesActionResult = service.HasPermission("identity_roles_createRole");
            
            // 断言
            // 考试模块权限继承（用户拥有模块级权限）
            Assert.True(examModuleResult, "用户应能访问考试模块（直接拥有权限）");
            Assert.True(examPapersControllerResult, "用户应能访问试卷控制器（继承自模块权限）");
            Assert.True(examPapersActionResult, "用户应能执行试卷管理操作（继承自模块权限）");
            
            // 身份模块权限继承（用户拥有控制器级权限）
            Assert.False(identityModuleResult, "用户不应能访问身份模块（无模块级权限）");
            Assert.True(identityUsersControllerResult, "用户应能访问用户控制器（直接拥有权限）");
            Assert.True(identityUsersActionResult, "用户应能执行用户管理操作（继承自控制器权限）");
            Assert.False(identityRolesActionResult, "用户不应能执行角色管理操作（无相关权限）");
        }

        /// <summary>
        /// 测试权限区分大小写
        /// </summary>
        [Fact]
        public void HasPermission_CaseSensitivity_ShouldWorkAsExpected()
        {
            // 安排
            _mockCurrentUser.SetupGet(u => u.IsAuthenticated).Returns(true);
            _mockCurrentUser.SetupGet(u => u.Roles).Returns(new[] { "User" });
            
            // 用户权限（全小写）
            var permissions = new HashSet<string>
            {
                "exam_examppapers_getexampaper"
            };
            
            _mockCurrentUser.SetupGet(u => u.Permissions).Returns(permissions);
            
            // 模拟权限服务 - 区分大小写
            _mockPermissionService
                .Setup(p => p.HasPermission(It.IsAny<string>(), It.IsAny<ISet<string>>()))
                .Returns((string permission, ISet<string> userPermissions) => 
                    userPermissions.Contains(permission));
            
            var service = new HasPermissionService(_mockLogger.Object, _mockPermissionService.Object, _mockCurrentUser.Object);

            // 执行
            bool lowerCaseResult = service.HasPermission("exam_examppapers_getexampaper");
            bool mixedCaseResult = service.HasPermission("exam_examPapers_getExamPaper");
            bool upperCaseResult = service.HasPermission("EXAM_EXAMPAPERS_GETEXAMPAPER");
            
            // 断言
            Assert.True(lowerCaseResult, "小写权限代码应匹配成功");
            Assert.False(mixedCaseResult, "混合大小写权限代码应匹配失败");
            Assert.False(upperCaseResult, "大写权限代码应匹配失败");
        }

        /// <summary>
        /// 测试导航权限功能 - 用户拥有三级权限时，能够自动获取二级导航权限，但不再获取一级权限
        /// </summary>
        [Fact]
        public void HasNavigationPermission_UserWithThreeLevelPermissions_ShouldExtractOnlySecondLevelPermissions()
        {
            // 安排
            _mockCurrentUser.SetupGet(u => u.IsAuthenticated).Returns(true);
            _mockCurrentUser.SetupGet(u => u.Roles).Returns(new[] { "User" });
            
            // 用户拥有三级权限
            var detailedPermissions = new HashSet<string>
            {
                "exam_examPapers_createExamPaper",
                "exam_examPapers_deleteExamPaper",
                "exam_examPapers_getExamPaper",
                "identity_users_createUser"
            };
            
            _mockCurrentUser.SetupGet(u => u.Permissions).Returns(detailedPermissions);
            
            // 模拟权限服务对导航权限的处理
            _mockPermissionService
                .Setup(p => p.HasPermission(It.IsAny<string>(), It.IsAny<ISet<string>>()))
                .Returns((string permission, ISet<string> permissions) => 
                {
                    // 检查导航权限是否在提取的权限集合中
                    return permissions.Contains(permission);
                });
            
            var service = new HasPermissionService(_mockLogger.Object, _mockPermissionService.Object, _mockCurrentUser.Object);

            // 执行
            bool examModuleResult = service.HasNavigationPermission("exam");
            bool examPapersResult = service.HasNavigationPermission("exam_examPapers");
            bool identityModuleResult = service.HasNavigationPermission("identity");
            bool identityUsersResult = service.HasNavigationPermission("identity_users");
            
            // 断言 - 一级菜单不再有权限，二级菜单有权限
            Assert.False(examModuleResult, "用户不应能访问考试模块导航（仅提取二级权限）");
            Assert.True(examPapersResult, "用户应能访问试卷管理导航");
            Assert.False(identityModuleResult, "用户不应能访问身份模块导航（仅提取二级权限）");
            Assert.True(identityUsersResult, "用户应能访问用户管理导航");
            
            // 验证调用，应提取出导航权限集合：["exam_examPapers", "identity_users"]
            _mockPermissionService.Verify(p => p.HasPermission("exam", It.Is<ISet<string>>(
                s => !s.Contains("exam") && s.Contains("exam_examPapers") && 
                     !s.Contains("identity") && s.Contains("identity_users"))), 
                Times.Once);
        }

        /// <summary>
        /// 测试权限继承逻辑 - 修改后只继承到二级权限，不再支持一级权限的访问
        /// </summary>
        [Fact]
        public void HasNavigationPermission_ParentPermissionInheritance_ShouldOnlyExtractSecondLevel()
        {
            // 安排
            _mockCurrentUser.SetupGet(u => u.IsAuthenticated).Returns(true);
            _mockCurrentUser.SetupGet(u => u.Roles).Returns(new[] { "User" });
            
            // 用户只有三级权限
            var hierarchyPermissions = new HashSet<string>
            {
                "exam_examPapers_createExamPaper",  // 三级权限
                "identity_users_createUser"         // 三级权限
            };
            
            _mockCurrentUser.SetupGet(u => u.Permissions).Returns(hierarchyPermissions);
            
            // 模拟权限服务 - 模拟只提取二级权限的逻辑
            _mockPermissionService
                .Setup(p => p.HasPermission(It.IsAny<string>(), It.IsAny<ISet<string>>()))
                .Returns((string permission, ISet<string> permissions) => 
                {
                    return permissions.Contains(permission);
                });
            
            var service = new HasPermissionService(_mockLogger.Object, _mockPermissionService.Object, _mockCurrentUser.Object);

            // 执行
            // 考试模块继承测试
            bool examModuleResult = service.HasNavigationPermission("exam");
            bool examPapersControllerResult = service.HasNavigationPermission("exam_examPapers");
            bool examPapersActionResult = service.HasNavigationPermission("exam_examPapers_createExamPaper");
            
            // 身份模块继承测试
            bool identityModuleResult = service.HasNavigationPermission("identity");
            bool identityUsersControllerResult = service.HasNavigationPermission("identity_users");
            bool identityUsersActionResult = service.HasNavigationPermission("identity_users_createUser");
            
            // 断言
            // 新行为：只能访问二级导航权限，一级模块导航权限不再自动生成
            Assert.False(examModuleResult, "用户不应能访问考试模块（仅提取二级权限）");
            Assert.True(examPapersControllerResult, "用户应能访问试卷控制器（提取二级权限）");
            Assert.False(examPapersActionResult, "用户不应能直接访问试卷操作（HasNavigationPermission不应用于三级权限）");
            
            Assert.False(identityModuleResult, "用户不应能访问身份模块（仅提取二级权限）");
            Assert.True(identityUsersControllerResult, "用户应能访问用户控制器（提取二级权限）");
            Assert.False(identityUsersActionResult, "用户不应能直接访问用户操作（HasNavigationPermission不应用于三级权限）");
            
            // 验证提取到的导航权限集合只包含二级权限
            _mockPermissionService.Verify(p => p.HasPermission("exam", It.Is<ISet<string>>(
                s => !s.Contains("exam") && s.Contains("exam_examPapers"))), 
                Times.Once);
        }

        /// <summary>
        /// 测试管理员用户的导航权限检查
        /// </summary>
        [Fact]
        public void HasNavigationPermission_AdminUser_ReturnsTrue()
        {
            // 安排
            _mockCurrentUser.SetupGet(u => u.IsAuthenticated).Returns(true);
            _mockCurrentUser.SetupGet(u => u.Roles).Returns(new[] { "Admin" });
            _mockCurrentUser.SetupGet(u => u.Permissions).Returns(new HashSet<string>());
            
            var service = new HasPermissionService(_mockLogger.Object, _mockPermissionService.Object, _mockCurrentUser.Object);

            // 执行
            bool result = service.HasNavigationPermission("any_navigation_permission");
            
            // 断言
            Assert.True(result, "管理员用户应该能访问任何导航");
            
            // 验证没有调用权限服务检查
            _mockPermissionService.Verify(
                p => p.HasPermission(It.IsAny<string>(), It.IsAny<ISet<string>>()), 
                Times.Never);
        }

        /// <summary>
        /// 测试未认证用户的导航权限检查
        /// </summary>
        [Fact]
        public void HasNavigationPermission_UserNotAuthenticated_ReturnsFalse()
        {
            // 安排
            _mockCurrentUser.SetupGet(u => u.IsAuthenticated).Returns(false);
            
            var service = new HasPermissionService(_mockLogger.Object, _mockPermissionService.Object, _mockCurrentUser.Object);

            // 执行
            bool result = service.HasNavigationPermission("exam");
            
            // 断言
            Assert.False(result, "未认证用户不应能访问导航");
            
            // 验证没有调用权限服务检查
            _mockPermissionService.Verify(
                p => p.HasPermission(It.IsAny<string>(), It.IsAny<ISet<string>>()), 
                Times.Never);
        }

        /// <summary>
        /// 测试空权限代码的导航权限检查
        /// </summary>
        [Fact]
        public void HasNavigationPermission_EmptyPermissionCode_ReturnsFalse()
        {
            // 安排
            _mockCurrentUser.SetupGet(u => u.IsAuthenticated).Returns(true);
            _mockCurrentUser.SetupGet(u => u.Roles).Returns(new[] { "User" });
            _mockCurrentUser.SetupGet(u => u.Permissions).Returns(new HashSet<string> { "exam_examPapers_getExamPaper" });
            
            var service = new HasPermissionService(_mockLogger.Object, _mockPermissionService.Object, _mockCurrentUser.Object);

            // 执行
            bool nullResult = service.HasNavigationPermission(null);
            bool emptyResult = service.HasNavigationPermission("");
            
            // 断言
            Assert.False(nullResult, "空权限代码应返回false");
            Assert.False(emptyResult, "空权限代码应返回false");
        }

        /// <summary>
        /// 测试复杂导航场景 - 用户只有特定模块权限
        /// </summary>
        [Fact]
        public void HasNavigationPermission_ComplexScenario_ShouldWorkAsExpected()
        {
            // 安排 - 用户只有考试模块的试卷管理权限和系统模块的用户权限
            var permissions = new HashSet<string>
            {
                "exam_examPapers_getExamPaper",
                "exam_examPapers_previewExamPaper",
                "system_users_getProfile",
                "system_users_updateProfile"
            };
            
            _mockCurrentUser.SetupGet(u => u.IsAuthenticated).Returns(true);
            _mockCurrentUser.SetupGet(u => u.Roles).Returns(new[] { "User" });
            _mockCurrentUser.SetupGet(u => u.Permissions).Returns(permissions);
            
            // 预期导航权限集合
            var expectedNavigationPermissions = new HashSet<string>
            {
                "exam", "exam_examPapers",
                "system", "system_users"
            };
            
            _mockPermissionService
                .Setup(p => p.HasPermission(It.IsAny<string>(), It.IsAny<ISet<string>>()))
                .Returns((string permission, ISet<string> perms) => 
                    expectedNavigationPermissions.Contains(permission));
            
            var service = new HasPermissionService(_mockLogger.Object, _mockPermissionService.Object, _mockCurrentUser.Object);

            // 执行和断言
            // 1. 用户有权限的导航
            Assert.True(service.HasNavigationPermission("exam"), "用户应能访问考试模块");
            Assert.True(service.HasNavigationPermission("exam_examPapers"), "用户应能访问试卷管理");
            Assert.True(service.HasNavigationPermission("system"), "用户应能访问系统模块");
            Assert.True(service.HasNavigationPermission("system_users"), "用户应能访问用户管理");
            
            // 2. 用户无权限的导航
            Assert.False(service.HasNavigationPermission("exam_examRecords"), "用户不应能访问考试记录");
            Assert.False(service.HasNavigationPermission("system_roles"), "用户不应能访问角色管理");
            Assert.False(service.HasNavigationPermission("reports"), "用户不应能访问报表模块");
            
            // 3. 验证权限服务调用
            foreach (var navPermission in new[] { "exam", "exam_examPapers", "system", "system_users",
                                                 "exam_examRecords", "system_roles", "reports" })
            {
                _mockPermissionService.Verify(
                    p => p.HasPermission(navPermission, It.Is<ISet<string>>(
                        s => s.SetEquals(expectedNavigationPermissions))),
                    Times.Once);
            }
        }

        /// <summary>
        /// 测试特殊权限键 - 包括单独的"exam"权限和不带下划线的"examPapers"权限
        /// </summary>
        [Fact]
        public void HasPermission_SpecialPermissionKeys_ShouldWorkAsExpected()
        {
            // 安排
            _mockCurrentUser.SetupGet(u => u.IsAuthenticated).Returns(true);
            _mockCurrentUser.SetupGet(u => u.Roles).Returns(new[] { "User" });
            
            // 用户拥有直接的模块级权限和不规范的权限名称
            var specialPermissions = new HashSet<string>
            {
                "exam",         // 直接的模块级权限
                "examPapers"    // 不带下划线的权限
            };
            
            _mockCurrentUser.SetupGet(u => u.Permissions).Returns(specialPermissions);
            
            // 模拟权限服务对特殊权限的处理
            _mockPermissionService
                .Setup(p => p.HasPermission(It.IsAny<string>(), It.IsAny<ISet<string>>()))
                .Returns((string permission, ISet<string> permissions) => 
                    permissions.Contains(permission));
            
            var service = new HasPermissionService(_mockLogger.Object, _mockPermissionService.Object, _mockCurrentUser.Object);

            // 执行
            bool examResult = service.HasPermission("exam");
            bool examPapersResult = service.HasPermission("examPapers");
            bool examPapersUnderscoreResult = service.HasPermission("exam_examPapers");
            bool examRecordsResult = service.HasPermission("exam_examRecords");
            
            // 断言
            Assert.True(examResult, "用户应能访问exam权限（直接拥有）");
            Assert.True(examPapersResult, "用户应能访问examPapers权限（直接拥有）");
            Assert.False(examPapersUnderscoreResult, "用户不应能访问exam_examPapers权限（未直接拥有）");
            Assert.False(examRecordsResult, "用户不应能访问exam_examRecords权限（未直接拥有）");
        }

        /// <summary>
        /// 测试导航权限功能 - 验证对特殊权限键"exam"和"examPapers"的导航权限处理
        /// </summary>
        [Fact]
        public void HasNavigationPermission_SpecialPermissionKeys_ShouldWorkAsExpected()
        {
            // 安排
            _mockCurrentUser.SetupGet(u => u.IsAuthenticated).Returns(true);
            _mockCurrentUser.SetupGet(u => u.Roles).Returns(new[] { "User" });
            
            // 用户拥有直接的模块级权限和不规范的权限名称
            var specialPermissions = new HashSet<string>
            {
                "exam",         // 直接的模块级权限
                "examPapers"    // 不带下划线的权限
            };
            
            _mockCurrentUser.SetupGet(u => u.Permissions).Returns(specialPermissions);
            
            // 模拟提取导航权限后的结果集
            var extractedPermissions = new HashSet<string> { "exam", "examPapers" };
            
            // 模拟权限服务对导航权限的处理
            _mockPermissionService
                .Setup(p => p.HasPermission(It.IsAny<string>(), It.IsAny<ISet<string>>()))
                .Returns((string permission, ISet<string> permissions) => 
                    extractedPermissions.Contains(permission));
            
            var service = new HasPermissionService(_mockLogger.Object, _mockPermissionService.Object, _mockCurrentUser.Object);

            // 执行
            bool examNavResult = service.HasNavigationPermission("exam");
            bool examPapersNavResult = service.HasNavigationPermission("examPapers");
            bool examPapersUnderscoreNavResult = service.HasNavigationPermission("exam_examPapers");
            
            // 断言
            Assert.True(examNavResult, "用户应能访问exam导航（直接拥有权限）");
            Assert.True(examPapersNavResult, "用户应能访问examPapers导航（直接拥有权限）");
            Assert.False(examPapersUnderscoreNavResult, "用户不应能访问exam_examPapers导航（未直接拥有权限）");
            
            // 验证提取导航权限的调用
            _mockPermissionService.Verify(p => p.HasPermission("exam", 
                It.Is<ISet<string>>(s => s.Contains("exam") && s.Contains("examPapers"))), 
                Times.Once);
            
            _mockPermissionService.Verify(p => p.HasPermission("examPapers", 
                It.Is<ISet<string>>(s => s.Contains("exam") && s.Contains("examPapers"))), 
                Times.Once);
            
            _mockPermissionService.Verify(p => p.HasPermission("exam_examPapers", 
                It.Is<ISet<string>>(s => s.Contains("exam") && s.Contains("examPapers"))), 
                Times.Once);
        }
    }
} 