﻿// Controllers/AuthController.cs
using CodeSpirit.Core;
using CodeSpirit.Core.Attributes;
using CodeSpirit.IdentityApi.Constants;
using CodeSpirit.IdentityApi.Dtos.LoginLogs;
using CodeSpirit.IdentityApi.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace CodeSpirit.IdentityApi.Controllers
{
    [DisplayName("登录日志")]
    [Navigation(Icon = "fa-solid fa-clock-rotate-left")]
    public partial class LoginLogsController : ApiControllerBase
    {
        private readonly ILoginLogService _loginLogService;

        public LoginLogsController(ILoginLogService loginLogService)
        {
            _loginLogService = loginLogService;
        }

        /// <summary>
        /// 获取分页的登录日志。
        /// </summary>
        /// <param name="pageNumber">页码，从1开始。</param>
        /// <param name="pageSize">每页的记录数。</param>
        /// <param name="userName">按用户名过滤（可选）。</param>
        /// <param name="isSuccess">按登录结果过滤（可选）。</param>
        /// <returns>分页的登录日志列表。</returns>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<PageList<LoginLogDto>>>> GetLoginLogs([FromQuery] LoginLogsQueryDto queryDto)
        {
            PageList<LoginLogDto> result = await _loginLogService.GetPagedLoginLogsAsync(queryDto);
            return SuccessResponse(result);
        }

        /// <summary>
        /// 获取指定 ID 的登录日志。
        /// </summary>
        /// <param name="id">登录日志的唯一标识。</param>
        /// <returns>单个登录日志详情。</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<LoginLogDto>> GetLoginLog(int id)
        {
            LoginLogDto logDto = await _loginLogService.GetLoginLogByIdAsync(id);
            return logDto == null ? (ActionResult<LoginLogDto>)NotFound() : (ActionResult<LoginLogDto>)Ok(logDto);
        }
    }
}
