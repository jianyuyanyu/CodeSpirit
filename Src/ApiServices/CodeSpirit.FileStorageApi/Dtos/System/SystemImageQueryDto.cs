using CodeSpirit.Core.Dtos;
using CodeSpirit.FileStorageApi.Entities;
using System.ComponentModel;

namespace CodeSpirit.FileStorageApi.Dtos.System;

/// <summary>
/// 系统图片查询DTO
/// </summary>
public class SystemImageQueryDto : QueryDtoBase
{
    /// <summary>
    /// 租户ID
    /// </summary>
    [DisplayName("租户ID")]
    public string? TenantId { get; set; }

    /// <summary>
    /// 存储桶名称
    /// </summary>
    [DisplayName("存储桶")]
    public string? BucketName { get; set; }

    /// <summary>
    /// 文件名关键词
    /// </summary>
    [DisplayName("文件名")]
    public string? FileName { get; set; }

    /// <summary>
    /// 图片格式
    /// </summary>
    [DisplayName("图片格式")]
    public string? Format { get; set; }

    /// <summary>
    /// 最小宽度
    /// </summary>
    [DisplayName("最小宽度")]
    public int? MinWidth { get; set; }

    /// <summary>
    /// 最大宽度
    /// </summary>
    [DisplayName("最大宽度")]
    public int? MaxWidth { get; set; }

    /// <summary>
    /// 最小高度
    /// </summary>
    [DisplayName("最小高度")]
    public int? MinHeight { get; set; }

    /// <summary>
    /// 最大高度
    /// </summary>
    [DisplayName("最大高度")]
    public int? MaxHeight { get; set; }

    /// <summary>
    /// 是否为动画图片
    /// </summary>
    [DisplayName("动画图片")]
    public bool? IsAnimated { get; set; }

    /// <summary>
    /// 是否有透明通道
    /// </summary>
    [DisplayName("透明通道")]
    public bool? HasAlpha { get; set; }

    /// <summary>
    /// 拍摄设备
    /// </summary>
    [DisplayName("拍摄设备")]
    public string? CameraModel { get; set; }

    /// <summary>
    /// 拍摄开始时间
    /// </summary>
    [DisplayName("拍摄时间从")]
    public DateTime? DateTakenFrom { get; set; }

    /// <summary>
    /// 拍摄结束时间
    /// </summary>
    [DisplayName("拍摄时间到")]
    public DateTime? DateTakenTo { get; set; }

    /// <summary>
    /// 创建开始时间
    /// </summary>
    [DisplayName("创建时间从")]
    public DateTime? CreatedFrom { get; set; }

    /// <summary>
    /// 创建结束时间
    /// </summary>
    [DisplayName("创建时间到")]
    public DateTime? CreatedTo { get; set; }

    /// <summary>
    /// 文件状态
    /// </summary>
    [DisplayName("文件状态")]
    public FileStatus? Status { get; set; }

    /// <summary>
    /// 是否公开访问
    /// </summary>
    [DisplayName("公开访问")]
    public bool? IsPublic { get; set; }
}
