using CodeSpirit.Core.IdGenerator;
using CodeSpirit.IdentityApi.Data.Models;
using Microsoft.AspNetCore.Identity;

public class UserSeeder: IScopedDependency
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<UserSeeder> _logger;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IIdGenerator _idGenerator;

    public UserSeeder(
        IServiceProvider serviceProvider,
        ILogger<UserSeeder> logger,
        RoleManager<ApplicationRole> roleManager,
        UserManager<ApplicationUser> userManager,
        IIdGenerator idGenerator)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _roleManager = roleManager;
        _userManager = userManager;
        _idGenerator = idGenerator;
    }

    public async Task SeedAdminUserAsync()
    {
        ApplicationUser adminUser = await _userManager.FindByNameAsync("admin");

        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                Id = _idGenerator.NewId(),
                UserName = "admin",
                Email = "admin@example.com",
                EmailConfirmed = true,
                Name = "Admin",
                IsActive = true,
                Gender = Gender.Unknown
            };

            IdentityResult result = await _userManager.CreateAsync(adminUser, "Admin@123");
            if (result.Succeeded)
            {
                _logger.LogInformation("管理员用户创建成功。");
            }
            else
            {
                _logger.LogError("创建管理员用户失败：");
                foreach (IdentityError error in result.Errors)
                {
                    _logger.LogError($" - {error.Description}");
                }
                return;
            }
        }
        else
        {
            _logger.LogInformation("管理员用户已存在，跳过创建。");
        }

        if (!await _roleManager.RoleExistsAsync("Admin"))
        {
            IdentityResult createRoleResult = await _roleManager.CreateAsync(new ApplicationRole() { Name = "Admin" });
            if (createRoleResult.Succeeded)
            {
                _logger.LogInformation("管理员角色创建成功。");
            }
            else
            {
                _logger.LogError("创建管理员角色失败：");
                foreach (IdentityError error in createRoleResult.Errors)
                {
                    _logger.LogError($" - {error.Description}");
                }
                return;
            }
        }

        if (!await _userManager.IsInRoleAsync(adminUser, "Admin"))
        {
            IdentityResult addToRoleResult = await _userManager.AddToRoleAsync(adminUser, "Admin");
            if (addToRoleResult.Succeeded)
            {
                _logger.LogInformation("管理员用户已分配 'Admin' 角色。");
            }
            else
            {
                _logger.LogError("分配 'Admin' 角色失败：");
                foreach (IdentityError error in addToRoleResult.Errors)
                {
                    _logger.LogError($" - {error.Description}");
                }
            }
        }
        else
        {
            _logger.LogInformation("管理员用户已存在 'Admin' 角色，跳过角色分配。");
        }
    }

    public async Task SeedRandomUsersAsync(int userCount, RoleManager<ApplicationRole> roleManager)
    {
        Random random = new();
        Gender[] genderValues = Enum.GetValues(typeof(Gender)).Cast<Gender>().ToArray();

        ApplicationRole defaultRole = await _roleManager.FindByNameAsync("User");
        if (defaultRole == null)
        {
            _logger.LogWarning("未找到默认角色 'User'，将跳过角色分配！");
        }

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
            DateTimeOffset createTime = GetRandomDate(DateTime.Now.AddMonths(-1), DateTime.Now);

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
                CreationTime = createTime.DateTime,
                PhoneNumber = GenerateRandomPhoneNumber(random),
                IdNo = GenerateRandomIdNumber(random)
            };

            if (i > 5)
            {
                user.LockoutEnd = DateTimeOffset.Now.AddHours(5);
            }

            IdentityResult result = await _userManager.CreateAsync(user, "Password@123");
            if (result.Succeeded)
            {
                _logger.LogInformation($"用户 {user.UserName} 创建成功。");

                if (defaultRole != null)
                {
                    IdentityResult addToRoleResult = await _userManager.AddToRoleAsync(user, defaultRole.Name);
                    if (addToRoleResult.Succeeded)
                    {
                        _logger.LogInformation($"用户 {user.UserName} 被分配到角色 '{defaultRole.Name}'。");
                    }
                    else
                    {
                        _logger.LogError($"分配角色 '{defaultRole.Name}' 给用户 {user.UserName} 失败：");
                        foreach (IdentityError error in addToRoleResult.Errors)
                        {
                            _logger.LogError($" - {error.Description}");
                        }
                    }
                }
            }
            else
            {
                _logger.LogError($"创建用户 {user.UserName} 失败：");
                foreach (IdentityError error in result.Errors)
                {
                    _logger.LogError($" - {error.Description}");
                }
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



