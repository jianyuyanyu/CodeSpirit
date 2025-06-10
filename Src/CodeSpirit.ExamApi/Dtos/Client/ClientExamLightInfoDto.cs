using System.ComponentModel;

namespace CodeSpirit.ExamApi.Dtos.Client;

/// <summary>
/// 客户端考试轻量信息DTO
/// 用于考试开始页面的倒计时和基本信息显示
/// </summary>
public class ClientExamLightInfoDto
{
    /// <summary>
    /// 考试ID
    /// </summary>
    [DisplayName("考试ID")]
    public long Id { get; set; }
    
    /// <summary>
    /// 考试名称
    /// </summary>
    [DisplayName("考试名称")]
    public string Name { get; set; }
    
    /// <summary>
    /// 考试描述
    /// </summary>
    [DisplayName("考试描述")]
    public string Description { get; set; }
    
    /// <summary>
    /// 考试时长(分钟)
    /// </summary>
    [DisplayName("考试时长")]
    public int Duration { get; set; }
    
    /// <summary>
    /// 开始时间
    /// </summary>
    [DisplayName("开始时间")]
    public DateTime StartTime { get; set; }
    
    /// <summary>
    /// 结束时间
    /// </summary>
    [DisplayName("结束时间")]
    public DateTime EndTime { get; set; }
    
    /// <summary>
    /// 总分
    /// </summary>
    [DisplayName("总分")]
    public double TotalScore { get; set; }
    
    /// <summary>
    /// 题目数量
    /// </summary>
    [DisplayName("题目数量")]
    public int QuestionCount { get; set; }
    
    /// <summary>
    /// 服务器当前时间 (UTC)
    /// </summary>
    [DisplayName("服务器当前时间")]
    public DateTime ServerTime { get; set; }
    
    /// <summary>
    /// 考试状态
    /// </summary>
    [DisplayName("考试状态")]
    public string Status { get; set; }
    
    /// <summary>
    /// 是否可以开始考试
    /// </summary>
    [DisplayName("是否可以开始考试")]
    public bool CanStart { get; set; }
} 