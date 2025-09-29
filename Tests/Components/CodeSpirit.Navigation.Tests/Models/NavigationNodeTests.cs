using CodeSpirit.Core.Enums;
using CodeSpirit.Navigation.Models;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace CodeSpirit.Navigation.Tests.Models
{
    /// <summary>
    /// NavigationNode 模型单元测试
    /// </summary>
    public class NavigationNodeTests
    {
        /// <summary>
        /// 测试NavigationNode的默认值
        /// </summary>
        [Fact]
        public void NavigationNode_DefaultValues_ShouldBeCorrect()
        {
            // Arrange & Act
            var node = new NavigationNode("test", "测试", "/test");

            // Assert
            Assert.Equal("test", node.Name);
            Assert.Equal("测试", node.Title);
            Assert.Equal("/test", node.Path);
            Assert.Equal(PlatformType.Both, node.PlatformType);
            Assert.Equal(PlatformType.Inherit, node.OriginalPlatformType);
            Assert.False(node.Hidden);
            Assert.True(node.RequireAuth);
            Assert.False(node.IsExperimental);
            Assert.Equal(0, node.Order);
            Assert.Equal(0, node.Priority);
            Assert.Equal("info", node.BadgeType);
            Assert.True(node.Visible);
            Assert.NotNull(node.Children);
            Assert.Empty(node.Children);
            Assert.NotNull(node.Tags);
            Assert.Empty(node.Tags);
            Assert.NotNull(node.MetaData);
            Assert.Empty(node.MetaData);
            Assert.NotNull(node.SupportedDevices);
            Assert.Equal(3, node.SupportedDevices.Length);
            Assert.Contains("desktop", node.SupportedDevices);
            Assert.Contains("tablet", node.SupportedDevices);
            Assert.Contains("mobile", node.SupportedDevices);
        }

        /// <summary>
        /// 测试NavigationNode的构造函数
        /// </summary>
        [Fact]
        public void NavigationNode_Constructor_ShouldSetRequiredProperties()
        {
            // Arrange
            var name = "testNode";
            var title = "测试节点";
            var path = "/test/path";

            // Act
            var node = new NavigationNode(name, title, path);

            // Assert
            Assert.Equal(name, node.Name);
            Assert.Equal(title, node.Title);
            Assert.Equal(path, node.Path);
        }

        /// <summary>
        /// 测试NavigationNode的PlatformType属性设置
        /// </summary>
        [Fact]
        public void NavigationNode_PlatformType_ShouldBeSettable()
        {
            // Arrange
            var node = new NavigationNode("test", "测试", "/test");

            // Act & Assert
            node.PlatformType = PlatformType.System;
            Assert.Equal(PlatformType.System, node.PlatformType);

            node.PlatformType = PlatformType.Tenant;
            Assert.Equal(PlatformType.Tenant, node.PlatformType);

            node.PlatformType = PlatformType.Both;
            Assert.Equal(PlatformType.Both, node.PlatformType);

            node.PlatformType = PlatformType.Inherit;
            Assert.Equal(PlatformType.Inherit, node.PlatformType);

            node.PlatformType = PlatformType.None;
            Assert.Equal(PlatformType.None, node.PlatformType);
        }

        /// <summary>
        /// 测试NavigationNode的OriginalPlatformType属性设置
        /// </summary>
        [Fact]
        public void NavigationNode_OriginalPlatformType_ShouldBeSettable()
        {
            // Arrange
            var node = new NavigationNode("test", "测试", "/test");

            // Act & Assert
            node.OriginalPlatformType = PlatformType.System;
            Assert.Equal(PlatformType.System, node.OriginalPlatformType);

            node.OriginalPlatformType = PlatformType.Tenant;
            Assert.Equal(PlatformType.Tenant, node.OriginalPlatformType);

            node.OriginalPlatformType = PlatformType.Both;
            Assert.Equal(PlatformType.Both, node.OriginalPlatformType);

            node.OriginalPlatformType = PlatformType.Inherit;
            Assert.Equal(PlatformType.Inherit, node.OriginalPlatformType);

            node.OriginalPlatformType = PlatformType.None;
            Assert.Equal(PlatformType.None, node.OriginalPlatformType);
        }

        /// <summary>
        /// 测试NavigationNode的Clone方法 - 基本属性
        /// </summary>
        [Fact]
        public void NavigationNode_Clone_ShouldCopyAllBasicProperties()
        {
            // Arrange
            var original = new NavigationNode("test", "测试", "/test")
            {
                Link = "http://example.com",
                Icon = "fa-test",
                Order = 100,
                ParentPath = "/parent",
                Hidden = true,
                Permission = "test.permission",
                Description = "测试描述",
                IsExternal = true,
                Target = "_blank",
                Route = "/api/test",
                ModuleName = "TestModule",
                PlatformType = PlatformType.System,
                OriginalPlatformType = PlatformType.Inherit,
                Group = "测试分组",
                Tags = new[] { "tag1", "tag2" },
                RequireAuth = false,
                IsExperimental = true,
                MinVersion = "1.0.0",
                MaxVersion = "2.0.0",
                SupportedDevices = new[] { "desktop" },
                Priority = 5,
                Shortcut = "Ctrl+T",
                Badge = "NEW",
                BadgeType = "success",
                Visible = false
            };

            // 添加元数据
            original.MetaData["key1"] = "value1";
            original.MetaData["key2"] = 42;

            // Act
            var clone = original.Clone();

            // Assert - 验证所有属性都被正确复制
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
            Assert.Equal(original.OriginalPlatformType, clone.OriginalPlatformType);
            Assert.Equal(original.Group, clone.Group);
            Assert.Equal(original.RequireAuth, clone.RequireAuth);
            Assert.Equal(original.IsExperimental, clone.IsExperimental);
            Assert.Equal(original.MinVersion, clone.MinVersion);
            Assert.Equal(original.MaxVersion, clone.MaxVersion);
            Assert.Equal(original.Priority, clone.Priority);
            Assert.Equal(original.Shortcut, clone.Shortcut);
            Assert.Equal(original.Badge, clone.Badge);
            Assert.Equal(original.BadgeType, clone.BadgeType);
            Assert.Equal(original.Visible, clone.Visible);
        }

        /// <summary>
        /// 测试NavigationNode的Clone方法 - 数组属性
        /// </summary>
        [Fact]
        public void NavigationNode_Clone_ShouldCopyArrayProperties()
        {
            // Arrange
            var original = new NavigationNode("test", "测试", "/test")
            {
                Tags = new[] { "tag1", "tag2", "tag3" },
                SupportedDevices = new[] { "desktop", "mobile" }
            };

            // Act
            var clone = original.Clone();

            // Assert
            Assert.NotNull(clone.Tags);
            Assert.Equal(original.Tags.Length, clone.Tags.Length);
            Assert.Equal(original.Tags, clone.Tags);
            
            Assert.NotNull(clone.SupportedDevices);
            Assert.Equal(original.SupportedDevices.Length, clone.SupportedDevices.Length);
            Assert.Equal(original.SupportedDevices, clone.SupportedDevices);

            // 验证是不同的数组实例（深拷贝）
            Assert.NotSame(original.Tags, clone.Tags);
            Assert.NotSame(original.SupportedDevices, clone.SupportedDevices);
        }

        /// <summary>
        /// 测试NavigationNode的Clone方法 - MetaData字典
        /// </summary>
        [Fact]
        public void NavigationNode_Clone_ShouldCopyMetaDataDictionary()
        {
            // Arrange
            var original = new NavigationNode("test", "测试", "/test");
            original.MetaData["stringKey"] = "stringValue";
            original.MetaData["numberKey"] = 123;
            original.MetaData["boolKey"] = true;

            // Act
            var clone = original.Clone();

            // Assert
            Assert.NotNull(clone.MetaData);
            Assert.Equal(original.MetaData.Count, clone.MetaData.Count);
            Assert.Equal("stringValue", clone.MetaData["stringKey"]);
            Assert.Equal(123, clone.MetaData["numberKey"]);
            Assert.Equal(true, clone.MetaData["boolKey"]);

            // 验证是不同的字典实例（深拷贝）
            Assert.NotSame(original.MetaData, clone.MetaData);
        }

        /// <summary>
        /// 测试NavigationNode的Clone方法 - Children集合
        /// </summary>
        [Fact]
        public void NavigationNode_Clone_ShouldCreateEmptyChildrenCollection()
        {
            // Arrange
            var original = new NavigationNode("parent", "父节点", "/parent");
            original.Children.Add(new NavigationNode("child1", "子节点1", "/parent/child1"));
            original.Children.Add(new NavigationNode("child2", "子节点2", "/parent/child2"));

            // Act
            var clone = original.Clone();

            // Assert
            Assert.NotNull(clone.Children);
            Assert.Empty(clone.Children); // Clone方法应该创建空的子节点集合
            Assert.NotSame(original.Children, clone.Children);
        }

        /// <summary>
        /// 测试NavigationNode的Clone方法 - 空数组和空字典处理
        /// </summary>
        [Fact]
        public void NavigationNode_Clone_ShouldHandleNullArraysAndDictionaries()
        {
            // Arrange
            var original = new NavigationNode("test", "测试", "/test")
            {
                Tags = null,
                SupportedDevices = null
            };

            // Act
            var clone = original.Clone();

            // Assert
            Assert.NotNull(clone.Tags);
            Assert.Empty(clone.Tags);
            Assert.NotNull(clone.SupportedDevices);
            Assert.Empty(clone.SupportedDevices);
            Assert.NotNull(clone.MetaData);
            Assert.Empty(clone.MetaData);
        }

        /// <summary>
        /// 测试NavigationNode的Clone方法 - 修改克隆不影响原始对象
        /// </summary>
        [Fact]
        public void NavigationNode_Clone_ModificationsShouldNotAffectOriginal()
        {
            // Arrange
            var original = new NavigationNode("test", "测试", "/test")
            {
                PlatformType = PlatformType.System,
                OriginalPlatformType = PlatformType.Inherit,
                Tags = new[] { "original" }
            };
            original.MetaData["key"] = "original";

            // Act
            var clone = original.Clone();
            clone.Title = "修改的标题";
            clone.PlatformType = PlatformType.Tenant;
            clone.OriginalPlatformType = PlatformType.System;
            clone.Tags = new[] { "modified" };
            clone.MetaData["key"] = "modified";
            clone.MetaData["newKey"] = "newValue";

            // Assert
            Assert.Equal("测试", original.Title); // 原始对象未被修改
            Assert.Equal(PlatformType.System, original.PlatformType);
            Assert.Equal(PlatformType.Inherit, original.OriginalPlatformType);
            Assert.Single(original.Tags);
            Assert.Equal("original", original.Tags[0]);
            Assert.Single(original.MetaData);
            Assert.Equal("original", original.MetaData["key"]);
            Assert.False(original.MetaData.ContainsKey("newKey"));

            Assert.Equal("修改的标题", clone.Title); // 克隆对象已被修改
            Assert.Equal(PlatformType.Tenant, clone.PlatformType);
            Assert.Equal(PlatformType.System, clone.OriginalPlatformType);
            Assert.Single(clone.Tags);
            Assert.Equal("modified", clone.Tags[0]);
            Assert.Equal(2, clone.MetaData.Count);
            Assert.Equal("modified", clone.MetaData["key"]);
            Assert.Equal("newValue", clone.MetaData["newKey"]);
        }

        /// <summary>
        /// 测试创建导航节点层次结构 - 验证父子关系
        /// </summary>
        [Fact]
        public void Create_NavigationTree_WithChildNodes_ShouldSetCorrectHierarchy()
        {
            // 创建父节点
            var parentNode = new NavigationNode("parent", "父节点", "/parent")
            {
                Icon = "folder",
                Order = 1,
                ModuleName = "TestModule"
            };

            // 创建子节点
            var childNode1 = new NavigationNode("child1", "子节点1", "/parent/child1")
            {
                ParentPath = "/parent",
                Order = 1,
                ModuleName = "TestModule"
            };

            var childNode2 = new NavigationNode("child2", "子节点2", "/parent/child2")
            {
                ParentPath = "/parent",
                Order = 2,
                ModuleName = "TestModule"
            };

            // 添加子节点到父节点
            parentNode.Children.Add(childNode1);
            parentNode.Children.Add(childNode2);

            // 验证结果
            Assert.Equal(2, parentNode.Children.Count);
            Assert.Equal("child1", parentNode.Children[0].Name);
            Assert.Equal("child2", parentNode.Children[1].Name);
            Assert.Equal("/parent", parentNode.Children[0].ParentPath);
        }

        /// <summary>
        /// 测试导航节点的外部链接属性
        /// </summary>
        [Fact]
        public void NavigationNode_WithExternalLink_ShouldSetExternalProperties()
        {
            // 创建外部链接节点
            var externalNode = new NavigationNode("external", "外部链接", "")
            {
                Link = "https://example.com",
                IsExternal = true,
                Target = "_blank",
                ModuleName = "TestModule"
            };

            // 验证结果
            Assert.True(externalNode.IsExternal);
            Assert.Equal("_blank", externalNode.Target);
            Assert.Equal("https://example.com", externalNode.Link);
            Assert.Empty(externalNode.Path);
        }

        /// <summary>
        /// 测试NavigationNode的Visible属性
        /// </summary>
        [Fact]
        public void NavigationNode_Visible_ShouldDefaultToTrueAndBeSettable()
        {
            // Arrange & Act
            var node = new NavigationNode("test", "测试", "/test");

            // Assert - 默认值应该为true
            Assert.True(node.Visible);

            // Act - 设置为false
            node.Visible = false;

            // Assert - 应该可以设置为false
            Assert.False(node.Visible);

            // Act - 设置为true
            node.Visible = true;

            // Assert - 应该可以设置为true
            Assert.True(node.Visible);
        }
    }
} 