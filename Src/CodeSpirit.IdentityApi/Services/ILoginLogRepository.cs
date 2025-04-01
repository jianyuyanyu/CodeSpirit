using CodeSpirit.Core.DependencyInjection;
using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.Shared.Repositories;

namespace CodeSpirit.IdentityApi.Services
{
    /// <summary>
    /// 登录日志仓储接口
    /// </summary>
    public interface ILoginLogRepository : IRepository<LoginLog>, IScopedDependency
    {
    }
} 