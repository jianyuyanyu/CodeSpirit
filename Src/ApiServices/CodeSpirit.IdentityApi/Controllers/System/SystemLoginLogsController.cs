using CodeSpirit.Core;
using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Enums;
using CodeSpirit.IdentityApi.Dtos.LoginLogs;
using CodeSpirit.IdentityApi.Services;
using CodeSpirit.Shared.Data;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace CodeSpirit.IdentityApi.Controllers.System
{
    /// <summary>
    /// 系统平台登录日志管理控制器
    /// </summary>
    [DisplayName("登录日志")]
    [Navigation(Icon = "fa-solid fa-clock-rotate-left", PlatformType = PlatformType.System, TitleResourceKey = "Controller.LoginLogs", TitleResourceType = typeof(CodeSpirit.Navigation.Resources.NavigationResources))]
    public partial class SystemLoginLogsController : ApiControllerBase
    {
        private readonly ILoginLogService _loginLogService;
        private readonly IDataFilter _dataFilter;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="loginLogService">登录日志服务</param>
        /// <param name="dataFilter">数据筛选器</param>
        public SystemLoginLogsController(
            ILoginLogService loginLogService,
            IDataFilter dataFilter)
        {
            _loginLogService = loginLogService;
            _dataFilter = dataFilter;
        }

        /// <summary>
        /// 获取系统平台登录日志列表（跨租户查询）
        /// </summary>
        /// <param name="queryDto">查询参数</param>
        /// <returns>登录日志列表</returns>
        [HttpGet]
        [DisplayName("获取登录日志列表")]
        public async Task<ActionResult<ApiResponse<PageList<LoginLogDto>>>> GetLoginLogs(
            [FromQuery] SystemLoginLogsQueryDto queryDto)
        {
            // 使用 IDataFilter 禁用多租户筛选器来执行跨租户查询
            using (_dataFilter.Disable<IMultiTenant>())
            {
                var result = await _loginLogService.GetSystemPagedLoginLogsAsync(queryDto);
                return SuccessResponse(result);
            }
        }

        /// <summary>
        /// 导出系统平台登录日志列表
        /// </summary>
        /// <param name="queryDto">查询条件</param>
        /// <returns>登录日志数据</returns>
        [HttpGet("export")]
        [DisplayName("导出登录日志列表")]
        public async Task<ActionResult<ApiResponse<PageList<LoginLogDto>>>> ExportLoginLogs([FromQuery] SystemLoginLogsQueryDto queryDto)
        {
            // 使用 IDataFilter 禁用多租户筛选器来执行跨租户查询
            using (_dataFilter.Disable<IMultiTenant>())
            {
                // 设置导出时的分页参数
                const int MaxExportLimit = 50000; // 系统级导出数量上限更高
                queryDto.PerPage = MaxExportLimit;
                queryDto.Page = 1;

                // 获取登录日志数据
                PageList<LoginLogDto> logs = await _loginLogService.GetSystemPagedLoginLogsAsync(queryDto);

                // 如果数据为空则返回错误信息
                return logs.Items.Count == 0 ? BadResponse<PageList<LoginLogDto>>("没有数据可供导出") : SuccessResponse(logs);
            }
        }

        /// <summary>
        /// 获取按租户分组的登录日志统计信息
        /// </summary>
        /// <returns>租户登录日志统计列表</returns>
        [HttpGet("tenant-statistics")]
        [DisplayName("获取租户登录日志统计")]
        public async Task<ActionResult<ApiResponse<List<TenantLoginLogStatisticsDto>>>> GetTenantLoginLogStatistics()
        {
            // 使用 IDataFilter 禁用多租户筛选器来执行跨租户查询
            using (_dataFilter.Disable<IMultiTenant>())
            {
                List<TenantLoginLogStatisticsDto> statistics = await _loginLogService.GetLoginLogsByTenantStatisticsAsync();
                return SuccessResponse(statistics);
            }
        }

        /// <summary>
        /// 获取指定 ID 的登录日志详情（跨租户查询）
        /// </summary>
        /// <param name="id">登录日志的唯一标识</param>
        /// <returns>单个登录日志详情</returns>
        [HttpGet("{id}")]
        [DisplayName("获取登录日志详情")]
        public async Task<ActionResult<LoginLogDto>> GetLoginLog(int id)
        {
            // 使用 IDataFilter 禁用多租户筛选器来执行跨租户查询
            using (_dataFilter.Disable<IMultiTenant>())
            {
                LoginLogDto logDto = await _loginLogService.GetLoginLogByIdAsync(id);
                return logDto == null ? (ActionResult<LoginLogDto>)NotFound() : (ActionResult<LoginLogDto>)Ok(logDto);
            }
        }
    }
} 