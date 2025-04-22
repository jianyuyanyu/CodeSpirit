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
    }

    /// <summary>
    /// 考试API设置选项类
    /// </summary>
    public class ExamApiSettings
    {
        /// <summary>
        /// 考试API基础URL
        /// </summary>
        public string BaseUrl { get; set; } = "https://localhost:61882";

        /// <summary>
        /// SignalR配置
        /// </summary>
        public SignalRSettings SignalR { get; set; } = new SignalRSettings();
    }

    /// <summary>
    /// SignalR设置选项类
    /// </summary>
    public class SignalRSettings
    {
        /// <summary>
        /// 题目生成Hub的URL
        /// </summary>
        public string QuestionGenerationHubUrl { get; set; } = "/api/exam/questionGenerationHub";
    }
} 