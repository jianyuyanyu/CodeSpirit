using AutoMapper;
using CodeSpirit.Core.Dtos;
using CodeSpirit.FileStorageApi.Abstractions;
using CodeSpirit.FileStorageApi.Dtos.System;
using CodeSpirit.FileStorageApi.Entities;
using CodeSpirit.FileStorageApi.Options;

namespace CodeSpirit.FileStorageApi.MappingProfiles;

/// <summary>
/// 文件存储映射配置
/// </summary>
public class FileStorageProfile : Profile
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public FileStorageProfile()
    {
        ConfigureFileEntityMappings();
        ConfigureFileReferenceMappings();
        ConfigureBucketMappings();
        ConfigureOperationResultMappings();
    }

    /// <summary>
    /// 配置文件实体映射
    /// </summary>
    private void ConfigureFileEntityMappings()
    {
        // FileEntity 到 SystemFileDto 的映射
        CreateMap<FileEntity, SystemFileDto>()
            .ForMember(dest => dest.SizeFormatted, opt => opt.MapFrom(src => FormatFileSize(src.Size)))
            .ForMember(dest => dest.ReferenceCount, opt => opt.Ignore()) // 在服务层单独设置
            .ForMember(dest => dest.CreatedTime, opt => opt.MapFrom(src => src.CreatedAt))
            .ForMember(dest => dest.ModifiedTime, opt => opt.MapFrom(src => src.UpdatedAt));

        // FileUploadRequest 到 FileEntity 的映射 (用于部分属性映射)
        CreateMap<FileUploadRequest, FileEntity>()
            .ForMember(dest => dest.OriginalFileName, opt => opt.MapFrom(src => src.FileName))
            .ForMember(dest => dest.ContentType, opt => opt.MapFrom(src => src.ContentType ?? "application/octet-stream"))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.ExpirationTime, opt => opt.MapFrom(src => src.ExpirationTime))
            .ForMember(dest => dest.IsPublic, opt => opt.MapFrom(src => src.IsPublic))
            .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => src.Tags != null ? string.Join(",", src.Tags.Select(kvp => $"{kvp.Key}:{kvp.Value}")) : string.Empty))
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.TenantId, opt => opt.Ignore())
            .ForMember(dest => dest.BucketName, opt => opt.Ignore())
            .ForMember(dest => dest.StorageFileName, opt => opt.Ignore())
            .ForMember(dest => dest.FilePath, opt => opt.Ignore())
            .ForMember(dest => dest.Size, opt => opt.Ignore())
            .ForMember(dest => dest.FileHash, opt => opt.Ignore())
            .ForMember(dest => dest.Extension, opt => opt.Ignore())
            .ForMember(dest => dest.Category, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.Ignore())
            .ForMember(dest => dest.AccessCount, opt => opt.Ignore())
            .ForMember(dest => dest.LastAccessTime, opt => opt.Ignore())
            .ForMember(dest => dest.DownloadUrl, opt => opt.Ignore())
            .ForMember(dest => dest.ETag, opt => opt.Ignore())
            .ForMember(dest => dest.Properties, opt => opt.Ignore())
            .ForMember(dest => dest.References, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore());

        // PageList 映射
        CreateMap<PageList<FileEntity>, PageList<SystemFileDto>>();
    }

    /// <summary>
    /// 配置文件引用映射
    /// </summary>
    private void ConfigureFileReferenceMappings()
    {
        // FileReferenceCreateRequest 到 FileReferenceEntity 的映射
        CreateMap<FileReferenceCreateRequest, FileReferenceEntity>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.IsTemporary ? ReferenceStatus.Pending : ReferenceStatus.Confirmed))
            .ForMember(dest => dest.IsTemporary, opt => opt.MapFrom(src => src.IsTemporary))
            .ForMember(dest => dest.ExpirationTime, opt => opt.MapFrom(src => src.IsTemporary ? (src.ExpirationTime ?? DateTime.UtcNow.AddDays(1)) : src.ExpirationTime))
            .ForMember(dest => dest.ConfirmedTime, opt => opt.MapFrom(src => src.IsTemporary ? (DateTime?)null : DateTime.UtcNow))
            .ForMember(dest => dest.Id, opt => opt.Ignore()) 
            .ForMember(dest => dest.TenantId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.File, opt => opt.Ignore());

        // PageList 映射
        CreateMap<PageList<FileReferenceEntity>, PageList<FileReferenceEntity>>();
    }

    /// <summary>
    /// 配置存储桶映射
    /// </summary>
    private void ConfigureBucketMappings()
    {
        // StorageBucketOptions 到 SystemBucketDto 的基础映射
        CreateMap<StorageBucketOptions, SystemBucketDto>()
            .ForMember(dest => dest.BucketName, opt => opt.Ignore()) // 需要单独设置
            .ForMember(dest => dest.StorageQuotaFormatted, opt => opt.MapFrom(src => src.StorageQuota.HasValue ? FormatFileSize(src.StorageQuota.Value) : "无限制"))
            .ForMember(dest => dest.MaxFileSizeFormatted, opt => opt.MapFrom(src => FormatFileSize(src.MaxFileSize ?? 0)))
            //.ForMember(dest => dest.MaxFileSize, opt => opt.MapFrom(src => src.MaxFileSize ?? 0))
            .ForMember(dest => dest.TotalFiles, opt => opt.Ignore()) // 需要从统计数据填充
            .ForMember(dest => dest.TotalStorageSize, opt => opt.Ignore()) // 需要从统计数据填充
            .ForMember(dest => dest.TotalStorageSizeFormatted, opt => opt.Ignore()) // 需要从统计数据填充
            .ForMember(dest => dest.LastUploadTime, opt => opt.Ignore()) // 需要从统计数据填充
            .ForMember(dest => dest.UsageTenantsCount, opt => opt.Ignore()) // 需要从统计数据填充
            .ForMember(dest => dest.CreatedTime, opt => opt.MapFrom(src => DateTime.UtcNow)); // 配置创建时间
    }

    /// <summary>
    /// 配置操作结果映射
    /// </summary>
    private void ConfigureOperationResultMappings()
    {
        // BatchOperationResult 映射
        CreateMap<BatchOperationResult, BatchOperationResult>();
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
