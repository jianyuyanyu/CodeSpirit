using System.Collections.Generic;

namespace CodeSpirit.Navigation.Models
{
    /// <summary>
    /// 导航节点
    /// </summary>
    public class NavigationNode
    {
        /// <summary>
        /// 导航项标识
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 显示标题
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// 路由路径
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// 外部地址
        /// </summary>
        public string Link { get; set; }

        /// <summary>
        /// 图标
        /// </summary>
        public string Icon { get; set; }

        /// <summary>
        /// 排序值
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// 父级路径
        /// </summary>
        public string ParentPath { get; set; }

        /// <summary>
        /// 是否隐藏
        /// </summary>
        public bool Hidden { get; set; }

        /// <summary>
        /// 所需权限
        /// </summary>
        public string Permission { get; set; }

        /// <summary>
        /// 描述信息
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 是否为外部链接
        /// </summary>
        public bool IsExternal { get; set; }

        /// <summary>
        /// 打开方式（_blank/_self）
        /// </summary>
        public string Target { get; set; }

        /// <summary>
        /// 子节点
        /// </summary>
        public List<NavigationNode> Children { get; set; } = [];

        public string Route { get; set; }

        public string ModuleName { get; set; }

        public NavigationNode(string name, string title, string path)
        {
            Name = name;
            Title = title;
            Path = path;
        }
    }
}
