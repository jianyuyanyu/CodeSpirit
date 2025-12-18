using CodeSpirit.Navigation.Models;
using CodeSpirit.Navigation.Services.Filters;
using System.Collections.Generic;

namespace CodeSpirit.Navigation.Services
{
    /// <summary>
    /// 导航过滤服务接口
    /// </summary>
    public interface INavigationFilterService
    {
        /// <summary>
        /// 根据上下文过滤导航节点
        /// </summary>
        /// <param name="nodes">导航节点列表</param>
        /// <param name="context">过滤上下文</param>
        /// <returns>过滤后的导航节点列表</returns>
        List<NavigationNode> FilterNodes(List<NavigationNode> nodes, NavigationFilterContext context);

        /// <summary>
        /// 注册自定义过滤器
        /// </summary>
        /// <param name="filter">过滤器实例</param>
        void RegisterFilter(INavigationFilter filter);
    }
}
