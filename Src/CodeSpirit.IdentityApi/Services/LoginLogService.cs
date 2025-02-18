using AutoMapper;
using CodeSpirit.Core;
using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.IdentityApi.Dtos.LoginLogs;
using CodeSpirit.Shared.Repositories;
using LinqKit;

namespace CodeSpirit.IdentityApi.Services
{
    /// <summary>
    /// 登录日志服务类
    /// </summary>
    public class LoginLogService : ILoginLogService
    {
        private readonly IRepository<LoginLog> _loginLogRepository;
        private readonly IMapper _mapper;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="loginLogRepository">登录日志仓储接口</param>
        /// <param name="mapper">AutoMapper实例</param>
        public LoginLogService(IRepository<LoginLog> loginLogRepository, IMapper mapper)
        {
            _loginLogRepository = loginLogRepository;
            _mapper = mapper;
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

            return _mapper.Map<PageList<LoginLogDto>>(result);
        }

        /// <summary>
        /// 根据ID获取登录日志详情
        /// </summary>
        /// <param name="id">登录日志ID</param>
        /// <returns>登录日志DTO，如果未找到则返回null</returns>
        public async Task<LoginLogDto> GetLoginLogByIdAsync(int id)
        {
            LoginLog log = await _loginLogRepository.GetByIdAsync(id);
            return log == null
                ? throw new AppServiceException(404, "登录日志不存在")
                : _mapper.Map<LoginLogDto>(log);
        }
    }
}