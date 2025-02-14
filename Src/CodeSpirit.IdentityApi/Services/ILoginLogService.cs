using CodeSpirit.Core;
using CodeSpirit.Core.DependencyInjection;
using CodeSpirit.IdentityApi.Controllers.Dtos.LoginLogs;

namespace CodeSpirit.IdentityApi.Services
{
    public interface ILoginLogService: IScopedDependency
    {
        Task<ListData<LoginLogDto>> GetPagedLoginLogsAsync(LoginLogsQueryDto queryDto);
        Task<LoginLogDto> GetLoginLogByIdAsync(int id);
    }
}