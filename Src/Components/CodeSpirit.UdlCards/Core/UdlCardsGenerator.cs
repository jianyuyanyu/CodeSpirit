using CodeSpirit.UdlCards.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CodeSpirit.UdlCards.Core;

/// <summary>
/// UDL Cards生成器，用于生成Amis Cards V2.0兼容的配置
/// </summary>
public class UdlCardsGenerator
{
    private readonly ILogger<UdlCardsGenerator> _logger;
    private readonly UdlCardsOptions _options;
    private readonly Dictionary<string, IUdlCardBuilderBase> _builders;

    /// <summary>
    /// 初始化UDL Cards生成器
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="options">配置选项</param>
    /// <param name="builders">卡片建构器列表</param>
    public UdlCardsGenerator(
        ILogger<UdlCardsGenerator> logger,
        IOptions<UdlCardsOptions> options,
        IEnumerable<IUdlCardBuilderBase> builders)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _builders = builders.ToDictionary(b => b.CardType, b => b);

        _logger.LogInformation("UDL Cards生成器已初始化，支持的卡片类型: {CardTypes}",
            string.Join(", ", _builders.Keys));
    }

    /// <summary>
    /// 生成单个卡片的Amis配置
    /// </summary>
    /// <param name="cardConfig">卡片配置</param>
    /// <returns>Amis卡片配置</returns>
    public Dictionary<string, object> GenerateCard(UdlCardConfig cardConfig)
    {
        if (cardConfig == null)
            throw new ArgumentNullException(nameof(cardConfig));

        _logger.LogDebug("开始生成卡片: {CardType} - {CardId}", cardConfig.Type, cardConfig.Id);

        if (!_builders.TryGetValue(cardConfig.Type, out var builder))
        {
            _logger.LogError("不支持的卡片类型: {CardType}", cardConfig.Type);
            throw new NotSupportedException($"不支持的卡片类型: {cardConfig.Type}");
        }

        try
        {
            var amisConfig = builder.Build(cardConfig);
            
            // 应用全局设置
            ApplyGlobalSettings(amisConfig, cardConfig);
            
            _logger.LogDebug("卡片生成完成: {CardType} - {CardId}", cardConfig.Type, cardConfig.Id);
            return amisConfig;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成卡片失败: {CardType} - {CardId}", cardConfig.Type, cardConfig.Id);
            throw;
        }
    }

    /// <summary>
    /// 生成多个卡片的Amis页面配置
    /// </summary>
    /// <param name="cards">卡片配置列表</param>
    /// <param name="pageConfig">页面配置</param>
    /// <returns>Amis页面配置</returns>
    public UdlPageConfig GeneratePage(IEnumerable<UdlCardConfig> cards, UdlPageConfig? pageConfig = null)
    {
        var cardList = cards?.ToList() ?? throw new ArgumentNullException(nameof(cards));
        
        _logger.LogInformation("开始生成页面，包含 {CardCount} 个卡片", cardList.Count);

        var page = pageConfig ?? new UdlPageConfig();

        // 生成卡片
        var cardConfigs = new List<Dictionary<string, object>>();
        foreach (var card in cardList)
        {
            try
            {
                var cardConfig = GenerateCard(card);
                cardConfigs.Add(cardConfig);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "跳过生成失败的卡片: {CardType} - {CardId}", card.Type, card.Id);
                
                if (_options.StrictMode)
                    throw;
            }
        }

        page.Cards = cardConfigs;

        _logger.LogInformation("页面生成完成，成功生成 {GeneratedCount} 个卡片", cardConfigs.Count);
        return page;
    }

    /// <summary>
    /// 生成仪表板配置
    /// </summary>
    /// <param name="dashboardConfig">仪表板配置</param>
    /// <returns>Amis仪表板配置</returns>
    public UdlDashboardConfig GenerateDashboard(UdlDashboardConfig dashboardConfig)
    {
        if (dashboardConfig == null)
            throw new ArgumentNullException(nameof(dashboardConfig));

        _logger.LogInformation("开始生成仪表板: {DashboardTitle}", dashboardConfig.Title);

        foreach (var section in dashboardConfig.Sections)
        {
            GenerateDashboardSection(section);
        }

        _logger.LogInformation("仪表板生成完成: {DashboardTitle}", dashboardConfig.Title);
        return dashboardConfig;
    }

    /// <summary>
    /// 应用全局设置
    /// </summary>
    private void ApplyGlobalSettings(Dictionary<string, object> cardConfig, UdlCardConfig config)
    {
        // 应用主题
        if (!string.IsNullOrEmpty(config.Theme))
        {
            cardConfig["theme"] = config.Theme;
        }

        // 应用权限控制
        if (config.Permissions?.Count > 0)
        {
            cardConfig["permissions"] = config.Permissions;
        }

        // 应用角色控制
        if (config.Roles?.Count > 0)
        {
            cardConfig["roles"] = config.Roles;
        }

        // 应用显示条件
        if (!string.IsNullOrEmpty(config.VisibleOn))
        {
            cardConfig["visibleOn"] = config.VisibleOn;
        }

        // 应用样式
        if (config.Style?.Count > 0)
        {
            cardConfig["style"] = config.Style;
        }

        // 应用CSS类名
        if (!string.IsNullOrEmpty(config.ClassName))
        {
            cardConfig["className"] = config.ClassName;
        }
    }

    /// <summary>
    /// 生成仪表板版块
    /// </summary>
    private void GenerateDashboardSection(UdlDashboardSection section)
    {
        var cardConfigs = new List<Dictionary<string, object>>();
        
        // 这里简化处理，直接将卡片配置作为字典存储
        foreach (var cardDict in section.Cards)
        {
            cardConfigs.Add(cardDict);
        }

        section.Cards = cardConfigs;
    }
} 