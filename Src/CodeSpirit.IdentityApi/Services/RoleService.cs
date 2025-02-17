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
                predicate
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

        protected override async Task ValidateCreateDto(RoleCreateDto createDto)
        {
            if (await _roleRepository.ExistsAsync(r => r.Name == createDto.Name))
            {
                throw new AppServiceException(400, "角色名称已存在！");
            }
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

        protected override async Task<ApplicationRole> GetEntityForUpdate(RoleUpdateDto updateDto)
        {
            ApplicationRole entity = await _roleRepository.GetByIdAsync(updateDto.Id);
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
