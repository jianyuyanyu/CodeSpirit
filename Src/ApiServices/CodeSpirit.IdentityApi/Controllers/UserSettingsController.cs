using CodeSpirit.Amis.Attributes;
using CodeSpirit.Core;
using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Enums;
using CodeSpirit.IdentityApi.Dtos.Settings;
using CodeSpirit.Settings.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace CodeSpirit.IdentityApi.Controllers;

/// <summary>
/// 用户设置控制器
/// 统一管理用户中心的各类设置
/// </summary>
[DisplayName("用户设置")]
[Navigation(Icon = "fa-solid fa-user-cog", Order = 150, PlatformType = PlatformType.Tenant)]
[SettingsPage(Title = "用户设置", Description = "管理用户偏好和系统配置")]
public class UserSettingsController : ApiControllerBase
{
    private readonly ISettingsService _settingsService;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<UserSettingsController> _logger;
    
    /// <summary>
    /// 初始化用户设置控制器
    /// </summary>
    /// <param name="settingsService">设置服务</param>
    /// <param name="currentUser">当前用户</param>
    /// <param name="logger">日志记录器</param>
    public UserSettingsController(
        ISettingsService settingsService,
        ICurrentUser currentUser,
        ILogger<UserSettingsController> logger)
    {
        _settingsService = settingsService;
        _currentUser = currentUser;
        _logger = logger;
    }

    #region 微信登录设置
    
    /// <summary>
    /// 获取微信登录设置
    /// </summary>
    /// <returns>微信登录设置</returns>
    [HttpGet("wechat-login")]
    [DisplayName("获取微信登录设置")]
    public async Task<ActionResult<ApiResponse<WeChatLoginSettingsDto>>> GetWeChatLoginSettings()
    {
        var tenantId = _currentUser.TenantId ?? "default";
        var settings = await _settingsService.GetTenantSettingAsync<WeChatLoginSettingsDto>(tenantId);
        return SuccessResponse(settings ?? new WeChatLoginSettingsDto());
    }
    
    /// <summary>
    /// 保存微信登录设置
    /// </summary>
    /// <param name="dto">微信登录设置DTO</param>
    /// <returns>操作结果</returns>
    [HttpPut("wechat-login")]
    [DisplayName("保存微信登录设置")]
    [HeaderOperation("微信登录", "form", Icon = "fa-brands fa-weixin", DialogSize = DialogSize.LG)]
    public async Task<ActionResult<ApiResponse>> SaveWeChatLoginSettings([FromBody] WeChatLoginSettingsDto dto)
    {
        var tenantId = _currentUser.TenantId ?? "default";
        var success = await _settingsService.SetTenantSettingAsync(
            dto,
            tenantId,
            $"用户 {_currentUser.UserName} 更新微信登录设置");
        
        return success ? SuccessResponse("微信登录设置保存成功") : BadResponse("保存设置失败");
    }
    
    #endregion

    #region 支付宝登录设置
    
    /// <summary>
    /// 获取支付宝登录设置
    /// </summary>
    /// <returns>支付宝登录设置</returns>
    [HttpGet("alipay-login")]
    [DisplayName("获取支付宝登录设置")]
    public async Task<ActionResult<ApiResponse<AlipayLoginSettingsDto>>> GetAlipayLoginSettings()
    {
        var tenantId = _currentUser.TenantId ?? "default";
        var settings = await _settingsService.GetTenantSettingAsync<AlipayLoginSettingsDto>(tenantId);
        return SuccessResponse(settings ?? new AlipayLoginSettingsDto());
    }
    
    /// <summary>
    /// 保存支付宝登录设置
    /// </summary>
    /// <param name="dto">支付宝登录设置DTO</param>
    /// <returns>操作结果</returns>
    [HttpPut("alipay-login")]
    [DisplayName("保存支付宝登录设置")]
    [HeaderOperation("支付宝登录", "form", Icon = "fa-brands fa-alipay", DialogSize = DialogSize.LG)]
    public async Task<ActionResult<ApiResponse>> SaveAlipayLoginSettings([FromBody] AlipayLoginSettingsDto dto)
    {
        var tenantId = _currentUser.TenantId ?? "default";
        var success = await _settingsService.SetTenantSettingAsync(
            dto,
            tenantId,
            $"用户 {_currentUser.UserName} 更新支付宝登录设置");
        
        return success ? SuccessResponse("支付宝登录设置保存成功") : BadResponse("保存设置失败");
    }
    
    #endregion

    #region 通知设置
    
    /// <summary>
    /// 获取通知设置
    /// </summary>
    /// <returns>通知设置</returns>
    [HttpGet("notification")]
    [DisplayName("获取通知设置")]
    public async Task<ActionResult<ApiResponse<NotificationSettingsDto>>> GetNotificationSettings()
    {
        var tenantId = _currentUser.TenantId ?? "default";
        var settings = await _settingsService.GetTenantSettingAsync<NotificationSettingsDto>(tenantId);
        return SuccessResponse(settings ?? new NotificationSettingsDto());
    }
    
    /// <summary>
    /// 保存通知设置
    /// </summary>
    /// <param name="dto">通知设置DTO</param>
    /// <returns>操作结果</returns>
    [HttpPut("notification")]
    [DisplayName("保存通知设置")]
    [HeaderOperation("通知设置", "form", Icon = "fa-solid fa-bell", DialogSize = DialogSize.MD)]
    public async Task<ActionResult<ApiResponse>> SaveNotificationSettings([FromBody] NotificationSettingsDto dto)
    {
        var tenantId = _currentUser.TenantId ?? "default";
        var success = await _settingsService.SetTenantSettingAsync(
            dto,
            tenantId,
            $"用户 {_currentUser.UserName} 更新通知设置");
        
        return success ? SuccessResponse("通知设置保存成功") : BadResponse("保存设置失败");
    }
    
    #endregion

    #region 用户偏好设置
    
    /// <summary>
    /// 获取用户偏好设置
    /// </summary>
    /// <returns>用户偏好设置</returns>
    [HttpGet("preferences")]
    [DisplayName("获取用户偏好")]
    public async Task<ActionResult<ApiResponse<UserPreferencesDto>>> GetUserPreferences()
    {
        var tenantId = _currentUser.TenantId ?? "default";
        var settings = await _settingsService.GetTenantSettingAsync<UserPreferencesDto>(tenantId);
        return SuccessResponse(settings ?? new UserPreferencesDto());
    }
    
    /// <summary>
    /// 保存用户偏好设置
    /// </summary>
    /// <param name="dto">用户偏好设置DTO</param>
    /// <returns>操作结果</returns>
    [HttpPut("preferences")]
    [DisplayName("保存用户偏好")]
    [HeaderOperation("用户偏好", "form", Icon = "fa-solid fa-sliders-h", DialogSize = DialogSize.MD)]
    public async Task<ActionResult<ApiResponse>> SaveUserPreferences([FromBody] UserPreferencesDto dto)
    {
        var tenantId = _currentUser.TenantId ?? "default";
        var success = await _settingsService.SetTenantSettingAsync(
            dto,
            tenantId,
            $"用户 {_currentUser.UserName} 更新用户偏好设置");
        
        return success ? SuccessResponse("用户偏好设置保存成功") : BadResponse("保存设置失败");
    }
    
    #endregion

    #region 短信验证码设置

    /// <summary>
    /// 获取短信验证码设置
    /// </summary>
    /// <returns>短信验证码设置</returns>
    [HttpGet("sms")]
    [DisplayName("获取短信验证码设置")]
    public async Task<ActionResult<ApiResponse<SmsSettingsDto>>> GetSmsSettings()
    {
        var tenantId = _currentUser.TenantId ?? "default";
        var settings = await _settingsService.GetTenantSettingAsync<SmsSettingsDto>(tenantId);
        return SuccessResponse(settings ?? new SmsSettingsDto());
    }

    /// <summary>
    /// 保存短信验证码设置
    /// </summary>
    /// <param name="dto">短信验证码设置DTO</param>
    /// <returns>操作结果</returns>
    [HttpPut("sms")]
    [DisplayName("保存短信验证码设置")]
    [HeaderOperation("短信验证码", "form", Icon = "fa-solid fa-comment-sms", DialogSize = DialogSize.LG)]
    public async Task<ActionResult<ApiResponse>> SaveSmsSettings([FromBody] SmsSettingsDto dto)
    {
        var tenantId = _currentUser.TenantId ?? "default";
        var success = await _settingsService.SetTenantSettingAsync(
            dto,
            tenantId,
            $"用户 {_currentUser.UserName} 更新短信验证码设置");

        return success ? SuccessResponse("短信验证码设置保存成功") : BadResponse("保存设置失败");
    }

    #endregion
}

