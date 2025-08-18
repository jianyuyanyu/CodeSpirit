using AutoMapper;
using CodeSpirit.Core;
using CodeSpirit.IdentityApi.Data;
using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.IdentityApi.Dtos.Tenant;
using CodeSpirit.MultiTenant.Models;
using CodeSpirit.Shared.Repositories;
using CodeSpirit.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace CodeSpirit.IdentityApi.Services
{
    /// <summary>
    /// 租户服务实现
    /// </summary>
    public class TenantService : BaseCRUDService<TenantInfo, TenantDto, string, TenantCreateDto, TenantUpdateDto>, ITenantService
    {
        private readonly ApplicationDbContext _context;
        private readonly ITenantDataInitializationService _dataInitializationService;
        private readonly ILogger<TenantService> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="repository">仓储</param>
        /// <param name="mapper">映射器</param>
        /// <param name="context">数据库上下文</param>
        /// <param name="dataInitializationService">租户数据初始化服务</param>
        /// <param name="logger">日志记录器</param>
        public TenantService(
            IRepository<TenantInfo> repository,
            IMapper mapper,
            ApplicationDbContext context,
            ITenantDataInitializationService dataInitializationService,
            ILogger<TenantService> logger) : base(repository, mapper)
        {
            _context = context;
            _dataInitializationService = dataInitializationService;
            _logger = logger;
        }

        /// <summary>
        /// 构建查询表达式
        /// </summary>
        /// <param name="queryDto">查询条件</param>
        /// <returns>查询表达式</returns>
        private Expression<Func<TenantInfo, bool>> BuildQueryExpression(TenantQueryDto queryDto)
        {
            Expression<Func<TenantInfo, bool>> expression = x => true;

            if (!string.IsNullOrWhiteSpace(queryDto.TenantId))
            {
                var tenantIdFilter = (Expression<Func<TenantInfo, bool>>)(x => x.TenantId.Contains(queryDto.TenantId));
                expression = CombineExpressions(expression, tenantIdFilter);
            }

            if (!string.IsNullOrWhiteSpace(queryDto.Name))
            {
                var nameFilter = (Expression<Func<TenantInfo, bool>>)(x => x.Name.Contains(queryDto.Name));
                expression = CombineExpressions(expression, nameFilter);
            }

            if (!string.IsNullOrWhiteSpace(queryDto.DisplayName))
            {
                var displayNameFilter = (Expression<Func<TenantInfo, bool>>)(x => x.DisplayName.Contains(queryDto.DisplayName));
                expression = CombineExpressions(expression, displayNameFilter);
            }

            if (queryDto.Strategy.HasValue)
            {
                var strategyFilter = (Expression<Func<TenantInfo, bool>>)(x => x.Strategy == queryDto.Strategy.Value);
                expression = CombineExpressions(expression, strategyFilter);
            }

            if (queryDto.IsActive.HasValue)
            {
                var activeFilter = (Expression<Func<TenantInfo, bool>>)(x => x.IsActive == queryDto.IsActive.Value);
                expression = CombineExpressions(expression, activeFilter);
            }

            if (!string.IsNullOrWhiteSpace(queryDto.Domain))
            {
                var domainFilter = (Expression<Func<TenantInfo, bool>>)(x => x.Domain.Contains(queryDto.Domain));
                expression = CombineExpressions(expression, domainFilter);
            }

            if (queryDto.IsExpired.HasValue)
            {
                var now = DateTime.UtcNow;
                if (queryDto.IsExpired.Value)
                {
                    var expiredFilter = (Expression<Func<TenantInfo, bool>>)(x => x.ExpiresAt.HasValue && x.ExpiresAt.Value < now);
                    expression = CombineExpressions(expression, expiredFilter);
                }
                else
                {
                    var notExpiredFilter = (Expression<Func<TenantInfo, bool>>)(x => !x.ExpiresAt.HasValue || x.ExpiresAt.Value >= now);
                    expression = CombineExpressions(expression, notExpiredFilter);
                }
            }

            if (queryDto.CreatedAtStart.HasValue)
            {
                var startFilter = (Expression<Func<TenantInfo, bool>>)(x => x.CreatedAt >= queryDto.CreatedAtStart.Value);
                expression = CombineExpressions(expression, startFilter);
            }

            if (queryDto.CreatedAtEnd.HasValue)
            {
                var endFilter = (Expression<Func<TenantInfo, bool>>)(x => x.CreatedAt <= queryDto.CreatedAtEnd.Value);
                expression = CombineExpressions(expression, endFilter);
            }

            return expression;
        }

        /// <summary>
        /// 组合表达式
        /// </summary>
        private Expression<Func<T, bool>> CombineExpressions<T>(Expression<Func<T, bool>> expr1, Expression<Func<T, bool>> expr2)
        {
            var parameter = Expression.Parameter(typeof(T));
            var body = Expression.AndAlso(
                Expression.Invoke(expr1, parameter),
                Expression.Invoke(expr2, parameter)
            );
            return Expression.Lambda<Func<T, bool>>(body, parameter);
        }

        /// <summary>
        /// 创建前验证
        /// </summary>
        /// <param name="createDto">创建数据</param>
        /// <returns>验证结果</returns>
        private async Task<ApiResponse> ValidateBeforeCreateAsync(TenantCreateDto createDto)
        {
            // 检查租户ID是否已存在
            if (await ExistsByTenantIdAsync(createDto.TenantId))
            {
                return new ApiResponse(400, $"租户ID '{createDto.TenantId}' 已存在");
            }

            // 验证连接字符串（如果是独立数据库策略）
            if (createDto.Strategy == TenantStrategy.SeparateDatabase && string.IsNullOrWhiteSpace(createDto.ConnectionString))
            {
                return new ApiResponse(400, "独立数据库策略必须提供连接字符串");
            }

            return new ApiResponse(0, "验证通过");
        }

        /// <summary>
        /// 根据租户ID获取租户信息
        /// </summary>
        /// <param name="tenantId">租户ID</param>
        /// <returns>租户信息</returns>
        public async Task<TenantInfo> GetByTenantIdAsync(string tenantId)
        {
            return await _context.WithoutMultiTenantFilterAsync(async () =>
            {
                return await _context.Tenants.FirstOrDefaultAsync(x => x.TenantId == tenantId);
            });
        }

        /// <summary>
        /// 检查租户ID是否存在
        /// </summary>
        /// <param name="tenantId">租户ID</param>
        /// <returns>是否存在</returns>
        public async Task<bool> ExistsByTenantIdAsync(string tenantId)
        {
            return await _context.WithoutMultiTenantFilterAsync(async () =>
            {
                return await _context.Tenants.AnyAsync(x => x.TenantId == tenantId);
            });
        }

        /// <summary>
        /// 启用租户
        /// </summary>
        /// <param name="tenantId">租户ID</param>
        /// <returns>操作结果</returns>
        public async Task<ApiResponse> EnableTenantAsync(string tenantId)
        {
            var tenant = await GetByTenantIdAsync(tenantId);
            if (tenant == null)
            {
                return new ApiResponse(404, "租户不存在");
            }

            tenant.IsActive = true;
            _context.Tenants.Update(tenant);
            await _context.SaveChangesAsync();
            return new ApiResponse(0, "租户已启用");
        }

        /// <summary>
        /// 禁用租户
        /// </summary>
        /// <param name="tenantId">租户ID</param>
        /// <returns>操作结果</returns>
        public async Task<ApiResponse> DisableTenantAsync(string tenantId)
        {
            var tenant = await GetByTenantIdAsync(tenantId);
            if (tenant == null)
            {
                return new ApiResponse(404, "租户不存在");
            }

            tenant.IsActive = false;
            _context.Tenants.Update(tenant);
            await _context.SaveChangesAsync();
            return new ApiResponse(0, "租户已禁用");
        }

        /// <summary>
        /// 获取租户统计信息
        /// </summary>
        /// <param name="tenantId">租户ID</param>
        /// <returns>统计信息</returns>
        public async Task<object> GetTenantStatisticsAsync(string tenantId)
        {
            var tenant = await GetByTenantIdAsync(tenantId);
            if (tenant == null)
            {
                return null;
            }

            // 统计租户下的用户数量
            var userCount = await _context.Users.CountAsync(u => u.TenantId == tenantId);
            
            // 统计租户下的角色数量
            var roleCount = await _context.Roles.CountAsync(r => r.TenantId == tenantId);

            // 计算存储使用情况（这里是示例，实际需要根据业务需求计算）
            var storageUsed = 0L; // 实际应该计算租户的存储使用量

            return new
            {
                TenantId = tenantId,
                TenantName = tenant.Name,
                UserCount = userCount,
                RoleCount = roleCount,
                MaxUsers = tenant.MaxUsers,
                StorageUsed = storageUsed,
                StorageLimit = tenant.StorageLimit,
                IsActive = tenant.IsActive,
                IsExpired = tenant.ExpiresAt.HasValue && tenant.ExpiresAt.Value < DateTime.UtcNow,
                ExpiresAt = tenant.ExpiresAt,
                CreatedAt = tenant.CreatedAt
            };
        }

        /// <summary>
        /// 检查租户是否过期
        /// </summary>
        /// <param name="tenantId">租户ID</param>
        /// <returns>是否过期</returns>
        public async Task<bool> IsTenantExpiredAsync(string tenantId)
        {
            var tenant = await GetByTenantIdAsync(tenantId);
            if (tenant == null)
            {
                return true; // 租户不存在视为过期
            }

            return tenant.ExpiresAt.HasValue && tenant.ExpiresAt.Value < DateTime.UtcNow;
        }

        /// <summary>
        /// 续期租户
        /// </summary>
        /// <param name="tenantId">租户ID</param>
        /// <param name="expiresAt">新的过期时间</param>
        /// <returns>操作结果</returns>
        public async Task<ApiResponse> RenewTenantAsync(string tenantId, DateTime expiresAt)
        {
            var tenant = await GetByTenantIdAsync(tenantId);
            if (tenant == null)
            {
                return new ApiResponse(404, "租户不存在");
            }

            if (expiresAt <= DateTime.UtcNow)
            {
                return new ApiResponse(400, "过期时间必须大于当前时间");
            }

            tenant.ExpiresAt = expiresAt;
            _context.Tenants.Update(tenant);
            await _context.SaveChangesAsync();
            return new ApiResponse(0, "租户续期成功");
        }

        /// <summary>
        /// 分页查询租户
        /// </summary>
        /// <param name="queryDto">查询条件</param>
        /// <returns>分页结果</returns>
        public async Task<PageList<TenantDto>> GetPagedListAsync(TenantQueryDto queryDto)
        {
            var expression = BuildQueryExpression(queryDto);
            var query = _context.Tenants.Where(expression);
            
            var total = await query.CountAsync();
            var items = await query
                .Skip((queryDto.Page - 1) * queryDto.PerPage)
                .Take(queryDto.PerPage)
                .ToListAsync();
            
            var dtos = Mapper.Map<List<TenantDto>>(items);
            
            return new PageList<TenantDto>(dtos, total);
        }

        /// <summary>
        /// 更新租户（重写基类方法以正确处理租户筛选器）
        /// </summary>
        /// <param name="tenantId">租户ID</param>
        /// <param name="updateDto">更新数据</param>
        /// <returns>更新任务</returns>
        public override async Task UpdateAsync(string tenantId, TenantUpdateDto updateDto)
        {
            ArgumentNullException.ThrowIfNull(updateDto);

            // 使用 WithoutMultiTenantFilterAsync 确保整个更新过程都禁用租户筛选器
            await _context.WithoutMultiTenantFilterAsync(async () =>
            {
                // 获取要更新的租户
                var tenant = await _context.Tenants.FirstOrDefaultAsync(x => x.TenantId == tenantId);
                if (tenant == null)
                {
                    throw new AppServiceException(404, "租户不存在！");
                }

                // 使用 AutoMapper 进行部分更新
                Mapper.Map(updateDto, tenant);
                
                // 手动设置更新时间（AutoMapper配置中已处理，但这里确保设置）
                tenant.UpdatedAt = DateTime.UtcNow;
                
                // 更新实体
                _context.Tenants.Update(tenant);
                await _context.SaveChangesAsync();
                
                return Task.CompletedTask;
            });
        }

        /// <summary>
        /// 创建租户（重写基类方法以添加数据初始化）
        /// </summary>
        /// <param name="createDto">创建数据</param>
        /// <returns>创建结果</returns>
        public override async Task<TenantDto> CreateAsync(TenantCreateDto createDto)
        {
            // 创建前验证
            var validationResult = await ValidateBeforeCreateAsync(createDto);
            if (validationResult.Status != 0)
            {
                throw new BusinessException(validationResult.Msg);
            }

            // 调用基类方法创建租户
            var tenantDto = await base.CreateAsync(createDto);

            try
            {
                // 为新创建的租户初始化默认数据
                var initResult = await _dataInitializationService.InitializeTenantDataAsync(createDto.TenantId, true);
                if (initResult.Status != 0)
                {
                    // 如果数据初始化失败，记录警告但不回滚租户创建
                    // 这样可以避免因为数据初始化问题导致租户创建失败
                    // 管理员可以后续手动初始化数据
                    _logger.LogWarning("租户 {TenantId} 创建成功，但数据初始化失败: {Message}", 
                        createDto.TenantId, initResult.Msg);
                }
                else
                {
                    _logger.LogInformation("租户 {TenantId} 创建成功，数据初始化完成", createDto.TenantId);
                }
            }
            catch (Exception ex)
            {
                // 记录数据初始化异常但不影响租户创建
                _logger.LogError(ex, "租户 {TenantId} 创建成功，但数据初始化过程中发生异常: {Message}", 
                    createDto.TenantId, ex.Message);
            }

            return tenantDto;
        }






    }
} 