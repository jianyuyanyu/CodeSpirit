using CodeSpirit.Navigation.Models;
using CodeSpirit.Navigation.Services.Filters;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CodeSpirit.Navigation.Services
{
    /// <summary>
    /// 导航过滤服务实现
    /// </summary>
    public class NavigationFilterService : INavigationFilterService
    {
        private readonly List<INavigationFilter> _filters = new();
        private readonly ILogger<NavigationFilterService> _logger;

        /// <summary>
        /// 初始化导航过滤服务
        /// </summary>
        /// <param name="filters">过滤器集合</param>
        /// <param name="logger">日志记录器</param>
        public NavigationFilterService(
            IEnumerable<INavigationFilter> filters,
            ILogger<NavigationFilterService> logger)
        {
            _logger = logger;

            // 按优先级排序过滤器
            _filters.AddRange(filters.OrderBy(f => f.Priority));

            _logger.LogInformation(
                "Registered {Count} navigation filters: {FilterTypes}",
                _filters.Count,
                string.Join(", ", _filters.Select(f => f.GetType().Name)));
        }

        /// <summary>
        /// 注册自定义过滤器
        /// </summary>
        public void RegisterFilter(INavigationFilter filter)
        {
            _filters.Add(filter);
            _filters.Sort((a, b) => a.Priority.CompareTo(b.Priority));

            _logger.LogInformation("Registered custom filter: {FilterType}", filter.GetType().Name);
        }

        /// <summary>
        /// 根据上下文过滤导航节点
        /// </summary>
        public List<NavigationNode> FilterNodes(List<NavigationNode> nodes, NavigationFilterContext context)
        {
            if (nodes == null || !nodes.Any())
                return new List<NavigationNode>();

            var result = new List<NavigationNode>();

            foreach (var node in nodes)
            {
                // 深拷贝节点，避免修改原始数据
                var nodeCopy = node.Clone();

                // 递归过滤子节点
                var filteredChildren = FilterNodes(node.Children, context);

                // 应用所有过滤器
                bool shouldInclude = true;

                foreach (var filter in _filters)
                {
                    try
                    {
                        if (!filter.ShouldInclude(node, context))
                        {
                            shouldInclude = false;
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "Filter {FilterType} failed for node {NodeName}",
                            filter.GetType().Name,
                            node.Name);

                        // 过滤器异常时，默认包含节点
                        // 这样不会因为单个过滤器失败导致整个导航不可用
                    }
                }

                // 重要逻辑：如果节点本身不满足条件，但有子节点满足，则包含该节点
                if (!shouldInclude && filteredChildren.Any())
                {
                    shouldInclude = true;
                }

                if (shouldInclude)
                {
                    nodeCopy.Children = filteredChildren;
                    result.Add(nodeCopy);
                }
            }

            // 按 Order 和 Priority 排序
            return result
                .OrderBy(n => n.Order)
                .ThenByDescending(n => n.Priority)
                .ToList();
        }
    }
}
