using CodeSpirit.Amis.Attributes;
using CodeSpirit.Amis.Attributes.Columns;
using CodeSpirit.Core.Attributes;
using CodeSpirit.FileStorageApi.Entities;
using System.ComponentModel;

namespace CodeSpirit.FileStorageApi.Dtos.System;

/// <summary>
/// 系统图片DTO
/// </summary>
public class SystemImageDto
{
    /// <summary>
    /// 文件ID
    /// </summary>
    [DisplayName("文件ID")]
    public long Id { get; set; }

    /// <summary>
    /// 租户ID
    /// </summary>
    [DisplayName("租户ID")]
    [AmisCardField(FieldType = CardFieldType.Description, Order = 3)]
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// 存储桶名称
    /// </summary>
    [DisplayName("存储桶")]
    public string BucketName { get; set; } = string.Empty;

    /// <summary>
    /// 原始文件名
    /// </summary>
    [DisplayName("文件名")]
    [AmisCardField(FieldType = CardFieldType.Title, Order = 1)]
    public string OriginalFileName { get; set; } = string.Empty;

    /// <summary>
    /// 文件路径
    /// </summary>
    [DisplayName("文件路径")]
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// 文件大小（格式化显示）
    /// </summary>
    [DisplayName("大小")]
    [AmisCardField(FieldType = CardFieldType.SubTitle, Order = 2)]
    public string SizeFormatted { get; set; } = string.Empty;

    /// <summary>
    /// 内容类型
    /// </summary>
    [DisplayName("文件类型")]
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// 文件扩展名
    /// </summary>
    [DisplayName("扩展名")]
    public string Extension { get; set; } = string.Empty;

    /// <summary>
    /// 文件分类
    /// </summary>
    [DisplayName("分类")]
    public FileTypeCategory Category { get; set; }

    /// <summary>
    /// 文件描述
    /// </summary>
    [DisplayName("描述")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 文件状态
    /// </summary>
    [DisplayName("状态")]
    public FileStatus Status { get; set; }

    /// <summary>
    /// 访问次数
    /// </summary>
    [DisplayName("访问次数")]
    public long AccessCount { get; set; }

    /// <summary>
    /// 最后访问时间
    /// </summary>
    [DisplayName("最后访问时间")]
    public DateTime? LastAccessTime { get; set; }

    /// <summary>
    /// 过期时间
    /// </summary>
    [DisplayName("过期时间")]
    public DateTime? ExpirationTime { get; set; }

    /// <summary>
    /// 是否公开访问
    /// </summary>
    [DisplayName("公开访问")]
    public bool IsPublic { get; set; }

    /// <summary>
    /// 下载URL
    /// </summary>
    [DisplayName("下载链接")]
    [AmisColumn(Type = "image")]
    [AmisCardField(FieldType = CardFieldType.Avatar, Order = 0)]
    public string DownloadUrl { get; set; } = string.Empty;

    /// <summary>
    /// 标签
    /// </summary>
    [DisplayName("标签")]
    public string Tags { get; set; } = string.Empty;

    /// <summary>
    /// 引用数量
    /// </summary>
    [DisplayName("引用数")]
    public int ReferenceCount { get; set; }

    /// <summary>
    /// 上传者ID
    /// </summary>
    [DisplayName("上传者ID")]
    [AggregateField(dataSource: "http://identity/api/identity/internal/users/{value}.data.name", template: "{field}")]
    public long? CreatedBy { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [DisplayName("上传时间")]
    [AmisCardField(FieldType = CardFieldType.Body, Order = 6, Template = "<p class=\"text-muted\"><i class=\"fa fa-clock\"></i> 上传于: ${createdTime|date:YYYY-MM-DD HH:mm}</p>")]
    public DateTime CreatedTime { get; set; }

    /// <summary>
    /// 修改时间
    /// </summary>
    [DisplayName("修改时间")]
    public DateTime? ModifiedTime { get; set; }

    // 图片特有属性
    /// <summary>
    /// 图片宽度（像素）
    /// </summary>
    [DisplayName("宽度")]
    [AmisCardField(FieldType = CardFieldType.Body, Order = 4, Template = "<span class=\"label label-info\">宽度: ${width}px</span>")]
    public int Width { get; set; }

    /// <summary>
    /// 图片高度（像素）
    /// </summary>
    [DisplayName("高度")]
    [AmisCardField(FieldType = CardFieldType.Body, Order = 5, Template = "<span class=\"label label-info\">高度: ${height}px</span>")]
    public int Height { get; set; }

    /// <summary>
    /// 颜色深度（位）
    /// </summary>
    [DisplayName("颜色深度")]
    public int ColorDepth { get; set; }

    /// <summary>
    /// 图片格式
    /// </summary>
    [DisplayName("图片格式")]
    public string? Format { get; set; }

    /// <summary>
    /// 是否有透明通道
    /// </summary>
    [DisplayName("透明通道")]
    public bool HasAlpha { get; set; }

    /// <summary>
    /// 是否为动画图片
    /// </summary>
    [DisplayName("动画图片")]
    public bool IsAnimated { get; set; }

    /// <summary>
    /// 帧数（动画图片）
    /// </summary>
    [DisplayName("帧数")]
    public int FrameCount { get; set; }

    /// <summary>
    /// DPI水平分辨率
    /// </summary>
    [DisplayName("水平DPI")]
    public double DpiX { get; set; }

    /// <summary>
    /// DPI垂直分辨率
    /// </summary>
    [DisplayName("垂直DPI")]
    public double DpiY { get; set; }

    /// <summary>
    /// 拍摄设备
    /// </summary>
    [DisplayName("拍摄设备")]
    public string? CameraModel { get; set; }

    /// <summary>
    /// 拍摄时间
    /// </summary>
    [DisplayName("拍摄时间")]
    public DateTime? DateTaken { get; set; }

    /// <summary>
    /// GPS纬度
    /// </summary>
    [DisplayName("纬度")]
    public double? Latitude { get; set; }

    /// <summary>
    /// GPS经度
    /// </summary>
    [DisplayName("经度")]
    public double? Longitude { get; set; }
}
