using AutoMapper;
using CodeSpirit.Core;
using CodeSpirit.Core.IdGenerator;
using CodeSpirit.{Service}Api.Data.Models;
using CodeSpirit.{Service}Api.Dtos.{EntityName};
using CodeSpirit.Shared.Repositories;
using CodeSpirit.Shared.Services;
using LinqKit;
using Microsoft.EntityFrameworkCore;

namespace CodeSpirit.{Service}Api.Services;

/// <summary>
/// {EntityName} 服务接口
/// </summary>
public interface I{EntityName}Service 
    : IBaseCRUDIService<{EntityName}, {EntityName}Dto, long, Create{EntityName}Dto, Update{EntityName}Dto, {EntityName}BatchImportItemDto>, 
      IScopedDependency
{
    Task<PageList<{EntityName}Dto>> Get{EntityName}sAsync({EntityName}QueryDto queryDto);
}

/// <summary>
/// {EntityName} 服务实现
/// </summary>
public class {EntityName}Service 
    : BaseCRUDIService<{EntityName}, {EntityName}Dto, long, Create{EntityName}Dto, Update{EntityName}Dto, {EntityName}BatchImportItemDto>, 
      I{EntityName}Service
{
    private readonly IRepository<{EntityName}> _{entityName}Repository;
    private readonly IIdGenerator _idGenerator;
    private readonly ICurrentUser _currentUser;
    
    public {EntityName}Service(
        IRepository<{EntityName}> {entityName}Repository,
        IMapper mapper,
        IIdGenerator idGenerator,
        ICurrentUser currentUser,
        EnhancedBatchImportHelper<{EntityName}BatchImportItemDto> importHelper)
        : base({entityName}Repository, mapper, importHelper)
    {
        _{entityName}Repository = {entityName}Repository;
        _idGenerator = idGenerator;
        _currentUser = currentUser;
    }
    
    public async Task<PageList<{EntityName}Dto>> Get{EntityName}sAsync({EntityName}QueryDto queryDto)
    {
        var predicate = PredicateBuilder.New<{EntityName}>(true);
        
        // 关键字搜索
        if (!string.IsNullOrWhiteSpace(queryDto.Keywords))
        {
            string searchLower = queryDto.Keywords.ToLower();
            predicate = predicate.Or(e => e.{PropertyName}.ToLower().Contains(searchLower));
        }
        
        // 其他过滤条件
        if (queryDto.IsActive.HasValue)
        {
            predicate = predicate.And(e => e.IsActive == queryDto.IsActive.Value);
        }
        
        var query = _{entityName}Repository.CreateQuery()
            .Where(predicate);
        
        var totalCount = await query.CountAsync();
        var {entityName}s = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((queryDto.Page - 1) * queryDto.PerPage)
            .Take(queryDto.PerPage)
            .ToListAsync();
        
        return new PageList<{EntityName}Dto>
        {
            Items = Mapper.Map<List<{EntityName}Dto>>({entityName}s),
            Total = totalCount
        };
    }
    
    public override async Task<{EntityName}Dto> CreateAsync(Create{EntityName}Dto createDto)
    {
        var {entityName} = Mapper.Map<{EntityName}>(createDto);
        {entityName}.Id = _idGenerator.NewId();
        {entityName}.TenantId = _currentUser.TenantId;
        
        await _{entityName}Repository.AddAsync({entityName});
        await _dbContext.SaveChangesAsync();
        
        return Mapper.Map<{EntityName}Dto>({entityName});
    }
}
