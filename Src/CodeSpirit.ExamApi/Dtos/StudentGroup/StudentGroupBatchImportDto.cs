using Newtonsoft.Json;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.ExamApi.Dtos.StudentGroup;

/// <summary>
/// 考生组批量导入DTO
/// </summary>
public class StudentGroupBatchImportDto
{
    /// <summary>
    /// 分组名称
    /// </summary>
    [Required(ErrorMessage = "分组名称不能为空")]
    [StringLength(100, ErrorMessage = "分组名称最大长度为100")]
    [DisplayName("分组名称")]
    [JsonProperty("分组名称")]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 分组描述
    /// </summary>
    [StringLength(500, ErrorMessage = "分组描述最大长度为500")]
    [DisplayName("描述")]
    [JsonProperty("描述")]
    public string? Description { get; set; }
} 