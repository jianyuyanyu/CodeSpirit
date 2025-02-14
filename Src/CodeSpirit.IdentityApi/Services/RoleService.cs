using AutoMapper;
using CodeSpirit.Core;
using CodeSpirit.IdentityApi.Controllers.Dtos.Role;
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
        private readonly ILogger<RoleService> _logger;

        public RoleService(IRoleRepository roleRepository, IMapper mapper, IDistributedCache cache, ILogger<RoleService> logger)
        {
            _roleRepository = roleRepository;
            _mapper = mapper;
            _cache = cache;
            _logger = logger;
        }

        public async Task<(List<RoleDto> roles, int total)> GetRolesAsync(RoleQueryDto queryDto)
        {
            (List<ApplicationRole> roles, int total) = await _roleRepository.GetRolesAsync(queryDto);
            return (_mapper.Map<List<RoleDto>>(roles), total);
        }

        public async Task<RoleDto> GetRoleByIdAsync(long id)
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

        public async Task UpdateRoleAsync(long id, RoleUpdateDto updateDto)
        {
            ApplicationRole role = await _roleRepository.GetRoleByIdAsync(id);
            _mapper.Map(updateDto, role);
            await _roleRepository.UpdateRoleAsync(role, updateDto.PermissionIds);
            // 清理所有拥有该角色的用户的权限缓存
            await ClearUserPermissionsCacheByRoleAsync(role.Id);
        }

        public async Task DeleteRoleAsync(long id)
        {
            ApplicationRole role = await _roleRepository.GetRoleByIdAsync(id);
            
            // 检查是否为 Admin 角色
            if (role.Name.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                throw new AppServiceException(400, "Admin角色不允许删除！");
            }

            if (role.RolePermission != null && role.RolePermission.PermissionIds != null)
            {
                throw new AppServiceException(400, "请移除权限后再删除该角色！");
            }
            
            await _roleRepository.DeleteRoleAsync(role);
            // 清理所有拥有该角色的用户的权限缓存
            await ClearUserPermissionsCacheByRoleAsync(role.Id);
        }

        private async Task ClearUserPermissionsCacheByRoleAsync(long id)
        {
            List<long> userIds = await _roleRepository.GetUserIdsByRoleId(id);

            IEnumerable<Task> cacheTasks = userIds.Select(userId =>
                _cache.RemoveAsync($"UserPermissions_{userId}"));

            await Task.WhenAll(cacheTasks);
        }

        /// <summary>
        /// 批量导入角色（高性能）
        /// </summary>
        /// <param name="importDtos">批量导入 DTO 列表</param>
        public async Task BatchImportRolesAsync(List<RoleBatchImportItemDto> importDtos)
        {
            // 数据不能为空
            if (importDtos == null || !importDtos.Any())
            {
                throw new AppServiceException(400, "导入数据不能为空！");
            }

            // 校验导入数据格式是否合法
            List<RoleBatchImportItemDto> invalidDtos = importDtos.Where(dto => string.IsNullOrEmpty(dto.Name) || dto.Name.Length > 100).ToList();
            if (invalidDtos.Any())
            {
                throw new AppServiceException(400, $"以下角色数据格式错误: {string.Join(", ", invalidDtos.Select(dto => dto.Name))}！");
            }

            // 去重处理：确保每个角色名唯一（在导入时去重）
            List<RoleBatchImportItemDto> distinctDtos = importDtos
                .GroupBy(dto => dto.Name)
                .Select(group => group.First())
                .ToList();

            // 检查数据库中是否已有重复的角色名
            List<ApplicationRole> existingRoles = await _roleRepository.GetRolesByNamesAsync(distinctDtos.Select(dto => dto.Name).ToList());

            List<RoleBatchImportItemDto> duplicateRoles = distinctDtos.Where(dto => existingRoles.Any(role => role.Name.Equals(dto.Name, StringComparison.OrdinalIgnoreCase))).ToList();
            if (duplicateRoles.Any())
            {
                throw new AppServiceException(400, $"以下角色名已存在: {string.Join(", ", duplicateRoles.Select(dto => dto.Name))}！");
            }

            // 将 DTO 转换为实体集合
            List<ApplicationRole> roles = [];
            foreach (RoleBatchImportItemDto dto in distinctDtos)
            {
                // 利用 AutoMapper 将 DTO 映射为 ApplicationRole 实体
                ApplicationRole role = _mapper.Map<ApplicationRole>(dto);
                roles.Add(role);
            }

            if (!roles.Any())
            {
                throw new AppServiceException(400, "没有有效的角色数据可以导入！");
            }

            // 批量插入角色（利用 EF Core 的 AddRange 提高性能）
            try
            {
                // 使用批量插入 API，避免单条插入过多导致性能问题
                await _roleRepository.BulkInsertRolesAsync(roles);
                _logger.LogInformation($"成功批量导入了 {roles.Count} 个角色！");
            }
            catch (Exception ex)
            {
                // 记录批量插入异常日志
                _logger.LogError($"批量插入角色数据失败: {ex.Message}");
                throw new AppServiceException(500, "批量导入角色时发生错误，请稍后重试！");
            }
        }
    }

}
