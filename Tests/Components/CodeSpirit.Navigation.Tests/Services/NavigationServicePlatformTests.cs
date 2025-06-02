using CodeSpirit.Core.Enums;
using CodeSpirit.Navigation.Models;
using CodeSpirit.Navigation.Tests.TestBase;
using System.Collections.Generic;
using Xunit;

namespace CodeSpirit.Navigation.Tests.Services
{
    /// <summary>
    /// 导航服务平台类型过滤测试
    /// </summary>
    public class NavigationServicePlatformTests : NavigationTestBase
    {
        [Fact]
        public void FilterNodesByPlatform_SystemPlatform_ShouldReturnSystemNodes()
        {
            // Arrange
            var nodes = new List<NavigationNode>
            {
                new NavigationNode("system1", "系统功能1", "/system1")
                {
                    PlatformType = PlatformType.System
                },
                new NavigationNode("tenant1", "租户功能1", "/tenant1")
                {
                    PlatformType = PlatformType.Tenant
                },
                new NavigationNode("both1", "通用功能1", "/both1")
                {
                    PlatformType = PlatformType.Both
                }
            };

            // Act
            var result = NavigationService.FilterNodesByPlatform(nodes, PlatformType.System);

            // Assert
            Assert.Equal(2, result.Count); // system1 and both1
            Assert.Contains(result, n => n.Name == "system1");
            Assert.Contains(result, n => n.Name == "both1");
            Assert.DoesNotContain(result, n => n.Name == "tenant1");
        }

        [Fact]
        public void FilterNodesByPlatform_TenantPlatform_ShouldReturnTenantNodes()
        {
            // Arrange
            var nodes = new List<NavigationNode>
            {
                new NavigationNode("system1", "系统功能1", "/system1")
                {
                    PlatformType = PlatformType.System
                },
                new NavigationNode("tenant1", "租户功能1", "/tenant1")
                {
                    PlatformType = PlatformType.Tenant
                },
                new NavigationNode("both1", "通用功能1", "/both1")
                {
                    PlatformType = PlatformType.Both
                }
            };

            // Act
            var result = NavigationService.FilterNodesByPlatform(nodes, PlatformType.Tenant);

            // Assert
            Assert.Equal(2, result.Count); // tenant1 and both1
            Assert.Contains(result, n => n.Name == "tenant1");
            Assert.Contains(result, n => n.Name == "both1");
            Assert.DoesNotContain(result, n => n.Name == "system1");
        }

        [Fact]
        public void FilterNodesByPlatform_BothPlatforms_ShouldReturnAllNodes()
        {
            // Arrange
            var nodes = new List<NavigationNode>
            {
                new NavigationNode("system1", "系统功能1", "/system1")
                {
                    PlatformType = PlatformType.System
                },
                new NavigationNode("tenant1", "租户功能1", "/tenant1")
                {
                    PlatformType = PlatformType.Tenant
                },
                new NavigationNode("both1", "通用功能1", "/both1")
                {
                    PlatformType = PlatformType.Both
                }
            };

            // Act
            var result = NavigationService.FilterNodesByPlatform(nodes, PlatformType.Both);

            // Assert
            Assert.Equal(3, result.Count);
            Assert.Contains(result, n => n.Name == "system1");
            Assert.Contains(result, n => n.Name == "tenant1");
            Assert.Contains(result, n => n.Name == "both1");
        }

        [Fact]
        public void FilterNodesByPlatform_WithChildren_ShouldFilterRecursively()
        {
            // Arrange
            var parent = new NavigationNode("parent", "父节点", "/parent")
            {
                PlatformType = PlatformType.Both,
                Children = new List<NavigationNode>
                {
                    new NavigationNode("child1", "子节点1", "/parent/child1")
                    {
                        PlatformType = PlatformType.System
                    },
                    new NavigationNode("child2", "子节点2", "/parent/child2")
                    {
                        PlatformType = PlatformType.Tenant
                    },
                    new NavigationNode("child3", "子节点3", "/parent/child3")
                    {
                        PlatformType = PlatformType.Both
                    }
                }
            };

            var nodes = new List<NavigationNode> { parent };

            // Act
            var result = NavigationService.FilterNodesByPlatform(nodes, PlatformType.System);

            // Assert
            Assert.Single(result);
            var filteredParent = result[0];
            Assert.Equal("parent", filteredParent.Name);
            Assert.Equal(2, filteredParent.Children.Count); // child1 and child3
            Assert.Contains(filteredParent.Children, c => c.Name == "child1");
            Assert.Contains(filteredParent.Children, c => c.Name == "child3");
            Assert.DoesNotContain(filteredParent.Children, c => c.Name == "child2");
        }

        [Fact]
        public void FilterNodesByPlatform_EmptyNodes_ShouldReturnEmpty()
        {
            // Arrange
            var nodes = new List<NavigationNode>();

            // Act
            var result = NavigationService.FilterNodesByPlatform(nodes, PlatformType.System);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void FilterNodesByPlatform_NullNodes_ShouldReturnEmpty()
        {
            // Act
            var result = NavigationService.FilterNodesByPlatform(null, PlatformType.System);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void FilterNodesByPlatform_NodeWithNonePlatform_ShouldBeFiltered()
        {
            // Arrange
            var nodes = new List<NavigationNode>
            {
                new NavigationNode("none1", "无平台功能", "/none1")
                {
                    PlatformType = PlatformType.None
                },
                new NavigationNode("system1", "系统功能1", "/system1")
                {
                    PlatformType = PlatformType.System
                }
            };

            // Act
            var result = NavigationService.FilterNodesByPlatform(nodes, PlatformType.System);

            // Assert
            Assert.Single(result);
            Assert.Equal("system1", result[0].Name);
        }

        [Fact]
        public void FilterNodesByPlatform_SystemPlatform_ShouldNotIncludeTenantOnlyModules()
        {
            // Arrange - 模拟类似 ExamApi 的模块结构
            var moduleNode = new NavigationNode("examApi", "考试中心", "/examApi")
            {
                PlatformType = PlatformType.Tenant, // 模块级别设置为 Tenant
                OriginalPlatformType = PlatformType.Tenant
            };

            var controllerNode = new NavigationNode("apiControllerBase", "考试API基础", "/examApi/api")
            {
                PlatformType = PlatformType.Tenant,
                OriginalPlatformType = PlatformType.Tenant
            };

            var actionNode = new NavigationNode("examPapers", "试卷管理", "/examApi/api/examPapers")
            {
                PlatformType = PlatformType.Tenant,
                OriginalPlatformType = PlatformType.Tenant
            };

            controllerNode.Children.Add(actionNode);
            moduleNode.Children.Add(controllerNode);

            var nodes = new List<NavigationNode> { moduleNode };

            // Act - 使用系统平台过滤
            var result = NavigationService.FilterNodesByPlatform(nodes, PlatformType.System);

            // Assert - 应该不包含任何租户专用的节点
            Assert.Empty(result);
        }

        [Fact]
        public void FilterNodesByPlatform_TenantPlatform_ShouldIncludeTenantOnlyModules()
        {
            // Arrange - 模拟类似 ExamApi 的模块结构
            var moduleNode = new NavigationNode("examApi", "考试中心", "/examApi")
            {
                PlatformType = PlatformType.Tenant,
                OriginalPlatformType = PlatformType.Tenant
            };

            var controllerNode = new NavigationNode("apiControllerBase", "考试API基础", "/examApi/api")
            {
                PlatformType = PlatformType.Tenant,
                OriginalPlatformType = PlatformType.Tenant
            };

            var actionNode = new NavigationNode("examPapers", "试卷管理", "/examApi/api/examPapers")
            {
                PlatformType = PlatformType.Tenant,
                OriginalPlatformType = PlatformType.Tenant
            };

            controllerNode.Children.Add(actionNode);
            moduleNode.Children.Add(controllerNode);

            var nodes = new List<NavigationNode> { moduleNode };

            // Act - 使用租户平台过滤
            var result = NavigationService.FilterNodesByPlatform(nodes, PlatformType.Tenant);

            // Assert - 应该包含租户专用的节点
            Assert.Single(result);
            Assert.Equal("examApi", result[0].Name);
            Assert.Equal(PlatformType.Tenant, result[0].PlatformType);
            Assert.Single(result[0].Children);
            Assert.Equal("apiControllerBase", result[0].Children[0].Name);
        }
    }
} 