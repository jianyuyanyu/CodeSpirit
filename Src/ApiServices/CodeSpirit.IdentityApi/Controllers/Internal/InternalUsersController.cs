using CodeSpirit.Core;
using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Dtos;
using CodeSpirit.IdentityApi.Dtos.User;
using CodeSpirit.IdentityApi.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace CodeSpirit.IdentityApi.Controllers.Internal
{
    /// <summary>
    /// 内部用户信息访问控制器
    /// </summary>
    [DisplayName("内部用户信息")]
    [Module("default")]
    [Route("api/identity/internal/users")]
    public class InternalUsersController : ControllerBase
    {
        private readonly IUserService _userService;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="userService">用户服务</param>
        public InternalUsersController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// 获取多个用户信息
        /// </summary>
        /// <param name="queryDto">查询参数</param>
        /// <returns>用户列表</returns>
        [HttpGet]
        [DisplayName("获取内部用户列表")]
        public async Task<ActionResult<PageList<UserDto>>> GetInternalUsers([FromQuery] UserQueryDto queryDto)
        {
            PageList<UserDto> users = await _userService.GetUsersAsync(queryDto);
            return Ok(users);
        }

        /// <summary>
        /// 获取单个用户信息
        /// </summary>
        /// <param name="id">用户ID</param>
        /// <returns>用户详情</returns>
        [HttpGet("{id}")]
        [DisplayName("获取内部用户详情")]
        public async Task<ActionResult<UserDto>> GetInternalUser(long id)
        {
            UserDto userDto = await _userService.GetAsync(id);
            return Ok(userDto);
        }

        /// <summary>
        /// 根据用户名获取用户信息
        /// </summary>
        /// <param name="username">用户名</param>
        /// <returns>用户详情</returns>
        [HttpGet("by-username/{username}")]
        [DisplayName("根据用户名获取用户信息")]
        public async Task<ActionResult<UserDto>> GetUserByUsername(string username)
        {
            // 查找指定用户名的用户
            UserQueryDto queryDto = new UserQueryDto
            {
                Keywords = username,
                Page = 1,
                PerPage = 1
            };

            var result = await _userService.GetUsersAsync(queryDto);
            var userDto = result.Items.FirstOrDefault();

            if (userDto == null)
            {
                return NotFound(new ApiResponse(404, "用户不存在"));
            }

            return Ok(userDto);
        }

        /// <summary>
        /// 批量获取用户信息
        /// </summary>
        /// <param name="ids">用户ID列表</param>
        /// <returns>用户列表</returns>
        [HttpPost("batch")]
        [DisplayName("批量获取用户信息")]
        public async Task<ActionResult<List<UserDto>>> GetUsersByIds([FromBody] List<long> ids)
        {
            List<UserDto> users = new List<UserDto>();

            foreach (var id in ids)
            {
                try
                {
                    var user = await _userService.GetAsync(id);
                    if (user != null)
                    {
                        users.Add(user);
                    }
                }
                catch (Exception)
                {
                    // 忽略获取失败的用户
                    continue;
                }
            }

            return Ok(users);
        }
    }
}