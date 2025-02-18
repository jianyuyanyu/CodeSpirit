using AutoMapper;
using CodeSpirit.Core;
using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.IdentityApi.Dtos.LoginLogs;

namespace CodeSpirit.IdentityApi.MappingProfiles;

/// <summary>
/// 登录日志映射配置文件
/// </summary>
public class LoginLogProfile : Profile
{
    public LoginLogProfile()
    {
        // 从 LoginLog 实体到 LoginLogDto 的映射
        CreateMap<LoginLog, LoginLogDto>();

        // 添加 PageList 映射配置
        CreateMap<PageList<LoginLog>, PageList<LoginLogDto>>();
    }
} 