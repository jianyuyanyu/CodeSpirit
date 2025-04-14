using AutoMapper;
using CodeSpirit.Core;
using CodeSpirit.Core.IdGenerator;
using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.IdentityApi.Dtos.Role;
using CodeSpirit.Shared.Repositories;
using CodeSpirit.Shared.Services;
using LinqKit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Data;
using System.Text.Json;
using CodeSpirit.Authorization.Extensions;

namespace CodeSpirit.IdentityApi.Services
{
    public class RoleService : BaseCRUDIService<ApplicationRole, RoleDto, long, RoleCreateDto, RoleUpdateDto, RoleBatchImportItemDto>, IRoleService
    {
        private readonly IRepository<ApplicationRole> _roleRepository;
        private readonly IDistributedCache _cache;
        private readonly ILogger<RoleService> _logger;
        private readonly IIdGenerator idGenerator;
        private readonly IRepository<ApplicationUser> _userRepository;

        public RoleService(
            IRepository<ApplicationRole> roleRepository,
            IMapper mapper,
            IDistributedCache cache,
            ILogger<RoleService> logger,
            IIdGenerator idGenerator,
            IRepository<ApplicationUser> userRepository)
            : base(roleRepository, mapper)
        {
            _roleRepository = roleRepository;
            _cache = cache;
            _logger = logger;
            this.idGenerator = idGenerator;
            _userRepository = userRepository;
        }

        public async Task<PageList<RoleDto>> GetRolesAsync(RoleQueryDto queryDto)
        {
            ExpressionStarter<ApplicationRole> predicate = PredicateBuilder.New<ApplicationRole>(true);

            if (!string.IsNullOrEmpty(queryDto.Keywords))
            {
                predicate = predicate.Or(x => x.Name.Contains(queryDto.Keywords));
                predicate = predicate.Or(x => x.Description.Contains(queryDto.Keywords));
            }

            return await GetPagedListAsync(
                queryDto,
                predicate,
                "RolePermission"
            );
        }

        public async Task<(int successCount, List<string> failedIds)> BatchImportRolesAsync(List<RoleBatchImportItemDto> importDtos)
        {
            // 去重处理
            List<RoleBatchImportItemDto> distinctImportDtos = importDtos
                .GroupBy(x => x.Name.ToLower())
                .Select(g => g.First())
                .ToList();

            (int successCount, List<string> failedIds) result = await BatchImportAsync(distinctImportDtos);
            return result;
        }

        /// <summary>
        /// 获取用户权限列表
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>用户权限列表</returns>
        public async Task<HashSet<string>> GetUserPermissionsAsync(long userId)
        {
            // 定义缓存键
            string cacheKey = $"UserPermissions:{userId}";
            
            // 尝试从缓存中获取
            var cachedPermissions = await _cache.GetAsync<HashSet<string>>(cacheKey);
            if (cachedPermissions != null)
            {
                _logger.LogDebug("从缓存中获取用户权限，用户ID: {UserId}", userId);
                return cachedPermissions;
            }
            
            // 缓存未命中，从数据库获取
            var user = await _userRepository.CreateQuery()
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                        .ThenInclude(r => r.RolePermission)
                .FirstOrDefaultAsync(u => u.Id == userId);
                
            if (user == null)
            {
                _logger.LogWarning("获取权限失败，用户不存在，用户ID: {UserId}", userId);
                return new HashSet<string>();
            }
            
            // 收集用户所有角色的所有权限
            var permissions = new HashSet<string>();
            foreach (var userRole in user.UserRoles)
            {
                if (userRole.Role?.RolePermission?.PermissionIds != null)
                {
                    foreach (var permission in userRole.Role.RolePermission.PermissionIds)
                    {
                        permissions.Add(permission);
                    }
                }
            }
            
            // 将权限存入缓存
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12),
                SlidingExpiration = TimeSpan.FromMinutes(30)
            };
            
            await _cache.SetAsync(cacheKey, permissions, cacheOptions);
            _logger.LogDebug("已缓存用户权限，用户ID: {UserId}, 权限数量: {PermissionCount}", userId, permissions.Count);
            
            return permissions;
        }

        #region Override Base Methods

        public override async Task<RoleDto> CreateAsync(RoleCreateDto createDto)
        {
            if (await _roleRepository.ExistsAsync(r => r.Name == createDto.Name))
            {
                throw new AppServiceException(400, "角色名称已存在！");
            }

            ApplicationRole role = Mapper.Map<ApplicationRole>(createDto);
            role.NormalizedName = role.Name.ToUpperInvariant();

            // Generate a new ID for the role
            role.Id = idGenerator.NewId();
            
            if (createDto.PermissionAssignments != null && createDto.PermissionAssignments.Any())
            {
                role.RolePermission = new RolePermission
                {
                    RoleId = role.Id,
                    PermissionIds = createDto.PermissionAssignments.Distinct().ToArray()
                };
            }

            ApplicationRole createdEntity = await Repository.AddAsync(role);
            return Mapper.Map<RoleDto>(createdEntity);
        }

        protected override async Task OnUpdating(ApplicationRole entity, RoleUpdateDto updateDto)
        {
            // 如果名称发生变化，需要更新 NormalizedName
            if (!string.Equals(entity.Name, updateDto.Name, StringComparison.Ordinal))
            {
                entity.NormalizedName = updateDto.Name.ToUpperInvariant();
            }

            if (updateDto.PermissionIds != null)
            {
                string[] distinctPermissionIds = updateDto.PermissionIds.Distinct().ToArray();

                if (distinctPermissionIds.Any())
                {
                    // Load the existing RolePermission if not already loaded
                    if (entity.RolePermission == null)
                    {
                        entity.RolePermission = await Repository.CreateQuery()
                            .Where(r => r.Id == entity.Id)
                            .Select(r => r.RolePermission)
                            .FirstOrDefaultAsync();
                    }

                    if (entity.RolePermission == null)
                    {
                        entity.RolePermission = new RolePermission
                        {
                            RoleId = entity.Id,
                            PermissionIds = distinctPermissionIds
                        };
                    }
                    else
                    {
                        entity.RolePermission.PermissionIds = distinctPermissionIds;
                    }
                }
                else
                {
                    // If no permission IDs are provided, remove the role permission
                    if (entity.RolePermission != null)
                    {
                        entity.RolePermission.PermissionIds = Array.Empty<string>();
                    }
                }
                
                // 角色权限变更，需要清除相关用户的权限缓存
                await InvalidateUserPermissionCachesAsync(entity.Id);
            }

            await base.OnUpdating(entity, updateDto);
        }

        /// <summary>
        /// 当角色权限变更时，清除所有关联用户的权限缓存
        /// </summary>
        private async Task InvalidateUserPermissionCachesAsync(long roleId)
        {
            try
            {
                // 查找拥有此角色的所有用户
                var userIds = await _userRepository.CreateQuery()
                    .Where(u => u.UserRoles.Any(ur => ur.RoleId == roleId))
                    .Select(u => u.Id)
                    .ToListAsync();

                foreach (var userId in userIds)
                {
                    string cacheKey = $"UserPermissions:{userId}";
                    await _cache.RemoveAsync(cacheKey);
                    _logger.LogDebug("已清除用户权限缓存，用户ID: {UserId}, 角色ID: {RoleId}", userId, roleId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清除用户权限缓存失败，角色ID: {RoleId}", roleId);
            }
        }

        protected override async Task<IEnumerable<RoleBatchImportItemDto>> ValidateImportItems(IEnumerable<RoleBatchImportItemDto> importData)
        {
            // 去重处理：确保每个角色名唯一（在导入时去重）
            List<RoleBatchImportItemDto> distinctDtos = importData
                .GroupBy(dto => NormalizeRoleName(dto.Name))  // 使用标准化名称进行分组
                .Select(group => group.First())
                .ToList();

            // 检查数据库中是否已有重复的角色名
            List<string> normalizedRoleNames = distinctDtos.Select(dto => NormalizeRoleName(dto.Name)).ToList();
            List<ApplicationRole> existingRoles = await _roleRepository.CreateQuery()
                .Where(role => normalizedRoleNames.Contains(role.NormalizedName))  // 使用 NormalizedName 进行查询
                .ToListAsync();

            List<RoleBatchImportItemDto> duplicateRoles = distinctDtos
                .Where(dto => existingRoles.Any(role =>
                    role.NormalizedName == NormalizeRoleName(dto.Name)))
                .ToList();

            return duplicateRoles.Any()
                ? throw new AppServiceException(400, $"以下角色名已存在: {string.Join(", ", duplicateRoles.Select(dto => dto.Name))}！")
                : distinctDtos;
        }

        protected override async Task<ApplicationRole> GetEntityForUpdate(long id, RoleUpdateDto updateDto)
        {
            ApplicationRole entity = await _roleRepository.GetByIdAsync(id);
            return entity == null ? throw new AppServiceException(404, "角色不存在！") : entity;
        }

        protected override string GetImportItemId(RoleBatchImportItemDto importDto)
        {
            return importDto.Name;
        }

        protected override Task OnDeleting(ApplicationRole entity)
        {
            return entity.Name.Equals("Admin", StringComparison.OrdinalIgnoreCase)
                ? throw new AppServiceException(400, "Admin角色不允许删除！")
                : entity.RolePermission?.PermissionIds != null ? throw new AppServiceException(400, "请移除权限后再删除该角色！") : Task.CompletedTask;
        }

        protected override Task OnImportMapping(ApplicationRole entity, RoleBatchImportItemDto importDto)
        {
            base.OnImportMapping(entity, importDto);
            
            // 确保设置 NormalizedName
            entity.NormalizedName = entity.Name.ToUpperInvariant();
            return Task.CompletedTask;
        }

        // 建议添加一个帮助方法来统一处理角色名称的标准化
        private string NormalizeRoleName(string roleName)
        {
            return roleName?.ToUpperInvariant();
        }

        #endregion
    }
}
