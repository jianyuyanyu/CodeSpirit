﻿using AutoMapper;
using CodeSpirit.Core;
using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.IdentityApi.Dtos.Role;

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

            CreateMap<RoleBatchImportItemDto, ApplicationRole>();

            // 添加 PageList 映射配置
            CreateMap<PageList<ApplicationRole>, PageList<RoleDto>>();
        }
    }
}
