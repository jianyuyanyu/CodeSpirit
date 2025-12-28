using System;
using System.Collections.Generic;

namespace CodeSpirit.Navigation.Models
{
    /// <summary>
    /// 导航缓存数据封装类（包含版本信息）
    /// </summary>
    public class NavigationCacheData
    {
        /// <summary>
        /// 版本哈希值（基于导航树内容的SHA256哈希）
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// 最后更新时间（UTC）
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// 导航树节点列表
        /// </summary>
        public List<NavigationNode> Nodes { get; set; }
    }
}

