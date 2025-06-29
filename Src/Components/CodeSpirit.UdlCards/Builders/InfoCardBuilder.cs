using CodeSpirit.UdlCards.Core;
using CodeSpirit.UdlCards.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CodeSpirit.UdlCards.Builders;

/// <summary>
/// 信息卡片建构器
/// </summary>
public class InfoCardBuilder : IUdlCardBuilder<InfoCardConfig>, IUdlCardBuilderBase
{
    private readonly ILogger<InfoCardBuilder> _logger;

    /// <summary>
    /// 无参构造函数（用于测试）
    /// </summary>
    public InfoCardBuilder() : this(NullLogger<InfoCardBuilder>.Instance)
    {
    }

    /// <summary>
    /// 带日志的构造函数
    /// </summary>
    /// <param name="logger">日志服务</param>
    public InfoCardBuilder(ILogger<InfoCardBuilder> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 支持的卡片类型
    /// </summary>
    public string CardType => "info";

    /// <summary>
    /// 构建Amis信息卡片配置
    /// </summary>
    public Dictionary<string, object> Build(InfoCardConfig cardConfig)
    {
        var card = new Dictionary<string, object>
        {
            ["type"] = "info",
            ["id"] = cardConfig.Id,
            ["className"] = "amis-cards-info"
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

        // 构建内容配置
        var content = cardConfig.Content;
        switch (content.Type)
        {
            case "text":
                card["body"] = new Dictionary<string, object>
                {
                    ["type"] = "tpl",
                    ["tpl"] = content.Text ?? ""
                };
                break;
            case "html":
                card["body"] = new Dictionary<string, object>
                {
                    ["type"] = "html",
                    ["html"] = content.Html ?? ""
                };
                break;
            case "properties":
                if (content.PropertyItems?.Count > 0)
                {
                    card["body"] = new Dictionary<string, object>
                    {
                        ["type"] = "property",
                        ["items"] = content.PropertyItems.Select(p => new Dictionary<string, object>
                        {
                            ["label"] = p.Label,
                            ["content"] = p.Value ?? string.Empty
                        }).ToList()
                    };
                }
                break;
        }

        return card;
    }

    /// <summary>
    /// 验证卡片配置
    /// </summary>
    public bool Validate(InfoCardConfig cardConfig)
    {
        if (string.IsNullOrEmpty(cardConfig.Content.Type))
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
        if (cardConfig is not InfoCardConfig infoConfig)
        {
            throw new ArgumentException($"配置类型不匹配，期望 {nameof(InfoCardConfig)}");
        }
        return Build(infoConfig);
    }

    /// <summary>
    /// 验证卡片配置（非泛型接口实现）
    /// </summary>
    bool IUdlCardBuilderBase.Validate(UdlCardConfig cardConfig)
    {
        if (cardConfig is not InfoCardConfig infoConfig)
        {
            return false;
        }
        return Validate(infoConfig);
    }
} 