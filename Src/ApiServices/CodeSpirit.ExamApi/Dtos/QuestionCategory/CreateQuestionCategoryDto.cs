using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CodeSpirit.Amis.Attributes.FormFields;

namespace CodeSpirit.ExamApi.Dtos.QuestionCategory;

/// <summary>
/// 创建题目分类DTO
/// </summary>
public class CreateQuestionCategoryDto
{
    /// <summary>
    /// 分类名称
    /// </summary>
    [Required(ErrorMessage = "分类名称不能为空")]
    [StringLength(100, ErrorMessage = "分类名称最大长度为100")]
    [DisplayName("分类名称")]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 分类描述
    /// </summary>
    [StringLength(500, ErrorMessage = "分类描述最大长度为500")]
    [DisplayName("描述")]
    [AmisTextareaField(MaxLength = 500, ShowCounter = true)]
    public string? Description { get; set; }
    
    /// <summary>
    /// 父分类ID
    /// </summary>
    [DisplayName("父分类")]
    [AmisSelectField(
        Source = "${ROOT_API}/api/exam/QuestionCategories",
        ValueField = "id",
        LabelField = "name",
        Searchable = true,
        Multiple = false
    )]
    public long? ParentId { get; set; }
} 