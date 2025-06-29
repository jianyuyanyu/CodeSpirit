using CodeSpirit.UdlCards.Core;
using CodeSpirit.UdlCards.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CodeSpirit.UdlCards.Builders;

/// <summary>
/// 信息网格卡片建构器
/// </summary>
public class InfoGridCardBuilder : IUdlCardBuilder<InfoGridCardConfig>, IUdlCardBuilderBase
{
    private readonly ILogger<InfoGridCardBuilder> _logger;

    /// <summary>
    /// 无参构造函数（用于测试）
    /// </summary>
    public InfoGridCardBuilder() : this(NullLogger<InfoGridCardBuilder>.Instance)
    {
    }

    /// <summary>
    /// 带日志的构造函数
    /// </summary>
    /// <param name="logger">日志服务</param>
    public InfoGridCardBuilder(ILogger<InfoGridCardBuilder> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 支持的卡片类型
    /// </summary>
    public string CardType => "info-grid";

    /// <summary>
    /// 构建Amis信息网格卡片配置
    /// </summary>
    public Dictionary<string, object> Build(InfoGridCardConfig cardConfig)
    {
        var card = new Dictionary<string, object>
        {
            ["type"] = "info-grid",
            ["id"] = cardConfig.Id,
            ["className"] = "amis-cards-info-grid"
        };

        // 设置标题和描述
        if (!string.IsNullOrEmpty(cardConfig.Title))
        {
            card["title"] = cardConfig.Title;
        }

        if (!string.IsNullOrEmpty(cardConfig.Description))
        {
            card["description"] = cardConfig.Description;
        }

        // 设置主题
        if (!string.IsNullOrEmpty(cardConfig.Theme))
        {
            card["theme"] = cardConfig.Theme;
        }

        // 构建网格配置
        card["grid"] = new Dictionary<string, object>
        {
            ["columns"] = cardConfig.Grid.Columns,
            ["gap"] = cardConfig.Grid.Gap,
            ["responsive"] = cardConfig.Grid.Responsive
        };

        // 构建网格项
        if (cardConfig.Items.Count > 0)
        {
            card["items"] = cardConfig.Items.Select(item => new Dictionary<string, object>
            {
                ["title"] = item.Title ?? "",
                ["value"] = item.Value ?? "",
                ["icon"] = item.Icon?.Name ?? string.Empty,
                ["theme"] = item.Theme ?? string.Empty,
                ["highlight"] = item.Highlight
            }).ToList();
        }

        return card;
    }

    /// <summary>
    /// 验证卡片配置
    /// </summary>
    public bool Validate(InfoGridCardConfig cardConfig)
    {
        if (cardConfig.Items.Count == 0)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// 构建Amis卡片配置（非泛型接口实现）
    /// </summary>
    Dictionary<string, object> IUdlCardBuilderBase.Build(UdlCardConfig cardConfig)
    {
        if (cardConfig is not InfoGridCardConfig gridConfig)
        {
            throw new ArgumentException($"配置类型不匹配，期望 {nameof(InfoGridCardConfig)}");
        }
        return Build(gridConfig);
    }

    /// <summary>
    /// 验证卡片配置（非泛型接口实现）
    /// </summary>
    bool IUdlCardBuilderBase.Validate(UdlCardConfig cardConfig)
    {
        if (cardConfig is not InfoGridCardConfig gridConfig)
        {
            return false;
        }
        return Validate(gridConfig);
    }
} 