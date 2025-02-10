using CodeSpirit.Core;
using CodeSpirit.IdentityApi.Controllers.Dtos;
using CodeSpirit.IdentityApi.Data.Models;
using Microsoft.AspNetCore.Identity;

namespace CodeSpirit.IdentityApi.Services
{
    public interface IUserService
    {
        Task<IdentityResult> AssignRolesAsync(string id, List<string> roles);
        Task<(IdentityResult, string)> CreateUserAsync(CreateUserDto createUserDto);
        Task<IdentityResult> DeleteUserAsync(string id);
        Task<List<ActiveUserDto>> GetActiveUsersAsync(DateTimeOffset startDate, DateTimeOffset endDate);
        Task<UserDto> GetUserByIdAsync(string id);
        Task<List<UserGrowthDto>> GetUserGrowthAsync(DateTimeOffset startDate, DateTimeOffset endDate);
        Task<ListData<UserDto>> GetUsersAsync(UserQueryDto queryDto);
        Task<List<ApplicationUser>> GetUsersByIdsAsync(List<string> ids);
        Task QuickSaveUsersAsync(QuickSaveRequestDto request);
        Task<IdentityResult> RemoveRolesAsync(string id, List<string> roles);
        Task<(bool Success, string NewPassword)> ResetRandomPasswordAsync(string id);
        Task<IdentityResult> SetActiveStatusAsync(string id, bool isActive);
        Task<IdentityResult> UnlockUserAsync(string id);
        Task<IdentityResult> UpdateUserAsync(string id, UpdateUserDto updateUserDto);
    }
}