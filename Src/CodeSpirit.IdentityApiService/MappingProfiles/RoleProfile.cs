using AutoMapper;
using CodeSpirit.IdentityApi.Controllers.Dtos;
using CodeSpirit.IdentityApi.Data.Models;

namespace CodeSpirit.IdentityApi.MappingProfiles
{
    public class RoleProfile : Profile
    {
        public RoleProfile()
        {
            // 映射 ApplicationRole 到 RoleDto
            CreateMap<ApplicationRole, RoleDto>()
                .ForMember(dest => dest.PermissionIds,
                           opt => opt.MapFrom(src => src.RolePermission != null ? src.RolePermission.PermissionIds.ToList() : new List<string>()));

            // 映射 RoleCreateDto 到 ApplicationRole
            CreateMap<RoleCreateDto, ApplicationRole>();

            // 映射 RoleUpdateDto 到 ApplicationRole
            CreateMap<RoleUpdateDto, ApplicationRole>();
        }
    }
}
