using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using CodeSpirit.ExamApi.Data.Models.Enums;

namespace CodeSpirit.ExamApi.Dtos.PracticeRecord;

/// <summary>
/// 练习记录批量导入项DTO
/// </summary>
public class PracticeRecordBatchImportDto
{
    /// <summary>
    /// 考生ID
    /// </summary>
    [Required]
    [DisplayName("考生ID")]
    [JsonProperty("考生ID")]
    public long StudentId { get; set; }
    
    /// <summary>
    /// 题目ID
    /// </summary>
    [Required]
    [DisplayName("题目ID")]
    [JsonProperty("题目ID")]
    public long QuestionId { get; set; }
    
    /// <summary>
    /// 题目类型
    /// </summary>
    [Required]
    [DisplayName("题目类型")]
    [JsonProperty("题目类型")]
    public string QuestionType { get; set; } = string.Empty;
    
    /// <summary>
    /// 练习类型
    /// </summary>
    [Required]
    [DisplayName("练习类型")]
    [JsonProperty("练习类型")]
    public string PracticeType { get; set; } = string.Empty;
    
    /// <summary>
    /// 考生回答
    /// </summary>
    [Required]
    [DisplayName("考生回答")]
    [JsonProperty("考生回答")]
    public string Answer { get; set; } = string.Empty;
    
    /// <summary>
    /// 正确答案
    /// </summary>
    [Required]
    [DisplayName("正确答案")]
    [JsonProperty("正确答案")]
    public string CorrectAnswer { get; set; } = string.Empty;
    
    /// <summary>
    /// 耗时（秒）
    /// </summary>
    [Range(0, int.MaxValue)]
    [DisplayName("耗时（秒）")]
    [JsonProperty("耗时（秒）")]
    public int TimeSpent { get; set; }
    
    /// <summary>
    /// 练习设置ID
    /// </summary>
    [DisplayName("练习设置ID")]
    [JsonProperty("练习设置ID")]
    public long? PracticeSettingId { get; set; }
} 