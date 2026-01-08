using CodeSpirit.Amis.Enums;
using CodeSpirit.Amis.Tabs;
using CodeSpirit.ConfigCenter.Dtos.Config;
using CodeSpirit.ConfigCenter.Models;
using CodeSpirit.ConfigCenter.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeSpirit.ConfigCenter.Configuration;

/// <summary>
/// 配置项Tab配置
/// </summary>
public class ConfigItemTabsConfig : TabsConfigBase<ConfigItemQueryDto>
{
    /// <summary>
    /// Tab键常量
    /// </summary>
    public static class TabKeys
    {
        public const string All = "all";
        public const string Init = "init";
        public const string Editing = "editing";
        public const string Released = "released";
    }

    /// <summary>
    /// 配置Tabs
    /// </summary>
    public override void Configure(TabsBuilder<ConfigItemQueryDto> builder)
    {
        // 配置容器
        builder.SetCountApi("api/config/ConfigItems/tab-counts")
               .SetDefaultTab(TabKeys.All)
               .SetTabsMode(TabsMode.Line)
               .SetShowBadge(true);

        // 全部配置
        builder.AddTab(TabKeys.All, "全部配置")
               .WithFilter(q => q.Status = null)
               .WithOrder(1)
               .WithCustomCount<ConfigItem>(async query =>
                   await query.CountAsync());

        // 初始状态
        builder.AddTab(TabKeys.Init, "初始状态")
               .WithFilter(q => q.Status = ConfigStatus.Init)
               .WithOrder(2)
               .WithBadgeLevel(BadgeLevel.Default)
               .WithCustomCount<ConfigItem>(async query =>
                   await query.Where(x => x.Status == ConfigStatus.Init).CountAsync());

        // 编辑中
        builder.AddTab(TabKeys.Editing, "编辑中")
               .WithFilter(q => q.Status = ConfigStatus.Editing)
               .WithOrder(3)
               .WithBadgeLevel(BadgeLevel.Warning)
               .WithCustomCount<ConfigItem>(async query =>
                   await query.Where(x => x.Status == ConfigStatus.Editing).CountAsync());

        // 已发布
        builder.AddTab(TabKeys.Released, "已发布")
               .WithFilter(q => q.Status = ConfigStatus.Released)
               .WithOrder(4)
               .WithBadgeLevel(BadgeLevel.Success)
               .WithCustomCount<ConfigItem>(async query =>
                   await query.Where(x => x.Status == ConfigStatus.Released).CountAsync());
    }
}
