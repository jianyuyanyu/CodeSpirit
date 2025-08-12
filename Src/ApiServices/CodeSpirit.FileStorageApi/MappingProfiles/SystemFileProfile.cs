using AutoMapper;
using CodeSpirit.Core.Dtos;
using CodeSpirit.FileStorageApi.Abstractions;
using CodeSpirit.FileStorageApi.Dtos.System;
using CodeSpirit.FileStorageApi.Entities;
using CodeSpirit.FileStorageApi.Services.System;

namespace CodeSpirit.FileStorageApi.MappingProfiles;

/// <summary>
/// 系统文件映射配置
/// </summary>
public class SystemFileProfile : Profile
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public SystemFileProfile()
    {
        ConfigureFileQueryMappings();
        ConfigureFileResultMappings();
        ConfigureStorageStatisticsMappings();
    }

    /// <summary>
    /// 配置文件查询映射
    /// </summary>
    private void ConfigureFileQueryMappings()
    {
        // SystemFileQueryDto 到 FileQueryRequest 的映射
        CreateMap<SystemFileQueryDto, FileQueryRequest>()
            .ForMember(dest => dest.PageNumber, opt => opt.MapFrom(src => src.Page))
            .ForMember(dest => dest.PageSize, opt => opt.MapFrom(src => src.PerPage))
            .ForMember(dest => dest.OrderBy, opt => opt.MapFrom(src => src.OrderBy))
            .ForMember(dest => dest.Descending, opt => opt.MapFrom(src => src.OrderDir == "desc"))
            .ForMember(dest => dest.FileName, opt => opt.MapFrom(src => src.FileName))
            .ForMember(dest => dest.BucketName, opt => opt.MapFrom(src => src.BucketName))
            .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Category))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
            .ForMember(dest => dest.CreatedFrom, opt => opt.MapFrom(src => src.UploadStartTime))
            .ForMember(dest => dest.CreatedTo, opt => opt.MapFrom(src => src.UploadEndTime));
    }

    /// <summary>
    /// 配置文件结果映射
    /// </summary>
    private void ConfigureFileResultMappings()
    {
        // CleanupResult 映射
        CreateMap<CleanupResult, CleanupResult>();

        // BucketConnectionTestResult 映射
        CreateMap<BucketConnectionTestResult, BucketConnectionTestResult>();

        // SystemBatchOperationResult 映射
        CreateMap<SystemBatchOperationResult, SystemBatchOperationResult>();
        CreateMap<SystemBatchOperationDetail, SystemBatchOperationDetail>();
    }

    /// <summary>
    /// 配置存储统计映射
    /// </summary>
    private void ConfigureStorageStatisticsMappings()
    {
        // TenantStorageStatisticsDto 映射
        CreateMap<TenantStorageStatisticsDto, TenantStorageStatisticsDto>();

        // BucketStatistics 映射
        CreateMap<BucketStatistics, BucketStatistics>();
    }
}
