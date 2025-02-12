using CodeSpirit.IdentityApi.Data.Models;
using Microsoft.AspNetCore.Identity;

namespace CodeSpirit.IdentityApi.Repositories
{
    /// <summary>
    /// 用户仓储接口
    /// </summary>
    public interface IUserRepository : IRepository<ApplicationUser>
    {
        /// <summary>
        /// 创建用户
        /// </summary>
        /// <param name="user">用户实体</param>
        /// <returns>创建结果</returns>
        Task<IdentityResult> CreateUserAsync(ApplicationUser user);

        /// <summary>
        /// 删除用户
        /// </summary>
        /// <param name="user">用户实体</param>
        /// <returns>删除结果</returns>
        Task<IdentityResult> DeleteUserAsync(ApplicationUser user);

        /// <summary>
        /// 根据ID获取用户
        /// </summary>
        /// <param name="id">用户ID</param>
        /// <returns>用户实体</returns>
        Task<ApplicationUser> GetUserByIdAsync(long id);

        /// <summary>
        /// 获取用户查询对象
        /// </summary>
        /// <returns>用户查询对象</returns>
        IQueryable<ApplicationUser> GetUsersQueryable();

        /// <summary>
        /// 更新用户
        /// </summary>
        /// <param name="user">用户实体</param>
        /// <returns>更新结果</returns>
        Task<IdentityResult> UpdateUserAsync(ApplicationUser user);
    }
}
