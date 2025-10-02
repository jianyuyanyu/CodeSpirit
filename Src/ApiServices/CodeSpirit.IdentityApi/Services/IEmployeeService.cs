using CodeSpirit.Core;
using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.IdentityApi.Dtos.Employee;
using CodeSpirit.Shared.Services;

namespace CodeSpirit.IdentityApi.Services;

/// <summary>
/// 职工服务接口
/// </summary>
public interface IEmployeeService : IBaseCRUDIService<Employee, EmployeeDto, long, CreateEmployeeDto, UpdateEmployeeDto, EmployeeBatchImportItemDto>, IScopedDependency
{
    /// <summary>
    /// 获取职工列表（分页）
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>职工分页列表</returns>
    Task<PageList<EmployeeDto>> GetEmployeesAsync(EmployeeQueryDto queryDto);

    /// <summary>
    /// 根据部门获取职工列表
    /// </summary>
    /// <param name="departmentId">部门ID</param>
    /// <param name="includeSubDepartments">是否包含子部门</param>
    /// <returns>职工列表</returns>
    Task<List<EmployeeDto>> GetEmployeesByDepartmentAsync(long departmentId, bool includeSubDepartments = false);

    /// <summary>
    /// 设置职工激活状态
    /// </summary>
    /// <param name="id">职工ID</param>
    /// <param name="isActive">是否激活</param>
    Task SetActiveStatusAsync(long id, bool isActive);

    /// <summary>
    /// 转移职工到新部门
    /// </summary>
    /// <param name="employeeId">职工ID</param>
    /// <param name="newDepartmentId">新部门ID</param>
    Task TransferEmployeeAsync(long employeeId, long? newDepartmentId);

    /// <summary>
    /// 办理职工离职
    /// </summary>
    /// <param name="employeeId">职工ID</param>
    /// <param name="terminationDate">离职日期</param>
    Task TerminateEmployeeAsync(long employeeId, DateTime terminationDate);

    /// <summary>
    /// 验证工号是否唯一
    /// </summary>
    /// <param name="employeeNo">工号</param>
    /// <param name="excludeId">排除的职工ID（用于更新时的验证）</param>
    /// <returns>是否唯一</returns>
    Task<bool> IsEmployeeNoUniqueAsync(string employeeNo, long? excludeId = null);

    /// <summary>
    /// 关联职工与用户账号
    /// </summary>
    /// <param name="employeeId">职工ID</param>
    /// <param name="userId">用户ID</param>
    Task LinkEmployeeToUserAsync(long employeeId, long userId);

    /// <summary>
    /// 取消职工与用户账号的关联
    /// </summary>
    /// <param name="employeeId">职工ID</param>
    Task UnlinkEmployeeFromUserAsync(long employeeId);
}

