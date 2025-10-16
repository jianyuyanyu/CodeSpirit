namespace CodeSpirit.Web.Options
{
    /// <summary>
    /// 站点设置选项类
    /// </summary>
    public class SiteSettings
    {
        /// <summary>
        /// 站点名称
        /// </summary>
        public string SiteName { get; set; } = "CodeSpirit";

        /// <summary>
        /// 登录页顶部显示的站点名称
        /// </summary>
        public string TopSiteName { get; set; } = "CodeSpirit";

        /// <summary>
        /// Logo地址
        /// </summary>
        public string LogoUrl { get; set; } = "/favicon.ico";

        /// <summary>
        /// 是否启用CDN
        /// </summary>
        public bool EnableCdn { get; set; } = false;

        /// <summary>
        /// CDN域名地址，例如 https://cdn.example.com
        /// </summary>
        public string CdnUrl { get; set; } = "";

        public string WebHost { get; set; } = "https://codespirit-app.xin-lai.com";

        /// <summary>
        /// API基础地址，用于生产环境直连API服务
        /// 如果设置了此值，前端将直接请求API服务而不使用代理
        /// 例如：https://api.example.com
        /// 留空则使用默认的代理方式
        /// </summary>
        public string ApiBaseUrl { get; set; } = "";

        /// <summary>
        /// 资源版本号，用于CDN缓存控制
        /// </summary>
        public string ResourceVersion { get; set; } = "";
    }
} 