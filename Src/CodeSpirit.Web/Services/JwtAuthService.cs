using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Text.Json;

namespace CodeSpirit.Web.Services;

/// <summary>
/// 提供JWT令牌管理和用户信息提取的服务
/// </summary>
public interface IJwtAuthService
{
    /// <summary>
    /// 检查用户是否已认证
    /// </summary>
    /// <returns>如果用户已认证则返回true</returns>
    Task<bool> IsAuthenticatedAsync();
    
    /// <summary>
    /// 获取当前用户ID
    /// </summary>
    /// <returns>用户ID，未认证返回null</returns>
    Task<string> GetUserIdAsync();
    
    /// <summary>
    /// 获取当前用户名
    /// </summary>
    /// <returns>用户名，未认证返回null</returns>
    Task<string> GetUsernameAsync();
    
    /// <summary>
    /// 获取JWT令牌
    /// </summary>
    /// <returns>令牌字符串，未认证返回null</returns>
    Task<string> GetTokenAsync();
    
    /// <summary>
    /// 注销登录
    /// </summary>
    Task LogoutAsync();
}

/// <summary>
/// JWT认证服务实现
/// </summary>
public class JwtAuthService : IJwtAuthService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly NavigationManager _navigationManager;
    
    // 缓存用户信息以避免重复解析
    private string _cachedUserId;
    private string _cachedUsername;
    private bool? _cachedIsAuthenticated;
    
    public JwtAuthService(IJSRuntime jsRuntime, NavigationManager navigationManager)
    {
        _jsRuntime = jsRuntime;
        _navigationManager = navigationManager;
    }
    
    /// <inheritdoc />
    public async Task<bool> IsAuthenticatedAsync()
    {
        // 如果已缓存，则返回缓存值
        if (_cachedIsAuthenticated.HasValue)
        {
            return _cachedIsAuthenticated.Value;
        }
        
        try
        {
            var hasToken = await _jsRuntime.InvokeAsync<bool>("eval", 
                "typeof TokenManager !== 'undefined' && TokenManager.hasToken() && !TokenManager.isTokenExpired()");
            
            _cachedIsAuthenticated = hasToken;
            return hasToken;
        }
        catch
        {
            // 当JS交互失败时（如服务器预渲染），默认为未认证
            _cachedIsAuthenticated = false;
            return false;
        }
    }
    
    /// <inheritdoc />
    public async Task<string> GetUserIdAsync()
    {
        if (!string.IsNullOrEmpty(_cachedUserId))
        {
            return _cachedUserId;
        }
        
        if (!await IsAuthenticatedAsync())
        {
            return null;
        }
        
        try
        {
            var userId = await _jsRuntime.InvokeAsync<string>("eval", 
                "(() => { try { const token = TokenManager.getToken(); " +
                "if (!token) return null; " +
                "const base64Url = token.split('.')[1]; " +
                "const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/'); " +
                "const payload = JSON.parse(window.atob(base64)); " +
                "return payload.nameid || payload.sub || ''; } catch(e) { console.error(e); return ''; } })()");
            
            _cachedUserId = userId;
            return userId;
        }
        catch
        {
            return null;
        }
    }
    
    /// <inheritdoc />
    public async Task<string> GetUsernameAsync()
    {
        if (!string.IsNullOrEmpty(_cachedUsername))
        {
            return _cachedUsername;
        }
        
        if (!await IsAuthenticatedAsync())
        {
            return null;
        }
        
        try
        {
            var username = await _jsRuntime.InvokeAsync<string>("eval", 
                "(() => { try { const token = TokenManager.getToken(); " +
                "if (!token) return null; " +
                "const base64Url = token.split('.')[1]; " +
                "const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/'); " +
                "const payload = JSON.parse(window.atob(base64)); " +
                "return payload.name || payload.unique_name || '用户'; } catch(e) { console.error(e); return '用户'; } })()");
            
            _cachedUsername = username;
            return username;
        }
        catch
        {
            return "用户";
        }
    }
    
    /// <inheritdoc />
    public async Task<string> GetTokenAsync()
    {
        if (!await IsAuthenticatedAsync())
        {
            return null;
        }
        
        try
        {
            return await _jsRuntime.InvokeAsync<string>("eval", "TokenManager.getToken() || ''");
        }
        catch
        {
            return null;
        }
    }
    
    /// <inheritdoc />
    public async Task LogoutAsync()
    {
        try
        {
            // 清除前端存储的 JWT token
            await _jsRuntime.InvokeVoidAsync("eval", "if(typeof TokenManager !== 'undefined') TokenManager.clearToken()");
            
            // 清除缓存
            _cachedIsAuthenticated = false;
            _cachedUserId = null;
            _cachedUsername = null;
            
            // 重定向到首页
            _navigationManager.NavigateTo("/", forceLoad: true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during logout: {ex.Message}");
        }
    }
} 