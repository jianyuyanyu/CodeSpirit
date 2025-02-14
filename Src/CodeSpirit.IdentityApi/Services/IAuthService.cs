// Services/AuthService.cs

using CodeSpirit.Core.DependencyInjection;

namespace CodeSpirit.IdentityApi.Services
{
    public interface IAuthService: IScopedDependency
    {
        Task<(bool Success, string Message, string Token, UserDto UserInfo)> ImpersonateLoginAsync(string userName);
        Task<(bool Success, string Message, string Token, UserDto UserInfo)> LoginAsync(string userName, string password);
    }
}