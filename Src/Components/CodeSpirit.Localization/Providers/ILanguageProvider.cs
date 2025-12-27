namespace CodeSpirit.Localization.Providers;

/// <summary>
/// 语言提供者接口
/// </summary>
public interface ILanguageProvider
{
    /// <summary>
    /// 获取语言代码
    /// </summary>
    /// <returns>语言代码，如果无法获取返回 null</returns>
    Task<string?> GetLanguageAsync();
}
