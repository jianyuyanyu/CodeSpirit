namespace CodeSpirit.IdentityApi.Services
{
    public interface ITokenBlacklistService : IScopedDependency
    {
        Task AddToBlacklistAsync(string token, TimeSpan duration);
        Task<bool> IsBlacklistedAsync(string token);
    }
}