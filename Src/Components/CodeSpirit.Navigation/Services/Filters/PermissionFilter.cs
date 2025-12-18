using CodeSpirit.Navigation.Models;

namespace CodeSpirit.Navigation.Services.Filters
{
    /// <summary>
    /// 权限过滤器
    /// </summary>
    public class PermissionFilter : INavigationFilter
    {
        /// <summary>
        /// 过滤器优先级：2
        /// </summary>
        public int Priority => 2;

        /// <summary>
        /// 判断节点是否应该包含在结果中
        /// </summary>
        /// <param name="node">导航节点</param>
        /// <param name="context">过滤上下文</param>
        /// <returns>true表示包含，false表示排除</returns>
        public bool ShouldInclude(NavigationNode node, NavigationFilterContext context)
        {
            // 没有权限要求，直接包含
            if (string.IsNullOrEmpty(node.Permission))
                return true;

            // 没有权限服务，包含（由调用方决定）
            if (context.PermissionService == null)
                return true;

            // 检查用户是否有该权限
            return context.PermissionService.HasNavigationPermission(node.Permission);
        }
    }
}
