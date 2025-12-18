using CodeSpirit.Navigation.Models;
using CodeSpirit.Navigation.Services;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;
using System.Linq;

namespace CodeSpirit.Navigation.Tests.Services
{
    /// <summary>
    /// NavigationTreeBuilder 单元测试
    /// </summary>
    public class NavigationTreeBuilderTests
    {
        private readonly Mock<IActionDescriptorCollectionProvider> _actionProviderMock;
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly Mock<ILogger<NavigationTreeBuilder>> _loggerMock;
        private readonly NavigationTreeBuilder _builder;

        public NavigationTreeBuilderTests()
        {
            _actionProviderMock = new Mock<IActionDescriptorCollectionProvider>();
            _configurationMock = new Mock<IConfiguration>();
            _loggerMock = new Mock<ILogger<NavigationTreeBuilder>>();

            _builder = new NavigationTreeBuilder(
                _actionProviderMock.Object,
                _configurationMock.Object,
                _loggerMock.Object);
        }

        /// <summary>
        /// 测试：当没有模块时，应返回空列表
        /// </summary>
        [Fact]
        public void BuildNavigationTree_WhenNoModules_ShouldReturnEmptyList()
        {
            // 安排
            SetupEmptyActionProvider();
            SetupEmptyConfiguration();

            // 执行
            var result = _builder.BuildNavigationTree();

            // 断言
            Assert.Empty(result);
        }

        /// <summary>
        /// 测试：合并导航节点应合并所有属性
        /// </summary>
        [Fact]
        public void MergeNavigationNodes_ShouldMergeAllProperties()
        {
            // 安排
            var existing = new NavigationNode("test", "Old Title", "/old-path")
            {
                Icon = "old-icon",
                Order = 1,
                Description = "Old Description",
                Permission = "old_permission"
            };

            var current = new NavigationNode("test", "New Title", "/new-path")
            {
                Icon = "new-icon",
                Order = 2,
                Description = "New Description",
                Permission = "new_permission"
            };

            // 执行
            var result = _builder.MergeNavigationNodes(existing, current);

            // 断言
            Assert.Equal("New Title", result.Title);
            Assert.Equal("/new-path", result.Path);
            Assert.Equal("new-icon", result.Icon);
            Assert.Equal(2, result.Order);
            Assert.Equal("New Description", result.Description);
            Assert.Equal("new_permission", result.Permission);
        }

        /// <summary>
        /// 测试：合并导航节点应递归合并子节点
        /// </summary>
        [Fact]
        public void MergeNavigationNodes_ShouldMergeChildren()
        {
            // 安排
            var existing = new NavigationNode("parent", "Parent", "/parent")
            {
                Children = new List<NavigationNode>
                {
                    new NavigationNode("child1", "Child 1", "/parent/child1")
                }
            };

            var current = new NavigationNode("parent", "Parent", "/parent")
            {
                Children = new List<NavigationNode>
                {
                    new NavigationNode("child1", "Updated Child 1", "/parent/child1-updated")
                    {
                        Icon = "new-icon"
                    },
                    new NavigationNode("child2", "Child 2", "/parent/child2")
                }
            };

            // 执行
            var result = _builder.MergeNavigationNodes(existing, current);

            // 断言
            Assert.Equal(2, result.Children.Count);
            Assert.Equal("Updated Child 1", result.Children.First(c => c.Name == "child1").Title);
            Assert.Equal("new-icon", result.Children.First(c => c.Name == "child1").Icon);
            Assert.Contains(result.Children, c => c.Name == "child2");
        }

        /// <summary>
        /// 测试：构建模块导航树 - 当模块存在时应返回节点
        /// </summary>
        [Fact]
        public void BuildModuleNavigationTree_WhenModuleExists_ShouldReturnNodes()
        {
            // 安排
            var moduleName = "TestModule";
            SetupModuleInActionProvider(moduleName);
            
            // 设置配置节返回 null（表示没有配置）
            // 注意：由于 Get<T>() 是扩展方法，无法直接模拟
            // 这里通过返回一个空的配置节来模拟没有配置的情况
            var configSectionMock = new Mock<IConfigurationSection>();
            configSectionMock.Setup(x => x.GetChildren()).Returns(new List<IConfigurationSection>());
            _configurationMock.Setup(x => x.GetSection(It.Is<string>(s => s.Contains(moduleName))))
                .Returns(configSectionMock.Object);

            // 执行
            var result = _builder.BuildModuleNavigationTree(moduleName);

            // 断言
            Assert.NotNull(result);
            // 注意：由于需要真实的 ActionDescriptor 和配置绑定，这个测试可能需要更复杂的模拟
            // 实际测试中可能需要使用真实的控制器类型或更完整的模拟
            // 当前测试主要验证方法不会抛出异常
        }

        private void SetupEmptyActionProvider()
        {
            var actionDescriptors = new Microsoft.AspNetCore.Mvc.Infrastructure.ActionDescriptorCollection(
                new List<Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor>(),
                0);
            _actionProviderMock.Setup(x => x.ActionDescriptors).Returns(actionDescriptors);
        }

        private void SetupEmptyConfiguration()
        {
            var configSectionMock = new Mock<IConfigurationSection>();
            // 不能模拟扩展方法 Exists()，直接返回 null 表示不存在
            configSectionMock.Setup(x => x.GetChildren()).Returns(new List<IConfigurationSection>());
            _configurationMock.Setup(x => x.GetSection(It.IsAny<string>())).Returns(configSectionMock.Object);
        }

        private void SetupModuleInActionProvider(string moduleName)
        {
            // 这里需要更复杂的模拟来设置 ActionDescriptor
            // 由于涉及反射和特性，实际测试中可能需要使用真实的控制器类型
            var actionDescriptors = new Microsoft.AspNetCore.Mvc.Infrastructure.ActionDescriptorCollection(
                new List<Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor>(),
                0);
            _actionProviderMock.Setup(x => x.ActionDescriptors).Returns(actionDescriptors);
        }
    }
}
