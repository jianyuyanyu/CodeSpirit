using CodeSpirit.IdentityApi.Data.Models;
using Microsoft.AspNetCore.Identity;

namespace CodeSpirit.IdentityApi.Repositories
{
    public interface IUserRepository : IRepository<ApplicationUser>
    {
        Task<IdentityResult> CreateUserAsync(ApplicationUser user);
        Task<IdentityResult> DeleteUserAsync(ApplicationUser user);
        Task<ApplicationUser> GetUserByIdAsync(string id);
        IQueryable<ApplicationUser> GetUsersQueryable();
        Task<IdentityResult> UpdateUserAsync(ApplicationUser user);
    }
}
