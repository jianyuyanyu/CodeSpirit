// Services/AuthService.cs
using AutoMapper;
using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.Shared.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CodeSpirit.IdentityApi.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IRepository<LoginLog> _loginLogRepository;
        private readonly IConfiguration _configuration;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;  // 引入 IHttpContextAccessor

        private readonly string _secretKey;
        private readonly string _issuer;
        private readonly string _audience;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IRepository<LoginLog> loginLogRepository,
            IConfiguration configuration,
            IMapper mapper,
            IHttpContextAccessor httpContextAccessor) // 通过依赖注入获取 IHttpContextAccessor
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _loginLogRepository = loginLogRepository;
            _configuration = configuration;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;  // 初始化 IHttpContextAccessor

            // 配置常量初始化
            _secretKey = _configuration["Jwt:SecretKey"];
            _issuer = _configuration["Jwt:Issuer"];
            _audience = _configuration["Jwt:Audience"];
        }

        /// <summary>
        /// 获取客户端的 IP 地址
        /// </summary>
        /// <returns>返回客户端的 IP 地址，如果无法获取则返回空字符串</returns>
        private string GetClientIpAddress()
        {
            // 从请求头中获取代理服务器（如果有的话）设置的客户端 IP 地址
            string remoteIpAddress = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();
            return remoteIpAddress ?? "NotAvailable"; // 如果为空，返回默认值
        }

        /// <summary>
        /// 登录方法，验证用户名和密码，并返回结果及JWT Token
        /// </summary>
        /// <param name="userName">用户名</param>
        /// <param name="password">密码</param>
        /// <returns>返回一个包含登录成功与否、信息和JWT Token的元组</returns>
        public async Task<(bool Success, string Message, string Token, UserDto UserInfo)> LoginAsync(string userName, string password)
        {
            // 使用Include确保加载所需的关联数据
            ApplicationUser user = await _userManager.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                        .ThenInclude(r => r.RolePermission)
                .FirstOrDefaultAsync(u => u.UserName == userName);
            LoginLog loginLog = CreateLoginLog(user);

            // 如果用户不存在，记录登录日志并返回失败信息
            if (user == null)
            {
                return (false, ErrorMessages.InvalidCredentials, null, null);
            }

            // 检查用户密码是否正确并处理结果
            SignInResult result = await _signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);

            return result.Succeeded ? await HandleSuccessfulLoginAsync(user, loginLog) : await HandleFailedLoginAsync(result, loginLog);
        }

        /// <summary>
        /// 模拟用户登录，直接生成JWT Token而不验证密码
        /// </summary>
        /// <param name="userName">用户名</param>
        /// <returns>返回登录结果</returns>
        public async Task<(bool Success, string Message, string Token, UserDto UserInfo)> ImpersonateLoginAsync(string userName)
        {
            // 使用Include确保加载所需的关联数据
            ApplicationUser user = await _userManager.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                        .ThenInclude(r => r.RolePermission)
                .FirstOrDefaultAsync(u => u.UserName == userName);

            LoginLog loginLog = CreateLoginLog(user);

            // 如果用户不存在，记录登录日志并返回失败信息
            if (user == null)
            {
                return (false, ErrorMessages.InvalidCredentials, null, null);
            }

            // 模拟登录：直接生成token
            return await HandleSuccessfulLoginAsync(user, loginLog);
        }

        /// <summary>
        /// 创建登录日志，记录登录尝试
        /// </summary>
        /// <param name="user">用户对象</param>
        /// <param name="userName">用户名</param>
        /// <returns>返回登录日志对象</returns>
        private LoginLog CreateLoginLog(ApplicationUser user)
        {
            return new LoginLog
            {
                UserId = user?.Id,
                UserName = user?.UserName,
                LoginTime = DateTime.UtcNow,
                IPAddress = GetClientIpAddress(),  // 从 HTTP 上下文中获取客户端 IP 地址
                IsSuccess = false,
                FailureReason = null
            };
        }

        /// <summary>
        /// 处理登录成功的逻辑，记录日志并生成JWT Token
        /// </summary>
        /// <param name="user">登录的用户</param>
        /// <param name="loginLog">登录日志</param>
        /// <returns>返回一个包含登录成功、消息、JWT Token以及用户信息的元组</returns>
        private async Task<(bool Success, string Message, string Token, UserDto UserInfo)> HandleSuccessfulLoginAsync(ApplicationUser user, LoginLog loginLog)
        {
            // 更新最后登录时间
            user.LastLoginTime = DateTimeOffset.UtcNow;
            await _userManager.UpdateAsync(user);

            // 登录成功，更新日志并保存
            loginLog.IsSuccess = true;
            await _loginLogRepository.AddAsync(loginLog);

            // 生成JWT Token
            string token = GenerateJwtToken(user);

            // 将用户对象映射到DTO对象
            UserDto userDto = _mapper.Map<UserDto>(user);

            return (true, "登录成功", token, userDto);
        }

        /// <summary>
        /// 处理登录失败的逻辑，记录失败原因并返回失败消息
        /// </summary>
        /// <param name="result">登录结果</param>
        /// <param name="loginLog">登录日志</param>
        /// <returns>返回一个包含登录失败、消息、JWT Token和用户信息的元组</returns>
        private async Task<(bool Success, string Message, string Token, UserDto UserInfo)> HandleFailedLoginAsync(SignInResult result, LoginLog loginLog)
        {
            // 记录失败原因
            loginLog.FailureReason = result.IsLockedOut ? "账户被锁定" : "密码不正确";
            await _loginLogRepository.AddAsync(loginLog);

            // 更新访问失败次数已经由 SignInManager 在 CheckPasswordSignInAsync 中自动处理
            // 因为我们在调用时设置了 lockoutOnFailure: true

            return result.IsLockedOut
                ? (false, ErrorMessages.AccountLocked, null, null)
                : (false, ErrorMessages.InvalidCredentials, null, null);
        }

        /// <summary>
        /// 生成JWT Token
        /// </summary>
        /// <param name="user">用户对象</param>
        /// <returns>返回生成的JWT Token字符串</returns>
        private string GenerateJwtToken(ApplicationUser user)
        {
            // 构建JWT声明
            List<Claim> claims =
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),// JWT ID
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName),
            ];

            // 添加角色声明
            if (user.UserRoles != null)
            {
                foreach (ApplicationUserRole role in user.UserRoles.Where(ur => ur.Role != null))
                {
                    claims.Add(new Claim(ClaimTypes.Role, role.Role.Name));
                }
            }

            // 添加权限声明
            IEnumerable<string> allPermissions = user.UserRoles?
                .Where(ur => ur.Role?.RolePermission != null)
                .SelectMany(ur => ur.Role.RolePermission.PermissionIds ?? Array.Empty<string>())
                .Distinct() ?? Enumerable.Empty<string>();

            foreach (string permission in allPermissions)
            {
                claims.Add(new Claim("permissions", permission));
            }

            // 创建加密密钥和签名凭证
            SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(_secretKey));
            SigningCredentials credentials = new(key, SecurityAlgorithms.HmacSha256);

            // 生成JWT Token
            JwtSecurityToken token = new(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                expires: DateTime.Now.AddMinutes(30),
                signingCredentials: credentials
            );

            // 返回JWT Token字符串
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    /// <summary>
    /// 存放错误消息的静态类
    /// </summary>
    public static class ErrorMessages
    {
        public const string InvalidCredentials = "用户名或密码不正确，请重新输入！";
        public const string AccountLocked = "账户被锁定，请稍后再试！";
    }
}
