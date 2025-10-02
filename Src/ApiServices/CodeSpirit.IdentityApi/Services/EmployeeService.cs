using AutoMapper;
using CodeSpirit.Core;
using CodeSpirit.Core.IdGenerator;
using CodeSpirit.IdentityApi.Data;
using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.IdentityApi.Dtos.Employee;
using CodeSpirit.IdentityApi.Utilities;
using CodeSpirit.Shared.Repositories;
using CodeSpirit.Shared.Services;
using LinqKit;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CodeSpirit.IdentityApi.Services;

/// <summary>
/// 职工服务实现
/// </summary>
public class EmployeeService : BaseCRUDIService<Employee, EmployeeDto, long, CreateEmployeeDto, UpdateEmployeeDto, EmployeeBatchImportItemDto>, IEmployeeService
{
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<Department> _departmentRepository;
    private readonly IRepository<ApplicationUser> _userRepository;
    private readonly ILogger<EmployeeService> _logger;
    private readonly IIdGenerator _idGenerator;
    private readonly ICurrentUser _currentUser;
    private readonly ApplicationDbContext _dbContext;
    private readonly IDepartmentService _departmentService;
    private readonly UserManager<ApplicationUser> _userManager;

    /// <summary>
    /// 构造函数
    /// </summary>
    public EmployeeService(
        IRepository<Employee> employeeRepository,
        IRepository<Department> departmentRepository,
        IRepository<ApplicationUser> userRepository,
        IMapper mapper,
        ILogger<EmployeeService> logger,
        IIdGenerator idGenerator,
        ICurrentUser currentUser,
        ApplicationDbContext dbContext,
        IDepartmentService departmentService,
        UserManager<ApplicationUser> userManager)
        : base(employeeRepository, mapper)
    {
        _employeeRepository = employeeRepository;
        _departmentRepository = departmentRepository;
        _userRepository = userRepository;
        _logger = logger;
        _idGenerator = idGenerator;
        _currentUser = currentUser;
        _dbContext = dbContext;
        _departmentService = departmentService;
        _userManager = userManager;
    }

    /// <summary>
    /// 获取职工列表（分页）
    /// </summary>
    public async Task<PageList<EmployeeDto>> GetEmployeesAsync(EmployeeQueryDto queryDto)
    {
        var predicate = PredicateBuilder.New<Employee>(true);

        // 应用搜索关键词过滤
        if (!string.IsNullOrWhiteSpace(queryDto.Keywords))
        {
            string searchLower = queryDto.Keywords.ToLower();
            predicate = predicate.Or(e => e.Name.ToLower().Contains(searchLower));
            predicate = predicate.Or(e => e.EmployeeNo.ToLower().Contains(searchLower));
            predicate = predicate.Or(e => e.IdNo.Contains(queryDto.Keywords));
            predicate = predicate.Or(e => e.PhoneNumber.Contains(queryDto.Keywords));
            predicate = predicate.Or(e => e.Email.ToLower().Contains(searchLower));
        }

        // 应用其他过滤条件
        if (queryDto.IsActive.HasValue)
        {
            predicate = predicate.And(e => e.IsActive == queryDto.IsActive.Value);
        }

        if (queryDto.Gender.HasValue)
        {
            predicate = predicate.And(e => e.Gender == queryDto.Gender.Value);
        }

        if (queryDto.DepartmentId.HasValue)
        {
            predicate = predicate.And(e => e.DepartmentId == queryDto.DepartmentId.Value);
        }

        if (queryDto.EmploymentStatus.HasValue)
        {
            predicate = predicate.And(e => e.EmploymentStatus == queryDto.EmploymentStatus.Value);
        }

        if (!string.IsNullOrWhiteSpace(queryDto.Position))
        {
            predicate = predicate.And(e => e.Position == queryDto.Position);
        }

        if (!string.IsNullOrWhiteSpace(queryDto.JobLevel))
        {
            predicate = predicate.And(e => e.JobLevel == queryDto.JobLevel);
        }

        if (queryDto.HireDate != null && queryDto.HireDate.Length == 2)
        {
            predicate = predicate.And(e => e.HireDate >= queryDto.HireDate[0]);
            predicate = predicate.And(e => e.HireDate <= queryDto.HireDate[1]);
        }

        // 创建查询
        var query = _employeeRepository.CreateQuery()
            .Include(e => e.Department)
            .Include(e => e.User)
            .Where(predicate);

        // 执行分页查询
        var totalCount = await query.CountAsync();
        var employees = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((queryDto.Page - 1) * queryDto.PerPage)
            .Take(queryDto.PerPage)
            .ToListAsync();

        // 映射到DTO
        var employeeDtos = Mapper.Map<List<EmployeeDto>>(employees);

        // 设置关联数据
        foreach (var dto in employeeDtos)
        {
            var employee = employees.FirstOrDefault(e => e.Id == dto.Id);
            if (employee?.Department != null)
            {
                dto.DepartmentName = employee.Department.Name;
            }
            if (employee?.User != null)
            {
                dto.UserName = employee.User.UserName;
            }
        }

        return new PageList<EmployeeDto>
        {
            Items = employeeDtos,
            Total = totalCount
        };
    }

    /// <summary>
    /// 根据部门获取职工列表
    /// </summary>
    public async Task<List<EmployeeDto>> GetEmployeesByDepartmentAsync(long departmentId, bool includeSubDepartments = false)
    {
        var departmentIds = new List<long> { departmentId };

        if (includeSubDepartments)
        {
            var childDepartments = await _departmentService.GetChildDepartmentsAsync(departmentId);
            departmentIds.AddRange(childDepartments.Select(d => d.Id));
        }

        var employees = await _employeeRepository.CreateQuery()
            .Include(e => e.Department)
            .Include(e => e.User)
            .Where(e => departmentIds.Contains(e.DepartmentId.Value))
            .ToListAsync();

        var employeeDtos = Mapper.Map<List<EmployeeDto>>(employees);

        // 设置关联数据
        foreach (var dto in employeeDtos)
        {
            var employee = employees.FirstOrDefault(e => e.Id == dto.Id);
            if (employee?.Department != null)
            {
                dto.DepartmentName = employee.Department.Name;
            }
            if (employee?.User != null)
            {
                dto.UserName = employee.User.UserName;
            }
        }

        return employeeDtos;
    }

    /// <summary>
    /// 设置职工激活状态
    /// </summary>
    public async Task SetActiveStatusAsync(long id, bool isActive)
    {
        var employee = await _employeeRepository.GetByIdAsync(id);
        if (employee == null)
        {
            throw new BusinessException("职工不存在");
        }

        employee.IsActive = isActive;
        await _employeeRepository.UpdateAsync(employee);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation($"职工 {employee.Name} 的激活状态已设置为 {isActive}");
    }

    /// <summary>
    /// 转移职工到新部门
    /// </summary>
    public async Task TransferEmployeeAsync(long employeeId, long? newDepartmentId)
    {
        var employee = await _employeeRepository.GetByIdAsync(employeeId);
        if (employee == null)
        {
            throw new BusinessException("职工不存在");
        }

        // 验证新部门存在性
        if (newDepartmentId.HasValue)
        {
            var newDepartment = await _departmentRepository.GetByIdAsync(newDepartmentId.Value);
            if (newDepartment == null)
            {
                throw new BusinessException("目标部门不存在");
            }

            if (!newDepartment.IsActive)
            {
                throw new BusinessException("目标部门未激活");
            }
        }

        employee.DepartmentId = newDepartmentId;
        await _employeeRepository.UpdateAsync(employee);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation($"职工 {employee.Name} 已转移到新部门");
    }

    /// <summary>
    /// 办理职工离职
    /// </summary>
    public async Task TerminateEmployeeAsync(long employeeId, DateTime terminationDate)
    {
        var employee = await _employeeRepository.GetByIdAsync(employeeId);
        if (employee == null)
        {
            throw new BusinessException("职工不存在");
        }

        if (employee.EmploymentStatus == EmploymentStatus.Terminated)
        {
            throw new BusinessException("该职工已离职");
        }

        employee.EmploymentStatus = EmploymentStatus.Terminated;
        employee.TerminationDate = terminationDate;
        employee.IsActive = false;

        await _employeeRepository.UpdateAsync(employee);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation($"职工 {employee.Name} 办理离职成功，离职日期: {terminationDate:yyyy-MM-dd}");
    }

    /// <summary>
    /// 验证工号是否唯一
    /// </summary>
    public async Task<bool> IsEmployeeNoUniqueAsync(string employeeNo, long? excludeId = null)
    {
        var query = _employeeRepository.CreateQuery()
            .Where(e => e.EmployeeNo == employeeNo);

        if (excludeId.HasValue)
        {
            query = query.Where(e => e.Id != excludeId.Value);
        }

        return !await query.AnyAsync();
    }

    /// <summary>
    /// 关联职工与用户账号
    /// </summary>
    public async Task LinkEmployeeToUserAsync(long employeeId, long userId)
    {
        var employee = await _employeeRepository.GetByIdAsync(employeeId);
        if (employee == null)
        {
            throw new BusinessException("职工不存在");
        }

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new BusinessException("用户不存在");
        }

        // 检查该职工是否已关联其他用户
        if (employee.UserId.HasValue)
        {
            throw new BusinessException($"该职工已关联用户，请先取消现有关联");
        }

        // 检查该用户是否已关联其他职工
        var existingEmployee = await _employeeRepository.CreateQuery()
            .FirstOrDefaultAsync(e => e.UserId == userId);
        if (existingEmployee != null)
        {
            throw new BusinessException($"该用户已关联职工 {existingEmployee.Name}，一个用户只能关联一个职工");
        }

        employee.UserId = userId;
        await _employeeRepository.UpdateAsync(employee);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation($"职工 {employee.Name} 已关联用户 {user.UserName}");
    }

    /// <summary>
    /// 取消职工与用户账号的关联
    /// </summary>
    public async Task UnlinkEmployeeFromUserAsync(long employeeId)
    {
        var employee = await _employeeRepository.GetByIdAsync(employeeId);
        if (employee == null)
        {
            throw new BusinessException("职工不存在");
        }

        employee.UserId = null;
        await _employeeRepository.UpdateAsync(employee);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation($"职工 {employee.Name} 已取消用户关联");
    }

    /// <summary>
    /// 创建职工
    /// </summary>
    public override async Task<EmployeeDto> CreateAsync(CreateEmployeeDto createDto)
    {
        // 验证工号唯一性
        if (!await IsEmployeeNoUniqueAsync(createDto.EmployeeNo))
        {
            throw new BusinessException($"工号 {createDto.EmployeeNo} 已存在");
        }

        // 验证部门存在性
        if (createDto.DepartmentId.HasValue)
        {
            var department = await _departmentRepository.GetByIdAsync(createDto.DepartmentId.Value);
            if (department == null)
            {
                throw new BusinessException("部门不存在");
            }
        }

        // 处理用户关联：如果未指定用户，自动创建用户
        long? userId = createDto.UserId;
        if (!userId.HasValue)
        {
            userId = await CreateUserForEmployeeAsync(createDto);
            _logger.LogInformation($"为职工 {createDto.Name} 自动创建了用户账号，用户ID: {userId}");
        }
        else
        {
            // 验证用户存在性
            var user = await _userRepository.GetByIdAsync(userId.Value);
            if (user == null)
            {
                throw new BusinessException("关联用户不存在");
            }

            // 检查该用户是否已关联其他职工
            var existingEmployee = await _employeeRepository.CreateQuery()
                .FirstOrDefaultAsync(e => e.UserId == userId.Value);
            if (existingEmployee != null)
            {
                throw new BusinessException($"该用户已关联职工 {existingEmployee.Name}");
            }
        }

        var employee = Mapper.Map<Employee>(createDto);
        employee.Id = _idGenerator.NewId();
        employee.UserId = userId;

        await _employeeRepository.AddAsync(employee);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation($"创建职工成功: {employee.Name}");

        return Mapper.Map<EmployeeDto>(employee);
    }

    /// <summary>
    /// 为职工创建关联用户账号
    /// </summary>
    private async Task<long> CreateUserForEmployeeAsync(CreateEmployeeDto createDto)
    {
        // 生成用户名：优先使用工号，如果为空则使用手机号或邮箱前缀
        string userName = createDto.EmployeeNo;
        if (string.IsNullOrWhiteSpace(userName))
        {
            if (!string.IsNullOrWhiteSpace(createDto.PhoneNumber))
            {
                userName = createDto.PhoneNumber;
            }
            else if (!string.IsNullOrWhiteSpace(createDto.Email))
            {
                userName = createDto.Email.Split('@')[0];
            }
            else
            {
                // 如果都没有，使用姓名拼音（简化处理，直接使用姓名）
                userName = createDto.Name.Replace(" ", "");
            }
        }

        // 确保用户名唯一
        var originalUserName = userName;
        var counter = 1;
        while (await _userManager.FindByNameAsync(userName) != null)
        {
            userName = $"{originalUserName}{counter}";
            counter++;
        }

        // 创建用户实体
        var user = new ApplicationUser
        {
            Id = _idGenerator.NewId(),
            UserName = userName,
            Name = createDto.Name,
            Email = createDto.Email,
            PhoneNumber = createDto.PhoneNumber,
            IdNo = createDto.IdNo,
            Gender = createDto.Gender,
            IsActive = createDto.IsActive,
            EmailConfirmed = false,
            PhoneNumberConfirmed = false,
            TwoFactorEnabled = false,
            LockoutEnabled = true,
            AccessFailedCount = 0
        };

        // 生成随机密码（12位，包含大小写字母、数字和特殊字符）
        string defaultPassword = PasswordGenerator.GenerateRandomPassword(12);

        // 创建用户
        var result = await _userManager.CreateAsync(user, defaultPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new BusinessException($"创建用户失败: {errors}");
        }

        _logger.LogInformation($"为职工自动创建用户账号成功: 用户名={userName}");

        return user.Id;
    }

    /// <summary>
    /// 更新职工
    /// </summary>
    public override async Task UpdateAsync(long id, UpdateEmployeeDto updateDto)
    {
        var employee = await _employeeRepository.GetByIdAsync(id);
        if (employee == null)
        {
            throw new BusinessException("职工不存在");
        }

        // 验证工号唯一性
        if (employee.EmployeeNo != updateDto.EmployeeNo && !await IsEmployeeNoUniqueAsync(updateDto.EmployeeNo, id))
        {
            throw new BusinessException($"工号 {updateDto.EmployeeNo} 已存在");
        }

        // 验证部门存在性
        if (updateDto.DepartmentId.HasValue)
        {
            var department = await _departmentRepository.GetByIdAsync(updateDto.DepartmentId.Value);
            if (department == null)
            {
                throw new BusinessException("部门不存在");
            }
        }

        // 验证用户存在性
        if (updateDto.UserId.HasValue)
        {
            var user = await _userRepository.GetByIdAsync(updateDto.UserId.Value);
            if (user == null)
            {
                throw new BusinessException("关联用户不存在");
            }

            // 检查该用户是否已关联其他职工
            var existingEmployee = await _employeeRepository.CreateQuery()
                .FirstOrDefaultAsync(e => e.UserId == updateDto.UserId.Value && e.Id != id);
            if (existingEmployee != null)
            {
                throw new BusinessException($"该用户已关联职工 {existingEmployee.Name}");
            }
        }

        Mapper.Map(updateDto, employee);
        await _employeeRepository.UpdateAsync(employee);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation($"更新职工成功: {employee.Name}");
    }

    /// <summary>
    /// 获取导入项的ID
    /// </summary>
    protected override string GetImportItemId(EmployeeBatchImportItemDto importDto)
    {
        return importDto.EmployeeNo;
    }

    /// <summary>
    /// 导入时的映射后处理
    /// </summary>
    protected override async Task OnImportMapping(Employee entity, EmployeeBatchImportItemDto importDto)
    {
        // 设置ID
        entity.Id = _idGenerator.NewId();

        // 根据部门编码查找部门
        if (!string.IsNullOrWhiteSpace(importDto.DepartmentCode))
        {
            var department = await _departmentRepository.CreateQuery()
                .FirstOrDefaultAsync(d => d.Code == importDto.DepartmentCode);
            if (department != null)
            {
                entity.DepartmentId = department.Id;
            }
        }
    }

    /// <summary>
    /// 导入前的处理
    /// </summary>
    protected override async Task OnImporting(Employee entity)
    {
        // 验证工号唯一性
        if (!await IsEmployeeNoUniqueAsync(entity.EmployeeNo))
        {
            throw new BusinessException($"工号 {entity.EmployeeNo} 已存在");
        }
    }
}

