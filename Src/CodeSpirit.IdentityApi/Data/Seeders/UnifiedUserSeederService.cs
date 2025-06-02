using CodeSpirit.Core.IdGenerator;
using CodeSpirit.IdentityApi.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CodeSpirit.Core;

namespace CodeSpirit.IdentityApi.Data.Seeders
{
    /// <summary>
    /// 统一的用户种子数据服务
    /// </summary>
    public class UnifiedUserSeederService : IUserSeederService, IScopedDependency
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IPasswordHasher<ApplicationUser> _passwordHasher;
        private readonly ILogger<UnifiedUserSeederService> _logger;
        private readonly IIdGenerator _idGenerator;
        private readonly IHostEnvironment _hostEnvironment;

        /// <summary>
        /// 构造函数
        /// </summary>
        public UnifiedUserSeederService(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IPasswordHasher<ApplicationUser> passwordHasher,
            ILogger<UnifiedUserSeederService> logger,
            IIdGenerator idGenerator,
            IHostEnvironment hostEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _passwordHasher = passwordHasher;
            _logger = logger;
            _idGenerator = idGenerator;
            _hostEnvironment = hostEnvironment;
        }

        /// <summary>
        /// 确保用户存在
        /// </summary>
        public async Task<ApplicationUser> EnsureUserExistsAsync(string userName, string email, string displayName, string password, string tenantId)
        {
            // 查找现有用户
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.TenantId == tenantId &&
                                         (u.UserName == userName || u.NormalizedUserName == userName.ToUpper()));

            if (existingUser == null)
            {
                _logger.LogInformation("创建用户: {UserName} (租户: {TenantId})", userName, tenantId);

                var newUser = new ApplicationUser
                {
                    Id = _idGenerator.NewId(),
                    TenantId = tenantId,
                    UserName = userName,
                    NormalizedUserName = userName.ToUpper(),
                    Email = email,
                    NormalizedEmail = email.ToUpper(),
                    EmailConfirmed = true,
                    Name = displayName,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = 1L,
                    IsDeleted = false,
                    SecurityStamp = Guid.NewGuid().ToString(),
                    ConcurrencyStamp = Guid.NewGuid().ToString()
                };

                // 对于系统用户，使用DbContext直接操作
                // 对于业务用户，使用UserManager
                if (tenantId == TenantConstants.SystemTenantId)
                {
                    // 设置密码
                    newUser.PasswordHash = _passwordHasher.HashPassword(newUser, password);
                    _context.Users.Add(newUser);
                }
                else
                {
                    var result = await _userManager.CreateAsync(newUser, password);
                    if (!result.Succeeded)
                    {
                        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                        throw new InvalidOperationException($"创建用户失败: {errors}");
                    }
                }

                _logger.LogInformation("用户创建完成: {UserName}, ID: {UserId}, 默认密码: {Password}",
                    userName, newUser.Id, password);
                return newUser;
            }
            else
            {
                _logger.LogInformation("用户已存在: {UserName}, ID: {UserId}", existingUser.UserName, existingUser.Id);

                // 检查是否有密码，如果没有则设置密码
                if (string.IsNullOrEmpty(existingUser.PasswordHash))
                {
                    _logger.LogWarning("用户没有密码，设置默认密码...");
                    
                    if (tenantId == TenantConstants.SystemTenantId)
                    {
                        existingUser.PasswordHash = _passwordHasher.HashPassword(existingUser, password);
                        _context.Users.Update(existingUser);
                    }
                    else
                    {
                        var token = await _userManager.GeneratePasswordResetTokenAsync(existingUser);
                        await _userManager.ResetPasswordAsync(existingUser, token, password);
                    }
                    
                    _logger.LogInformation("用户密码已设置: {Password}", password);
                }

                return existingUser;
            }
        }

        /// <summary>
        /// 批量创建用户
        /// </summary>
        public async Task<List<ApplicationUser>> CreateUsersBatchAsync(List<UserDefinition> users)
        {
            var createdUsers = new List<ApplicationUser>();

            foreach (var userDefinition in users)
            {
                var user = await EnsureUserExistsAsync(
                    userDefinition.UserName, 
                    userDefinition.Email, 
                    userDefinition.DisplayName, 
                    userDefinition.Password, 
                    userDefinition.TenantId);
                    
                createdUsers.Add(user);
            }

            return createdUsers;
        }

        /// <summary>
        /// 确保用户角色关联存在
        /// </summary>
        public async Task EnsureUserRoleExistsAsync(long userId, long roleId, string tenantId)
        {
            try
            {
                var existingUserRole = await _context.UserRoles
                    .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

                if (existingUserRole == null)
                {
                    _logger.LogInformation("为用户分配角色: UserId={UserId}, RoleId={RoleId}, TenantId={TenantId}", userId, roleId, tenantId);

                    var newUserRole = new ApplicationUserRole
                    {
                        UserId = userId,
                        RoleId = roleId,
                        TenantId = tenantId,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.UserRoles.Add(newUserRole);
                    
                    // 使用单独的保存操作，避免与其他实体的事务冲突
                    try
                    {
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("用户角色关联创建完成");
                    }
                    catch (Microsoft.EntityFrameworkCore.DbUpdateException ex) when (ex.InnerException?.Message?.Contains("duplicate key") == true)
                    {
                        // 处理并发插入导致的重复键异常
                        _logger.LogWarning("用户角色关联已存在（并发创建）: UserId={UserId}, RoleId={RoleId}", userId, roleId);
                        
                        // 清理添加的实体，避免ChangeTracker冲突
                        _context.Entry(newUserRole).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                    }
                }
                else
                {
                    _logger.LogInformation("用户角色关联已存在: UserId={UserId}, RoleId={RoleId}", userId, roleId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "确保用户角色关联存在时发生错误: UserId={UserId}, RoleId={RoleId}, TenantId={TenantId}", 
                    userId, roleId, tenantId);
                throw;
            }
        }

        /// <summary>
        /// 获取预定义的系统用户
        /// </summary>
        public List<UserDefinition> GetSystemUsers()
        {
            return new List<UserDefinition>
            {
                new()
                {
                    UserName = "systemadmin",
                    Email = "systemadmin@system.local",
                    DisplayName = "系统管理员",
                    Password = "CodeSpirit@2025",
                    TenantId = TenantConstants.SystemTenantId,
                    IsSystemUser = true,
                    Roles = new List<string> { "Admin" }
                }
            };
        }

        /// <summary>
        /// 获取预定义的业务用户
        /// </summary>
        public List<UserDefinition> GetBusinessUsers()
        {
            var users = new List<UserDefinition>
            {
                new()
                {
                    UserName = "admin",
                    Email = "admin@example.com",
                    DisplayName = "Admin",
                    Password = "123@Admin",
                    TenantId = TenantConstants.DefaultTenantId,
                    IsSystemUser = false,
                    Roles = new List<string> { "Admin" }
                }
            };

            // 只在开发环境下添加随机测试用户
            if (_hostEnvironment.IsDevelopment())
            {
                // 可以在这里添加更多的测试用户定义
                // 但通常随机用户应该在运行时动态生成，而不是预定义
            }

            return users;
        }

        /// <summary>
        /// 创建随机用户（开发环境专用）
        /// </summary>
        /// <param name="userCount">用户数量</param>
        /// <returns></returns>
        public async Task CreateRandomUsersAsync(int userCount)
        {
            if (!_hostEnvironment.IsDevelopment())
            {
                return;
            }

            if (await _userManager.Users.CountAsync() > 10) // 如果已有足够用户则跳过
            {
                _logger.LogInformation("已有足够用户，跳过随机用户创建");
                return;
            }

            Random random = new();
            Gender[] genderValues = Enum.GetValues(typeof(Gender)).Cast<Gender>().ToArray();

            for (int i = 0; i < userCount; i++)
            {
                string userName = $"user{random.Next(1000, 9999)}";
                int avatarStyle = random.Next(0, 3);
                string avatarUrl = avatarStyle switch
                {
                    0 => $"https://avatars.dicebear.com/api/identicon/{userName}.svg",
                    1 => $"https://avatars.dicebear.com/api/bottts/{userName}.svg",
                    2 => $"https://avatars.dicebear.com/api/avataaars/{userName}.svg",
                    _ => $"https://avatars.dicebear.com/api/identicon/{userName}.svg",
                };

                DateTimeOffset lastLoginTime = GetRandomDate(DateTime.Now.AddMonths(-3), DateTime.Now);

                ApplicationUser user = new()
                {
                    Id = _idGenerator.NewId(),
                    UserName = userName,
                    Email = $"{userName}@example.com",
                    LastLoginTime = lastLoginTime,
                    EmailConfirmed = true,
                    Name = $"User {random.Next(1000, 9999)}",
                    Gender = genderValues[random.Next(genderValues.Length)],
                    IsActive = random.Next(1, 10) % 2 == 0,
                    AvatarUrl = avatarUrl,
                    PhoneNumber = GenerateRandomPhoneNumber(random),
                    IdNo = GenerateRandomIdNumber(random),
                    TenantId = TenantConstants.DefaultTenantId
                };

                if (i > 5)
                {
                    user.LockoutEnd = DateTimeOffset.Now.AddHours(5);
                }

                IdentityResult result = await _userManager.CreateAsync(user, "Password@123");
                if (result.Succeeded)
                {
                    _logger.LogInformation("随机用户 {UserName} 创建成功", user.UserName);
                }
                else
                {
                    _logger.LogError("创建随机用户 {UserName} 失败：{Errors}", 
                        user.UserName, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }

        private DateTimeOffset GetRandomDate(DateTime startDate, DateTime endDate)
        {
            Random random = new();
            TimeSpan range = endDate - startDate;
            TimeSpan randomTimeSpan = new((long)(random.NextDouble() * range.Ticks));
            return startDate + randomTimeSpan;
        }

        private string GenerateRandomPhoneNumber(Random random)
        {
            // 手机号码格式：1[3-9]后面9位随机数
            string[] prefixes = { "3", "4", "5", "6", "7", "8", "9" };
            string prefix = prefixes[random.Next(prefixes.Length)];
            return $"1{prefix}{random.Next(100000000, 999999999)}";
        }

        private string GenerateRandomIdNumber(Random random)
        {
            // 身份证号码格式：6位地区码 + 8位出生日期 + 3位顺序码 + 1位校验码
            string[] areaCodes = { "110000", "310000", "440100", "510100", "330100" }; // 示例地区码
            string areaCode = areaCodes[random.Next(areaCodes.Length)];

            // 生成1970-2000年之间的随机出生日期
            DateTime startDate = new(1970, 1, 1);
            DateTime endDate = new(2000, 12, 31);
            DateTime birthDate = startDate.AddDays(random.Next((endDate - startDate).Days));
            string birthDateStr = birthDate.ToString("yyyyMMdd");

            // 生成3位顺序码
            string sequenceCode = random.Next(100, 999).ToString();

            // 简化版的校验码生成（实际的校验码计算更复杂）
            string[] checkCodes = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "X" };
            string checkCode = checkCodes[random.Next(checkCodes.Length)];

            return $"{areaCode}{birthDateStr}{sequenceCode}{checkCode}";
        }
    }
} 