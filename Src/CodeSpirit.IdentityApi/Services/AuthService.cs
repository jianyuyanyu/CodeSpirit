// Services/AuthService.cs
using AutoMapper;
using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.IdentityApi.Dtos.Auth;
using CodeSpirit.IdentityApi.Jwt;
using CodeSpirit.Shared.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

namespace CodeSpirit.IdentityApi.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IRepository<RefreshToken> _refreshTokenRepository;
        private readonly ILoginLogRepository _loginLogRepository;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;
        private readonly IJwtTokenHandler _jwtHandler;
        private readonly ILogger<AuthService> _logger;
        private readonly IRoleService _roleService;
        private readonly int _refreshTokenExpirationDays;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IMapper mapper,
            IConfiguration configuration,
            IRepository<RefreshToken> refreshTokenRepository,
            ILoginLogRepository loginLogRepository,
            IJwtTokenHandler jwtHandler,
            ILogger<AuthService> logger,
            IRoleService roleService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _mapper = mapper;
            _configuration = configuration;
            _refreshTokenRepository = refreshTokenRepository;
            _loginLogRepository = loginLogRepository;
            _jwtHandler = jwtHandler;
            _logger = logger;
            _roleService = roleService;

            // 刷新令牌过期时间，默认7天
            if (!int.TryParse(_configuration["Jwt:RefreshTokenExpirationDays"], out _refreshTokenExpirationDays))
            {
                _refreshTokenExpirationDays = 7; // 默认值为7天
            }
        }

        /// <summary>
        /// 登录方法，验证用户名和密码，并返回结果及JWT Token
        /// </summary>
        /// <param name="input">登录请求</param>
        /// <returns>返回一个包含登录成功与否、信息和JWT Token的元组</returns>
        public async Task<AuthResultDto> LoginAsync(LoginDto input)
        {
            try
            {
                // 验证租户信息（如果提供了租户ID）
                if (!string.IsNullOrEmpty(input.TenantId))
                {
                    // 这里可以添加租户验证逻辑
                    // 由于当前没有直接的租户验证服务，我们暂时跳过
                    // 在实际应用中，应该验证租户是否存在且处于活跃状态
                }

                var user = await _userManager.FindByNameAsync(input.UserName);
                if (user == null)
                {
                    await LogLoginAsync(input, null, false, "用户不存在！");
                    return AuthResultDto.CreateFailure("用户名或密码不正确！");
                }

                // 验证用户是否属于指定租户（如果提供了租户ID）
                if (!string.IsNullOrEmpty(input.TenantId) && !string.IsNullOrEmpty(user.TenantId))
                {
                    if (user.TenantId != input.TenantId)
                    {
                        await LogLoginAsync(input, user.Id, false, "用户不属于指定租户");
                        return AuthResultDto.CreateFailure("用户名或密码不正确！");
                    }
                }

                if (!user.IsActive)
                {
                    await LogLoginAsync(input, user.Id, false, "账号已被禁用！");
                    return AuthResultDto.CreateFailure("账号已被禁用！");
                }

                var result = await _signInManager.CheckPasswordSignInAsync(user, input.Password, true);
                var loginLog = new LoginLog
                {
                    UserId = user.Id,
                    UserName = input.UserName,
                    LoginTime = DateTime.UtcNow,
                    IPAddress = input.IpAddress,
                    IsSuccess = result.Succeeded,
                    TenantId = user.TenantId ?? input.TenantId ?? "default"
                };

                if (result.Succeeded)
                {
                    // 更新最后登录时间
                    user.LastLoginTime = DateTimeOffset.UtcNow;
                    await _userManager.UpdateAsync(user);

                    // 预热缓存：提前获取并缓存用户权限
                    await _roleService.GetUserPermissionsAsync(user.Id);
                    
                    // 生成令牌
                    var token = await _jwtHandler.GenerateTokenAsync(user);

                    // 从JWT中获取jwtId
                    var tokenHandler = new JwtSecurityTokenHandler();
                    var jwtToken = tokenHandler.ReadJwtToken(token);
                    var jwtId = jwtToken.Id;

                    // 生成刷新令牌
                    var refreshToken = await GenerateRefreshTokenAsync(user.Id, jwtId);

                    // 记录登录日志
                    loginLog.IsSuccess = true;
                    await _loginLogRepository.AddAsync(loginLog);

                    // 准备用户信息
                    var userDto = _mapper.Map<UserDto>(user);

                    // 返回成功结果
                    return AuthResultDto.CreateSuccess(token, refreshToken, userDto);
                }
                else
                {
                    // 记录失败原因
                    string failReason = "密码错误！";
                    if (result.IsLockedOut)
                    {
                        failReason = "账号已被锁定！";
                    }
                    else if (result.IsNotAllowed)
                    {
                        failReason = "账号未被授权！";
                    }

                    loginLog.FailureReason = failReason;
                    await _loginLogRepository.AddAsync(loginLog);

                    return AuthResultDto.CreateFailure(failReason == "密码错误！" ? "登录名或密码不正确！" : failReason);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "登录过程发生异常");
                await LogLoginAsync(input, null, false, "系统异常：" + ex.Message);
                return AuthResultDto.CreateFailure("登录失败：系统异常");
            }
        }

        public async Task<bool> LogoutAsync(long userId)
        {
            // 可以在这里实现额外的登出逻辑，如撤销令牌等
            return true;
        }

        public async Task<AuthResultDto> RefreshTokenAsync(string accessToken, string refreshToken)
        {
            try
            {
                // 验证访问令牌是否有效（即使过期也可以验证）
                ClaimsPrincipal principal = null;
                try
                {
                    // 忽略过期验证，只检查令牌格式和签名
                    principal = _jwtHandler.ValidateTokenWithoutLifetime(accessToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "令牌验证失败: {accessToken}", accessToken);
                    return AuthResultDto.CreateFailure("无效的访问令牌");
                }

                if (principal == null)
                {
                    return AuthResultDto.CreateFailure("无效的访问令牌");
                }

                // 获取用户ID和jwtId
                var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);
                var jwtIdClaim = principal.FindFirst("jti");

                if (userIdClaim == null || jwtIdClaim == null || !long.TryParse(userIdClaim.Value, out var userId))
                {
                    return AuthResultDto.CreateFailure("无效的访问令牌");
                }

                // 获取刷新令牌
                var storedRefreshToken = await _refreshTokenRepository.CreateQuery()
                    .FirstOrDefaultAsync(r => r.Token == refreshToken && r.UserId == userId && r.JwtId == jwtIdClaim.Value);

                // 验证刷新令牌
                if (storedRefreshToken == null)
                {
                    return AuthResultDto.CreateFailure("刷新令牌不存在");
                }

                if (storedRefreshToken.ExpiryTime < DateTime.UtcNow)
                {
                    return AuthResultDto.CreateFailure("刷新令牌已过期");
                }

                if (storedRefreshToken.IsUsed)
                {
                    return AuthResultDto.CreateFailure("刷新令牌已被使用");
                }

                if (storedRefreshToken.IsRevoked)
                {
                    return AuthResultDto.CreateFailure("刷新令牌已被撤销");
                }

                // 标记当前刷新令牌为已使用
                storedRefreshToken.IsUsed = true;
                await _refreshTokenRepository.UpdateAsync(storedRefreshToken);

                // 获取用户
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                {
                    return AuthResultDto.CreateFailure("用户不存在");
                }

                // 预热缓存：提前获取并缓存用户权限
                await _roleService.GetUserPermissionsAsync(user.Id);

                // 生成新的访问令牌
                var newToken = await _jwtHandler.GenerateTokenAsync(user);

                // 从新JWT中获取jwtId
                var tokenHandler = new JwtSecurityTokenHandler();
                var newJwtToken = tokenHandler.ReadJwtToken(newToken);
                var newJwtId = newJwtToken.Id;

                // 生成新的刷新令牌
                var newRefreshToken = await GenerateRefreshTokenAsync(userId, newJwtId);

                // 准备用户信息
                var userDto = _mapper.Map<UserDto>(user);

                // 返回成功结果
                return AuthResultDto.CreateSuccess(newToken, newRefreshToken, userDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刷新令牌过程发生异常");
                return AuthResultDto.CreateFailure("刷新令牌失败：系统异常");
            }
        }

        public async Task LogLoginAsync(LoginDto input, long? userId, bool isSuccess, string failReason = null)
        {
            try
            {
                var loginLog = new LoginLog
                {
                    UserId = userId,
                    UserName = input.UserName,
                    LoginTime = DateTime.UtcNow,
                    IPAddress = input.IpAddress,
                    IsSuccess = isSuccess,
                    FailureReason = failReason
                };

                await _loginLogRepository.AddAsync(loginLog);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "记录登录日志过程发生异常");
            }
        }

        /// <summary>
        /// 生成刷新令牌
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="jwtId">JWT令牌ID</param>
        /// <returns>刷新令牌字符串</returns>
        private async Task<string> GenerateRefreshTokenAsync(long userId, string jwtId)
        {
            // 生成随机令牌
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            string refreshToken = Convert.ToBase64String(randomNumber);

            // 创建刷新令牌实体
            var refreshTokenEntity = new RefreshToken
            {
                UserId = userId,
                Token = refreshToken,
                JwtId = jwtId,
                IsUsed = false,
                IsRevoked = false,
                CreatedTime = DateTime.UtcNow,
                ExpiryTime = DateTime.UtcNow.AddDays(_refreshTokenExpirationDays)
            };

            // 保存到数据库
            await _refreshTokenRepository.AddAsync(refreshTokenEntity);

            return refreshToken;
        }

        /// <summary>
        /// 撤销刷新令牌
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="refreshToken">刷新令牌</param>
        /// <returns>撤销结果</returns>
        public async Task<bool> RevokeRefreshTokenAsync(long userId, string refreshToken)
        {
            var storedRefreshToken = await _refreshTokenRepository.CreateQuery()
                .FirstOrDefaultAsync(r => r.Token == refreshToken && r.UserId == userId);

            if (storedRefreshToken == null)
            {
                return false;
            }

            storedRefreshToken.IsRevoked = true;
            await _refreshTokenRepository.UpdateAsync(storedRefreshToken);

            return true;
        }

        /// <summary>
        /// 模拟用户登录，直接生成JWT Token而不验证密码
        /// </summary>
        /// <param name="userName">用户名</param>
        /// <returns>返回登录结果</returns>
        public async Task<(bool Success, string Message, string Token, UserDto UserInfo)> ImpersonateLoginAsync(string userName)
        {
            try
            {
                // 查找用户
                var user = await _userManager.FindByNameAsync(userName);

                // 如果用户不存在，返回失败信息
                if (user == null)
                {
                    return (false, "用户不存在", null, null);
                }

                // 检查用户是否活跃
                if (!user.IsActive)
                {
                    return (false, "账号已被禁用", null, null);
                }

                // 预热缓存：提前获取并缓存用户权限
                await _roleService.GetUserPermissionsAsync(user.Id);

                // 生成token
                var token = await _jwtHandler.GenerateTokenAsync(user);

                // 将用户对象映射到DTO对象
                var userDto = _mapper.Map<UserDto>(user);

                return (true, "模拟登录成功", token, userDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "模拟登录过程发生异常");
                return (false, "模拟登录失败：系统异常", null, null);
            }
        }
    }

    /// <summary>
    /// 存放错误消息的静态类
    /// </summary>
    public static class ErrorMessages
    {
        public const string InvalidCredentials = "用户名或密码不正确，请重新输入！";
        public const string AccountLocked = "账户被锁定，请稍后再试！";
        public const string InactiveAccount = "账户未激活，请联系管理员！";
        public const string InvalidToken = "无效的令牌！";
        public const string RefreshTokenExpired = "刷新令牌已过期！";
    }
}
