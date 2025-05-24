using System.Net.Sockets;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace CodeSpirit.Shared.Services;

/// <summary>
/// 客户端IP地址获取服务实现
/// </summary>
public class ClientIpService : IClientIpService
{
    private readonly ILogger<ClientIpService> _logger;

    /// <summary>
    /// 需要检查的代理头列表，按优先级排序
    /// </summary>
    private static readonly string[] ProxyHeaders = 
    {
        "X-Forwarded-For",     // 最常用的代理头
        "X-Real-IP",           // Nginx常用
        "CF-Connecting-IP",    // Cloudflare
        "True-Client-IP",      // Akamai
        "X-Client-IP",         // 自定义头
        "X-Original-IP",       // 其他代理
        "X-Forwarded",         // 备用选项
        "Forwarded-For",       // 备用选项
        "Forwarded"            // RFC 7239标准
    };

    public ClientIpService(ILogger<ClientIpService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 获取客户端真实IP地址
    /// </summary>
    /// <param name="httpContext">HTTP上下文</param>
    /// <returns>客户端IP地址</returns>
    public string GetClientIpAddress(HttpContext httpContext)
    {
        if (httpContext == null)
        {
            _logger.LogWarning("HttpContext为空，无法获取客户端IP地址");
            return "未知";
        }

        try
        {
            // 1. 首先尝试从代理头获取IP地址
            foreach (var header in ProxyHeaders)
            {
                if (httpContext.Request.Headers.TryGetValue(header, out StringValues headerValue) && 
                    !StringValues.IsNullOrEmpty(headerValue))
                {
                    var value = headerValue.ToString();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        var ip = ExtractIpFromHeader(header, value);
                        if (!string.IsNullOrEmpty(ip) && IsValidIpAddress(ip))
                        {
                            _logger.LogDebug("从头 {Header} 获取到IP地址: {IpAddress}", header, ip);
                            return ip;
                        }
                    }
                }
            }

            // 2. 尝试从连接特性获取IP地址
            var connection = httpContext.Features.Get<IHttpConnectionFeature>();
            var connectionIp = connection?.RemoteIpAddress?.ToString();
            
            if (!string.IsNullOrEmpty(connectionIp) && IsValidIpAddress(connectionIp))
            {
                _logger.LogDebug("从连接特性获取到IP地址: {IpAddress}", connectionIp);
                return connectionIp;
            }

            // 3. 尝试从Connection获取IP地址
            var remoteIp = httpContext.Connection.RemoteIpAddress?.ToString();
            if (!string.IsNullOrEmpty(remoteIp) && IsValidIpAddress(remoteIp))
            {
                _logger.LogDebug("从Connection获取到IP地址: {IpAddress}", remoteIp);
                return remoteIp;
            }

            _logger.LogWarning("无法获取有效的客户端IP地址");
            return "未知";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取客户端IP地址时发生异常");
            return "未知";
        }
    }

    /// <summary>
    /// 获取客户端真实IP地址
    /// </summary>
    /// <param name="httpContextAccessor">HTTP上下文访问器</param>
    /// <returns>客户端IP地址</returns>
    public string GetClientIpAddress(IHttpContextAccessor httpContextAccessor)
    {
        if (httpContextAccessor?.HttpContext == null)
        {
            _logger.LogWarning("HttpContextAccessor或HttpContext为空，无法获取客户端IP地址");
            return "未知";
        }

        return GetClientIpAddress(httpContextAccessor.HttpContext);
    }

    /// <summary>
    /// 从指定的头中提取IP地址
    /// </summary>
    /// <param name="headerName">头名称</param>
    /// <param name="headerValue">头值</param>
    /// <returns>提取的IP地址</returns>
    private string ExtractIpFromHeader(string headerName, string headerValue)
    {
        if (string.IsNullOrWhiteSpace(headerValue))
        {
            return null;
        }

        try
        {
            // 处理X-Forwarded-For等可能包含多个IP的情况
            if (headerName.Equals("X-Forwarded-For", StringComparison.OrdinalIgnoreCase) ||
                headerName.Equals("Forwarded-For", StringComparison.OrdinalIgnoreCase))
            {
                var ips = headerValue.Split(',', StringSplitOptions.RemoveEmptyEntries);
                if (ips.Length > 0)
                {
                    // 通常第一个是客户端真实IP
                    return ips[0].Trim();
                }
            }

            // 处理RFC 7239格式的Forwarded头
            if (headerName.Equals("Forwarded", StringComparison.OrdinalIgnoreCase))
            {
                return ExtractIpFromForwardedHeader(headerValue);
            }

            // 其他头直接返回值
            return headerValue.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "从头 {Header} 提取IP地址时发生异常: {HeaderValue}", headerName, headerValue);
            return null;
        }
    }

    /// <summary>
    /// 从RFC 7239格式的Forwarded头中提取IP地址
    /// </summary>
    /// <param name="forwardedValue">Forwarded头的值</param>
    /// <returns>提取的IP地址</returns>
    private string ExtractIpFromForwardedHeader(string forwardedValue)
    {
        if (string.IsNullOrWhiteSpace(forwardedValue))
        {
            return null;
        }

        try
        {
            // Forwarded头格式: for=192.0.2.60;proto=http;by=203.0.113.43
            var parts = forwardedValue.Split(';', StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var part in parts)
            {
                var trimmedPart = part.Trim();
                if (trimmedPart.StartsWith("for=", StringComparison.OrdinalIgnoreCase))
                {
                    var ip = trimmedPart.Substring(4).Trim('"', ' ');
                    // 可能包含端口号，如 192.0.2.60:8080
                    var colonIndex = ip.LastIndexOf(':');
                    if (colonIndex > 0 && !ip.Contains('[')) // 排除IPv6地址
                    {
                        ip = ip.Substring(0, colonIndex);
                    }
                    return ip;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "解析Forwarded头时发生异常: {ForwardedValue}", forwardedValue);
        }

        return null;
    }

    /// <summary>
    /// 验证IP地址是否有效
    /// </summary>
    /// <param name="ipAddress">IP地址</param>
    /// <returns>是否有效</returns>
    private bool IsValidIpAddress(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            return false;
        }

        // 过滤本地回环地址
        if (ipAddress == "::1" || ipAddress == "127.0.0.1" || ipAddress == "localhost")
        {
            return false;
        }

        // 过滤私有网络地址（可选，根据需求决定）
        // 如果需要保留私有网络地址，可以注释掉以下代码
        if (IsPrivateIpAddress(ipAddress))
        {
            // 在某些场景下，私有IP地址也是有效的（如内网环境）
            // 这里可以根据具体需求决定是否返回私有IP
            return true;
        }

        return true;
    }

    /// <summary>
    /// 检查是否为私有IP地址
    /// </summary>
    /// <param name="ipAddress">IP地址</param>
    /// <returns>是否为私有IP地址</returns>
    private bool IsPrivateIpAddress(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            return false;
        }

        try
        {
            var ip = System.Net.IPAddress.Parse(ipAddress);
            
            // IPv4私有地址范围
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                var bytes = ip.GetAddressBytes();
                
                // 10.0.0.0/8
                if (bytes[0] == 10)
                {
                    return true;
                }
                
                // 172.16.0.0/12
                if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                {
                    return true;
                }
                
                // 192.168.0.0/16
                if (bytes[0] == 192 && bytes[1] == 168)
                {
                    return true;
                }
            }
            
            // IPv6私有地址范围（简化处理）
            if (ip.AddressFamily == AddressFamily.InterNetworkV6)
            {
                // fc00::/7 (私有单播地址)
                var bytes = ip.GetAddressBytes();
                if ((bytes[0] & 0xfe) == 0xfc)
                {
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "解析IP地址时发生异常: {IpAddress}", ipAddress);
        }

        return false;
    }
} 