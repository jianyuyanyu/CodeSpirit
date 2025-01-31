// Controllers/AuthController.cs
using CodeSpirit.Amis.Attributes;
using CodeSpirit.Core;
using CodeSpirit.IdentityApi.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;

namespace CodeSpirit.IdentityApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize(Roles = "Administrator")] // 仅管理员可以查看登录日志
    [Page(Label = "登录日志", ParentLabel = "用户中心", Icon = "fa-solid fa-info")]
    [DisplayName("登录日志")]
    public partial class LoginLogsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public LoginLogsController(ApplicationDbContext context)
        {
            _context = context;
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
        public async Task<ActionResult<ApiResponse<ListData<LoginLogDto>>>> GetLoginLogs(
            int pageNumber = 1,
            int pageSize = 20,
            string userName = null,
            bool? isSuccess = null)
        {
            if (pageNumber <= 0) pageNumber = 1;
            if (pageSize <= 0) pageSize = 20;

            var query = _context.LoginLogs
                .Include(l => l.User)
                .AsQueryable();

            if (!string.IsNullOrEmpty(userName))
            {
                query = query.Where(l => l.UserName.Contains(userName));
            }

            if (isSuccess.HasValue)
            {
                query = query.Where(l => l.IsSuccess == isSuccess.Value);
            }

            var totalRecords = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

            var logs = await query
                .OrderByDescending(l => l.LoginTime)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var logDtos = logs.Select(l => new LoginLogDto
            {
                Id = l.Id,
                UserId = l.UserId,
                UserName = l.UserName,
                LoginTime = l.LoginTime,
                IPAddress = l.IPAddress,
                IsSuccess = l.IsSuccess,
                FailureReason = l.FailureReason
            });

            var pagedResult = new PagedResult<LoginLogDto>
            {
                Items = logDtos,
                TotalRecords = totalRecords,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = totalPages
            };

            return Ok(pagedResult);
        }

        /// <summary>
        /// 获取指定 ID 的登录日志。
        /// </summary>
        /// <param name="id">登录日志的唯一标识。</param>
        /// <returns>单个登录日志详情。</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<LoginLogDto>> GetLoginLog(int id)
        {
            var log = await _context.LoginLogs
                .Include(l => l.User)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (log == null)
            {
                return NotFound();
            }

            var logDto = new LoginLogDto
            {
                Id = log.Id,
                UserId = log.UserId,
                UserName = log.UserName,
                LoginTime = log.LoginTime,
                IPAddress = log.IPAddress,
                IsSuccess = log.IsSuccess,
                FailureReason = log.FailureReason
            };

            return Ok(logDto);
        }
    }
}
