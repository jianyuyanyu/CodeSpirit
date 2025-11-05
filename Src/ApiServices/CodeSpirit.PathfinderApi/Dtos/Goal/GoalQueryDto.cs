using CodeSpirit.PathfinderApi.Models.Enums;
using System.ComponentModel;

namespace CodeSpirit.PathfinderApi.Dtos.Goal;

/// <summary>
/// 目标查询数据传输对象
/// </summary>
public class GoalQueryDto : QueryDtoBase
{
    /// <summary>
    /// 用户ID筛选
    /// </summary>
    [DisplayName("用户ID")]
    public long? UserId { get; set; }
    
    /// <summary>
    /// 目标状态筛选
    /// </summary>
    [DisplayName("目标状态")]
    public GoalStatus? Status { get; set; }
    
    /// <summary>
    /// 目标类型筛选
    /// </summary>
    [DisplayName("目标类型")]
    public string? Category { get; set; }
    
    /// <summary>
    /// 搜索关键字（搜索标题和描述）
    /// </summary>
    [DisplayName("搜索关键字")]
    public string? Keyword { get; set; }
    
    /// <summary>
    /// 开始日期
    /// </summary>
    [DisplayName("开始日期")]
    public DateTime? StartDate { get; set; }
    
    /// <summary>
    /// 结束日期
    /// </summary>
    [DisplayName("结束日期")]
    public DateTime? EndDate { get; set; }
}

