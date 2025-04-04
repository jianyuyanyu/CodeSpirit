using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CodeSpirit.ExamApi.Data.Models.Enums;

namespace CodeSpirit.ExamApi.Dtos.Monitor;

/// <summary>
/// 监考大屏考生信息DTO
/// </summary>
public class ExamStudentMonitorDto
{
    /// <summary>
    /// 考试ID
    /// </summary>
    [DisplayName("考试ID")]
    public long ExamId { get; set; }
    
    /// <summary>
    /// 考试记录ID
    /// </summary>
    [DisplayName("考试记录ID")]
    public long RecordId { get; set; }
    
    /// <summary>
    /// 学生ID
    /// </summary>
    [DisplayName("学生ID")]
    public long StudentId { get; set; }
    
    /// <summary>
    /// 学生姓名
    /// </summary>
    [DisplayName("学生姓名")]
    public string Name { get; set; }
    
    /// <summary>
    /// 学号
    /// </summary>
    [DisplayName("学号")]
    public string StudentNumber { get; set; }
    
    /// <summary>
    /// 性别
    /// </summary>
    [DisplayName("性别")]
    public string Gender { get; set; }
    
    /// <summary>
    /// IP地址
    /// </summary>
    [DisplayName("IP地址")]
    public string IpAddress { get; set; }
    
    /// <summary>
    /// 设备信息
    /// </summary>
    [DisplayName("设备信息")]
    public string DeviceInfo { get; set; }
    
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
    /// 状态描述
    /// </summary>
    [DisplayName("状态描述")]
    public string StatusText { get; set; }
    
    /// <summary>
    /// 切屏次数
    /// </summary>
    [DisplayName("切屏次数")]
    public int ScreenSwitchCount { get; set; }
    
    /// <summary>
    /// 作弊嫌疑等级
    /// </summary>
    [DisplayName("作弊嫌疑等级")]
    public int CheatingSuspicionLevel { get; set; }
    
    /// <summary>
    /// 已答题数量
    /// </summary>
    [DisplayName("已答题数量")]
    public int AnsweredCount { get; set; }
    
    /// <summary>
    /// 总题目数量
    /// </summary>
    [DisplayName("总题目数量")]
    public int TotalQuestions { get; set; }
    
    /// <summary>
    /// 进度百分比
    /// </summary>
    [DisplayName("进度百分比")]
    public double ProgressPercentage { get; set; }
    
    /// <summary>
    /// 剩余时间(秒)
    /// </summary>
    [DisplayName("剩余时间(秒)")]
    public int? RemainingSeconds { get; set; }
    
    /// <summary>
    /// 上次活动时间
    /// </summary>
    [DisplayName("上次活动时间")]
    public DateTime? LastActivityTime { get; set; }
    
    /// <summary>
    /// 是否在线
    /// </summary>
    [DisplayName("是否在线")]
    public bool IsOnline { get; set; }
    
    /// <summary>
    /// 作弊记录
    /// </summary>
    [DisplayName("作弊记录")]
    public string CheatingSuspicionRecord { get; set; }
    
    /// <summary>
    /// 身份证号码
    /// </summary>
    [DisplayName("身份证号码")]
    public string IdCardNumber { get; set; }
    
    /// <summary>
    /// 剩余时间显示
    /// </summary>
    [DisplayName("剩余时间显示")]
    public string RemainingTimeDisplay { get; set; }
    
    /// <summary>
    /// 进度显示
    /// </summary>
    [DisplayName("进度显示")]
    public string ProgressDisplay { get; set; }
} 