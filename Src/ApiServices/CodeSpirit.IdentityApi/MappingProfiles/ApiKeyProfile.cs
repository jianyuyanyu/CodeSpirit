using AutoMapper;
using CodeSpirit.Core;
using CodeSpirit.IdentityApi.Dtos.ApiKey;

namespace CodeSpirit.IdentityApi.MappingProfiles;

/// <summary>
/// API密钥映射配置
/// </summary>
public class ApiKeyProfile : Profile
{
    public ApiKeyProfile()
    {
        // 从 ApiKey 到 ApiKeyDto 的映射
        CreateMap<Data.Models.ApiKey, ApiKeyDto>();

        // 从 CreateApiKeyDto 到 ApiKey 的映射
        CreateMap<CreateApiKeyDto, Data.Models.ApiKey>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Prefix, opt => opt.Ignore())
            .ForMember(dest => dest.KeyHash, opt => opt.Ignore())
            .ForMember(dest => dest.UserId, opt => opt.Ignore())
            .ForMember(dest => dest.TenantId, opt => opt.Ignore())
            .ForMember(dest => dest.LastUsedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
            .ForMember(dest => dest.Permissions, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedBy, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(src => false));

        // 从 UpdateApiKeyDto 到 ApiKey 的映射
        CreateMap<UpdateApiKeyDto, Data.Models.ApiKey>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Prefix, opt => opt.Ignore())
            .ForMember(dest => dest.KeyHash, opt => opt.Ignore())
            .ForMember(dest => dest.UserId, opt => opt.Ignore())
            .ForMember(dest => dest.TenantId, opt => opt.Ignore())
            .ForMember(dest => dest.LastUsedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Permissions, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedBy, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore());

        // 从 ApiKey 到 CreateApiKeyResultDto 的映射
        CreateMap<Data.Models.ApiKey, CreateApiKeyResultDto>()
            .ForMember(dest => dest.ApiKey, opt => opt.Ignore()); // 明文密钥需要单独设置

        // 添加 PageList 映射配置
        CreateMap<PageList<Data.Models.ApiKey>, PageList<ApiKeyDto>>();
    }
}

