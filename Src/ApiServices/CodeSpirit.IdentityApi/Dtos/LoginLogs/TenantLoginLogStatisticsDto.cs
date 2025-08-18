using System.ComponentModel;

namespace CodeSpirit.IdentityApi.Dtos.LoginLogs
{
    /// <summary>
    /// 租户登录日志统计DTO
    /// </summary>
    public class TenantLoginLogStatisticsDto
    {
        /// <summary>
        /// 租户ID
        /// </summary>
        [DisplayName("租户ID")]
        public string TenantId { get; set; } = string.Empty;

        /// <summary>
        /// 租户名称
        /// </summary>
        [DisplayName("租户名称")]
        public string TenantName { get; set; } = string.Empty;

        /// <summary>
        /// 租户显示名称
        /// </summary>
        [DisplayName("租户显示名称")]
        public string TenantDisplayName { get; set; } = string.Empty;

        /// <summary>
        /// 总登录次数
        /// </summary>
        [DisplayName("总登录次数")]
        public int TotalLogins { get; set; }

        /// <summary>
        /// 成功登录次数
        /// </summary>
        [DisplayName("成功登录次数")]
        public int SuccessfulLogins { get; set; }

        /// <summary>
        /// 失败登录次数
        /// </summary>
        [DisplayName("失败登录次数")]
        public int FailedLogins { get; set; }

        /// <summary>
        /// 成功登录率
        /// </summary>
        [DisplayName("成功登录率")]
        public decimal SuccessRate { get; set; }

        /// <summary>
        /// 独立用户数量
        /// </summary>
        [DisplayName("独立用户数量")]
        public int UniqueUsers { get; set; }

        /// <summary>
        /// 今日登录次数
        /// </summary>
        [DisplayName("今日登录次数")]
        public int TodayLogins { get; set; }

        /// <summary>
        /// 本周登录次数
        /// </summary>
        [DisplayName("本周登录次数")]
        public int ThisWeekLogins { get; set; }

        /// <summary>
        /// 本月登录次数
        /// </summary>
        [DisplayName("本月登录次数")]
        public int ThisMonthLogins { get; set; }

        /// <summary>
        /// 最后登录时间
        /// </summary>
        [DisplayName("最后登录时间")]
        public DateTime? LastLoginTime { get; set; }

        /// <summary>
        /// 最活跃的登录时间段
        /// </summary>
        [DisplayName("最活跃时间段")]
        public string MostActiveHour { get; set; } = string.Empty;
    }
} 