using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.IdentityApi.Data.Models;

/// <summary>
/// 在职状态枚举
/// </summary>
public enum EmploymentStatus
{
    /// <summary>
    /// 在职
    /// </summary>
    [Display(Name = "在职")]
    Active = 1,

    /// <summary>
    /// 试用期
    /// </summary>
    [Display(Name = "试用期")]
    Probation = 2,

    /// <summary>
    /// 离职
    /// </summary>
    [Display(Name = "离职")]
    Terminated = 3,

    /// <summary>
    /// 停职
    /// </summary>
    [Display(Name = "停职")]
    Suspended = 4,

    /// <summary>
    /// 退休
    /// </summary>
    [Display(Name = "退休")]
    Retired = 5
}

