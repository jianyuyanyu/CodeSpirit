using System.Collections.Generic;
using System.Linq;
using CodeSpirit.Core.Enums;
using Newtonsoft.Json;

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

        /// <summary>
        /// 路由模板
        /// </summary>
        public string Route { get; set; }

        /// <summary>
        /// 所属模块
        /// </summary>
        public string ModuleName { get; set; }

        /// <summary>
        /// 平台类型
        /// </summary>
        public PlatformType PlatformType { get; set; } = PlatformType.Both;

        /// <summary>
        /// 导航项的分组/分类
        /// </summary>
        public string Group { get; set; }

        /// <summary>
        /// 导航项的标签
        /// </summary>
        public string[] Tags { get; set; } = [];

        /// <summary>
        /// 导航项的键值对元数据
        /// </summary>
        public Dictionary<string, object> MetaData { get; set; } = new();

        /// <summary>
        /// 是否需要认证
        /// </summary>
        public bool RequireAuth { get; set; } = true;

        /// <summary>
        /// 是否为测试功能
        /// </summary>
        public bool IsExperimental { get; set; } = false;

        /// <summary>
        /// 功能启用的最小版本号
        /// </summary>
        public string MinVersion { get; set; }

        /// <summary>
        /// 功能禁用的版本号
        /// </summary>
        public string MaxVersion { get; set; }

        /// <summary>
        /// 支持的设备类型
        /// </summary>
        public string[] SupportedDevices { get; set; } = ["desktop", "tablet", "mobile"];

        /// <summary>
        /// 导航项优先级
        /// </summary>
        public int Priority { get; set; } = 0;

        /// <summary>
        /// 快捷键
        /// </summary>
        public string Shortcut { get; set; }

        /// <summary>
        /// 徽章文本
        /// </summary>
        public string Badge { get; set; }

        /// <summary>
        /// 徽章样式类型
        /// </summary>
        public string BadgeType { get; set; } = "info";

        public NavigationNode(string name, string title, string path)
        {
            Name = name;
            Title = title;
            Path = path;
        }

        /// <summary>
        /// 深拷贝导航节点
        /// </summary>
        /// <returns></returns>
        public NavigationNode Clone()
        {
            var clone = new NavigationNode(Name, Title, Path)
            {
                Link = Link,
                Icon = Icon,
                Order = Order,
                ParentPath = ParentPath,
                Hidden = Hidden,
                Permission = Permission,
                Description = Description,
                IsExternal = IsExternal,
                Target = Target,
                Route = Route,
                ModuleName = ModuleName,
                PlatformType = PlatformType,
                Group = Group,
                Tags = Tags ?? [],
                MetaData = new Dictionary<string, object>(MetaData),
                RequireAuth = RequireAuth,
                IsExperimental = IsExperimental,
                MinVersion = MinVersion,
                MaxVersion = MaxVersion,
                SupportedDevices = SupportedDevices ?? [],
                Priority = Priority,
                Shortcut = Shortcut,
                Badge = Badge,
                BadgeType = BadgeType,
                Children = []
            };

            return clone;
        }
    }
}
