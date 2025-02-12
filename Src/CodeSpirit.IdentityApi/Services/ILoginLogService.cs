using CodeSpirit.Core;
using CodeSpirit.IdentityApi.Controllers.Dtos.LoginLogs;

namespace CodeSpirit.IdentityApi.Services
{
    public interface ILoginLogService
    {
        Task<ListData<LoginLogDto>> GetPagedLoginLogsAsync(LoginLogsQueryDto queryDto);
        Task<LoginLogDto> GetLoginLogByIdAsync(int id);
    }
}