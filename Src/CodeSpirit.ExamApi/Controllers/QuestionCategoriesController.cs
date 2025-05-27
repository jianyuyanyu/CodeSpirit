using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Dtos;
using CodeSpirit.ExamApi.Dtos.QuestionCategory;
using CodeSpirit.ExamApi.Services.Interfaces;
using CodeSpirit.Shared.Dtos.Common;
using Microsoft.AspNetCore.Mvc;

namespace CodeSpirit.ExamApi.Controllers;

/// <summary>
/// 题目分类管理控制器
/// </summary>
[DisplayName("题目分类管理")]
[Navigation(Icon = "fa-solid fa-folder-tree")]
public class QuestionCategoriesController : ApiControllerBase
{
    private readonly IQuestionCategoryService _questionCategoryService;
    private readonly ILogger<QuestionCategoriesController> _logger;

    /// <summary>
    /// 初始化题目分类管理控制器
    /// </summary>
    /// <param name="questionCategoryService">题目分类服务</param>
    /// <param name="logger">日志记录器</param>
    public QuestionCategoriesController(
        IQuestionCategoryService questionCategoryService,
        ILogger<QuestionCategoriesController> logger)
    {
        ArgumentNullException.ThrowIfNull(questionCategoryService);
        ArgumentNullException.ThrowIfNull(logger);

        _questionCategoryService = questionCategoryService;
        _logger = logger;
    }

    /// <summary>
    /// 获取题目分类列表（优化为非分页模式，支持树形展示）
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>题目分类列表结果</returns>
    [HttpGet]
    [DisplayName("获取题目分类列表")]
    public async Task<ActionResult<ApiResponse<PageList<QuestionCategoryDto>>>> GetQuestionCategories([FromQuery] QuestionCategoryQueryDto queryDto)
    {
        // 获取所有分类数据
        List<QuestionCategoryDto> allCategories = await _questionCategoryService.GetAllCategoriesAsync();
        
        // 如果有查询关键字，进行过滤
        List<QuestionCategoryDto> filteredCategories = allCategories;
        if (!string.IsNullOrEmpty(queryDto.Keywords))
        {
            filteredCategories = allCategories.Where(c => 
                c.Name.Contains(queryDto.Keywords, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrEmpty(c.Description) && c.Description.Contains(queryDto.Keywords, StringComparison.OrdinalIgnoreCase))
            ).ToList();
        }

        // 构建树形结构
        List<QuestionCategoryDto> treeCategories = BuildCategoryTree(filteredCategories);

        // 创建非分页的PageList，这样Amis会自动使用树形展示
        PageList<QuestionCategoryDto> listData = new(treeCategories, treeCategories.Count);

        return SuccessResponse(listData);
    }

    /// <summary>
    /// 获取所有题目分类
    /// </summary>
    /// <returns>所有题目分类列表</returns>
    [HttpGet("all")]
    [DisplayName("获取所有题目分类")]
    public async Task<ActionResult<ApiResponse<List<QuestionCategoryDto>>>> GetAllCategories()
    {
        List<QuestionCategoryDto> categories = await _questionCategoryService.GetAllCategoriesAsync();
        return SuccessResponse(categories);
    }

    /// <summary>
    /// 获取题目分类树形结构
    /// </summary>
    /// <returns>树形结构的题目分类列表</returns>
    [HttpGet("tree")]
    [DisplayName("获取题目分类树")]
    public async Task<ActionResult<ApiResponse<List<QuestionCategoryTreeDto>>>> GetCategoryTree()
    {
        List<QuestionCategoryTreeDto> categoryTree = await _questionCategoryService.GetCategoryTreeAsync();
        return SuccessResponse(categoryTree);
    }

    /// <summary>
    /// 获取题目分类详情
    /// </summary>
    /// <param name="id">题目分类ID</param>
    /// <returns>题目分类详细信息</returns>
    [HttpGet("{id:long}")]
    [DisplayName("获取题目分类详情")]
    public async Task<ActionResult<ApiResponse<QuestionCategoryDto>>> GetQuestionCategory(long id)
    {
        QuestionCategoryDto category = await _questionCategoryService.GetAsync(id);
        return SuccessResponse(category);
    }

    /// <summary>
    /// 创建题目分类
    /// </summary>
    /// <param name="createDto">创建题目分类请求数据</param>
    /// <returns>创建的题目分类信息</returns>
    [HttpPost]
    [DisplayName("创建题目分类")]
    public async Task<ActionResult<ApiResponse<QuestionCategoryDto>>> CreateQuestionCategory(CreateQuestionCategoryDto createDto)
    {
        ArgumentNullException.ThrowIfNull(createDto);
        QuestionCategoryDto categoryDto = await _questionCategoryService.CreateAsync(createDto);
        return SuccessResponse(categoryDto);
    }

    /// <summary>
    /// 更新题目分类
    /// </summary>
    /// <param name="id">题目分类ID</param>
    /// <param name="updateDto">更新题目分类请求数据</param>
    /// <returns>更新操作结果</returns>
    [HttpPut("{id:long}")]
    [DisplayName("更新题目分类")]
    public async Task<ActionResult<ApiResponse>> UpdateQuestionCategory(long id, UpdateQuestionCategoryDto updateDto)
    {
        await _questionCategoryService.UpdateAsync(id, updateDto);
        return SuccessResponse();
    }

    /// <summary>
    /// 删除题目分类
    /// </summary>
    /// <param name="id">题目分类ID</param>
    /// <returns>删除操作结果</returns>
    [HttpDelete("{id:long}")]
    [Operation("删除", "ajax", null, "确定要删除此题目分类吗？")]
    [DisplayName("删除题目分类")]
    public async Task<ActionResult<ApiResponse>> DeleteQuestionCategory(long id)
    {
        await _questionCategoryService.DeleteAsync(id);
        return SuccessResponse();
    }

    /// <summary>
    /// 批量删除题目分类
    /// </summary>
    /// <param name="ids">题目分类ID列表</param>
    /// <returns>批量删除操作结果</returns>
    [HttpPost("batch-delete")]
    [Operation("批量删除", "ajax", null, "确定要批量删除选中的题目分类吗？", isBulkOperation: true)]
    [DisplayName("批量删除题目分类")]
    public async Task<ActionResult<ApiResponse>> BatchDeleteQuestionCategories([FromBody] BatchOperationDto<long> request)
    {
        ArgumentNullException.ThrowIfNull(request);
        (int successCount, List<long> failedIds) = await _questionCategoryService.BatchDeleteAsync(request.Ids);
        
        return failedIds.Any()
            ? SuccessResponse($"成功删除 {successCount} 个题目分类，但以下题目分类删除失败: {string.Join(", ", failedIds)}")
            : SuccessResponse($"成功删除 {successCount} 个题目分类！");
    }

    /// <summary>
    /// 构建分类树形结构
    /// </summary>
    /// <param name="categories">分类列表</param>
    /// <returns>树形结构的分类列表</returns>
    private static List<QuestionCategoryDto> BuildCategoryTree(List<QuestionCategoryDto> categories)
    {
        // 创建字典以便快速查找
        var categoryDict = categories.ToDictionary(c => c.Id, c => c);

        // 初始化所有分类的Children列表
        foreach (var category in categories)
        {
            category.Children = [];
        }

        // 构建父子关系
        foreach (var category in categories)
        {
            if (category.ParentId.HasValue && categoryDict.TryGetValue(category.ParentId.Value, out var parent))
            {
                parent.Children.Add(category);
            }
        }

        // 返回根节点（没有父级的分类）
        return categories.Where(c => !c.ParentId.HasValue).ToList();
    }
} 