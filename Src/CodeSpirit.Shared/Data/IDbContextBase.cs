using Microsoft.EntityFrameworkCore;

namespace CodeSpirit.Shared.Data
{
    public interface IDbContextBase<TDbContext> where TDbContext : DbContext
    {
    }
}