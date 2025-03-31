using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Options;
using CodeSpirit.Web.Options;

namespace CodeSpirit.Web.Helpers
{
    /// <summary>
    /// 资源URL生成辅助类
    /// </summary>
    public static class ResourceHelper
    {
        /// <summary>
        /// 获取站点配置和资源基础URL
        /// </summary>
        /// <param name="html">The HTML helper.</param>
        /// <returns>包含站点配置和资源URL的元组</returns>
        public static (SiteSettings SiteSettings, string ResourceBase) GetSiteConfig(this IHtmlHelper html)
        {
            var siteOptions = html.ViewContext.HttpContext.RequestServices.GetService<IOptions<SiteSettings>>();
            var settings = siteOptions?.Value ?? new SiteSettings();
            var cdnEnabled = settings.EnableCdn;
            var resourceBase = cdnEnabled ? settings.CdnUrl : "";
            
            return (settings, resourceBase);
        }

        /// <summary>
        /// 生成HTML非编码的原始输出，防止中文等字符显示乱码
        /// </summary>
        /// <param name="html">The HTML helper.</param>
        /// <param name="value">要输出的值</param>
        /// <returns>原始HTML内容</returns>
        public static IHtmlContent Raw(this IHtmlHelper html, string value)
        {
            return new HtmlString(value);
        }
        
        /// <summary>
        /// 初始化页面所需的资源变量
        /// </summary>
        /// <param name="html">The HTML helper.</param>
        /// <returns>包含资源URL的元组</returns>
        public static (SiteSettings Settings, string ResourceBase) InitializeResources(this IHtmlHelper html)
        {
            return html.GetSiteConfig();
        }
    }
} 