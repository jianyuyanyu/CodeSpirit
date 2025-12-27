using CodeSpirit.Core.Dtos;
using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.IdentityApi.Resources;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.IdentityApi.Dtos.User
{
    /// <summary>
    /// 用户查询参数
    /// </summary>
    public class UserQueryDto : QueryDtoBase
    {
        /// <summary>
        /// 是否激活
        /// </summary>
        [Display(Name = nameof(IsActive), ResourceType = typeof(IdentityDisplayResources))]
        public bool? IsActive { get; set; }

        /// <summary>
        /// 性别筛选
        /// </summary>
        [Display(Name = nameof(Gender), ResourceType = typeof(IdentityDisplayResources))]
        public Gender? Gender { get; set; }

        /// <summary>
        /// 角色名称筛选
        /// </summary>
        [Display(Name = nameof(Role), ResourceType = typeof(IdentityDisplayResources))]
        public string Role { get; set; }

        /// <summary>
        /// 最后登录时间起始 (时间戳，逗号分隔)
        /// </summary>
        [Display(Name = nameof(LastLoginTime), ResourceType = typeof(IdentityDisplayResources))]
        public DateTime[] LastLoginTime { get; set; }
    }
}

