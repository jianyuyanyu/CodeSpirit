using Microsoft.AspNetCore.Http;
using System.Globalization;

namespace CodeSpirit.Amis.Helpers;

/// <summary>
/// 文化信息解析器，提供统一的语言获取逻辑
/// </summary>
public class CultureResolver
{
    private readonly IHttpContextAccessor? _httpContextAccessor;

    /// <summary>
    /// 初始化文化信息解析器
    /// </summary>
    /// <param name="httpContextAccessor">HTTP上下文访问器</param>
    public CultureResolver(IHttpContextAccessor? httpContextAccessor = null)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// 获取当前请求的语言文化信息
    /// </summary>
    /// <returns>当前语言文化信息</returns>
    public CultureInfo GetCurrentCulture()
    {
        try
        {
            var httpContext = _httpContextAccessor?.HttpContext;
            if (httpContext != null)
            {
                // 1. 优先从请求特性中获取（UseCodeSpiritRequestLocalization 中间件设置）
                var requestCultureFeature = httpContext.Features.Get<Microsoft.AspNetCore.Localization.IRequestCultureFeature>();
                if (requestCultureFeature?.RequestCulture?.UICulture != null)
                {
                    return requestCultureFeature.RequestCulture.UICulture;
                }
                
                // 2. 尝试从线程当前文化获取（由本地化中间件设置）
                var currentCulture = CultureInfo.CurrentUICulture;
                if (currentCulture != null && currentCulture.Name != "zh-CN")
                {
                    // 如果当前文化不是默认值，则使用它
                    return currentCulture;
                }
                
                // 3. 作为最后的回退，直接从 Cookie 读取
                // 这是为了处理异步或缓存场景下中间件尚未执行的情况
                var cultureCookie = httpContext.Request.Cookies[".AspNetCore.Culture"];
                if (!string.IsNullOrEmpty(cultureCookie))
                {
                    var language = ParseCultureFromCookie(cultureCookie);
                    if (!string.IsNullOrEmpty(language))
                    {
                        try
                        {
                            return new CultureInfo(language);
                        }
                        catch
                        {
                            // 如果语言代码无效，继续尝试其他方式
                        }
                    }
                }
            }
        }
        catch
        {
            // 如果获取失败，回退到线程当前文化
        }
        
        // 4. 最终回退到线程当前文化（应该由 UseCodeSpiritRequestLocalization 中间件设置）
        return CultureInfo.CurrentUICulture;
    }

    /// <summary>
    /// 获取当前请求的语言代码（字符串格式）
    /// </summary>
    /// <returns>语言代码（如 zh-CN, en）</returns>
    public string GetCurrentLanguage()
    {
        return GetCurrentCulture().Name;
    }

    /// <summary>
    /// 从 Cookie 中解析语言代码
    /// </summary>
    /// <param name="cultureCookie">Cookie 值</param>
    /// <returns>语言代码，如果解析失败则返回 null</returns>
    private static string? ParseCultureFromCookie(string cultureCookie)
    {
        // Cookie 格式: c=en|uic=en 或 c=zh-CN|uic=zh-CN
        var parts = cultureCookie.Split('|');
        foreach (var part in parts)
        {
            if (part.StartsWith("uic="))
            {
                var language = part.Substring(4);
                if (!string.IsNullOrEmpty(language))
                {
                    return language;
                }
            }
        }
        return null;
    }
}
