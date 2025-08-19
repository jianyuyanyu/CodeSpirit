using AutoMapper;
using CodeSpirit.FileStorageApi.Dtos.System;
using CodeSpirit.FileStorageApi.Entities;

namespace CodeSpirit.FileStorageApi.MappingProfiles;

/// <summary>
/// 系统图片映射配置
/// </summary>
public class SystemImageProfile : Profile
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public SystemImageProfile()
    {
        ConfigureSystemImageMappings();
    }

    /// <summary>
    /// 配置系统图片映射
    /// </summary>
    private void ConfigureSystemImageMappings()
    {
        // 创建从 FileEntity + ImageMetadataEntity 组合到 SystemImageDto 的映射
        // 这个映射需要在服务层手动处理，因为涉及到两个实体的组合
        CreateMap<FileImageCombined, SystemImageDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.File.Id))
            .ForMember(dest => dest.TenantId, opt => opt.MapFrom(src => src.File.TenantId))
            .ForMember(dest => dest.BucketName, opt => opt.MapFrom(src => src.File.BucketName))
            .ForMember(dest => dest.OriginalFileName, opt => opt.MapFrom(src => src.File.OriginalFileName))
            .ForMember(dest => dest.FilePath, opt => opt.MapFrom(src => src.File.FilePath))
            .ForMember(dest => dest.SizeFormatted, opt => opt.MapFrom(src => FormatFileSize(src.File.Size)))
            .ForMember(dest => dest.ContentType, opt => opt.MapFrom(src => src.File.ContentType))
            .ForMember(dest => dest.Extension, opt => opt.MapFrom(src => Path.GetExtension(src.File.OriginalFileName)))
            .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.File.Category))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.File.Description))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.File.Status))
            .ForMember(dest => dest.AccessCount, opt => opt.MapFrom(src => src.File.AccessCount))
            .ForMember(dest => dest.LastAccessTime, opt => opt.MapFrom(src => src.File.LastAccessTime))
            .ForMember(dest => dest.ExpirationTime, opt => opt.MapFrom(src => src.File.ExpirationTime))
            .ForMember(dest => dest.IsPublic, opt => opt.MapFrom(src => src.File.IsPublic))
            .ForMember(dest => dest.DownloadUrl, opt => opt.MapFrom(src => src.File.DownloadUrl))
            .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => src.File.Tags))
            .ForMember(dest => dest.ReferenceCount, opt => opt.Ignore()) // 需要在服务层单独计算
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.File.CreatedBy))
            .ForMember(dest => dest.CreatedTime, opt => opt.MapFrom(src => src.File.CreatedAt))
            .ForMember(dest => dest.ModifiedTime, opt => opt.MapFrom(src => src.File.UpdatedAt))
            // 图片特有属性
            .ForMember(dest => dest.Width, opt => opt.MapFrom(src => src.Image != null ? src.Image.Width : 0))
            .ForMember(dest => dest.Height, opt => opt.MapFrom(src => src.Image != null ? src.Image.Height : 0))
            .ForMember(dest => dest.ColorDepth, opt => opt.MapFrom(src => src.Image != null ? src.Image.ColorDepth : 0))
            .ForMember(dest => dest.Format, opt => opt.MapFrom(src => src.Image != null ? src.Image.Format : null))
            .ForMember(dest => dest.HasAlpha, opt => opt.MapFrom(src => src.Image != null && src.Image.HasAlpha))
            .ForMember(dest => dest.IsAnimated, opt => opt.MapFrom(src => src.Image != null && src.Image.IsAnimated))
            .ForMember(dest => dest.FrameCount, opt => opt.MapFrom(src => src.Image != null ? src.Image.FrameCount : 0))
            .ForMember(dest => dest.DpiX, opt => opt.MapFrom(src => src.Image != null ? src.Image.DpiX : 0))
            .ForMember(dest => dest.DpiY, opt => opt.MapFrom(src => src.Image != null ? src.Image.DpiY : 0))
            .ForMember(dest => dest.CameraModel, opt => opt.MapFrom(src => src.Image != null ? src.Image.CameraModel : null))
            .ForMember(dest => dest.DateTaken, opt => opt.MapFrom(src => src.Image != null ? src.Image.DateTaken : null))
            .ForMember(dest => dest.Latitude, opt => opt.MapFrom(src => src.Image != null ? src.Image.Latitude : null))
            .ForMember(dest => dest.Longitude, opt => opt.MapFrom(src => src.Image != null ? src.Image.Longitude : null));
    }

    /// <summary>
    /// 格式化文件大小
    /// </summary>
    /// <param name="bytes">字节数</param>
    /// <returns>格式化后的大小字符串</returns>
    private static string FormatFileSize(long bytes)
    {
        if (bytes == 0) return "0 B";
        
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}

/// <summary>
/// 文件和图片元数据的组合类，用于AutoMapper映射
/// </summary>
public class FileImageCombined
{
    /// <summary>
    /// 文件实体
    /// </summary>
    public FileEntity File { get; set; } = null!;

    /// <summary>
    /// 图片元数据实体
    /// </summary>
    public ImageMetadataEntity? Image { get; set; }
}
