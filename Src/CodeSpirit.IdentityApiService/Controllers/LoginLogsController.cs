// Controllers/AuthController.cs
using CodeSpirit.Core;
using CodeSpirit.IdentityApi.Controllers.Dtos;
using CodeSpirit.IdentityApi.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.Data;

namespace CodeSpirit.IdentityApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Page(Label = "登录日志", ParentLabel = "用户中心", Icon = "fa-solid fa-info")]
    [DisplayName("登录日志")]
    public partial class LoginLogsController : ApiControllerBase
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
        public async Task<ActionResult<ApiResponse<ListData<LoginLogDto>>>> GetLoginLogs([FromQuery]LoginLogsQueryDto queryDto)
        {
            IQueryable<Data.Models.LoginLog> query = _context.LoginLogs
                .Include(l => l.User)
                .AsQueryable();

            // 应用关键词过滤
            if (!string.IsNullOrWhiteSpace(queryDto.Keywords))
            {
                string searchLower = queryDto.Keywords.ToLower();
                query = query.Where(u =>
                    u.UserName.ToLower().Contains(searchLower) ||
                    u.User.PhoneNumber.ToLower().Contains(searchLower) ||
                    u.IPAddress.ToLower().Contains(searchLower));
            }

            if (!string.IsNullOrEmpty(queryDto.UserName))
            {
                query = query.Where(l => l.UserName.Contains(queryDto.UserName));
            }

            if (queryDto.IsSuccess.HasValue)
            {
                query = query.Where(l => l.IsSuccess == queryDto.IsSuccess.Value);
            }

            int totalRecords = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalRecords / (double)queryDto.PerPage);

            List<Data.Models.LoginLog> logs = await query
                .OrderByDescending(l => l.LoginTime)
                .Skip((queryDto.Page - 1) * queryDto.PerPage)
                .Take(queryDto.PerPage)
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
            }).ToList();

            return SuccessResponse(new ListData<LoginLogDto>
            {
                Items = logDtos,
                Total = totalPages
            });
        }

        /// <summary>
        /// 获取指定 ID 的登录日志。
        /// </summary>
        /// <param name="id">登录日志的唯一标识。</param>
        /// <returns>单个登录日志详情。</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<LoginLogDto>> GetLoginLog(int id)
        {
            Data.Models.LoginLog log = await _context.LoginLogs
                .Include(l => l.User)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (log == null)
            {
                return NotFound();
            }

            LoginLogDto logDto = new LoginLogDto
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
