using CodeSpirit.IdentityApi.Data.Models;

namespace CodeSpirit.IdentityApi.Repositories
{
    public interface ILoginLogRepository
    {
        Task AddLoginLogAsync(LoginLog loginLog);
        Task<List<LoginLog>> GetAllLoginLogsAsync(int take = 10);
        Task<List<LoginLog>> GetLoginLogsByUserIdAsync(string userId, int take = 10);
    }
}