#nullable enable
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CodeSpirit.Shared.Authentication;

/// <summary>
/// API Key 认证处理器
/// 从 X-API-Key 请求头读取 API Key 并验证
/// </summary>
public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ApiKeyAuthenticationHandler> _logger;
    private const string ApiKeyHeaderName = "X-API-Key";

    /// <summary>
    /// 构造函数
    /// </summary>
    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IHttpClientFactory httpClientFactory)
        : base(options, logger, encoder)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger.CreateLogger<ApiKeyAuthenticationHandler>();
    }

    /// <summary>
    /// 执行认证
    /// </summary>
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        _logger.LogDebug("开始 API Key 认证处理，请求路径：{Path}", Request.Path);
        
        // 检查请求头是否包含 API Key
        if (!Request.Headers.TryGetValue(ApiKeyHeaderName, out var apiKeyValues))
        {
            _logger.LogDebug("请求头中未找到 {HeaderName}，跳过 API Key 认证", ApiKeyHeaderName);
            // 没有 API Key，不处理（让其他认证方案处理）
            return AuthenticateResult.NoResult();
        }

        var apiKey = apiKeyValues.ToString();
        _logger.LogDebug("从请求头获取到 API Key：{ApiKeyPrefix}... (长度: {Length})", 
            apiKey.Length > 10 ? apiKey[..10] : apiKey, 
            apiKey.Length);
            
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("API Key 为空");
            return AuthenticateResult.Fail("API Key 为空");
        }

        try
        {
            _logger.LogDebug("开始调用 IdentityApi 验证 API Key");
            
            // 调用 IdentityApi 验证 API Key
            var validationResult = await ValidateApiKeyAsync(apiKey);
            
            if (validationResult == null)
            {
                _logger.LogWarning("API Key 验证失败：密钥无效或过期");
                return AuthenticateResult.Fail("API Key 无效");
            }

            _logger.LogDebug("API Key 验证成功，用户信息：UserId={UserId}, UserName={UserName}, TenantId={TenantId}, TenantName={TenantName}, ApiKeyId={ApiKeyId}", 
                validationResult.UserId, validationResult.UserName, validationResult.TenantId, validationResult.TenantName, validationResult.ApiKeyId);

            // 构建 Claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, validationResult.UserId.ToString()),
                new Claim("id", validationResult.UserId.ToString()),
                new Claim(ClaimTypes.Name, validationResult.UserName),
                new Claim("TenantId", validationResult.TenantId),
                new Claim("ApiKeyId", validationResult.ApiKeyId.ToString()),
                new Claim("AuthType", "ApiKey")
            };

            // 添加租户名称（如果有）
            if (!string.IsNullOrEmpty(validationResult.TenantName))
            {
                claims.Add(new Claim("TenantName", validationResult.TenantName));
            }

            // 添加角色Claims（如果有）
            if (validationResult.Roles != null && validationResult.Roles.Count > 0)
            {
                _logger.LogDebug("添加 {Count} 个角色 Claims", validationResult.Roles.Count);
                foreach (var role in validationResult.Roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }
            }

            // 添加权限Claims（如果有）
            if (validationResult.Permissions != null && validationResult.Permissions.Count > 0)
            {
                _logger.LogDebug("添加 {Count} 个权限 Claims", validationResult.Permissions.Count);
                foreach (var permission in validationResult.Permissions)
                {
                    claims.Add(new Claim("Permission", permission));
                }
            }
            else
            {
                _logger.LogDebug("该 API Key 没有分配任何权限");
            }

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            _logger.LogInformation("✅ API Key 认证成功：UserId={UserId}, UserName={UserName}, TenantId={TenantId}, TenantName={TenantName}, RoleCount={RoleCount}, PermissionCount={PermissionCount}", 
                validationResult.UserId, validationResult.UserName, validationResult.TenantId, validationResult.TenantName,
                validationResult.Roles?.Count ?? 0, validationResult.Permissions?.Count ?? 0);

            return AuthenticateResult.Success(ticket);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ API Key 认证过程中发生异常");
            return AuthenticateResult.Fail($"API Key 认证失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 调用 IdentityApi 验证 API Key
    /// </summary>
    private async Task<ApiKeyValidationResult?> ValidateApiKeyAsync(string apiKey)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var identityApiUrl = Options.IdentityApiBaseUrl?.TrimEnd('/') ?? "http://localhost:5001";
            
            // 正确的路径：internal/api-keys/validate，使用 GET 请求，apiKey 作为查询参数
            var encodedApiKey = Uri.EscapeDataString(apiKey);
            var url = $"{identityApiUrl}/internal/api-keys/validate?apiKey={encodedApiKey}";

            _logger.LogDebug("准备调用 IdentityApi 验证接口：{Url}", url.Replace(encodedApiKey, "***"));
            _logger.LogDebug("IdentityApiBaseUrl 配置值：{BaseUrl}", Options.IdentityApiBaseUrl ?? "(未配置，使用默认值)");

            var request = new HttpRequestMessage(HttpMethod.Get, url);

            // 打印请求头信息用于调试
            _logger.LogDebug("发送 HTTP GET 请求到 IdentityApi，API Key 作为查询参数传递");
            _logger.LogDebug("HttpClient 请求头：{Headers}", string.Join(", ", request.Headers.Select(h => $"{h.Key}={string.Join(",", h.Value)}")));

            var response = await httpClient.SendAsync(request);

            _logger.LogDebug("收到 IdentityApi 响应，状态码：{StatusCode}", response.StatusCode);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("验证 API Key 失败，状态码：{StatusCode}，响应内容：{Content}", 
                    response.StatusCode, 
                    errorContent.Length > 500 ? errorContent[..500] + "..." : errorContent);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("IdentityApi 返回 JSON 响应（长度：{Length} 字节）：{Json}", 
                json.Length, 
                json.Length > 500 ? json[..500] + "..." : json);

            // 解析 ApiResponse<ApiKey> 结构
            var apiResponse = System.Text.Json.JsonSerializer.Deserialize<ApiKeyApiResponse>(json, 
                new System.Text.Json.JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });

            if (apiResponse == null)
            {
                _logger.LogWarning("反序列化 API Key 验证响应失败：apiResponse 为 null");
                return null;
            }

            if (apiResponse.Code != 0)
            {
                _logger.LogWarning("API Key 验证未成功，错误码：{Code}，消息：{Message}", 
                    apiResponse.Code, apiResponse.Message ?? "(无消息)");
                return null;
            }

            if (apiResponse.Data == null)
            {
                _logger.LogWarning("API Key 验证响应中 Data 为 null");
                return null;
            }

            var validationData = apiResponse.Data;
            
            // 解析 Permissions JSON 字符串
            List<string>? permissions = null;
            if (!string.IsNullOrEmpty(validationData.Permissions))
            {
                try
                {
                    permissions = System.Text.Json.JsonSerializer.Deserialize<List<string>>(validationData.Permissions);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "解析权限 JSON 失败：{Permissions}", validationData.Permissions);
                }
            }

            // 解析 Roles JSON 字符串
            List<string>? roles = null;
            if (!string.IsNullOrEmpty(validationData.Roles))
            {
                try
                {
                    roles = System.Text.Json.JsonSerializer.Deserialize<List<string>>(validationData.Roles);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "解析角色 JSON 失败：{Roles}", validationData.Roles);
                }
            }

            // 将字符串类型的 ID 转换为 long
            if (!long.TryParse(validationData.UserId, out var userId))
            {
                _logger.LogWarning("无效的 UserId 格式：{UserId}", validationData.UserId);
                return null;
            }

            if (!long.TryParse(validationData.Id, out var apiKeyId))
            {
                _logger.LogWarning("无效的 ApiKeyId 格式：{ApiKeyId}", validationData.Id);
                return null;
            }

            var result = new ApiKeyValidationResult
            {
                UserId = userId,
                TenantId = validationData.TenantId,
                TenantName = validationData.TenantName,
                UserName = validationData.User?.UserName ?? $"User_{userId}",
                ApiKeyId = apiKeyId,
                Roles = roles,
                Permissions = permissions
            };

            _logger.LogDebug("成功解析验证结果：UserId={UserId}, UserName={UserName}, TenantId={TenantId}, TenantName={TenantName}, RoleCount={RoleCount}, PermissionCount={PermissionCount}", 
                result.UserId, result.UserName, result.TenantId, result.TenantName, 
                result.Roles?.Count ?? 0, result.Permissions?.Count ?? 0);

            return result;
        }
        catch (HttpRequestException httpEx)
        {
            _logger.LogError(httpEx, "HTTP 请求失败：无法连接到 IdentityApi（{Url}）", 
                Options.IdentityApiBaseUrl ?? "http://localhost:5001");
            return null;
        }
        catch (System.Text.Json.JsonException jsonEx)
        {
            _logger.LogError(jsonEx, "JSON 反序列化失败：响应内容格式不正确");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "调用 IdentityApi 验证 API Key 时发生未知异常");
            return null;
        }
    }
}

/// <summary>
/// API Key 认证选项
/// </summary>
public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    /// <summary>
    /// IdentityApi 基础 URL
    /// </summary>
    public string? IdentityApiBaseUrl { get; set; }
}

/// <summary>
/// API Key 验证结果
/// </summary>
public class ApiKeyValidationResult
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 租户ID
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// 租户名称
    /// </summary>
    public string? TenantName { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// API密钥ID
    /// </summary>
    public long ApiKeyId { get; set; }

    /// <summary>
    /// 角色列表
    /// </summary>
    public List<string>? Roles { get; set; }

    /// <summary>
    /// 权限列表
    /// </summary>
    public List<string>? Permissions { get; set; }
}

/// <summary>
/// API Key API 响应（匹配 ApiResponse 结构）
/// </summary>
public class ApiKeyApiResponse
{
    /// <summary>
    /// 错误码（0 表示成功）
    /// </summary>
    public int Code { get; set; }

    /// <summary>
    /// 消息
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// API Key 验证数据（匹配 ApiKeyValidationDto）
    /// </summary>
    public ApiKeyValidationData? Data { get; set; }
}

/// <summary>
/// API Key 验证数据（匹配 ApiKeyValidationDto 结构）
/// </summary>
public class ApiKeyValidationData
{
    /// <summary>
    /// API密钥ID（字符串类型，用于支持大数字）
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 租户ID
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// 租户名称
    /// </summary>
    public string? TenantName { get; set; }

    /// <summary>
    /// 关联的用户ID（字符串类型，用于支持大数字）
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// 用户信息
    /// </summary>
    public ApiKeyUserData? User { get; set; }

    /// <summary>
    /// 角色配置（JSON字符串）
    /// </summary>
    public string? Roles { get; set; }

    /// <summary>
    /// 权限配置（JSON字符串）
    /// </summary>
    public string? Permissions { get; set; }
}

/// <summary>
/// API Key 用户数据（匹配 ApiKeyUserDto）
/// </summary>
public class ApiKeyUserData
{
    /// <summary>
    /// 用户ID（字符串类型，用于支持大数字）
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 用户名
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 姓名
    /// </summary>
    public string Name { get; set; } = string.Empty;
}

