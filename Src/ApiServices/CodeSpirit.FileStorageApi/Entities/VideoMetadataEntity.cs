using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CodeSpirit.Shared.Entities;

namespace CodeSpirit.FileStorageApi.Entities;

/// <summary>
/// 视频元数据实体
/// 存储视频文件的详细信息
/// </summary>
[Table("VideoMetadata")]
public class VideoMetadataEntity : LongKeyAuditableEntityBase
{
    /// <summary>
    /// 文件ID（一对一关系）
    /// </summary>
    [Required]
    public long FileId { get; set; }
    
    /// <summary>
    /// 视频宽度（像素）
    /// </summary>
    public int Width { get; set; }
    
    /// <summary>
    /// 视频高度（像素）
    /// </summary>
    public int Height { get; set; }
    
    /// <summary>
    /// 时长（秒）
    /// </summary>
    public double Duration { get; set; }
    
    /// <summary>
    /// 比特率（bps）
    /// </summary>
    public long Bitrate { get; set; }
    
    /// <summary>
    /// 帧率（fps）
    /// </summary>
    public double FrameRate { get; set; }
    
    /// <summary>
    /// 视频编码格式
    /// </summary>
    [MaxLength(64)]
    public string VideoCodec { get; set; }
    
    /// <summary>
    /// 音频编码格式
    /// </summary>
    [MaxLength(64)]
    public string AudioCodec { get; set; }
    
    /// <summary>
    /// 容器格式
    /// </summary>
    [MaxLength(32)]
    public string Container { get; set; }
    
    /// <summary>
    /// 是否有音频轨道
    /// </summary>
    public bool HasAudio { get; set; }
    
    /// <summary>
    /// 是否有视频轨道
    /// </summary>
    public bool HasVideo { get; set; }
    
    /// <summary>
    /// 音频采样率（Hz）
    /// </summary>
    public int AudioSampleRate { get; set; }
    
    /// <summary>
    /// 音频通道数
    /// </summary>
    public int AudioChannels { get; set; }
    
    /// <summary>
    /// 缩略图时间点（秒）
    /// </summary>
    public double ThumbnailTimePosition { get; set; }
    
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime? CreatedTime { get; set; }
    
    /// <summary>
    /// 元数据信息（JSON格式）
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string MetadataInfo { get; set; }
    
    /// <summary>
    /// 关联的文件
    /// </summary>
    public virtual FileEntity File { get; set; }
}
