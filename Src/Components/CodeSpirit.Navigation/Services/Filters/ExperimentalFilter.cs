using CodeSpirit.Navigation.Models;

namespace CodeSpirit.Navigation.Services.Filters
{
    /// <summary>
    /// 实验性功能过滤器
    /// </summary>
    public class ExperimentalFilter : INavigationFilter
    {
        /// <summary>
        /// 过滤器优先级：6
        /// </summary>
        public int Priority => 6;

        /// <summary>
        /// 判断节点是否应该包含在结果中
        /// </summary>
        /// <param name="node">导航节点</param>
        /// <param name="context">过滤上下文</param>
        /// <returns>true表示包含，false表示排除</returns>
        public bool ShouldInclude(NavigationNode node, NavigationFilterContext context)
        {
            // 实验性功能只在开发环境显示
            if (node.IsExperimental && !context.IsDevelopment)
                return false;

            return true;
        }
    }
}
