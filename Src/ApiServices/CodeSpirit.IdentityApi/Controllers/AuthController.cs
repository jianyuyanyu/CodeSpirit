// Controllers/AuthController.cs
using CodeSpirit.Audit.Attributes;
using CodeSpirit.Core;
using CodeSpirit.Core.Attributes;
using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.IdentityApi.Dtos.Auth;
using CodeSpirit.IdentityApi.Dtos.Settings;
using CodeSpirit.IdentityApi.Services;
using CodeSpirit.Settings.Services.Interfaces;
using CodeSpirit.Shared.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Linq;

namespace CodeSpirit.IdentityApi.Controllers
{
    /// <summary>
    /// 授权控制器，处理用户登录、令牌刷新和登出功能
    /// </summary>
    [AllowAnonymous]
    [Navigation(Hidden = true)]
    [NoAudit("授权控制器不需要审计")]
    public class AuthController : ApiControllerBase
    {
        private readonly IAuthService _authService;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<AuthController> _logger;
        private readonly IClientIpService _clientIpService;
        private readonly ICurrentUser _currentUser;
        private readonly CodeSpirit.MultiTenant.Abstractions.ITenantStore _tenantStore;

        /// <summary>
        /// 初始化授权控制器
        /// </summary>
        /// <param name="authService">授权服务</param>
        /// <param name="signInManager">登录管理器</param>
        /// <param name="logger">日志记录器</param>
        /// <param name="clientIpService">客户端IP地址获取服务</param>
        /// <param name="currentUser">当前用户服务</param>
        /// <param name="tenantStore">租户存储服务</param>
        public AuthController(
            IAuthService authService,
            SignInManager<ApplicationUser> signInManager,
            ILogger<AuthController> logger,
            IClientIpService clientIpService,
            ICurrentUser currentUser,
            CodeSpirit.MultiTenant.Abstractions.ITenantStore tenantStore)
        {
            _authService = authService;
            _signInManager = signInManager;
            _logger = logger;
            _clientIpService = clientIpService;
            _currentUser = currentUser;
            _tenantStore = tenantStore;
        }

        /// <summary>
        /// 用户登录接口
        /// </summary>
        /// <param name="model">登录模型</param>
        /// <returns>登录结果</returns>
        [HttpPost("login")]
        [AllowAnonymous]
        [DisplayName("用户登录")]
        public async Task<ActionResult<ApiResponse<AuthTokenResponse>>> Login([FromBody] LoginModel model)
        {
            try
            {
                // 从请求头或模型中获取租户ID
                var tenantId = HttpContext.Request.Headers["TenantId"].FirstOrDefault() 
                              ?? HttpContext.Items["TenantId"]?.ToString()
                              ?? model.TenantId;

                // 在服务器端获取客户端信息
                var loginDto = new LoginDto
                {
                    UserName = model.UserName,
                    Password = model.Password,
                    TenantId = tenantId,
                    IpAddress = _clientIpService.GetClientIpAddress(HttpContext),
                    UserAgent = HttpContext.Request.Headers["User-Agent"].ToString()
                };

                var result = await _authService.LoginAsync(loginDto);
                if (!result.Success)
                {
                    return BadResponse<AuthTokenResponse>(result.Message);
                }

                var response = new AuthTokenResponse
                {
                    Token = result.Token,
                    RefreshToken = result.RefreshToken,
                    User = result.UserInfo
                };
                return SuccessResponse(response, msg: "登录成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "登录异常");
                return BadResponse<AuthTokenResponse>("登录失败，请检查登录名或密码！");
            }
        }

        /// <summary>
        /// 刷新访问令牌
        /// </summary>
        /// <param name="refreshTokenDto">包含访问令牌和刷新令牌的请求对象</param>
        /// <returns>新的令牌信息</returns>
        [HttpPost("refresh-token")]
        [AllowAnonymous]
        [DisplayName("刷新访问令牌")]
        public async Task<ActionResult<ApiResponse<AuthTokenResponse>>> RefreshToken([FromBody] RefreshTokenDto refreshTokenDto)
        {
            try
            {
                if (refreshTokenDto == null || string.IsNullOrEmpty(refreshTokenDto.Token) || string.IsNullOrEmpty(refreshTokenDto.RefreshToken))
                {
                    return BadResponse<AuthTokenResponse>("访问令牌和刷新令牌不能为空");
                }

                var result = await _authService.RefreshTokenAsync(refreshTokenDto.Token, refreshTokenDto.RefreshToken);
                if (!result.Success)
                {
                    return BadResponse<AuthTokenResponse>(result.Message);
                }

                var response = new AuthTokenResponse
                {
                    Token = result.Token,
                    RefreshToken = result.RefreshToken,
                    User = result.UserInfo
                };
                return SuccessResponse(response, msg: "令牌刷新成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刷新令牌异常");
                return BadResponse<AuthTokenResponse>("刷新令牌失败: " + ex.Message);
            }
        }

        /// <summary>
        /// 用户登出接口
        /// </summary>
        /// <returns>登出结果</returns>
        [HttpPost("logout")]
        [Authorize]
        [DisplayName("用户登出")]
        public async Task<ActionResult<ApiResponse>> Logout()
        {
            try
            {
                if (!_currentUser.IsAuthenticated || !_currentUser.Id.HasValue)
                {
                    return BadResponse("用户未登录");
                }

                await _authService.LogoutAsync(_currentUser.Id.Value);
                return SuccessResponse("退出登录成功!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "退出登录异常");
                return BadResponse("退出登录失败");
            }
        }

        /// <summary>
        /// 系统平台登录接口
        /// </summary>
        /// <param name="model">系统平台登录请求</param>
        /// <returns>登录结果</returns>
        [HttpPost("system/login")]
        [AllowAnonymous]
        [DisplayName("系统平台登录")]
        public async Task<ActionResult<ApiResponse<AuthTokenResponse>>> SystemLogin([FromBody] SystemLoginModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadResponse<AuthTokenResponse>("请求参数验证失败");
                }

                // 🔥 设置系统登录的租户上下文
                HttpContext.Items["TenantId"] = TenantConstants.SystemTenantId;

                var ipAddress = _clientIpService.GetClientIpAddress(HttpContext);
                var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

                var result = await _authService.SystemLoginAsync(model, ipAddress, userAgent);
                if (!result.Success)
                {
                    return BadResponse<AuthTokenResponse>(result.Message);
                }

                var response = new AuthTokenResponse
                {
                    Token = result.Token,
                    RefreshToken = result.RefreshToken,
                    User = result.UserInfo
                };

                return SuccessResponse(response, msg: "系统管理员登录成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "系统平台登录异常");
                return BadResponse<AuthTokenResponse>("系统平台登录失败，请检查登录信息或联系管理员！");
            }
        }

        /// <summary>
        /// 租户平台登录接口
        /// </summary>
        /// <param name="model">租户平台登录请求</param>
        /// <returns>登录结果</returns>
        [HttpPost("tenant/login")]
        [AllowAnonymous]
        [DisplayName("租户平台登录")]
        public async Task<ActionResult<ApiResponse<AuthTokenResponse>>> TenantLogin([FromBody] TenantLoginModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadResponse<AuthTokenResponse>("请求参数验证失败");
                }

                // 从请求头获取租户ID（如果模型中没有提供）
                if (string.IsNullOrEmpty(model.TenantId))
                {
                    model.TenantId = HttpContext.Request.Headers["TenantId"].FirstOrDefault() 
                                    ?? HttpContext.Items["TenantId"]?.ToString();
                }

                if (string.IsNullOrEmpty(model.TenantId))
                {
                    return BadResponse<AuthTokenResponse>("租户ID不能为空");
                }

                var ipAddress = _clientIpService.GetClientIpAddress(HttpContext);
                var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

                var result = await _authService.TenantLoginAsync(model, ipAddress, userAgent);
                if (!result.Success)
                {
                    return BadResponse<AuthTokenResponse>(result.Message);
                }

                var response = new AuthTokenResponse
                {
                    Token = result.Token,
                    RefreshToken = result.RefreshToken,
                    User = result.UserInfo
                };

                return SuccessResponse(response, msg: "租户用户登录成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "租户平台登录异常");
                return BadResponse<AuthTokenResponse>("租户平台登录失败，请检查登录信息或联系管理员！");
            }
        }

        /// <summary>
        /// 客户端系统登录接口（支持考试系统、培训系统等）
        /// </summary>
        /// <param name="model">客户端登录请求</param>
        /// <returns>登录结果</returns>
        [HttpPost("client/login")]
        [AllowAnonymous]
        [DisplayName("客户端系统登录")]
        public async Task<ActionResult<ApiResponse<AuthTokenResponse>>> ClientLogin([FromBody] ClientLoginModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadResponse<AuthTokenResponse>("请求参数验证失败");
                }

                // 从请求头获取租户ID（如果模型中没有提供）
                if (string.IsNullOrEmpty(model.TenantId))
                {
                    model.TenantId = HttpContext.Request.Headers["TenantId"].FirstOrDefault() 
                                    ?? HttpContext.Items["TenantId"]?.ToString();
                }

                if (string.IsNullOrEmpty(model.TenantId))
                {
                    return BadResponse<AuthTokenResponse>("租户ID不能为空");
                }

                var ipAddress = _clientIpService.GetClientIpAddress(HttpContext);
                var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

                // 设置客户端系统的租户上下文
                HttpContext.Items["TenantId"] = model.TenantId;
                HttpContext.Items["ClientType"] = model.ClientType ?? "exam"; // 默认为考试系统
                HttpContext.Items["IsClientLogin"] = true;

                // 将ClientLoginModel转换为TenantLoginModel
                var tenantLoginModel = new TenantLoginModel
                {
                    UserName = model.UserName,
                    Password = model.Password,
                    TenantId = model.TenantId
                };

                var result = await _authService.TenantLoginAsync(tenantLoginModel, ipAddress, userAgent);
                if (!result.Success)
                {
                    return BadResponse<AuthTokenResponse>(result.Message);
                }

                var response = new AuthTokenResponse
                {
                    Token = result.Token,
                    RefreshToken = result.RefreshToken,
                    User = result.UserInfo
                };

                var clientTypeName = GetClientTypeName(model.ClientType);
                return SuccessResponse(response, msg: $"{clientTypeName}登录成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "客户端系统登录异常，客户端类型: {ClientType}", model.ClientType);
                return BadResponse<AuthTokenResponse>("客户端系统登录失败，请检查登录信息或联系管理员！");
            }
        }

        /// <summary>
        /// 获取客户端类型显示名称
        /// </summary>
        /// <param name="clientType">客户端类型</param>
        /// <returns>显示名称</returns>
        private string GetClientTypeName(string clientType)
        {
            return clientType?.ToLower() switch
            {
                "exam" => "考试系统",
                "training" => "培训系统",
                "learning" => "学习系统",
                "assessment" => "评估系统",
                _ => "客户端系统"
            };
        }

        /// <summary>
        /// 第三方平台登录接口（通用）
        /// </summary>
        /// <param name="model">第三方登录请求</param>
        /// <returns>登录结果</returns>
        [HttpPost("third-party/login")]
        [AllowAnonymous]
        [DisplayName("第三方平台登录")]
        public async Task<ActionResult<ApiResponse<AuthTokenResponse>>> ThirdPartyLogin([FromBody] ThirdPartyLoginModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadResponse<AuthTokenResponse>("请求参数验证失败");
                }

                // 验证租户ID
                if (string.IsNullOrEmpty(model.TenantId))
                {
                    return BadResponse<AuthTokenResponse>("租户ID不能为空");
                }

                // 验证租户是否存在和有效
                var tenantInfo = await _tenantStore.GetTenantAsync(model.TenantId);
                if (tenantInfo == null || !tenantInfo.IsActive)
                {
                    return BadResponse<AuthTokenResponse>("租户不存在或已禁用");
                }

                // 设置租户上下文（用于后续数据库操作）
                HttpContext.Items["TenantId"] = model.TenantId;

                var ipAddress = _clientIpService.GetClientIpAddress(HttpContext);
                var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

                // 调用服务方法
                var result = await _authService.ThirdPartyLoginAsync(model, ipAddress, userAgent);
                if (!result.Success)
                {
                    return BadResponse<AuthTokenResponse>(result.Message);
                }

                // 返回结果（包含租户信息）
                var response = new AuthTokenResponse
                {
                    Token = result.Token,
                    RefreshToken = result.RefreshToken,
                    User = result.UserInfo,
                    TenantInfo = new TenantInfoDto
                    {
                        TenantId = tenantInfo.TenantId,
                        TenantName = tenantInfo.Name
                    }
                };

                return SuccessResponse(response, "第三方登录成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "第三方登录异常");
                return BadResponse<AuthTokenResponse>("第三方登录失败，请检查登录信息或联系管理员！");
            }
        }

        /// <summary>
        /// 微信登录接口（兼容性，内部转换为ThirdPartyLogin）
        /// </summary>
        /// <param name="model">微信登录请求</param>
        /// <returns>登录结果</returns>
        [HttpPost("wechat/login")]
        [AllowAnonymous]
        [DisplayName("微信登录")]
        public async Task<ActionResult<ApiResponse<AuthTokenResponse>>> WeChatLogin([FromBody] WeChatLoginModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadResponse<AuthTokenResponse>("请求参数验证失败");
                }

                // 验证租户ID
                if (string.IsNullOrEmpty(model.TenantId))
                {
                    return BadResponse<AuthTokenResponse>("租户ID不能为空");
                }

                // 验证租户是否存在和有效
                var tenantInfo = await _tenantStore.GetTenantAsync(model.TenantId);
                if (tenantInfo == null || !tenantInfo.IsActive)
                {
                    return BadResponse<AuthTokenResponse>("租户不存在或已禁用");
                }

                // 设置租户上下文（用于后续数据库操作）
                HttpContext.Items["TenantId"] = model.TenantId;

                var ipAddress = _clientIpService.GetClientIpAddress(HttpContext);
                var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

                // 调用服务方法
                var result = await _authService.WeChatLoginAsync(model, ipAddress, userAgent);
                if (!result.Success)
                {
                    return BadResponse<AuthTokenResponse>(result.Message);
                }

                // 返回结果（包含租户信息）
                var response = new AuthTokenResponse
                {
                    Token = result.Token,
                    RefreshToken = result.RefreshToken,
                    User = result.UserInfo,
                    TenantInfo = new TenantInfoDto
                    {
                        TenantId = tenantInfo.TenantId,
                        TenantName = tenantInfo.Name
                    }
                };

                return SuccessResponse(response, "微信登录成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "微信登录异常");
                return BadResponse<AuthTokenResponse>("微信登录失败，请检查登录信息或联系管理员！");
            }
        }

        /// <summary>
        /// 获取微信手机号接口
        /// </summary>
        /// <param name="request">手机号获取请求</param>
        /// <returns>手机号信息</returns>
        [HttpPost("wechat/phone")]
        [Authorize]
        [DisplayName("获取微信手机号")]
        public async Task<ActionResult<ApiResponse<WeChatPhoneResult>>> GetWeChatPhone([FromBody] WeChatPhoneRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadResponse<WeChatPhoneResult>("请求参数验证失败");
                }

                // 从请求头或当前用户获取租户ID
                var tenantId = HttpContext.Request.Headers["TenantId"].FirstOrDefault() 
                              ?? HttpContext.Items["TenantId"]?.ToString()
                              ?? _currentUser.TenantId;

                if (string.IsNullOrEmpty(tenantId))
                {
                    return BadResponse<WeChatPhoneResult>("租户ID不能为空");
                }

                // 调用服务方法获取手机号
                var result = await _authService.GetWeChatPhoneAsync(request.Code, tenantId);
                return SuccessResponse(result, "获取手机号成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取微信手机号异常");
                return BadResponse<WeChatPhoneResult>($"获取手机号失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 发送短信验证码接口
        /// </summary>
        /// <param name="request">发送验证码请求</param>
        /// <returns>发送结果</returns>
        [HttpPost("sms/send")]
        [AllowAnonymous]
        [DisplayName("发送短信验证码")]
        public async Task<ActionResult<ApiResponse<SendSmsCodeResponse>>> SendSmsCode([FromBody] SendSmsCodeRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadResponse<SendSmsCodeResponse>("请求参数验证失败");
                }

                // 从请求头或模型中获取租户ID
                var tenantId = HttpContext.Request.Headers["TenantId"].FirstOrDefault() 
                              ?? HttpContext.Items["TenantId"]?.ToString()
                              ?? request.TenantId ?? "default";

                // 调用短信验证码服务发送验证码
                var smsCodeService = HttpContext.RequestServices.GetRequiredService<ISmsCodeService>();
                var success = await smsCodeService.SendCodeAsync(request.PhoneNumber, tenantId);

                if (!success)
                {
                    return BadResponse<SendSmsCodeResponse>("发送验证码失败，请稍后重试");
                }

                // 获取短信设置以获取有效期
                var settingsService = HttpContext.RequestServices.GetRequiredService<ISettingsService>();
                var settings = await settingsService.GetTenantSettingAsync<SmsSettingsDto>(tenantId);
                var expiresInSeconds = settings?.CodeExpireSeconds ?? 300;

                var response = new SendSmsCodeResponse
                {
                    Success = true,
                    ExpiresInSeconds = expiresInSeconds,
                    Message = "验证码发送成功"
                };

                return SuccessResponse(response, "验证码发送成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发送短信验证码异常");
                return BadResponse<SendSmsCodeResponse>($"发送验证码失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 短信验证码登录接口
        /// </summary>
        /// <param name="request">短信登录请求</param>
        /// <returns>登录结果</returns>
        [HttpPost("sms/login")]
        [AllowAnonymous]
        [DisplayName("短信验证码登录")]
        public async Task<ActionResult<ApiResponse<AuthTokenResponse>>> SmsLogin([FromBody] SmsLoginRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadResponse<AuthTokenResponse>("请求参数验证失败");
                }

                // 从请求头或模型中获取租户ID
                var tenantId = HttpContext.Request.Headers["TenantId"].FirstOrDefault() 
                              ?? HttpContext.Items["TenantId"]?.ToString()
                              ?? request.TenantId;

                if (string.IsNullOrEmpty(tenantId))
                {
                    return BadResponse<AuthTokenResponse>("租户ID不能为空");
                }

                // 设置租户上下文
                HttpContext.Items["TenantId"] = tenantId;

                var ipAddress = _clientIpService.GetClientIpAddress(HttpContext);
                var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

                // 调用服务方法进行登录
                var result = await _authService.SmsLoginAsync(request, ipAddress, userAgent);
                if (!result.Success)
                {
                    return BadResponse<AuthTokenResponse>(result.Message);
                }

                var response = new AuthTokenResponse
                {
                    Token = result.Token,
                    RefreshToken = result.RefreshToken,
                    User = result.UserInfo
                };

                return SuccessResponse(response, "短信验证码登录成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "短信验证码登录异常");
                return BadResponse<AuthTokenResponse>($"短信验证码登录失败: {ex.Message}");
            }
        }
    }
}