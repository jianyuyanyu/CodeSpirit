using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Dtos;
using CodeSpirit.FileStorageApi.Entities;
using Newtonsoft.Json;

namespace CodeSpirit.FileStorageApi.Dtos;

/// <summary>
/// 图片信息DTO
/// </summary>
public class ImageDto : FileDto
{
    /// <summary>
    /// 图片宽度（像素）
    /// </summary>
    [DisplayName("宽度")]
    public int Width { get; set; }

    /// <summary>
    /// 图片高度（像素）
    /// </summary>
    [DisplayName("高度")]
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
    public string Format { get; set; }

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
    public string CameraModel { get; set; }

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

    /// <summary>
    /// 缩略图列表
    /// </summary>
    [DisplayName("缩略图")]
    public List<ThumbnailDto> Thumbnails { get; set; } = new();
}

/// <summary>
/// 缩略图DTO
/// </summary>
public class ThumbnailDto
{
    /// <summary>
    /// 缩略图ID
    /// </summary>
    [DisplayName("缩略图ID")]
    public long Id { get; set; }

    /// <summary>
    /// 缩略图尺寸标识
    /// </summary>
    [DisplayName("尺寸标识")]
    public string SizeKey { get; set; }

    /// <summary>
    /// 宽度
    /// </summary>
    [DisplayName("宽度")]
    public int Width { get; set; }

    /// <summary>
    /// 高度
    /// </summary>
    [DisplayName("高度")]
    public int Height { get; set; }

    /// <summary>
    /// 缩略图文件ID
    /// </summary>
    [DisplayName("文件ID")]
    public long ThumbnailFileId { get; set; }

    /// <summary>
    /// 缩略图下载URL
    /// </summary>
    [DisplayName("下载链接")]
    public string DownloadUrl { get; set; }
}

/// <summary>
/// 图片查询DTO
/// </summary>
public class ImageQueryDto : QueryDtoBase
{
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
}

/// <summary>
/// 图片上传DTO
/// </summary>
public class CreateImageDto : CreateFileDto
{
    /// <summary>
    /// 是否自动生成缩略图
    /// </summary>
    [DisplayName("自动生成缩略图")]
    public bool AutoGenerateThumbnails { get; set; } = true;

    /// <summary>
    /// 缩略图尺寸配置
    /// </summary>
    [DisplayName("缩略图尺寸")]
    public List<string> ThumbnailSizes { get; set; } = new() { "small" };

    /// <summary>
    /// 图片质量（1-100）
    /// </summary>
    [DisplayName("图片质量")]
    [Range(1, 100)]
    public int Quality { get; set; } = 85;

    /// <summary>
    /// 是否提取EXIF信息
    /// </summary>
    [DisplayName("提取EXIF信息")]
    public bool ExtractExifData { get; set; } = true;
}

/// <summary>
/// 图片处理DTO
/// </summary>
public class ImageProcessDto
{
    /// <summary>
    /// 目标宽度
    /// </summary>
    [DisplayName("目标宽度")]
    public int? TargetWidth { get; set; }

    /// <summary>
    /// 目标高度
    /// </summary>
    [DisplayName("目标高度")]
    public int? TargetHeight { get; set; }

    /// <summary>
    /// 图片质量（1-100）
    /// </summary>
    [DisplayName("图片质量")]
    [Range(1, 100)]
    public int Quality { get; set; } = 85;

    /// <summary>
    /// 是否保持长宽比
    /// </summary>
    [DisplayName("保持长宽比")]
    public bool KeepAspectRatio { get; set; } = true;

    /// <summary>
    /// 输出格式
    /// </summary>
    [DisplayName("输出格式")]
    public string OutputFormat { get; set; }

    /// <summary>
    /// 水印文本
    /// </summary>
    [DisplayName("水印文本")]
    public string WatermarkText { get; set; }

    /// <summary>
    /// 水印位置
    /// </summary>
    [DisplayName("水印位置")]
    public string WatermarkPosition { get; set; } = "BottomRight";
}

/// <summary>
/// 生成缩略图DTO
/// </summary>
public class GenerateThumbnailDto
{
    /// <summary>
    /// 缩略图尺寸列表
    /// </summary>
    [Required]
    [DisplayName("缩略图尺寸")]
    public List<string> ThumbnailSizes { get; set; } = new();

    /// <summary>
    /// 是否覆盖已存在的缩略图
    /// </summary>
    [DisplayName("覆盖已存在")]
    public bool OverwriteExisting { get; set; } = false;

    /// <summary>
    /// 图片质量（1-100）
    /// </summary>
    [DisplayName("图片质量")]
    [Range(1, 100)]
    public int Quality { get; set; } = 85;
}
