using CodeSpirit.IdentityApi.Data;
using CodeSpirit.IdentityApi.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace CodeSpirit.IdentityApi.Repositories
{
    public class LoginLogRepository : Repository<LoginLog>, ILoginLogRepository
    {
        public LoginLogRepository(ApplicationDbContext context) : base(context)
        {
        }

        /// <summary>
        /// 添加登录日志记录。
        /// </summary>
        /// <param name="loginLog">要记录的登录日志对象。</param>
        public async Task AddLoginLogAsync(LoginLog loginLog)
        {
            _context.LoginLogs.Add(loginLog);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// 获取指定用户的登录日志列表。
        /// </summary>
        /// <param name="userId">用户的唯一标识。</param>
        /// <param name="take">每次查询返回的记录数量。</param>
        /// <returns>登录日志列表。</returns>
        public async Task<List<LoginLog>> GetLoginLogsByUserIdAsync(string userId, int take = 10)
        {
            return await _context.LoginLogs
                .Where(log => log.UserId == userId)
                .OrderByDescending(log => log.LoginTime)
                .Take(take)
                .ToListAsync();
        }

        /// <summary>
        /// 获取所有登录日志，按时间降序排序。
        /// </summary>
        /// <param name="take">每次查询返回的记录数量。</param>
        /// <returns>登录日志列表。</returns>
        public async Task<List<LoginLog>> GetAllLoginLogsAsync(int take = 10)
        {
            return await _context.LoginLogs
                .OrderByDescending(log => log.LoginTime)
                .Take(take)
                .ToListAsync();
        }
    }
}
