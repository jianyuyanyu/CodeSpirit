using AutoMapper;
using CodeSpirit.ApprovalApi.Data;
using CodeSpirit.ApprovalApi.Dtos.WorkflowCategory;
using CodeSpirit.ApprovalApi.Models;
using CodeSpirit.Shared.Repositories;
using CodeSpirit.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace CodeSpirit.ApprovalApi.Services;

/// <summary>
/// 流程分类服务实现
/// </summary>
public class WorkflowCategoryService : BaseCRUDService<WorkflowCategory, WorkflowCategoryDto, int, CreateWorkflowCategoryDto, UpdateWorkflowCategoryDto>, IWorkflowCategoryService
{
    private readonly ApprovalDbContext _context;

    /// <summary>
    /// 初始化流程分类服务
    /// </summary>
    /// <param name="repository">仓储接口</param>
    /// <param name="mapper">对象映射器</param>
    /// <param name="context">数据库上下文</param>
    public WorkflowCategoryService(
        IRepository<WorkflowCategory> repository,
        IMapper mapper,
        ApprovalDbContext context) : base(repository, mapper)
    {
        _context = context;
    }

    /// <summary>
    /// 获取所有分类列表
    /// </summary>
    /// <returns>所有分类列表</returns>
    public async Task<List<WorkflowCategoryDto>> GetAllCategoriesAsync()
    {
        var categories = await _context.WorkflowCategories
            .Include(c => c.Parent)
            .Include(c => c.WorkflowDefinitions)
            .OrderBy(c => c.OrderIndex)
            .ThenBy(c => c.Name)
            .ToListAsync();

        return Mapper.Map<List<WorkflowCategoryDto>>(categories);
    }

    /// <summary>
    /// 根据查询条件获取分类列表（支持树形结构）
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>分类列表（树形结构）</returns>
    public async Task<List<WorkflowCategoryDto>> GetCategoriesWithTreeAsync(WorkflowCategoryQueryDto queryDto)
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
    private static List<WorkflowCategoryDto> ApplyQueryFilters(List<WorkflowCategoryDto> categories, WorkflowCategoryQueryDto queryDto)
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
        
        // 启用状态过滤
        if (queryDto.IsEnabled.HasValue)
        {
            filteredCategories = filteredCategories.Where(c => c.IsEnabled == queryDto.IsEnabled.Value);
        }
        
        // 父级分类过滤
        if (queryDto.ParentId.HasValue)
        {
            filteredCategories = filteredCategories.Where(c => c.ParentId == queryDto.ParentId.Value);
        }
        
        // 只显示顶级分类
        if (queryDto.OnlyTopLevel == true)
        {
            filteredCategories = filteredCategories.Where(c => c.ParentId == null);
        }
        
        return filteredCategories.ToList();
    }

    /// <summary>
    /// 构建分类树形结构
    /// </summary>
    /// <param name="categories">分类列表</param>
    /// <returns>树形结构的分类列表</returns>
    private static List<WorkflowCategoryDto> BuildCategoryTree(List<WorkflowCategoryDto> categories)
    {
        // 创建分类字典以便快速查找
        var categoryDict = categories.ToDictionary(c => c.Id, c => c);
        
        // 找到所有顶级分类（没有父级的分类）
        var topLevelCategories = categories.Where(c => c.ParentId == null).ToList();
        
        // 为每个分类构建子分类树
        foreach (var category in categories)
        {
            if (category.ParentId.HasValue && categoryDict.ContainsKey(category.ParentId.Value))
            {
                var parent = categoryDict[category.ParentId.Value];
                parent.Children.Add(category);
            }
        }
        
        // 对每个层级进行排序
        SortCategoryTree(topLevelCategories);
        
        return topLevelCategories;
    }

    /// <summary>
    /// 递归排序分类树
    /// </summary>
    /// <param name="categories">分类列表</param>
    private static void SortCategoryTree(List<WorkflowCategoryDto> categories)
    {
        categories.Sort((a, b) =>
        {
            var orderComparison = a.OrderIndex.CompareTo(b.OrderIndex);
            return orderComparison != 0 ? orderComparison : string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
        });
        
        foreach (var category in categories)
        {
            if (category.Children.Any())
            {
                SortCategoryTree(category.Children);
            }
        }
    }

    /// <summary>
    /// 获取分类树形结构
    /// </summary>
    /// <param name="parentId">父级分类ID，null表示获取所有顶级分类</param>
    /// <returns>分类树形结构</returns>
    public async Task<List<WorkflowCategoryDto>> GetCategoryTreeAsync(int? parentId = null)
    {
        var query = _context.WorkflowCategories
            .Include(c => c.Parent)
            .Include(c => c.WorkflowDefinitions)
            .Where(c => c.ParentId == parentId)
            .OrderBy(c => c.OrderIndex)
            .ThenBy(c => c.Name);

        var categories = await query.ToListAsync();
        var categoryDtos = Mapper.Map<List<WorkflowCategoryDto>>(categories);

        // 递归获取子分类
        foreach (var category in categoryDtos)
        {
            category.Children = await GetCategoryTreeAsync(category.Id);
        }

        return categoryDtos;
    }

    /// <summary>
    /// 获取启用的分类列表
    /// </summary>
    /// <returns>启用的分类列表</returns>
    public async Task<List<WorkflowCategoryDto>> GetEnabledCategoriesAsync()
    {
        var categories = await _context.WorkflowCategories
            .Include(c => c.Parent)
            .Include(c => c.WorkflowDefinitions)
            .Where(c => c.IsEnabled)
            .OrderBy(c => c.OrderIndex)
            .ThenBy(c => c.Name)
            .ToListAsync();

        return Mapper.Map<List<WorkflowCategoryDto>>(categories);
    }

    /// <summary>
    /// 检查分类是否可以删除
    /// </summary>
    /// <param name="id">分类ID</param>
    /// <returns>是否可以删除</returns>
    public async Task<bool> CanDeleteAsync(int id)
    {
        // 检查是否有子分类
        var hasChildren = await _context.WorkflowCategories
            .AnyAsync(c => c.ParentId == id);

        if (hasChildren)
        {
            return false;
        }

        // 检查是否有关联的工作流定义
        var hasWorkflowDefinitions = await _context.WorkflowDefinitions
            .AnyAsync(w => w.CategoryId == id);

        return !hasWorkflowDefinitions;
    }

    /// <summary>
    /// 移动分类到指定父级
    /// </summary>
    /// <param name="id">分类ID</param>
    /// <param name="newParentId">新的父级分类ID</param>
    /// <returns>操作结果</returns>
    public async Task<bool> MoveCategoryAsync(int id, int? newParentId)
    {
        var category = await _context.WorkflowCategories.FindAsync(id);
        if (category == null)
        {
            return false;
        }

        // 检查是否会形成循环引用
        if (newParentId.HasValue && await WouldCreateCircularReference(id, newParentId.Value))
        {
            return false;
        }

        category.ParentId = newParentId;
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
            if (currentParentId.Value == categoryId)
            {
                return true;
            }

            var parent = await _context.WorkflowCategories
                .FirstOrDefaultAsync(c => c.Id == currentParentId.Value);
            
            currentParentId = parent?.ParentId;
        }

        return false;
    }

    /// <summary>
    /// 批量更新分类排序
    /// </summary>
    /// <param name="categoryOrders">分类排序信息</param>
    /// <returns>操作结果</returns>
    public async Task<bool> UpdateOrdersAsync(Dictionary<int, int> categoryOrders)
    {
        foreach (var (categoryId, orderIndex) in categoryOrders)
        {
            var category = await _context.WorkflowCategories.FindAsync(categoryId);
            if (category != null)
            {
                category.OrderIndex = orderIndex;
            }
        }

        await _context.SaveChangesAsync();
        return true;
    }
}
