using CodeSpirit.Navigation.Models;
using System.Linq;

namespace CodeSpirit.Navigation.Services.Filters
{
    /// <summary>
    /// 设备类型过滤器
    /// </summary>
    public class DeviceFilter : INavigationFilter
    {
        /// <summary>
        /// 过滤器优先级：5
        /// </summary>
        public int Priority => 5;

        /// <summary>
        /// 判断节点是否应该包含在结果中
        /// </summary>
        /// <param name="node">导航节点</param>
        /// <param name="context">过滤上下文</param>
        /// <returns>true表示包含，false表示排除</returns>
        public bool ShouldInclude(NavigationNode node, NavigationFilterContext context)
        {
            // 如果没有设备限制，则包含
            if (node.SupportedDevices == null || !node.SupportedDevices.Any())
                return true;

            // 如果没有指定设备类型，则包含
            if (string.IsNullOrEmpty(context.DeviceType))
                return true;

            // 检查设备类型是否支持
            return node.SupportedDevices.Contains(context.DeviceType, System.StringComparer.OrdinalIgnoreCase);
        }
    }
}
