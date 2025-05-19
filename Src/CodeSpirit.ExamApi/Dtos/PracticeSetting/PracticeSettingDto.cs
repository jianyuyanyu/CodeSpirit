using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CodeSpirit.Core.Attributes;
using CodeSpirit.Amis.Attributes.Columns;
using CodeSpirit.ExamApi.Data.Models.Enums;

namespace CodeSpirit.ExamApi.Dtos.PracticeSetting;

/// <summary>
/// 练习设置DTO
/// </summary>
public class PracticeSettingDto
{
    /// <summary>
    /// ID
    /// </summary>
    [DisplayName("ID")]
    public long Id { get; set; }
    
    /// <summary>
    /// 名称
    /// </summary>
    [DisplayName("名称")]
    [TplColumn(template: "${name}")]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 描述
    /// </summary>
    [DisplayName("描述")]
    public string? Description { get; set; }
    
    /// <summary>
    /// 试卷ID
    /// </summary>
    [DisplayName("试卷ID")]
    public long ExamPaperId { get; set; }
    
    /// <summary>
    /// 试卷名称
    /// </summary>
    [DisplayName("试卷名称")]
    [AggregateField("ExamPaper", "Name")]
    [TplColumn(template: "${examPaperName}")]
    public string ExamPaperName { get; set; } = string.Empty;
    
    /// <summary>
    /// 练习模式
    /// </summary>
    [DisplayName("练习模式")]
    public PracticeMode PracticeMode { get; set; }
    
    /// <summary>
    /// 练习次数限制(0表示不限制)
    /// </summary>
    [DisplayName("练习次数限制")]
    public int MaxAttempts { get; set; }
    
    /// <summary>
    /// 时长限制(分钟, 0表示不限制)
    /// </summary>
    [DisplayName("时长限制(分钟)")]
    public int TimeLimit { get; set; }
    
    /// <summary>
    /// 是否显示答案解析
    /// </summary>
    [DisplayName("显示答案解析")]
    public bool ShowAnalysis { get; set; }
    
    /// <summary>
    /// 是否随机排序题目
    /// </summary>
    [DisplayName("随机排序题目")]
    public bool RandomizeQuestions { get; set; }
    
    /// <summary>
    /// 状态
    /// </summary>
    [DisplayName("状态")]
    public PracticeSettingStatus Status { get; set; }
} 