using AutoMapper;
using CodeSpirit.IdentityApi.Controllers.Dtos;
using CodeSpirit.IdentityApi.Data.Models;
using System.Data;

namespace CodeSpirit.IdentityApi.MappingProfiles
{
    public class RoleProfile : Profile
    {
        public RoleProfile()
        {
            CreateMap<ApplicationRole, RoleDto>()
                .ForMember(dest => dest.Permissions,
                           opt => opt.MapFrom(src => src.RolePermissions.Select(rp => rp.Permission)));

            //CreateMap<Permission, PermissionDto>()
            //    .ForMember(dest => dest.Children,
            //               opt => opt.MapFrom(src => src.Children != null
            //                   ? src.Children.Select(c => new PermissionDto
            //                   {
            //                       Id = c.Id,
            //                       Name = c.Name,
            //                       Description = c.Description,
            //                       IsAllowed = c.IsAllowed,  // 确保使用子权限自身的值
            //                       ParentId = c.ParentId
            //                   })
            //                   : null));
        }
    }
}
