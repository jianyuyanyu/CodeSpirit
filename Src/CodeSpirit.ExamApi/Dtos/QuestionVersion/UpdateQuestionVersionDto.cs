using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CodeSpirit.Amis.Attributes.FormFields;

namespace CodeSpirit.ExamApi.Dtos.QuestionVersion;

/// <summary>
/// 更新题目版本DTO
/// </summary>
public class UpdateQuestionVersionDto
{
    /// <summary>
    /// 题目内容
    /// </summary>
    [Required(ErrorMessage = "题目内容不能为空")]
    [StringLength(2000, ErrorMessage = "题目内容长度不能超过2000")]
    [DisplayName("题目内容")]
    [AmisTextareaField(MaxLength = 2000, ShowCounter = true)]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 题目选项
    /// </summary>
    [Required(ErrorMessage = "题目选项不能为空")]
    [DisplayName("选项")]
    public List<string> Options { get; set; } = [];

    /// <summary>
    /// 正确答案
    /// </summary>
    [Required(ErrorMessage = "正确答案不能为空")]
    [StringLength(4000, ErrorMessage = "正确答案长度不能超过4000")]
    [DisplayName("正确答案")]
    [AmisTextareaField(MaxLength = 4000, ShowCounter = true)]
    public string CorrectAnswer { get; set; } = string.Empty;

    /// <summary>
    /// 解析
    /// </summary>
    [StringLength(2000, ErrorMessage = "解析长度不能超过2000")]
    [DisplayName("解析")]
    [AmisTextareaField(MaxLength = 2000, ShowCounter = true)]
    public string? Analysis { get; set; }

    /// <summary>
    /// 知识点
    /// </summary>
    [StringLength(500, ErrorMessage = "知识点长度不能超过500")]
    [DisplayName("知识点")]
    public string? KnowledgePoints { get; set; }

    /// <summary>
    /// 题目分值
    /// </summary>
    [Required(ErrorMessage = "分值不能为空")]
    [Range(0, 100, ErrorMessage = "分值必须在0-100之间")]
    [DisplayName("分值")]
    public int DefaultScore { get; set; }

    /// <summary>
    /// 标签
    /// </summary>
    [StringLength(500, ErrorMessage = "标签长度不能超过500")]
    [DisplayName("标签")]
    public string? Tags { get; set; }

    /// <summary>
    /// 修改原因
    /// </summary>
    [Required(ErrorMessage = "修改原因不能为空")]
    [StringLength(500, ErrorMessage = "修改原因长度不能超过500")]
    [DisplayName("修改原因")]
    [AmisTextareaField(MaxLength = 500, ShowCounter = true)]
    public string ChangeReason { get; set; } = string.Empty;
} 