using CodeSpirit.Core.Enums;
using CodeSpirit.Navigation.Models;
using CodeSpirit.Navigation.Tests.TestBase;
using System.Collections.Generic;
using Xunit;

namespace CodeSpirit.Navigation.Tests.Services
{
    /// <summary>
    /// 导航服务扩展属性测试
    /// </summary>
    public class NavigationServiceExtendedPropertiesTests : NavigationTestBase
    {
        [Fact]
        public void NavigationNode_Clone_ShouldCopyAllProperties()
        {
            // Arrange
            var original = new NavigationNode("test", "测试节点", "/test")
            {
                Link = "https://example.com",
                Icon = "fa-solid fa-test",
                Order = 5,
                ParentPath = "/parent",
                Hidden = true,
                Permission = "test_permission",
                Description = "测试描述",
                IsExternal = true,
                Target = "_blank",
                Route = "test/route",
                ModuleName = "TestModule",
                PlatformType = PlatformType.System,
                Group = "TestGroup",
                Tags = new[] { "tag1", "tag2" },
                MetaData = new Dictionary<string, object> { { "key1", "value1" } },
                RequireAuth = false,
                IsExperimental = true,
                MinVersion = "1.0.0",
                MaxVersion = "2.0.0",
                SupportedDevices = new[] { "desktop", "mobile" },
                Priority = 10,
                Shortcut = "Ctrl+T",
                Badge = "NEW",
                BadgeType = "success",
                Children = new List<NavigationNode>
                {
                    new NavigationNode("child1", "子节点1", "/test/child1")
                }
            };

            // Act
            var clone = original.Clone();

            // Assert
            Assert.Equal(original.Name, clone.Name);
            Assert.Equal(original.Title, clone.Title);
            Assert.Equal(original.Path, clone.Path);
            Assert.Equal(original.Link, clone.Link);
            Assert.Equal(original.Icon, clone.Icon);
            Assert.Equal(original.Order, clone.Order);
            Assert.Equal(original.ParentPath, clone.ParentPath);
            Assert.Equal(original.Hidden, clone.Hidden);
            Assert.Equal(original.Permission, clone.Permission);
            Assert.Equal(original.Description, clone.Description);
            Assert.Equal(original.IsExternal, clone.IsExternal);
            Assert.Equal(original.Target, clone.Target);
            Assert.Equal(original.Route, clone.Route);
            Assert.Equal(original.ModuleName, clone.ModuleName);
            Assert.Equal(original.PlatformType, clone.PlatformType);
            Assert.Equal(original.Group, clone.Group);
            Assert.Equal(original.Tags, clone.Tags);
            Assert.Equal(original.RequireAuth, clone.RequireAuth);
            Assert.Equal(original.IsExperimental, clone.IsExperimental);
            Assert.Equal(original.MinVersion, clone.MinVersion);
            Assert.Equal(original.MaxVersion, clone.MaxVersion);
            Assert.Equal(original.SupportedDevices, clone.SupportedDevices);
            Assert.Equal(original.Priority, clone.Priority);
            Assert.Equal(original.Shortcut, clone.Shortcut);
            Assert.Equal(original.Badge, clone.Badge);
            Assert.Equal(original.BadgeType, clone.BadgeType);

            // 验证元数据被深拷贝
            Assert.NotSame(original.MetaData, clone.MetaData);
            Assert.Equal(original.MetaData["key1"], clone.MetaData["key1"]);

            // 验证子节点列表被重置为空
            Assert.Empty(clone.Children);
            Assert.NotSame(original.Children, clone.Children);
        }

        [Fact]
        public void NavigationNode_DefaultValues_ShouldBeCorrect()
        {
            // Arrange & Act
            var node = new NavigationNode("test", "测试", "/test");

            // Assert
            Assert.Equal(PlatformType.Both, node.PlatformType);
            Assert.Empty(node.Tags);
            Assert.Empty(node.MetaData);
            Assert.True(node.RequireAuth);
            Assert.False(node.IsExperimental);
            Assert.Equal(new[] { "desktop", "tablet", "mobile" }, node.SupportedDevices);
            Assert.Equal(0, node.Priority);
            Assert.Equal("info", node.BadgeType);
            Assert.Empty(node.Children);
        }

        [Fact]
        public void NavigationConfigItem_DefaultValues_ShouldBeCorrect()
        {
            // Arrange & Act
            var config = new NavigationConfigItem();

            // Assert
            Assert.Equal(PlatformType.Both, config.PlatformType);
            Assert.Empty(config.Tags);
            Assert.Empty(config.MetaData);
            Assert.True(config.RequireAuth);
            Assert.False(config.IsExperimental);
            Assert.Equal(new[] { "desktop", "tablet", "mobile" }, config.SupportedDevices);
            Assert.Equal(0, config.Priority);
            Assert.Equal("info", config.BadgeType);
            Assert.Empty(config.Children);
        }

        [Theory]
        [InlineData("NEW", "success")]
        [InlineData("HOT", "danger")]
        [InlineData("BETA", "warning")]
        [InlineData("", "info")]
        public void NavigationNode_BadgeProperties_ShouldWorkCorrectly(string badge, string badgeType)
        {
            // Arrange & Act
            var node = new NavigationNode("test", "测试", "/test")
            {
                Badge = badge,
                BadgeType = badgeType
            };

            // Assert
            Assert.Equal(badge, node.Badge);
            Assert.Equal(badgeType, node.BadgeType);
        }

        [Theory]
        [InlineData("Ctrl+N")]
        [InlineData("Alt+F4")]
        [InlineData("Shift+Delete")]
        [InlineData("F5")]
        public void NavigationNode_Shortcut_ShouldWorkCorrectly(string shortcut)
        {
            // Arrange & Act
            var node = new NavigationNode("test", "测试", "/test")
            {
                Shortcut = shortcut
            };

            // Assert
            Assert.Equal(shortcut, node.Shortcut);
        }

        [Fact]
        public void NavigationNode_MetaData_ShouldSupportComplexTypes()
        {
            // Arrange
            var metaData = new Dictionary<string, object>
            {
                { "string", "value" },
                { "number", 42 },
                { "boolean", true },
                { "array", new[] { 1, 2, 3 } },
                { "object", new { name = "test", value = 123 } }
            };

            // Act
            var node = new NavigationNode("test", "测试", "/test")
            {
                MetaData = metaData
            };

            // Assert
            Assert.Equal("value", node.MetaData["string"]);
            Assert.Equal(42, node.MetaData["number"]);
            Assert.Equal(true, node.MetaData["boolean"]);
            Assert.Equal(new[] { 1, 2, 3 }, node.MetaData["array"]);
            Assert.NotNull(node.MetaData["object"]);
        }

        [Fact]
        public void NavigationNode_SupportedDevices_ShouldWorkCorrectly()
        {
            // Test case 1: Desktop only
            var node1 = new NavigationNode("test1", "测试1", "/test1")
            {
                SupportedDevices = new[] { "desktop" }
            };
            Assert.Single(node1.SupportedDevices);
            Assert.Equal("desktop", node1.SupportedDevices[0]);

            // Test case 2: Mobile and tablet
            var node2 = new NavigationNode("test2", "测试2", "/test2")
            {
                SupportedDevices = new[] { "mobile", "tablet" }
            };
            Assert.Equal(2, node2.SupportedDevices.Length);
            Assert.Contains("mobile", node2.SupportedDevices);
            Assert.Contains("tablet", node2.SupportedDevices);

            // Test case 3: All devices
            var node3 = new NavigationNode("test3", "测试3", "/test3")
            {
                SupportedDevices = new[] { "desktop", "mobile", "tablet" }
            };
            Assert.Equal(3, node3.SupportedDevices.Length);
            Assert.Contains("desktop", node3.SupportedDevices);
            Assert.Contains("mobile", node3.SupportedDevices);
            Assert.Contains("tablet", node3.SupportedDevices);
        }

        [Fact]
        public void NavigationNode_Tags_ShouldWorkCorrectly()
        {
            // Test case 1: Multiple tags
            var node1 = new NavigationNode("test1", "测试1", "/test1")
            {
                Tags = new[] { "admin", "user" }
            };
            Assert.Equal(2, node1.Tags.Length);
            Assert.Contains("admin", node1.Tags);
            Assert.Contains("user", node1.Tags);

            // Test case 2: Single tag
            var node2 = new NavigationNode("test2", "测试2", "/test2")
            {
                Tags = new[] { "management" }
            };
            Assert.Single(node2.Tags);
            Assert.Equal("management", node2.Tags[0]);

            // Test case 3: Empty tags
            var node3 = new NavigationNode("test3", "测试3", "/test3")
            {
                Tags = new string[0]
            };
            Assert.Empty(node3.Tags);
        }

        [Theory]
        [InlineData(PlatformType.None)]
        [InlineData(PlatformType.System)]
        [InlineData(PlatformType.Tenant)]
        [InlineData(PlatformType.Both)]
        public void NavigationNode_PlatformType_ShouldWorkCorrectly(PlatformType platformType)
        {
            // Arrange & Act
            var node = new NavigationNode("test", "测试", "/test")
            {
                PlatformType = platformType
            };

            // Assert
            Assert.Equal(platformType, node.PlatformType);
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void NavigationNode_AuthAndExperimentalFlags_ShouldWorkCorrectly(bool requireAuth, bool isExperimental)
        {
            // Arrange & Act
            var node = new NavigationNode("test", "测试", "/test")
            {
                RequireAuth = requireAuth,
                IsExperimental = isExperimental
            };

            // Assert
            Assert.Equal(requireAuth, node.RequireAuth);
            Assert.Equal(isExperimental, node.IsExperimental);
        }

        [Theory]
        [InlineData("1.0.0", "2.0.0")]
        [InlineData("0.1.0", "1.0.0")]
        [InlineData(null, "1.0.0")]
        [InlineData("1.0.0", null)]
        public void NavigationNode_VersionRange_ShouldWorkCorrectly(string minVersion, string maxVersion)
        {
            // Arrange & Act
            var node = new NavigationNode("test", "测试", "/test")
            {
                MinVersion = minVersion,
                MaxVersion = maxVersion
            };

            // Assert
            Assert.Equal(minVersion, node.MinVersion);
            Assert.Equal(maxVersion, node.MaxVersion);
        }

        [Theory]
        [InlineData(-10)]
        [InlineData(0)]
        [InlineData(5)]
        [InlineData(100)]
        public void NavigationNode_Priority_ShouldWorkCorrectly(int priority)
        {
            // Arrange & Act
            var node = new NavigationNode("test", "测试", "/test")
            {
                Priority = priority
            };

            // Assert
            Assert.Equal(priority, node.Priority);
        }
    }
} 