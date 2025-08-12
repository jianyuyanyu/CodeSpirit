using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Dtos;
using CodeSpirit.FileStorageApi.Entities;
using Newtonsoft.Json;

namespace CodeSpirit.FileStorageApi.Dtos;

/// <summary>
/// 文件引用信息DTO
/// </summary>
public class FileReferenceDto
{
    /// <summary>
    /// 引用ID
    /// </summary>
    [DisplayName("引用ID")]
    public long Id { get; set; }

    /// <summary>
    /// 文件ID
    /// </summary>
    [DisplayName("文件ID")]
    public long FileId { get; set; }

    /// <summary>
    /// 引用来源服务
    /// </summary>
    [DisplayName("来源服务")]
    public string? SourceService { get; set; }

    /// <summary>
    /// 引用来源实体类型
    /// </summary>
    [DisplayName("来源实体类型")]
    public string? SourceEntityType { get; set; }

    /// <summary>
    /// 引用来源实体ID
    /// </summary>
    [DisplayName("来源实体ID")]
    public string? SourceEntityId { get; set; }

    /// <summary>
    /// 引用字段名
    /// </summary>
    [DisplayName("字段名")]
    public string? FieldName { get; set; }

    /// <summary>
    /// 引用类型
    /// </summary>
    [DisplayName("引用类型")]
    public ReferenceType ReferenceType { get; set; }

    /// <summary>
    /// 引用状态
    /// </summary>
    [DisplayName("引用状态")]
    public ReferenceStatus Status { get; set; }

    /// <summary>
    /// 是否为临时引用
    /// </summary>
    [DisplayName("临时引用")]
    public bool IsTemporary { get; set; }

    /// <summary>
    /// 引用过期时间
    /// </summary>
    [DisplayName("过期时间")]
    public DateTime? ExpirationTime { get; set; }

    /// <summary>
    /// 引用确认时间
    /// </summary>
    [DisplayName("确认时间")]
    public DateTime? ConfirmedTime { get; set; }

    /// <summary>
    /// 引用备注
    /// </summary>
    [DisplayName("备注")]
    public string? Remarks { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [DisplayName("创建时间")]
    public DateTime CreatedTime { get; set; }

    /// <summary>
    /// 创建者
    /// </summary>
    [DisplayName("创建者")]
    [AggregateField("/identity/users/{value}.userName")]
    public string? CreatedBy { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    [DisplayName("更新时间")]
    public DateTime? UpdatedTime { get; set; }

    /// <summary>
    /// 更新者
    /// </summary>
    [DisplayName("更新者")]
    [AggregateField("/identity/users/{value}.userName")]
    public string? UpdatedBy { get; set; }

    /// <summary>
    /// 关联的文件信息
    /// </summary>
    [DisplayName("文件信息")]
    public FileDto? File { get; set; }
}

/// <summary>
/// 文件引用查询DTO
/// </summary>
public class FileReferenceQueryDto : QueryDtoBase
{
    /// <summary>
    /// 文件ID
    /// </summary>
    [DisplayName("文件ID")]
    public long? FileId { get; set; }

    /// <summary>
    /// 引用来源服务
    /// </summary>
    [DisplayName("来源服务")]
    public string? SourceService { get; set; }

    /// <summary>
    /// 引用来源实体类型
    /// </summary>
    [DisplayName("来源实体类型")]
    public string? SourceEntityType { get; set; }

    /// <summary>
    /// 引用来源实体ID
    /// </summary>
    [DisplayName("来源实体ID")]
    public string? SourceEntityId { get; set; }

    /// <summary>
    /// 引用类型
    /// </summary>
    [DisplayName("引用类型")]
    public ReferenceType? ReferenceType { get; set; }

    /// <summary>
    /// 引用状态
    /// </summary>
    [DisplayName("引用状态")]
    public ReferenceStatus? Status { get; set; }

    /// <summary>
    /// 是否为临时引用
    /// </summary>
    [DisplayName("临时引用")]
    public bool? IsTemporary { get; set; }

    /// <summary>
    /// 过期开始时间
    /// </summary>
    [DisplayName("过期时间从")]
    public DateTime? ExpirationFrom { get; set; }

    /// <summary>
    /// 过期结束时间
    /// </summary>
    [DisplayName("过期时间到")]
    public DateTime? ExpirationTo { get; set; }

    /// <summary>
    /// 创建开始时间
    /// </summary>
    [DisplayName("创建时间从")]
    public DateTime? CreatedFrom { get; set; }

    /// <summary>
    /// 创建结束时间
    /// </summary>
    [DisplayName("创建时间到")]
    public DateTime? CreatedTo { get; set; }
}

/// <summary>
/// 创建文件引用DTO
/// </summary>
public class CreateFileReferenceDto
{
    /// <summary>
    /// 文件ID
    /// </summary>
    [Required]
    [DisplayName("文件ID")]
    public long FileId { get; set; }

    /// <summary>
    /// 引用来源服务
    /// </summary>
    [Required]
    [DisplayName("来源服务")]
    [MaxLength(128)]
    public string SourceService { get; set; } = string.Empty;

    /// <summary>
    /// 引用来源实体类型
    /// </summary>
    [Required]
    [DisplayName("来源实体类型")]
    [MaxLength(128)]
    public string SourceEntityType { get; set; } = string.Empty;

    /// <summary>
    /// 引用来源实体ID
    /// </summary>
    [Required]
    [DisplayName("来源实体ID")]
    [MaxLength(128)]
    public string SourceEntityId { get; set; } = string.Empty;

    /// <summary>
    /// 引用字段名
    /// </summary>
    [DisplayName("字段名")]
    [MaxLength(128)]
    public string? FieldName { get; set; }

    /// <summary>
    /// 引用类型
    /// </summary>
    [DisplayName("引用类型")]
    public ReferenceType ReferenceType { get; set; } = ReferenceType.Attachment;

    /// <summary>
    /// 是否为临时引用
    /// </summary>
    [DisplayName("临时引用")]
    public bool IsTemporary { get; set; } = false;

    /// <summary>
    /// 引用过期时间（临时引用）
    /// </summary>
    [DisplayName("过期时间")]
    public DateTime? ExpirationTime { get; set; }

    /// <summary>
    /// 引用备注
    /// </summary>
    [DisplayName("备注")]
    [MaxLength(500)]
    public string? Remarks { get; set; }
}

/// <summary>
/// 更新文件引用DTO
/// </summary>
public class UpdateFileReferenceDto
{
    /// <summary>
    /// 引用状态
    /// </summary>
    [DisplayName("引用状态")]
    public ReferenceStatus? Status { get; set; }

    /// <summary>
    /// 引用过期时间
    /// </summary>
    [DisplayName("过期时间")]
    public DateTime? ExpirationTime { get; set; }

    /// <summary>
    /// 引用备注
    /// </summary>
    [DisplayName("备注")]
    [MaxLength(500)]
    public string? Remarks { get; set; }
}

/// <summary>
/// 确认文件引用DTO
/// </summary>
public class ConfirmFileReferenceDto
{
    /// <summary>
    /// 引用ID列表
    /// </summary>
    [Required]
    [DisplayName("引用ID列表")]
    public List<long> ReferenceIds { get; set; } = new();

    /// <summary>
    /// 确认备注
    /// </summary>
    [DisplayName("确认备注")]
    [MaxLength(500)]
    public string? Remarks { get; set; }
}

/// <summary>
/// 文件引用批量导入项DTO
/// </summary>
public class FileReferenceBatchImportItemDto
{
    /// <summary>
    /// 文件ID
    /// </summary>
    [Required]
    [DisplayName("文件ID")]
    [JsonProperty("文件ID")]
    public long FileId { get; set; }

    /// <summary>
    /// 引用来源服务
    /// </summary>
    [Required]
    [DisplayName("来源服务")]
    [JsonProperty("来源服务")]
    public string SourceService { get; set; } = string.Empty;

    /// <summary>
    /// 引用来源实体类型
    /// </summary>
    [Required]
    [DisplayName("来源实体类型")]
    [JsonProperty("来源实体类型")]
    public string SourceEntityType { get; set; } = string.Empty;

    /// <summary>
    /// 引用来源实体ID
    /// </summary>
    [Required]
    [DisplayName("来源实体ID")]
    [JsonProperty("来源实体ID")]
    public string SourceEntityId { get; set; } = string.Empty;

    /// <summary>
    /// 引用字段名
    /// </summary>
    [DisplayName("字段名")]
    [JsonProperty("字段名")]
    public string? FieldName { get; set; }

    /// <summary>
    /// 引用类型
    /// </summary>
    [DisplayName("引用类型")]
    [JsonProperty("引用类型")]
    public ReferenceType ReferenceType { get; set; } = ReferenceType.Attachment;

    /// <summary>
    /// 是否为临时引用
    /// </summary>
    [DisplayName("临时引用")]
    [JsonProperty("临时引用")]
    public bool IsTemporary { get; set; } = false;

    /// <summary>
    /// 引用备注
    /// </summary>
    [DisplayName("备注")]
    [JsonProperty("备注")]
    public string? Remarks { get; set; }
}

/// <summary>
/// 文件引用统计DTO
/// </summary>
public class FileReferenceStatisticsDto
{
    /// <summary>
    /// 总引用数
    /// </summary>
    [DisplayName("总引用数")]
    public long TotalReferences { get; set; }

    /// <summary>
    /// 待确认引用数
    /// </summary>
    [DisplayName("待确认引用数")]
    public long PendingReferences { get; set; }

    /// <summary>
    /// 已确认引用数
    /// </summary>
    [DisplayName("已确认引用数")]
    public long ConfirmedReferences { get; set; }

    /// <summary>
    /// 已过期引用数
    /// </summary>
    [DisplayName("已过期引用数")]
    public long ExpiredReferences { get; set; }

    /// <summary>
    /// 临时引用数
    /// </summary>
    [DisplayName("临时引用数")]
    public long TemporaryReferences { get; set; }

    /// <summary>
    /// 按引用类型分组统计
    /// </summary>
    [DisplayName("按类型分组")]
    public Dictionary<ReferenceType, long> ReferencesByType { get; set; } = new();

    /// <summary>
    /// 按来源服务分组统计
    /// </summary>
    [DisplayName("按服务分组")]
    public Dictionary<string, long> ReferencesByService { get; set; } = new();
}
