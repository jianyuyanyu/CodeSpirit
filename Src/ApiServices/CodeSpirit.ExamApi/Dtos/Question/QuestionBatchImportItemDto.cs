using Newtonsoft.Json;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.ExamApi.Dtos.Question;

/// <summary>
/// 题目批量导入项DTO
/// </summary>
public class QuestionBatchImportItemDto
{
    /// <summary>
    /// 题目内容
    /// </summary>
    [Required(ErrorMessage = "题目内容不能为空")]
    [StringLength(2000, ErrorMessage = "题目内容最多2000字符")]
    [DisplayName("题目内容")]
    [JsonProperty("题目内容")]
    public string Content { get; set; } = null!;

    /// <summary>
    /// 题目类型
    /// </summary>
    [Required(ErrorMessage = "请选择题目类型")]
    [DisplayName("题目类型")]
    [JsonProperty("题目类型")]
    public string QuestionType { get; set; } = null!;

    /// <summary>
    /// 难度等级
    /// </summary>
    [Required(ErrorMessage = "请选择题目难度")]
    [DisplayName("难度")]
    [JsonProperty("难度")]
    public int DifficultyLevel { get; set; }

    /// <summary>
    /// 标签列表
    /// </summary>
    [DisplayName("标签")]
    [JsonProperty("标签")]
    public string Tags { get; set; }

    /// <summary>
    /// 答案
    /// </summary>
    [Required(ErrorMessage = "请填写正确答案")]
    [StringLength(1000, ErrorMessage = "正确答案最多1000字符")]
    [DisplayName("正确答案")]
    [JsonProperty("正确答案")]
    public string Answer { get; set; } = null!;

    /// <summary>
    /// 解析说明
    /// </summary>
    [StringLength(2000, ErrorMessage = "解析最多2000字符")]
    [DisplayName("解析")]
    [JsonProperty("解析")]
    public string? Analysis { get; set; }
} 