using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.ExamApi.Dtos.Question;

/// <summary>
/// 题目批量导入项DTO
/// </summary>
public class QuestionBatchImportItemDto
{
    /// <summary>
    /// 题目标题
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = null!;

    /// <summary>
    /// 题目内容
    /// </summary>
    [Required]
    public string Content { get; set; } = null!;

    /// <summary>
    /// 题目类型
    /// </summary>
    [Required]
    public string QuestionType { get; set; } = null!;

    /// <summary>
    /// 难度等级
    /// </summary>
    [Required]
    public int DifficultyLevel { get; set; }

    /// <summary>
    /// 标签列表
    /// </summary>
    public List<string>? Tags { get; set; }

    /// <summary>
    /// 答案
    /// </summary>
    [Required]
    public string Answer { get; set; } = null!;

    /// <summary>
    /// 解析说明
    /// </summary>
    public string? Analysis { get; set; }
} 