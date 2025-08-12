using System.ComponentModel;
using Newtonsoft.Json;

namespace CodeSpirit.FileStorageApi.Dtos;

/// <summary>
/// Amis input-image 组件专用的图片响应DTO
/// 符合 Amis 组件的数据格式要求
/// </summary>
public class AmisImageDto
{
    /// <summary>
    /// 图片值，通常是文件ID或路径
    /// </summary>
    [JsonProperty("value")]
    [DisplayName("图片值")]
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// 图片URL地址
    /// </summary>
    [JsonProperty("url")]
    [DisplayName("图片URL")]
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// 文件名
    /// </summary>
    [JsonProperty("name")]
    [DisplayName("文件名")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 文件大小（字节）
    /// </summary>
    [JsonProperty("size")]
    [DisplayName("文件大小")]
    public long Size { get; set; }

    /// <summary>
    /// 文件类型
    /// </summary>
    [JsonProperty("type")]
    [DisplayName("文件类型")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 图片宽度
    /// </summary>
    [JsonProperty("width")]
    [DisplayName("宽度")]
    public int? Width { get; set; }

    /// <summary>
    /// 图片高度
    /// </summary>
    [JsonProperty("height")]
    [DisplayName("高度")]
    public int? Height { get; set; }

    /// <summary>
    /// 文件ID
    /// </summary>
    [JsonProperty("id")]
    [DisplayName("文件ID")]
    public long Id { get; set; }

    /// <summary>
    /// 是否为图片
    /// </summary>
    [JsonProperty("isImage")]
    [DisplayName("是否为图片")]
    public bool IsImage { get; set; } = true;

    /// <summary>
    /// 上传时间
    /// </summary>
    [JsonProperty("uploadTime")]
    [DisplayName("上传时间")]
    public DateTime UploadTime { get; set; }


}


