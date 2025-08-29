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