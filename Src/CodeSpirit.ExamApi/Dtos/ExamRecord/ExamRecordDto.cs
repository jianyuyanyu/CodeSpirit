using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CodeSpirit.Amis.Attributes.Columns;
using CodeSpirit.ExamApi.Data.Models.Enums;

namespace CodeSpirit.ExamApi.Dtos.ExamRecord;

/// <summary>
/// 考试记录DTO
/// </summary>
public class ExamRecordDto
{
    /// <summary>
    /// ID
    /// </summary>
    [DisplayName("ID")]
    public long Id { get; set; }
    
    /// <summary>
    /// 考试设置ID
    /// </summary>
    [DisplayName("考试设置ID")]
    [IgnoreColumn]
    public long ExamSettingId { get; set; }
    
    /// <summary>
    /// 考试名称
    /// </summary>
    [DisplayName("考试名称")]
    public string ExamName { get; set; }
    
    /// <summary>
    /// 考生ID
    /// </summary>
    [DisplayName("考生ID")]
    [IgnoreColumn]
    public long StudentId { get; set; }
    
    /// <summary>
    /// 考生姓名
    /// </summary>
    [DisplayName("考生姓名")]
    public string StudentName { get; set; }
    
    /// <summary>
    /// 尝试次数
    /// </summary>
    [DisplayName("尝试次数")]
    public int AttemptNumber { get; set; }
    
    /// <summary>
    /// 开始时间
    /// </summary>
    [DisplayName("开始时间")]
    public DateTime StartTime { get; set; }
    
    /// <summary>
    /// 提交时间
    /// </summary>
    [DisplayName("提交时间")]
    public DateTime? SubmitTime { get; set; }
    
    /// <summary>
    /// 状态
    /// </summary>
    [DisplayName("状态")]
    public ExamRecordStatus Status { get; set; }
    
    /// <summary>
    /// 状态名称
    /// </summary>
    [DisplayName("状态")]
    public string StatusName => Status.ToString();
    
    /// <summary>
    /// 得分
    /// </summary>
    [DisplayName("得分")]
    public double? Score { get; set; }
    
    /// <summary>
    /// 是否通过
    /// </summary>
    [DisplayName("是否通过")]
    public bool IsPassed { get; set; }
    
    /// <summary>
    /// 切屏次数
    /// </summary>
    [DisplayName("切屏次数")]
    public int ScreenSwitchCount { get; set; }
    
    /// <summary>
    /// IP地址
    /// </summary>
    [DisplayName("IP地址")]
    public string IpAddress { get; set; }
    
    /// <summary>
    /// 作弊嫌疑等级
    /// </summary>
    [DisplayName("作弊嫌疑等级")]
    public int CheatingSuspicionLevel { get; set; }
    
    /// <summary>
    /// 考试时长（分钟）
    /// </summary>
    [DisplayName("考试时长")]
    public int? Duration { get; set; }
    
    /// <summary>
    /// 评语
    /// </summary>
    [DisplayName("评语")]
    public string Comments { get; set; }
    
    /// <summary>
    /// 创建时间
    /// </summary>
    [DisplayName("创建时间")]
    public DateTime CreatedAt { get; set; }
} 