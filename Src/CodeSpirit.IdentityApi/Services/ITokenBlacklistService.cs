using Microsoft.Extensions.Caching.Distributed;

namespace CodeSpirit.IdentityApi.Services
{
    public interface ITokenBlacklistService
    {
        Task AddToBlacklistAsync(string token, TimeSpan duration);
        Task<bool> IsBlacklistedAsync(string token);
    }
} 