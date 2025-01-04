// Models/LoginLog.cs
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CodeSpirit.IdentityApi.Data.Models
{
    /// <summary>
    /// 登录日志模型，记录用户的登录尝试信息。
    /// </summary>
    public class LoginLog
    {
        /// <summary>
        /// 日志的唯一标识。
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// 登录用户的唯一标识（外键）。
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// 导航属性，指向用户。
        /// </summary>
        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; }

        /// <summary>
        /// 用户名。
        /// </summary>
        [Required]
        [MaxLength(256)]
        public string UserName { get; set; }

        /// <summary>
        /// 登录尝试的时间。
        /// </summary>
        public DateTime LoginTime { get; set; }

        /// <summary>
        /// 登录尝试的 IP 地址。
        /// </summary>
        [MaxLength(45)] // 支持 IPv6
        public string IPAddress { get; set; }

        /// <summary>
        /// 登录是否成功。
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 如果登录失败，记录失败原因。
        /// </summary>
        [MaxLength(512)]
        public string FailureReason { get; set; }
    }
}
