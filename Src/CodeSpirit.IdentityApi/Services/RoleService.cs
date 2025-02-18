using AutoMapper;
using CodeSpirit.Core;
using CodeSpirit.Core.IdGenerator;
using CodeSpirit.IdentityApi.Controllers.Dtos.Role;
using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.Shared.Repositories;
using CodeSpirit.Shared.Services;
using LinqKit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Data;

namespace CodeSpirit.IdentityApi.Services
{
    public class RoleService : BaseService<ApplicationRole, RoleDto, long, RoleCreateDto, RoleUpdateDto, RoleBatchImportItemDto>, IRoleService
    {
        private readonly IRepository<ApplicationRole> _roleRepository;
        private readonly IDistributedCache _cache;
        private readonly ILogger<RoleService> _logger;
        private readonly IIdGenerator idGenerator;

        public RoleService(
            IRepository<ApplicationRole> roleRepository,
            IMapper mapper,
            IDistributedCache cache,
            ILogger<RoleService> logger,
            IIdGenerator idGenerator)
            : base(roleRepository, mapper)
        {
            _roleRepository = roleRepository;
            _cache = cache;
            _logger = logger;
            this.idGenerator = idGenerator;
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

        #region Override Base Methods

        public override async Task<RoleDto> CreateAsync(RoleCreateDto createDto)
        {
            if (await _roleRepository.ExistsAsync(r => r.Name == createDto.Name))
            {
                throw new AppServiceException(400, "角色名称已存在！");
            }

            ApplicationRole role = Mapper.Map<ApplicationRole>(createDto);
            
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
            }

            await base.OnUpdating(entity, updateDto);
        }

        protected override async Task<IEnumerable<RoleBatchImportItemDto>> ValidateImportItems(IEnumerable<RoleBatchImportItemDto> importData)
        {
            // 去重处理：确保每个角色名唯一（在导入时去重）
            List<RoleBatchImportItemDto> distinctDtos = importData
                .GroupBy(dto => dto.Name)
                .Select(group => group.First())
                .ToList();

            // 检查数据库中是否已有重复的角色名
            List<string> roleNames = distinctDtos.Select(dto => dto.Name).ToList();
            List<ApplicationRole> existingRoles = await _roleRepository.CreateQuery()
                .Where(role => roleNames.Contains(role.Name))
                .ToListAsync();

            List<RoleBatchImportItemDto> duplicateRoles = distinctDtos
                .Where(dto => existingRoles.Any(role =>
                    role.Name.Equals(dto.Name, StringComparison.OrdinalIgnoreCase)))
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


        #endregion
    }
}
