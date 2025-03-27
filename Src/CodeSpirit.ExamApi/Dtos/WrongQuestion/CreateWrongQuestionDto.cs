using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CodeSpirit.Amis.Attributes.FormFields;

namespace CodeSpirit.ExamApi.Dtos.WrongQuestion;

/// <summary>
/// 创建错题DTO
/// </summary>
public class CreateWrongQuestionDto
{
    /// <summary>
    /// 考生ID
    /// </summary>
    [Required(ErrorMessage = "考生ID不能为空")]
    [DisplayName("考生")]
    [AmisSelectField(
        Source = "${ROOT_API}/api/exam/Students",
        ValueField = "id",
        LabelField = "name",
        Searchable = true,
        Multiple = false
    )]
    public long StudentId { get; set; }
    
    /// <summary>
    /// 题目ID
    /// </summary>
    [Required(ErrorMessage = "题目ID不能为空")]
    [DisplayName("题目")]
    [AmisSelectField(
        Source = "${ROOT_API}/api/exam/Questions",
        ValueField = "id",
        LabelField = "content",
        Searchable = true,
        Multiple = false
    )]
    public long QuestionId { get; set; }
    
    /// <summary>
    /// 错误次数
    /// </summary>
    [Required(ErrorMessage = "错误次数不能为空")]
    [Range(1, 1000, ErrorMessage = "错误次数必须大于0且小于1000")]
    [DisplayName("错误次数")]
    public int WrongCount { get; set; } = 1;
    
    /// <summary>
    /// 最后一次错误答案
    /// </summary>
    [Required(ErrorMessage = "错误答案不能为空")]
    [StringLength(1000, ErrorMessage = "错误答案长度不能超过1000")]
    [DisplayName("错误答案")]
    [AmisTextareaField(MaxLength = 1000, ShowCounter = true)]
    public string LastWrongAnswer { get; set; } = string.Empty;
    
    /// <summary>
    /// 最后错误时间
    /// </summary>
    [DisplayName("错误时间")]
    [AmisDatetimeField(Utc = true)]
    public DateTime LastWrongTime { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 分类标签
    /// </summary>
    [StringLength(200, ErrorMessage = "标签长度不能超过200")]
    [DisplayName("标签")]
    public string? Tags { get; set; }
    
    /// <summary>
    /// 考生笔记
    /// </summary>
    [StringLength(1000, ErrorMessage = "笔记长度不能超过1000")]
    [DisplayName("笔记")]
    [AmisTextareaField(MaxLength = 1000, ShowCounter = true)]
    public string? Notes { get; set; }
} 