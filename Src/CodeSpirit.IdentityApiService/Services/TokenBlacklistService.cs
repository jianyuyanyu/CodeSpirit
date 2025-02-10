using Microsoft.Extensions.Caching.Distributed;

namespace CodeSpirit.IdentityApi.Services
{
    public class TokenBlacklistService : ITokenBlacklistService
    {
        private readonly IDistributedCache _cache;
        private const string KeyPrefix = "BlacklistedToken:";

        public TokenBlacklistService(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task AddToBlacklistAsync(string token, TimeSpan duration)
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = duration
            };

            await _cache.SetStringAsync($"{KeyPrefix}{token}", "blacklisted", options);
        }

        public async Task<bool> IsBlacklistedAsync(string token)
        {
            var value = await _cache.GetStringAsync($"{KeyPrefix}{token}");
            return value != null;
        }
    }
} 