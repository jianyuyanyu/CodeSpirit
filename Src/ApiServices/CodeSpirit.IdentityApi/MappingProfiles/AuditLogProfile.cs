using AutoMapper;
using CodeSpirit.Core;
using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.IdentityApi.Dtos.AuditLog;

namespace CodeSpirit.IdentityApi.MappingProfiles
{
    public class AuditLogProfile : Profile
    {
        public AuditLogProfile()
        {
            CreateMap<AuditLog, AuditLogDto>();
            
            // 添加 PageList 映射配置
            CreateMap<PageList<AuditLog>, PageList<AuditLogDto>>();
        }
    }
}