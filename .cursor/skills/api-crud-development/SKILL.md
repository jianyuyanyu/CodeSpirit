---
name: api-crud-development
description: 指导从 Entity 到 Controller 的完整 CRUD 功能开发流程。包括实体创建、DTO设计、服务实现、控制器开发、数据库配置和迁移。当用户需要开发新的 CRUD 功能、创建 API 接口、或添加新的业务模块时使用。
---

# API CRUD 开发 Skill

## 快速开始

CodeSpirit 项目的标准 CRUD 开发流程包含 7 个步骤：

1. 创建实体（Entity）
2. 创建 DTO 类
3. 配置 AutoMapper 映射
4. 创建服务接口和实现
5. 创建控制器
6. 配置数据库上下文
7. 创建数据库迁移

---

## 完整工作流程

### 步骤 1：创建实体（Entity）

**位置**：`Data/Models/{EntityName}.cs`

**要求**：
- 实现 `IFullAuditable`（审计字段）
- 实现 `IMultiTenant`（多租户）
- 实现 `IIsActive`（激活状态）
- 使用 `long` 作为主键类型
- 添加验证特性（`[Required]`、`[MaxLength]` 等）

**模板**：参见 [templates/entity-template.cs](templates/entity-template.cs)

**示例**：
```csharp
/// <summary>
/// 职工信息
/// </summary>
public class Employee : IFullAuditable, IMultiTenant, IIsActive
{
    public long Id { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string TenantId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string EmployeeNo { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    // 审计字段（IFullAuditable）
    public long CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public long? DeletedBy { get; set; }
    public DateTime? DeletedAt { get; set; }
    public bool IsDeleted { get; set; }
    
    // 激活状态（IIsActive）
    public bool IsActive { get; set; }
}
```

---

### 步骤 2：创建 DTO 类

**位置**：`Dtos/{EntityName}/`

需要创建 4 种 DTO：

#### 2.1 展示 DTO（{EntityName}Dto）

用于列表展示和详情查询，使用**列特性**（Columns）：

```csharp
/// <summary>
/// 职工数据传输对象
/// </summary>
public class EmployeeDto
{
    public long Id { get; set; }
    
    [DisplayName("工号")]
    public string EmployeeNo { get; set; }
    
    [DisplayName("姓名")]
    [TplColumn(template: "${name}")]
    public string Name { get; set; }
    
    [DisplayName("头像")]
    [AvatarColumn(Text = "${name}", Src = "${avatarUrl}")]
    [Badge(Animation = true, VisibleOn = "isActive", Level = "info")]
    public string AvatarUrl { get; set; }
    
    [DisplayName("创建时间")]
    [DateColumn(FromNow = true)]
    public DateTime CreatedAt { get; set; }
}
```

**常用列特性**：
- `AmisColumn`：基础列（Hidden、Sortable、Copyable、Fixed）
- `TplColumn`：自定义模板列
- `AvatarColumn`：头像列
- `DateColumn`：日期列（Format、FromNow）
- `TagsColumn`：标签列
- `LinkColumn`：链接列
- `AmisStatusColumn`：状态列

#### 2.2 创建 DTO（Create{EntityName}Dto）

用于创建操作，使用**表单字段特性**（FormFields）：

```csharp
/// <summary>
/// 创建职工数据传输对象
/// </summary>
[FormGroup("basic", "基本信息", "EmployeeNo,Name,Gender", Order = 1)]
[FormGroup("contact", "联系方式", "PhoneNumber,Email", Order = 2)]
public class CreateEmployeeDto
{
    [Required(ErrorMessage = "工号不能为空")]
    [MaxLength(50, ErrorMessage = "工号长度不能超过50个字符")]
    [DisplayName("工号")]
    [AmisInputTextField(ColumnRatio = 6)]
    public string EmployeeNo { get; set; }
    
    [Required(ErrorMessage = "姓名不能为空")]
    [DisplayName("姓名")]
    [AmisInputTextField(ColumnRatio = 6)]
    public string Name { get; set; }
    
    [DisplayName("部门")]
    [AmisInputTreeField(
        DataSource = "${ROOT_API}/api/identity/Departments/tree",
        LabelField = "name",
        ValueField = "id",
        Multiple = false,
        Searchable = true,
        ColumnRatio = 12
    )]
    public long? DepartmentId { get; set; }
}
```

**常用表单字段特性**：
- `FormGroup`：表单分组（Name、Title、Fields、Order）
- `AmisInputTextField`：文本输入框
- `AmisInputTreeField`：树形选择
- `AmisSelectField`：下拉选择
- `AmisInputImageField`：图片上传
- `AmisDateFieldAttribute`：日期选择
- `AmisTextareaField`：多行文本
- `AmisNumberField`：数字输入
- `AmisSwitchField`：开关

**AI 表单填充**（可选）：
```csharp
[AiFormFill(TriggerField = nameof(Name))]
public class CreateEmployeeDto
{
    [AiFieldFill(Priority = 1, CustomDescription = "基于姓名生成相关信息")]
    public string? Email { get; set; }
}
```

#### 2.3 更新 DTO（Update{EntityName}Dto）

用于更新操作，通常继承或复用 Create DTO 的字段。

#### 2.4 查询 DTO（{EntityName}QueryDto）

用于查询筛选，继承 `QueryDtoBase`：

```csharp
/// <summary>
/// 职工查询数据传输对象
/// </summary>
public class EmployeeQueryDto : QueryDtoBase
{
    [DisplayName("关键字")]
    public string? Keywords { get; set; }
    
    [DisplayName("部门")]
    [AmisInputTreeField(
        DataSource = "${ROOT_API}/api/identity/Departments/tree",
        SubmitOnChange = true,
        ColumnRatio = 12
    )]
    [PageAside()]  // 标记为侧边栏字段
    public long? DepartmentId { get; set; }
    
    [DisplayName("是否激活")]
    public bool? IsActive { get; set; }
}
```

**模板**：参见 [templates/dto-template.cs](templates/dto-template.cs)

---

### 步骤 3：配置 AutoMapper 映射

**位置**：`MappingProfiles/{EntityName}Profile.cs`

```csharp
public class EmployeeProfile : Profile
{
    public EmployeeProfile()
    {
        // Entity -> DTO
        CreateMap<Employee, EmployeeDto>()
            .ForMember(dest => dest.DepartmentName, 
                opt => opt.MapFrom(src => src.Department != null ? src.Department.Name : null));
        
        // CreateDTO -> Entity
        CreateMap<CreateEmployeeDto, Employee>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.TenantId, opt => opt.Ignore())
            .ForMember(dest => dest.Department, opt => opt.Ignore());
        
        // UpdateDTO -> Entity
        CreateMap<UpdateEmployeeDto, Employee>()
            .ForMember(dest => dest.Id, opt => opt.Ignore());
        
        // PageList 映射
        CreateMap<PageList<Employee>, PageList<EmployeeDto>>();
    }
}
```

---

### 步骤 4：创建服务接口和实现

#### 4.1 服务接口

**位置**：`Services/I{EntityName}Service.cs`

```csharp
/// <summary>
/// 职工服务接口
/// </summary>
public interface IEmployeeService 
    : IBaseCRUDIService<Employee, EmployeeDto, long, CreateEmployeeDto, UpdateEmployeeDto, EmployeeBatchImportItemDto>, 
      IScopedDependency  // 自动注册标记
{
    Task<PageList<EmployeeDto>> GetEmployeesAsync(EmployeeQueryDto queryDto);
    
    Task SetActiveStatusAsync(long id, bool isActive);
    
    Task<bool> IsEmployeeNoUniqueAsync(string employeeNo, long? excludeId = null);
}
```

**依赖注入标记接口**：
- `IScopedDependency`：Scoped 生命周期（接口继承）
- `ITransientDependency`：Transient 生命周期
- `ISingletonDependency`：Singleton 生命周期

#### 4.2 服务实现

**位置**：`Services/{EntityName}Service.cs`

```csharp
/// <summary>
/// 职工服务实现
/// </summary>
public class EmployeeService 
    : BaseCRUDIService<Employee, EmployeeDto, long, CreateEmployeeDto, UpdateEmployeeDto, EmployeeBatchImportItemDto>, 
      IEmployeeService
{
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IIdGenerator _idGenerator;
    private readonly ICurrentUser _currentUser;
    
    public EmployeeService(
        IRepository<Employee> employeeRepository,
        IMapper mapper,
        IIdGenerator idGenerator,
        ICurrentUser currentUser,
        EnhancedBatchImportHelper<EmployeeBatchImportItemDto> importHelper)
        : base(employeeRepository, mapper, importHelper)
    {
        _employeeRepository = employeeRepository;
        _idGenerator = idGenerator;
        _currentUser = currentUser;
    }
    
    public async Task<PageList<EmployeeDto>> GetEmployeesAsync(EmployeeQueryDto queryDto)
    {
        var predicate = PredicateBuilder.New<Employee>(true);
        
        // 关键字搜索
        if (!string.IsNullOrWhiteSpace(queryDto.Keywords))
        {
            string searchLower = queryDto.Keywords.ToLower();
            predicate = predicate.Or(e => e.Name.ToLower().Contains(searchLower));
            predicate = predicate.Or(e => e.EmployeeNo.ToLower().Contains(searchLower));
        }
        
        // 其他过滤条件
        if (queryDto.IsActive.HasValue)
        {
            predicate = predicate.And(e => e.IsActive == queryDto.IsActive.Value);
        }
        
        var query = _employeeRepository.CreateQuery()
            .Include(e => e.Department)
            .Where(predicate);
        
        var totalCount = await query.CountAsync();
        var employees = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((queryDto.Page - 1) * queryDto.PerPage)
            .Take(queryDto.PerPage)
            .ToListAsync();
        
        return new PageList<EmployeeDto>
        {
            Items = Mapper.Map<List<EmployeeDto>>(employees),
            Total = totalCount
        };
    }
    
    public override async Task<EmployeeDto> CreateAsync(CreateEmployeeDto createDto)
    {
        // 业务验证
        if (!await IsEmployeeNoUniqueAsync(createDto.EmployeeNo))
        {
            throw new BusinessException($"工号 {createDto.EmployeeNo} 已存在");
        }
        
        var employee = Mapper.Map<Employee>(createDto);
        employee.Id = _idGenerator.NewId();
        employee.TenantId = _currentUser.TenantId;
        
        await _employeeRepository.AddAsync(employee);
        await _dbContext.SaveChangesAsync();
        
        return Mapper.Map<EmployeeDto>(employee);
    }
    
    protected override async Task ValidateCreateDto(CreateEmployeeDto createDto)
    {
        await base.ValidateCreateDto(createDto);
        // 自定义验证逻辑
    }
    
    protected override async Task<Employee> OnCreating(CreateEmployeeDto createDto)
    {
        var employee = await base.OnCreating(createDto);
        // 创建前处理
        return employee;
    }
}
```

**服务基类方法**：
- `CreateAsync`：创建
- `UpdateAsync`：更新
- `DeleteAsync`：删除（软删除）
- `GetAsync`：获取单个
- `GetPagedListAsync`：分页查询
- `BatchDeleteAsync`：批量删除
- `BatchImportAsync`：批量导入

**可重写的生命周期方法**：
- `ValidateCreateDto`：创建前验证
- `ValidateUpdateDto`：更新前验证
- `OnCreating`：创建前处理
- `OnUpdating`：更新前处理
- `OnDeleting`：删除前处理

**模板**：参见 [templates/service-template.cs](templates/service-template.cs)

---

### 步骤 5：创建控制器

**位置**：`Controllers/{EntityName}sController.cs`

```csharp
/// <summary>
/// 职工管理控制器
/// </summary>
[DisplayName("职工管理")]
[Navigation(Icon = "fa-solid fa-user-tie", PlatformType = PlatformType.Tenant)]
public class EmployeesController : ApiControllerBase
{
    private readonly IEmployeeService _employeeService;
    
    public EmployeesController(IEmployeeService employeeService)
    {
        _employeeService = employeeService;
    }
    
    /// <summary>
    /// 获取职工列表
    /// </summary>
    [HttpGet]
    [DisplayName("获取职工列表")]
    public async Task<ActionResult<ApiResponse<PageList<EmployeeDto>>>> GetEmployees(
        [FromQuery] EmployeeQueryDto queryDto)
    {
        var employees = await _employeeService.GetEmployeesAsync(queryDto);
        return SuccessResponse(employees);
    }
    
    /// <summary>
    /// 获取职工详情
    /// </summary>
    [HttpGet("{id}")]
    [DisplayName("获取职工详情")]
    public async Task<ActionResult<ApiResponse<EmployeeDto>>> Detail(long id)
    {
        var employee = await _employeeService.GetAsync(id);
        return SuccessResponse(employee);
    }
    
    /// <summary>
    /// 创建职工
    /// </summary>
    [HttpPost]
    [DisplayName("创建职工")]
    public async Task<ActionResult<ApiResponse<EmployeeDto>>> CreateEmployee(
        CreateEmployeeDto createDto)
    {
        var employee = await _employeeService.CreateAsync(createDto);
        return SuccessResponseWithCreate<EmployeeDto>(nameof(Detail), employee);
    }
    
    /// <summary>
    /// 更新职工
    /// </summary>
    [HttpPut("{id}")]
    [DisplayName("更新职工")]
    public async Task<ActionResult<ApiResponse>> UpdateEmployee(
        long id, UpdateEmployeeDto updateDto)
    {
        await _employeeService.UpdateAsync(id, updateDto);
        return SuccessResponse();
    }
    
    /// <summary>
    /// 删除职工
    /// </summary>
    [HttpDelete("{id}")]
    [DisplayName("删除职工")]
    [Operation("删除", "ajax", null, "确定要删除此职工吗？")]
    public async Task<ActionResult<ApiResponse>> DeleteEmployee(long id)
    {
        await _employeeService.DeleteAsync(id);
        return SuccessResponse();
    }
    
    /// <summary>
    /// 批量删除职工
    /// </summary>
    [HttpPost("batch/delete")]
    [DisplayName("批量删除职工")]
    [Operation("批量删除", "ajax", null, "确定要批量删除选中的职工吗？", isBulkOperation: true)]
    public async Task<ActionResult<ApiResponse>> BatchDelete(
        [FromBody] BatchOperationDto<long> request)
    {
        var (successCount, failedIds) = await _employeeService.BatchDeleteAsync(request.Ids);
        return SuccessResponse($"成功删除 {successCount} 个职工！");
    }
    
    /// <summary>
    /// 快速创建（HeaderOperation示例）
    /// </summary>
    [HttpPost("quick-create")]
    [HeaderOperation("快速创建", "form", Icon = "fa-solid fa-plus", DialogSize = DialogSize.MD)]
    [DisplayName("快速创建职工")]
    public async Task<ActionResult<ApiResponse<EmployeeDto>>> QuickCreate(
        CreateEmployeeDto createDto)
    {
        var employee = await _employeeService.CreateAsync(createDto);
        return SuccessResponse(employee);
    }
}
```

**控制器特性**：
- `DisplayName`：显示名称
- `Navigation`：导航菜单（Icon、PlatformType）
- `Operation`：操作按钮（Label、Type、Icon、ConfirmText、isBulkOperation）
- `HeaderOperation`：头部操作按钮（Label、Type、Icon、DialogSize）
- `Permission`：权限控制（`[Permission("identity_employees_create")]`）

**模板**：参见 [templates/controller-template.cs](templates/controller-template.cs)

---

### 步骤 6：配置数据库上下文

**位置**：`Data/{Service}DbContext.cs`

```csharp
public class ApplicationDbContext : MultiDatabaseDbContextBase
{
    public DbSet<Employee> Employees => Set<Employee>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<Employee>(entity =>
        {
            entity.ToTable(nameof(Employee));
            entity.Property(e => e.Id).ValueGeneratedNever(); // 雪花ID配置
            
            // 复合唯一索引（租户ID + 工号）
            entity.HasIndex(e => new { e.TenantId, e.EmployeeNo })
                .IsUnique()
                .HasDatabaseName("IX_Employee_TenantId_EmployeeNo");
            
            // 关联关系配置
            entity.HasOne(e => e.Department)
                .WithMany()
                .HasForeignKey(e => e.DepartmentId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
```

**推荐**：使用 `IEntityTypeConfiguration<T>` 配置类，在 `Configurations/` 目录下。

---

### 步骤 7：创建数据库迁移

使用 [db-migration Skill](../db-migration/SKILL.md)：

```powershell
# MySQL 迁移
dotnet ef migrations add AddEmployees --context MySqlApplicationDbContext --output-dir Migrations/MySql

# SQL Server 迁移
dotnet ef migrations add AddEmployees --context SqlServerApplicationDbContext --output-dir Migrations/SqlServer
```

---

## 检查清单

开发完成后，检查以下事项：

- [ ] 实体实现必需接口（`IFullAuditable`、`IMultiTenant`、`IIsActive`）
- [ ] DTO 添加验证和显示特性（`[Required]`、`[DisplayName]`）
- [ ] DTO 添加 AMIS 特性（表单字段、表格列）
- [ ] AutoMapper 配置完整（Entity ↔ DTO 双向映射）
- [ ] 服务接口继承 `IBaseCRUDIService` + `IScopedDependency`
- [ ] 服务实现继承 `BaseCRUDIService`
- [ ] 控制器添加必需特性（`DisplayName`、`Navigation`）
- [ ] 控制器方法返回 `ActionResult<ApiResponse<T>>`
- [ ] 数据库上下文配置实体和索引
- [ ] 雪花 ID 配置正确（`ValueGeneratedNever()`）
- [ ] 创建数据库迁移（MySQL 和 SQL Server）

---

## 代码模板

- [实体模板](templates/entity-template.cs)
- [DTO 模板](templates/dto-template.cs)
- [服务模板](templates/service-template.cs)
- [控制器模板](templates/controller-template.cs)

---

## 最佳实践

1. **实体设计**：实现标准接口，使用 `long` 主键
2. **DTO 分离**：创建、更新、查询分别创建 DTO
3. **特性使用**：合理使用 AMIS 特性控制界面生成
4. **服务层**：继承 `BaseCRUDIService`，重写验证和生命周期方法
5. **控制器**：保持简洁，主要调用服务层方法
6. **依赖注入**：接口继承标记接口，自动注册
7. **数据库**：为常用查询字段创建索引，配置关联关系

---

## 相关资源

- [控制器规范](../../rules/controller.mdc)
- [服务类规范](../../rules/service.mdc)
- [DTO 规范](../../rules/dto.mdc)
- [数据库规范](../../rules/database.mdc)
- [多数据库迁移 Skill](../db-migration/SKILL.md)
