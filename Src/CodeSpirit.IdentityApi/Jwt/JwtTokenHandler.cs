using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.IdentityApi.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace CodeSpirit.IdentityApi.Jwt
{
    /// <summary>
    /// JWT令牌处理实现
    /// </summary>
    public class JwtTokenHandler : IJwtTokenHandler
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<JwtTokenHandler> _logger;
        private readonly IRoleService _roleService;
        
        private readonly string _issuer;
        private readonly string _audience;
        private readonly string _secretKey;
        private readonly int _expirationMinutes;

        public JwtTokenHandler(
            UserManager<ApplicationUser> userManager, 
            IConfiguration configuration,
            ILogger<JwtTokenHandler> logger,
            IRoleService roleService)
        {
            _userManager = userManager;
            _configuration = configuration;
            _logger = logger;
            _roleService = roleService;
            
            _issuer = _configuration["Jwt:Issuer"];
            _audience = _configuration["Jwt:Audience"];
            _secretKey = _configuration["Jwt:SecretKey"];
            
            if (!int.TryParse(_configuration["Jwt:ExpirationMinutes"], out _expirationMinutes))
            {
                _expirationMinutes = 180; // 默认3小时
            }
        }

        /// <summary>
        /// 生成访问令牌
        /// </summary>
        /// <param name="user">用户信息</param>
        /// <returns>访问令牌</returns>
        public async Task<string> GenerateTokenAsync(ApplicationUser user)
        {
            try
            {
                // 构建JWT声明
                var claims = new List<Claim>
                {
                    new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // JWT ID
                    new Claim(ClaimTypes.Name, user.Name),
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                    new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName)
                };

                // 添加角色声明
                var roles = await _userManager.GetRolesAsync(user);
                foreach (var role in roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }

                //// 获取用户权限并添加到令牌中
                //var permissions = await _roleService.GetUserPermissionsAsync(user.Id);
                //foreach (var permission in permissions)
                //{
                //    claims.Add(new Claim("permissions", permission));
                //}

                // 创建加密密钥和签名凭证
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
                var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                // 生成JWT Token
                var token = new JwtSecurityToken(
                    issuer: _issuer,
                    audience: _audience,
                    claims: claims,
                    expires: DateTime.UtcNow.AddMinutes(_expirationMinutes),
                    signingCredentials: credentials
                );

                // 返回JWT Token字符串
                return new JwtSecurityTokenHandler().WriteToken(token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成JWT令牌时发生错误");
                throw;
            }
        }

        /// <summary>
        /// 验证令牌而不检查过期时间
        /// </summary>
        /// <param name="token">令牌</param>
        /// <returns>ClaimsPrincipal</returns>
        public ClaimsPrincipal ValidateTokenWithoutLifetime(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));

                // 设置令牌验证参数，不验证过期时间
                var tokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateIssuer = true,
                    ValidIssuer = _issuer,
                    ValidateAudience = true,
                    ValidAudience = _audience,
                    ValidateLifetime = false, // 不验证过期时间
                    ClockSkew = TimeSpan.Zero
                };

                // 解析和验证令牌
                return tokenHandler.ValidateToken(token, tokenValidationParameters, out _);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证JWT令牌时发生错误: {token}", token);
                return null;
            }
        }
    }
} 