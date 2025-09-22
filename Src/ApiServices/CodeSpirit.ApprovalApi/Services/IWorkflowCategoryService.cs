using CodeSpirit.ApprovalApi.Dtos.WorkflowCategory;
using CodeSpirit.ApprovalApi.Models;
using CodeSpirit.Shared.Services;

namespace CodeSpirit.ApprovalApi.Services;

/// <summary>
/// 流程分类服务接口
/// </summary>
public interface IWorkflowCategoryService : IBaseCRUDService<WorkflowCategory, WorkflowCategoryDto, int, CreateWorkflowCategoryDto, UpdateWorkflowCategoryDto>
{
    /// <summary>
    /// 获取所有分类列表
    /// </summary>
    /// <returns>所有分类列表</returns>
    Task<List<WorkflowCategoryDto>> GetAllCategoriesAsync();

    /// <summary>
    /// 根据查询条件获取分类列表（支持树形结构）
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>分类列表（树形结构）</returns>
    Task<List<WorkflowCategoryDto>> GetCategoriesWithTreeAsync(WorkflowCategoryQueryDto queryDto);

    /// <summary>
    /// 获取分类树形结构
    /// </summary>
    /// <param name="parentId">父级分类ID，null表示获取所有顶级分类</param>
    /// <returns>分类树形结构</returns>
    Task<List<WorkflowCategoryDto>> GetCategoryTreeAsync(int? parentId = null);

    /// <summary>
    /// 获取启用的分类列表
    /// </summary>
    /// <returns>启用的分类列表</returns>
    Task<List<WorkflowCategoryDto>> GetEnabledCategoriesAsync();

    /// <summary>
    /// 检查分类是否可以删除
    /// </summary>
    /// <param name="id">分类ID</param>
    /// <returns>是否可以删除</returns>
    Task<bool> CanDeleteAsync(int id);

    /// <summary>
    /// 移动分类到指定父级
    /// </summary>
    /// <param name="id">分类ID</param>
    /// <param name="newParentId">新的父级分类ID</param>
    /// <returns>操作结果</returns>
    Task<bool> MoveCategoryAsync(int id, int? newParentId);

    /// <summary>
    /// 批量更新分类排序
    /// </summary>
    /// <param name="categoryOrders">分类排序信息</param>
    /// <returns>操作结果</returns>
    Task<bool> UpdateOrdersAsync(Dictionary<int, int> categoryOrders);
}
