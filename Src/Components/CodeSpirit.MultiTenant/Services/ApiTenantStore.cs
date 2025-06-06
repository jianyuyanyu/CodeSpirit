using CodeSpirit.Core;
using CodeSpirit.MultiTenant.Abstractions;
using CodeSpirit.MultiTenant.Models;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Text;

namespace CodeSpirit.MultiTenant.Services;

/// <summary>
/// API租户存储实现
/// 通过HTTP API调用获取和管理租户信息
/// </summary>
public class ApiTenantStore : ITenantStore
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ApiTenantStore> _logger;
    private readonly ApiTenantStoreOptions _options;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="httpClient">HTTP客户端</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="options">API租户存储配置选项</param>
    public ApiTenantStore(
        HttpClient httpClient, 
        ILogger<ApiTenantStore> logger,
        IOptions<ApiTenantStoreOptions> options)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options.Value;

        // 配置HttpClient基础设置
        if (!string.IsNullOrEmpty(_options.BaseUrl))
        {
            _httpClient.BaseAddress = new Uri(_options.BaseUrl);
        }

        if (_options.Timeout > 0)
        {
            _httpClient.Timeout = TimeSpan.FromSeconds(_options.Timeout);
        }
    }

    /// <summary>
    /// 获取租户信息
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <returns>租户信息</returns>
    public async Task<ITenantInfo?> GetTenantAsync(string tenantId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                return null;
            }

            var endpoint = _options.GetTenantEndpoint.Replace("{tenantId}", tenantId);
            _logger.LogDebug("调用API获取租户信息: GET {Endpoint}", endpoint);

            var response = await _httpClient.GetAsync(endpoint);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                
                // 如果响应是ApiResponse格式，需要解析Data字段
                if (_options.UseApiResponseFormat)
                {
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<TenantInfo>>(content);
                    if (apiResponse?.Status == 0 && apiResponse.Data != null)
                    {
                        _logger.LogDebug("成功获取租户信息: {TenantId}", tenantId);
                        return apiResponse.Data;
                    }
                    
                    _logger.LogWarning("API返回错误: {Message}", apiResponse?.Msg);
                    return null;
                }
                else
                {
                    var tenantInfo = JsonConvert.DeserializeObject<TenantInfo>(content);
                    if (tenantInfo != null)
                    {
                        _logger.LogDebug("成功获取租户信息: {TenantId}", tenantId);
                        return tenantInfo;
                    }
                }
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogDebug("租户不存在: {TenantId}", tenantId);
                return null;
            }
            else
            {
                _logger.LogWarning("获取租户信息失败: {StatusCode} - {ReasonPhrase}", 
                    response.StatusCode, response.ReasonPhrase);
                return null;
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP请求异常，获取租户信息失败: {TenantId}", tenantId);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "请求超时，获取租户信息失败: {TenantId}", tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取租户信息时发生异常: {TenantId}", tenantId);
        }

        return null;
    }

    /// <summary>
    /// 获取所有活跃租户
    /// </summary>
    /// <returns>活跃租户列表</returns>
    public async Task<IEnumerable<ITenantInfo>> GetActiveTenantsAsync()
    {
        try
        {
            _logger.LogDebug("调用API获取活跃租户列表: GET {Endpoint}", _options.GetActiveTenantsEndpoint);

            var response = await _httpClient.GetAsync(_options.GetActiveTenantsEndpoint);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                
                // 如果响应是ApiResponse格式，需要解析Data字段
                if (_options.UseApiResponseFormat)
                {
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<List<TenantInfo>>>(content);
                    if (apiResponse?.Status == 0 && apiResponse.Data != null)
                    {
                        _logger.LogDebug("成功获取活跃租户列表，数量: {Count}", apiResponse.Data.Count);
                        return apiResponse.Data.Cast<ITenantInfo>();
                    }
                    
                    _logger.LogWarning("API返回错误: {Message}", apiResponse?.Msg);
                    return Enumerable.Empty<ITenantInfo>();
                }
                else
                {
                    var tenants = JsonConvert.DeserializeObject<List<TenantInfo>>(content);
                    if (tenants != null)
                    {
                        _logger.LogDebug("成功获取活跃租户列表，数量: {Count}", tenants.Count);
                        return tenants.Cast<ITenantInfo>();
                    }
                }
            }
            else
            {
                _logger.LogWarning("获取活跃租户列表失败: {StatusCode} - {ReasonPhrase}", 
                    response.StatusCode, response.ReasonPhrase);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取活跃租户列表时发生异常");
        }

        return Enumerable.Empty<ITenantInfo>();
    }

    /// <summary>
    /// 创建租户
    /// </summary>
    /// <param name="tenantInfo">租户信息</param>
    /// <returns>创建结果</returns>
    public async Task<bool> CreateTenantAsync(ITenantInfo tenantInfo)
    {
        try
        {
            if (tenantInfo == null)
            {
                return false;
            }

            _logger.LogDebug("调用API创建租户: POST {Endpoint}", _options.CreateTenantEndpoint);

            var json = JsonConvert.SerializeObject(tenantInfo);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_options.CreateTenantEndpoint, content);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("成功创建租户: {TenantId}", tenantInfo.TenantId);
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("创建租户失败: {StatusCode} - {Content}", 
                    response.StatusCode, errorContent);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建租户时发生异常: {TenantId}", tenantInfo?.TenantId);
            return false;
        }
    }

    /// <summary>
    /// 更新租户
    /// </summary>
    /// <param name="tenantInfo">租户信息</param>
    /// <returns>更新结果</returns>
    public async Task<bool> UpdateTenantAsync(ITenantInfo tenantInfo)
    {
        try
        {
            if (tenantInfo == null)
            {
                return false;
            }

            var endpoint = _options.UpdateTenantEndpoint.Replace("{tenantId}", tenantInfo.TenantId);
            _logger.LogDebug("调用API更新租户: PUT {Endpoint}", endpoint);

            var json = JsonConvert.SerializeObject(tenantInfo);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync(endpoint, content);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("成功更新租户: {TenantId}", tenantInfo.TenantId);
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("更新租户失败: {StatusCode} - {Content}", 
                    response.StatusCode, errorContent);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新租户时发生异常: {TenantId}", tenantInfo?.TenantId);
            return false;
        }
    }

    /// <summary>
    /// 删除租户
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <returns>删除结果</returns>
    public async Task<bool> DeleteTenantAsync(string tenantId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                return false;
            }

            var endpoint = _options.DeleteTenantEndpoint.Replace("{tenantId}", tenantId);
            _logger.LogDebug("调用API删除租户: DELETE {Endpoint}", endpoint);

            var response = await _httpClient.DeleteAsync(endpoint);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("成功删除租户: {TenantId}", tenantId);
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("删除租户失败: {StatusCode} - {Content}", 
                    response.StatusCode, errorContent);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除租户时发生异常: {TenantId}", tenantId);
            return false;
        }
    }

    /// <summary>
    /// 检查租户是否存在
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <returns>是否存在</returns>
    public async Task<bool> TenantExistsAsync(string tenantId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                return false;
            }

            var endpoint = _options.CheckTenantExistsEndpoint.Replace("{tenantId}", tenantId);
            _logger.LogDebug("调用API检查租户是否存在: HEAD {Endpoint}", endpoint);

            var response = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, endpoint));
            
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查租户是否存在时发生异常: {TenantId}", tenantId);
            return false;
        }
    }
}

/// <summary>
/// API租户存储配置选项
/// </summary>
public class ApiTenantStoreOptions
{
    /// <summary>
    /// 配置节名称
    /// </summary>
    public const string SectionName = "MultiTenant:ApiStore";

    /// <summary>
    /// API基础URL（支持内部服务发现，如：http://identity-api 或 https://identity-api.default.svc.cluster.local）
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// 请求超时时间（秒）
    /// </summary>
    public int Timeout { get; set; } = 30;

    /// <summary>
    /// 是否使用ApiResponse格式
    /// </summary>
    public bool UseApiResponseFormat { get; set; } = true;

    /// <summary>
    /// 获取租户信息端点
    /// </summary>
    public string GetTenantEndpoint { get; set; } = "api/tenants/{tenantId}";

    /// <summary>
    /// 获取活跃租户列表端点
    /// </summary>
    public string GetActiveTenantsEndpoint { get; set; } = "api/tenants/active";

    /// <summary>
    /// 创建租户端点
    /// </summary>
    public string CreateTenantEndpoint { get; set; } = "api/tenants";

    /// <summary>
    /// 更新租户端点
    /// </summary>
    public string UpdateTenantEndpoint { get; set; } = "api/tenants/{tenantId}";

    /// <summary>
    /// 删除租户端点
    /// </summary>
    public string DeleteTenantEndpoint { get; set; } = "api/tenants/{tenantId}";

    /// <summary>
    /// 检查租户是否存在端点（通过HEAD请求获取租户详情判断）
    /// </summary>
    public string CheckTenantExistsEndpoint { get; set; } = "api/tenants/{tenantId}";
} 