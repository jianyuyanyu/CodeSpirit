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
        public async Task<List<LoginLog>> GetLoginLogsByUserIdAsync(long userId, int take = 10)
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

        public async Task<(List<LoginLog> Items, int Total)> GetPagedLoginLogsAsync(
            string keywords,
            string userName,
            bool? isSuccess,
            int page,
            int perPage)
        {
            IQueryable<LoginLog> query = _context.LoginLogs
                .Include(l => l.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keywords))
            {
                string searchLower = keywords.ToLower();
                query = query.Where(u =>
                    u.UserName.ToLower().Contains(searchLower) ||
                    u.User.PhoneNumber.ToLower().Contains(searchLower) ||
                    u.IPAddress.ToLower().Contains(searchLower));
            }

            if (!string.IsNullOrEmpty(userName))
            {
                query = query.Where(l => l.UserName.Contains(userName));
            }

            if (isSuccess.HasValue)
            {
                query = query.Where(l => l.IsSuccess == isSuccess.Value);
            }

            int totalRecords = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalRecords / (double)perPage);

            List<LoginLog> items = await query
                .OrderByDescending(l => l.LoginTime)
                .Skip((page - 1) * perPage)
                .Take(perPage)
                .ToListAsync();

            return (items, totalPages);
        }

        public async Task<LoginLog> GetByIdAsync(int id)
        {
            return await _context.LoginLogs
                .Include(l => l.User)
                .FirstOrDefaultAsync(l => l.Id == id);
        }

    }
}
