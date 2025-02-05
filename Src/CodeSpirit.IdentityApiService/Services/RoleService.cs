using AutoMapper;
using CodeSpirit.Core;
using CodeSpirit.IdentityApi.Controllers.Dtos;
using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.IdentityApi.Repositories;
using Microsoft.Extensions.Caching.Distributed;

namespace CodeSpirit.IdentityApi.Services
{
    public class RoleService : IRoleService
    {
        private readonly IRoleRepository _roleRepository;
        private readonly IMapper _mapper;
        private readonly IDistributedCache _cache;

        public RoleService(IRoleRepository roleRepository, IMapper mapper, IDistributedCache cache)
        {
            _roleRepository = roleRepository;
            _mapper = mapper;
            _cache = cache;
        }

        public async Task<(List<RoleDto> roles, int total)> GetRolesAsync(RoleQueryDto queryDto)
        {
            (List<ApplicationRole> roles, int total) = await _roleRepository.GetRolesAsync(queryDto);
            return (_mapper.Map<List<RoleDto>>(roles), total);
        }

        public async Task<RoleDto> GetRoleByIdAsync(string id)
        {
            ApplicationRole role = await _roleRepository.GetRoleByIdAsync(id);
            return _mapper.Map<RoleDto>(role);
        }

        public async Task<RoleDto> CreateRoleAsync(RoleCreateDto createDto)
        {
            ApplicationRole role = _mapper.Map<ApplicationRole>(createDto);
            await _roleRepository.CreateRoleAsync(role, createDto.PermissionAssignments);
            // 清理所有拥有该角色的用户的权限缓存
            await ClearUserPermissionsCacheByRoleAsync(role.Id);
            return _mapper.Map<RoleDto>(role);
        }

        public async Task UpdateRoleAsync(string id, RoleUpdateDto updateDto)
        {
            ApplicationRole role = await _roleRepository.GetRoleByIdAsync(id);
            _mapper.Map(updateDto, role);
            await _roleRepository.UpdateRoleAsync(role, updateDto.PermissionIds);
            // 清理所有拥有该角色的用户的权限缓存
            await ClearUserPermissionsCacheByRoleAsync(role.Id);
        }

        public async Task DeleteRoleAsync(string id)
        {
            ApplicationRole role = await _roleRepository.GetRoleByIdAsync(id);
            if (role.RolePermissions.Any())
            {
                throw new AppServiceException(400, "请移除权限后再删除该角色！");
            }
            await _roleRepository.DeleteRoleAsync(role);
            // 清理所有拥有该角色的用户的权限缓存
            await ClearUserPermissionsCacheByRoleAsync(role.Id);
        }

        private async Task ClearUserPermissionsCacheByRoleAsync(string id)
        {
            List<string> userIds = await _roleRepository.GetUserIdsByRoleId(id);

            IEnumerable<Task> cacheTasks = userIds.Select(userId =>
                _cache.RemoveAsync($"UserPermissions_{userId}"));

            await Task.WhenAll(cacheTasks);
        }

        public async Task RemovePermissionsFromRoleAsync(string id, IEnumerable<int> permissionIds)
        {
            ApplicationRole role = await _roleRepository.GetRoleByIdAsync(id);
            await _roleRepository.RemovePermissionsFromRoleAsync(role, permissionIds);
            await ClearUserPermissionsCacheByRoleAsync(id);
        }
    }

}
