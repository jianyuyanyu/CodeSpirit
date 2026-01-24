# 多数据库迁移示例

## 示例 1：添加新实体 Product

### 步骤 1：创建实体和配置

```csharp
// Data/Models/Product.cs
public class Product : AuditableEntityBase<long>, IMultiTenant
{
    public string TenantId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

// Data/Configurations/ProductConfiguration.cs
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever(); // 雪花ID
        builder.Property(x => x.TenantId).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Price).HasColumnType("decimal(18,2)");
    }
}
```

### 步骤 2：创建迁移

```powershell
# MySQL 迁移
dotnet ef migrations add AddProduct `
  --context MySqlMallDbContext `
  --output-dir Data/Migrations/MySql

# SQL Server 迁移
dotnet ef migrations add AddProduct `
  --context SqlServerMallDbContext `
  --output-dir Data/Migrations/SqlServer
```

### 步骤 3：应用迁移

```powershell
dotnet ef database update --context MySqlMallDbContext
dotnet ef database update --context SqlServerMallDbContext
```

---

## 示例 2：添加字段到现有实体

### 步骤 1：修改实体

```csharp
public class Product : AuditableEntityBase<long>, IMultiTenant
{
    // ... 现有字段 ...
    
    // 新增字段
    public string? Description { get; set; }
    public int Stock { get; set; }
}
```

### 步骤 2：更新配置

```csharp
builder.Property(x => x.Description).HasMaxLength(500);
builder.Property(x => x.Stock).HasDefaultValue(0);
```

### 步骤 3：创建迁移

```powershell
dotnet ef migrations add AddDescriptionAndStockToProduct `
  --context MySqlMallDbContext `
  --output-dir Data/Migrations/MySql

dotnet ef migrations add AddDescriptionAndStockToProduct `
  --context SqlServerMallDbContext `
  --output-dir Data/Migrations/SqlServer
```

---

## 示例 3：添加索引

### 步骤 1：在配置中添加索引

```csharp
builder.HasIndex(x => new { x.TenantId, x.Name })
    .IsUnique()
    .HasDatabaseName("IX_Products_TenantId_Name");
```

### 步骤 2：创建迁移

```powershell
dotnet ef migrations add AddIndexToProducts `
  --context MySqlMallDbContext `
  --output-dir Data/Migrations/MySql

dotnet ef migrations add AddIndexToProducts `
  --context SqlServerMallDbContext `
  --output-dir Data/Migrations/SqlServer
```

---

## 常见问题

### Q: 迁移文件应该提交到版本控制吗？

A: 是的，迁移文件应该提交到版本控制，确保团队成员数据库结构一致。

### Q: 可以删除迁移文件吗？

A: 只有在迁移未应用到生产环境时才能删除。已应用的迁移不应删除。

### Q: MySQL 和 SQL Server 迁移必须同时创建吗？

A: 是的，为了保持数据库结构一致，应该同时为两个数据库创建迁移。
