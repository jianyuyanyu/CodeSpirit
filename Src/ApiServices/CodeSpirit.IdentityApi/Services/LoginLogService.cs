using AutoMapper;
using CodeSpirit.Core;
using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.IdentityApi.Dtos.LoginLogs;
using CodeSpirit.MultiTenant.Models;
using CodeSpirit.Shared.Data;
using CodeSpirit.Shared.Repositories;
using LinqKit;
using Microsoft.EntityFrameworkCore;

namespace CodeSpirit.IdentityApi.Services
{
    /// <summary>
    /// 登录日志服务类
    /// </summary>
    public class LoginLogService : ILoginLogService
    {
        private readonly IRepository<LoginLog> _loginLogRepository;
        private readonly IRepository<TenantInfo> _tenantRepository;
        private readonly IMapper _mapper;
        private readonly IDataFilter dataFilter;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="loginLogRepository">登录日志仓储接口</param>
        /// <param name="tenantRepository">租户仓储接口</param>
        /// <param name="mapper">AutoMapper实例</param>
        public LoginLogService(IRepository<LoginLog> loginLogRepository, IRepository<TenantInfo> tenantRepository, IMapper mapper, IDataFilter dataFilter)
        {
            _loginLogRepository = loginLogRepository;
            _tenantRepository = tenantRepository;
            _mapper = mapper;
            this.dataFilter = dataFilter;
        }

        /// <summary>
        /// 获取分页的登录日志列表
        /// </summary>
        /// <param name="queryDto">查询参数DTO</param>
        /// <returns>登录日志列表数据</returns>
        public async Task<PageList<LoginLogDto>> GetPagedLoginLogsAsync(LoginLogsQueryDto queryDto)
        {
            ExpressionStarter<LoginLog> predicate = PredicateBuilder.New<LoginLog>(true);

            if (!string.IsNullOrWhiteSpace(queryDto.Keywords))
            {
                string searchLower = queryDto.Keywords.ToLower();
                predicate = predicate.And(x =>
                    x.UserName.ToLower().Contains(searchLower) ||
                    x.User.PhoneNumber.ToLower().Contains(searchLower) ||
                    x.IPAddress.ToLower().Contains(searchLower));
            }

            if (!string.IsNullOrEmpty(queryDto.UserName))
            {
                predicate = predicate.And(x => x.UserName.Contains(queryDto.UserName));
            }

            if (queryDto.IsSuccess.HasValue)
            {
                predicate = predicate.And(x => x.IsSuccess == queryDto.IsSuccess.Value);
            }

            if (string.IsNullOrEmpty(queryDto.OrderBy))
            {
                queryDto.OrderBy = "LoginTime";
                queryDto.OrderDir = "desc";
            }

            PageList<LoginLog> result = await _loginLogRepository.GetPagedAsync(
                queryDto.Page,
                queryDto.PerPage,
                predicate,
                queryDto.OrderBy,
                queryDto.OrderDir,
                "User"
            );

            // 获取租户信息
            var tenantIds = result.Items.Select(x => x.TenantId).Distinct().ToList();
            var tenants = await _tenantRepository.Find(t => tenantIds.Contains(t.TenantId)).ToListAsync();
            var tenantDict = tenants.ToDictionary(t => t.TenantId, t => t.DisplayName ?? t.Name);

            // 映射并设置租户名称
            var loginLogDtos = _mapper.Map<PageList<LoginLogDto>>(result);
            foreach (var dto in loginLogDtos.Items)
            {
                dto.TenantName = tenantDict.GetValueOrDefault(dto.TenantId, dto.TenantId);
            }

            return loginLogDtos;
        }

        /// <summary>
        /// 获取系统平台分页的登录日志列表（支持租户查询）
        /// </summary>
        /// <param name="queryDto">系统查询参数DTO</param>
        /// <returns>登录日志列表数据</returns>
        public async Task<PageList<LoginLogDto>> GetSystemPagedLoginLogsAsync(SystemLoginLogsQueryDto queryDto)
        {
            ExpressionStarter<LoginLog> predicate = PredicateBuilder.New<LoginLog>(true);

            if (!string.IsNullOrWhiteSpace(queryDto.Keywords))
            {
                string searchLower = queryDto.Keywords.ToLower();
                predicate = predicate.And(x =>
                    x.UserName.ToLower().Contains(searchLower) ||
                    x.User.PhoneNumber.ToLower().Contains(searchLower) ||
                    x.IPAddress.ToLower().Contains(searchLower) ||
                    x.FailureReason.ToLower().Contains(searchLower));
            }

            if (!string.IsNullOrEmpty(queryDto.UserName))
            {
                predicate = predicate.And(x => x.UserName.Contains(queryDto.UserName));
            }

            if (queryDto.IsSuccess.HasValue)
            {
                predicate = predicate.And(x => x.IsSuccess == queryDto.IsSuccess.Value);
            }

            if (!string.IsNullOrEmpty(queryDto.IPAddress))
            {
                predicate = predicate.And(x => x.IPAddress.Contains(queryDto.IPAddress));
            }

            if (!string.IsNullOrEmpty(queryDto.TenantId))
            {
                predicate = predicate.And(x => x.TenantId == queryDto.TenantId);
            }

            if (!string.IsNullOrEmpty(queryDto.FailureReason))
            {
                predicate = predicate.And(x => x.FailureReason.Contains(queryDto.FailureReason));
            }

            if (queryDto.LoginTimeStart.HasValue)
            {
                predicate = predicate.And(x => x.LoginTime >= queryDto.LoginTimeStart.Value);
            }

            if (queryDto.LoginTimeEnd.HasValue)
            {
                predicate = predicate.And(x => x.LoginTime <= queryDto.LoginTimeEnd.Value);
            }

            if (queryDto.UserId.HasValue)
            {
                predicate = predicate.And(x => x.UserId == queryDto.UserId.Value);
            }

            if (string.IsNullOrEmpty(queryDto.OrderBy))
            {
                queryDto.OrderBy = "LoginTime";
                queryDto.OrderDir = "desc";
            }

            PageList<LoginLog> result = await _loginLogRepository.GetPagedAsync(
                queryDto.Page,
                queryDto.PerPage,
                predicate,
                queryDto.OrderBy,
                queryDto.OrderDir,
                "User"
            );

            // 获取租户信息
            var tenantIds = result.Items.Select(x => x.TenantId).Distinct().ToList();
            var tenants = await _tenantRepository.Find(t => tenantIds.Contains(t.TenantId)).ToListAsync();
            var tenantDict = tenants.ToDictionary(t => t.TenantId, t => t.DisplayName ?? t.Name);

            // 映射并设置租户名称
            var loginLogDtos = _mapper.Map<PageList<LoginLogDto>>(result);
            foreach (var dto in loginLogDtos.Items)
            {
                dto.TenantName = tenantDict.GetValueOrDefault(dto.TenantId, dto.TenantId);
            }
            return loginLogDtos;
        }

        /// <summary>
        /// 获取按租户分组的登录日志统计信息
        /// </summary>
        /// <returns>租户登录日志统计列表</returns>
        public async Task<List<TenantLoginLogStatisticsDto>> GetLoginLogsByTenantStatisticsAsync()
        {
            var now = DateTime.Now;
            var today = now.Date;
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
            var startOfMonth = new DateTime(now.Year, now.Month, 1);

            // 获取所有登录日志
            var logs = await _loginLogRepository.Find(x => true).ToListAsync();

            // 获取租户信息
            var tenantIds = logs.Select(x => x.TenantId).Distinct().ToList();
            var tenants = await _tenantRepository.Find(t => tenantIds.Contains(t.TenantId)).ToListAsync();
            var tenantDict = tenants.ToDictionary(t => t.TenantId, t => new { t.Name, t.DisplayName });

            // 按租户分组统计
            var statistics = logs
                .GroupBy(x => x.TenantId)
                .Select(g => new TenantLoginLogStatisticsDto
                {
                    TenantId = g.Key,
                    TenantName = tenantDict.GetValueOrDefault(g.Key)?.Name ?? g.Key,
                    TenantDisplayName = tenantDict.GetValueOrDefault(g.Key)?.DisplayName ?? tenantDict.GetValueOrDefault(g.Key)?.Name ?? g.Key,
                    TotalLogins = g.Count(),
                    SuccessfulLogins = g.Count(x => x.IsSuccess),
                    FailedLogins = g.Count(x => !x.IsSuccess),
                    SuccessRate = g.Count() > 0 ? Math.Round((decimal)g.Count(x => x.IsSuccess) / g.Count() * 100, 2) : 0,
                    UniqueUsers = g.Where(x => x.UserId.HasValue).Select(x => x.UserId).Distinct().Count(),
                    TodayLogins = g.Count(x => x.LoginTime >= today),
                    ThisWeekLogins = g.Count(x => x.LoginTime >= startOfWeek),
                    ThisMonthLogins = g.Count(x => x.LoginTime >= startOfMonth),
                    LastLoginTime = g.OrderByDescending(x => x.LoginTime).FirstOrDefault()?.LoginTime,
                    MostActiveHour = g.Any() ? g.GroupBy(x => x.LoginTime.Hour)
                        .OrderByDescending(h => h.Count())
                        .First().Key + ":00-" + (g.GroupBy(x => x.LoginTime.Hour)
                        .OrderByDescending(h => h.Count())
                        .First().Key + 1) + ":00" : "无数据"
                })
                .OrderByDescending(x => x.TotalLogins)
                .ToList();

            return statistics;
        }

        /// <summary>
        /// 获取租户平台分页的登录日志列表（不包含租户信息）
        /// </summary>
        /// <param name="queryDto">查询参数DTO</param>
        /// <returns>登录日志列表数据</returns>
        public async Task<PageList<TenantLoginLogDto>> GetTenantPagedLoginLogsAsync(LoginLogsQueryDto queryDto)
        {
            ExpressionStarter<LoginLog> predicate = PredicateBuilder.New<LoginLog>(true);

            if (!string.IsNullOrWhiteSpace(queryDto.Keywords))
            {
                string searchLower = queryDto.Keywords.ToLower();
                predicate = predicate.And(x =>
                    x.UserName.ToLower().Contains(searchLower) ||
                    x.User.PhoneNumber.ToLower().Contains(searchLower) ||
                    x.IPAddress.ToLower().Contains(searchLower));
            }

            if (!string.IsNullOrEmpty(queryDto.UserName))
            {
                predicate = predicate.And(x => x.UserName.Contains(queryDto.UserName));
            }

            if (queryDto.IsSuccess.HasValue)
            {
                predicate = predicate.And(x => x.IsSuccess == queryDto.IsSuccess.Value);
            }

            if (string.IsNullOrEmpty(queryDto.OrderBy))
            {
                queryDto.OrderBy = "LoginTime";
                queryDto.OrderDir = "desc";
            }

            PageList<LoginLog> result = await _loginLogRepository.GetPagedAsync(
                queryDto.Page,
                queryDto.PerPage,
                predicate,
                queryDto.OrderBy,
                queryDto.OrderDir,
                "User"
            );

            // 直接映射到租户DTO，不包含租户信息
            return _mapper.Map<PageList<TenantLoginLogDto>>(result);
        }

        /// <summary>
        /// 根据ID获取登录日志详情（租户平台版本，不包含租户信息）
        /// </summary>
        /// <param name="id">登录日志ID</param>
        /// <returns>租户登录日志DTO，如果未找到则返回null</returns>
        public async Task<TenantLoginLogDto> GetTenantLoginLogByIdAsync(int id)
        {
            LoginLog log = await _loginLogRepository.GetByIdAsync(id);
            return log == null
                ? throw new AppServiceException(404, "登录日志不存在")
                : _mapper.Map<TenantLoginLogDto>(log);
        }

        /// <summary>
        /// 根据ID获取登录日志详情
        /// </summary>
        /// <param name="id">登录日志ID</param>
        /// <returns>登录日志DTO，如果未找到则返回null</returns>
        public async Task<LoginLogDto> GetLoginLogByIdAsync(int id)
        {
            LoginLog log = await _loginLogRepository.GetByIdAsync(id);
            if (log == null)
            {
                throw new AppServiceException(404, "登录日志不存在");
            }

            // 🔥 修复：添加租户名称映射
            var loginLogDto = _mapper.Map<LoginLogDto>(log);

            // 获取租户信息
            var tenant = await _tenantRepository.Find(t => t.TenantId == log.TenantId).FirstOrDefaultAsync();
            loginLogDto.TenantName = tenant?.DisplayName ?? tenant?.Name ?? log.TenantId;

            return loginLogDto;
        }
    }
}