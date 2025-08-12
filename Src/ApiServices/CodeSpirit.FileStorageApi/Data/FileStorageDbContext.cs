using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using CodeSpirit.FileStorageApi.Entities;
using CodeSpirit.Shared.Data;
using CodeSpirit.Core;

namespace CodeSpirit.FileStorageApi.Data;

/// <summary>
/// 文件存储数据库上下文
/// </summary>
public class FileStorageDbContext : MultiTenantDbContext
{
    /// <summary>
    /// 文件
    /// </summary>
    public DbSet<FileEntity> Files { get; set; }
    
    /// <summary>
    /// 文件引用
    /// </summary>
    public DbSet<FileReferenceEntity> FileReferences { get; set; }
    
    /// <summary>
    /// 图片元数据
    /// </summary>
    public DbSet<ImageMetadataEntity> ImageMetadata { get; set; }
    
    /// <summary>
    /// 缩略图
    /// </summary>
    public DbSet<ThumbnailEntity> Thumbnails { get; set; }
    
    /// <summary>
    /// 视频元数据
    /// </summary>
    public DbSet<VideoMetadataEntity> VideoMetadata { get; set; }
    
    /// <summary>
    /// 构造函数
    /// </summary>
    public FileStorageDbContext(
        DbContextOptions<FileStorageDbContext> options,
        IServiceProvider serviceProvider,
        ICurrentUser currentUser,
        IHttpContextAccessor httpContextAccessor) : base(options, serviceProvider, currentUser, httpContextAccessor)
    {
    }
    
    /// <summary>
    /// 配置实体模型
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // 配置文件
        ConfigureFile(modelBuilder);
        
        // 配置文件引用
        ConfigureFileReference(modelBuilder);
        
        // 配置图片元数据
        ConfigureImageMetadata(modelBuilder);
        
        // 配置缩略图
        ConfigureThumbnail(modelBuilder);
        
        // 配置视频元数据
        ConfigureVideoMetadata(modelBuilder);
    }
    
    /// <summary>
    /// 配置文件实体
    /// </summary>
    private static void ConfigureFile(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<FileEntity>();
        
        // 配置主键生成策略 - 使用自定义ID生成器而非数据库自增长
        entity.Property(e => e.Id).ValueGeneratedNever();
        
        // 配置可空字段
        entity.Property(e => e.Description).IsRequired(false);
        entity.Property(e => e.FilePath).IsRequired(false);
        entity.Property(e => e.Extension).IsRequired(false);
        entity.Property(e => e.DownloadUrl).IsRequired(false);
        entity.Property(e => e.ETag).IsRequired(false);
        entity.Property(e => e.Tags).IsRequired(false);
        entity.Property(e => e.Properties).IsRequired(false);
        
        // 创建索引
        entity.HasIndex(e => new { e.TenantId, e.BucketName })
              .HasDatabaseName("IX_Files_TenantId_BucketName");
        
        entity.HasIndex(e => e.FileHash)
              .HasDatabaseName("IX_Files_FileHash");
        
        entity.HasIndex(e => new { e.TenantId, e.StorageFileName })
              .IsUnique()
              .HasDatabaseName("IX_Files_TenantId_StorageFileName");
        
        entity.HasIndex(e => e.ExpirationTime)
              .HasDatabaseName("IX_Files_ExpirationTime");
        
        entity.HasIndex(e => e.Status)
              .HasDatabaseName("IX_Files_Status");
        
        entity.HasIndex(e => e.Category)
              .HasDatabaseName("IX_Files_Category");
        
        entity.HasIndex(e => new { e.TenantId, e.Category })
              .HasDatabaseName("IX_Files_TenantId_Category");
        
        entity.HasIndex(e => e.CreatedAt)
              .HasDatabaseName("IX_Files_CreatedAt");
        
        entity.HasIndex(e => new { e.TenantId, e.OriginalFileName })
              .HasDatabaseName("IX_Files_TenantId_OriginalFileName");
    }
    
    /// <summary>
    /// 配置文件引用实体
    /// </summary>
    private static void ConfigureFileReference(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<FileReferenceEntity>();
        
        // 配置主键生成策略 - 使用自定义ID生成器而非数据库自增长
        entity.Property(e => e.Id).ValueGeneratedNever();
        
        // 配置可空字段
        entity.Property(e => e.Remarks).IsRequired(false);
        entity.Property(e => e.Properties).IsRequired(false);
        
        // 配置外键关系
        entity.HasOne(e => e.File)
              .WithMany(e => e.References)
              .HasForeignKey(e => e.FileId)
              .OnDelete(DeleteBehavior.Cascade);
        
        // 创建复合索引
        entity.HasIndex(e => new { e.SourceService, e.SourceEntityType, e.SourceEntityId })
              .HasDatabaseName("IX_FileReferences_Source");
        
        entity.HasIndex(e => new { e.TenantId, e.FileId })
              .HasDatabaseName("IX_FileReferences_TenantId_FileId");
        
        entity.HasIndex(e => e.Status)
              .HasDatabaseName("IX_FileReferences_Status");
        
        entity.HasIndex(e => e.ExpirationTime)
              .HasDatabaseName("IX_FileReferences_ExpirationTime");
        
        entity.HasIndex(e => new { e.TenantId, e.Status })
              .HasDatabaseName("IX_FileReferences_TenantId_Status");
        
        entity.HasIndex(e => new { e.TenantId, e.IsTemporary, e.Status })
              .HasDatabaseName("IX_FileReferences_TenantId_IsTemporary_Status");
    }
    
    /// <summary>
    /// 配置图片元数据实体
    /// </summary>
    private static void ConfigureImageMetadata(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<ImageMetadataEntity>();
        
        // 配置主键生成策略 - 使用自定义ID生成器而非数据库自增长
        entity.Property(e => e.Id).ValueGeneratedNever();
        
        // 配置一对一关系
        entity.HasOne(e => e.File)
              .WithOne(e => e.ImageMetadata)
              .HasForeignKey<ImageMetadataEntity>(e => e.FileId)
              .OnDelete(DeleteBehavior.Cascade);
        
        // 创建索引
        entity.HasIndex(e => new { e.Width, e.Height })
              .HasDatabaseName("IX_ImageMetadata_Dimensions");
        
        entity.HasIndex(e => e.Format)
              .HasDatabaseName("IX_ImageMetadata_Format");
        
        entity.HasIndex(e => e.DateTaken)
              .HasDatabaseName("IX_ImageMetadata_DateTaken");
        
        entity.HasIndex(e => new { e.Latitude, e.Longitude })
              .HasDatabaseName("IX_ImageMetadata_GpsLocation");
    }
    
    /// <summary>
    /// 配置缩略图实体
    /// </summary>
    private static void ConfigureThumbnail(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<ThumbnailEntity>();
        
        // 配置主键生成策略 - 使用自定义ID生成器而非数据库自增长
        entity.Property(e => e.Id).ValueGeneratedNever();
        
        // 配置外键关系
        entity.HasOne(e => e.ImageMetadata)
              .WithMany(e => e.Thumbnails)
              .HasForeignKey(e => e.ImageMetadataId)
              .OnDelete(DeleteBehavior.Cascade);
        
        entity.HasOne(e => e.ThumbnailFile)
              .WithMany()
              .HasForeignKey(e => e.ThumbnailFileId)
              .OnDelete(DeleteBehavior.Restrict);
        
        // 创建复合索引
        entity.HasIndex(e => new { e.ImageMetadataId, e.SizeKey })
              .IsUnique()
              .HasDatabaseName("IX_Thumbnails_ImageMetadataId_SizeKey");
        
        entity.HasIndex(e => e.SizeKey)
              .HasDatabaseName("IX_Thumbnails_SizeKey");
    }
    
    /// <summary>
    /// 配置视频元数据实体
    /// </summary>
    private static void ConfigureVideoMetadata(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<VideoMetadataEntity>();
        
        // 配置主键生成策略 - 使用自定义ID生成器而非数据库自增长
        entity.Property(e => e.Id).ValueGeneratedNever();
        
        // 配置一对一关系
        entity.HasOne(e => e.File)
              .WithOne(e => e.VideoMetadata)
              .HasForeignKey<VideoMetadataEntity>(e => e.FileId)
              .OnDelete(DeleteBehavior.Cascade);
        
        // 创建索引
        entity.HasIndex(e => e.Duration)
              .HasDatabaseName("IX_VideoMetadata_Duration");
        
        entity.HasIndex(e => new { e.Width, e.Height })
              .HasDatabaseName("IX_VideoMetadata_Dimensions");
        
        entity.HasIndex(e => e.VideoCodec)
              .HasDatabaseName("IX_VideoMetadata_VideoCodec");
        
        entity.HasIndex(e => e.CreatedTime)
              .HasDatabaseName("IX_VideoMetadata_CreatedTime");
    }
}
