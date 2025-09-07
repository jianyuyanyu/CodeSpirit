using AutoMapper;
using CodeSpirit.Shared.Repositories;
using CodeSpirit.Shared.Services;
using CodeSpirit.SurveyApi.Data;
using CodeSpirit.SurveyApi.Dtos.SurveyCategory;
using CodeSpirit.SurveyApi.Models;
using CodeSpirit.SurveyApi.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CodeSpirit.SurveyApi.Services.Implementations;

/// <summary>
/// 问卷分类服务实现
/// </summary>
public class SurveyCategoryService : BaseCRUDService<SurveyCategory, SurveyCategoryDto, int, CreateSurveyCategoryDto, UpdateSurveyCategoryDto>, ISurveyCategoryService
{
    private readonly SurveyDbContext _context;

    /// <summary>
    /// 初始化问卷分类服务
    /// </summary>
    /// <param name="repository">仓储接口</param>
    /// <param name="mapper">对象映射器</param>
    /// <param name="context">数据库上下文</param>
    public SurveyCategoryService(
        IRepository<SurveyCategory> repository,
        IMapper mapper,
        SurveyDbContext context) : base(repository, mapper)
    {
        _context = context;
    }

    /// <summary>
    /// 获取所有分类列表
    /// </summary>
    /// <returns>所有分类列表</returns>
    public async Task<List<SurveyCategoryDto>> GetAllCategoriesAsync()
    {
        var categories = await _context.SurveyCategories
            .Include(c => c.Parent)
            .Include(c => c.Surveys)
            .OrderBy(c => c.OrderIndex)
            .ThenBy(c => c.Name)
            .ToListAsync();

        return Mapper.Map<List<SurveyCategoryDto>>(categories);
    }

    /// <summary>
    /// 根据查询条件获取分类列表（支持树形结构）
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>分类列表（树形结构）</returns>
    public async Task<List<SurveyCategoryDto>> GetCategoriesWithTreeAsync(SurveyCategoryQueryDto queryDto)
    {
        // 获取所有分类数据
        var allCategories = await GetAllCategoriesAsync();
        
        // 特殊处理：如果只是查询特定父级下的分类，直接使用现有的树形方法
        if (queryDto.ParentId.HasValue && 
            string.IsNullOrEmpty(queryDto.Keywords) && 
            string.IsNullOrEmpty(queryDto.Name) && 
            !queryDto.IsEnabled.HasValue && 
            queryDto.OnlyTopLevel != true)
        {
            return await GetCategoryTreeAsync(queryDto.ParentId.Value);
        }
        
        // 应用查询条件进行过滤
        var filteredCategories = ApplyQueryFilters(allCategories, queryDto);
        
        // 构建树形结构
        return BuildCategoryTree(filteredCategories);
    }

    /// <summary>
    /// 应用查询条件过滤分类
    /// </summary>
    /// <param name="categories">分类列表</param>
    /// <param name="queryDto">查询条件</param>
    /// <returns>过滤后的分类列表</returns>
    private static List<SurveyCategoryDto> ApplyQueryFilters(List<SurveyCategoryDto> categories, SurveyCategoryQueryDto queryDto)
    {
        var filteredCategories = categories.AsEnumerable();
        
        // 关键字搜索（通用搜索）
        if (!string.IsNullOrEmpty(queryDto.Keywords))
        {
            filteredCategories = filteredCategories.Where(c => 
                c.Name.Contains(queryDto.Keywords, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrEmpty(c.Description) && c.Description.Contains(queryDto.Keywords, StringComparison.OrdinalIgnoreCase))
            );
        }
        
        // 分类名称搜索
        if (!string.IsNullOrEmpty(queryDto.Name))
        {
            filteredCategories = filteredCategories.Where(c => 
                c.Name.Contains(queryDto.Name, StringComparison.OrdinalIgnoreCase)
            );
        }
        
        // 启用状态筛选
        if (queryDto.IsEnabled.HasValue)
        {
            filteredCategories = filteredCategories.Where(c => 
                c.IsEnabled == queryDto.IsEnabled.Value
            );
        }
        
        // 只查询顶级分类
        if (queryDto.OnlyTopLevel == true)
        {
            filteredCategories = filteredCategories.Where(c => !c.ParentId.HasValue);
        }
        
        return filteredCategories.ToList();
    }


    /// <summary>
    /// 构建分类树形结构
    /// </summary>
    /// <param name="categories">分类列表</param>
    /// <returns>树形结构的分类列表</returns>
    private static List<SurveyCategoryDto> BuildCategoryTree(List<SurveyCategoryDto> categories)
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

    /// <summary>
    /// 获取分类树形结构
    /// </summary>
    /// <param name="parentId">父级分类ID，null表示获取所有顶级分类</param>
    /// <returns>分类树形结构</returns>
    public async Task<List<SurveyCategoryDto>> GetCategoryTreeAsync(int? parentId = null)
    {
        var categories = await _context.SurveyCategories
            .Include(c => c.Parent)
            .Include(c => c.Surveys)
            .Where(c => c.ParentId == parentId)
            .OrderBy(c => c.OrderIndex)
            .ThenBy(c => c.Name)
            .ToListAsync();

        return Mapper.Map<List<SurveyCategoryDto>>(categories);
    }

    /// <summary>
    /// 获取启用的分类列表
    /// </summary>
    /// <returns>启用的分类列表</returns>
    public async Task<List<SurveyCategoryDto>> GetEnabledCategoriesAsync()
    {
        var categories = await _context.SurveyCategories
            .Include(c => c.Parent)
            .Include(c => c.Surveys)
            .Where(c => c.IsEnabled)
            .OrderBy(c => c.OrderIndex)
            .ThenBy(c => c.Name)
            .ToListAsync();

        return Mapper.Map<List<SurveyCategoryDto>>(categories);
    }

    /// <summary>
    /// 检查分类是否可以删除
    /// </summary>
    /// <param name="id">分类ID</param>
    /// <returns>是否可以删除</returns>
    public async Task<bool> CanDeleteAsync(int id)
    {
        // 检查是否有子分类
        var hasChildren = await _context.SurveyCategories
            .AnyAsync(c => c.ParentId == id);

        if (hasChildren)
        {
            return false;
        }

        // 检查是否有关联的问卷
        var hasSurveys = await _context.Surveys
            .AnyAsync(s => s.CategoryId == id);

        return !hasSurveys;
    }

    /// <summary>
    /// 移动分类到指定父级
    /// </summary>
    /// <param name="id">分类ID</param>
    /// <param name="newParentId">新的父级分类ID</param>
    /// <returns>操作结果</returns>
    public async Task<bool> MoveCategoryAsync(int id, int? newParentId)
    {
        // 检查是否会形成循环引用
        if (newParentId.HasValue && await WouldCreateCircularReference(id, newParentId.Value))
        {
            return false;
        }

        var entity = await _context.SurveyCategories.FindAsync(id);
        if (entity == null)
        {
            return false;
        }

        entity.ParentId = newParentId;
        entity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// 批量更新分类排序
    /// </summary>
    /// <param name="categoryOrders">分类排序信息</param>
    /// <returns>操作结果</returns>
    public async Task<bool> UpdateOrdersAsync(Dictionary<int, int> categoryOrders)
    {
        var categoryIds = categoryOrders.Keys.ToList();
        var categories = await _context.SurveyCategories
            .Where(c => categoryIds.Contains(c.Id))
            .ToListAsync();

        foreach (var category in categories)
        {
            if (categoryOrders.TryGetValue(category.Id, out var newOrder))
            {
                category.OrderIndex = newOrder;
                category.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// 检查是否会形成循环引用
    /// </summary>
    /// <param name="categoryId">分类ID</param>
    /// <param name="newParentId">新的父级分类ID</param>
    /// <returns>是否会形成循环引用</returns>
    private async Task<bool> WouldCreateCircularReference(int categoryId, int newParentId)
    {
        int? currentParentId = newParentId;
        while (currentParentId.HasValue)
        {
            if (currentParentId == categoryId)
            {
                return true;
            }

            var parent = await _context.SurveyCategories
                .Where(c => c.Id == currentParentId.Value)
                .Select(c => new { c.ParentId })
                .FirstOrDefaultAsync();

            currentParentId = parent?.ParentId;
        }

        return false;
    }
}