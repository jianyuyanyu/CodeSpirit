using AutoMapper;
using CodeSpirit.IdentityApi.Dtos.Tenant;
using CodeSpirit.MultiTenant.Models;

namespace CodeSpirit.IdentityApi.MappingProfiles
{
    /// <summary>
    /// 租户映射配置
    /// </summary>
    public class TenantProfile : Profile
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public TenantProfile()
        {
            // TenantInfo -> TenantDto
            CreateMap<TenantInfo, TenantDto>();

            // TenantCreateDto -> TenantInfo
            CreateMap<TenantCreateDto, TenantInfo>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.TenantId))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
                .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(src => false));

            // TenantUpdateDto -> TenantInfo
            CreateMap<TenantUpdateDto, TenantInfo>()
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.TenantId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
                .ForMember(dest => dest.DeletedBy, opt => opt.Ignore());
        }
    }
} 