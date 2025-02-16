using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.Core.Dtos
{
    /// <summary>
    /// 批量删除请求DTO
    /// </summary>
    /// <typeparam name="T">ID的类型，通常是string、int或Guid等</typeparam>
    public class BatchDeleteDto<T>
    {
        /// <summary>
        /// 要删除的ID列表
        /// </summary>
        [Required(ErrorMessage = "ID列表不能为空")]
        [MinLength(1, ErrorMessage = "至少需要一个ID")]
        public List<T> Ids { get; set; } = new();
    }
}