using CodeSpirit.Core;
using CodeSpirit.Core.DependencyInjection;
using CodeSpirit.IdentityApi.Dtos.LoginLogs;

namespace CodeSpirit.IdentityApi.Services
{
    public interface ILoginLogService: IScopedDependency
    {
        Task<PageList<LoginLogDto>> GetPagedLoginLogsAsync(LoginLogsQueryDto queryDto);
        Task<LoginLogDto> GetLoginLogByIdAsync(int id);
    }
}