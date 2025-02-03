using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.IdentityApi.Data;
using CodeSpirit.IdentityApi.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
public partial class UserRepository : Repository<ApplicationUser>, IUserRepository
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UserRepository(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
        : base(context)
    {
        _userManager = userManager;
    }

    public IQueryable<ApplicationUser> GetUsersQueryable()
    {
        // 返回一个可查询的用户集合，而不是直接查询并返回列表
        return _userManager.Users.Include(u => u.UserRoles).ThenInclude(ur => ur.Role);
    }

    public async Task<ApplicationUser> GetUserByIdAsync(string id)
    {
        return await _userManager.Users.Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<IdentityResult> CreateUserAsync(ApplicationUser user)
    {
        return await _userManager.CreateAsync(user);
    }

    public async Task<IdentityResult> UpdateUserAsync(ApplicationUser user)
    {
        return await _userManager.UpdateAsync(user);
    }

    public async Task<IdentityResult> DeleteUserAsync(ApplicationUser user)
    {
        _context.SoftDelete(user);
        await _context.SaveChangesAsync();
        return IdentityResult.Success;
    }
}
