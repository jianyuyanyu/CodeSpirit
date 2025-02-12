// 文件路径: Controllers/Dtos/UserQueryDto.cs
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.IdentityApi.Controllers.Dtos.Common
{
    public class QueryDtoBase
    {
        /// <summary>
        /// 关键字搜索
        /// </summary>
        [DisplayName("关键字")]
        public string Keywords { get; set; }

        /// <summary>
        /// 页码（默认第1页）
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "页码必须大于或等于1。")]
        public int Page { get; set; } = 1;

        /// <summary>
        /// 每页条数（默认10条）
        /// </summary>
        [Range(1, 100, ErrorMessage = "每页条数必须在1到100之间。")]
        public int PerPage { get; set; } = 10;

        /// <summary>
        /// 排序字段（可选，例如 "Name", "Email" 等）
        /// </summary>
        public string OrderBy { get; set; }

        /// <summary>
        /// 排序顺序（"asc" 或 "desc"，默认 "asc"）
        /// </summary>
        public string OrderDir { get; set; } = "asc";
    }
}
