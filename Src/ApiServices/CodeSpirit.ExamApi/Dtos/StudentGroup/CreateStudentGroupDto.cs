using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CodeSpirit.Amis.Attributes.FormFields;

namespace CodeSpirit.ExamApi.Dtos.StudentGroup;

/// <summary>
/// 创建考生组DTO
/// </summary>
public class CreateStudentGroupDto
{
    /// <summary>
    /// 分组名称
    /// </summary>
    [Required(ErrorMessage = "分组名称不能为空")]
    [StringLength(100, ErrorMessage = "分组名称最大长度为100")]
    [DisplayName("分组名称")]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 分组描述
    /// </summary>
    [StringLength(500, ErrorMessage = "分组描述最大长度为500")]
    [DisplayName("描述")]
    [AmisTextareaField(MaxLength = 500, ShowCounter = true)]
    public string? Description { get; set; }
    
    /// <summary>
    /// 考生ID列表
    /// </summary>
    [DisplayName("考生")]
    [AmisTransferField(
        Source = "${ROOT_API}/api/exam/Students",
        //SearchField = "name,studentNumber",
        ValueField = "id",
        LabelField = "name",
        Searchable = true,
        Multiple = true,
        JoinValues = false,
        ExtractValue = true
    )]
    public List<long> StudentIds { get; set; } = new();
} 