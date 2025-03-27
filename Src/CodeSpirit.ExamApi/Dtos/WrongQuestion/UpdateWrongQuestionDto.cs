using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CodeSpirit.Amis.Attributes.FormFields;

namespace CodeSpirit.ExamApi.Dtos.WrongQuestion;

/// <summary>
/// 更新错题DTO
/// </summary>
public class UpdateWrongQuestionDto
{    
    /// <summary>
    /// 错误次数
    /// </summary>
    [Required(ErrorMessage = "错误次数不能为空")]
    [Range(1, 1000, ErrorMessage = "错误次数必须大于0且小于1000")]
    [DisplayName("错误次数")]
    public int WrongCount { get; set; }
    
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
    public DateTime LastWrongTime { get; set; }
    
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