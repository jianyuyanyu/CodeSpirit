using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.FileStorageApi.Entities;

/// <summary>
/// 文件状态
/// </summary>
public enum FileStatus
{
    /// <summary>
    /// 上传中
    /// </summary>
    [Display(Name = "上传中")]
    Uploading = 1,
    
    /// <summary>
    /// 活跃状态
    /// </summary>
    [Display(Name = "正常")]
    Active = 2,
    
    /// <summary>
    /// 已过期
    /// </summary>
    [Display(Name = "已过期")]
    Expired = 3,
    
    /// <summary>
    /// 已删除
    /// </summary>
    [Display(Name = "已删除")]
    Deleted = 4,
    
    /// <summary>
    /// 处理中
    /// </summary>
    [Display(Name = "处理中")]
    Processing = 5
}

/// <summary>
/// 文件类型分类
/// </summary>
public enum FileTypeCategory
{
    /// <summary>
    /// 未知类型
    /// </summary>
    [Display(Name = "未知")]
    Unknown = 0,
    
    /// <summary>
    /// 图片
    /// </summary>
    [Display(Name = "图片")]
    Image = 1,
    
    /// <summary>
    /// 视频
    /// </summary>
    [Display(Name = "视频")]
    Video = 2,
    
    /// <summary>
    /// 音频
    /// </summary>
    [Display(Name = "音频")]
    Audio = 3,
    
    /// <summary>
    /// 文档
    /// </summary>
    [Display(Name = "文档")]
    Document = 4,
    
    /// <summary>
    /// 压缩包
    /// </summary>
    [Display(Name = "压缩包")]
    Archive = 5,
    
    /// <summary>
    /// 其他
    /// </summary>
    [Display(Name = "其他")]
    Other = 99
}

/// <summary>
/// 引用类型
/// </summary>
public enum ReferenceType
{
    /// <summary>
    /// 附件
    /// </summary>
    [Display(Name = "附件")]
    Attachment = 1,
    
    /// <summary>
    /// 头像
    /// </summary>
    [Display(Name = "头像")]
    Avatar = 2,
    
    /// <summary>
    /// 图片
    /// </summary>
    [Display(Name = "图片")]
    Image = 3,
    
    /// <summary>
    /// 文档
    /// </summary>
    [Display(Name = "文档")]
    Document = 4,
    
    /// <summary>
    /// 视频
    /// </summary>
    [Display(Name = "视频")]
    Video = 5,
    
    /// <summary>
    /// 音频
    /// </summary>
    [Display(Name = "音频")]
    Audio = 6
}

/// <summary>
/// 引用状态
/// </summary>
public enum ReferenceStatus
{
    /// <summary>
    /// 待确认
    /// </summary>
    [Display(Name = "待确认")]
    Pending = 1,
    
    /// <summary>
    /// 已确认
    /// </summary>
    [Display(Name = "已确认")]
    Confirmed = 2,
    
    /// <summary>
    /// 已取消
    /// </summary>
    [Display(Name = "已取消")]
    Cancelled = 3,
    
    /// <summary>
    /// 已过期
    /// </summary>
    [Display(Name = "已过期")]
    Expired = 4
}
