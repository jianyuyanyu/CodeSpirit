namespace CodeSpirit.OrderApi.Data
{
    //public class OrderDbContext : DbContextBase<OrderDbContext>
    //{
    //    public DbSet<Order> Orders { get; set; }

    //    public OrderDbContext(DbContextOptions<OrderDbContext> options, IServiceProvider serviceProvider)
    //        : base(options, serviceProvider)
    //    {
    //    }

    //    protected override void OnModelCreating(ModelBuilder builder)
    //    {
    //        base.OnModelCreating(builder);

    //        #region Order
    //        builder.Entity<Order>(b =>
    //        {
    //            b.ToTable(nameof(Order));

    //            // 设置索引
    //            b.HasIndex(o => o.OrderNumber).IsUnique();
    //            b.HasIndex(o => o.UserId);
    //            b.HasIndex(o => o.Status);
    //            b.HasIndex(o => o.OrderTime);

    //            // 设置字段类型和约束
    //            b.Property(o => o.OrderNumber).HasMaxLength(50).IsRequired();
    //            b.Property(o => o.TotalAmount).HasColumnType("decimal(18,2)");
    //            b.Property(o => o.Remarks).HasMaxLength(500);

    //            // 配置软删除和审计字段的默认值
    //            b.Property(o => o.IsDeleted).HasDefaultValue(false);
    //            b.Property(o => o.IsActive).HasDefaultValue(true);
    //            b.Property(o => o.CreationTime).HasDefaultValueSql("GETDATE()");
    //        });
    //        #endregion

    //        ConfigureGlobalFiltersOnModelCreating(builder);
    //    }
    //}
}