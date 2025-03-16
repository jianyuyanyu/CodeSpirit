namespace CodeSpirit.Audit.Models;

/// <summary>
/// 地理位置信息
/// </summary>
public class GeoLocation
{
    /// <summary>
    /// 国家
    /// </summary>
    public string Country { get; set; } = string.Empty;
    
    /// <summary>
    /// 国家代码
    /// </summary>
    public string CountryCode { get; set; } = string.Empty;
    
    /// <summary>
    /// 省/州
    /// </summary>
    public string Region { get; set; } = string.Empty;
    
    /// <summary>
    /// 城市
    /// </summary>
    public string City { get; set; } = string.Empty;
    
    /// <summary>
    /// 经度
    /// </summary>
    public double? Longitude { get; set; }
    
    /// <summary>
    /// 纬度
    /// </summary>
    public double? Latitude { get; set; }
    
    /// <summary>
    /// 互联网服务提供商
    /// </summary>
    public string ISP { get; set; } = string.Empty;
} 