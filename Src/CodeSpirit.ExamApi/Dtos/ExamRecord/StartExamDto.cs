using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.ExamApi.Dtos.ExamRecord;

/// <summary>
/// 开始考试DTO
/// </summary>
public class StartExamDto
{
    /// <summary>
    /// 考试设置ID
    /// </summary>
    [Required(ErrorMessage = "考试设置ID不能为空")]
    [DisplayName("考试设置ID")]
    public long ExamSettingId { get; set; }
    
    /// <summary>
    /// 考生ID（可选，如果不提供则使用当前登录用户）
    /// </summary>
    [DisplayName("考生ID")]
    public long? StudentId { get; set; }
    
    /// <summary>
    /// IP地址（可选，如果不提供则使用客户端IP）
    /// </summary>
    [DisplayName("IP地址")]
    [StringLength(50)]
    public string IpAddress { get; set; }
    
    /// <summary>
    /// 设备信息（JSON格式）
    /// </summary>
    [DisplayName("设备信息")]
    [StringLength(1000)]
    public string DeviceInfo { get; set; }
    
    /// <summary>
    /// 浏览器信息
    /// </summary>
    [DisplayName("浏览器信息")]
    [StringLength(500)]
    public string BrowserInfo { get; set; }
} 