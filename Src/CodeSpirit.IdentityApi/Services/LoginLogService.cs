using CodeSpirit.Core;
using CodeSpirit.IdentityApi.Controllers.Dtos;
using CodeSpirit.IdentityApi.Repositories;
using System.Threading.Tasks;

namespace CodeSpirit.IdentityApi.Services
{
    /// <summary>
    /// 登录日志服务类
    /// </summary>
    public class LoginLogService : ILoginLogService
    {
        private readonly ILoginLogRepository _loginLogRepository;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="loginLogRepository">登录日志仓储接口</param>
        public LoginLogService(ILoginLogRepository loginLogRepository)
        {
            _loginLogRepository = loginLogRepository;
        }

        /// <summary>
        /// 获取分页的登录日志列表
        /// </summary>
        /// <param name="queryDto">查询参数DTO</param>
        /// <returns>登录日志列表数据</returns>
        public async Task<ListData<LoginLogDto>> GetPagedLoginLogsAsync(LoginLogsQueryDto queryDto)
        {
            (List<Data.Models.LoginLog> logs, int totalPages) = await _loginLogRepository.GetPagedLoginLogsAsync(
                queryDto.Keywords,
                queryDto.UserName,
                queryDto.IsSuccess,
                queryDto.Page,
                queryDto.PerPage);

            List<LoginLogDto> logDtos = logs.Select(l => new LoginLogDto
            {
                Id = l.Id,
                UserId = l.UserId,
                UserName = l.UserName,
                LoginTime = l.LoginTime,
                IPAddress = l.IPAddress,
                IsSuccess = l.IsSuccess,
                FailureReason = l.FailureReason
            }).ToList();

            return new ListData<LoginLogDto>
            {
                Items = logDtos,
                Total = totalPages
            };
        }

        /// <summary>
        /// 根据ID获取登录日志详情
        /// </summary>
        /// <param name="id">登录日志ID</param>
        /// <returns>登录日志DTO，如果未找到则返回null</returns>
        public async Task<LoginLogDto> GetLoginLogByIdAsync(int id)
        {
            Data.Models.LoginLog log = await _loginLogRepository.GetByIdAsync(id);
            return log == null
                ? null
                : new LoginLogDto
                {
                    Id = log.Id,
                    UserId = log.UserId,
                    UserName = log.UserName,
                    LoginTime = log.LoginTime,
                    IPAddress = log.IPAddress,
                    IsSuccess = log.IsSuccess,
                    FailureReason = log.FailureReason
                };
        }
    }
}