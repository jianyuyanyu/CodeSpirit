using CodeSpirit.Navigation.Models;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace CodeSpirit.Navigation.Tests.Models
{
    /// <summary>
    /// 导航节点模型单元测试
    /// </summary>
    public class NavigationNodeTests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public NavigationNodeTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        /// <summary>
        /// 测试创建导航节点 - 验证基本属性设置
        /// </summary>
        [Fact]
        public void Create_NavigationNode_WithBasicProperties_ShouldSetCorrectly()
        {
            // 记录测试信息
            _testOutputHelper.WriteLine("测试创建导航节点 - 基本属性设置");

            // 准备测试数据
            const string name = "dashboard";
            const string title = "仪表盘";
            const string path = "/dashboard";

            // 执行测试
            var node = new NavigationNode(name, title, path);

            // 验证结果
            Assert.Equal(name, node.Name);
            Assert.Equal(title, node.Title);
            Assert.Equal(path, node.Path);
            Assert.NotNull(node.Children);
            Assert.Empty(node.Children);

            _testOutputHelper.WriteLine($"测试创建导航节点 - 节点名称: {node.Name}, 标题: {node.Title}, 路径: {node.Path}");
        }

        /// <summary>
        /// 测试创建导航节点层次结构 - 验证父子关系
        /// </summary>
        [Fact]
        public void Create_NavigationTree_WithChildNodes_ShouldSetCorrectHierarchy()
        {
            // 记录测试信息
            _testOutputHelper.WriteLine("测试创建导航节点层次结构");

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

            _testOutputHelper.WriteLine($"测试创建导航节点层次结构 - 父节点: {parentNode.Name}, 子节点数量: {parentNode.Children.Count}");
        }

        /// <summary>
        /// 测试导航节点的外部链接属性
        /// </summary>
        [Fact]
        public void NavigationNode_WithExternalLink_ShouldSetExternalProperties()
        {
            // 记录测试信息
            _testOutputHelper.WriteLine("测试导航节点的外部链接属性");

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

            _testOutputHelper.WriteLine($"测试导航节点外部链接 - 链接: {externalNode.Link}, 打开方式: {externalNode.Target}");
        }
    }
} 