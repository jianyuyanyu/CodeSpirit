using CodeSpirit.Core.Enums;
using CodeSpirit.Navigation.Models;
using CodeSpirit.Navigation.Tests.TestBase;
using System.Collections.Generic;
using Xunit;
using System.Reflection;
using System;

namespace CodeSpirit.Navigation.Tests.Services
{
    /// <summary>
    /// 导航服务平台类型继承测试
    /// </summary>
    public class NavigationServiceInheritanceTests : NavigationTestBase
    {
        /// <summary>
        /// 测试解析平台类型 - 当前节点为继承且父级为系统平台时
        /// </summary>
        [Fact]
        public void ResolvePlatformType_InheritWithSystemParent_ShouldReturnSystem()
        {
            // Arrange
            var currentPlatformType = PlatformType.Inherit;
            var parentPlatformType = PlatformType.System;

            // Act
            var result = InvokePrivateMethod<PlatformType>("ResolvePlatformType", currentPlatformType, parentPlatformType);

            // Assert
            Assert.Equal(PlatformType.System, result);
        }

        /// <summary>
        /// 测试解析平台类型 - 当前节点为继承且父级为租户平台时
        /// </summary>
        [Fact]
        public void ResolvePlatformType_InheritWithTenantParent_ShouldReturnTenant()
        {
            // Arrange
            var currentPlatformType = PlatformType.Inherit;
            var parentPlatformType = PlatformType.Tenant;

            // Act
            var result = InvokePrivateMethod<PlatformType>("ResolvePlatformType", currentPlatformType, parentPlatformType);

            // Assert
            Assert.Equal(PlatformType.Tenant, result);
        }

        /// <summary>
        /// 测试解析平台类型 - 当前节点为继承且父级为双平台时
        /// </summary>
        [Fact]
        public void ResolvePlatformType_InheritWithBothParent_ShouldReturnBoth()
        {
            // Arrange
            var currentPlatformType = PlatformType.Inherit;
            var parentPlatformType = PlatformType.Both;

            // Act
            var result = InvokePrivateMethod<PlatformType>("ResolvePlatformType", currentPlatformType, parentPlatformType);

            // Assert
            Assert.Equal(PlatformType.Both, result);
        }

        /// <summary>
        /// 测试解析平台类型 - 当前节点为继承且父级也为继承时，应返回Both
        /// </summary>
        [Fact]
        public void ResolvePlatformType_InheritWithInheritParent_ShouldReturnBoth()
        {
            // Arrange
            var currentPlatformType = PlatformType.Inherit;
            var parentPlatformType = PlatformType.Inherit;

            // Act
            var result = InvokePrivateMethod<PlatformType>("ResolvePlatformType", currentPlatformType, parentPlatformType);

            // Assert
            Assert.Equal(PlatformType.Both, result);
        }

        /// <summary>
        /// 测试解析平台类型 - 当前节点为继承且没有父级时，应返回Both
        /// </summary>
        [Fact]
        public void ResolvePlatformType_InheritWithNoParent_ShouldReturnBoth()
        {
            // Arrange
            var currentPlatformType = PlatformType.Inherit;

            // Act
            var result = InvokePrivateMethod<PlatformType>("ResolvePlatformType", currentPlatformType, null);

            // Assert
            Assert.Equal(PlatformType.Both, result);
        }

        /// <summary>
        /// 测试解析平台类型 - 当前节点为具体平台类型时，应返回当前类型
        /// </summary>
        [Fact]
        public void ResolvePlatformType_SpecificPlatformType_ShouldReturnCurrent()
        {
            // Arrange & Act & Assert
            var result1 = InvokePrivateMethod<PlatformType>("ResolvePlatformType", PlatformType.System, PlatformType.Tenant);
            Assert.Equal(PlatformType.System, result1);

            var result2 = InvokePrivateMethod<PlatformType>("ResolvePlatformType", PlatformType.Tenant, PlatformType.System);
            Assert.Equal(PlatformType.Tenant, result2);

            var result3 = InvokePrivateMethod<PlatformType>("ResolvePlatformType", PlatformType.Both, PlatformType.System);
            Assert.Equal(PlatformType.Both, result3);

            var result4 = InvokePrivateMethod<PlatformType>("ResolvePlatformType", PlatformType.None, PlatformType.Both);
            Assert.Equal(PlatformType.None, result4);
        }

        /// <summary>
        /// 测试平台类型继承处理 - 单层级继承
        /// </summary>
        [Fact]
        public void ProcessPlatformTypeInheritance_SingleLevelInheritance_ShouldResolveCorrectly()
        {
            // Arrange
            var nodes = new List<NavigationNode>
            {
                new NavigationNode("parent", "父节点", "/parent")
                {
                    PlatformType = PlatformType.System,
                    OriginalPlatformType = PlatformType.System,
                    Children = new List<NavigationNode>
                    {
                        new NavigationNode("child1", "子节点1", "/parent/child1")
                        {
                            PlatformType = PlatformType.Inherit,
                            OriginalPlatformType = PlatformType.Inherit
                        },
                        new NavigationNode("child2", "子节点2", "/parent/child2")
                        {
                            PlatformType = PlatformType.Tenant,
                            OriginalPlatformType = PlatformType.Tenant
                        }
                    }
                }
            };

            // Act
            InvokePrivateMethod("ProcessPlatformTypeInheritance", nodes, null);

            // Assert
            var parent = nodes[0];
            Assert.Equal(PlatformType.System, parent.PlatformType); // 父节点不变
            
            var child1 = parent.Children[0];
            Assert.Equal(PlatformType.System, child1.PlatformType); // 继承父级的System
            Assert.Equal(PlatformType.Inherit, child1.OriginalPlatformType); // 原始配置保持不变
            
            var child2 = parent.Children[1];
            Assert.Equal(PlatformType.Tenant, child2.PlatformType); // 明确设置的不变
            Assert.Equal(PlatformType.Tenant, child2.OriginalPlatformType);
        }

        /// <summary>
        /// 测试平台类型继承处理 - 多层级继承
        /// </summary>
        [Fact]
        public void ProcessPlatformTypeInheritance_MultiLevelInheritance_ShouldResolveCorrectly()
        {
            // Arrange
            var nodes = new List<NavigationNode>
            {
                new NavigationNode("root", "根节点", "/root")
                {
                    PlatformType = PlatformType.Tenant,
                    OriginalPlatformType = PlatformType.Tenant,
                    Children = new List<NavigationNode>
                    {
                        new NavigationNode("level1", "一级节点", "/root/level1")
                        {
                            PlatformType = PlatformType.Inherit,
                            OriginalPlatformType = PlatformType.Inherit,
                            Children = new List<NavigationNode>
                            {
                                new NavigationNode("level2", "二级节点", "/root/level1/level2")
                                {
                                    PlatformType = PlatformType.Inherit,
                                    OriginalPlatformType = PlatformType.Inherit
                                }
                            }
                        }
                    }
                }
            };

            // Act
            InvokePrivateMethod("ProcessPlatformTypeInheritance", nodes, null);

            // Assert
            var root = nodes[0];
            Assert.Equal(PlatformType.Tenant, root.PlatformType);
            
            var level1 = root.Children[0];
            Assert.Equal(PlatformType.Tenant, level1.PlatformType); // 继承根节点的Tenant
            Assert.Equal(PlatformType.Inherit, level1.OriginalPlatformType);
            
            var level2 = level1.Children[0];
            Assert.Equal(PlatformType.Tenant, level2.PlatformType); // 继承一级节点的Tenant
            Assert.Equal(PlatformType.Inherit, level2.OriginalPlatformType);
        }

        /// <summary>
        /// 测试平台类型继承处理 - 根节点为继承时默认为Both
        /// </summary>
        [Fact]
        public void ProcessPlatformTypeInheritance_RootNodeInherit_ShouldDefaultToBoth()
        {
            // Arrange
            var nodes = new List<NavigationNode>
            {
                new NavigationNode("root", "根节点", "/root")
                {
                    PlatformType = PlatformType.Inherit,
                    OriginalPlatformType = PlatformType.Inherit,
                    Children = new List<NavigationNode>
                    {
                        new NavigationNode("child", "子节点", "/root/child")
                        {
                            PlatformType = PlatformType.Inherit,
                            OriginalPlatformType = PlatformType.Inherit
                        }
                    }
                }
            };

            // Act
            InvokePrivateMethod("ProcessPlatformTypeInheritance", nodes, null);

            // Assert
            var root = nodes[0];
            Assert.Equal(PlatformType.Both, root.PlatformType); // 根节点继承时默认为Both
            
            var child = root.Children[0];
            Assert.Equal(PlatformType.Both, child.PlatformType); // 继承根节点的Both
        }

        /// <summary>
        /// 测试平台类型继承处理 - 混合配置场景
        /// </summary>
        [Fact]
        public void ProcessPlatformTypeInheritance_MixedConfiguration_ShouldResolveCorrectly()
        {
            // Arrange
            var nodes = new List<NavigationNode>
            {
                new NavigationNode("parent1", "父节点1", "/parent1")
                {
                    PlatformType = PlatformType.System,
                    OriginalPlatformType = PlatformType.System,
                    Children = new List<NavigationNode>
                    {
                        new NavigationNode("inherit_child", "继承子节点", "/parent1/inherit")
                        {
                            PlatformType = PlatformType.Inherit,
                            OriginalPlatformType = PlatformType.Inherit
                        },
                        new NavigationNode("specific_child", "指定子节点", "/parent1/specific")
                        {
                            PlatformType = PlatformType.Tenant,
                            OriginalPlatformType = PlatformType.Tenant,
                            Children = new List<NavigationNode>
                            {
                                new NavigationNode("grandchild", "孙节点", "/parent1/specific/grandchild")
                                {
                                    PlatformType = PlatformType.Inherit,
                                    OriginalPlatformType = PlatformType.Inherit
                                }
                            }
                        }
                    }
                },
                new NavigationNode("parent2", "父节点2", "/parent2")
                {
                    PlatformType = PlatformType.Both,
                    OriginalPlatformType = PlatformType.Both,
                    Children = new List<NavigationNode>
                    {
                        new NavigationNode("inherit_child2", "继承子节点2", "/parent2/inherit")
                        {
                            PlatformType = PlatformType.Inherit,
                            OriginalPlatformType = PlatformType.Inherit
                        }
                    }
                }
            };

            // Act
            InvokePrivateMethod("ProcessPlatformTypeInheritance", nodes, null);

            // Assert
            // 第一个父节点及其子节点
            var parent1 = nodes[0];
            Assert.Equal(PlatformType.System, parent1.PlatformType);
            
            var inheritChild = parent1.Children[0];
            Assert.Equal(PlatformType.System, inheritChild.PlatformType); // 继承System
            Assert.Equal(PlatformType.Inherit, inheritChild.OriginalPlatformType);
            
            var specificChild = parent1.Children[1];
            Assert.Equal(PlatformType.Tenant, specificChild.PlatformType); // 明确指定Tenant
            Assert.Equal(PlatformType.Tenant, specificChild.OriginalPlatformType);
            
            var grandchild = specificChild.Children[0];
            Assert.Equal(PlatformType.Tenant, grandchild.PlatformType); // 继承specificChild的Tenant
            Assert.Equal(PlatformType.Inherit, grandchild.OriginalPlatformType);
            
            // 第二个父节点及其子节点
            var parent2 = nodes[1];
            Assert.Equal(PlatformType.Both, parent2.PlatformType);
            
            var inheritChild2 = parent2.Children[0];
            Assert.Equal(PlatformType.Both, inheritChild2.PlatformType); // 继承Both
            Assert.Equal(PlatformType.Inherit, inheritChild2.OriginalPlatformType);
        }

        /// <summary>
        /// 测试平台类型继承处理 - 空节点列表
        /// </summary>
        [Fact]
        public void ProcessPlatformTypeInheritance_EmptyNodes_ShouldNotThrow()
        {
            // Arrange
            var nodes = new List<NavigationNode>();

            // Act & Assert (不应抛出异常)
            InvokePrivateMethod("ProcessPlatformTypeInheritance", nodes, null);
        }

        /// <summary>
        /// 测试平台类型继承处理 - 节点没有子节点
        /// </summary>
        [Fact]
        public void ProcessPlatformTypeInheritance_NoChildren_ShouldProcessCorrectly()
        {
            // Arrange
            var nodes = new List<NavigationNode>
            {
                new NavigationNode("single", "单节点", "/single")
                {
                    PlatformType = PlatformType.Inherit,
                    OriginalPlatformType = PlatformType.Inherit
                }
            };

            // Act
            InvokePrivateMethod("ProcessPlatformTypeInheritance", nodes, null);

            // Assert
            var single = nodes[0];
            Assert.Equal(PlatformType.Both, single.PlatformType); // 根节点继承时默认为Both
            Assert.Equal(PlatformType.Inherit, single.OriginalPlatformType);
        }

        /// <summary>
        /// 调用私有方法的辅助方法
        /// </summary>
        /// <typeparam name="T">返回类型</typeparam>
        /// <param name="methodName">方法名</param>
        /// <param name="parameters">参数</param>
        /// <returns>方法返回值</returns>
        private T InvokePrivateMethod<T>(string methodName, params object[] parameters)
        {
            var method = typeof(NavigationService).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (method == null)
            {
                throw new InvalidOperationException($"Method '{methodName}' not found");
            }
            
            var result = method.Invoke(NavigationService, parameters);
            return (T)result;
        }

        /// <summary>
        /// 调用私有方法的辅助方法（无返回值）
        /// </summary>
        /// <param name="methodName">方法名</param>
        /// <param name="parameters">参数</param>
        private void InvokePrivateMethod(string methodName, params object[] parameters)
        {
            var method = typeof(NavigationService).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (method == null)
            {
                throw new InvalidOperationException($"Method '{methodName}' not found");
            }
            
            method.Invoke(NavigationService, parameters);
        }
    }
} 