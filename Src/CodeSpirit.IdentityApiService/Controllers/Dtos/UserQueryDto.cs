// 文件路径: Controllers/Dtos/UserQueryDto.cs
using CodeSpirit.IdentityApi.Data.Models;

namespace CodeSpirit.IdentityApi.Controllers.Dtos
{
    /// <summary>
    /// 用户查询参数
    /// </summary>
    public class UserQueryDto : QueryDtoBase
    {
        /// <summary>
        /// 是否激活
        /// </summary>
        public bool? IsActive { get; set; }

        /// <summary>
        /// 性别筛选
        /// </summary>
        public Gender? Gender { get; set; }

        /// <summary>
        /// 角色名称筛选
        /// </summary>
        public string? Role { get; set; }
    }
}
