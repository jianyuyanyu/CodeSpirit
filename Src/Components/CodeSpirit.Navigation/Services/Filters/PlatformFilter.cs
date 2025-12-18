using CodeSpirit.Core.Enums;
using CodeSpirit.Navigation.Models;

namespace CodeSpirit.Navigation.Services.Filters
{
    /// <summary>
    /// 平台类型过滤器
    /// </summary>
    public class PlatformFilter : INavigationFilter
    {
        /// <summary>
        /// 过滤器优先级：1（最高优先级）
        /// </summary>
        public int Priority => 1;

        /// <summary>
        /// 判断节点是否应该包含在结果中
        /// </summary>
        /// <param name="node">导航节点</param>
        /// <param name="context">过滤上下文</param>
        /// <returns>true表示包含，false表示排除</returns>
        public bool ShouldInclude(NavigationNode node, NavigationFilterContext context)
        {
            // 使用位运算检查平台类型匹配
            // 例如: Both (3) & System (1) = 1 (true)
            //      Tenant (2) & System (1) = 0 (false)
            return (node.PlatformType & context.PlatformType) != 0;
        }
    }
}
