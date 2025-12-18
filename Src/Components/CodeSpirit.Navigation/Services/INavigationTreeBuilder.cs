using CodeSpirit.Navigation.Models;
using System.Collections.Generic;

namespace CodeSpirit.Navigation.Services
{
    /// <summary>
    /// 导航树构建器接口
    /// </summary>
    public interface INavigationTreeBuilder
    {
        /// <summary>
        /// 构建完整的导航树
        /// </summary>
        /// <returns>导航节点列表</returns>
        List<NavigationNode> BuildNavigationTree();

        /// <summary>
        /// 构建指定模块的导航树
        /// </summary>
        /// <param name="moduleName">模块名称</param>
        /// <returns>导航节点列表</returns>
        List<NavigationNode> BuildModuleNavigationTree(string moduleName);

        /// <summary>
        /// 合并代码导航和配置导航
        /// </summary>
        /// <param name="existing">已存在的导航节点（通常是配置节点）</param>
        /// <param name="current">当前的导航节点（通常是代码节点）</param>
        /// <returns>合并后的导航节点</returns>
        NavigationNode MergeNavigationNodes(NavigationNode existing, NavigationNode current);
    }
}
