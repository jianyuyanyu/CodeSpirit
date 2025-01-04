// Controllers/UserRolesController.cs
using CodeSpirit.IdentityApi.Controllers.Dtos;
using CodeSpirit.IdentityApi.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CodeSpirit.IdentityApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Administrator")] // 仅管理员可以管理用户角色
    public class UserRolesController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;

        public UserRolesController(UserManager<IdentityUser> userManager, RoleManager<ApplicationRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // POST: api/UserRoles/Assign
        [HttpPost("Assign")]
        public async Task<IActionResult> AssignRolesToUser(AssignRolesDto assignDto)
        {
            var user = await _userManager.FindByIdAsync(assignDto.UserId);
            if (user == null)
            {
                return NotFound("用户不存在。");
            }

            var roles = await _roleManager.Roles
                .Where(r => assignDto.RoleNames.Contains(r.Name))
                .Select(r => r.Name)
                .ToListAsync();

            var result = await _userManager.AddToRolesAsync(user, roles);
            if (result.Succeeded)
            {
                return Ok("角色分配成功。");
            }
            else
            {
                return BadRequest(result.Errors);
            }
        }

        // POST: api/UserRoles/Remove
        [HttpPost("Remove")]
        public async Task<IActionResult> RemoveRolesFromUser(RemoveRolesDto removeDto)
        {
            var user = await _userManager.FindByIdAsync(removeDto.UserId);
            if (user == null)
            {
                return NotFound("用户不存在。");
            }

            var roles = await _roleManager.Roles
                .Where(r => removeDto.RoleNames.Contains(r.Name))
                .Select(r => r.Name)
                .ToListAsync();

            var result = await _userManager.RemoveFromRolesAsync(user, roles);
            if (result.Succeeded)
            {
                return Ok("角色移除成功。");
            }
            else
            {
                return BadRequest(result.Errors);
            }
        }

        // GET: api/UserRoles/{userId}
        [HttpGet("{userId}")]
        public async Task<ActionResult<UserRolesDto>> GetUserRoles(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound("用户不存在。");
            }

            var roles = await _userManager.GetRolesAsync(user);

            return Ok(new UserRolesDto
            {
                UserId = user.Id,
                UserName = user.UserName,
                Roles = roles.ToList()
            });
        }
    }
}
