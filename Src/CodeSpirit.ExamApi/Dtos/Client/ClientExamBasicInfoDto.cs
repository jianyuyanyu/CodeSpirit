using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace CodeSpirit.ExamApi.Dtos.Client;

/// <summary>
/// 客户端考试基本信息DTO
/// </summary>
public class ClientExamBasicInfoDto
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
    /// 考试记录ID
    /// </summary>
    [DisplayName("考试记录ID")]
    public long? RecordId { get; set; }
    
    /// <summary>
    /// 允许切屏次数（0表示不限制）
    /// </summary>
    [DisplayName("允许切屏次数")]
    public int AllowedScreenSwitchCount { get; set; }
    
    /// <summary>
    /// 当前切屏次数
    /// </summary>
    [DisplayName("当前切屏次数")]
    public int ScreenSwitchCount { get; set; }
    
    /// <summary>
    /// 提交后是否可以查看考试结果
    /// </summary>
    [DisplayName("提交后是否可以查看考试结果")]
    public bool EnableViewResult { get; set; }
    
    /// <summary>
    /// 最小考试时间（分钟），低于此时间不允许提交
    /// </summary>
    [DisplayName("最小考试时间（分钟）")]
    public int MinExamTime { get; set; }
    
    /// <summary>
    /// 是否启用切屏检测
    /// </summary>
    [DisplayName("是否启用切屏检测")]
    public bool EnableScreenSwitchDetection => AllowedScreenSwitchCount >= 0;
    
    /// <summary>
    /// 考试题目列表
    /// </summary>
    [DisplayName("考试题目列表")]
    public List<ClientExamQuestionDto> Questions { get; set; } = new List<ClientExamQuestionDto>();    
} 