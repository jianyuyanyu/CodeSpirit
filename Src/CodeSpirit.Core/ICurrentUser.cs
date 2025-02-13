using CodeSpirit.Core.DependencyInjection;
using System.Collections.Generic;
using System.Security.Claims;

namespace CodeSpirit.Core
{
    /// <summary>
    /// 当前用户接口，定义获取当前用户信息的基本操作
    /// </summary>
    public interface ICurrentUser
    {
        /// <summary>
        /// 获取用户ID
        /// </summary>
        long? Id { get; }

        /// <summary>
        /// 获取用户名
        /// </summary>
        string UserName { get; }

        /// <summary>
        /// 获取用户角色列表
        /// </summary>
        string[] Roles { get; }

        /// <summary>
        /// 判断用户是否已认证
        /// </summary>
        bool IsAuthenticated { get; }

        /// <summary>
        /// 获取用户的所有声明（Claims）
        /// </summary>
        IEnumerable<Claim> Claims { get; }

        /// <summary>
        /// 判断用户是否属于指定角色
        /// </summary>
        /// <param name="role">角色名称</param>
        /// <returns>如果用户属于该角色返回true，否则返回false</returns>
        bool IsInRole(string role);
    }
} 