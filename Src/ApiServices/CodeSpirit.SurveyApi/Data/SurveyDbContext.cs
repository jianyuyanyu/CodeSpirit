using CodeSpirit.SurveyApi.Models;
using CodeSpirit.SurveyApi.Models.Enums;
using CodeSpirit.Shared.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace CodeSpirit.SurveyApi.Data;

/// <summary>
/// 问卷系统数据库上下文
/// </summary>
public class SurveyDbContext : MultiTenantDbContext
{
    /// <summary>
    /// 初始化问卷数据库上下文
    /// </summary>
    /// <param name="options">数据库选项</param>
    /// <param name="serviceProvider">服务提供者</param>
    /// <param name="currentUser">当前用户服务</param>
    /// <param name="httpContextAccessor">HTTP上下文访问器</param>
    public SurveyDbContext(
        DbContextOptions<SurveyDbContext> options,
        IServiceProvider serviceProvider,
        ICurrentUser currentUser,
        IHttpContextAccessor httpContextAccessor) 
        : base(options, serviceProvider, currentUser, httpContextAccessor)
    {
    }

    #region DbSets

    /// <summary>
    /// 问卷集合
    /// </summary>
    public DbSet<Survey> Surveys { get; set; }

    /// <summary>
    /// 题目集合
    /// </summary>
    public DbSet<Question> Questions { get; set; }

    /// <summary>
    /// 题目选项集合
    /// </summary>
    public DbSet<QuestionOption> QuestionOptions { get; set; }

    /// <summary>
    /// 问卷回答集合
    /// </summary>
    public DbSet<SurveyResponse> SurveyResponses { get; set; }

    /// <summary>
    /// 回答详情集合
    /// </summary>
    public DbSet<ResponseAnswer> ResponseAnswers { get; set; }

    /// <summary>
    /// 问卷草稿集合
    /// </summary>
    public DbSet<SurveyDraft> SurveyDrafts { get; set; }

    #endregion

    /// <summary>
    /// 配置实体模型
    /// </summary>
    /// <param name="modelBuilder">模型构建器</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 配置问卷实体
        ConfigureSurvey(modelBuilder);

        // 配置题目实体
        ConfigureQuestion(modelBuilder);

        // 配置题目选项实体
        ConfigureQuestionOption(modelBuilder);

        // 配置问卷回答实体
        ConfigureSurveyResponse(modelBuilder);

        // 配置回答详情实体
        ConfigureResponseAnswer(modelBuilder);

        // 配置问卷草稿实体
        ConfigureSurveyDraft(modelBuilder);
    }

    /// <summary>
    /// 配置问卷实体
    /// </summary>
    /// <param name="modelBuilder">模型构建器</param>
    private static void ConfigureSurvey(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Survey>();

        entity.ToTable("Surveys");

        entity.HasKey(s => s.Id);

        entity.Property(s => s.TenantId).IsRequired().HasMaxLength(50);
        entity.Property(s => s.Title).IsRequired().HasMaxLength(200);
        entity.Property(s => s.Description).HasMaxLength(2000);
        entity.Property(s => s.Status).IsRequired().HasConversion<int>();
        entity.Property(s => s.AccessType).IsRequired().HasConversion<int>();
        entity.Property(s => s.Settings).HasMaxLength(4000);
        entity.Property(s => s.LLMPrompt).HasMaxLength(4000);

        // 索引
        entity.HasIndex(s => s.TenantId).HasDatabaseName("IX_Surveys_TenantId");
        entity.HasIndex(s => s.Status).HasDatabaseName("IX_Surveys_Status");
        entity.HasIndex(s => s.CreatedBy).HasDatabaseName("IX_Surveys_CreatedBy");
        entity.HasIndex(s => new { s.TenantId, s.Status }).HasDatabaseName("IX_Surveys_TenantId_Status");

        // 关系配置
        entity.HasMany(s => s.Questions)
            .WithOne(q => q.Survey)
            .HasForeignKey(q => q.SurveyId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasMany(s => s.Responses)
            .WithOne(r => r.Survey)
            .HasForeignKey(r => r.SurveyId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasMany(s => s.Drafts)
            .WithOne(d => d.Survey)
            .HasForeignKey(d => d.SurveyId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    /// <summary>
    /// 配置题目实体
    /// </summary>
    /// <param name="modelBuilder">模型构建器</param>
    private static void ConfigureQuestion(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Question>();

        entity.ToTable("Questions");

        entity.HasKey(q => q.Id);

        entity.Property(q => q.SurveyId).IsRequired();
        entity.Property(q => q.Title).IsRequired().HasMaxLength(500);
        entity.Property(q => q.Description).HasMaxLength(2000);
        entity.Property(q => q.Type).IsRequired().HasConversion<int>();
        entity.Property(q => q.OrderIndex).IsRequired();
        entity.Property(q => q.Validation).HasMaxLength(2000);
        entity.Property(q => q.Settings).HasMaxLength(2000);

        // 索引
        entity.HasIndex(q => q.SurveyId).HasDatabaseName("IX_Questions_SurveyId");
        entity.HasIndex(q => new { q.SurveyId, q.OrderIndex }).HasDatabaseName("IX_Questions_SurveyId_OrderIndex");

        // 关系配置
        entity.HasMany(q => q.Options)
            .WithOne(o => o.Question)
            .HasForeignKey(o => o.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasMany(q => q.Answers)
            .WithOne(a => a.Question)
            .HasForeignKey(a => a.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    /// <summary>
    /// 配置题目选项实体
    /// </summary>
    /// <param name="modelBuilder">模型构建器</param>
    private static void ConfigureQuestionOption(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<QuestionOption>();

        entity.ToTable("QuestionOptions");

        entity.HasKey(o => o.Id);

        entity.Property(o => o.QuestionId).IsRequired();
        entity.Property(o => o.Text).IsRequired().HasMaxLength(500);
        entity.Property(o => o.Value).HasMaxLength(200);
        entity.Property(o => o.OrderIndex).IsRequired();

        // 索引
        entity.HasIndex(o => o.QuestionId).HasDatabaseName("IX_QuestionOptions_QuestionId");
        entity.HasIndex(o => new { o.QuestionId, o.OrderIndex }).HasDatabaseName("IX_QuestionOptions_QuestionId_OrderIndex");
    }

    /// <summary>
    /// 配置问卷回答实体
    /// </summary>
    /// <param name="modelBuilder">模型构建器</param>
    private static void ConfigureSurveyResponse(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<SurveyResponse>();

        entity.ToTable("SurveyResponses");

        entity.HasKey(r => r.Id);

        entity.Property(r => r.SurveyId).IsRequired();
        entity.Property(r => r.RespondentId).HasMaxLength(50);
        entity.Property(r => r.SessionId).IsRequired().HasMaxLength(50);
        entity.Property(r => r.Status).IsRequired().HasConversion<int>();
        entity.Property(r => r.IpAddress).HasMaxLength(50);
        entity.Property(r => r.UserAgent).HasMaxLength(500);
        entity.Property(r => r.DeviceFingerprint).HasMaxLength(100);
        entity.Property(r => r.Metadata).HasMaxLength(2000);

        // 索引
        entity.HasIndex(r => r.SurveyId).HasDatabaseName("IX_SurveyResponses_SurveyId");
        entity.HasIndex(r => r.RespondentId).HasDatabaseName("IX_SurveyResponses_RespondentId");
        entity.HasIndex(r => r.SessionId).HasDatabaseName("IX_SurveyResponses_SessionId");
        entity.HasIndex(r => r.IpAddress).HasDatabaseName("IX_SurveyResponses_IpAddress");
        entity.HasIndex(r => new { r.SurveyId, r.RespondentId }).HasDatabaseName("IX_SurveyResponses_SurveyId_RespondentId");
        entity.HasIndex(r => new { r.SurveyId, r.SessionId }).HasDatabaseName("IX_SurveyResponses_SurveyId_SessionId");

        // 关系配置
        entity.HasMany(r => r.Answers)
            .WithOne(a => a.Response)
            .HasForeignKey(a => a.ResponseId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    /// <summary>
    /// 配置回答详情实体
    /// </summary>
    /// <param name="modelBuilder">模型构建器</param>
    private static void ConfigureResponseAnswer(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<ResponseAnswer>();

        entity.ToTable("ResponseAnswers");

        entity.HasKey(a => a.Id);

        entity.Property(a => a.ResponseId).IsRequired();
        entity.Property(a => a.QuestionId).IsRequired();
        entity.Property(a => a.AnswerText).HasMaxLength(4000);
        entity.Property(a => a.AnswerValue).HasMaxLength(2000);

        // 索引
        entity.HasIndex(a => a.ResponseId).HasDatabaseName("IX_ResponseAnswers_ResponseId");
        entity.HasIndex(a => a.QuestionId).HasDatabaseName("IX_ResponseAnswers_QuestionId");
        entity.HasIndex(a => new { a.ResponseId, a.QuestionId }).HasDatabaseName("IX_ResponseAnswers_ResponseId_QuestionId");
    }

    /// <summary>
    /// 配置问卷草稿实体
    /// </summary>
    /// <param name="modelBuilder">模型构建器</param>
    private static void ConfigureSurveyDraft(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<SurveyDraft>();

        entity.ToTable("SurveyDrafts");

        entity.HasKey(d => d.Id);

        entity.Property(d => d.SurveyId).IsRequired();
        entity.Property(d => d.SessionId).IsRequired().HasMaxLength(50);
        entity.Property(d => d.UserId).HasMaxLength(50);
        entity.Property(d => d.DraftData).IsRequired().HasMaxLength(8000);
        entity.Property(d => d.IpAddress).HasMaxLength(50);
        entity.Property(d => d.UserAgent).HasMaxLength(500);

        // 索引
        entity.HasIndex(d => d.SurveyId).HasDatabaseName("IX_SurveyDrafts_SurveyId");
        entity.HasIndex(d => new { d.SurveyId, d.SessionId }).HasDatabaseName("IX_SurveyDrafts_SurveyId_SessionId");
        entity.HasIndex(d => new { d.SurveyId, d.UserId }).HasDatabaseName("IX_SurveyDrafts_SurveyId_UserId");
        entity.HasIndex(d => d.ExpiresAt).HasDatabaseName("IX_SurveyDrafts_ExpiresAt");
    }

    /// <summary>
    /// 初始化数据库数据
    /// </summary>
    /// <returns>异步任务</returns>
    public async Task InitializeDatabaseAsync()
    {
        // 这里可以添加初始化数据的逻辑
        // 例如创建默认的问卷模板等
        await Task.CompletedTask;
    }
}
