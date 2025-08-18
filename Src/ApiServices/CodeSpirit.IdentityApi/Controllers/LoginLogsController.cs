// Controllers/AuthController.cs
using CodeSpirit.Core;
using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Enums;
using CodeSpirit.IdentityApi.Constants;
using CodeSpirit.IdentityApi.Dtos.LoginLogs;
using CodeSpirit.IdentityApi.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace CodeSpirit.IdentityApi.Controllers
{
    /// <summary>
    /// 租户平台登录日志管理控制器
    /// </summary>
    [DisplayName("登录日志")]
    [Navigation(Icon = "fa-solid fa-clock-rotate-left", PlatformType = PlatformType.Tenant)]
    public partial class LoginLogsController : ApiControllerBase
    {
        private readonly ILoginLogService _loginLogService;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="loginLogService">登录日志服务</param>
        public LoginLogsController(ILoginLogService loginLogService)
        {
            _loginLogService = loginLogService;
        }

        /// <summary>
        /// 获取分页的登录日志（租户平台，仅显示当前租户的日志，不包含租户信息）
        /// </summary>
        /// <param name="queryDto">查询条件</param>
        /// <returns>分页的登录日志列表</returns>
        [HttpGet]
        [DisplayName("获取登录日志列表")]
        public async Task<ActionResult<ApiResponse<PageList<TenantLoginLogDto>>>> GetLoginLogs([FromQuery] LoginLogsQueryDto queryDto)
        {
            // 租户平台自动过滤当前租户的登录日志，并且不显示租户信息
            PageList<TenantLoginLogDto> result = await _loginLogService.GetTenantPagedLoginLogsAsync(queryDto);
            return SuccessResponse(result);
        }

        /// <summary>
        /// 导出租户平台登录日志列表
        /// </summary>
        /// <param name="queryDto">查询条件</param>
        /// <returns>登录日志数据</returns>
        [HttpGet("export")]
        [DisplayName("导出登录日志列表")]
        public async Task<ActionResult<ApiResponse<PageList<TenantLoginLogDto>>>> ExportLoginLogs([FromQuery] LoginLogsQueryDto queryDto)
        {
            // 设置导出时的分页参数
            const int MaxExportLimit = 10000; // 租户级导出数量限制
            queryDto.PerPage = MaxExportLimit;
            queryDto.Page = 1;

            // 获取登录日志数据（不包含租户信息）
            PageList<TenantLoginLogDto> logs = await _loginLogService.GetTenantPagedLoginLogsAsync(queryDto);

            // 如果数据为空则返回错误信息
            return logs.Items.Count == 0 ? BadResponse<PageList<TenantLoginLogDto>>("没有数据可供导出") : SuccessResponse(logs);
        }

        /// <summary>
        /// 获取指定 ID 的登录日志（租户平台，不包含租户信息）
        /// </summary>
        /// <param name="id">登录日志的唯一标识。</param>
        /// <returns>单个登录日志详情。</returns>
        [HttpGet("{id}")]
        [DisplayName("获取登录日志详情")]
        public async Task<ActionResult<TenantLoginLogDto>> GetLoginLog(int id)
        {
            TenantLoginLogDto logDto = await _loginLogService.GetTenantLoginLogByIdAsync(id);
            return logDto == null ? (ActionResult<TenantLoginLogDto>)NotFound() : (ActionResult<TenantLoginLogDto>)Ok(logDto);
        }
    }
}
