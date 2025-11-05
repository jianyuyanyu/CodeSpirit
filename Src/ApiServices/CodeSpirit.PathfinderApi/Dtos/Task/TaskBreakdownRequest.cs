using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CodeSpirit.Amis.Attributes.FormFields;

namespace CodeSpirit.PathfinderApi.Dtos.Task;

/// <summary>
/// 任务拆解请求DTO
/// </summary>
public class TaskBreakdownRequest
{
    /// <summary>
    /// 目标ID
    /// </summary>
    [Required(ErrorMessage = "目标ID不能为空")]
    [DisplayName("目标ID")]
    [AmisInputTextField(Static = true)]
    public Guid GoalId { get; set; }
    
    /// <summary>
    /// 是否自动执行拆解后的任务
    /// </summary>
    [DisplayName("自动执行")]
    public bool AutoExecute { get; set; } = true;
    
    /// <summary>
    /// 拆解粒度（1-5，数字越大拆解越细）
    /// </summary>
    [Range(1, 5, ErrorMessage = "拆解粒度必须在1-5之间")]
    [DisplayName("拆解粒度")]
    public int Granularity { get; set; } = 3;
}

