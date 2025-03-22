namespace CodeSpirit.ExamApi.Dtos.ExamPaper;

/// <summary>
/// 创建试卷题目DTO
/// </summary>
[DisplayName("创建试卷题目")]
public class CreateExamPaperQuestionDto
{
    /// <summary>
    /// 题目ID
    /// </summary>
    [DisplayName("题目ID")]
    [Required(ErrorMessage = "题目ID不能为空")]
    public long QuestionId { get; set; }
    
    /// <summary>
    /// 题目版本ID
    /// </summary>
    [DisplayName("题目版本ID")]
    [Required(ErrorMessage = "题目版本ID不能为空")]
    public long QuestionVersionId { get; set; }
    
    /// <summary>
    /// 分值
    /// </summary>
    [DisplayName("分值")]
    [Required(ErrorMessage = "分值不能为空")]
    [Range(0, 100, ErrorMessage = "分值必须在0-100之间")]
    public int Score { get; set; }
    
    /// <summary>
    /// 题目序号
    /// </summary>
    [DisplayName("题目序号")]
    [Required(ErrorMessage = "题目序号不能为空")]
    public int OrderNumber { get; set; }
    
    /// <summary>
    /// 是否必答
    /// </summary>
    [DisplayName("是否必答")]
    public bool IsRequired { get; set; } = true;
}
