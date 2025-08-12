using AutoMapper;
using CodeSpirit.FileStorageApi.Dtos;
using CodeSpirit.FileStorageApi.Entities;

namespace CodeSpirit.FileStorageApi.MappingProfiles;

/// <summary>
/// 文件映射配置
/// </summary>
public class FileProfile : Profile
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public FileProfile()
    {
        CreateMap<FileEntity, FileDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.BucketName, opt => opt.MapFrom(src => src.BucketName))
            .ForMember(dest => dest.OriginalFileName, opt => opt.MapFrom(src => src.OriginalFileName))
            .ForMember(dest => dest.Size, opt => opt.MapFrom(src => src.Size))
            .ForMember(dest => dest.ContentType, opt => opt.MapFrom(src => src.ContentType))
            .ForMember(dest => dest.Extension, opt => opt.MapFrom(src => src.Extension))
            .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Category))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.AccessCount, opt => opt.MapFrom(src => src.AccessCount))
            .ForMember(dest => dest.LastAccessTime, opt => opt.MapFrom(src => src.LastAccessTime))
            .ForMember(dest => dest.ExpirationTime, opt => opt.MapFrom(src => src.ExpirationTime))
            .ForMember(dest => dest.IsPublic, opt => opt.MapFrom(src => src.IsPublic))
            .ForMember(dest => dest.DownloadUrl, opt => opt.MapFrom(src => src.DownloadUrl))
            .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => src.Tags))
            .ForMember(dest => dest.CreatedTime, opt => opt.MapFrom(src => src.CreatedAt))
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy.ToString()))
            .ForMember(dest => dest.UpdatedTime, opt => opt.MapFrom(src => src.UpdatedAt))
            .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy.HasValue ? src.UpdatedBy.Value.ToString() : null));

        CreateMap<CreateFileDto, FileUploadRequest>()
            .ForMember(dest => dest.BucketName, opt => opt.MapFrom(src => src.BucketName))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.ExpirationTime, opt => opt.MapFrom(src => src.ExpirationTime))
            .ForMember(dest => dest.IsPublic, opt => opt.MapFrom(src => src.IsPublic))
            .ForMember(dest => dest.OverwriteExisting, opt => opt.MapFrom(src => src.OverwriteExisting))
            .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => 
                !string.IsNullOrEmpty(src.Tags) ? 
                src.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                       .ToDictionary(tag => tag.Trim(), tag => tag.Trim()) : 
                null))
            .ForMember(dest => dest.FileName, opt => opt.Ignore())
            .ForMember(dest => dest.FileStream, opt => opt.Ignore())
            .ForMember(dest => dest.ContentType, opt => opt.Ignore());

        CreateMap<FileEntity, ImageDto>()
            .IncludeBase<FileEntity, FileDto>()
            .ForMember(dest => dest.Width, opt => opt.MapFrom(src => src.ImageMetadata != null ? src.ImageMetadata.Width : 0))
            .ForMember(dest => dest.Height, opt => opt.MapFrom(src => src.ImageMetadata != null ? src.ImageMetadata.Height : 0))
            .ForMember(dest => dest.ColorDepth, opt => opt.MapFrom(src => src.ImageMetadata != null ? src.ImageMetadata.ColorDepth : 0))
            .ForMember(dest => dest.Format, opt => opt.MapFrom(src => src.ImageMetadata != null ? src.ImageMetadata.Format : null))
            .ForMember(dest => dest.HasAlpha, opt => opt.MapFrom(src => src.ImageMetadata != null && src.ImageMetadata.HasAlpha))
            .ForMember(dest => dest.IsAnimated, opt => opt.MapFrom(src => src.ImageMetadata != null && src.ImageMetadata.IsAnimated))
            .ForMember(dest => dest.FrameCount, opt => opt.MapFrom(src => src.ImageMetadata != null ? src.ImageMetadata.FrameCount : 0))
            .ForMember(dest => dest.DpiX, opt => opt.MapFrom(src => src.ImageMetadata != null ? src.ImageMetadata.DpiX : 0))
            .ForMember(dest => dest.DpiY, opt => opt.MapFrom(src => src.ImageMetadata != null ? src.ImageMetadata.DpiY : 0))
            .ForMember(dest => dest.CameraModel, opt => opt.MapFrom(src => src.ImageMetadata != null ? src.ImageMetadata.CameraModel : null))
            .ForMember(dest => dest.DateTaken, opt => opt.MapFrom(src => src.ImageMetadata != null ? src.ImageMetadata.DateTaken : null))
            .ForMember(dest => dest.Latitude, opt => opt.MapFrom(src => src.ImageMetadata != null ? src.ImageMetadata.Latitude : null))
            .ForMember(dest => dest.Longitude, opt => opt.MapFrom(src => src.ImageMetadata != null ? src.ImageMetadata.Longitude : null))
            .ForMember(dest => dest.Thumbnails, opt => opt.MapFrom(src => src.ImageMetadata != null ? src.ImageMetadata.Thumbnails : null));

        CreateMap<ThumbnailEntity, ThumbnailDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.SizeKey, opt => opt.MapFrom(src => src.SizeKey))
            .ForMember(dest => dest.Width, opt => opt.MapFrom(src => src.Width))
            .ForMember(dest => dest.Height, opt => opt.MapFrom(src => src.Height))
            .ForMember(dest => dest.ThumbnailFileId, opt => opt.MapFrom(src => src.ThumbnailFileId))
            .ForMember(dest => dest.DownloadUrl, opt => opt.MapFrom(src => src.ThumbnailFile != null ? src.ThumbnailFile.DownloadUrl : null));

        CreateMap<FileReferenceEntity, FileReferenceDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.FileId, opt => opt.MapFrom(src => src.FileId))
            .ForMember(dest => dest.SourceService, opt => opt.MapFrom(src => src.SourceService))
            .ForMember(dest => dest.SourceEntityType, opt => opt.MapFrom(src => src.SourceEntityType))
            .ForMember(dest => dest.SourceEntityId, opt => opt.MapFrom(src => src.SourceEntityId))
            .ForMember(dest => dest.FieldName, opt => opt.MapFrom(src => src.FieldName))
            .ForMember(dest => dest.ReferenceType, opt => opt.MapFrom(src => src.ReferenceType))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
            .ForMember(dest => dest.IsTemporary, opt => opt.MapFrom(src => src.IsTemporary))
            .ForMember(dest => dest.ExpirationTime, opt => opt.MapFrom(src => src.ExpirationTime))
            .ForMember(dest => dest.ConfirmedTime, opt => opt.MapFrom(src => src.ConfirmedTime))
            .ForMember(dest => dest.Remarks, opt => opt.MapFrom(src => src.Remarks))
            .ForMember(dest => dest.CreatedTime, opt => opt.MapFrom(src => src.CreatedAt))
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy.ToString()))
            .ForMember(dest => dest.UpdatedTime, opt => opt.MapFrom(src => src.UpdatedAt))
            .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy.HasValue ? src.UpdatedBy.Value.ToString() : null))
            .ForMember(dest => dest.File, opt => opt.MapFrom(src => src.File));

        CreateMap<CreateFileReferenceDto, FileReferenceCreateRequest>()
            .ForMember(dest => dest.FileId, opt => opt.MapFrom(src => src.FileId))
            .ForMember(dest => dest.SourceService, opt => opt.MapFrom(src => src.SourceService))
            .ForMember(dest => dest.SourceEntityType, opt => opt.MapFrom(src => src.SourceEntityType))
            .ForMember(dest => dest.SourceEntityId, opt => opt.MapFrom(src => src.SourceEntityId))
            .ForMember(dest => dest.FieldName, opt => opt.MapFrom(src => src.FieldName))
            .ForMember(dest => dest.ReferenceType, opt => opt.MapFrom(src => src.ReferenceType))
            .ForMember(dest => dest.IsTemporary, opt => opt.MapFrom(src => src.IsTemporary))
            .ForMember(dest => dest.ExpirationTime, opt => opt.MapFrom(src => src.ExpirationTime))
            .ForMember(dest => dest.Remarks, opt => opt.MapFrom(src => src.Remarks))
            .ForMember(dest => dest.Properties, opt => opt.Ignore());

        CreateMap<FileQueryDto, FileQueryRequest>()
            .ForMember(dest => dest.BucketName, opt => opt.MapFrom(src => src.BucketName))
            .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Category))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
            .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => src.Tags))
            .ForMember(dest => dest.FileName, opt => opt.MapFrom(src => src.FileName))
            .ForMember(dest => dest.CreatedFrom, opt => opt.MapFrom(src => src.CreatedFrom))
            .ForMember(dest => dest.CreatedTo, opt => opt.MapFrom(src => src.CreatedTo))
            .ForMember(dest => dest.PageNumber, opt => opt.MapFrom(src => src.Page))
            .ForMember(dest => dest.PageSize, opt => opt.MapFrom(src => src.PerPage))
            .ForMember(dest => dest.OrderBy, opt => opt.MapFrom(src => src.OrderBy ?? "CreatedTime"))
            .ForMember(dest => dest.Descending, opt => opt.MapFrom(src => src.OrderDir == "desc"));

        CreateMap<FileReferenceQueryDto, ReferenceQueryRequest>()
            .ForMember(dest => dest.FileId, opt => opt.MapFrom(src => src.FileId))
            .ForMember(dest => dest.SourceService, opt => opt.MapFrom(src => src.SourceService))
            .ForMember(dest => dest.SourceEntityType, opt => opt.MapFrom(src => src.SourceEntityType))
            .ForMember(dest => dest.SourceEntityId, opt => opt.MapFrom(src => src.SourceEntityId))
            .ForMember(dest => dest.ReferenceType, opt => opt.MapFrom(src => src.ReferenceType))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
            .ForMember(dest => dest.IsTemporary, opt => opt.MapFrom(src => src.IsTemporary))
            .ForMember(dest => dest.CreatedFrom, opt => opt.MapFrom(src => src.CreatedFrom))
            .ForMember(dest => dest.CreatedTo, opt => opt.MapFrom(src => src.CreatedTo))
            .ForMember(dest => dest.PageNumber, opt => opt.MapFrom(src => src.Page))
            .ForMember(dest => dest.PageSize, opt => opt.MapFrom(src => src.PerPage))
            .ForMember(dest => dest.OrderBy, opt => opt.MapFrom(src => src.OrderBy ?? "CreatedTime"))
            .ForMember(dest => dest.Descending, opt => opt.MapFrom(src => src.OrderDir == "desc"));
    }
}
