using CodeSpirit.Navigation.Models;
using System.Linq;

namespace CodeSpirit.Navigation.Services.Filters
{
    /// <summary>
    /// 标签过滤器
    /// </summary>
    public class TagFilter : INavigationFilter
    {
        /// <summary>
        /// 过滤器优先级：8
        /// </summary>
        public int Priority => 8;

        /// <summary>
        /// 判断节点是否应该包含在结果中
        /// </summary>
        /// <param name="node">导航节点</param>
        /// <param name="context">过滤上下文</param>
        /// <returns>true表示包含，false表示排除</returns>
        public bool ShouldInclude(NavigationNode node, NavigationFilterContext context)
        {
            // 如果没有用户标签，则包含所有
            if (context.UserTags == null || !context.UserTags.Any())
                return true;

            // 如果节点没有标签，则包含
            if (node.Tags == null || !node.Tags.Any())
                return true;

            // 检查是否有交集
            return node.Tags.Intersect(context.UserTags, System.StringComparer.OrdinalIgnoreCase).Any();
        }
    }
}
