using CodeSpirit.Navigation.Models;

namespace CodeSpirit.Navigation.Services.Filters
{
    /// <summary>
    /// 认证过滤器
    /// </summary>
    public class AuthenticationFilter : INavigationFilter
    {
        /// <summary>
        /// 过滤器优先级：3
        /// </summary>
        public int Priority => 3;

        /// <summary>
        /// 判断节点是否应该包含在结果中
        /// </summary>
        /// <param name="node">导航节点</param>
        /// <param name="context">过滤上下文</param>
        /// <returns>true表示包含，false表示排除</returns>
        public bool ShouldInclude(NavigationNode node, NavigationFilterContext context)
        {
            // 如果节点需要认证但用户未认证，则排除
            if (node.RequireAuth && !context.IsAuthenticated)
                return false;

            return true;
        }
    }
}
