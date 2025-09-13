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
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ApiTenantStore> _logger;
    private readonly ApiTenantStoreOptions _options;
    private readonly string _clientName;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="httpClientFactory">HTTP客户端工厂</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="options">API租户存储配置选项</param>
    /// <param name="clientName">HTTP客户端名称</param>
    public ApiTenantStore(
        IHttpClientFactory httpClientFactory, 
        ILogger<ApiTenantStore> logger,
        IOptions<ApiTenantStoreOptions> options,
        string clientName = "ApiTenantStore")
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _clientName = clientName ?? "ApiTenantStore";
    }

    /// <summary>
    /// 获取租户信息
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <returns>租户信息</returns>
    public async Task<ITenantInfo> GetTenantAsync(string tenantId)
    {
        _logger.LogWarning("==== ApiTenantStore.GetTenantAsync 被调用，租户ID: {TenantId}，HttpClient名称: {ClientName} ====", tenantId, _clientName);
        
        try
        {
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                return null;
            }

            using var httpClient = _httpClientFactory.CreateClient(_clientName);
            var endpoint = _options.GetTenantEndpoint.Replace("{tenantId}", tenantId);
            _logger.LogInformation("调用API获取租户信息: GET {Endpoint}，BaseAddress: {BaseAddress}", endpoint, httpClient.BaseAddress);

            var response = await httpClient.GetAsync(endpoint);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                
                // 如果响应是ApiResponse格式，需要解析Data字段
                if (_options.UseApiResponseFormat)
                {
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<InternalTenantDto>>(content);
                    if (apiResponse?.Status == 0 && apiResponse.Data != null)
                    {
                        _logger.LogDebug("成功获取租户信息: {TenantId}", tenantId);
                        return MapToTenantInfo(apiResponse.Data);
                    }
                    
                    _logger.LogWarning("API返回错误: {Message}", apiResponse?.Msg);
                    return null;
                }
                else
                {
                    var internalDto = JsonConvert.DeserializeObject<InternalTenantDto>(content);
                    if (internalDto != null)
                    {
                        _logger.LogDebug("成功获取租户信息: {TenantId}", tenantId);
                        return MapToTenantInfo(internalDto);
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
            var endpoint = _options.GetTenantEndpoint.Replace("{tenantId}", tenantId);
            _logger.LogError(ex, "HTTP请求异常，获取租户信息失败: {TenantId}，端点: {Endpoint}，错误: {Message}", 
                tenantId, endpoint, ex.Message);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            var endpoint = _options.GetTenantEndpoint.Replace("{tenantId}", tenantId);
            _logger.LogError(ex, "请求超时，获取租户信息失败: {TenantId}，端点: {Endpoint}，超时时间: {Timeout}ms", 
                tenantId, endpoint, ex.ToString());
        }
        catch (TaskCanceledException ex)
        {
            var endpoint = _options.GetTenantEndpoint.Replace("{tenantId}", tenantId);
            _logger.LogError(ex, "请求被取消，获取租户信息失败: {TenantId}，端点: {Endpoint}", 
                tenantId, endpoint);
        }
        catch (JsonException ex)
        {
            var endpoint = _options.GetTenantEndpoint.Replace("{tenantId}", tenantId);
            _logger.LogError(ex, "JSON反序列化失败，获取租户信息失败: {TenantId}，端点: {Endpoint}，错误: {Message}", 
                tenantId, endpoint, ex.Message);
        }
        catch (Exception ex)
        {
            var endpoint = _options.GetTenantEndpoint.Replace("{tenantId}", tenantId);
            _logger.LogError(ex, "获取租户信息时发生未知异常: {TenantId}，端点: {Endpoint}，异常类型: {ExceptionType}，错误: {Message}", 
                tenantId, endpoint, ex.GetType().Name, ex.Message);
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
            using var httpClient = _httpClientFactory.CreateClient(_clientName);
            _logger.LogDebug("调用API获取活跃租户列表: GET {Endpoint}", _options.GetActiveTenantsEndpoint);

            var response = await httpClient.GetAsync(_options.GetActiveTenantsEndpoint);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                
                // 如果响应是ApiResponse格式，需要解析Data字段
                if (_options.UseApiResponseFormat)
                {
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<List<InternalTenantDto>>>(content);
                    if (apiResponse?.Status == 0 && apiResponse.Data != null)
                    {
                        _logger.LogDebug("成功获取活跃租户列表，数量: {Count}", apiResponse.Data.Count);
                        return apiResponse.Data.Select(MapToTenantInfo).Cast<ITenantInfo>();
                    }
                    
                    _logger.LogWarning("API返回错误: {Message}", apiResponse?.Msg);
                    return Enumerable.Empty<ITenantInfo>();
                }
                else
                {
                    var internalDtos = JsonConvert.DeserializeObject<List<InternalTenantDto>>(content);
                    if (internalDtos != null)
                    {
                        _logger.LogDebug("成功获取活跃租户列表，数量: {Count}", internalDtos.Count);
                        return internalDtos.Select(MapToTenantInfo).Cast<ITenantInfo>();
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

            using var httpClient = _httpClientFactory.CreateClient(_clientName);
            _logger.LogDebug("调用API创建租户: POST {Endpoint}", _options.CreateTenantEndpoint);

            var json = JsonConvert.SerializeObject(tenantInfo);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(_options.CreateTenantEndpoint, content);
            
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

            using var httpClient = _httpClientFactory.CreateClient(_clientName);
            var endpoint = _options.UpdateTenantEndpoint.Replace("{tenantId}", tenantInfo.TenantId);
            _logger.LogDebug("调用API更新租户: PUT {Endpoint}", endpoint);

            var json = JsonConvert.SerializeObject(tenantInfo);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PutAsync(endpoint, content);
            
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

            using var httpClient = _httpClientFactory.CreateClient(_clientName);
            var endpoint = _options.DeleteTenantEndpoint.Replace("{tenantId}", tenantId);
            _logger.LogDebug("调用API删除租户: DELETE {Endpoint}", endpoint);

            var response = await httpClient.DeleteAsync(endpoint);
            
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

            using var httpClient = _httpClientFactory.CreateClient(_clientName);
            var endpoint = _options.CheckTenantExistsEndpoint.Replace("{tenantId}", tenantId);
            _logger.LogDebug("调用API检查租户是否存在: HEAD {Endpoint}", endpoint);

            var response = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, endpoint));
            
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查租户是否存在时发生异常: {TenantId}", tenantId);
            return false;
        }
    }

    /// <summary>
    /// 将内部租户DTO映射为租户信息实体
    /// </summary>
    /// <param name="dto">内部租户DTO</param>
    /// <returns>租户信息实体</returns>
    private static TenantInfo MapToTenantInfo(InternalTenantDto dto)
    {
        return new TenantInfo
        {
            Id = dto.TenantId,
            TenantId = dto.TenantId,
            Name = dto.Name,
            DisplayName = dto.DisplayName,
            Description = dto.Description,
            Strategy = dto.Strategy,
            IsActive = dto.IsActive,
            Domain = dto.Domain,
            LogoUrl = dto.LogoUrl,
            MaxUsers = dto.MaxUsers,
            StorageLimit = dto.StorageLimit,
            ExpiresAt = dto.ExpiresAt,
            CreatedAt = dto.CreatedAt,
            ThemeConfig = dto.ThemeConfig ?? "{}",
            Configuration = dto.Configuration ?? "{}"
        };
    }
}

/// <summary>
/// 内部租户数据传输对象
/// 用于内部API调用，包含租户存储所需的基本信息
/// </summary>
public class InternalTenantDto
{
    /// <summary>
    /// 租户ID
    /// </summary>
    public string TenantId { get; set; }

    /// <summary>
    /// 租户名称
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 显示名称
    /// </summary>
    public string DisplayName { get; set; }

    /// <summary>
    /// 描述
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// 租户策略
    /// </summary>
    public TenantStrategy Strategy { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// 租户域名
    /// </summary>
    public string Domain { get; set; }

    /// <summary>
    /// 租户Logo URL
    /// </summary>
    public string LogoUrl { get; set; }

    /// <summary>
    /// 最大用户数
    /// </summary>
    public int MaxUsers { get; set; }

    /// <summary>
    /// 存储限制(MB)
    /// </summary>
    public long StorageLimit { get; set; }

    /// <summary>
    /// 过期时间
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 主题配置
    /// </summary>
    public string ThemeConfig { get; set; }

    /// <summary>
    /// 功能配置
    /// </summary>
    public string Configuration { get; set; }
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
    public string BaseUrl { get; set; } = "http://identity";

    /// <summary>
    /// 请求超时时间（秒）
    /// </summary>
    public int Timeout { get; set; } = 2;

    /// <summary>
    /// 是否使用ApiResponse格式
    /// </summary>
    public bool UseApiResponseFormat { get; set; } = true;

    /// <summary>
    /// 获取租户信息端点
    /// </summary>
    public string GetTenantEndpoint { get; set; } = "api/identity/internal/tenants/{tenantId}";

    /// <summary>
    /// 获取活跃租户列表端点
    /// </summary>
    public string GetActiveTenantsEndpoint { get; set; } = "api/identity/internal/tenants/active";

    /// <summary>
    /// 创建租户端点
    /// </summary>
    public string CreateTenantEndpoint { get; set; } = "api/identity/internal/tenants";

    /// <summary>
    /// 更新租户端点
    /// </summary>
    public string UpdateTenantEndpoint { get; set; } = "api/identity/internal/tenants/{tenantId}";

    /// <summary>
    /// 删除租户端点
    /// </summary>
    public string DeleteTenantEndpoint { get; set; } = "api/identity/internal/tenants/{tenantId}";

    /// <summary>
    /// 检查租户是否存在端点（通过HEAD请求获取租户详情判断）
    /// </summary>
    public string CheckTenantExistsEndpoint { get; set; } = "api/identity/internal/tenants/{tenantId}";
} 