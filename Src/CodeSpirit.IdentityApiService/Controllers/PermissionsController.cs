// Controllers/PermissionsController.cs
using CodeSpirit.IdentityApi.Controllers.Dtos;
using CodeSpirit.IdentityApi.Data;
using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.IdentityApi.Data.Models.RoleManagementApiIdentity.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace RoleManagementApiIdentity.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Administrator")] // 仅管理员可以管理权限
    public class PermissionsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        private readonly IDistributedCache _cache;

        public PermissionsController(ApplicationDbContext context, IDistributedCache cache)
        {
            _context = context;
            _cache = cache;
        }


        // GET: api/Permissions
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PermissionDto>>> GetPermissions()
        {
            var permissions = await _context.Permissions
                .Include(p => p.Children)
                .ToListAsync();

            var permissionDtos = permissions
                .Where(p => p.ParentId == null) // 获取顶级权限
                .Select(p => MapPermissionToDto(p))
                .ToList();

            return Ok(permissionDtos);
        }

        // GET: api/Permissions/5
        [HttpGet("{id}")]
        public async Task<ActionResult<PermissionDto>> GetPermission(int id)
        {
            var permission = await _context.Permissions
                .Include(p => p.Children)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (permission == null)
            {
                return NotFound();
            }

            var permissionDto = MapPermissionToDto(permission);

            return Ok(permissionDto);
        }

        // POST: api/Permissions
        [HttpPost]
        public async Task<ActionResult<PermissionDto>> PostPermission(PermissionCreateDto permissionDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // 检查权限名称是否已存在
            if (await _context.Permissions.AnyAsync(p => p.Name == permissionDto.Name))
            {
                return BadRequest("权限名称已存在。");
            }

            var permission = new Permission
            {
                Name = permissionDto.Name,
                Description = permissionDto.Description,
                ParentId = permissionDto.ParentId,
                IsAllowed = permissionDto.IsAllowed
            };

            _context.Permissions.Add(permission);
            await _context.SaveChangesAsync();

            var createdPermission = await _context.Permissions
                .Include(p => p.Children)
                .FirstOrDefaultAsync(p => p.Id == permission.Id);

            var createdPermissionDto = MapPermissionToDto(createdPermission);

            // 清理所有拥有相关权限的用户的权限缓存
            await ClearUserPermissionsCacheByPermissionAsync(permission.Name);

            return CreatedAtAction(nameof(GetPermission), new { id = permission.Id }, createdPermissionDto);
        }

        // PUT: api/Permissions/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutPermission(int id, PermissionUpdateDto permissionDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var permission = await _context.Permissions.FindAsync(id);
            if (permission == null)
            {
                return NotFound();
            }

            // 检查是否有重名的权限
            if (permission.Name != permissionDto.Name && await _context.Permissions.AnyAsync(p => p.Name == permissionDto.Name))
            {
                return BadRequest("权限名称已存在。");
            }

            permission.Name = permissionDto.Name;
            permission.Description = permissionDto.Description;
            permission.ParentId = permissionDto.ParentId;
            permission.IsAllowed = permissionDto.IsAllowed;

            _context.Entry(permission).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PermissionExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            // 清理所有拥有相关权限的用户的权限缓存
            await ClearUserPermissionsCacheByPermissionAsync(permission.Name);

            return NoContent();
        }

        // DELETE: api/Permissions/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePermission(int id)
        {
            var permission = await _context.Permissions
                .Include(p => p.Children)
                .Include(p => p.RolePermissions)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (permission == null)
            {
                return NotFound();
            }

            // 检查是否有子权限
            if (permission.Children != null && permission.Children.Any())
            {
                return BadRequest("该权限有子权限，无法删除。请先删除或移除子权限。");
            }

            // 移除与角色的关联
            if (permission.RolePermissions != null && permission.RolePermissions.Any())
            {
                foreach (var role in permission.RolePermissions.ToList())
                {
                    permission.RolePermissions.Remove(role);
                }
            }

            _context.Permissions.Remove(permission);
            await _context.SaveChangesAsync();

            // 清理所有拥有相关权限的用户的权限缓存
            await ClearUserPermissionsCacheByPermissionAsync(permission.Name);

            return NoContent();
        }

        // 辅助方法：按权限清理拥有该权限的用户的权限缓存
        private async Task ClearUserPermissionsCacheByPermissionAsync(string permissionName)
        {
            // 获取拥有该权限的角色 ID
            var roleIds = await _context.RolePermissions
                .Where(rp => rp.Permission.Name == permissionName)
                .Select(rp => rp.RoleId)
                .Distinct()
                .ToListAsync();

            if (!roleIds.Any())
                return;

            // 获取拥有这些角色的用户 ID
            var userIds = await _context.UserRoles
                .Where(ur => roleIds.Contains(ur.RoleId))
                .Select(ur => ur.UserId)
                .Distinct()
                .ToListAsync();

            foreach (var userId in userIds)
            {
                var cacheKey = $"UserPermissions_{userId}";
                await _cache.RemoveAsync(cacheKey);
            }
        }

        private bool PermissionExists(int id)
        {
            return _context.Permissions.Any(e => e.Id == id);
        }

        // 辅助方法：映射权限到 DTO，包含子权限
        private PermissionDto MapPermissionToDto(Permission permission)
        {
            return new PermissionDto
            {
                Id = permission.Id,
                Name = permission.Name,
                Description = permission.Description,
                IsAllowed = permission.IsAllowed,
                ParentId = permission.ParentId,
                Children = permission.Children?.Select(c => MapPermissionToDto(c)).ToList()
            };
        }
    }
}
