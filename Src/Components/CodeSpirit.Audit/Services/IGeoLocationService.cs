using GeoLoc = CodeSpirit.Audit.Models.GeoLocation;

namespace CodeSpirit.Audit.Services;

/// <summary>
/// 地理位置服务接口
/// </summary>
public interface IGeoLocationService
{
    /// <summary>
    /// 根据IP地址获取地理位置信息
    /// </summary>
    /// <param name="ipAddress">IP地址</param>
    /// <returns>地理位置信息</returns>
    Task<GeoLoc> GetLocationByIpAsync(string ipAddress);
} 