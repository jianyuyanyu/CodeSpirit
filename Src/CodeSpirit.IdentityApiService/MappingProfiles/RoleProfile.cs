using AutoMapper;
using CodeSpirit.IdentityApi.Controllers.Dtos;
using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.IdentityApi.Data.Models.RoleManagementApiIdentity.Models;
using System.Linq;

namespace CodeSpirit.IdentityApi.MappingProfiles
{
    public class RoleProfile : Profile
    {
        public RoleProfile()
        {
            // 映射 ApplicationRole 到 RoleDto
            CreateMap<ApplicationRole, RoleDto>()
                .ForMember(dest => dest.PermissionIds,
                           opt => opt.MapFrom(src => src.RolePermissions != null ? src.RolePermissions.Select(rp => rp.PermissionId).ToList() : new List<int>()));

            // 映射 RoleCreateDto 到 ApplicationRole
            CreateMap<RoleCreateDto, ApplicationRole>();

            // 映射 RoleUpdateDto 到 ApplicationRole
            CreateMap<RoleUpdateDto, ApplicationRole>();
        }
    }
}
