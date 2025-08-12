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
    public string? OutputFormat { get; set; }

    /// <summary>
    /// 水印文本
    /// </summary>
    [DisplayName("水印文本")]
    public string? WatermarkText { get; set; }

    /// <summary>
    /// 水印位置
    /// </summary>
    [DisplayName("水印位置")]
    public string WatermarkPosition { get; set; } = "BottomRight";
}
