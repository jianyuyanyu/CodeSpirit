using AutoMapper;
using CodeSpirit.IdentityApi.Controllers.Dtos;
using CodeSpirit.IdentityApi.Data.Models.RoleManagementApiIdentity.Models;

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
            CreateMap<Permission, PermissionDto>()
                .ForMember(dest => dest.Children, opt => opt.MapFrom(src => src.Children))
                .ReverseMap();

            CreateMap<Permission, PermissionTreeDto>()
                .ForMember(dest => dest.Label, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Children, opt => opt.MapFrom(src => src.Children))
                .ReverseMap();

            // DTO to Entity mappings
            CreateMap<PermissionCreateDto, Permission>();
            CreateMap<PermissionUpdateDto, Permission>();
        }
    }
}
