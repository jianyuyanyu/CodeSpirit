// 文件路径: Controllers/Dtos/UserQueryDto.cs
using CodeSpirit.IdentityApi.Controllers.Dtos.Common;
using CodeSpirit.IdentityApi.Data.Models;
using System.ComponentModel;

namespace CodeSpirit.IdentityApi.Controllers.Dtos.User
{
    /// <summary>
    /// 用户查询参数
    /// </summary>
    public class UserQueryDto : QueryDtoBase
    {
        /// <summary>
        /// 是否激活
        /// </summary>
        [DisplayName("是否激活")]
        public bool? IsActive { get; set; }

        /// <summary>
        /// 性别筛选
        /// </summary>
        [DisplayName("性别")]
        public Gender? Gender { get; set; }

        /// <summary>
        /// 角色名称筛选
        /// </summary>
        [DisplayName("角色")]
        public string Role { get; set; }

        /// <summary>
        /// 最后登录时间起始 (时间戳，逗号分隔)
        /// </summary>
        [DisplayName("最后登录时间")]
        public DateTime[] LastLoginTime { get; set; }
    }
}

