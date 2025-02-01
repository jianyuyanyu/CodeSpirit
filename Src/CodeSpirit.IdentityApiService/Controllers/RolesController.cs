// Controllers/RolesController.cs
using CodeSpirit.Amis.Attributes;
using CodeSpirit.Core;
using CodeSpirit.IdentityApi.Controllers.Dtos;
using CodeSpirit.IdentityApi.Data;
using CodeSpirit.IdentityApi.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.ComponentModel;

namespace CodeSpirit.IdentityApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize(Roles = "Administrator")] // 仅管理员可以管理角色
    [DisplayName("角色管理")]
    [Page(Label = "角色管理", ParentLabel = "用户中心", Icon = "fa-solid fa-user-group")]
    public class RolesController : ApiControllerBase
    {
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly IDistributedCache _cache;

        public RolesController(RoleManager<ApplicationRole> roleManager, ApplicationDbContext context, IDistributedCache cache)
        {
            _roleManager = roleManager;
            _context = context;
            _cache = cache;
        }

        // GET: api/Roles
        [HttpGet]
        public async Task<ActionResult<ApiResponse<ListData<RoleDto>>>> GetRoles()
        {
            var roles = await _roleManager.Roles
                .Include(r => r.RolePermissions)
                .ThenInclude(p => p.Permission.Children)
                .ToListAsync();

            var roleDtos = roles.Select(role => new RoleDto
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                Permissions = role.RolePermissions.Select(p => new PermissionDto
                {
                    Id = p.Permission.Id,
                    Name = p.Permission.Name,
                    Description = p.Permission.Description,
                    IsAllowed = p.Permission.IsAllowed,
                    ParentId = p.Permission.ParentId,
                    Children = p.Permission.Children?.Select(c => new PermissionDto
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Description = c.Description,
                        IsAllowed = p.Permission.IsAllowed,
                        ParentId = c.ParentId,
                        Children = null // 可以根据需要递归更多层级
                    }).ToList()
                }).ToList()
            });

            return Ok(new
            {
                status = 0,
                msg = "查询成功！",
                data = new
                {
                    items = roleDtos,
                    total = roleDtos.Count()
                }
            });
        }

        // GET: api/Roles/5
        [HttpGet("{id}")]
        public async Task<ActionResult<RoleDto>> GetRole(string id)
        {
            var role = await _roleManager.Roles
                .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .ThenInclude(p => p.Children)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (role == null)
            {
                return NotFound();
            }

            var roleDto = new RoleDto
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                Permissions = role.RolePermissions.Select(p => new PermissionDto
                {
                    Id = p.Permission.Id,
                    Name = p.Permission.Name,
                    Description = p.Permission.Description,
                    IsAllowed = p.Permission.IsAllowed,
                    ParentId = p.Permission.ParentId,
                    Children = p.Permission.Children?.Select(c => new PermissionDto
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Description = c.Description,
                        IsAllowed = p.IsAllowed,
                        ParentId = c.ParentId,
                        Children = null // 可以根据需要递归更多层级
                    }).ToList()
                }).ToList()
            };

            return roleDto;
        }

        // POST: api/Roles
        [HttpPost("")]
        //[Authorize(Policy = "edit_roles")]
        public async Task<ActionResult<RoleDto>> Create(RoleCreateDto roleDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // 检查角色名称是否已存在
            if (await _roleManager.RoleExistsAsync(roleDto.Name))
            {
                return BadRequest("角色名称已存在。");
            }

            var role = new ApplicationRole
            {
                Name = roleDto.Name,
                Description = roleDto.Description,
                RolePermissions = new List<RolePermission>()
            };

            var result = await _roleManager.CreateAsync(role);
            if (result.Succeeded)
            {
                // 分配权限（如果有）
                if (roleDto.PermissionAssignments != null && roleDto.PermissionAssignments.Any())
                {
                    var permissions = await _context.Permissions
                        .Where(p => roleDto.PermissionAssignments.Contains(p.Id))
                        .ToListAsync();

                    foreach (var permission in permissions)
                    {
                        role.RolePermissions.Add(new RolePermission() { IsAllowed = true, Permission = permission, Role = role });
                    }

                    await _roleManager.UpdateAsync(role);
                }
                // 清理所有拥有该角色的用户的权限缓存
                await ClearUserPermissionsCacheByRoleAsync(role.Name);

                // 获取角色信息
                var createdRole = await _roleManager.Roles
                    .Include(r => r.RolePermissions)
                    .ThenInclude(r => r.Permission)
                    .ThenInclude(p => p.Children)
                    .FirstOrDefaultAsync(r => r.Id == role.Id);

                var createdRoleDto = new RoleDto
                {
                    Id = createdRole.Id,
                    Name = createdRole.Name,
                    Description = createdRole.Description,
                    Permissions = createdRole.RolePermissions.Select(p => new PermissionDto
                    {
                        Id = p.Permission.Id,
                        Name = p.Permission.Name,
                        Description = p.Permission.Description,
                        IsAllowed = p.Permission.IsAllowed,
                        ParentId = p.Permission.ParentId,
                        Children = p.Permission.Children?.Select(c => new PermissionDto
                        {
                            Id = c.Id,
                            Name = c.Name,
                            Description = c.Description,
                            IsAllowed = p.IsAllowed,
                            ParentId = c.ParentId,
                            Children = null // 可以根据需要递归更多层级
                        }).ToList()
                    }).ToList()
                };

                return CreatedAtAction(nameof(GetRole), new { id = role.Id }, createdRoleDto);
            }
            else
            {
                return BadRequest(result.Errors);
            }
        }

        // PUT: api/Roles/5
        [HttpPut("{id}")]
        [Authorize(Policy = "edit_roles")]
        public async Task<IActionResult> Update(string id, RoleUpdateDto roleDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var role = await _roleManager.Roles
                .Include(r => r.RolePermissions)
                    .ThenInclude(r => r.Permission)
                .ThenInclude(p => p.Children)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (role == null)
            {
                return NotFound();
            }

            // 检查是否有重名的角色
            if (role.Name != roleDto.Name && await _roleManager.RoleExistsAsync(roleDto.Name))
            {
                return BadRequest("角色名称已存在。");
            }

            role.Name = roleDto.Name;
            role.Description = roleDto.Description;

            // 更新权限
            if (roleDto.PermissionIds != null)
            {
                // 获取新的权限列表
                var newPermissions = await _context.Permissions
                    .Where(p => roleDto.PermissionIds.Contains(p.Id))
                    .ToListAsync();

                // 移除所有现有权限
                role.RolePermissions.Clear();

                // 添加新的权限
                foreach (var permission in newPermissions)
                {
                    role.RolePermissions.Add(new RolePermission() { IsAllowed = true, Permission = permission, Role = role });
                }
            }

            var result = await _roleManager.UpdateAsync(role);
            if (result.Succeeded)
            {
                // 清理所有拥有该角色的用户的权限缓存
                await ClearUserPermissionsCacheByRoleAsync(role.Name);

                return NoContent();
            }
            else
            {
                return BadRequest(result.Errors);
            }
        }

        // DELETE: api/Roles/5
        [HttpDelete("{id}")]
        [Authorize(Policy = "delete_roles")]
        public async Task<IActionResult> Delete(string id)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
            {
                return NotFound();
            }

            var result = await _roleManager.DeleteAsync(role);
            if (result.Succeeded)
            {
                // 清理所有拥有该角色的用户的权限缓存
                await ClearUserPermissionsCacheByRoleAsync(role.Name);

                return NoContent();
            }
            else
            {
                return BadRequest(result.Errors);
            }
        }

        // 辅助方法：按角色清理拥有该角色的用户的权限缓存
        private async Task ClearUserPermissionsCacheByRoleAsync(string roleName)
        {
            // 获取角色 ID
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
            if (role == null)
                return;

            var roleId = role.Id;

            // 获取拥有该角色的用户 ID
            var userIds = await _context.UserRoles
                .Where(ur => ur.RoleId == roleId)
                .Select(ur => ur.UserId)
                .Distinct()
                .ToListAsync();

            foreach (var userId in userIds)
            {
                var cacheKey = $"UserPermissions_{userId}";
                await _cache.RemoveAsync(cacheKey);
            }
        }

        // POST: api/Roles/5/Permissions
        [HttpPost("{id}/Permissions")]
        public async Task<IActionResult> AssignPermissionsToRole(string id, AssignPermissionsDto assignDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var role = await _roleManager.Roles
                .Include(r => r.RolePermissions)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (role == null)
            {
                return NotFound("角色不存在。");
            }

            var permissions = await _context.Permissions
                .Where(p => assignDto.PermissionIds.Contains(p.Id))
                .ToListAsync();

            foreach (var permission in permissions)
            {
                if (!role.RolePermissions.Any(p => p.PermissionId == permission.Id))
                {
                    role.RolePermissions.Add(new RolePermission() { IsAllowed = true, Permission = permission, Role = role });
                }
            }

            var result = await _roleManager.UpdateAsync(role);
            if (result.Succeeded)
            {
                return NoContent();
            }
            else
            {
                return BadRequest(result.Errors);
            }
        }

        // DELETE: api/Roles/5/Permissions
        [HttpDelete("{id}/Permissions")]
        public async Task<IActionResult> RemovePermissionsFromRole(string id, RemovePermissionsDto removeDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var role = await _roleManager.Roles
                .Include(r => r.RolePermissions)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (role == null)
            {
                return NotFound("角色不存在。");
            }

            var permissions = await _context.Permissions
                .Where(p => removeDto.PermissionIds.Contains(p.Id))
                .ToListAsync();

            foreach (var permission in permissions)
            {
                var rolePermission = role.RolePermissions.FirstOrDefault(p => p.PermissionId == permission.Id);
                if (role.RolePermissions.Any(p => p.PermissionId == permission.Id))
                {
                    role.RolePermissions.Remove(rolePermission);
                }
            }

            var result = await _roleManager.UpdateAsync(role);
            if (result.Succeeded)
            {
                return NoContent();
            }
            else
            {
                return BadRequest(result.Errors);
            }
        }
    }
}
