using CodeSpirit.IdentityApi.Controllers.Dtos;
using CodeSpirit.IdentityApi.Data.Models;
using Microsoft.AspNetCore.Identity;

namespace CodeSpirit.IdentityApi.Repositories
{
    public interface IUserRepository : IRepository<ApplicationUser>
    {
        Task<ListData<UserDto>> GetUsersAsync(UserQueryDto queryDto);
        Task<UserDto> GetUserByIdAsync(string id);
        Task<(IdentityResult Result, string UserId)> CreateUserAsync(CreateUserDto createUserDto);
        Task<IdentityResult> UpdateUserAsync(string id, UpdateUserDto updateUserDto);
        Task<IdentityResult> DeleteUserAsync(string id);
        Task<IdentityResult> AssignRolesAsync(string id, List<string> roles);
        Task<IdentityResult> RemoveRolesAsync(string id, List<string> roles);
        Task<IdentityResult> SetActiveStatusAsync(string id, bool isActive);
        Task<(bool Success, string NewPassword)> ResetRandomPasswordAsync(string id);
        Task<IdentityResult> UnlockUserAsync(string id);
        Task<List<ApplicationUser>> GetUsersByIdsAsync(List<string> ids);
        Task<List<ActiveUserDto>> GetActiveUsersAsync(DateTimeOffset startDate, DateTimeOffset endDate);
        Task<List<UserGrowthDto>> GetUserGrowthAsync(DateTimeOffset startDate, DateTimeOffset endDate);
    }
}
