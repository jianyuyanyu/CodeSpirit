using CodeSpirit.Core.DependencyInjection;

namespace CodeSpirit.Localization.Providers;

/// <summary>
/// 组合语言提供者，按优先级链式查找
/// </summary>
public class CompositeLanguageProvider : ILanguageProvider, IScopedDependency
{
    private readonly IEnumerable<ILanguageProvider> _providers;

    public CompositeLanguageProvider(IEnumerable<ILanguageProvider> providers)
    {
        _providers = providers;
    }

    public async Task<string?> GetLanguageAsync()
    {
        // 按优先级顺序查找
        foreach (var provider in _providers)
        {
            var language = await provider.GetLanguageAsync();
            if (!string.IsNullOrEmpty(language))
            {
                return language;
            }
        }

        return null;
    }
}
