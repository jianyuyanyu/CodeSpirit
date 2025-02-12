using System.ComponentModel.DataAnnotations.Schema;

namespace CodeSpirit.IdentityApi.Data.Models
{
    /// <summary>
    /// 审计日志实体类，用于记录系统操作日志
    /// </summary>
    public class AuditLog
    {
        /// <summary>
        /// 审计日志记录的唯一标识符
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        /// <summary>
        /// 事件类型，例如"GET.Users.GetAll"
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string EventType { get; set; }

        /// <summary>
        /// 执行操作的用户ID
        /// </summary>
        public long? UserId { get; set; }

        /// <summary>
        /// 执行操作的用户名
        /// </summary>
        [MaxLength(256)]
        public string UserName { get; set; }

        /// <summary>
        /// 客户端IP地址
        /// </summary>
        [MaxLength(50)]
        public string IpAddress { get; set; }

        /// <summary>
        /// HTTP请求方法(GET/POST/PUT/DELETE等)
        /// </summary>
        [Required]
        [MaxLength(10)]
        public string Method { get; set; }

        /// <summary>
        /// 请求的URL路径
        /// </summary>
        [Required]
        [MaxLength(2000)]
        public string Url { get; set; }

        /// <summary>
        /// URL查询字符串
        /// </summary>
        [MaxLength(2000)]
        public string QueryString { get; set; }

        /// <summary>
        /// HTTP请求头信息(JSON格式)
        /// </summary>
        [Column(TypeName = "ntext")]
        public string Headers { get; set; }

        /// <summary>
        /// 请求体内容(JSON格式)
        /// </summary>
        [Column(TypeName = "ntext")]
        public string RequestBody { get; set; }

        /// <summary>
        /// 响应体内容(JSON格式)
        /// </summary>
        [Column(TypeName = "ntext")]
        public string ResponseBody { get; set; }

        /// <summary>
        /// HTTP响应状态码
        /// </summary>
        [Required]
        public int StatusCode { get; set; }

        /// <summary>
        /// 请求处理持续时间(毫秒)
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18,4)")]
        public double Duration { get; set; }

        /// <summary>
        /// 事件发生时间
        /// </summary>
        [Required]
        [Column(TypeName = "timestamp")]
        public DateTime EventTime { get; set; } = DateTime.UtcNow;
    }
}