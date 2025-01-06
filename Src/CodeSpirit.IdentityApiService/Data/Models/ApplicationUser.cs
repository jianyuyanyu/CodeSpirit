using CodeSpirit.Shared.Data;
using CodeSpirit.Shared.Entities;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CodeSpirit.IdentityApi.Data.Models
{
    /// <summary>
    /// 用户信息
    /// </summary> 
    public class ApplicationUser : IdentityUser<string>, IIsActive, IFullEntityEvent
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
        public bool IsActive { get; set; }

        /// <summary>
        /// 用户与角色的多对多关系。
        /// </summary>
        public ICollection<ApplicationUserRole> UserRoles { get; set; }

        /// <summary>
        /// 手机号码（覆盖 IdentityUser 中的 PhoneNumber）
        /// </summary>
        [Phone(ErrorMessage = "无效的手机号码格式")]
        [MaxLength(11, ErrorMessage = "手机号码长度不能超过11位")]
        public override string PhoneNumber { get; set; }

        /// <summary>
        /// 性别（使用枚举）
        /// </summary>
        [Required]
        public Gender Gender { get; set; }
    }
}