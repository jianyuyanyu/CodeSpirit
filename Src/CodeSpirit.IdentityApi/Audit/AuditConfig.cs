namespace CodeSpirit.IdentityApi.Audit
{
    public class AuditConfig
    {
        /// <summary>
        /// 是否启用审计日志
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 需要排除的路径
        /// </summary>
        public List<string> ExcludePaths { get; set; } = new List<string>
        {
            "/health",
            "/metrics",
            "/swagger"
        };

        /// <summary>
        /// 需要排除的请求头
        /// </summary>
        public List<string> ExcludeHeaders { get; set; } = new List<string>
        {
            "Authorization",
            "Cookie",
            "X-CSRF"
        };

        /// <summary>
        /// 需要排除的请求体字段
        /// </summary>
        public List<string> ExcludeBodyFields { get; set; } = new List<string>
        {
            "password",
            "newPassword",
            "confirmPassword",
            "currentPassword"
        };
    }
} 