using GeoLoc = CodeSpirit.Audit.Models.GeoLocation;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace CodeSpirit.Audit.Services.Implementation;

/// <summary>
/// 地理位置服务实现
/// </summary>
public class GeoLocationService : IGeoLocationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GeoLocationService> _logger;
    private readonly IMemoryCache _cache;
    private readonly AuditOptions _options;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    public GeoLocationService(
        IHttpClientFactory httpClientFactory,
        ILogger<GeoLocationService> logger,
        IMemoryCache cache,
        IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _cache = cache;
        
        // 获取配置
        var options = new AuditOptions();
        configuration.GetSection("Audit").Bind(options);
        _options = options;
    }
    
    /// <summary>
    /// 根据IP地址获取地理位置信息
    /// </summary>
    public async Task<GeoLoc> GetLocationByIpAsync(string ipAddress)
    {
        // 如果IP地址为空或本地IP，返回空对象
        if (string.IsNullOrEmpty(ipAddress) || 
            ipAddress == "127.0.0.1" || 
            ipAddress == "::1" || 
            ipAddress == "localhost" || 
            ipAddress == "未知")
        {
            return new GeoLoc
            {
                Country = "本地",
                City = "本地环境"
            };
        }
        
        // 检查缓存
        var cacheKey = $"GeoLocation:{ipAddress}";
        if (_cache.TryGetValue(cacheKey, out GeoLoc cachedLocation))
        {
            return cachedLocation;
        }
        
        try
        {
            // 检查是否启用IP地理位置查询
            if (!_options.EnableGeoLocation)
            {
                return new GeoLoc();
            }
            
            // 使用配置的API查询IP地址
            var apiUrl = string.Format(_options.GeoLocationApiUrl, ipAddress);
            
            var client = _httpClientFactory.CreateClient("GeoLocation");
            var response = await client.GetAsync(apiUrl);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                
                // 根据配置的API类型处理响应
                var location = ParseGeoLocationResponse(content, _options.GeoLocationApiType);
                
                // 缓存结果（1天）
                _cache.Set(cacheKey, location, TimeSpan.FromDays(1));
                
                return location;
            }
            
            _logger.LogWarning("获取IP地址 {IpAddress} 的地理位置信息失败: {StatusCode}", 
                ipAddress, response.StatusCode);
            return new GeoLoc();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取IP地址 {IpAddress} 的地理位置信息时发生错误", ipAddress);
            return new GeoLoc();
        }
    }
    
    /// <summary>
    /// 解析地理位置响应
    /// </summary>
    private GeoLoc ParseGeoLocationResponse(string content, string apiType)
    {
        try
        {
            // 根据不同的API类型解析响应
            switch (apiType?.ToLower())
            {
                case "ipapi":
                    return ParseIpApiResponse(content);
                case "ipinfo":
                    return ParseIpInfoResponse(content);
                case "ipstack":
                    return ParseIpStackResponse(content);
                default:
                    // 默认使用 ipapi.co 格式
                    return ParseIpApiResponse(content);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解析地理位置响应时发生错误");
            return new GeoLoc();
        }
    }
    
    /// <summary>
    /// 解析 ipapi.co 响应
    /// </summary>
    private GeoLoc ParseIpApiResponse(string content)
    {
        try
        {
            var response = JsonSerializer.Deserialize<JsonElement>(content);
            
            return new GeoLoc
            {
                Country = GetJsonValue(response, "country_name"),
                CountryCode = GetJsonValue(response, "country_code"),
                Region = GetJsonValue(response, "region_name"),
                City = GetJsonValue(response, "city"),
                Latitude = GetJsonDoubleValue(response, "latitude"),
                Longitude = GetJsonDoubleValue(response, "longitude"),
                ISP = GetJsonValue(response, "org")
            };
        }
        catch
        {
            return new GeoLoc();
        }
    }
    
    /// <summary>
    /// 解析 ipinfo.io 响应
    /// </summary>
    private GeoLoc ParseIpInfoResponse(string content)
    {
        try
        {
            var response = JsonSerializer.Deserialize<JsonElement>(content);
            
            var loc = GetJsonValue(response, "loc").Split(',');
            double? lat = null;
            double? lon = null;
            
            if (loc.Length == 2)
            {
                double.TryParse(loc[0], out var latitude);
                double.TryParse(loc[1], out var longitude);
                lat = latitude;
                lon = longitude;
            }
            
            return new GeoLoc
            {
                Country = GetJsonValue(response, "country"),
                CountryCode = GetJsonValue(response, "country"),
                Region = GetJsonValue(response, "region"),
                City = GetJsonValue(response, "city"),
                Latitude = lat,
                Longitude = lon,
                ISP = GetJsonValue(response, "org")
            };
        }
        catch
        {
            return new GeoLoc();
        }
    }
    
    /// <summary>
    /// 解析 ipstack.com 响应
    /// </summary>
    private GeoLoc ParseIpStackResponse(string content)
    {
        try
        {
            var response = JsonSerializer.Deserialize<JsonElement>(content);
            
            return new GeoLoc
            {
                Country = GetJsonValue(response, "country_name"),
                CountryCode = GetJsonValue(response, "country_code"),
                Region = GetJsonValue(response, "region_name"),
                City = GetJsonValue(response, "city"),
                Latitude = GetJsonDoubleValue(response, "latitude"),
                Longitude = GetJsonDoubleValue(response, "longitude"),
                ISP = GetJsonValue(response, "connection", "isp")
            };
        }
        catch
        {
            return new GeoLoc();
        }
    }
    
    /// <summary>
    /// 获取JSON中的字符串值
    /// </summary>
    private string GetJsonValue(JsonElement element, params string[] path)
    {
        try
        {
            JsonElement current = element;
            
            foreach (var segment in path)
            {
                if (current.TryGetProperty(segment, out var property))
                {
                    current = property;
                }
                else
                {
                    return string.Empty;
                }
            }
            
            return current.ValueKind == JsonValueKind.String ? current.GetString() : string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
    
    /// <summary>
    /// 获取JSON中的double值
    /// </summary>
    private double? GetJsonDoubleValue(JsonElement element, params string[] path)
    {
        try
        {
            JsonElement current = element;
            
            foreach (var segment in path)
            {
                if (current.TryGetProperty(segment, out var property))
                {
                    current = property;
                }
                else
                {
                    return null;
                }
            }
            
            if (current.ValueKind == JsonValueKind.Number && current.TryGetDouble(out var value))
            {
                return value;
            }
            
            return null;
        }
        catch
        {
            return null;
        }
    }
} 