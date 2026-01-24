using CodeSpirit.Shared.Entities.Interfaces;
using CodeSpirit.MultiTenant.Abstractions;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.{Service}Api.Data.Models;

/// <summary>
/// {EntityName} 实体
/// </summary>
public class {EntityName} : IFullAuditable, IMultiTenant, IIsActive
{
    /// <summary>
    /// 主键ID
    /// </summary>
    public long Id { get; set; }
    
    /// <summary>
    /// 租户ID
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string TenantId { get; set; } = string.Empty;
    
    /// <summary>
    /// {字段描述}
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string {PropertyName} { get; set; } = string.Empty;
    
    // 审计字段（IFullAuditable）
    public long CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public long? DeletedBy { get; set; }
    public DateTime? DeletedAt { get; set; }
    public bool IsDeleted { get; set; }
    
    // 激活状态（IIsActive）
    public bool IsActive { get; set; }
}
