using AutoMapper;
using CodeSpirit.Core;
using CodeSpirit.Core.IdGenerator;
using CodeSpirit.IdentityApi.Data;
using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.IdentityApi.Dtos.Department;
using CodeSpirit.Shared.Repositories;
using CodeSpirit.Shared.Services;
using CodeSpirit.Shared.Dtos.Common;
using LinqKit;
using Microsoft.EntityFrameworkCore;

namespace CodeSpirit.IdentityApi.Services;

/// <summary>
/// 部门服务实现
/// </summary>
public class DepartmentService : BaseCRUDIService<Department, DepartmentDto, long, CreateDepartmentDto, UpdateDepartmentDto, DepartmentBatchImportItemDto>, IDepartmentService
{
    private readonly IRepository<Department> _departmentRepository;
    private readonly ILogger<DepartmentService> _logger;
    private readonly IIdGenerator _idGenerator;
    private readonly ICurrentUser _currentUser;
    private readonly ApplicationDbContext _dbContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    public DepartmentService(
        IRepository<Department> departmentRepository,
        IMapper mapper,
        ILogger<DepartmentService> logger,
        IIdGenerator idGenerator,
        ICurrentUser currentUser,
        ApplicationDbContext dbContext,
        EnhancedBatchImportHelper<DepartmentBatchImportItemDto> importHelper)
        : base(departmentRepository, mapper, importHelper)
    {
        _departmentRepository = departmentRepository;
        _logger = logger;
        _idGenerator = idGenerator;
        _currentUser = currentUser;
        _dbContext = dbContext;
    }

    /// <summary>
    /// 获取部门列表（分页）
    /// </summary>
    public async Task<PageList<DepartmentDto>> GetDepartmentsAsync(DepartmentQueryDto queryDto)
    {
        var predicate = PredicateBuilder.New<Department>(true);

        // 应用搜索关键词过滤
        if (!string.IsNullOrWhiteSpace(queryDto.Keywords))
        {
            string searchLower = queryDto.Keywords.ToLower();
            predicate = predicate.Or(d => d.Name.ToLower().Contains(searchLower));
            predicate = predicate.Or(d => d.Code.ToLower().Contains(searchLower));
            predicate = predicate.Or(d => d.Description.ToLower().Contains(searchLower));
        }

        // 应用其他过滤条件
        if (queryDto.IsActive.HasValue)
        {
            predicate = predicate.And(d => d.IsActive == queryDto.IsActive.Value);
        }

        if (queryDto.ParentId.HasValue)
        {
            predicate = predicate.And(d => d.ParentId == queryDto.ParentId.Value);
        }

        if (queryDto.OnlyRootDepartments)
        {
            predicate = predicate.And(d => d.ParentId == null);
        }

        // 创建查询
        var query = _departmentRepository.CreateQuery()
            .Include(d => d.Parent)
            .Include(d => d.Manager)
            .Where(predicate);

        // 执行分页查询
        var totalCount = await query.CountAsync();
        var departments = await query
            .OrderBy(d => d.SortOrder)
            .ThenBy(d => d.Name)
            .Skip((queryDto.Page - 1) * queryDto.PerPage)
            .Take(queryDto.PerPage)
            .ToListAsync();

        // 映射到DTO
        var departmentDtos = Mapper.Map<List<DepartmentDto>>(departments);

        // 设置父部门名称
        foreach (var dto in departmentDtos)
        {
            var department = departments.FirstOrDefault(d => d.Id == dto.Id);
            if (department?.Parent != null)
            {
                dto.ParentName = department.Parent.Name;
            }
            if (department?.Manager != null)
            {
                dto.ManagerName = department.Manager.Name;
            }
        }

        return new PageList<DepartmentDto>
        {
            Items = departmentDtos,
            Total = totalCount
        };
    }

    /// <summary>
    /// 获取部门树形结构（支持查询条件）
    /// </summary>
    public async Task<List<DepartmentDto>> GetDepartmentsWithTreeAsync(DepartmentQueryDto queryDto)
    {
        var predicate = PredicateBuilder.New<Department>(true);

        // 应用搜索关键词过滤
        if (!string.IsNullOrWhiteSpace(queryDto.Keywords))
        {
            string searchLower = queryDto.Keywords.ToLower();
            predicate = predicate.Or(d => d.Name.ToLower().Contains(searchLower));
            predicate = predicate.Or(d => d.Code.ToLower().Contains(searchLower));
            predicate = predicate.Or(d => d.Description.ToLower().Contains(searchLower));
        }

        // 应用其他过滤条件
        if (queryDto.IsActive.HasValue)
        {
            predicate = predicate.And(d => d.IsActive == queryDto.IsActive.Value);
        }

        if (queryDto.ParentId.HasValue)
        {
            predicate = predicate.And(d => d.ParentId == queryDto.ParentId.Value);
        }

        // 获取所有符合条件的部门
        var query = _departmentRepository.CreateQuery()
            .Include(d => d.Manager)
            .Include(d => d.Parent)
            .Where(predicate)
            .OrderBy(d => d.SortOrder)
            .ThenBy(d => d.Name);

        var allDepartments = await query.ToListAsync();
        var allDepartmentDtos = Mapper.Map<List<DepartmentDto>>(allDepartments);

        // 设置管理者和父部门名称
        foreach (var dto in allDepartmentDtos)
        {
            var department = allDepartments.FirstOrDefault(d => d.Id == dto.Id);
            if (department?.Manager != null)
            {
                dto.ManagerName = department.Manager.Name;
            }
            if (department?.Parent != null)
            {
                dto.ParentName = department.Parent.Name;
            }
        }

        // 构建树形结构
        var rootDepartments = allDepartmentDtos.Where(d => d.ParentId == null).ToList();
        BuildDepartmentTree(rootDepartments, allDepartmentDtos);

        return rootDepartments;
    }

    /// <summary>
    /// 构建部门树形结构（递归）
    /// </summary>
    private void BuildDepartmentTree(List<DepartmentDto> parents, List<DepartmentDto> allDepartments)
    {
        foreach (var parent in parents)
        {
            parent.Children = allDepartments.Where(d => d.ParentId == parent.Id).ToList();
            if (parent.Children.Any())
            {
                BuildDepartmentTree(parent.Children, allDepartments);
            }
        }
    }

    /// <summary>
    /// 获取部门树形结构
    /// </summary>
    public async Task<List<DepartmentDto>> GetDepartmentTreeAsync(long? parentId = null)
    {
        var query = _departmentRepository.CreateQuery()
            .Include(d => d.Manager)
            .Where(d => d.ParentId == parentId && d.IsActive)
            .OrderBy(d => d.SortOrder)
            .ThenBy(d => d.Name);

        var departments = await query.ToListAsync();
        var departmentDtos = Mapper.Map<List<DepartmentDto>>(departments);

        // 递归获取子部门
        foreach (var dto in departmentDtos)
        {
            dto.Children = await GetDepartmentTreeAsync(dto.Id);
            
            var department = departments.FirstOrDefault(d => d.Id == dto.Id);
            if (department?.Manager != null)
            {
                dto.ManagerName = department.Manager.Name;
            }
        }

        return departmentDtos;
    }

    /// <summary>
    /// 获取部门的所有子部门（递归）
    /// </summary>
    public async Task<List<DepartmentDto>> GetChildDepartmentsAsync(long departmentId)
    {
        var result = new List<DepartmentDto>();
        var children = await _departmentRepository.CreateQuery()
            .Include(d => d.Manager)
            .Where(d => d.ParentId == departmentId)
            .ToListAsync();

        foreach (var child in children)
        {
            var childDto = Mapper.Map<DepartmentDto>(child);
            if (child.Manager != null)
            {
                childDto.ManagerName = child.Manager.Name;
            }
            result.Add(childDto);

            // 递归获取子部门
            var grandChildren = await GetChildDepartmentsAsync(child.Id);
            result.AddRange(grandChildren);
        }

        return result;
    }

    /// <summary>
    /// 设置部门激活状态
    /// </summary>
    public async Task SetActiveStatusAsync(long id, bool isActive)
    {
        var department = await _departmentRepository.GetByIdAsync(id);
        if (department == null)
        {
            throw new BusinessException("部门不存在");
        }

        department.IsActive = isActive;
        await _departmentRepository.UpdateAsync(department);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation($"部门 {department.Name} 的激活状态已设置为 {isActive}");
    }

    /// <summary>
    /// 移动部门到新的父部门
    /// </summary>
    public async Task MoveDepartmentAsync(long departmentId, long? newParentId)
    {
        var department = await _departmentRepository.GetByIdAsync(departmentId);
        if (department == null)
        {
            throw new BusinessException("部门不存在");
        }

        // 验证不能将部门移动到自己或自己的子部门下
        if (newParentId.HasValue)
        {
            if (newParentId.Value == departmentId)
            {
                throw new BusinessException("不能将部门移动到自己下面");
            }

            var childDepartments = await GetChildDepartmentsAsync(departmentId);
            if (childDepartments.Any(c => c.Id == newParentId.Value))
            {
                throw new BusinessException("不能将部门移动到自己的子部门下");
            }

            // 验证新父部门存在
            var newParent = await _departmentRepository.GetByIdAsync(newParentId.Value);
            if (newParent == null)
            {
                throw new BusinessException("目标父部门不存在");
            }
        }

        department.ParentId = newParentId;
        await _departmentRepository.UpdateAsync(department);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation($"部门 {department.Name} 已移动到新的父部门");
    }

    /// <summary>
    /// 验证部门编码是否唯一
    /// </summary>
    public async Task<bool> IsDepartmentCodeUniqueAsync(string code, long? excludeId = null)
    {
        var query = _departmentRepository.CreateQuery()
            .Where(d => d.Code == code);

        if (excludeId.HasValue)
        {
            query = query.Where(d => d.Id != excludeId.Value);
        }

        return !await query.AnyAsync();
    }

    /// <summary>
    /// 创建部门
    /// </summary>
    public override async Task<DepartmentDto> CreateAsync(CreateDepartmentDto createDto)
    {
        // 验证部门编码唯一性
        if (!await IsDepartmentCodeUniqueAsync(createDto.Code))
        {
            throw new BusinessException($"部门编码 {createDto.Code} 已存在");
        }

        // 验证父部门存在性
        if (createDto.ParentId.HasValue)
        {
            var parent = await _departmentRepository.GetByIdAsync(createDto.ParentId.Value);
            if (parent == null)
            {
                throw new BusinessException("父部门不存在");
            }
        }

        var department = Mapper.Map<Department>(createDto);
        department.Id = _idGenerator.NewId();

        await _departmentRepository.AddAsync(department);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation($"创建部门成功: {department.Name}");

        return Mapper.Map<DepartmentDto>(department);
    }

    /// <summary>
    /// 更新部门
    /// </summary>
    public override async Task UpdateAsync(long id, UpdateDepartmentDto updateDto)
    {
        var department = await _departmentRepository.GetByIdAsync(id);
        if (department == null)
        {
            throw new BusinessException("部门不存在");
        }

        // 验证部门编码唯一性
        if (department.Code != updateDto.Code && !await IsDepartmentCodeUniqueAsync(updateDto.Code, id))
        {
            throw new BusinessException($"部门编码 {updateDto.Code} 已存在");
        }

        // 验证父部门
        if (updateDto.ParentId.HasValue)
        {
            if (updateDto.ParentId.Value == id)
            {
                throw new BusinessException("不能将部门的父部门设置为自己");
            }

            var parent = await _departmentRepository.GetByIdAsync(updateDto.ParentId.Value);
            if (parent == null)
            {
                throw new BusinessException("父部门不存在");
            }

            // 检查是否会造成循环引用
            var childDepartments = await GetChildDepartmentsAsync(id);
            if (childDepartments.Any(c => c.Id == updateDto.ParentId.Value))
            {
                throw new BusinessException("不能将部门的父部门设置为其子部门");
            }
        }

        Mapper.Map(updateDto, department);
        await _departmentRepository.UpdateAsync(department);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation($"更新部门成功: {department.Name}");
    }

    /// <summary>
    /// 删除部门
    /// </summary>
    public override async Task DeleteAsync(long id)
    {
        var department = await _departmentRepository.CreateQuery()
            .Include(d => d.Children)
            .Include(d => d.Employees)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (department == null)
        {
            throw new BusinessException("部门不存在");
        }

        // 检查是否有子部门
        if (department.Children != null && department.Children.Any())
        {
            throw new BusinessException("该部门下还有子部门，无法删除");
        }

        // 检查是否有职工
        if (department.Employees != null && department.Employees.Any())
        {
            throw new BusinessException("该部门下还有职工，无法删除");
        }

        _dbContext.SoftDelete(department);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation($"删除部门成功: {department.Name}");
    }

    /// <summary>
    /// 获取导入项的ID
    /// </summary>
    protected override string GetImportItemId(DepartmentBatchImportItemDto importDto)
    {
        return importDto.Code;
    }

    /// <summary>
    /// 导入时的映射后处理
    /// </summary>
    protected override async Task OnImportMapping(Department entity, DepartmentBatchImportItemDto importDto)
    {
        // 设置ID
        entity.Id = _idGenerator.NewId();

        // 根据父部门编码查找父部门
        if (!string.IsNullOrWhiteSpace(importDto.ParentCode))
        {
            var parent = await _departmentRepository.CreateQuery()
                .FirstOrDefaultAsync(d => d.Code == importDto.ParentCode);
            if (parent != null)
            {
                entity.ParentId = parent.Id;
            }
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// 导入前的处理
    /// </summary>
    protected override async Task OnImporting(Department entity)
    {
        // 验证部门编码唯一性
        if (!await IsDepartmentCodeUniqueAsync(entity.Code))
        {
            throw new BusinessException($"部门编码 {entity.Code} 已存在");
        }
    }

    /// <summary>
    /// 批量创建组织结构（基于AI生成的数据）
    /// </summary>
    public async Task<int> CreateOrganizationStructureAsync(List<GeneratedDepartmentItemDto> departments)
    {
        if (departments == null || !departments.Any())
        {
            throw new BusinessException("部门列表不能为空");
        }

        // 验证部门编码唯一性
        var codes = departments.Select(d => d.Code).ToList();
        var duplicateCodes = codes.GroupBy(c => c).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (duplicateCodes.Any())
        {
            throw new BusinessException($"部门编码重复: {string.Join(", ", duplicateCodes)}");
        }

        // 检查是否与现有部门编码冲突
        foreach (var dept in departments)
        {
            if (!await IsDepartmentCodeUniqueAsync(dept.Code))
            {
                throw new BusinessException($"部门编码 {dept.Code} 已存在于系统中");
            }
        }

        // 创建部门编码到ID的映射字典
        var codeToIdMap = new Dictionary<string, long>();

        // 按层级排序：先创建没有父部门的（顶层），再创建有父部门的
        var departmentsToCreate = new List<GeneratedDepartmentItemDto>(departments);
        var createdCount = 0;

        // 最多尝试创建20次（防止循环依赖导致死循环）
        var maxAttempts = 20;
        var attempt = 0;

        while (departmentsToCreate.Any() && attempt < maxAttempts)
        {
            attempt++;
            var createdInThisRound = new List<GeneratedDepartmentItemDto>();

            foreach (var deptDto in departmentsToCreate)
            {
                // 如果没有父部门，或者父部门已经创建，则可以创建此部门
                if (string.IsNullOrWhiteSpace(deptDto.ParentCode) || codeToIdMap.ContainsKey(deptDto.ParentCode))
                {
                    var department = new Department
                    {
                        Id = _idGenerator.NewId(),
                        Code = deptDto.Code,
                        Name = deptDto.Name,
                        Description = deptDto.Description,
                        SortOrder = deptDto.SortOrder,
                        IsActive = deptDto.IsActive,
                        ParentId = string.IsNullOrWhiteSpace(deptDto.ParentCode) 
                            ? null 
                            : codeToIdMap.GetValueOrDefault(deptDto.ParentCode)
                    };

                    await _departmentRepository.AddAsync(department, saveChanges: false);
                    codeToIdMap[deptDto.Code] = department.Id;
                    createdInThisRound.Add(deptDto);
                    createdCount++;

                    _logger.LogInformation("创建部门: {Code} - {Name}, ParentCode: {ParentCode}", 
                        deptDto.Code, deptDto.Name, deptDto.ParentCode ?? "无");
                }
            }

            // 从待创建列表中移除已创建的部门
            departmentsToCreate = departmentsToCreate.Except(createdInThisRound).ToList();

            // 如果这一轮没有创建任何部门，说明存在循环依赖或引用了不存在的父部门
            if (!createdInThisRound.Any() && departmentsToCreate.Any())
            {
                var unresolvedCodes = string.Join(", ", departmentsToCreate.Select(d => $"{d.Code}(父:{d.ParentCode})"));
                throw new BusinessException($"无法解析以下部门的父子关系，可能存在循环依赖或引用了不存在的父部门: {unresolvedCodes}");
            }
        }

        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("成功创建组织结构，共 {Count} 个部门", createdCount);

        return createdCount;
    }
}

