using CodeSpirit.Core;
using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.IdentityApi.Dtos.Department;
using CodeSpirit.Shared.Services;

namespace CodeSpirit.IdentityApi.Services;

/// <summary>
/// 部门服务接口
/// </summary>
public interface IDepartmentService : IBaseCRUDIService<Department, DepartmentDto, long, CreateDepartmentDto, UpdateDepartmentDto, DepartmentBatchImportItemDto>, IScopedDependency
{
    /// <summary>
    /// 获取部门列表（分页）
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>部门分页列表</returns>
    Task<PageList<DepartmentDto>> GetDepartmentsAsync(DepartmentQueryDto queryDto);

    /// <summary>
    /// 获取部门树形结构（支持查询条件）
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>部门树形结构列表</returns>
    Task<List<DepartmentDto>> GetDepartmentsWithTreeAsync(DepartmentQueryDto queryDto);

    /// <summary>
    /// 获取部门树形结构
    /// </summary>
    /// <param name="parentId">父部门ID，null表示获取根节点</param>
    /// <returns>部门树形结构列表</returns>
    Task<List<DepartmentDto>> GetDepartmentTreeAsync(long? parentId = null);

    /// <summary>
    /// 获取部门的所有子部门（递归）
    /// </summary>
    /// <param name="departmentId">部门ID</param>
    /// <returns>子部门列表</returns>
    Task<List<DepartmentDto>> GetChildDepartmentsAsync(long departmentId);

    /// <summary>
    /// 设置部门激活状态
    /// </summary>
    /// <param name="id">部门ID</param>
    /// <param name="isActive">是否激活</param>
    Task SetActiveStatusAsync(long id, bool isActive);

    /// <summary>
    /// 移动部门到新的父部门
    /// </summary>
    /// <param name="departmentId">部门ID</param>
    /// <param name="newParentId">新父部门ID</param>
    Task MoveDepartmentAsync(long departmentId, long? newParentId);

    /// <summary>
    /// 验证部门编码是否唯一
    /// </summary>
    /// <param name="code">部门编码</param>
    /// <param name="excludeId">排除的部门ID（用于更新时的验证）</param>
    /// <returns>是否唯一</returns>
    Task<bool> IsDepartmentCodeUniqueAsync(string code, long? excludeId = null);

    /// <summary>
    /// 批量创建组织结构（基于AI生成的数据）
    /// </summary>
    /// <param name="departments">部门列表</param>
    /// <returns>创建成功的部门数量</returns>
    Task<int> CreateOrganizationStructureAsync(List<GeneratedDepartmentItemDto> departments);
}

