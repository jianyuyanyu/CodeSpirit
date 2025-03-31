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
        /// Logo地址
        /// </summary>
        public string LogoUrl { get; set; } = "/favicon.ico";
    }
} 