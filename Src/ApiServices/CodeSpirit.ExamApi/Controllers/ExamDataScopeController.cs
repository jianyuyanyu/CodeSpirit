using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Enums;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace CodeSpirit.ExamApi.Controllers;

/// <summary>
/// 考试数据可见性权限定义控制器
/// </summary>
/// <remarks>
/// 用于在权限树中注册 exam_view_all 权限，供角色分配使用。
/// 该控制器不提供实际业务接口，仅用于权限扫描。
/// </remarks>
[Permission(Name = "view", DisplayName = "数据可见性", Description = "考试数据可见性相关权限")]
[Navigation(Hidden = true)]
public class ExamDataScopeController : ApiControllerBase
{
    /// <summary>
    /// 查看全部考试数据权限定义
    /// </summary>
    /// <remarks>
    /// 拥有此权限的用户可查看所有用户创建的题目、试卷、考试、考生组、考生及考试记录。
    /// Admin 角色默认拥有此能力，无需分配此权限。
    /// </remarks>
    [HttpGet("view-all")]
    [DisplayName("查看全部考试数据")]
    [Permission(Name = "all", DisplayName = "查看全部考试数据", Description = "可查看所有用户创建的考试相关数据")]
    public ActionResult ViewAllPermissionDefinition()
    {
        // 占位接口，仅用于权限树注册，不提供实际功能
        return Ok();
    }
}
