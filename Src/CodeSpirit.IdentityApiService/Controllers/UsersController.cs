using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.IdentityApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CodeSpirit.IdentityApi.Controllers.Dtos;
using System.Linq.Dynamic.Core;
using System.Linq.Dynamic.Core.Exceptions;
using CodeSpirit.IdentityApi.Utilities; // 引入动态 LINQ

[Route("api/[controller]")]
[ApiController]
public class UsersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ApplicationDbContext _context;

    public UsersController(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ApplicationDbContext context)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
    }

    // GET: api/Users
    [HttpGet]
    public async Task<ActionResult<ApiResponse<ListData<UserDto>>>> GetUsers([FromQuery] UserQueryDto queryDto)
    {
        // 输入验证
        if (queryDto.PageNumber < 1)
        {
            return BadRequest(new ApiResponse<string>(1, "页码必须大于或等于1。", null));
        }

        if (queryDto.PageSize < 1 || queryDto.PageSize > 100)
        {
            return BadRequest(new ApiResponse<string>(1, "每页条数必须在1到100之间。", null));
        }

        try
        {
            // 1) 基础查询
            var query = _userManager.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .AsQueryable();

            // 2) 关键字搜索
            if (!string.IsNullOrWhiteSpace(queryDto.Search))
            {
                var searchLower = queryDto.Search.ToLower();
                query = query.Where(u =>
                    u.Name.ToLower().Contains(searchLower) ||
                    u.Email.ToLower().Contains(searchLower) ||
                    u.IdNo.Contains(queryDto.Search) ||
                    u.UserName.ToLower().Contains(searchLower));
            }

            // 3) 是否激活筛选
            if (queryDto.IsActive.HasValue)
            {
                query = query.Where(u => u.IsActive == queryDto.IsActive.Value);
            }

            // 4) 性别筛选
            if (queryDto.Gender.HasValue)
            {
                query = query.Where(u => u.Gender == queryDto.Gender.Value);
            }

            // 5) 角色筛选
            if (!string.IsNullOrWhiteSpace(queryDto.Role))
            {
                var roleName = queryDto.Role.Trim();
                query = query.Where(u => u.UserRoles.Any(ur => ur.Role.Name == roleName));
            }

            // 6) 动态排序
            if (!string.IsNullOrWhiteSpace(queryDto.SortField))
            {
                // 规范化排序字段，防止注入和错误
                var sortField = queryDto.SortField.Trim();

                // 定义允许排序的字段
                var allowedSortFields = new List<string>
                {
                    "name",
                    "email",
                    "lastlogintime",
                    "username",
                    "gender",
                    "phonenumber"
                    // 其他允许排序的字段可以在这里添加
                };

                if (!allowedSortFields.Contains(sortField.ToLower()))
                {
                    return BadRequest(new { msg = $"不支持的排序字段: {sortField}" });
                }

                var sortOrder = queryDto.SortOrder?.ToLower() == "desc" ? "descending" : "ascending";

                // 构建动态排序字符串
                var ordering = $"{sortField} {sortOrder}";

                try
                {
                    query = query.OrderBy(ordering);
                }
                catch (ParseException)
                {
                    return BadRequest(new { msg = "排序字段格式错误。" });
                }
            }
            else
            {
                // 默认排序
                query = query.OrderBy(u => u.Id);
            }

            // 7) 计算总数（分页前）
            var totalCount = await query.CountAsync();

            // 8) 分页
            var skipCount = (queryDto.PageNumber - 1) * queryDto.PageSize;
            var users = await query
                .Skip(skipCount)
                .Take(queryDto.PageSize)
                .ToListAsync();

            // 9) 转换为 Dto
            var userDtos = users.Select(u => new UserDto
            {
                Id = u.Id,
                UserName = u.UserName,
                Email = u.Email,
                Name = u.Name,
                IdNo = u.IdNo,
                AvatarUrl = u.AvatarUrl,
                LastLoginTime = u.LastLoginTime,
                IsActive = u.IsActive,
                PhoneNumber = u.PhoneNumber,
                Gender = u.Gender,
                Roles = u.UserRoles.Select(ur => ur.Role.Name).ToList()
            }).ToList();

            // 10) 封装列表数据
            var listData = new ListData<UserDto>(userDtos, totalCount);

            // 11) 封装响应
            var response = new ApiResponse<ListData<UserDto>>(0, "查询成功！", listData);
            return Ok(response);
        }
        catch (Exception)
        {
            // 记录异常（根据实际项目配置日志服务）
            // _logger.LogError(ex, "获取用户列表时发生异常。");
            return StatusCode(500, new ApiResponse<string>(1, "服务器内部错误，请稍后再试。", null));
        }
    }

    // GET: api/Users/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetUser(string id)
    {
        var user = await _userManager.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            return NotFound(new ApiResponse<string>(1, "用户不存在！", null));
        }

        var userDto = new UserDto
        {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            Name = user.Name,
            IdNo = user.IdNo,
            AvatarUrl = user.AvatarUrl,
            LastLoginTime = user.LastLoginTime,
            IsActive = user.IsActive,
            PhoneNumber = user.PhoneNumber,
            Gender = user.Gender,
            Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList()
        };

        var response = new ApiResponse<UserDto>(0, "查询成功！", userDto);

        return Ok(response);
    }


    // POST: api/Users
    [HttpPost]
    public async Task<ActionResult<ApiResponse<UserDto>>> CreateUser(CreateUserDto createUserDto)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString("N"),
            UserName = createUserDto.Email,
            Email = createUserDto.Email,
            Name = createUserDto.Name,
            IdNo = createUserDto.IdNo,
            AvatarUrl = createUserDto.AvatarUrl,
            IsActive = true, // 创建用户默认为激活状态
            Gender = createUserDto.Gender,
            PhoneNumber = createUserDto.PhoneNumber
        };

        var result = await _userManager.CreateAsync(user, createUserDto.Password);
        if (!result.Succeeded)
        {
            return BadRequest(new ApiResponse<string>(1, "创建用户失败！", null));
        }

        if (createUserDto.Roles != null && createUserDto.Roles.Any())
        {
            foreach (var role in createUserDto.Roles)
            {
                if (await _roleManager.RoleExistsAsync(role))
                {
                    await _userManager.AddToRoleAsync(user, role);
                }
            }
        }

        var userDto = new UserDto
        {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            Name = user.Name,
            IdNo = user.IdNo,
            AvatarUrl = user.AvatarUrl,
            LastLoginTime = user.LastLoginTime,
            IsActive = user.IsActive,
            PhoneNumber = user.PhoneNumber,
            Gender = user.Gender,
            Roles = createUserDto.Roles ?? new List<string>()
        };

        var response = new ApiResponse<UserDto>(0, "用户创建成功！", userDto);

        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, response);
    }


    // PUT: api/Users/{id}
    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<string>>> UpdateUser(string id, UpdateUserDto updateUserDto)
    {
        var user = await _userManager.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            return NotFound(new ApiResponse<string>(1, "用户不存在！", null));
        }

        user.Name = updateUserDto.Name;
        user.IdNo = updateUserDto.IdNo;
        user.AvatarUrl = updateUserDto.AvatarUrl;
        user.IsActive = updateUserDto.IsActive;
        user.Gender = updateUserDto.Gender;
        user.PhoneNumber = updateUserDto.PhoneNumber;

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return BadRequest(new ApiResponse<string>(1, "更新用户失败！", null));
        }

        // 更新角色
        if (updateUserDto.Roles != null)
        {
            var currentRoles = await _userManager.GetRolesAsync(user);
            var rolesToAdd = updateUserDto.Roles.Except(currentRoles);
            var rolesToRemove = currentRoles.Except(updateUserDto.Roles);

            if (rolesToAdd.Any())
            {
                foreach (var role in rolesToAdd)
                {
                    if (await _roleManager.RoleExistsAsync(role))
                    {
                        await _userManager.AddToRoleAsync(user, role);
                    }
                }
            }

            if (rolesToRemove.Any())
            {
                await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
            }
        }

        var response = new ApiResponse<string>(0, "用户更新成功！", null);

        return Ok(response);
    }

    // DELETE: api/Users/{id}
    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<string>>> DeleteUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound(new ApiResponse<string>(1, "用户不存在！", null));
        }

        // 逻辑删除：设置 IsActive = false
        user.IsActive = false;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return BadRequest(new ApiResponse<string>(1, "禁用用户失败！", null));
        }

        var response = new ApiResponse<string>(0, "用户禁用成功！", null);

        return NoContent(); // 或者返回 Ok(response);
    }


    // 额外：分配角色给用户
    [HttpPost("{id}/roles")]
    public async Task<IActionResult> AssignRoles(string id, [FromBody] List<string> roles)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        var validRoles = roles.Where(r => _roleManager.RoleExistsAsync(r).Result).ToList();
        var result = await _userManager.AddToRolesAsync(user, validRoles);

        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Ok();
    }

    // 额外：移除用户的角色
    [HttpDelete("{id}/roles")]
    public async Task<IActionResult> RemoveRoles(string id, [FromBody] List<string> roles)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        var userRoles = await _userManager.GetRolesAsync(user);
        var rolesToRemove = roles.Intersect(userRoles).ToList();

        var result = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);

        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Ok();
    }

    /// <summary>
    /// 合并激活和禁用：通过参数 isActive 设置激活(true) 或禁用(false)
    /// PUT: /api/Users/{id}/setActive?isActive=true/false
    /// </summary>
    [HttpPut("{id}/setActive")]
    public async Task<ActionResult<ApiResponse<string>>> SetActiveStatus(string id, [FromQuery] bool isActive)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound(new ApiResponse<string>(1, "用户不存在！", null));
        }

        // 如果状态和当前一致，则无需重复设置
        if (user.IsActive == isActive)
        {
            var msg = isActive ? "用户已处于激活状态，无需重复操作。" : "用户已处于禁用状态，无需重复操作。";
            return BadRequest(new ApiResponse<string>(1, msg, null));
        }

        user.IsActive = isActive;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return BadRequest(new ApiResponse<string>(1, "更新用户状态失败！", null));
        }

        var status = isActive ? "激活" : "禁用";
        var responseMsg = $"用户已{status}成功！";

        var response = new ApiResponse<string>(0, responseMsg, null);

        return Ok(response);
    }


    /// <summary>
    /// 随机生成新密码并重置（无需管理员输入新密码）
    /// POST: /api/Users/{id}/resetRandomPassword
    /// </summary>
    [HttpPost("{id}/resetRandomPassword")]
    public async Task<ActionResult<ApiResponse<string>>> ResetRandomPassword(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound(new ApiResponse<string>(1, "用户不存在！", null));
        }

        // 生成一个新的随机密码
        var newPassword = PasswordGenerator.GenerateRandomPassword(12); // 推荐使用12位或更长

        // 生成重置密码令牌
        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);

        // 使用令牌执行密码重置
        var resetResult = await _userManager.ResetPasswordAsync(user, resetToken, newPassword);
        if (!resetResult.Succeeded)
        {
            return BadRequest(new ApiResponse<string>(1, "密码重置失败！", null));
        }

        // 在实际应用中，建议通过安全渠道（如邮件）将新密码发送给用户
        var response = new ApiResponse<string>(0, "密码已重置成功！", newPassword);

        return Ok(response);
    }


    /// <summary>
    /// 3) 解除用户锁定
    /// 如果用户因多次密码错误等原因被锁定，管理员可手动解锁
    /// PUT: /api/Users/{id}/unlock
    /// </summary>
    [HttpPut("{id}/unlock")]
    public async Task<IActionResult> UnlockUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound(new { msg = "用户不存在！" });
        }

        // 查看用户的 LockoutEnd 时间
        var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
        if (!lockoutEnd.HasValue || lockoutEnd.Value <= DateTimeOffset.UtcNow)
        {
            return BadRequest(new { msg = "该用户未被锁定，无需解锁。" });
        }

        // 将锁定截止日期置空表示解除锁定
        var setLockoutResult = await _userManager.SetLockoutEndDateAsync(user, null);
        if (!setLockoutResult.Succeeded)
        {
            return BadRequest(new { msg = "解除锁定失败！", errors = setLockoutResult.Errors });
        }

        // 同时可重置失败次数
        user.AccessFailedCount = 0;
        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return BadRequest(new { msg = "更新用户信息失败！", errors = updateResult.Errors });
        }

        return Ok(new { msg = "用户已成功解锁。" });
    }
}