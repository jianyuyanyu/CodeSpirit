using CodeSpirit.ExamApi.Data.Models.Enums;
using System;
using System.Collections.Generic;

namespace CodeSpirit.ExamApi.Dtos.PracticeSetting;

/// <summary>
/// 练习基本信息DTO
/// </summary>
public class PracticeBasicInfoDto
{
    /// <summary>
    /// 练习设置ID
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 练习名称
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 练习描述
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// 试卷ID
    /// </summary>
    public long ExamPaperId { get; set; }

    /// <summary>
    /// 试卷名称
    /// </summary>
    public string ExamPaperName { get; set; }

    /// <summary>
    /// 练习模式
    /// </summary>
    public PracticeMode PracticeMode { get; set; }

    /// <summary>
    /// 题目数量
    /// </summary>
    public int QuestionCount { get; set; }

    /// <summary>
    /// 总分值
    /// </summary>
    public decimal TotalScore { get; set; }

    /// <summary>
    /// 学生ID
    /// </summary>
    public long StudentId { get; set; }

    /// <summary>
    /// 学生姓名
    /// </summary>
    public string StudentName { get; set; }

    /// <summary>
    /// 练习历史次数
    /// </summary>
    public int PracticeHistoryCount { get; set; }

    /// <summary>
    /// 最高得分
    /// </summary>
    public decimal HighestScore { get; set; }

    /// <summary>
    /// 练习记录ID
    /// </summary>
    public long? RecordId { get; set; }

    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// 时间限制(分钟)
    /// </summary>
    public int? TimeLimit { get; set; }

    /// <summary>
    /// 题目列表
    /// </summary>
    public object Questions { get; set; }
} 