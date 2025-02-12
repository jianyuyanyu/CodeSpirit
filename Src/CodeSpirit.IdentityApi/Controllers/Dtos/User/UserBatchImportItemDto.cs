using CodeSpirit.IdentityApi.Data.Models;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.IdentityApi.Controllers.Dtos.User
{
    /// <summary>
    /// 批量导入用户 DTO
    /// </summary>
    public class UserBatchImportItemDto
    {
        /// <summary>
        /// 用户名
        /// </summary>
        [JsonProperty("用户名")]
        [Required]
        [MaxLength(100)]
        public string UserName { get; set; }

        /// <summary>
        /// 电子邮箱
        /// </summary>
        [JsonProperty("邮箱")]
        [Required]
        [EmailAddress]
        [MaxLength(256)]
        public string Email { get; set; }

        /// <summary>
        /// 手机号码
        /// </summary>
        [JsonProperty("手机号码")]
        [Phone]
        [MaxLength(20)]
        public string PhoneNumber { get; set; }

        /// <summary>
        /// 姓名
        /// </summary>
        [JsonProperty("姓名")]
        [Required]
        [MaxLength(20)]
        public string Name { get; set; }

        /// <summary>
        /// 身份证号码
        /// </summary>
        [JsonProperty("身份证号码")]
        [MaxLength(18)]
        public string IdNo { get; set; }

        /// <summary>
        /// 性别
        /// </summary>
        [JsonProperty("性别")]
        public Gender Gender { get; set; }
    }
} 