using CodeSpirit.UdlCards.Core;
using CodeSpirit.UdlCards.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CodeSpirit.UdlCards.Builders;

/// <summary>
/// 统计卡片建构器
/// </summary>
public class StatCardBuilder : IUdlCardBuilder<StatCardConfig>, IUdlCardBuilderBase
{
    private readonly ILogger<StatCardBuilder> _logger;

    /// <summary>
    /// 无参构造函数（用于测试）
    /// </summary>
    public StatCardBuilder() : this(NullLogger<StatCardBuilder>.Instance)
    {
    }

    /// <summary>
    /// 带日志的构造函数
    /// </summary>
    /// <param name="logger">日志服务</param>
    public StatCardBuilder(ILogger<StatCardBuilder> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 支持的卡片类型
    /// </summary>
    public string CardType => "stat";

    /// <summary>
    /// 构建Amis统计卡片配置
    /// </summary>
    /// <param name="cardConfig">卡片配置</param>
    /// <returns>Amis配置对象</returns>
    public Dictionary<string, object> Build(StatCardConfig cardConfig)
    {
        _logger.LogDebug("构建统计卡片: {CardId}", cardConfig.Id);

        var card = new Dictionary<string, object>
        {
            ["type"] = "stat",
            ["id"] = cardConfig.Id,
            ["className"] = "amis-cards-stat"
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

        // 构建数据配置
        BuildDataConfig(card, cardConfig);

        // 构建图标配置
        if (cardConfig.Icon != null)
        {
            BuildIconConfig(card, cardConfig.Icon);
        }

        // 构建趋势配置
        if (cardConfig.Trend != null)
        {
            BuildTrendConfig(card, cardConfig.Trend);
        }

        // 构建进度条配置
        if (cardConfig.Progress != null)
        {
            BuildProgressConfig(card, cardConfig.Progress);
        }

        // 构建动画配置
        if (cardConfig.Animation != null)
        {
            BuildAnimationConfig(card, cardConfig.Animation);
        }

        return card;
    }

    /// <summary>
    /// 验证卡片配置
    /// </summary>
    /// <param name="cardConfig">卡片配置</param>
    /// <returns>验证结果</returns>
    public bool Validate(StatCardConfig cardConfig)
    {
        // 验证数据配置
        if (cardConfig.Data?.Value == null)
        {
            return false;
        }

        // 验证图标配置
        if (cardConfig.Icon != null && string.IsNullOrEmpty(cardConfig.Icon.Name))
        {
            return false;
        }

        // 验证进度条配置
        if (cardConfig.Progress != null && cardConfig.Progress.Target <= 0)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// 构建Amis卡片配置（非泛型接口实现）
    /// </summary>
    /// <param name="cardConfig">卡片配置</param>
    /// <returns>Amis配置对象</returns>
    Dictionary<string, object> IUdlCardBuilderBase.Build(UdlCardConfig cardConfig)
    {
        if (cardConfig is not StatCardConfig statConfig)
        {
            throw new ArgumentException($"配置类型不匹配，期望 {nameof(StatCardConfig)}，实际 {cardConfig.GetType().Name}");
        }
        return Build(statConfig);
    }

    /// <summary>
    /// 验证卡片配置（非泛型接口实现）
    /// </summary>
    /// <param name="cardConfig">卡片配置</param>
    /// <returns>验证结果</returns>
    bool IUdlCardBuilderBase.Validate(UdlCardConfig cardConfig)
    {
        if (cardConfig is not StatCardConfig statConfig)
        {
            return false;
        }
        return Validate(statConfig);
    }

    /// <summary>
    /// 构建数据配置
    /// </summary>
    private void BuildDataConfig(Dictionary<string, object> card, StatCardConfig config)
    {
        var data = config.Data;
        
        card["data"] = new Dictionary<string, object>
        {
            ["value"] = data.Value,
            ["label"] = data.Label ?? "",
            ["unit"] = data.Unit ?? "",
            ["prefix"] = data.Prefix ?? "",
            ["suffix"] = data.Suffix ?? "",
            ["formatter"] = data.Formatter ?? "",
            ["decimalPlaces"] = data.DecimalPlaces,
            ["showSeparator"] = data.ShowSeparator
        };

        // API数据源配置
        if (!string.IsNullOrEmpty(data.ApiUrl))
        {
            card["api"] = new Dictionary<string, object>
            {
                ["method"] = "get",
                ["url"] = data.ApiUrl ?? ""
            };

            if (data.FieldMapping?.Count > 0)
            {
                ((Dictionary<string, object>)card["data"])["fieldMapping"] = data.FieldMapping;
            }
        }
    }

    /// <summary>
    /// 构建图标配置
    /// </summary>
    private void BuildIconConfig(Dictionary<string, object> card, StatIconConfig iconConfig)
    {
        var icon = new Dictionary<string, object>
        {
            ["name"] = iconConfig.Name ?? "",
            ["position"] = iconConfig.Position ?? "",
            ["size"] = iconConfig.Size ?? "",
            ["color"] = iconConfig.Color ?? "",
            ["backgroundColor"] = iconConfig.BackgroundColor ?? "",
            ["showBorder"] = iconConfig.ShowBorder,
            ["borderColor"] = iconConfig.BorderColor ?? ""
        };

        if (iconConfig.Style?.Count > 0)
        {
            icon["style"] = iconConfig.Style;
        }

        card["icon"] = icon;
    }

    /// <summary>
    /// 构建趋势配置
    /// </summary>
    private void BuildTrendConfig(Dictionary<string, object> card, StatTrendConfig trendConfig)
    {
        var trend = new Dictionary<string, object>
        {
            ["direction"] = trendConfig.Direction ?? "",
            ["value"] = trendConfig.Value,
            ["isPercentage"] = trendConfig.IsPercentage,
            ["text"] = trendConfig.Text ?? ""
        };

        if (trendConfig.Colors != null)
        {
            trend["colors"] = new Dictionary<string, object>
            {
                ["up"] = trendConfig.Colors.Up ?? "",
                ["down"] = trendConfig.Colors.Down ?? "",
                ["stable"] = trendConfig.Colors.Stable ?? ""
            };
        }

        card["trend"] = trend;
    }

    /// <summary>
    /// 构建进度条配置
    /// </summary>
    private void BuildProgressConfig(Dictionary<string, object> card, StatProgressConfig progressConfig)
    {
        card["progress"] = new Dictionary<string, object>
        {
            ["target"] = progressConfig.Target,
            ["show"] = progressConfig.Show,
            ["height"] = progressConfig.Height,
            ["showText"] = progressConfig.ShowText,
            ["color"] = progressConfig.Color ?? "",
            ["backgroundColor"] = progressConfig.BackgroundColor ?? ""
        };
    }

    /// <summary>
    /// 构建动画配置
    /// </summary>
    private void BuildAnimationConfig(Dictionary<string, object> card, StatAnimationConfig animationConfig)
    {
        card["animation"] = new Dictionary<string, object>
        {
            ["enableValueAnimation"] = animationConfig.EnableValueAnimation,
            ["duration"] = animationConfig.Duration,
            ["easing"] = animationConfig.Easing ?? "",
            ["delay"] = animationConfig.Delay
        };
    }
} 