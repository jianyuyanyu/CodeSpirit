using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.IdentityApi.Resources;
using Newtonsoft.Json;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.IdentityApi.Dtos.User
{
    /// <summary>
    /// 批量导入用户 DTO
    /// 注意：JsonProperty 特性的 propertyName 需要与 DisplayName 保持一致
    /// 批量导入时会根据 DisplayName 进行列匹配
    /// </summary>
    public class UserBatchImportItemDto
    {
        /// <summary>
        /// 用户名
        /// </summary>
        [Display(Name = nameof(UserName), ResourceType = typeof(IdentityDisplayResources))]
        [JsonProperty("用户名")] // 中文列名用于导入模板
        [Required]
        [MaxLength(100)]
        public string UserName { get; set; }

        /// <summary>
        /// 电子邮箱
        /// </summary>
        [Display(Name = nameof(Email), ResourceType = typeof(IdentityDisplayResources))]
        [JsonProperty("电子邮箱")] // 中文列名用于导入模板
        [Required]
        [EmailAddress]
        [MaxLength(256)]
        public string Email { get; set; }

        /// <summary>
        /// 手机号码
        /// </summary>
        [Display(Name = nameof(PhoneNumber), ResourceType = typeof(IdentityDisplayResources))]
        [JsonProperty("手机号码")] // 中文列名用于导入模板
        [Phone]
        [MaxLength(20)]
        public string PhoneNumber { get; set; }

        /// <summary>
        /// 姓名
        /// </summary>
        [Display(Name = nameof(Name), ResourceType = typeof(IdentityDisplayResources))]
        [JsonProperty("姓名")] // 中文列名用于导入模板
        [Required]
        [MaxLength(20)]
        public string Name { get; set; }

        /// <summary>
        /// 身份证号码
        /// </summary>
        [Display(Name = nameof(IdNo), ResourceType = typeof(IdentityDisplayResources))]
        [JsonProperty("身份证")] // 中文列名用于导入模板
        [MaxLength(18)]
        public string IdNo { get; set; }

        /// <summary>
        /// 性别
        /// </summary>
        [Display(Name = nameof(Gender), ResourceType = typeof(IdentityDisplayResources))]
        [JsonProperty("性别")] // 中文列名用于导入模板
        public Gender Gender { get; set; }
    }
}