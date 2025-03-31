using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Options;
using CodeSpirit.Web.Options;
using System;

namespace CodeSpirit.Web.TagHelpers
{
    /// <summary>
    /// 资源引用TagHelper，支持CDN和本地资源自动切换
    /// </summary>
    [HtmlTargetElement("resource", TagStructure = TagStructure.WithoutEndTag)]
    public class ResourceTagHelper : TagHelper
    {
        private readonly SiteSettings _siteSettings;

        /// <summary>
        /// 资源路径
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// 资源类型 (js, css, url)
        /// </summary>
        public string Type { get; set; } = "url";

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="options">站点配置选项</param>
        public ResourceTagHelper(IOptions<SiteSettings> options)
        {
            _siteSettings = options.Value;
        }

        /// <summary>
        /// 处理标签
        /// </summary>
        /// <param name="context">上下文</param>
        /// <param name="output">输出</param>
        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            if (string.IsNullOrEmpty(Path))
            {
                output.SuppressOutput();
                return;
            }
            
            // 规范化路径
            var resourcePath = Path.StartsWith("/") ? Path.Substring(1) : Path;
            
            // 生成完整URL
            var cdnEnabled = _siteSettings.EnableCdn;
            var resourceBase = cdnEnabled ? _siteSettings.CdnUrl : "";
            var url = resourceBase.TrimEnd('/') + "/" + resourcePath;
            
            // 添加版本号
            if (cdnEnabled)
            {
                var timestamp = DateTime.UtcNow.ToString("yyyyMMddHH");
                url += $"?v={timestamp}";
            }
            
            // 根据类型输出不同的HTML
            switch (Type.ToLower())
            {
                case "js":
                    output.TagName = "script";
                    output.Attributes.SetAttribute("src", url);
                    if (!cdnEnabled)
                    {
                        output.Attributes.SetAttribute("asp-append-version", "true");
                    }
                    output.TagMode = TagMode.StartTagAndEndTag;
                    break;
                    
                case "css":
                    output.TagName = "link";
                    output.Attributes.SetAttribute("rel", "stylesheet");
                    output.Attributes.SetAttribute("href", url);
                    if (!cdnEnabled)
                    {
                        output.Attributes.SetAttribute("asp-append-version", "true");
                    }
                    output.TagMode = TagMode.SelfClosing;
                    break;
                    
                default:
                    output.TagName = "span";
                    output.Content.SetContent(url);
                    output.TagMode = TagMode.StartTagAndEndTag;
                    break;
            }
        }
    }
} 