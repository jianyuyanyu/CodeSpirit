using AutoMapper;
using CodeSpirit.IdentityApi.Controllers.Dtos.AuditLog;
using CodeSpirit.IdentityApi.Data.Models;

namespace CodeSpirit.IdentityApi.MappingProfiles
{
    public class AuditLogProfile : Profile
    {
        public AuditLogProfile()
        {
            CreateMap<AuditLog, AuditLogDto>();
        }
    }
}