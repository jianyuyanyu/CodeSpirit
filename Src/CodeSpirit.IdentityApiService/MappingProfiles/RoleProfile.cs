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
                .ForMember(dest => dest.Children,
                           opt => opt.MapFrom(src => src.RolePermissions != null ? src.RolePermissions.Select(rp => MapToRolePermissionDto(rp)).ToList() : new List<RolePermissionDto>()));

            // 映射 RoleCreateDto 到 ApplicationRole
            CreateMap<RoleCreateDto, ApplicationRole>();

            // 映射 RoleUpdateDto 到 ApplicationRole
            CreateMap<RoleUpdateDto, ApplicationRole>();
        }

        // 辅助方法，将 RolePermission 映射为 RolePermissionDto
        private RolePermissionDto MapToRolePermissionDto(RolePermission rp)
        {
            // 确保 rp.Permission 不为 null
            if (rp?.Permission == null)
            {
                return null;
            }

            return new RolePermissionDto
            {
                Id = rp.Permission.Id,
                Name = rp.Permission.Name,
                RoleId = rp.RoleId,
                ParentId = rp.Permission.ParentId,
                Description = rp.Permission.Description,
                IsAllowed = rp.Permission.IsAllowed,
                Children = rp.Permission.Children != null ? rp.Permission.Children.Select(child => MapToPermissionDto(child, rp.RoleId)).ToList() : new List<RolePermissionDto>()
            };
        }

        // 辅助方法，将 Permission 映射为 RolePermissionDto
        private RolePermissionDto MapToPermissionDto(Permission permission, string roleId)
        {
            // 确保 permission 不为 null
            if (permission == null)
            {
                return null;
            }

            return new RolePermissionDto
            {
                Id = permission.Id,
                Name = permission.Name,
                RoleId = roleId,
                ParentId = permission.ParentId,
                Description = permission.Description,
                IsAllowed = permission.IsAllowed,
                Children = permission.Children != null ? permission.Children.Select(child => MapToPermissionDto(child, roleId)).ToList() : new List<RolePermissionDto>()
            };
        }
    }
}
