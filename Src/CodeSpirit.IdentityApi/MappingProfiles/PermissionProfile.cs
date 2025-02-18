using AutoMapper;
using CodeSpirit.IdentityApi.Dtos.Permission;

namespace CodeSpirit.IdentityApi.MappingProfiles
{
    /// <summary>
    /// AutoMapper 配置文件，用于权限相关的映射。
    /// </summary>
    public class PermissionProfile : Profile
    {
        public PermissionProfile()
        {
            // Entity to DTO mappings
            CreateMap<PermissionNode, PermissionDto>()
                .ForMember(dest => dest.Children, opt => opt.MapFrom(src => src.Children))
                .ReverseMap();

            CreateMap<PermissionNode, PermissionTreeDto>()
                .ForMember(dest => dest.Label, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Code))
                .ForMember(dest => dest.Children, opt => opt.MapFrom(src => src.Children))
                .ReverseMap();

        }
    }
}
