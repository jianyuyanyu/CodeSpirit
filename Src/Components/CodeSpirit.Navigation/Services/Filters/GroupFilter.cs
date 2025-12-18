using CodeSpirit.Navigation.Models;
using System.Linq;

namespace CodeSpirit.Navigation.Services.Filters
{
    /// <summary>
    /// 分组过滤器
    /// </summary>
    public class GroupFilter : INavigationFilter
    {
        /// <summary>
        /// 过滤器优先级：7
        /// </summary>
        public int Priority => 7;

        /// <summary>
        /// 判断节点是否应该包含在结果中
        /// </summary>
        /// <param name="node">导航节点</param>
        /// <param name="context">过滤上下文</param>
        /// <returns>true表示包含，false表示排除</returns>
        public bool ShouldInclude(NavigationNode node, NavigationFilterContext context)
        {
            // 如果没有分组过滤器，则包含所有
            if (context.GroupFilter == null || !context.GroupFilter.Any())
                return true;

            // 如果节点没有分组，根据策略决定
            if (string.IsNullOrEmpty(node.Group))
                return true; // 默认包含无分组的节点

            // 检查节点分组是否在过滤器中
            return context.GroupFilter.Contains(node.Group, System.StringComparer.OrdinalIgnoreCase);
        }
    }
}
