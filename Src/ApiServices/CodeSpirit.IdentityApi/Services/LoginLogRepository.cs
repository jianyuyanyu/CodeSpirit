using CodeSpirit.IdentityApi.Data;
using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.Shared.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CodeSpirit.IdentityApi.Services
{
    /// <summary>
    /// 登录日志仓储实现
    /// </summary>
    public class LoginLogRepository : Repository<LoginLog>, ILoginLogRepository
    {
        public LoginLogRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
} 