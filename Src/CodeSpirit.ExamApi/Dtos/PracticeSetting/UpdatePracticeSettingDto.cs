using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CodeSpirit.ExamApi.Data.Models.Enums;

namespace CodeSpirit.ExamApi.Dtos.PracticeSetting;

/// <summary>
/// 更新练习设置DTO
/// </summary>
public class UpdatePracticeSettingDto
{
    /// <summary>
    /// 名称
    /// </summary>
    [Required(ErrorMessage = "名称不能为空")]
    [MaxLength(100, ErrorMessage = "名称长度不能超过100个字符")]
    [DisplayName("名称")]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 描述
    /// </summary>
    [MaxLength(500, ErrorMessage = "描述长度不能超过500个字符")]
    [DisplayName("描述")]
    public string? Description { get; set; }
    
    /// <summary>
    /// 练习模式
    /// </summary>
    [Required(ErrorMessage = "练习模式不能为空")]
    [DisplayName("练习模式")]
    public PracticeMode PracticeMode { get; set; }
    
    /// <summary>
    /// 练习次数限制(0表示不限制)
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "练习次数限制不能为负数")]
    [DisplayName("练习次数限制")]
    public int MaxAttempts { get; set; }
    
    /// <summary>
    /// 时长限制(分钟, 0表示不限制)
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "时长限制不能为负数")]
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
} 