using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json.Linq;

namespace CodeSpirit.Amis.Helpers
{
    public class CachingHelper
    {
        private readonly IMemoryCache _cache;
        private readonly ICurrentUser currentUser;
        private readonly CultureResolver _cultureResolver;

        public CachingHelper(IMemoryCache cache, ICurrentUser currentUser, CultureResolver cultureResolver)
        {
            _cache = cache;
            this.currentUser = currentUser;
            _cultureResolver = cultureResolver;
        }

        public string GenerateCacheKey(string controllerName)
        {
            string rolesHash = GetUserRolesHash();
            string language = _cultureResolver.GetCurrentLanguage();
            return $"AmisJson_{controllerName.ToLower()}_{language}_{rolesHash.GetHashCode()}";
        }

        private string GetUserRolesHash()
        {
            List<string> userRoles = currentUser.Roles
                .OrderBy(p => p)
                .ToList() ?? [];

            return string.Join(",", userRoles);
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

