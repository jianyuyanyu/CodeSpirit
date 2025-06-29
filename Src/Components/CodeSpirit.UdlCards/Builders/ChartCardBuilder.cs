using CodeSpirit.UdlCards.Core;
using CodeSpirit.UdlCards.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CodeSpirit.UdlCards.Builders;

/// <summary>
/// 图表卡片建构器
/// </summary>
public class ChartCardBuilder : IUdlCardBuilder<ChartCardConfig>, IUdlCardBuilderBase
{
    private readonly ILogger<ChartCardBuilder> _logger;

    /// <summary>
    /// 无参构造函数（用于测试）
    /// </summary>
    public ChartCardBuilder() : this(NullLogger<ChartCardBuilder>.Instance)
    {
    }

    /// <summary>
    /// 带日志的构造函数
    /// </summary>
    /// <param name="logger">日志服务</param>
    public ChartCardBuilder(ILogger<ChartCardBuilder> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 支持的卡片类型
    /// </summary>
    public string CardType => "chart";

    /// <summary>
    /// 构建Amis图表卡片配置
    /// </summary>
    /// <param name="cardConfig">卡片配置</param>
    /// <returns>Amis配置对象</returns>
    public Dictionary<string, object> Build(ChartCardConfig cardConfig)
    {
        _logger.LogDebug("构建图表卡片: {CardId}", cardConfig.Id);

        var card = new Dictionary<string, object>
        {
            ["type"] = "chart",
            ["id"] = cardConfig.Id,
            ["className"] = "amis-cards-chart"
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

        // 构建图表配置
        BuildChartConfig(card, cardConfig.Chart);

        // 构建数据配置
        BuildDataConfig(card, cardConfig.Data);

        return card;
    }

    /// <summary>
    /// 验证卡片配置
    /// </summary>
    /// <param name="cardConfig">卡片配置</param>
    /// <returns>验证结果</returns>
    public bool Validate(ChartCardConfig cardConfig)
    {
        // 验证图表类型
        if (string.IsNullOrEmpty(cardConfig.Chart.Type))
        {
            return false;
        }

        // 验证数据源
        if (cardConfig.Data.StaticData == null && string.IsNullOrEmpty(cardConfig.Data.ApiUrl))
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
        if (cardConfig is not ChartCardConfig chartConfig)
        {
            throw new ArgumentException($"配置类型不匹配，期望 {nameof(ChartCardConfig)}，实际 {cardConfig.GetType().Name}");
        }
        return Build(chartConfig);
    }

    /// <summary>
    /// 验证卡片配置（非泛型接口实现）
    /// </summary>
    /// <param name="cardConfig">卡片配置</param>
    /// <returns>验证结果</returns>
    bool IUdlCardBuilderBase.Validate(UdlCardConfig cardConfig)
    {
        if (cardConfig is not ChartCardConfig chartConfig)
        {
            return false;
        }
        return Validate(chartConfig);
    }

    /// <summary>
    /// 构建图表配置
    /// </summary>
    private void BuildChartConfig(Dictionary<string, object> card, ChartConfig chartConfig)
    {
        var config = new Dictionary<string, object>
        {
            ["type"] = chartConfig.Type ?? "",
            ["height"] = chartConfig.Height,
            ["width"] = chartConfig.Width,
            ["theme"] = chartConfig.Theme ?? "",
            ["responsive"] = chartConfig.Responsive
        };

        // ECharts配置选项
        if (chartConfig.Options?.Count > 0)
        {
            config["config"] = chartConfig.Options;
        }

        // 动画配置
        if (chartConfig.Animation != null)
        {
            config["animation"] = new Dictionary<string, object>
            {
                ["enabled"] = chartConfig.Animation.Enabled,
                ["duration"] = chartConfig.Animation.Duration,
                ["easing"] = chartConfig.Animation.Easing ?? "",
                ["delay"] = chartConfig.Animation.Delay
            };
        }

        // 工具栏配置
        if (chartConfig.Toolbox != null)
        {
            var toolbox = new Dictionary<string, object>
            {
                ["show"] = chartConfig.Toolbox.Show,
                ["orient"] = chartConfig.Toolbox.Position ?? ""
            };

            if (chartConfig.Toolbox.Tools.Count > 0)
            {
                toolbox["feature"] = chartConfig.Toolbox.Tools.ToDictionary(t => t, _ => new { show = true });
            }

            config["toolbox"] = toolbox;
        }

        card["chart"] = config;
    }

    /// <summary>
    /// 构建数据配置
    /// </summary>
    private void BuildDataConfig(Dictionary<string, object> card, ChartDataConfig dataConfig)
    {
        // 静态数据
        if (dataConfig.StaticData?.Count > 0)
        {
            card["data"] = dataConfig.StaticData.ToList();
        }

        // API数据源
        if (!string.IsNullOrEmpty(dataConfig.ApiUrl))
        {
            card["api"] = new Dictionary<string, object>
            {
                ["method"] = "get",
                ["url"] = dataConfig.ApiUrl
            };

            // 数据刷新间隔
            if (dataConfig.RefreshInterval > 0)
            {
                card["interval"] = dataConfig.RefreshInterval;
            }
        }

        // 字段映射
        if (dataConfig.FieldMapping != null)
        {
            var mapping = new Dictionary<string, object>();
            
            if (!string.IsNullOrEmpty(dataConfig.FieldMapping.XField))
                mapping["x"] = dataConfig.FieldMapping.XField;
            if (!string.IsNullOrEmpty(dataConfig.FieldMapping.YField))
                mapping["y"] = dataConfig.FieldMapping.YField;
            if (!string.IsNullOrEmpty(dataConfig.FieldMapping.SeriesField))
                mapping["series"] = dataConfig.FieldMapping.SeriesField;
            if (!string.IsNullOrEmpty(dataConfig.FieldMapping.ValueField))
                mapping["value"] = dataConfig.FieldMapping.ValueField;
            if (!string.IsNullOrEmpty(dataConfig.FieldMapping.LabelField))
                mapping["label"] = dataConfig.FieldMapping.LabelField;
            if (!string.IsNullOrEmpty(dataConfig.FieldMapping.ColorField))
                mapping["color"] = dataConfig.FieldMapping.ColorField;

            if (mapping.Count > 0)
            {
                card["dataMapping"] = mapping;
            }
        }

        // 数据过滤器
        if (dataConfig.Filters?.Count > 0)
        {
            card["dataFilter"] = dataConfig.Filters.Select(f => new Dictionary<string, object>
            {
                ["field"] = f.Field ?? "",
                ["op"] = f.Operator ?? "",
                ["value"] = f.Value ?? ""
            }).ToList();
        }
    }
} 