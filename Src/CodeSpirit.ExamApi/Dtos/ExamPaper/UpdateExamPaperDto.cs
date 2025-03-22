namespace CodeSpirit.ExamApi.Dtos.ExamPaper;

/// <summary>
/// 试卷更新DTO
/// </summary>
[DisplayName("更新试卷")]
public class UpdateExamPaperDto
{
    /// <summary>
    /// 试卷名称
    /// </summary>
    [DisplayName("试卷名称")]
    [Required(ErrorMessage = "试卷名称不能为空")]
    [StringLength(100, ErrorMessage = "试卷名称不能超过100个字符")]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 试卷描述
    /// </summary>
    [DisplayName("试卷描述")]
    [StringLength(500, ErrorMessage = "试卷描述不能超过500个字符")]
    public string? Description { get; set; }
    
    /// <summary>
    /// 总分
    /// </summary>
    [DisplayName("总分")]
    [Required(ErrorMessage = "总分不能为空")]
    [Range(1, 1000, ErrorMessage = "总分必须在1-1000之间")]
    public int TotalScore { get; set; }
    
    /// <summary>
    /// 及格分数
    /// </summary>
    [DisplayName("及格分数")]
    [Range(0, 1000, ErrorMessage = "及格分数必须在0-1000之间")]
    public int PassScore { get; set; }
    
    /// <summary>
    /// 时长（分钟）
    /// </summary>
    [DisplayName("时长（分钟）")]
    [Range(1, 1440, ErrorMessage = "考试时长必须在1-1440分钟之间")]
    public int Duration { get; set; }
    
    /// <summary>
    /// 随机试卷规则
    /// </summary>
    [DisplayName("随机试卷规则")]
    [StringLength(2000, ErrorMessage = "随机规则不能超过2000个字符")]
    public string? RandomRules { get; set; }
    
    /// <summary>
    /// 题目列表
    /// </summary>
    [DisplayName("题目列表")]
    public List<CreateExamPaperQuestionDto>? Questions { get; set; }
}
