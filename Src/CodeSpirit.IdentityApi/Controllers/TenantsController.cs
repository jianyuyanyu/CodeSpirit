using CodeSpirit.Core;
using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Enums;
using CodeSpirit.IdentityApi.Dtos.Tenant;
using CodeSpirit.IdentityApi.Services;
using CodeSpirit.Amis.Attributes.FormFields;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace CodeSpirit.IdentityApi.Controllers
{
    /// <summary>
    /// 租户管理控制器
    /// </summary>
    [DisplayName("租户管理")]
    [Navigation(Icon = "fa-solid fa-building", PlatformType = PlatformType.System)]
    public class TenantsController : ApiControllerBase
    {
        private readonly ITenantService _tenantService;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="tenantService">租户服务</param>
        public TenantsController(ITenantService tenantService)
        {
            _tenantService = tenantService;
        }

        /// <summary>
        /// 获取租户列表
        /// </summary>
        /// <param name="queryDto">查询条件</param>
        /// <returns>租户列表</returns>
        [HttpGet]
        [DisplayName("获取租户列表")]
        public async Task<ActionResult<ApiResponse<PageList<TenantDto>>>> GetTenants([FromQuery] TenantQueryDto queryDto)
        {
            var result = await _tenantService.GetPagedListAsync(queryDto);
            return Ok(ApiResponse<PageList<TenantDto>>.Success(result));
        }

        /// <summary>
        /// 根据ID获取租户详情
        /// </summary>
        /// <param name="tenantId">租户ID</param>
        /// <returns>租户详情</returns>
        [HttpGet("{tenantId}")]
        [DisplayName("获取租户详情")]
        public async Task<ActionResult<ApiResponse<TenantDto>>> GetTenant(string tenantId)
        {
            var result = await _tenantService.GetByTenantIdAsync(tenantId);
            if (result == null)
            {
                return NotFound(ApiResponse<TenantDto>.Error(404, "租户不存在"));
            }
            var dto = new TenantDto
            {
                TenantId = result.TenantId,
                Name = result.Name,
                DisplayName = result.DisplayName,
                Description = result.Description,
                Strategy = result.Strategy,
                IsActive = result.IsActive,
                Domain = result.Domain,
                MaxUsers = result.MaxUsers,
                StorageLimit = result.StorageLimit,
                ExpiresAt = result.ExpiresAt,
                CreatedAt = result.CreatedAt
            };
            return Ok(ApiResponse<TenantDto>.Success(dto));
        }

        /// <summary>
        /// 创建租户
        /// </summary>
        /// <param name="createDto">创建数据</param>
        /// <returns>创建结果</returns>
        [HttpPost]
        [DisplayName("创建租户")]
        public async Task<ActionResult<ApiResponse<TenantDto>>> CreateTenant([FromBody] TenantCreateDto createDto)
        {
            // 检查是否尝试创建系统租户
            if (string.Equals(createDto.TenantId, "system", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(ApiResponse<TenantDto>.Error(400, "不能创建租户ID为'system'的租户，该ID为系统保留"));
            }

            var result = await _tenantService.CreateAsync(createDto);
            return Ok(ApiResponse<TenantDto>.Success(result, "创建成功"));
        }

        ///// <summary>
        ///// 更新租户
        ///// </summary>
        ///// <param name="tenantId">租户ID</param>
        ///// <param name="updateDto">更新数据</param>
        ///// <returns>更新结果</returns>
        //[HttpPut("{tenantId}")]
        //[DisplayName("更新租户")]
        //public async Task<ActionResult<ApiResponse<TenantDto>>> UpdateTenant(string tenantId, [FromBody] TenantUpdateDto updateDto)
        //{
        //    // 检查是否为系统租户且尝试禁用
        //    if (string.Equals(tenantId, "system", StringComparison.OrdinalIgnoreCase) && !updateDto.IsActive)
        //    {
        //        return BadRequest(ApiResponse<TenantDto>.Error(400, "系统租户不能被禁用"));
        //    }

        //    await _tenantService.UpdateAsync(tenantId, updateDto);
        //    // 重新获取更新后的租户信息
        //    var updatedTenant = await _tenantService.GetByTenantIdAsync(tenantId);
        //    var tenantDto = new TenantDto
        //    {
        //        TenantId = updatedTenant.TenantId,
        //        Name = updatedTenant.Name,
        //        DisplayName = updatedTenant.DisplayName,
        //        Description = updatedTenant.Description,
        //        Strategy = updatedTenant.Strategy,
        //        IsActive = updatedTenant.IsActive,
        //        Domain = updatedTenant.Domain,
        //        MaxUsers = updatedTenant.MaxUsers,
        //        StorageLimit = updatedTenant.StorageLimit,
        //        ExpiresAt = updatedTenant.ExpiresAt,
        //        CreatedAt = updatedTenant.CreatedAt
        //    };
        //    return Ok(ApiResponse<TenantDto>.Success(tenantDto, "更新成功"));
        //}

        /// <summary>
        /// 删除租户
        /// </summary>
        /// <param name="tenantId">租户ID</param>
        /// <returns>删除结果</returns>
        [HttpDelete("{tenantId}")]
        [Operation("删除", "ajax", null, "确定要删除该租户吗？删除后将无法恢复！", "tenantId != 'system'")]
        [DisplayName("删除租户")]
        public async Task<ActionResult<ApiResponse>> DeleteTenant(string tenantId)
        {
            // 检查是否为系统租户
            if (string.Equals(tenantId, "system", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(ApiResponse.Error(400, "系统租户不能被删除"));
            }

            //TODO:租户数据处理
            await _tenantService.DeleteAsync(tenantId);
            return Ok(ApiResponse.Success("删除成功"));
        }

        /// <summary>
        /// 启用租户
        /// </summary>
        /// <param name="tenantId">租户ID</param>
        /// <returns>操作结果</returns>
        [HttpPut("{tenantId}/enable")]
        [Operation("启用", "ajax", null, "确定要启用该租户吗？", "!isActive")]
        [DisplayName("启用租户")]
        public async Task<ActionResult<ApiResponse>> EnableTenant(string tenantId)
        {
            var result = await _tenantService.EnableTenantAsync(tenantId);
            return Ok(result);
        }

        /// <summary>
        /// 禁用租户
        /// </summary>
        /// <param name="tenantId">租户ID</param>
        /// <returns>操作结果</returns>
        [HttpPut("{tenantId}/disable")]
        [Operation("禁用", "ajax", null, "确定要禁用该租户吗？", "isActive && tenantId != 'system'")]
        [DisplayName("禁用租户")]
        public async Task<ActionResult<ApiResponse>> DisableTenant(string tenantId)
        {
            // 检查是否为系统租户
            if (string.Equals(tenantId, "system", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(ApiResponse.Error(400, "系统租户不能被禁用"));
            }

            var result = await _tenantService.DisableTenantAsync(tenantId);
            return Ok(result);
        }

        /// <summary>
        /// 获取租户统计信息
        /// </summary>
        /// <param name="tenantId">租户ID</param>
        /// <returns>统计信息</returns>
        [HttpGet("{tenantId}/statistics")]
        [DisplayName("获取租户统计")]
        public async Task<ActionResult<ApiResponse<object>>> GetTenantStatistics(string tenantId)
        {
            var result = await _tenantService.GetTenantStatisticsAsync(tenantId);
            if (result == null)
            {
                return NotFound(ApiResponse<object>.Error(404, "租户不存在"));
            }
            return Ok(ApiResponse<object>.Success(result));
        }

        /// <summary>
        /// 检查租户是否过期
        /// </summary>
        /// <param name="tenantId">租户ID</param>
        /// <returns>是否过期</returns>
        [HttpGet("{tenantId}/expired")]
        [DisplayName("检查租户过期")]
        public async Task<ActionResult<ApiResponse<object>>> IsTenantExpired(string tenantId)
        {
            var result = await _tenantService.IsTenantExpiredAsync(tenantId);
            return Ok(ApiResponse<object>.Success(new { IsExpired = result }));
        }

        /// <summary>
        /// 续期租户
        /// </summary>
        /// <param name="tenantId">租户ID</param>
        /// <param name="request">续期请求</param>
        /// <returns>操作结果</returns>
        [HttpPut("{tenantId}/renew")]
        [Operation("续期", "form", null, null, "tenantId != 'system'")]
        [DisplayName("续期租户")]
        public async Task<ActionResult<ApiResponse>> RenewTenant(string tenantId, [FromBody] TenantRenewRequest request)
        {
            // 检查是否为系统租户
            if (string.Equals(tenantId, "system", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(ApiResponse.Error(400, "系统租户不需要续期"));
            }

            var result = await _tenantService.RenewTenantAsync(tenantId, request.ExpiresAt);
            return Ok(result);
        }

        /// <summary>
        /// 批量删除租户
        /// </summary>
        /// <param name="ids">租户ID列表</param>
        /// <returns>删除结果</returns>
        [HttpDelete("batch")]
        [DisplayName("批量删除租户")]
        public async Task<ActionResult<ApiResponse>> BatchDeleteTenants([FromBody] string[] ids)
        {
            // 检查是否包含系统租户
            if (ids.Any(id => string.Equals(id, "system", StringComparison.OrdinalIgnoreCase)))
            {
                return BadRequest(ApiResponse.Error(400, "批量删除中包含系统租户，系统租户不能被删除"));
            }

            var result = await _tenantService.BatchDeleteAsync(ids);
            return Ok(result);
        }

        /// <summary>
        /// 获取活跃租户列表（用于登录页选择）
        /// </summary>
        /// <returns>活跃租户列表</returns>
        [HttpGet("active")]
        [AllowAnonymous]
        [DisplayName("获取活跃租户列表")]
        public async Task<ActionResult<ApiResponse<IEnumerable<TenantSelectDto>>>> GetActiveTenants()
        {
            try
            {
                // 使用分页查询获取活跃租户
                var queryDto = new TenantQueryDto { IsActive = true, Page = 1, PerPage = 1000 };
                var result = await _tenantService.GetPagedListAsync(queryDto);
                
                var selectDtos = result.Items.Select(t => new TenantSelectDto
                {
                    TenantId = t.TenantId,
                    Name = t.Name,
                    DisplayName = t.DisplayName,
                    Description = t.Description,
                    LogoUrl = t.LogoUrl
                });
                
                return Ok(ApiResponse<IEnumerable<TenantSelectDto>>.Success(selectDtos));
            }
            catch
            {
                return BadRequest(ApiResponse<IEnumerable<TenantSelectDto>>.Error(400, "获取租户列表失败"));
            }
        }

        /// <summary>
        /// 跳转到租户登录页面
        /// </summary>
        /// <param name="id">租户ID</param>
        /// <returns>跳转结果</returns>
        [HttpPost("{tenantId}/redirect-login")]
        [Operation("进入登录", "ajax", null, "确定要进入该租户的登录页面吗？", "isActive == true", Redirect = "/${tenantId}/login")]
        [DisplayName("跳转租户登录")]
        public async Task<ActionResult<ApiResponse<object>>> RedirectToTenantLogin(string tenantId)
        {
            try
            {
                // 验证租户是否存在且活跃
                var tenant = await _tenantService.GetByTenantIdAsync(tenantId);
                if (tenant == null)
                {
                    return BadRequest(ApiResponse<object>.Error(404, "租户不存在"));
                }

                if (!tenant.IsActive)
                {
                    return BadRequest(ApiResponse<object>.Error(400, "租户已被禁用，无法登录"));
                }

                // 检查租户是否过期
                if (tenant.ExpiresAt.HasValue && tenant.ExpiresAt.Value < DateTime.UtcNow)
                {
                    return BadRequest(ApiResponse<object>.Error(400, "租户已过期，请联系管理员"));
                }

                // 构建租户登录页面URL
                var loginUrl = $"/{tenant.TenantId}/login";
                
                // 返回跳转信息
                var result = new
                {
                    tenantId = tenant.TenantId,
                    loginUrl = loginUrl,
                    tenantName = tenant.DisplayName ?? tenant.Name,
                    description = tenant.Description,
                    logoUrl = tenant.LogoUrl
                };

                return Ok(ApiResponse<object>.Success(result, "租户验证成功，即将跳转到登录页面"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.Error(500, "跳转失败：" + ex.Message));
            }
        }

        /// <summary>
        /// 获取租户登录页面配置
        /// </summary>
        /// <param name="tenantId">租户ID</param>
        /// <returns>登录页面配置</returns>
        [HttpGet("{tenantId}/login-config")]
        [AllowAnonymous]
        [DisplayName("获取租户登录配置")]
        public async Task<ActionResult<ApiResponse<object>>> GetTenantLoginConfig(string tenantId)
        {
            try
            {
                var tenant = await _tenantService.GetByTenantIdAsync(tenantId);
                if (tenant == null)
                {
                    return NotFound(ApiResponse<object>.Error(404, "租户不存在"));
                }

                if (!tenant.IsActive)
                {
                    return BadRequest(ApiResponse<object>.Error(400, "租户已停用"));
                }

                // 构建登录页面配置
                var config = new
                {
                    tenantId = tenant.TenantId,
                    name = tenant.Name,
                    displayName = tenant.DisplayName,
                    description = tenant.Description,
                    logoUrl = tenant.LogoUrl,
                    domain = tenant.Domain,
                    // 主题配置（如果有的话）
                    themeConfig = !string.IsNullOrEmpty(tenant.ThemeConfig) ? 
                        JsonSerializer.Deserialize<object>(tenant.ThemeConfig) : null,
                    // 功能配置（如果有的话）
                    configuration = !string.IsNullOrEmpty(tenant.Configuration) ? 
                        JsonSerializer.Deserialize<object>(tenant.Configuration) : null,
                    isActive = tenant.IsActive,
                    expiresAt = tenant.ExpiresAt
                };

                return Ok(ApiResponse<object>.Success(config));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.Error(500, "获取配置失败：" + ex.Message));
            }
        }
    }

    /// <summary>
    /// 租户续期请求
    /// </summary>
    public class TenantRenewRequest
    {
        /// <summary>
        /// 新的过期时间
        /// </summary>
        [Required(ErrorMessage = "过期时间不能为空")]
        [DisplayName("过期时间")]
        [AmisDatetimeField(RelativeTime = "nextmonth", Placeholder = "请选择新的过期时间")]
        public DateTime ExpiresAt { get; set; }
    }
} 