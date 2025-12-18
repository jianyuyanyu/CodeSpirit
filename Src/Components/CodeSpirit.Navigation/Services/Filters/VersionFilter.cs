using CodeSpirit.Navigation.Models;
using System;

namespace CodeSpirit.Navigation.Services.Filters
{
    /// <summary>
    /// 版本过滤器
    /// </summary>
    public class VersionFilter : INavigationFilter
    {
        /// <summary>
        /// 过滤器优先级：4
        /// </summary>
        public int Priority => 4;

        /// <summary>
        /// 判断节点是否应该包含在结果中
        /// </summary>
        /// <param name="node">导航节点</param>
        /// <param name="context">过滤上下文</param>
        /// <returns>true表示包含，false表示排除</returns>
        public bool ShouldInclude(NavigationNode node, NavigationFilterContext context)
        {
            // 如果没有版本约束，则包含
            if (string.IsNullOrEmpty(context.CurrentVersion))
                return true;

            // 检查最小版本
            if (!string.IsNullOrEmpty(node.MinVersion))
            {
                if (CompareVersions(context.CurrentVersion, node.MinVersion) < 0)
                    return false;
            }

            // 检查最大版本
            if (!string.IsNullOrEmpty(node.MaxVersion))
            {
                if (CompareVersions(context.CurrentVersion, node.MaxVersion) > 0)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 比较版本号
        /// </summary>
        private int CompareVersions(string version1, string version2)
        {
            try
            {
                var v1 = new Version(version1);
                var v2 = new Version(version2);
                return v1.CompareTo(v2);
            }
            catch
            {
                return string.Compare(version1, version2, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
