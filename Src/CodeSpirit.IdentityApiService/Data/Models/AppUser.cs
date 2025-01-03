using CodeSpirit.Shared.Data;
using CodeSpirit.Shared.Entities;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CodeSpirit.IdentityApiService.Data.Models
{
    /// <summary>
    /// 用户信息
    /// </summary> 

    public class AppUser : IdentityUser<int>, IIsActive, IFullEntityEvent
    {
        /// <summary>
        /// 姓名
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string Name { get; set; }

        /// <summary>
        /// 身份证号码
        /// </summary>
        [MaxLength(18)]
        public string IdNo { get; set; }

        /// <summary>
        /// 头像地址
        /// </summary>
        [MaxLength(255, ErrorMessage = "图片地址长度不应超过255！")]
        [DataType(DataType.ImageUrl)]
        public string AvatarUrl { get; set; }

        /// <summary>
        /// 最后登录时间
        /// </summary>
        public DateTimeOffset? LastLoginTime { get; set; }
        public bool IsActive { get; }
    }
}
