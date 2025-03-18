using AutoMapper;
using CodeSpirit.Core;
using CodeSpirit.ExamApi.Data.Models;
using CodeSpirit.ExamApi.Dtos.QuestionCategory;
using CodeSpirit.ExamApi.Services.Interfaces;
using CodeSpirit.Shared.Repositories;
using CodeSpirit.Shared.Services;
using LinqKit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace CodeSpirit.ExamApi.Services.Implementations;

/// <summary>
/// 题目分类服务实现
/// </summary>
public class QuestionCategoryService : BaseCRUDService<QuestionCategory, QuestionCategoryDto, long, CreateQuestionCategoryDto, UpdateQuestionCategoryDto>, IQuestionCategoryService
{
    private readonly ILogger<QuestionCategoryService> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public QuestionCategoryService(
        IRepository<QuestionCategory> repository,
        IMapper mapper,
        ILogger<QuestionCategoryService> logger)
        : base(repository, mapper)
    {
        _logger = logger;
    }

    /// <summary>
    /// 获取题目分类分页列表
    /// </summary>
    public async Task<PageList<QuestionCategoryDto>> GetQuestionCategoriesAsync(QuestionCategoryQueryDto queryDto)
    {
        var predicate = PredicateBuilder.New<QuestionCategory>(true);

        if (!string.IsNullOrEmpty(queryDto.Keywords))
        {
            predicate = predicate.Or(x => x.Name.Contains(queryDto.Keywords));
            predicate = predicate.Or(x => x.Description != null && x.Description.Contains(queryDto.Keywords));
        }

        return await GetPagedListAsync(
            queryDto,
            predicate,
            "Parent", "Questions"
        );
    }

    /// <summary>
    /// 获取所有题目分类
    /// </summary>
    public async Task<List<QuestionCategoryDto>> GetAllCategoriesAsync()
    {
        var categories = await Repository.CreateQuery()
            .Include(c => c.Parent)
            .Include(c => c.Questions)
            .ToListAsync();

        return Mapper.Map<List<QuestionCategoryDto>>(categories);
    }

    /// <summary>
    /// 验证创建DTO
    /// </summary>
    protected override async Task ValidateCreateDto(CreateQuestionCategoryDto createDto)
    {
        await base.ValidateCreateDto(createDto);

        // 验证父分类是否存在
        if (createDto.ParentId.HasValue)
        {
            var parentExists = await Repository.ExistsAsync(c => c.Id == createDto.ParentId.Value);
            if (!parentExists)
            {
                throw new AppServiceException(400, "父分类不存在");
            }
        }
    }

    /// <summary>
    /// 验证更新DTO
    /// </summary>
    protected override async Task ValidateUpdateDto(long id, UpdateQuestionCategoryDto updateDto)
    {
        await base.ValidateUpdateDto(id, updateDto);

        // 验证父分类是否存在
        if (updateDto.ParentId.HasValue)
        {
            // 不能将自己设为自己的父分类
            if (updateDto.ParentId.Value == id)
            {
                throw new AppServiceException(400, "不能将分类自身设为父分类");
            }

            var parentExists = await Repository.ExistsAsync(c => c.Id == updateDto.ParentId.Value);
            if (!parentExists)
            {
                throw new AppServiceException(400, "父分类不存在");
            }

            // 检查是否形成循环引用
            var parentId = updateDto.ParentId.Value;
            var maxDepth = 10; // 防止无限循环
            var depth = 0;

            while (parentId != 0 && depth < maxDepth)
            {
                var parent = await Repository.GetByIdAsync(parentId);
                if (parent == null)
                {
                    break;
                }

                if (parent.ParentId == id)
                {
                    throw new AppServiceException(400, "不能将子分类设为父分类，会形成循环引用");
                }

                parentId = parent.ParentId ?? 0;
                depth++;
            }
        }
    }

    /// <summary>
    /// 删除前验证
    /// </summary>
    protected override async Task OnDeleting(QuestionCategory entity)
    {
        await base.OnDeleting(entity);

        // 检查是否有子分类
        bool hasChildren = await Repository.CreateQuery().AnyAsync(c => c.ParentId == entity.Id);
        if (hasChildren)
        {
            throw new AppServiceException(400, "该分类下存在子分类，不能直接删除");
        }

        // 检查是否有题目关联
        if (entity.Questions.Any())
        {
            throw new AppServiceException(400, "该分类下存在题目，不能直接删除");
        }
    }
} 