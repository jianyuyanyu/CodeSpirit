using CodeSpirit.Navigation.Models;

namespace CodeSpirit.Navigation.Services.Filters
{
    /// <summary>
    /// 导航过滤器接口
    /// </summary>
    public interface INavigationFilter
    {
        /// <summary>
        /// 判断节点是否应该包含在结果中
        /// </summary>
        /// <param name="node">导航节点</param>
        /// <param name="context">过滤上下文</param>
        /// <returns>true表示包含，false表示排除</returns>
        bool ShouldInclude(NavigationNode node, NavigationFilterContext context);

        /// <summary>
        /// 过滤器优先级（越小越先执行）
        /// </summary>
        int Priority { get; }
    }
}
