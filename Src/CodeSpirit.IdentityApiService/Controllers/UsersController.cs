using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.IdentityApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CodeSpirit.IdentityApi.Controllers.Dtos;

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
    public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
    {
        var users = await _userManager.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .ToListAsync();

        var userDtos = users.Select(u => new UserDto
        {
            Id = u.Id,
            Name = u.Name,
            IdNo = u.IdNo,
            AvatarUrl = u.AvatarUrl,
            LastLoginTime = u.LastLoginTime,
            IsActive = u.IsActive,
            UserName = u.UserName,
            Email = u.Email,
            Roles = u.UserRoles.Select(ur => ur.Role.Name).ToList()
        });

        return Ok(new
        {
            status = 0,
            msg = "查询成功！",
            data = new
            {
                items = userDtos,
                total = userDtos.Count()
            }
        });
    }

    // GET: api/Users/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetUser(string id)
    {
        var user = await _userManager.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            return NotFound();
        }

        var userDto = new UserDto
        {
            Id = user.Id,
            Name = user.Name,
            IdNo = user.IdNo,
            AvatarUrl = user.AvatarUrl,
            LastLoginTime = user.LastLoginTime,
            IsActive = user.IsActive,
            Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList()
        };

        return Ok(userDto);
    }

    // POST: api/Users
    [HttpPost]
    public async Task<ActionResult<UserDto>> CreateUser(CreateUserDto createUserDto)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString("N"),
            UserName = createUserDto.Email,
            Email = createUserDto.Email,
            Name = createUserDto.Name,
            IdNo = createUserDto.IdNo,
            AvatarUrl = createUserDto.AvatarUrl,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, createUserDto.Password);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
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
            Name = user.Name,
            IdNo = user.IdNo,
            AvatarUrl = user.AvatarUrl,
            LastLoginTime = user.LastLoginTime,
            IsActive = user.IsActive,
            Roles = createUserDto.Roles
        };

        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, userDto);
    }

    // PUT: api/Users/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(string id, UpdateUserDto updateUserDto)
    {
        var user = await _userManager.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            return NotFound();
        }

        user.Name = updateUserDto.Name;
        user.IdNo = updateUserDto.IdNo;
        user.AvatarUrl = updateUserDto.AvatarUrl;
        user.IsActive = updateUserDto.IsActive;

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return BadRequest(updateResult.Errors);
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

        return NoContent();
    }

    // DELETE: api/Users/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        // 逻辑删除：设置 IsActive 为 false
        user.IsActive = false;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return NoContent();
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
}
