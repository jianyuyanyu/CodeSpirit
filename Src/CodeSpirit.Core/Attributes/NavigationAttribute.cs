using System;

namespace CodeSpirit.Core.Attributes;

/// <summary>
/// 自定义属性，用于定义站点导航结构和元数据。
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
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
}