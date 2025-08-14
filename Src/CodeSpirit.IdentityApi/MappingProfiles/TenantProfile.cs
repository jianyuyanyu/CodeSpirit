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

            // TenantUpdateDto -> TenantInfo (支持部分更新，忽略null和默认值)
            CreateMap<TenantUpdateDto, TenantInfo>()
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.TenantId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
                .ForMember(dest => dest.DeletedBy, opt => opt.Ignore())
                .ForMember(dest => dest.Strategy, opt => opt.Ignore()) // 策略字段不允许更新
                .ForMember(dest => dest.ConnectionString, opt => opt.Ignore()) // 连接字符串不允许更新
                .ForMember(dest => dest.TablePrefix, opt => opt.Ignore()) // 表前缀不允许更新
                // 配置部分更新逻辑：只有当源值不为null或有意义的值时才映射
                .ForMember(dest => dest.Name, opt => opt.Condition(src => !string.IsNullOrWhiteSpace(src.Name)))
                .ForMember(dest => dest.DisplayName, opt => opt.Condition(src => src.DisplayName != null))
                .ForMember(dest => dest.Description, opt => opt.Condition(src => src.Description != null))
                .ForMember(dest => dest.Domain, opt => opt.Condition(src => src.Domain != null))
                .ForMember(dest => dest.LogoUrl, opt => opt.Condition(src => src.LogoUrl != null))
                .ForMember(dest => dest.Configuration, opt => opt.Condition(src => src.Configuration != null))
                .ForMember(dest => dest.ThemeConfig, opt => opt.Condition(src => src.ThemeConfig != null))
                // 对于有默认值的数值字段，只有当值大于0时才更新
                .ForMember(dest => dest.MaxUsers, opt => opt.Condition(src => src.MaxUsers > 0))
                .ForMember(dest => dest.StorageLimit, opt => opt.Condition(src => src.StorageLimit > 0))
                // 对于可空字段，允许设置为null来清空值
                .ForMember(dest => dest.ExpiresAt, opt => opt.MapFrom(src => src.ExpiresAt))
                // IsActive字段始终更新，因为它是重要的状态字段
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive));
        }
    }
} 