using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Enums;
using CodeSpirit.Navigation.Models;
using CodeSpirit.Navigation.Tests.TestBase;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Xunit;

namespace CodeSpirit.Navigation.Tests.Integration
{
    /// <summary>
    /// 平台类型继承功能集成测试
    /// </summary>
    public class PlatformTypeInheritanceIntegrationTests : NavigationTestBase
    {
        /// <summary>
        /// 测试完整的继承流程 - 从默认值到处理后的结果
        /// </summary>
        [Fact]
        public void PlatformTypeInheritance_FullWorkflow_ShouldProcessCorrectly()
        {
            // Arrange - 创建测试控制器模拟
            var testAttribute = new NavigationAttribute(); // 使用默认的 Inherit
            var parentAttribute = new NavigationAttribute { PlatformType = PlatformType.System };
            
            // 验证默认值
            Assert.Equal(PlatformType.Inherit, testAttribute.PlatformType);
            
            // Act - 创建导航节点
            var parentNode = new NavigationNode("parent", "父节点", "/parent")
            {
                PlatformType = parentAttribute.PlatformType,
                OriginalPlatformType = parentAttribute.PlatformType
            };
            
            var childNode = new NavigationNode("child", "子节点", "/parent/child")
            {
                PlatformType = testAttribute.PlatformType,
                OriginalPlatformType = testAttribute.PlatformType
            };
            
            parentNode.Children.Add(childNode);
            var nodes = new List<NavigationNode> { parentNode };
            
            // 使用反射调用私有的继承处理方法
            var method = typeof(NavigationService).GetMethod("ProcessPlatformTypeInheritance", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method.Invoke(NavigationService, new object[] { nodes, null });
            
            // Assert
            Assert.Equal(PlatformType.System, parentNode.PlatformType);
            Assert.Equal(PlatformType.System, parentNode.OriginalPlatformType);
            
            Assert.Equal(PlatformType.System, childNode.PlatformType); // 继承了父级的System
            Assert.Equal(PlatformType.Inherit, childNode.OriginalPlatformType); // 原始配置保持不变
        }

        /// <summary>
        /// 测试NavigationConfigItem的默认值和继承
        /// </summary>
        [Fact]
        public void NavigationConfigItem_DefaultPlatformType_ShouldBeInherit()
        {
            // Arrange & Act
            var configItem = new NavigationConfigItem();
            
            // Assert
            Assert.Equal(PlatformType.Inherit, configItem.PlatformType);
        }

        /// <summary>
        /// 测试配置文件导航项的继承处理
        /// </summary>
        [Fact]
        public void ConfigurationBasedNavigation_Inheritance_ShouldProcessCorrectly()
        {
            // Arrange
            var parentConfig = new NavigationConfigItem
            {
                Name = "parent",
                Title = "父配置节点",
                Path = "/config/parent",
                PlatformType = PlatformType.Tenant,
                Children = new List<NavigationConfigItem>
                {
                    new NavigationConfigItem
                    {
                        Name = "child1",
                        Title = "子配置节点1",
                        Path = "/config/parent/child1",
                        PlatformType = PlatformType.Inherit // 使用继承
                    },
                    new NavigationConfigItem
                    {
                        Name = "child2",
                        Title = "子配置节点2",
                        Path = "/config/parent/child2",
                        PlatformType = PlatformType.Both // 明确设置
                    }
                }
            };

            // Act - 转换为导航节点
            var parentNode = ConvertConfigToNode(parentConfig);
            var nodes = new List<NavigationNode> { parentNode };
            
            // 处理继承
            var method = typeof(NavigationService).GetMethod("ProcessPlatformTypeInheritance", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method.Invoke(NavigationService, new object[] { nodes, null });

            // Assert
            Assert.Equal(PlatformType.Tenant, parentNode.PlatformType);
            Assert.Equal(PlatformType.Tenant, parentNode.OriginalPlatformType);
            
            var child1 = parentNode.Children[0];
            Assert.Equal(PlatformType.Tenant, child1.PlatformType); // 继承父级的Tenant
            Assert.Equal(PlatformType.Inherit, child1.OriginalPlatformType);
            
            var child2 = parentNode.Children[1];
            Assert.Equal(PlatformType.Both, child2.PlatformType); // 明确设置的Both
            Assert.Equal(PlatformType.Both, child2.OriginalPlatformType);
        }

        /// <summary>
        /// 测试复杂的多层级继承场景
        /// </summary>
        [Fact]
        public void ComplexMultiLevelInheritance_ShouldResolveCorrectly()
        {
            // Arrange - 创建复杂的层级结构
            var rootNode = new NavigationNode("root", "根节点", "/root")
            {
                PlatformType = PlatformType.System,
                OriginalPlatformType = PlatformType.System
            };

            var level1Inherit = new NavigationNode("level1_inherit", "一级继承", "/root/level1_inherit")
            {
                PlatformType = PlatformType.Inherit,
                OriginalPlatformType = PlatformType.Inherit
            };

            var level1Specific = new NavigationNode("level1_specific", "一级明确", "/root/level1_specific")
            {
                PlatformType = PlatformType.Tenant,
                OriginalPlatformType = PlatformType.Tenant
            };

            var level2FromInherit = new NavigationNode("level2_from_inherit", "二级从继承", "/root/level1_inherit/level2")
            {
                PlatformType = PlatformType.Inherit,
                OriginalPlatformType = PlatformType.Inherit
            };

            var level2FromSpecific = new NavigationNode("level2_from_specific", "二级从明确", "/root/level1_specific/level2")
            {
                PlatformType = PlatformType.Inherit,
                OriginalPlatformType = PlatformType.Inherit
            };

            var level3 = new NavigationNode("level3", "三级", "/root/level1_inherit/level2/level3")
            {
                PlatformType = PlatformType.Inherit,
                OriginalPlatformType = PlatformType.Inherit
            };

            // 构建层级关系
            level2FromInherit.Children.Add(level3);
            level1Inherit.Children.Add(level2FromInherit);
            level1Specific.Children.Add(level2FromSpecific);
            rootNode.Children.Add(level1Inherit);
            rootNode.Children.Add(level1Specific);

            var nodes = new List<NavigationNode> { rootNode };

            // Act
            var method = typeof(NavigationService).GetMethod("ProcessPlatformTypeInheritance", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method.Invoke(NavigationService, new object[] { nodes, null });

            // Assert
            // 根节点
            Assert.Equal(PlatformType.System, rootNode.PlatformType);
            Assert.Equal(PlatformType.System, rootNode.OriginalPlatformType);

            // 一级节点
            Assert.Equal(PlatformType.System, level1Inherit.PlatformType); // 继承根节点的System
            Assert.Equal(PlatformType.Inherit, level1Inherit.OriginalPlatformType);
            
            Assert.Equal(PlatformType.Tenant, level1Specific.PlatformType); // 明确设置的Tenant
            Assert.Equal(PlatformType.Tenant, level1Specific.OriginalPlatformType);

            // 二级节点
            Assert.Equal(PlatformType.System, level2FromInherit.PlatformType); // 继承level1Inherit的System
            Assert.Equal(PlatformType.Inherit, level2FromInherit.OriginalPlatformType);
            
            Assert.Equal(PlatformType.Tenant, level2FromSpecific.PlatformType); // 继承level1Specific的Tenant
            Assert.Equal(PlatformType.Inherit, level2FromSpecific.OriginalPlatformType);

            // 三级节点
            Assert.Equal(PlatformType.System, level3.PlatformType); // 继承level2FromInherit的System
            Assert.Equal(PlatformType.Inherit, level3.OriginalPlatformType);
        }

        /// <summary>
        /// 测试过滤器与继承的组合
        /// </summary>
        [Fact]
        public void InheritanceWithFiltering_ShouldWorkCorrectly()
        {
            // Arrange
            var systemParent = new NavigationNode("system_parent", "系统父节点", "/system")
            {
                PlatformType = PlatformType.System,
                OriginalPlatformType = PlatformType.System
            };

            var tenantParent = new NavigationNode("tenant_parent", "租户父节点", "/tenant")
            {
                PlatformType = PlatformType.Tenant,
                OriginalPlatformType = PlatformType.Tenant
            };

            var inheritChild1 = new NavigationNode("inherit_child1", "继承子节点1", "/system/child")
            {
                PlatformType = PlatformType.Inherit,
                OriginalPlatformType = PlatformType.Inherit
            };

            var inheritChild2 = new NavigationNode("inherit_child2", "继承子节点2", "/tenant/child")
            {
                PlatformType = PlatformType.Inherit,
                OriginalPlatformType = PlatformType.Inherit
            };

            systemParent.Children.Add(inheritChild1);
            tenantParent.Children.Add(inheritChild2);

            var nodes = new List<NavigationNode> { systemParent, tenantParent };

            // Act - 先处理继承
            var inheritanceMethod = typeof(NavigationService).GetMethod("ProcessPlatformTypeInheritance", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            inheritanceMethod.Invoke(NavigationService, new object[] { nodes, null });

            // 然后进行平台过滤
            var systemFiltered = NavigationService.FilterNodesByPlatform(nodes, PlatformType.System);
            var tenantFiltered = NavigationService.FilterNodesByPlatform(nodes, PlatformType.Tenant);

            // Assert
            // 系统平台过滤结果
            Assert.Single(systemFiltered);
            Assert.Equal("system_parent", systemFiltered[0].Name);
            Assert.Single(systemFiltered[0].Children);
            Assert.Equal("inherit_child1", systemFiltered[0].Children[0].Name);
            Assert.Equal(PlatformType.System, systemFiltered[0].Children[0].PlatformType);

            // 租户平台过滤结果
            Assert.Single(tenantFiltered);
            Assert.Equal("tenant_parent", tenantFiltered[0].Name);
            Assert.Single(tenantFiltered[0].Children);
            Assert.Equal("inherit_child2", tenantFiltered[0].Children[0].Name);
            Assert.Equal(PlatformType.Tenant, tenantFiltered[0].Children[0].PlatformType);
        }

        /// <summary>
        /// 将配置项转换为导航节点的辅助方法
        /// </summary>
        private NavigationNode ConvertConfigToNode(NavigationConfigItem config)
        {
            var node = new NavigationNode(config.Name, config.Title, config.Path)
            {
                PlatformType = config.PlatformType,
                OriginalPlatformType = config.PlatformType
            };

            foreach (var childConfig in config.Children)
            {
                node.Children.Add(ConvertConfigToNode(childConfig));
            }

            return node;
        }
    }
} 