using CodeSpirit.Messaging.Models;
using Microsoft.EntityFrameworkCore;

namespace CodeSpirit.Messaging.Data;

/// <summary>
/// 消息模块数据库上下文
/// </summary>
public class MessagingDbContext : DbContext
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="options">数据库上下文选项</param>
    public MessagingDbContext(DbContextOptions<MessagingDbContext> options) : base(options)
    {
    }
    
    /// <summary>
    /// 消息集合
    /// </summary>
    public DbSet<Message> Messages { get; set; }
    
    /// <summary>
    /// 对话集合
    /// </summary>
    public DbSet<Conversation> Conversations { get; set; }
    
    /// <summary>
    /// 对话参与者集合
    /// </summary>
    public DbSet<ConversationParticipant> ConversationParticipants { get; set; }
    
    /// <summary>
    /// 模型创建配置
    /// </summary>
    /// <param name="modelBuilder">模型构建器</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // 配置消息实体
        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.SenderId).HasMaxLength(100);
            entity.Property(e => e.SenderName).HasMaxLength(100);
            entity.Property(e => e.RecipientId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
        });
        
        // 配置对话实体
        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.LastActivityAt).HasDefaultValueSql("GETDATE()");
            
            // 定义对话与参与者的关系
            entity.HasMany(e => e.Participants)
                .WithOne()
                .HasForeignKey("ConversationId")
                .OnDelete(DeleteBehavior.Cascade);
                
            // 定义对话与消息的关系
            entity.HasMany(e => e.Messages)
                .WithOne()
                .HasForeignKey("ConversationId")
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        // 配置对话参与者实体
        modelBuilder.Entity<ConversationParticipant>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.ConversationId });
            entity.Property(e => e.UserId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.UserName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.JoinedAt).HasDefaultValueSql("GETDATE()");
        });
    }
} 