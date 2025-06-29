using CodeSpirit.UdlCards.Models;

namespace CodeSpirit.UdlCards.Core;

/// <summary>
/// UDL卡片建构器基础接口（非泛型）
/// </summary>
public interface IUdlCardBuilderBase
{
    /// <summary>
    /// 支持的卡片类型
    /// </summary>
    string CardType { get; }

    /// <summary>
    /// 构建Amis卡片配置
    /// </summary>
    /// <param name="cardConfig">卡片配置</param>
    /// <returns>Amis配置对象</returns>
    Dictionary<string, object> Build(UdlCardConfig cardConfig);

    /// <summary>
    /// 验证卡片配置
    /// </summary>
    /// <param name="cardConfig">卡片配置</param>
    /// <returns>验证结果</returns>
    bool Validate(UdlCardConfig cardConfig);
} 