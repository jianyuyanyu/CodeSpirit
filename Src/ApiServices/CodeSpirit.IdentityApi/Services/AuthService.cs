// Services/AuthService.cs
using AutoMapper;
using CodeSpirit.Core;
using CodeSpirit.Core.IdGenerator;
using CodeSpirit.IdentityApi.Data;
using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.IdentityApi.Dtos.Auth;
using CodeSpirit.IdentityApi.Dtos.User;
using CodeSpirit.IdentityApi.Jwt;
using CodeSpirit.IdentityApi.Models;
using CodeSpirit.IdentityApi.Resources;
using CodeSpirit.IdentityApi.Services.ThirdParty;
using CodeSpirit.Settings.Services.Interfaces;
using CodeSpirit.Shared.Repositories;
using Microsoft.AspNetCore.DataProtection;
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
        private readonly ApplicationDbContext _context;
        private readonly IThirdPartyApiService _thirdPartyApiService;
        private readonly ISettingsService _settingsService;
        private readonly ICurrentUser _currentUser;
        private readonly IIdGenerator _idGenerator;
        private readonly IDataProtectionProvider _dataProtectionProvider;
        private readonly ISmsCodeService _smsCodeService;
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
            IRoleService roleService,
            ApplicationDbContext context,
            IThirdPartyApiService thirdPartyApiService,
            ISettingsService settingsService,
            ICurrentUser currentUser,
            IIdGenerator idGenerator,
            IDataProtectionProvider dataProtectionProvider,
            ISmsCodeService smsCodeService)
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
            _context = context;
            _thirdPartyApiService = thirdPartyApiService;
            _settingsService = settingsService;
            _currentUser = currentUser;
            _idGenerator = idGenerator;
            _dataProtectionProvider = dataProtectionProvider;
            _smsCodeService = smsCodeService;

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

                ApplicationUser user = null;

                // 如果指定了租户ID，使用租户特定的查询方法
                if (!string.IsNullOrEmpty(input.TenantId))
                {
                    user = await FindUserByNameAndTenantAsync(input.UserName, input.TenantId);
                }
                else
                {
                    // 没有指定租户ID时，使用传统的UserManager查询（可能受租户筛选器影响）
                    user = await _userManager.FindByNameAsync(input.UserName);
                }

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
                    FailureReason = failReason,
                    TenantId = input.TenantId // 🔥 关键修复：使用传入的租户ID
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
        /// <param name="tenantId">租户ID（可选）</param>
        /// <returns>返回登录结果</returns>
        public async Task<(bool Success, string Message, string Token, UserDto UserInfo)> ImpersonateLoginAsync(string userName, string tenantId = null)
        {
            try
            {
                ApplicationUser user = null;

                // 如果指定了租户ID，使用租户特定的查询方法
                if (!string.IsNullOrEmpty(tenantId))
                {
                    user = await FindUserByNameAndTenantAsync(userName, tenantId);
                }
                else
                {
                    // 没有指定租户ID时，使用传统的UserManager查询
                    user = await _userManager.FindByNameAsync(userName);
                }

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

        /// <summary>
        /// 查找指定租户中的用户（忽略租户筛选器）
        /// </summary>
        /// <param name="userName">用户名</param>
        /// <param name="tenantId">租户ID</param>
        /// <returns>用户信息</returns>
        private async Task<ApplicationUser> FindUserByNameAndTenantAsync(string userName, string tenantId)
        {
            try
            {
                // 直接使用 DbContext 查询，忽略租户筛选器
                var user = await _context.Users
                    .IgnoreQueryFilters() // 忽略所有全局筛选器（包括租户筛选器和软删除筛选器）
                    .FirstOrDefaultAsync(u =>
                        u.UserName == userName &&
                        u.TenantId == tenantId &&
                        !u.IsDeleted);

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查找租户用户时发生异常: {UserName}, TenantId: {TenantId}", userName, tenantId);
                return null;
            }
        }

        /// <summary>
        /// 系统平台登录方法
        /// </summary>
        /// <param name="model">系统平台登录请求</param>
        /// <param name="ipAddress">客户端IP地址</param>
        /// <param name="userAgent">客户端信息</param>
        /// <returns>登录结果</returns>
        public async Task<AuthResultDto> SystemLoginAsync(SystemLoginModel model, string ipAddress, string userAgent)
        {
            try
            {
                var user = await FindUserByNameAndTenantAsync(model.UserName, TenantConstants.SystemTenantId);
                if (user == null)
                {
                    await LogLoginAsync(new LoginDto
                    {
                        UserName = model.UserName,
                        TenantId = "system",
                        IpAddress = ipAddress,
                        UserAgent = userAgent
                    }, null, false, "系统用户不存在");
                    return AuthResultDto.CreateFailure("系统管理员用户名或密码不正确！");
                }

                // 验证用户必须属于系统租户（这个检查现在是冗余的，但保留以确保安全）
                if (user.TenantId != "system")
                {
                    await LogLoginAsync(new LoginDto
                    {
                        UserName = model.UserName,
                        TenantId = "system",
                        IpAddress = ipAddress,
                        UserAgent = userAgent
                    }, user.Id, false, "非系统租户用户尝试系统平台登录");
                    return AuthResultDto.CreateFailure("访问被拒绝：此账号无权限访问系统管理平台，请使用系统管理员账号登录。");
                }

                // 验证用户是否激活
                if (!user.IsActive)
                {
                    await LogLoginAsync(new LoginDto
                    {
                        UserName = model.UserName,
                        TenantId = "system",
                        IpAddress = ipAddress,
                        UserAgent = userAgent
                    }, user.Id, false, "账号已被禁用");
                    return AuthResultDto.CreateFailure("账号已被禁用！");
                }

                // 验证密码
                var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, true);
                if (!result.Succeeded)
                {
                    string failReason = "密码错误";
                    if (result.IsLockedOut)
                    {
                        failReason = "账号已被锁定";
                    }
                    else if (result.IsNotAllowed)
                    {
                        failReason = "账号未被授权";
                    }

                    await LogLoginAsync(new LoginDto
                    {
                        UserName = model.UserName,
                        TenantId = "system",
                        IpAddress = ipAddress,
                        UserAgent = userAgent
                    }, user.Id, false, failReason);

                    return AuthResultDto.CreateFailure(failReason == "密码错误" ? "系统管理员用户名或密码不正确！" : failReason);
                }

                // 登录成功处理
                return await ProcessSuccessfulLoginAsync(user, ipAddress, userAgent, "system");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "系统平台登录过程发生异常");
                return AuthResultDto.CreateFailure("登录失败：系统异常");
            }
        }

        /// <summary>
        /// 租户平台登录方法
        /// </summary>
        /// <param name="model">租户平台登录请求</param>
        /// <param name="ipAddress">客户端IP地址</param>
        /// <param name="userAgent">客户端信息</param>
        /// <returns>登录结果</returns>
        public async Task<AuthResultDto> TenantLoginAsync(TenantLoginModel model, string ipAddress, string userAgent)
        {
            try
            {
                // 使用专门的租户用户查询方法，避免租户筛选器影响
                var user = await FindUserByNameAndTenantAsync(model.UserName, model.TenantId);
                if (user == null)
                {
                    await LogLoginAsync(new LoginDto
                    {
                        UserName = model.UserName,
                        TenantId = model.TenantId,
                        IpAddress = ipAddress,
                        UserAgent = userAgent
                    }, null, false, "指定租户中的用户不存在");
                    return AuthResultDto.CreateFailure("用户名或密码不正确！");
                }

                // 验证用户必须属于指定租户（这个检查现在是冗余的，但保留以确保安全）
                if (string.IsNullOrEmpty(user.TenantId) || user.TenantId != model.TenantId)
                {
                    var userTenantName = string.IsNullOrEmpty(user.TenantId) ? "未知" : user.TenantId;
                    await LogLoginAsync(new LoginDto
                    {
                        UserName = model.UserName,
                        TenantId = model.TenantId,
                        IpAddress = ipAddress,
                        UserAgent = userAgent
                    }, user.Id, false, $"用户租户不匹配，用户租户：{userTenantName}，请求租户：{model.TenantId}");
                    return AuthResultDto.CreateFailure($"访问被拒绝：此账号属于租户\"{userTenantName}\"，无法登录租户\"{model.TenantId}\"的管理平台。请使用正确的租户账号。");
                }

                // 验证不允许系统管理员通过租户平台登录
                if (user.TenantId == "system")
                {
                    await LogLoginAsync(new LoginDto
                    {
                        UserName = model.UserName,
                        TenantId = model.TenantId,
                        IpAddress = ipAddress,
                        UserAgent = userAgent
                    }, user.Id, false, "系统管理员尝试通过租户平台登录");
                    return AuthResultDto.CreateFailure("系统管理员账号请前往系统管理平台登录，不能通过租户平台登录。");
                }

                // 验证用户是否激活
                if (!user.IsActive)
                {
                    await LogLoginAsync(new LoginDto
                    {
                        UserName = model.UserName,
                        TenantId = model.TenantId,
                        IpAddress = ipAddress,
                        UserAgent = userAgent
                    }, user.Id, false, "账号已被禁用");
                    return AuthResultDto.CreateFailure("账号已被禁用！");
                }

                // 验证密码
                var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, true);
                if (!result.Succeeded)
                {
                    string failReason = "密码错误";
                    if (result.IsLockedOut)
                    {
                        failReason = "账号已被锁定";
                    }
                    else if (result.IsNotAllowed)
                    {
                        failReason = "账号未被授权";
                    }

                    await LogLoginAsync(new LoginDto
                    {
                        UserName = model.UserName,
                        TenantId = model.TenantId,
                        IpAddress = ipAddress,
                        UserAgent = userAgent
                    }, user.Id, false, failReason);

                    return AuthResultDto.CreateFailure(failReason == "密码错误" ? "用户名或密码不正确！" : failReason);
                }

                // 登录成功处理
                return await ProcessSuccessfulLoginAsync(user, ipAddress, userAgent, model.TenantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "租户平台登录过程发生异常");
                return AuthResultDto.CreateFailure("登录失败：系统异常");
            }
        }

        /// <summary>
        /// 处理成功登录的通用逻辑
        /// </summary>
        /// <param name="user">用户信息</param>
        /// <param name="ipAddress">客户端IP地址</param>
        /// <param name="userAgent">客户端信息</param>
        /// <param name="tenantId">租户ID</param>
        /// <returns>登录结果</returns>
        private async Task<AuthResultDto> ProcessSuccessfulLoginAsync(ApplicationUser user, string ipAddress, string userAgent, string tenantId)
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
            var loginLog = new LoginLog
            {
                UserId = user.Id,
                UserName = user.UserName,
                LoginTime = DateTime.UtcNow,
                IPAddress = ipAddress,
                IsSuccess = true,
                TenantId = tenantId
            };
            await _loginLogRepository.AddAsync(loginLog);

            // 准备用户信息
            var userDto = _mapper.Map<UserDto>(user);

            // 返回成功结果
            return AuthResultDto.CreateSuccess(token, refreshToken, userDto);
        }

        /// <summary>
        /// 第三方平台登录方法
        /// </summary>
        public async Task<AuthResultDto> ThirdPartyLoginAsync(ThirdPartyLoginModel model, string ipAddress, string userAgent)
        {
            try
            {
                // 1. 获取平台配置
                var config = await GetPlatformConfigAsync(model.PlatformType, model.TenantId);
                
                // 2. 调用第三方API获取会话信息
                var sessionInfo = await _thirdPartyApiService.GetSessionAsync(
                    model.PlatformType, 
                    model.Credential, 
                    config);
                
                // 3. 查找用户（优先UnionId，其次OpenId+PlatformType）
                ApplicationUser user = null;
                ThirdPartyAccount account = null;
                
                if (!string.IsNullOrEmpty(sessionInfo.UnionId))
                {
                    account = await FindAccountByUnionIdAsync(sessionInfo.UnionId, model.TenantId);
                }
                
                if (account == null)
                {
                    account = await FindAccountByOpenIdAsync(
                        model.PlatformType, 
                        sessionInfo.OpenId, 
                        model.TenantId);
                }
                
                // 4. 如果账号不存在，创建新用户和账号
                if (account == null)
                {
                    user = await CreateThirdPartyUserAsync(model.TenantId, model.PlatformType);
                    account = await CreateThirdPartyAccountAsync(
                        user.Id, 
                        model.TenantId, 
                        model.PlatformType, 
                        sessionInfo);
                }
                else
                {
                    user = account.User ?? await _context.Users.FindAsync(account.UserId);
                    
                    // 更新账号信息（UnionId、SessionKey等）
                    await UpdateThirdPartyAccountAsync(account, sessionInfo);
                }
                
                // 5. 验证用户状态并处理登录
                if (user == null || !user.IsActive)
                {
                    return AuthResultDto.CreateFailure("账号已被禁用");
                }
                
                return await ProcessSuccessfulLoginAsync(user, ipAddress, userAgent, model.TenantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "第三方登录异常，平台类型: {PlatformType}", model.PlatformType);
                return AuthResultDto.CreateFailure($"第三方登录失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 微信登录方法（兼容性包装）
        /// </summary>
        public async Task<AuthResultDto> WeChatLoginAsync(WeChatLoginModel model, string ipAddress, string userAgent)
        {
            var thirdPartyModel = new ThirdPartyLoginModel
            {
                PlatformType = ThirdPartyPlatformType.WeChatMiniProgram,
                Credential = model.Code,
                TenantId = model.TenantId
            };
            
            return await ThirdPartyLoginAsync(thirdPartyModel, ipAddress, userAgent);
        }

        /// <summary>
        /// 获取微信手机号
        /// </summary>
        /// <param name="code">手机号授权码</param>
        /// <param name="tenantId">租户ID</param>
        /// <returns>手机号信息</returns>
        public async Task<WeChatPhoneResult> GetWeChatPhoneAsync(string code, string tenantId)
        {
            var config = await GetPlatformConfigAsync(ThirdPartyPlatformType.WeChatMiniProgram, tenantId);
            
            if (_thirdPartyApiService is WeChatApiService weChatApiService)
            {
                return await weChatApiService.GetPhoneNumberAsync(code, config);
            }
            
            throw new InvalidOperationException("微信API服务未正确配置");
        }

        /// <summary>
        /// 短信验证码登录方法
        /// </summary>
        /// <param name="request">短信登录请求</param>
        /// <param name="ipAddress">客户端IP地址</param>
        /// <param name="userAgent">客户端信息</param>
        /// <returns>登录结果</returns>
        public async Task<AuthResultDto> SmsLoginAsync(SmsLoginRequest request, string ipAddress, string userAgent)
        {
            try
            {
                var tenantId = request.TenantId ?? "default";

                // 1. 验证验证码
                var isValid = await _smsCodeService.VerifyCodeAsync(request.PhoneNumber, request.Code, tenantId);
                if (!isValid)
                {
                    return AuthResultDto.CreateFailure("验证码错误或已过期");
                }

                // 2. 根据手机号查找用户
                ApplicationUser user = null;
                
                // 使用 IgnoreQueryFilters 避免租户过滤器影响
                user = await _context.Users
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber && u.TenantId == tenantId);

                // 3. 如果用户不存在，创建新用户
                if (user == null)
                {
                    var userId = _idGenerator.NewId();
                    var userName = $"sms_{request.PhoneNumber}_{userId}";

                    user = new ApplicationUser
                    {
                        Id = userId,
                        TenantId = tenantId,
                        UserName = userName,
                        NormalizedUserName = userName.ToUpperInvariant(),
                        PhoneNumber = request.PhoneNumber,
                        PhoneNumberConfirmed = true,
                        Name = $"用户{request.PhoneNumber.Substring(7)}", // 显示手机号后4位
                        IsActive = true
                    };

                    var createResult = await _userManager.CreateAsync(user);
                    if (!createResult.Succeeded)
                    {
                        _logger.LogError("创建短信登录用户失败: {Errors}", 
                            string.Join(", ", createResult.Errors.Select(e => e.Description)));
                        return AuthResultDto.CreateFailure($"创建用户失败: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
                    }

                    _logger.LogInformation("通过短信验证码创建新用户: {PhoneNumber}, UserId: {UserId}", 
                        request.PhoneNumber, userId);
                }

                // 4. 验证用户状态
                if (!user.IsActive)
                {
                    return AuthResultDto.CreateFailure("账号已被禁用");
                }

                // 5. 处理登录成功逻辑
                return await ProcessSuccessfulLoginAsync(user, ipAddress, userAgent, tenantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "短信验证码登录异常，手机号: {PhoneNumber}", request.PhoneNumber);
                return AuthResultDto.CreateFailure($"短信验证码登录失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 通过UnionId查找第三方账号
        /// </summary>
        private async Task<ThirdPartyAccount> FindAccountByUnionIdAsync(string unionId, string tenantId)
        {
            return await _context.ThirdPartyAccounts
                .Include(a => a.User)
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(a => a.UnionId == unionId && a.TenantId == tenantId);
        }

        /// <summary>
        /// 通过OpenId和平台类型查找第三方账号
        /// </summary>
        private async Task<ThirdPartyAccount> FindAccountByOpenIdAsync(
            ThirdPartyPlatformType platformType, 
            string openId, 
            string tenantId)
        {
            return await _context.ThirdPartyAccounts
                .Include(a => a.User)
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(a => 
                    a.PlatformType == platformType && 
                    a.OpenId == openId && 
                    a.TenantId == tenantId);
        }

        /// <summary>
        /// 创建第三方用户
        /// </summary>
        private async Task<ApplicationUser> CreateThirdPartyUserAsync(string tenantId, ThirdPartyPlatformType platformType)
        {
            var userId = _idGenerator.NewId();
            var userName = GenerateThirdPartyUserName(platformType, userId);
            
            var user = new ApplicationUser
            {
                Id = userId,
                TenantId = tenantId,
                UserName = userName,
                NormalizedUserName = userName.ToUpperInvariant(),
                Name = GetPlatformDisplayName(platformType),
                IsActive = true
                // CreatedAt 和 CreatedBy 由 SetAuditFields 自动设置
            };
            
            var result = await _userManager.CreateAsync(user);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"创建用户失败: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
            
            return user;
        }

        /// <summary>
        /// 创建第三方账号
        /// </summary>
        private async Task<ThirdPartyAccount> CreateThirdPartyAccountAsync(
            long userId,
            string tenantId,
            ThirdPartyPlatformType platformType,
            ThirdPartySessionInfo sessionInfo)
        {
            var account = new ThirdPartyAccount
            {
                Id = _idGenerator.NewId(),
                UserId = userId,
                TenantId = tenantId,
                PlatformType = platformType,
                OpenId = sessionInfo.OpenId,
                UnionId = sessionInfo.UnionId,
                SessionKey = EncryptSessionKey(sessionInfo.SessionKey),
                IsPrimary = true,
                LastLoginTime = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = 0
            };
            
            await _context.ThirdPartyAccounts.AddAsync(account);
            await _context.SaveChangesAsync();
            
            return account;
        }

        /// <summary>
        /// 更新第三方账号信息
        /// </summary>
        private async Task UpdateThirdPartyAccountAsync(ThirdPartyAccount account, ThirdPartySessionInfo sessionInfo)
        {
            bool updated = false;
            
            // 确保实体被跟踪
            var trackedAccount = _context.ThirdPartyAccounts.Local.FirstOrDefault(a => a.Id == account.Id);
            if (trackedAccount == null)
            {
                _context.ThirdPartyAccounts.Attach(account);
            }
            else
            {
                account = trackedAccount;
            }
            
            // 更新UnionId（如果之前不存在）
            if (string.IsNullOrEmpty(account.UnionId) && !string.IsNullOrEmpty(sessionInfo.UnionId))
            {
                account.UnionId = sessionInfo.UnionId;
                updated = true;
            }
            
            // 更新SessionKey
            if (!string.IsNullOrEmpty(sessionInfo.SessionKey))
            {
                account.SessionKey = EncryptSessionKey(sessionInfo.SessionKey);
                updated = true;
            }
            
            // 更新最后登录时间
            account.LastLoginTime = DateTime.UtcNow;
            account.UpdatedAt = DateTime.UtcNow;
            account.UpdatedBy = account.UserId;
            
            if (updated)
            {
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// 生成第三方用户用户名
        /// </summary>
        private string GenerateThirdPartyUserName(ThirdPartyPlatformType platformType, long userId)
        {
            var prefix = platformType switch
            {
                ThirdPartyPlatformType.WeChatMiniProgram => "wx",
                ThirdPartyPlatformType.AlipayMiniProgram => "alipay",
                _ => "tp"
            };
            return $"{prefix}_{userId}";
        }

        /// <summary>
        /// 获取平台显示名称
        /// </summary>
        private string GetPlatformDisplayName(ThirdPartyPlatformType platformType)
        {
            return platformType switch
            {
                ThirdPartyPlatformType.WeChatMiniProgram => "微信用户",
                ThirdPartyPlatformType.AlipayMiniProgram => "支付宝用户",
                _ => "第三方用户"
            };
        }

        /// <summary>
        /// 加密SessionKey
        /// </summary>
        private string EncryptSessionKey(string sessionKey)
        {
            if (string.IsNullOrEmpty(sessionKey))
            {
                return string.Empty;
            }
            
            var protector = _dataProtectionProvider.CreateProtector("ThirdParty.SessionKey");
            return protector.Protect(sessionKey);
        }

        /// <summary>
        /// 获取平台配置（从设置服务读取）
        /// </summary>
        private async Task<ThirdPartyPlatformConfig> GetPlatformConfigAsync(ThirdPartyPlatformType platformType, string tenantId)
        {
            return platformType switch
            {
                ThirdPartyPlatformType.WeChatMiniProgram => await GetWeChatConfigAsync(tenantId),
                ThirdPartyPlatformType.AlipayMiniProgram => await GetAlipayConfigAsync(tenantId),
                _ => throw new NotSupportedException($"不支持的平台类型: {platformType}")
            };
        }
        
        /// <summary>
        /// 获取微信小程序配置
        /// </summary>
        private async Task<ThirdPartyPlatformConfig> GetWeChatConfigAsync(string tenantId)
        {
            var settings = await _settingsService.GetTenantSettingAsync<Dtos.Settings.WeChatLoginSettingsDto>(tenantId);
            
            if (settings == null || string.IsNullOrEmpty(settings.AppId))
            {
                throw new BusinessException(IdentityErrorsResources.ThirdPartyLoginSettingsNotFound);
            }
            
            return new ThirdPartyPlatformConfig
            {
                AppId = settings.AppId,
                AppSecret = settings.AppSecret
            };
        }
        
        /// <summary>
        /// 获取支付宝小程序配置
        /// </summary>
        private async Task<ThirdPartyPlatformConfig> GetAlipayConfigAsync(string tenantId)
        {
            var settings = await _settingsService.GetTenantSettingAsync<Dtos.Settings.AlipayLoginSettingsDto>(tenantId);
            
            if (settings == null || string.IsNullOrEmpty(settings.AppId))
            {
                throw new BusinessException(IdentityErrorsResources.ThirdPartyLoginSettingsNotFound);
            }
            
            return new ThirdPartyPlatformConfig
            {
                AppId = settings.AppId,
                AppSecret = settings.AppSecret
            };
        }
    }
}
