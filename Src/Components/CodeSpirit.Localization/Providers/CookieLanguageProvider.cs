using CodeSpirit.Core.DependencyInjection;
using Microsoft.AspNetCore.Http;

namespace CodeSpirit.Localization.Providers;

/// <summary>
/// Cookie 语言提供者（最高优先级）
/// </summary>
public class CookieLanguageProvider : ILanguageProvider, IScopedDependency
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CookieLanguageProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Task<string?> GetLanguageAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return Task.FromResult<string?>(null);
        }

        // 从 Cookie 中读取语言设置
        var cultureCookie = httpContext.Request.Cookies[".AspNetCore.Culture"];
        if (string.IsNullOrEmpty(cultureCookie))
        {
            return Task.FromResult<string?>(null);
        }

        // Cookie 格式: c=zh-CN|uic=zh-CN
        var parts = cultureCookie.Split('|');
        foreach (var part in parts)
        {
            if (part.StartsWith("uic="))
            {
                var language = part.Substring(4);
                return Task.FromResult<string?>(language);
            }
        }

        return Task.FromResult<string?>(null);
    }
}
