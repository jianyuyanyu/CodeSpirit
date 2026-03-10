using CodeSpirit.ExamApi.Data.Models;
using CodeSpirit.ExamApi.Data.Models.Enums;
using CodeSpirit.Shared.Data;
using CodeSpirit.Shared.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using CodeSpirit.Core;
using CodeSpirit.ExamApi.Data.Seeds;
using CodeSpirit.Core.IdGenerator;
using Microsoft.Extensions.DependencyInjection;
using System;
using Microsoft.AspNetCore.Http;

namespace CodeSpirit.ExamApi.Data;

/// <summary>
/// 考试系统数据库上下文 - 支持多租户和多数据库
/// </summary>
public class ExamDbContext : MultiDatabaseDbContextBase, IInitializableDbContext
{
    private readonly IServiceProvider _serviceProvider;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="options">数据库上下文选项</param>
    /// <param name="serviceProvider">服务提供者</param>
    /// <param name="currentUser">当前用户</param>
    /// <param name="httpContextAccessor">HTTP上下文访问器</param>
    public ExamDbContext(
        DbContextOptions options,
        IServiceProvider serviceProvider,
        ICurrentUser currentUser,
        IHttpContextAccessor httpContextAccessor) : base(options, serviceProvider, currentUser, httpContextAccessor)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// 用于审计的用户ID
    /// </summary>
    public long? UserId { get; set; }

    /// <summary>
    /// 获取当前用户ID，优先使用设置的UserId，否则使用CurrentUser中的Id
    /// </summary>
    protected override long? CurrentUserId => this.UserId ?? base.CurrentUserId;

    #region DbSet Properties
    /// <summary>
    /// 问题
    /// </summary>
    public DbSet<Question> Questions => Set<Question>();

    /// <summary>
    /// 问题分类
    /// </summary>
    public DbSet<QuestionCategory> QuestionCategories => Set<QuestionCategory>();

    /// <summary>
    /// 考生
    /// </summary>
    public DbSet<Student> Students => Set<Student>();
    
    /// <summary>
    /// 考生分组
    /// </summary>
    public DbSet<StudentGroup> StudentGroups => Set<StudentGroup>();

    /// <summary>
    /// 考生分组映射
    /// </summary>
    public DbSet<StudentGroupMapping> StudentGroupMappings => Set<StudentGroupMapping>();
    
    /// <summary>
    /// 练习记录
    /// </summary>
    public DbSet<PracticeRecord> PracticeRecords => Set<PracticeRecord>();
    
    /// <summary>
    /// 错题记录
    /// </summary>
    public DbSet<WrongQuestion> WrongQuestions => Set<WrongQuestion>();
    
    /// <summary>
    /// 试卷
    /// </summary>
    public DbSet<ExamPaper> ExamPapers => Set<ExamPaper>();
    
    /// <summary>
    /// 试卷题目
    /// </summary>
    public DbSet<ExamPaperQuestion> ExamPaperQuestions => Set<ExamPaperQuestion>();
    
    /// <summary>
    /// 考试设置
    /// </summary>
    public DbSet<ExamSetting> ExamSettings => Set<ExamSetting>();

    /// <summary>
    /// 考试设置-学生分组关联
    /// </summary>
    public DbSet<ExamSettingStudentGroup> ExamSettingStudentGroups => Set<ExamSettingStudentGroup>();
    
    /// <summary>
    /// 考试记录
    /// </summary>
    public DbSet<ExamRecord> ExamRecords => Set<ExamRecord>();
    
    /// <summary>
    /// 考试答题记录
    /// </summary>
    public DbSet<ExamAnswerRecord> ExamAnswerRecords => Set<ExamAnswerRecord>();

    /// <summary>
    /// 题目版本
    /// </summary>
    public DbSet<QuestionVersion> QuestionVersions => Set<QuestionVersion>();
    
    /// <summary>
    /// 练习设置
    /// </summary>
    public DbSet<PracticeSetting> PracticeSettings => Set<PracticeSetting>();

    /// <summary>
    /// 考试答题操作日志
    /// </summary>
    public DbSet<ExamAnswerOperationLog> ExamAnswerOperationLogs => Set<ExamAnswerOperationLog>();
    #endregion

    /// <summary>
    /// 配置模型
    /// </summary>
    /// <param name="modelBuilder">模型构建器</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 禁用所有long类型的实体ID的自增
        foreach (var entity in modelBuilder.Model.GetEntityTypes()
            .Where(e => e.ClrType.BaseType != null && 
                (e.ClrType.BaseType.Name.Contains("LongKeyAuditableEntityBase") || 
                 e.ClrType.BaseType.Name.Contains("AuditableEntityBase<long>"))))
        {
            modelBuilder.Entity(entity.ClrType).Property("Id").ValueGeneratedNever();
        }

        ConfigureQuestionEntities(modelBuilder);
        ConfigureStudentEntities(modelBuilder);
        ConfigureExamEntities(modelBuilder);

        // 配置题目和试卷题目的关系
        modelBuilder.Entity<Question>()
            .HasMany(q => q.ExamPaperQuestions)
            .WithOne(epq => epq.Question)
            .HasForeignKey(epq => epq.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);

        // 配置试卷和试卷题目的关系
        modelBuilder.Entity<ExamPaper>()
            .HasMany(ep => ep.ExamPaperQuestions)
            .WithOne(epq => epq.ExamPaper)
            .HasForeignKey(epq => epq.ExamPaperId)
            .OnDelete(DeleteBehavior.Cascade);

        // 配置多租户索引
        ConfigureMultiTenantIndexes(modelBuilder);
    }

    /// <summary>
    /// 配置多租户索引
    /// </summary>
    /// <param name="modelBuilder">模型构建器</param>
    private void ConfigureMultiTenantIndexes(ModelBuilder modelBuilder)
    {
        // 为所有实现IMultiTenant的实体添加TenantId索引
        var multiTenantEntities = modelBuilder.Model.GetEntityTypes()
            .Where(e => typeof(IMultiTenant).IsAssignableFrom(e.ClrType))
            .ToList();

        foreach (var entityType in multiTenantEntities)
        {
            // 添加TenantId单列索引
            modelBuilder.Entity(entityType.ClrType)
                .HasIndex(nameof(IMultiTenant.TenantId))
                .HasDatabaseName($"IX_{entityType.GetTableName()}_TenantId");

            // 为主要查询字段添加组合索引（TenantId + Id）
            modelBuilder.Entity(entityType.ClrType)
                .HasIndex("TenantId", "Id")
                .HasDatabaseName($"IX_{entityType.GetTableName()}_TenantId_Id");
        }

        // 为特定实体添加业务组合索引
        AddBusinessSpecificIndexes(modelBuilder);
    }

    /// <summary>
    /// 添加业务相关的组合索引
    /// </summary>
    /// <param name="modelBuilder">模型构建器</param>
    private void AddBusinessSpecificIndexes(ModelBuilder modelBuilder)
    {
        // Student: TenantId + StudentNumber (学号在租户内唯一)
        modelBuilder.Entity<Student>()
            .HasIndex(e => new { e.TenantId, e.StudentNumber })
            .IsUnique()
            .HasDatabaseName("IX_Students_TenantId_StudentNumber");

        // Question: TenantId + CategoryId (按租户和分类查询题目)
        modelBuilder.Entity<Question>()
            .HasIndex(e => new { e.TenantId, e.CategoryId })
            .HasDatabaseName("IX_Questions_TenantId_CategoryId");

        // ExamRecord: TenantId + StudentId (按租户和学生查询考试记录)
        modelBuilder.Entity<ExamRecord>()
            .HasIndex(e => new { e.TenantId, e.StudentId })
            .HasDatabaseName("IX_ExamRecords_TenantId_StudentId");

        // ExamRecord: TenantId + ExamSettingId (按租户和考试设置查询)
        modelBuilder.Entity<ExamRecord>()
            .HasIndex(e => new { e.TenantId, e.ExamSettingId })
            .HasDatabaseName("IX_ExamRecords_TenantId_ExamSettingId");

        // PracticeRecord: TenantId + StudentId (按租户和学生查询练习记录)
        modelBuilder.Entity<PracticeRecord>()
            .HasIndex(e => new { e.TenantId, e.StudentId })
            .HasDatabaseName("IX_PracticeRecords_TenantId_StudentId");

        // WrongQuestion: TenantId + StudentId (按租户和学生查询错题)
        modelBuilder.Entity<WrongQuestion>()
            .HasIndex(e => new { e.TenantId, e.StudentId })
            .HasDatabaseName("IX_WrongQuestions_TenantId_StudentId");
    }

    private void ConfigureQuestionEntities(ModelBuilder modelBuilder)
    {
        // 配置问题实体
        modelBuilder.Entity<Question>(entity =>
        {
            entity.ToTable("Questions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Content).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.Options).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.CorrectAnswer).IsRequired().HasMaxLength(4000);
            entity.Property(e => e.Analysis).HasMaxLength(2000);
            entity.Property(e => e.KnowledgePoints).HasMaxLength(500);
            entity.Property(e => e.Tags).HasMaxLength(500);
            entity.Property(e => e.Status).IsRequired().HasDefaultValue(QuestionStatus.Published);
            entity.Property(e => e.PublishedAt);
            entity.Property(e => e.PublishedBy);

            // 配置与分类的关系
            entity.HasOne(e => e.Category)
                .WithMany(e => e.Questions)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 配置问题版本实体
        modelBuilder.Entity<QuestionVersion>(entity =>
        {
            entity.ToTable("QuestionVersions");
            entity.HasKey(e => e.Id);

            // 配置与题目的关系
            entity.HasOne(e => e.Question)
                .WithMany()
                .HasForeignKey(e => e.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);

            // 确保版本号在题目范围内唯一
            entity.HasIndex(e => new { e.QuestionId, e.Version }).IsUnique();
        });

        // 配置问题分类实体
        modelBuilder.Entity<QuestionCategory>(entity =>
        {
            entity.ToTable("QuestionCategories");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(200);
        });
    }

    private void ConfigureStudentEntities(ModelBuilder modelBuilder)
    {
        // 配置考生-分组多对多关系
        modelBuilder.Entity<StudentGroupMapping>(entity =>
        {
            entity.ToTable("StudentGroupMappings");
            entity.HasKey(e => e.Id);

            entity.HasOne(e => e.Student)
                .WithMany(s => s.StudentGroups)
                .HasForeignKey(e => e.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.StudentGroup)
                .WithMany(g => g.Students)
                .HasForeignKey(e => e.StudentGroupId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        
        // 配置练习记录关系
        modelBuilder.Entity<PracticeRecord>(entity =>
        {
            entity.ToTable("PracticeRecords");
            entity.HasKey(e => e.Id);
            
            entity.HasOne(p => p.Student)
                .WithMany(s => s.PracticeRecords)
                .HasForeignKey(p => p.StudentId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(p => p.Question)
                .WithMany()
                .HasForeignKey(p => p.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        
        // 配置错题记录关系
        modelBuilder.Entity<WrongQuestion>(entity =>
        {
            entity.ToTable("WrongQuestions");
            entity.HasKey(e => e.Id);
            
            entity.HasOne(w => w.Student)
                .WithMany(s => s.WrongQuestions)
                .HasForeignKey(w => w.StudentId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(w => w.Question)
                .WithMany()
                .HasForeignKey(w => w.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureExamEntities(ModelBuilder modelBuilder)
    {
        // 配置试卷题目关系
        modelBuilder.Entity<ExamPaperQuestion>(entity =>
        {
            entity.ToTable("ExamPaperQuestions");
            entity.HasKey(e => e.Id);
            
            // 配置与试卷的关系
            entity.HasOne(epq => epq.ExamPaper)
                .WithMany(ep => ep.ExamPaperQuestions)
                .HasForeignKey(epq => epq.ExamPaperId)
                .OnDelete(DeleteBehavior.Restrict);
                
            // 配置与题目的关系
            entity.HasOne(epq => epq.Question)
                .WithMany()
                .HasForeignKey(epq => epq.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);

            // 配置与题目版本的关系
            entity.HasOne(epq => epq.QuestionVersion)
                .WithMany()
                .HasForeignKey(epq => epq.QuestionVersionId)
                .OnDelete(DeleteBehavior.Restrict);

            // 确保题目在试卷中的序号唯一
            entity.HasIndex(e => new { e.ExamPaperId, e.OrderNumber }).IsUnique();
        });
        
        // 配置试卷实体
        modelBuilder.Entity<ExamPaper>(entity =>
        {
            entity.ToTable("ExamPapers");
            entity.HasKey(e => e.Id);
            
            // 配置 decimal 类型的精度
            entity.Property(e => e.AverageScore).HasPrecision(10, 2);
            entity.Property(e => e.PassRate).HasPrecision(5, 2);
        });

        // 配置题目实体
        modelBuilder.Entity<Question>(entity =>
        {
            entity.ToTable("Questions");
            entity.HasKey(e => e.Id);
            
            // 配置 decimal 类型的精度
            entity.Property(e => e.CorrectRate).HasPrecision(5, 2);
        });
        
        // 配置考试设置关系
        modelBuilder.Entity<ExamSetting>(entity =>
        {
            entity.ToTable("ExamSettings");
            entity.HasKey(e => e.Id);
            
            entity.HasOne(es => es.ExamPaper)
                .WithMany()
                .HasForeignKey(es => es.ExamPaperId)
                .OnDelete(DeleteBehavior.Restrict);

            // 配置与学生分组的多对多关系
            entity.HasMany(es => es.StudentGroups)
                .WithOne(esg => esg.ExamSetting)
                .HasForeignKey(esg => esg.ExamSettingId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // 配置考试设置-学生分组关联实体
        modelBuilder.Entity<ExamSettingStudentGroup>(entity =>
        {
            entity.ToTable("ExamSettingStudentGroups");
            entity.HasKey(e => e.Id);

            entity.HasOne(esg => esg.ExamSetting)
                .WithMany(es => es.StudentGroups)
                .HasForeignKey(esg => esg.ExamSettingId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(esg => esg.StudentGroup)
                .WithMany()
                .HasForeignKey(esg => esg.StudentGroupId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        
        // 配置考试记录关系
        modelBuilder.Entity<ExamRecord>(entity =>
        {
            entity.ToTable("ExamRecords");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.IpAddress).IsRequired().HasMaxLength(50);
            entity.Property(e => e.DeviceInfo).HasMaxLength(1000);
            entity.Property(e => e.BrowserInfo).HasMaxLength(500);
            entity.Property(e => e.CheatingSuspicionRecord).HasMaxLength(2000);
            entity.Property(e => e.Comments).HasMaxLength(1000);
            
            entity.HasOne(er => er.Student)
                .WithMany(s => s.ExamRecords)
                .HasForeignKey(er => er.StudentId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(er => er.ExamSetting)
                .WithMany(es => es.ExamRecords)
                .HasForeignKey(er => er.ExamSettingId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        
        // 配置考试答题记录关系
        modelBuilder.Entity<ExamAnswerRecord>(entity =>
        {
            entity.ToTable("ExamAnswerRecords");
            entity.HasKey(e => e.Id);
            
            // 配置字符串长度
            entity.Property(e => e.Answer).HasMaxLength(2000);
            entity.Property(e => e.Comments).HasMaxLength(1000);
            
            // 配置与考试记录的关系
            entity.HasOne(ear => ear.ExamRecord)
                .WithMany(er => er.AnswerRecords)
                .HasForeignKey(ear => ear.ExamRecordId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // 配置与题目的关系
            entity.HasOne(ear => ear.Question)
                .WithMany()
                .HasForeignKey(ear => ear.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);

            // 配置与题目版本的关系
            entity.HasOne(ear => ear.QuestionVersion)
                .WithMany()
                .HasForeignKey(ear => ear.QuestionVersionId)
                .OnDelete(DeleteBehavior.Restrict);

            // 确保题目序号在考试记录中唯一
            entity.HasIndex(e => new { e.ExamRecordId, e.OrderNumber }).IsUnique();
        });

        // 配置考试答题操作日志
        modelBuilder.Entity<ExamAnswerOperationLog>(entity =>
        {
            entity.ToTable("ExamAnswerOperationLogs");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Answer).HasMaxLength(2000);

            entity.HasOne(e => e.ExamRecord)
                .WithMany()
                .HasForeignKey(e => e.ExamRecordId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.QuestionVersion)
                .WithMany()
                .HasForeignKey(e => e.QuestionVersionId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.ExamRecordId, e.OperationTime })
                .HasDatabaseName("IX_ExamAnswerOperationLogs_ExamRecordId_OperationTime");
        });

        // 配置练习设置关系
        modelBuilder.Entity<PracticeSetting>(entity =>
        {
            entity.ToTable("PracticeSettings");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            
            // 配置与试卷的关系
            entity.HasOne(ps => ps.ExamPaper)
                .WithMany()
                .HasForeignKey(ps => ps.ExamPaperId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    /// <summary>
    /// 初始化数据库
    /// </summary>
    public async Task InitializeDatabaseAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var hostEnvironment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
        await ExamDbContextSeed.SeedAsync(this, hostEnvironment.IsDevelopment());
    }
}