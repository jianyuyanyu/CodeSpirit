using CodeSpirit.Core;
using CodeSpirit.OrderApi.Data.Models;
using CodeSpirit.Shared.Data;
using Microsoft.EntityFrameworkCore;

namespace CodeSpirit.OrderApi.Data
{
    public class OrderDbContext : AuditableDbContext
    {
        public OrderDbContext(
            DbContextOptions<OrderDbContext> options,
            IServiceProvider serviceProvider,
            ICurrentUser currentUser)
            : base(options, serviceProvider, currentUser)
        {
        }

        public DbSet<Order> Orders { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Order>(entity =>
            {
                entity.ToTable("Orders");
                
                entity.HasIndex(e => e.OrderNumber)
                    .IsUnique();

                entity.Property(e => e.TotalAmount)
                    .HasColumnType("decimal(18,2)");

                entity.Property(e => e.IsActive)
                    .HasDefaultValue(true);

                entity.Property(e => e.IsDeleted)
                    .HasDefaultValue(false);

                entity.Property(e => e.CreationTime)
                    .HasDefaultValueSql("GETDATE()");
            });
        }
    }
}