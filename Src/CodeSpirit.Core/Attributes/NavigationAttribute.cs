using System;
using System.Collections.Generic;
using CodeSpirit.Core.Enums;

namespace CodeSpirit.Core.Attributes;

/// <summary>
/// 自定义属性，用于定义站点导航结构和元数据。
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class NavigationAttribute : Attribute
{
    /// <summary>
    /// 导航项的显示名称
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// 导航项的路由路径
    /// </summary>
    public string Path { get; set; }

    /// <summary>
    /// 导航项的图标
    /// </summary>
    public string Icon { get; set; }

    /// <summary>
    /// 导航项的排序值，值越小越靠前
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// 父级导航项的路径
    /// </summary>
    public string ParentPath { get; set; }

    /// <summary>
    /// 是否在导航菜单中隐藏
    /// </summary>
    public bool Hidden { get; set; }

    /// <summary>
    /// 访问该导航项所需的权限（基于权限名称）
    /// </summary>
    public string Permission { get; set; }

    /// <summary>
    /// 导航项的描述信息
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// 是否为外部链接
    /// </summary>
    public bool IsExternal { get; set; }

    /// <summary>
    /// 外部链接打开方式，例如：_blank, _self（将渲染成Iframe）
    /// </summary>
    public string Target { get; set; }

    /// <summary>
    /// 平台类型，支持系统平台、租户平台或两者
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
    public string MetaDataJson { get; set; }

    /// <summary>
    /// 是否需要认证
    /// </summary>
    public bool RequireAuth { get; set; } = true;

    /// <summary>
    /// 是否为测试功能（开发环境可见）
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
    /// 支持的设备类型（mobile, tablet, desktop）
    /// </summary>
    public string[] SupportedDevices { get; set; } = ["desktop", "tablet", "mobile"];

    /// <summary>
    /// 导航项优先级（高优先级项目在相同Order时优先显示）
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// 快捷键
    /// </summary>
    public string Shortcut { get; set; }

    /// <summary>
    /// 徽章文本（如：NEW、HOT等）
    /// </summary>
    public string Badge { get; set; }

    /// <summary>
    /// 徽章样式类型
    /// </summary>
    public string BadgeType { get; set; } = "info";
}