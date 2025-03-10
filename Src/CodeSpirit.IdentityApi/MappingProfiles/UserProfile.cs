﻿using AutoMapper;
using CodeSpirit.Core;
using CodeSpirit.IdentityApi.Data.Models;
using System.Data;

namespace CodeSpirit.IdentityApi.MappingProfiles
{
    public class UserProfile : Profile
    {
        public UserProfile()
        {
            // 从 ApplicationUser 到 UserDto 的映射
        CreateMap<ApplicationUser, UserDto>()
            .ForMember(dest => dest.Roles, opt => opt.MapFrom(src => 
                src.UserRoles != null ? 
                src.UserRoles.Select(ur => ur.Role.Name).ToList() : 
                new List<string>()));
                
            // 从 CreateUserDto 到 ApplicationUser 的映射
            CreateMap<CreateUserDto, ApplicationUser>()
                //.ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true));

            // 从 UpdateUserDto 到 ApplicationUser 的映射
            CreateMap<UpdateUserDto, ApplicationUser>()
                .ForMember(dest => dest.UserName, opt => opt.Ignore()) // 通常不允许通过 UpdateUserDto 修改 UserName
                .ForMember(dest => dest.Email, opt => opt.Ignore()); // 如果需要，可以根据需求调整

            // 添加 PageList 映射配置
            CreateMap<PageList<ApplicationUser>, PageList<UserDto>>();
        }
    }
}
