using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Enums;
using Xunit;

namespace CodeSpirit.Navigation.Tests.Attributes
{
    /// <summary>
    /// NavigationAttribute 单元测试
    /// </summary>
    public class NavigationAttributeTests
    {
        /// <summary>
        /// 测试NavigationAttribute的默认值
        /// </summary>
        [Fact]
        public void NavigationAttribute_DefaultValues_ShouldBeCorrect()
        {
            // Arrange & Act
            var attribute = new NavigationAttribute();

            // Assert
            Assert.Equal(PlatformType.Inherit, attribute.PlatformType);
            Assert.Equal(0, attribute.Order);
            Assert.False(attribute.Hidden);
            Assert.False(attribute.IsExternal);
            Assert.True(attribute.RequireAuth);
            Assert.False(attribute.IsExperimental);
            Assert.Equal(0, attribute.Priority);
            Assert.Equal("info", attribute.BadgeType);
            Assert.NotNull(attribute.Tags);
            Assert.Empty(attribute.Tags);
            Assert.NotNull(attribute.SupportedDevices);
            Assert.Equal(3, attribute.SupportedDevices.Length);
            Assert.Contains("desktop", attribute.SupportedDevices);
            Assert.Contains("tablet", attribute.SupportedDevices);
            Assert.Contains("mobile", attribute.SupportedDevices);
        }

        /// <summary>
        /// 测试NavigationAttribute的PlatformType属性设置
        /// </summary>
        [Fact]
        public void NavigationAttribute_PlatformType_ShouldBeSettable()
        {
            // Arrange
            var attribute = new NavigationAttribute();

            // Act & Assert - 测试各种平台类型
            attribute.PlatformType = PlatformType.System;
            Assert.Equal(PlatformType.System, attribute.PlatformType);

            attribute.PlatformType = PlatformType.Tenant;
            Assert.Equal(PlatformType.Tenant, attribute.PlatformType);

            attribute.PlatformType = PlatformType.Both;
            Assert.Equal(PlatformType.Both, attribute.PlatformType);

            attribute.PlatformType = PlatformType.Inherit;
            Assert.Equal(PlatformType.Inherit, attribute.PlatformType);

            attribute.PlatformType = PlatformType.None;
            Assert.Equal(PlatformType.None, attribute.PlatformType);
        }

        /// <summary>
        /// 测试NavigationAttribute的所有属性设置
        /// </summary>
        [Fact]
        public void NavigationAttribute_AllProperties_ShouldBeSettable()
        {
            // Arrange
            var attribute = new NavigationAttribute();

            // Act
            attribute.Title = "测试标题";
            attribute.Path = "/test/path";
            attribute.Icon = "fa-test";
            attribute.Order = 100;
            attribute.ParentPath = "/parent";
            attribute.Hidden = true;
            attribute.Permission = "test.permission";
            attribute.Description = "测试描述";
            attribute.IsExternal = true;
            attribute.Target = "_blank";
            attribute.PlatformType = PlatformType.System;
            attribute.Group = "测试分组";
            attribute.Tags = new[] { "tag1", "tag2" };
            attribute.MetaDataJson = "{\"key\":\"value\"}";
            attribute.RequireAuth = false;
            attribute.IsExperimental = true;
            attribute.MinVersion = "1.0.0";
            attribute.MaxVersion = "2.0.0";
            attribute.SupportedDevices = new[] { "desktop" };
            attribute.Priority = 5;
            attribute.Shortcut = "Ctrl+T";
            attribute.Badge = "NEW";
            attribute.BadgeType = "success";

            // Assert
            Assert.Equal("测试标题", attribute.Title);
            Assert.Equal("/test/path", attribute.Path);
            Assert.Equal("fa-test", attribute.Icon);
            Assert.Equal(100, attribute.Order);
            Assert.Equal("/parent", attribute.ParentPath);
            Assert.True(attribute.Hidden);
            Assert.Equal("test.permission", attribute.Permission);
            Assert.Equal("测试描述", attribute.Description);
            Assert.True(attribute.IsExternal);
            Assert.Equal("_blank", attribute.Target);
            Assert.Equal(PlatformType.System, attribute.PlatformType);
            Assert.Equal("测试分组", attribute.Group);
            Assert.Equal(2, attribute.Tags.Length);
            Assert.Contains("tag1", attribute.Tags);
            Assert.Contains("tag2", attribute.Tags);
            Assert.Equal("{\"key\":\"value\"}", attribute.MetaDataJson);
            Assert.False(attribute.RequireAuth);
            Assert.True(attribute.IsExperimental);
            Assert.Equal("1.0.0", attribute.MinVersion);
            Assert.Equal("2.0.0", attribute.MaxVersion);
            Assert.Single(attribute.SupportedDevices);
            Assert.Contains("desktop", attribute.SupportedDevices);
            Assert.Equal(5, attribute.Priority);
            Assert.Equal("Ctrl+T", attribute.Shortcut);
            Assert.Equal("NEW", attribute.Badge);
            Assert.Equal("success", attribute.BadgeType);
        }

        /// <summary>
        /// 测试NavigationAttribute的构造函数和初始状态
        /// </summary>
        [Fact]
        public void NavigationAttribute_Constructor_ShouldInitializeCorrectly()
        {
            // Act
            var attribute = new NavigationAttribute();

            // Assert - 验证可变属性的初始状态
            Assert.Null(attribute.Title);
            Assert.Null(attribute.Path);
            Assert.Null(attribute.Icon);
            Assert.Null(attribute.ParentPath);
            Assert.Null(attribute.Permission);
            Assert.Null(attribute.Description);
            Assert.Null(attribute.Target);
            Assert.Null(attribute.Group);
            Assert.Null(attribute.MetaDataJson);
            Assert.Null(attribute.MinVersion);
            Assert.Null(attribute.MaxVersion);
            Assert.Null(attribute.Shortcut);
            Assert.Null(attribute.Badge);
        }
    }
} 