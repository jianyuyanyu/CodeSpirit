using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CodeSpirit.Shared.Entities;
using CodeSpirit.Core;

namespace CodeSpirit.FileStorageApi.Entities;

/// <summary>
/// 文件引用实体
/// 管理文件的引用关系，支持引用计数和生命周期管理
/// </summary>
[Table("FileReferences")]
public class FileReferenceEntity : LongKeyAuditableEntityBase, IMultiTenant
{
    /// <summary>
    /// 租户ID
    /// </summary>
    [Required]
    [MaxLength(64)]
    public string TenantId { get; set; }
    
    /// <summary>
    /// 文件ID
    /// </summary>
    [Required]
    public long FileId { get; set; }
    
    /// <summary>
    /// 引用来源服务
    /// </summary>
    [Required]
    [MaxLength(128)]
    public string SourceService { get; set; }
    
    /// <summary>
    /// 引用来源实体类型
    /// </summary>
    [Required]
    [MaxLength(128)]
    public string SourceEntityType { get; set; }
    
    /// <summary>
    /// 引用来源实体ID
    /// </summary>
    [Required]
    [MaxLength(128)]
    public string SourceEntityId { get; set; }
    
    /// <summary>
    /// 引用字段名
    /// </summary>
    [MaxLength(128)]
    public string FieldName { get; set; }
    
    /// <summary>
    /// 引用类型
    /// </summary>
    public ReferenceType ReferenceType { get; set; } = ReferenceType.Attachment;
    
    /// <summary>
    /// 引用状态
    /// </summary>
    public ReferenceStatus Status { get; set; } = ReferenceStatus.Pending;
    
    /// <summary>
    /// 是否为临时引用
    /// </summary>
    public bool IsTemporary { get; set; }
    
    /// <summary>
    /// 引用过期时间（临时引用）
    /// </summary>
    public DateTime? ExpirationTime { get; set; }
    
    /// <summary>
    /// 引用确认时间
    /// </summary>
    public DateTime? ConfirmedTime { get; set; }
    
    /// <summary>
    /// 引用备注
    /// </summary>
    [MaxLength(500)]
    public string Remarks { get; set; }
    
    /// <summary>
    /// 扩展属性（JSON格式）
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string Properties { get; set; }
    
    /// <summary>
    /// 关联的文件
    /// </summary>
    public virtual FileEntity File { get; set; }
}
