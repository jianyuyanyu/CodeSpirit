using CodeSpirit.Core.Enums;
using CodeSpirit.Navigation.Models;
using CodeSpirit.Navigation.Services;
using CodeSpirit.Navigation.Tests.TestBase;
using Moq;
using System.Collections.Generic;
using Xunit;

namespace CodeSpirit.Navigation.Tests.Services
{
    /// <summary>
    /// 导航服务上下文过滤测试
    /// </summary>
    public class NavigationServiceContextTests : NavigationTestBase
    {
        [Fact]
        public void FilterNodesByContext_AuthenticationFilter_ShouldFilterCorrectly()
        {
            // Arrange
            var nodes = new List<NavigationNode>
            {
                new NavigationNode("public1", "公开功能", "/public1")
                {
                    RequireAuth = false,
                    PlatformType = PlatformType.Both
                },
                new NavigationNode("auth1", "需要认证功能", "/auth1")
                {
                    RequireAuth = true,
                    PlatformType = PlatformType.Both
                }
            };

            var context = new NavigationFilterContext
            {
                IsAuthenticated = false,
                PlatformType = PlatformType.Both
            };

            // Act
            var result = NavigationService.FilterNodesByContext(nodes, context);

            // Assert
            Assert.Single(result);
            Assert.Equal("public1", result[0].Name);
        }

        [Fact]
        public void FilterNodesByContext_VisibilityField_ShouldReturnAllNodesWithVisibleProperty()
        {
            // Arrange
            var nodes = new List<NavigationNode>
            {
                new NavigationNode("visible1", "可见功能", "/visible1")
                {
                    Visible = true,
                    PlatformType = PlatformType.Both
                },
                new NavigationNode("hidden1", "隐藏功能", "/hidden1")
                {
                    Visible = false,
                    PlatformType = PlatformType.Both
                },
                new NavigationNode("visible2", "可见功能2", "/visible2")
                {
                    Visible = true,
                    PlatformType = PlatformType.Both
                }
            };

            var context = new NavigationFilterContext
            {
                PlatformType = PlatformType.Both,
                IsAuthenticated = true
            };

            // Act
            var result = NavigationService.FilterNodesByContext(nodes, context);

            // Assert - 所有节点都应该返回，包括 Visible = false 的节点
            Assert.Equal(3, result.Count);
            Assert.Contains(result, n => n.Name == "visible1" && n.Visible == true);
            Assert.Contains(result, n => n.Name == "hidden1" && n.Visible == false);
            Assert.Contains(result, n => n.Name == "visible2" && n.Visible == true);
        }

        [Fact]
        public void FilterNodesByContext_ExperimentalFilter_ShouldFilterCorrectly()
        {
            // Arrange
            var nodes = new List<NavigationNode>
            {
                new NavigationNode("stable1", "稳定功能", "/stable1")
                {
                    IsExperimental = false,
                    PlatformType = PlatformType.Both,
                    RequireAuth = false
                },
                new NavigationNode("experimental1", "实验性功能", "/experimental1")
                {
                    IsExperimental = true,
                    PlatformType = PlatformType.Both,
                    RequireAuth = false
                }
            };

            var context = new NavigationFilterContext
            {
                IsDevelopment = false,
                PlatformType = PlatformType.Both
            };

            // Act
            var result = NavigationService.FilterNodesByContext(nodes, context);

            // Assert
            Assert.Single(result);
            Assert.Equal("stable1", result[0].Name);
        }

        [Fact]
        public void FilterNodesByContext_VersionFilter_ShouldFilterCorrectly()
        {
            // Arrange
            var nodes = new List<NavigationNode>
            {
                new NavigationNode("old1", "旧版本功能", "/old1")
                {
                    MinVersion = "2.0.0",
                    MaxVersion = "3.0.0",
                    PlatformType = PlatformType.Both,
                    RequireAuth = false
                },
                new NavigationNode("current1", "当前版本功能", "/current1")
                {
                    MinVersion = "1.0.0",
                    MaxVersion = "2.0.0",
                    PlatformType = PlatformType.Both,
                    RequireAuth = false
                },
                new NavigationNode("future1", "未来版本功能", "/future1")
                {
                    MinVersion = "3.0.0",
                    PlatformType = PlatformType.Both,
                    RequireAuth = false
                }
            };

            var context = new NavigationFilterContext
            {
                CurrentVersion = "1.5.0",
                PlatformType = PlatformType.Both
            };

            // Act
            var result = NavigationService.FilterNodesByContext(nodes, context);

            // Assert
            Assert.Single(result);
            Assert.Equal("current1", result[0].Name);
        }

        [Fact]
        public void FilterNodesByContext_DeviceTypeFilter_ShouldFilterCorrectly()
        {
            // Arrange
            var nodes = new List<NavigationNode>
            {
                new NavigationNode("desktop1", "桌面功能", "/desktop1")
                {
                    SupportedDevices = new[] { "desktop" },
                    PlatformType = PlatformType.Both,
                    RequireAuth = false
                },
                new NavigationNode("mobile1", "移动功能", "/mobile1")
                {
                    SupportedDevices = new[] { "mobile", "tablet" },
                    PlatformType = PlatformType.Both,
                    RequireAuth = false
                },
                new NavigationNode("all1", "全平台功能", "/all1")
                {
                    SupportedDevices = new[] { "desktop", "mobile", "tablet" },
                    PlatformType = PlatformType.Both,
                    RequireAuth = false
                }
            };

            var context = new NavigationFilterContext
            {
                DeviceType = "mobile",
                PlatformType = PlatformType.Both
            };

            // Act
            var result = NavigationService.FilterNodesByContext(nodes, context);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains(result, n => n.Name == "mobile1");
            Assert.Contains(result, n => n.Name == "all1");
            Assert.DoesNotContain(result, n => n.Name == "desktop1");
        }

        [Fact]
        public void FilterNodesByContext_GroupFilter_ShouldFilterCorrectly()
        {
            // Arrange
            var nodes = new List<NavigationNode>
            {
                new NavigationNode("admin1", "管理功能", "/admin1")
                {
                    Group = "Admin",
                    PlatformType = PlatformType.Both,
                    RequireAuth = false
                },
                new NavigationNode("user1", "用户功能", "/user1")
                {
                    Group = "User",
                    PlatformType = PlatformType.Both,
                    RequireAuth = false
                },
                new NavigationNode("nogroup1", "无分组功能", "/nogroup1")
                {
                    Group = null,
                    PlatformType = PlatformType.Both,
                    RequireAuth = false
                }
            };

            var context = new NavigationFilterContext
            {
                GroupFilter = new[] { "Admin" },
                PlatformType = PlatformType.Both
            };

            // Act
            var result = NavigationService.FilterNodesByContext(nodes, context);

            // Assert
            Assert.Single(result);
            Assert.Equal("admin1", result[0].Name);
        }

        [Fact]
        public void FilterNodesByContext_TagsFilter_ShouldFilterCorrectly()
        {
            // Arrange
            var nodes = new List<NavigationNode>
            {
                new NavigationNode("admin1", "管理功能", "/admin1")
                {
                    Tags = new[] { "admin", "management" },
                    PlatformType = PlatformType.Both,
                    RequireAuth = false
                },
                new NavigationNode("user1", "用户功能", "/user1")
                {
                    Tags = new[] { "user", "profile" },
                    PlatformType = PlatformType.Both,
                    RequireAuth = false
                },
                new NavigationNode("shared1", "共享功能", "/shared1")
                {
                    Tags = new[] { "admin", "user" },
                    PlatformType = PlatformType.Both,
                    RequireAuth = false
                }
            };

            var context = new NavigationFilterContext
            {
                UserTags = new[] { "admin" },
                PlatformType = PlatformType.Both
            };

            // Act
            var result = NavigationService.FilterNodesByContext(nodes, context);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains(result, n => n.Name == "admin1");
            Assert.Contains(result, n => n.Name == "shared1");
            Assert.DoesNotContain(result, n => n.Name == "user1");
        }

        [Fact]
        public void FilterNodesByContext_PermissionFilter_ShouldFilterCorrectly()
        {
            // Arrange
            var nodes = new List<NavigationNode>
            {
                new NavigationNode("allowed1", "允许功能", "/allowed1")
                {
                    Permission = "allowed_permission",
                    PlatformType = PlatformType.Both,
                    RequireAuth = false
                },
                new NavigationNode("denied1", "拒绝功能", "/denied1")
                {
                    Permission = "denied_permission",
                    PlatformType = PlatformType.Both,
                    RequireAuth = false
                },
                new NavigationNode("noperm1", "无权限功能", "/noperm1")
                {
                    Permission = null,
                    PlatformType = PlatformType.Both,
                    RequireAuth = false
                }
            };

            // 设置权限模拟
            MockPermissionService.Setup(x => x.HasNavigationPermission("allowed_permission"))
                .Returns(true);
            MockPermissionService.Setup(x => x.HasNavigationPermission("denied_permission"))
                .Returns(false);

            var context = new NavigationFilterContext
            {
                PermissionService = MockPermissionService.Object,
                PlatformType = PlatformType.Both
            };

            // Act
            var result = NavigationService.FilterNodesByContext(nodes, context);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains(result, n => n.Name == "allowed1");
            Assert.Contains(result, n => n.Name == "noperm1");
            Assert.DoesNotContain(result, n => n.Name == "denied1");
        }

        [Fact]
        public void FilterNodesByContext_ComplexFilter_ShouldFilterCorrectly()
        {
            // Arrange
            var nodes = new List<NavigationNode>
            {
                new NavigationNode("complex1", "复杂功能1", "/complex1")
                {
                    PlatformType = PlatformType.System,
                    RequireAuth = true,
                    IsExperimental = false,
                    MinVersion = "1.0.0",
                    MaxVersion = "3.0.0",
                    SupportedDevices = new[] { "desktop", "tablet" },
                    Group = "Admin",
                    Tags = new[] { "admin", "management" },
                    Permission = "complex_permission"
                },
                new NavigationNode("complex2", "复杂功能2", "/complex2")
                {
                    PlatformType = PlatformType.Tenant,
                    RequireAuth = true,
                    IsExperimental = true,
                    SupportedDevices = new[] { "mobile" },
                    Group = "User",
                    Tags = new[] { "user" },
                    Permission = "another_permission"
                }
            };

            // 设置权限模拟
            MockPermissionService.Setup(x => x.HasNavigationPermission("complex_permission"))
                .Returns(true);
            MockPermissionService.Setup(x => x.HasNavigationPermission("another_permission"))
                .Returns(true);

            var context = new NavigationFilterContext
            {
                PlatformType = PlatformType.System,
                IsAuthenticated = true,
                IsDevelopment = false,
                CurrentVersion = "2.0.0",
                DeviceType = "desktop",
                GroupFilter = new[] { "Admin" },
                UserTags = new[] { "admin" },
                PermissionService = MockPermissionService.Object
            };

            // Act
            var result = NavigationService.FilterNodesByContext(nodes, context);

            // Assert
            Assert.Single(result);
            Assert.Equal("complex1", result[0].Name);
        }

        [Fact]
        public void FilterNodesByContext_WithSorting_ShouldReturnSortedResult()
        {
            // Arrange
            var nodes = new List<NavigationNode>
            {
                new NavigationNode("item3", "项目3", "/item3")
                {
                    Order = 3,
                    Priority = 1,
                    PlatformType = PlatformType.Both,
                    RequireAuth = false
                },
                new NavigationNode("item1", "项目1", "/item1")
                {
                    Order = 1,
                    Priority = 5,
                    PlatformType = PlatformType.Both,
                    RequireAuth = false
                },
                new NavigationNode("item2", "项目2", "/item2")
                {
                    Order = 1,
                    Priority = 3,
                    PlatformType = PlatformType.Both,
                    RequireAuth = false
                }
            };

            var context = new NavigationFilterContext
            {
                PlatformType = PlatformType.Both
            };

            // Act
            var result = NavigationService.FilterNodesByContext(nodes, context);

            // Assert
            Assert.Equal(3, result.Count);
            Assert.Equal("item1", result[0].Name); // Order=1, Priority=5 (highest)
            Assert.Equal("item2", result[1].Name); // Order=1, Priority=3
            Assert.Equal("item3", result[2].Name); // Order=3, Priority=1
        }

        [Fact]
        public void FilterNodesByContext_ParentWithNoMatchButChildrenMatch_ShouldIncludeParent()
        {
            // Arrange
            var parent = new NavigationNode("parent", "父节点", "/parent")
            {
                PlatformType = PlatformType.Tenant, // 不匹配
                RequireAuth = false,
                Children = new List<NavigationNode>
                {
                    new NavigationNode("child1", "子节点1", "/parent/child1")
                    {
                        PlatformType = PlatformType.System, // 匹配
                        RequireAuth = false
                    },
                    new NavigationNode("child2", "子节点2", "/parent/child2")
                    {
                        PlatformType = PlatformType.Tenant, // 不匹配
                        RequireAuth = false
                    }
                }
            };

            var nodes = new List<NavigationNode> { parent };

            var context = new NavigationFilterContext
            {
                PlatformType = PlatformType.System
            };

            // Act
            var result = NavigationService.FilterNodesByContext(nodes, context);

            // Assert
            Assert.Single(result);
            var filteredParent = result[0];
            Assert.Equal("parent", filteredParent.Name);
            Assert.Single(filteredParent.Children);
            Assert.Equal("child1", filteredParent.Children[0].Name);
        }

        [Fact]
        public void FilterNodesByContext_SimpleTest_ShouldWork()
        {
            // Arrange
            var nodes = new List<NavigationNode>
            {
                new NavigationNode("test1", "测试1", "/test1")
                {
                    PlatformType = PlatformType.Both,
                    RequireAuth = false  // 设置为不需要认证
                }
            };

            var context = new NavigationFilterContext
            {
                PlatformType = PlatformType.Both
            };

            // Act
            var result = NavigationService.FilterNodesByContext(nodes, context);

            // Assert
            Assert.Single(result);
            Assert.Equal("test1", result[0].Name);
        }
    }
} 