using CodeSpirit.UdlCards.Models;

namespace CodeSpirit.UdlCards.Core;

/// <summary>
/// UDL卡片建构器接口
/// </summary>
/// <typeparam name="TConfig">卡片配置类型</typeparam>
public interface IUdlCardBuilder<in TConfig> where TConfig : UdlCardConfig
{
    /// <summary>
    /// 构建Amis卡片配置
    /// </summary>
    /// <param name="cardConfig">卡片配置</param>
    /// <returns>Amis配置对象</returns>
    Dictionary<string, object> Build(TConfig cardConfig);

    /// <summary>
    /// 验证卡片配置
    /// </summary>
    /// <param name="cardConfig">卡片配置</param>
    /// <returns>验证结果</returns>
    bool Validate(TConfig cardConfig);
} 