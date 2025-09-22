using CodeSpirit.ApprovalApi.Dtos.WorkflowCategory;
using CodeSpirit.ApprovalApi.Services;
using CodeSpirit.Core;
using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Dtos;
using CodeSpirit.Core.Enums;
using CodeSpirit.Shared.Dtos.Common;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace CodeSpirit.ApprovalApi.Controllers;

/// <summary>
/// 流程分类管理控制器
/// </summary>
[DisplayName("流程分类管理")]
[Navigation(Icon = "fa-solid fa-folder-tree", PlatformType = PlatformType.Tenant)]
public class WorkflowCategoriesController : ApiControllerBase
{
    private readonly IWorkflowCategoryService _workflowCategoryService;
    private readonly ILogger<WorkflowCategoriesController> _logger;

    /// <summary>
    /// 初始化流程分类控制器
    /// </summary>
    /// <param name="workflowCategoryService">流程分类服务</param>
    /// <param name="logger">日志记录器</param>
    public WorkflowCategoriesController(
        IWorkflowCategoryService workflowCategoryService,
        ILogger<WorkflowCategoriesController> logger)
    {
        ArgumentNullException.ThrowIfNull(workflowCategoryService);
        ArgumentNullException.ThrowIfNull(logger);

        _workflowCategoryService = workflowCategoryService;
        _logger = logger;
    }

    /// <summary>
    /// 获取流程分类列表（优化为非分页模式，支持树形展示）
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>流程分类列表结果</returns>
    [HttpGet]
    [DisplayName("获取流程分类列表")]
    public async Task<ActionResult<ApiResponse<PageList<WorkflowCategoryDto>>>> GetCategories([FromQuery] WorkflowCategoryQueryDto queryDto)
    {
        // 调用服务层方法获取树形结构的分类数据
        List<WorkflowCategoryDto> treeCategories = await _workflowCategoryService.GetCategoriesWithTreeAsync(queryDto);

        // 创建非分页的PageList，这样Amis会自动使用树形展示
        PageList<WorkflowCategoryDto> listData = new(treeCategories, treeCategories.Count);

        return SuccessResponse(listData);
    }

    /// <summary>
    /// 获取所有流程分类
    /// </summary>
    /// <returns>所有流程分类列表</returns>
    [HttpGet("all")]
    [DisplayName("获取所有流程分类")]
    public async Task<ActionResult<ApiResponse<List<WorkflowCategoryDto>>>> GetAllCategories()
    {
        List<WorkflowCategoryDto> categories = await _workflowCategoryService.GetAllCategoriesAsync();
        return SuccessResponse(categories);
    }

    /// <summary>
    /// 根据ID获取流程分类
    /// </summary>
    /// <param name="id">分类ID</param>
    /// <returns>分类信息</returns>
    [HttpGet("{id:int}")]
    [DisplayName("获取分类详情")]
    public async Task<ActionResult<ApiResponse<WorkflowCategoryDto>>> GetCategory(int id)
    {
        var result = await _workflowCategoryService.GetAsync(id);
        if (result == null)
        {
            return NotFound("分类不存在");
        }
        return SuccessResponse(result);
    }

    /// <summary>
    /// 创建流程分类
    /// </summary>
    /// <param name="createDto">创建分类信息</param>
    /// <returns>创建结果</returns>
    [HttpPost]
    [DisplayName("创建分类")]
    public async Task<ActionResult<ApiResponse<WorkflowCategoryDto>>> CreateCategory([FromBody] CreateWorkflowCategoryDto createDto)
    {
        var result = await _workflowCategoryService.CreateAsync(createDto);
        return SuccessResponse(result);
    }

    /// <summary>
    /// 更新流程分类
    /// </summary>
    /// <param name="id">分类ID</param>
    /// <param name="updateDto">更新分类信息</param>
    /// <returns>更新结果</returns>
    [HttpPut("{id}")]
    [DisplayName("更新分类")]
    public async Task<ActionResult<ApiResponse<WorkflowCategoryDto>>> UpdateCategory(int id, [FromBody] UpdateWorkflowCategoryDto updateDto)
    {
        await _workflowCategoryService.UpdateAsync(id, updateDto);
        var result = await _workflowCategoryService.GetAsync(id);
        if (result == null)
        {
            return NotFound("分类不存在");
        }
        return SuccessResponse(result);
    }

    /// <summary>
    /// 删除流程分类
    /// </summary>
    /// <param name="id">分类ID</param>
    /// <returns>删除结果</returns>
    [HttpDelete("{id:int}")]
    [Operation("删除", "ajax", null, "确定要删除此流程分类吗？")]
    [DisplayName("删除分类")]
    public async Task<ActionResult<ApiResponse>> DeleteCategory(int id)
    {
        var canDelete = await _workflowCategoryService.CanDeleteAsync(id);
        if (!canDelete)
        {
            return BadRequest("该分类下还有子分类或流程定义，无法删除");
        }

        await _workflowCategoryService.DeleteAsync(id);
        return SuccessResponse();
    }

    /// <summary>
    /// 批量删除流程分类
    /// </summary>
    /// <param name="request">批量删除请求</param>
    /// <returns>批量删除操作结果</returns>
    [HttpPost("batch-delete")]
    [Operation("批量删除", "ajax", null, "确定要批量删除选中的流程分类吗？", isBulkOperation: true)]
    [DisplayName("批量删除流程分类")]
    public async Task<ActionResult<ApiResponse>> BatchDeleteCategories([FromBody] BatchOperationDto<int> request)
    {
        ArgumentNullException.ThrowIfNull(request);

        int successCount = 0;
        List<int> failedIds = new();

        foreach (var id in request.Ids)
        {
            try
            {
                var canDelete = await _workflowCategoryService.CanDeleteAsync(id);
                if (canDelete)
                {
                    await _workflowCategoryService.DeleteAsync(id);
                    successCount++;
                }
                else
                {
                    failedIds.Add(id);
                }
            }
            catch
            {
                failedIds.Add(id);
            }
        }

        return failedIds.Any()
            ? SuccessResponse($"成功删除 {successCount} 个流程分类，但以下分类删除失败: {string.Join(", ", failedIds)}")
            : SuccessResponse($"成功删除 {successCount} 个流程分类！");
    }

    /// <summary>
    /// 获取分类树形结构
    /// </summary>
    /// <param name="parentId">父级分类ID</param>
    /// <returns>分类树</returns>
    [HttpGet("tree")]
    [DisplayName("获取分类树")]
    [Permission(AllowInheritedPermissions = new[] { "approval_workflow_definitions" })]
    public async Task<ActionResult<ApiResponse<List<WorkflowCategoryDto>>>> GetCategoryTree([FromQuery] int? parentId = null)
    {
        var result = await _workflowCategoryService.GetCategoryTreeAsync(parentId);
        return SuccessResponse(result);
    }

    /// <summary>
    /// 获取启用的分类列表
    /// </summary>
    /// <returns>启用的分类列表</returns>
    [HttpGet("enabled")]
    [DisplayName("获取启用分类")]
    [Permission(AllowInheritedPermissions = new[] { "approval_workflow_definitions" })]
    public async Task<ActionResult<ApiResponse<List<WorkflowCategoryDto>>>> GetEnabledCategories()
    {
        var result = await _workflowCategoryService.GetEnabledCategoriesAsync();
        return SuccessResponse(result);
    }

    /// <summary>
    /// 移动分类到指定父级
    /// </summary>
    /// <param name="request">移动请求</param>
    /// <returns>移动结果</returns>
    [HttpPut("move")]
    [Operation("移动分类", "form", null, "确定要移动该分类吗？")]
    [DisplayName("移动分类")]
    public async Task<ActionResult<ApiResponse>> MoveCategory([FromBody] MoveCategoryRequest request)
    {
        var success = await _workflowCategoryService.MoveCategoryAsync(request.Id, request.NewParentId);
        if (!success)
        {
            return BadRequest("移动失败，请检查分类是否存在或是否会形成循环引用");
        }
        return SuccessResponse();
    }

    /// <summary>
    /// 批量更新分类排序
    /// </summary>
    /// <param name="request">排序请求</param>
    /// <returns>更新结果</returns>
    [HttpPut("orders")]
    [Operation("更新排序", "form", null, "确定要更新分类排序吗？")]
    [DisplayName("更新排序")]
    public async Task<ActionResult<ApiResponse>> UpdateOrders([FromBody] UpdateOrdersRequest request)
    {
        var success = await _workflowCategoryService.UpdateOrdersAsync(request.CategoryOrders);
        if (!success)
        {
            return BadRequest("更新排序失败");
        }
        return SuccessResponse();
    }
}

/// <summary>
/// 移动分类请求
/// </summary>
public class MoveCategoryRequest
{
    /// <summary>
    /// 分类ID
    /// </summary>
    [DisplayName("分类ID")]
    public int Id { get; set; }

    /// <summary>
    /// 新的父级分类ID
    /// </summary>
    [DisplayName("新父级分类")]
    public int? NewParentId { get; set; }
}

/// <summary>
/// 更新排序请求
/// </summary>
public class UpdateOrdersRequest
{
    /// <summary>
    /// 分类排序信息（分类ID -> 排序值）
    /// </summary>
    [DisplayName("排序信息")]
    public Dictionary<int, int> CategoryOrders { get; set; } = new();
}
