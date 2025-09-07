using CodeSpirit.Core;
using CodeSpirit.Core.Attributes;
using CodeSpirit.IdentityApi.Dtos.Tenant;
using CodeSpirit.IdentityApi.Services;
using CodeSpirit.Shared.Data;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace CodeSpirit.IdentityApi.Controllers.Internal
{
    /// <summary>
    /// 内部租户信息访问控制器
    /// 提供租户存储所需的内部API接口
    /// </summary>
    [DisplayName("内部租户信息")]
    [Module("default")]
    [Route("api/identity/internal/tenants")]
    public class InternalTenantsController : ControllerBase
    {
        private readonly ITenantService _tenantService;
        private readonly IDataFilter _dataFilter;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="tenantService">租户服务</param>
        /// <param name="dataFilter">数据筛选器</param>
        public InternalTenantsController(ITenantService tenantService, IDataFilter dataFilter)
        {
            _tenantService = tenantService;
            _dataFilter = dataFilter;
        }

        /// <summary>
        /// 根据租户ID获取租户信息
        /// </summary>
        /// <param name="tenantId">租户ID</param>
        /// <returns>租户信息</returns>
        [HttpGet("{tenantId}")]
        [DisplayName("获取内部租户信息")]
        public async Task<ActionResult<ApiResponse<InternalTenantDto>>> GetInternalTenant(string tenantId)
        {
            // 禁用多租户筛选器来查询指定租户
            using (_dataFilter.Disable<IMultiTenant>())
            {
                var tenant = await _tenantService.GetByTenantIdAsync(tenantId);
                if (tenant == null)
                {
                    return NotFound(ApiResponse<InternalTenantDto>.Error(404, "租户不存在"));
                }

                var dto = new InternalTenantDto
                {
                    TenantId = tenant.TenantId,
                    Name = tenant.Name,
                    DisplayName = tenant.DisplayName,
                    Description = tenant.Description,
                    Strategy = tenant.Strategy,
                    IsActive = tenant.IsActive,
                    Domain = tenant.Domain,
                    LogoUrl = tenant.LogoUrl,
                    MaxUsers = tenant.MaxUsers,
                    StorageLimit = tenant.StorageLimit,
                    ExpiresAt = tenant.ExpiresAt,
                    CreatedAt = tenant.CreatedAt,
                    ThemeConfig = tenant.ThemeConfig,
                    Configuration = tenant.Configuration
                };

                return Ok(ApiResponse<InternalTenantDto>.Success(dto));
            }
        }

        /// <summary>
        /// 检查租户是否存在
        /// </summary>
        /// <param name="tenantId">租户ID</param>
        /// <returns>是否存在</returns>
        [HttpHead("{tenantId}")]
        [DisplayName("检查租户是否存在")]
        public async Task<ActionResult> CheckTenantExists(string tenantId)
        {
            // 禁用多租户筛选器来检查租户
            using (_dataFilter.Disable<IMultiTenant>())
            {
                var tenant = await _tenantService.GetByTenantIdAsync(tenantId);
                if (tenant == null)
                {
                    return NotFound();
                }

                return Ok();
            }
        }

        /// <summary>
        /// 获取活跃租户列表
        /// </summary>
        /// <returns>活跃租户列表</returns>
        [HttpGet("active")]
        [DisplayName("获取活跃租户列表")]
        public async Task<ActionResult<ApiResponse<IEnumerable<InternalTenantDto>>>> GetActiveTenants()
        {
            // 禁用多租户筛选器来获取所有活跃租户
            using (_dataFilter.Disable<IMultiTenant>())
            {
                // 使用分页查询获取活跃租户
                var queryDto = new TenantQueryDto { IsActive = true, Page = 1, PerPage = 1000 };
                var result = await _tenantService.GetPagedListAsync(queryDto);

                var dtos = new List<InternalTenantDto>();
                foreach (var t in result.Items)
                {
                    // 获取完整的租户实体信息
                    var tenantEntity = await _tenantService.GetByTenantIdAsync(t.TenantId);
                    if (tenantEntity != null)
                    {
                        dtos.Add(new InternalTenantDto
                        {
                            TenantId = t.TenantId,
                            Name = t.Name,
                            DisplayName = t.DisplayName,
                            Description = t.Description,
                            Strategy = t.Strategy,
                            IsActive = t.IsActive,
                            Domain = t.Domain,
                            LogoUrl = t.LogoUrl,
                            MaxUsers = t.MaxUsers,
                            StorageLimit = t.StorageLimit,
                            ExpiresAt = t.ExpiresAt,
                            CreatedAt = t.CreatedAt,
                            ThemeConfig = tenantEntity.ThemeConfig,
                            Configuration = tenantEntity.Configuration
                        });
                    }
                }

                return Ok(ApiResponse<IEnumerable<InternalTenantDto>>.Success(dtos));
            }
        }

        /// <summary>
        /// 创建租户
        /// </summary>
        /// <param name="createDto">创建数据</param>
        /// <returns>创建结果</returns>
        [HttpPost]
        [DisplayName("创建内部租户")]
        public async Task<ActionResult<ApiResponse<InternalTenantDto>>> CreateInternalTenant([FromBody] TenantCreateDto createDto)
        {
            // 检查是否尝试创建系统租户
            if (string.Equals(createDto.TenantId, "system", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(ApiResponse<InternalTenantDto>.Error(400, "不能创建租户ID为'system'的租户，该ID为系统保留"));
            }

            // 禁用多租户筛选器来创建租户
            using (_dataFilter.Disable<IMultiTenant>())
            {
                var result = await _tenantService.CreateAsync(createDto);
                
                // 获取完整的租户实体信息
                var tenantEntity = await _tenantService.GetByTenantIdAsync(result.TenantId);
                
                var dto = new InternalTenantDto
                {
                    TenantId = result.TenantId,
                    Name = result.Name,
                    DisplayName = result.DisplayName,
                    Description = result.Description,
                    Strategy = result.Strategy,
                    IsActive = result.IsActive,
                    Domain = result.Domain,
                    LogoUrl = result.LogoUrl,
                    MaxUsers = result.MaxUsers,
                    StorageLimit = result.StorageLimit,
                    ExpiresAt = result.ExpiresAt,
                    CreatedAt = result.CreatedAt,
                    ThemeConfig = tenantEntity?.ThemeConfig,
                    Configuration = tenantEntity?.Configuration
                };

                return Ok(ApiResponse<InternalTenantDto>.Success(dto, "创建成功"));
            }
        }

        /// <summary>
        /// 更新租户
        /// </summary>
        /// <param name="tenantId">租户ID</param>
        /// <param name="updateDto">更新数据</param>
        /// <returns>更新结果</returns>
        [HttpPut("{tenantId}")]
        [DisplayName("更新内部租户")]
        public async Task<ActionResult<ApiResponse<InternalTenantDto>>> UpdateInternalTenant(string tenantId, [FromBody] TenantUpdateDto updateDto)
        {
            // 检查是否为系统租户且尝试禁用
            if (string.Equals(tenantId, "system", StringComparison.OrdinalIgnoreCase) && !updateDto.IsActive)
            {
                return BadRequest(ApiResponse<InternalTenantDto>.Error(400, "系统租户不能被禁用"));
            }

            // 禁用多租户筛选器来更新租户
            using (_dataFilter.Disable<IMultiTenant>())
            {
                await _tenantService.UpdateAsync(tenantId, updateDto);
                
                // 重新获取更新后的租户信息
                var updatedTenant = await _tenantService.GetByTenantIdAsync(tenantId);
                
                var dto = new InternalTenantDto
                {
                    TenantId = updatedTenant.TenantId,
                    Name = updatedTenant.Name,
                    DisplayName = updatedTenant.DisplayName,
                    Description = updatedTenant.Description,
                    Strategy = updatedTenant.Strategy,
                    IsActive = updatedTenant.IsActive,
                    Domain = updatedTenant.Domain,
                    LogoUrl = updatedTenant.LogoUrl,
                    MaxUsers = updatedTenant.MaxUsers,
                    StorageLimit = updatedTenant.StorageLimit,
                    ExpiresAt = updatedTenant.ExpiresAt,
                    CreatedAt = updatedTenant.CreatedAt,
                    ThemeConfig = updatedTenant.ThemeConfig,
                    Configuration = updatedTenant.Configuration
                };

                return Ok(ApiResponse<InternalTenantDto>.Success(dto, "更新成功"));
            }
        }

        /// <summary>
        /// 删除租户
        /// </summary>
        /// <param name="tenantId">租户ID</param>
        /// <returns>删除结果</returns>
        [HttpDelete("{tenantId}")]
        [DisplayName("删除内部租户")]
        public async Task<ActionResult<ApiResponse>> DeleteInternalTenant(string tenantId)
        {
            // 检查是否为系统租户
            if (string.Equals(tenantId, "system", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(ApiResponse.Error(400, "系统租户不能被删除"));
            }

            // 禁用多租户筛选器来删除租户
            using (_dataFilter.Disable<IMultiTenant>())
            {
                await _tenantService.DeleteAsync(tenantId);
                return Ok(ApiResponse.Success("删除成功"));
            }
        }
    }
}
