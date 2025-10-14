using CodeSpirit.ExamApi.LoadTests.Models;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace CodeSpirit.ExamApi.LoadTests.Services;

/// <summary>
/// 认证服务，用于获取JWT Token
/// </summary>
public class AuthenticationService
{
    private readonly HttpClient _httpClient;
    private readonly string _identityApiUrl;
    private readonly string _tenantId;
    private readonly string _clientType;
    private readonly Dictionary<string, (string Token, DateTime Expiry)> _tokenCache;
    private readonly SemaphoreSlim _semaphore;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="httpClient">HTTP客户端</param>
    /// <param name="identityApiUrl">身份认证API地址</param>
    /// <param name="tenantId">租户ID</param>
    /// <param name="clientType">客户端类型</param>
    public AuthenticationService(HttpClient httpClient, string identityApiUrl, string tenantId, string clientType = "exam")
    {
        _httpClient = httpClient;
        _identityApiUrl = identityApiUrl;
        _tenantId = tenantId;
        _clientType = clientType;
        _tokenCache = new Dictionary<string, (string Token, DateTime Expiry)>();
        _semaphore = new SemaphoreSlim(1, 1);
    }

    /// <summary>
    /// 获取用户认证Token
    /// </summary>
    /// <param name="username">用户名</param>
    /// <param name="password">密码</param>
    /// <returns>JWT Token</returns>
    public async Task<string> GetAuthTokenAsync(string username, string password)
    {
        var cacheKey = $"{username}:{password}";
        
        // 检查缓存
        if (_tokenCache.TryGetValue(cacheKey, out var cachedTokenInfo))
        {
            if (cachedTokenInfo.Expiry > DateTime.UtcNow.AddMinutes(5)) // 提前5分钟刷新
            {
                return cachedTokenInfo.Token;
            }
        }

        await _semaphore.WaitAsync();
        try
        {
            // 双重检查
            if (_tokenCache.TryGetValue(cacheKey, out cachedTokenInfo) && 
                cachedTokenInfo.Expiry > DateTime.UtcNow.AddMinutes(5))
            {
                return cachedTokenInfo.Token;
            }

            // 客户端登录请求
            var loginRequest = new ClientLoginRequest
            {
                UserName = username,
                Password = password,
                TenantId = _tenantId,
                ClientType = _clientType
            };

            var jsonContent = JsonConvert.SerializeObject(loginRequest);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            // 设置请求头
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("TenantId", _tenantId);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "NBomber-LoadTest/1.0");

            // 调用客户端登录接口
            var response = await _httpClient.PostAsync($"{_identityApiUrl}/api/identity/auth/client/login", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var loginResponse = JsonConvert.DeserializeObject<LoginResponse>(responseContent);
                
                if (loginResponse?.Code == 0 && loginResponse.Data?.Token != null)
                {
                    // 计算Token过期时间（假设Token有效期为1小时）
                    var expiry = DateTime.UtcNow.AddHours(1);
                    _tokenCache[cacheKey] = (loginResponse.Data.Token, expiry);
                    return loginResponse.Data.Token;
                }
                else
                {
                    throw new InvalidOperationException($"登录失败: {loginResponse?.Message ?? "未知错误"}");
                }
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"登录请求失败: {username}, 状态码: {response.StatusCode}, 响应: {errorContent}");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// 清除Token缓存
    /// </summary>
    public void ClearTokenCache()
    {
        _tokenCache.Clear();
    }

    /// <summary>
    /// 获取缓存的Token数量
    /// </summary>
    public int GetCachedTokenCount()
    {
        return _tokenCache.Count;
    }
}
