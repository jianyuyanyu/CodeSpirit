using CodeSpirit.ConfigCenter.Models;
using Microsoft.EntityFrameworkCore;

namespace CodeSpirit.ConfigCenter.Tests.TestHelpers;

/// <summary>
/// 测试专用的 DbContext
/// </summary>
public class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
    {
    }

    public DbSet<ConfigItem> ConfigItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<ConfigItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AppId).IsRequired();
            entity.Property(e => e.Key).IsRequired();
            entity.Property(e => e.Value).IsRequired();
        });
    }
}
