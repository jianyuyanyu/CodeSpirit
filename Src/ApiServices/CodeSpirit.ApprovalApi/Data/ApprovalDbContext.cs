using CodeSpirit.ApprovalApi.Models;
using CodeSpirit.Shared.Data;
using Microsoft.AspNetCore.Http;
using CodeSpirit.Core;

namespace CodeSpirit.ApprovalApi.Data;

/// <summary>
/// 审批数据库上下文 - 支持多租户和多数据库
/// </summary>
public class ApprovalDbContext : MultiDatabaseDbContextBase
{
    private readonly IServiceProvider _serviceProvider;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="options">数据库上下文选项</param>
    /// <param name="serviceProvider">服务提供者</param>
    /// <param name="currentUser">当前用户</param>
    /// <param name="httpContextAccessor">HTTP上下文访问器</param>
    public ApprovalDbContext(
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

    /// <summary>
    /// 工作流定义
    /// </summary>
    public DbSet<WorkflowDefinition> WorkflowDefinitions { get; set; }

    /// <summary>
    /// 工作流节点
    /// </summary>
    public DbSet<WorkflowNode> WorkflowNodes { get; set; }

    /// <summary>
    /// 工作流节点审批人
    /// </summary>
    public DbSet<WorkflowNodeApprover> WorkflowNodeApprovers { get; set; }

    /// <summary>
    /// 工作流节点条件
    /// </summary>
    public DbSet<WorkflowNodeCondition> WorkflowNodeConditions { get; set; }

    /// <summary>
    /// 审批实例
    /// </summary>
    public DbSet<ApprovalInstance> ApprovalInstances { get; set; }

    /// <summary>
    /// 审批任务
    /// </summary>
    public DbSet<ApprovalTask> ApprovalTasks { get; set; }

    /// <summary>
    /// 审批日志
    /// </summary>
    public DbSet<ApprovalLog> ApprovalLogs { get; set; }

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

        // 配置多租户索引
        ConfigureMultiTenantIndexes(modelBuilder);

        // 配置工作流定义
        modelBuilder.Entity<WorkflowDefinition>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.HasIndex(e => new { e.TenantId, e.Code }).IsUnique();
            // 配置为长文本字段，让DatabaseSpecificConfigurations处理具体类型
            entity.Property(e => e.Configuration).HasMaxLength(4000);
        });

        // 配置工作流节点
        modelBuilder.Entity<WorkflowNode>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.HasOne(e => e.WorkflowDefinition)
                  .WithMany(e => e.Nodes)
                  .HasForeignKey(e => e.WorkflowDefinitionId)
                  .OnDelete(DeleteBehavior.Cascade);
            // 配置为长文本字段，让DatabaseSpecificConfigurations处理具体类型
            entity.Property(e => e.Configuration).HasMaxLength(4000);
        });

        // 配置工作流节点审批人
        modelBuilder.Entity<WorkflowNodeApprover>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.HasOne(e => e.WorkflowNode)
                  .WithMany(e => e.Approvers)
                  .HasForeignKey(e => e.WorkflowNodeId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // 配置工作流节点条件
        modelBuilder.Entity<WorkflowNodeCondition>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.HasOne(e => e.WorkflowNode)
                  .WithMany(e => e.Conditions)
                  .HasForeignKey(e => e.WorkflowNodeId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // 配置审批实例
        modelBuilder.Entity<ApprovalInstance>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.HasIndex(e => new { e.TenantId, e.EntityType, e.EntityId });
            entity.HasIndex(e => new { e.TenantId, e.ApplicantId });
            entity.HasIndex(e => new { e.TenantId, e.Status });
            entity.HasOne(e => e.WorkflowDefinition)
                  .WithMany()
                  .HasForeignKey(e => e.WorkflowDefinitionId)
                  .OnDelete(DeleteBehavior.Restrict);
            // 配置为长文本字段，让DatabaseSpecificConfigurations处理具体类型
            entity.Property(e => e.BusinessData).HasMaxLength(4000);
            entity.Property(e => e.RiskAssessmentResult).HasMaxLength(4000);
            entity.Property(e => e.IntelligentSuggestion).HasMaxLength(4000);
        });

        // 配置审批任务
        modelBuilder.Entity<ApprovalTask>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.HasIndex(e => e.ApproverId);
            entity.HasIndex(e => e.Status);
            entity.HasOne(e => e.ApprovalInstance)
                  .WithMany(e => e.Tasks)
                  .HasForeignKey(e => e.ApprovalInstanceId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // 配置审批日志
        modelBuilder.Entity<ApprovalLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.HasIndex(e => new { e.TenantId, e.ApprovalInstanceId });
            entity.HasIndex(e => new { e.TenantId, e.OperatorId });
            entity.HasIndex(e => e.OperationTime);
            // 配置为长文本字段，让DatabaseSpecificConfigurations处理具体类型
            entity.Property(e => e.ExtensionData).HasMaxLength(4000);
        });
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
        // WorkflowDefinition: TenantId + Code (工作流代码在租户内唯一)
        modelBuilder.Entity<WorkflowDefinition>()
            .HasIndex(e => new { e.TenantId, e.Code })
            .IsUnique()
            .HasDatabaseName("IX_WorkflowDefinitions_TenantId_Code");

        // ApprovalInstance: TenantId + EntityType + EntityId (按租户和业务实体查询)
        modelBuilder.Entity<ApprovalInstance>()
            .HasIndex(e => new { e.TenantId, e.EntityType, e.EntityId })
            .HasDatabaseName("IX_ApprovalInstances_TenantId_EntityType_EntityId");

        // ApprovalInstance: TenantId + ApplicantId (按租户和申请人查询)
        modelBuilder.Entity<ApprovalInstance>()
            .HasIndex(e => new { e.TenantId, e.ApplicantId })
            .HasDatabaseName("IX_ApprovalInstances_TenantId_ApplicantId");

        // ApprovalInstance: TenantId + Status (按租户和状态查询)
        modelBuilder.Entity<ApprovalInstance>()
            .HasIndex(e => new { e.TenantId, e.Status })
            .HasDatabaseName("IX_ApprovalInstances_TenantId_Status");

        // ApprovalTask: ApproverId + Status (按审批人和状态查询待办任务)
        modelBuilder.Entity<ApprovalTask>()
            .HasIndex(e => new { e.ApproverId, e.Status })
            .HasDatabaseName("IX_ApprovalTasks_ApproverId_Status");

        // ApprovalLog: TenantId + ApprovalInstanceId (按租户和审批实例查询日志)
        modelBuilder.Entity<ApprovalLog>()
            .HasIndex(e => new { e.TenantId, e.ApprovalInstanceId })
            .HasDatabaseName("IX_ApprovalLogs_TenantId_ApprovalInstanceId");

        // ApprovalLog: TenantId + OperatorId (按租户和操作人查询日志)
        modelBuilder.Entity<ApprovalLog>()
            .HasIndex(e => new { e.TenantId, e.OperatorId })
            .HasDatabaseName("IX_ApprovalLogs_TenantId_OperatorId");
    }
}
