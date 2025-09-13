using CodeSpirit.Core;
using CodeSpirit.Messaging.Models;
using CodeSpirit.Shared.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CodeSpirit.Messaging.Data;

/// <summary>
/// 消息模块数据库上下文 - 支持多数据库
/// </summary>
public class MessagingDbContext : MultiDatabaseDbContextBase
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="options">数据库上下文选项</param>
    /// <param name="serviceProvider">服务提供者</param>
    /// <param name="currentUser">当前用户服务</param>
    /// <param name="httpContextAccessor">HTTP上下文访问器</param>
    public MessagingDbContext(
        DbContextOptions options,
        IServiceProvider serviceProvider,
        ICurrentUser currentUser,
        IHttpContextAccessor httpContextAccessor) : base(options, serviceProvider, currentUser, httpContextAccessor)
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
    /// 用户消息已读状态集合
    /// </summary>
    public DbSet<UserMessageRead> UserMessageReads { get; set; }
    
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
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            
            // 多租户字段配置
            entity.Property(e => e.TenantId).HasMaxLength(50).IsRequired();
            entity.HasIndex(e => e.TenantId).HasDatabaseName("IX_Messages_TenantId");
        });
        
        // 配置对话实体
        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.LastActivityAt).HasDefaultValueSql("GETUTCDATE()");
            
            // 多租户字段配置
            entity.Property(e => e.TenantId).HasMaxLength(50).IsRequired();
            entity.HasIndex(e => e.TenantId).HasDatabaseName("IX_Conversations_TenantId");
            
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
            entity.Property(e => e.JoinedAt).HasDefaultValueSql("GETUTCDATE()");
            
            // 多租户字段配置
            entity.Property(e => e.TenantId).HasMaxLength(50).IsRequired();
            entity.HasIndex(e => e.TenantId).HasDatabaseName("IX_ConversationParticipants_TenantId");
        });
        
        // 配置用户消息已读状态实体
        modelBuilder.Entity<UserMessageRead>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.MessageId });
            entity.Property(e => e.UserId).HasMaxLength(100).IsRequired();
            
            // 多租户字段配置
            entity.Property(e => e.TenantId).HasMaxLength(50).IsRequired();
            entity.HasIndex(e => e.TenantId).HasDatabaseName("IX_UserMessageReads_TenantId");
            
            entity.HasOne(e => e.Message)
                .WithMany()
                .HasForeignKey(e => e.MessageId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
} 