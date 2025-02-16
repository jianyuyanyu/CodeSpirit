using CodeSpirit.Core.Dtos;
using System;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.ConfigCenter.Dtos.QueryDtos;

/// <summary>
/// 发布历史查询参数
/// </summary>
public class PublishQueryDto : QueryDtoBase
{
    /// <summary>
    /// 应用ID
    /// </summary>
    [Required]
    public string AppId { get; set; }

    /// <summary>
    /// 环境
    /// </summary>
    [Required]
    public string Environment { get; set; }

    /// <summary>
    /// 发布状态
    /// </summary>
    public string Status { get; set; }

    /// <summary>
    /// 发布人
    /// </summary>
    public string PublishBy { get; set; }

    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// 结束时间
    /// </summary>
    public DateTime? EndTime { get; set; }
} 