using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json.Linq;

namespace CodeSpirit.Amis.Helpers
{
    public class CachingHelper
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMemoryCache _cache;

        public CachingHelper(IHttpContextAccessor httpContextAccessor, IMemoryCache cache)
        {
            _httpContextAccessor = httpContextAccessor;
            _cache = cache;
        }

        public string GenerateCacheKey(string controllerName)
        {
            var user = _httpContextAccessor.HttpContext?.User;
            var permissionsHash = GetUserPermissionsHash(user);
            return $"AmisJson_{controllerName.ToLower()}_{permissionsHash.GetHashCode()}";
        }

        private string GetUserPermissionsHash(System.Security.Claims.ClaimsPrincipal user)
        {
            var userPermissions = user?.Claims
                .Where(c => c.Type == "Permission")
                .Select(c => c.Value)
                .OrderBy(p => p)
                .ToList() ?? new List<string>();

            return string.Join(",", userPermissions);
        }

        public bool TryGetValue(string key, out JObject value)
        {
            return _cache.TryGetValue(key, out value);
        }

        public void Set(string key, JObject value, MemoryCacheEntryOptions options)
        {
            _cache.Set(key, value, options);
        }
    }
}

