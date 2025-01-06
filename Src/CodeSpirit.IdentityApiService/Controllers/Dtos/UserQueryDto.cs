// 文件路径: Controllers/Dtos/UserQueryDto.cs
using CodeSpirit.IdentityApi.Data.Models;

namespace CodeSpirit.IdentityApi.Controllers.Dtos
{
    /// <summary>
    /// 用户查询参数
    /// </summary>
    public class UserQueryDto
    {
        /// <summary>
        /// 关键字搜索（可匹配姓名、邮箱、身份证号码、用户名）
        /// </summary>
        public string? Search { get; set; }

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

        /// <summary>
        /// 页码（默认第1页）
        /// </summary>
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// 每页条数（默认10条）
        /// </summary>
        public int PageSize { get; set; } = 10;

        /// <summary>
        /// 排序字段（可选，例如 "Name", "Email" 等）
        /// </summary>
        public string? SortField { get; set; }

        /// <summary>
        /// 排序顺序（"asc" 或 "desc"，默认 "asc"）
        /// </summary>
        public string? SortOrder { get; set; } = "asc";
    }
}
