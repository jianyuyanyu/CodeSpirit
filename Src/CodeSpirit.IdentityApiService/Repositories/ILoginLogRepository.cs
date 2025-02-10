using CodeSpirit.IdentityApi.Data.Models;

namespace CodeSpirit.IdentityApi.Repositories
{
    public interface ILoginLogRepository
    {
        Task<(List<LoginLog> Items, int Total)> GetPagedLoginLogsAsync(
            string keywords,
            string userName,
            bool? isSuccess,
            int page,
            int perPage);
            
        Task<LoginLog> GetByIdAsync(int id);
        Task AddLoginLogAsync(LoginLog loginLog);
        Task<List<LoginLog>> GetLoginLogsByUserIdAsync(string userId, int take = 10);
        Task<List<LoginLog>> GetAllLoginLogsAsync(int take = 10);
    }
}