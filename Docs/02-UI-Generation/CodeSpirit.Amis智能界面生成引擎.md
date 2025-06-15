# CodeSpirit.Amis 智能界面生成引擎

## 概述

CodeSpirit.Amis 是基于百度 AMIS 前端框架的智能界面生成引擎，通过反射和特性配置自动生成管理后台界面。支持 CRUD 操作、统计图表、表单验证等功能，大幅提升开发效率。

## 核心架构

### 主要组件

- **AmisGenerator**: 核心生成器，负责生成 AMIS 配置
- **AmisCRUDConfigBuilder**: CRUD 配置构建器
- **StatisticsConfigBuilder**: 统计图表配置构建器
- **AmisContext**: 上下文信息管理
- **CrudActions**: CRUD 操作定义

### 帮助类 (Helpers)

- **ControllerHelper**: 控制器信息处理
- **ApiRouteHelper**: API 路由管理
- **ButtonHelper**: 按钮配置生成
- **ColumnHelper**: 列配置处理
- **SearchFieldHelper**: 搜索字段处理
- **CrudHelper**: CRUD 操作检测
- **UtilityHelper**: 通用工具方法
- **CachingHelper**: 缓存管理

## 功能特性

### 1. CRUD 操作支持

#### 支持的操作类型
- [x] **Create**: 新增数据
- [x] **List**: 列表查询（支持分页和非分页）
- [x] **Update**: 更新数据
- [x] **Delete**: 删除数据
- [x] **QuickSave**: 快速保存
- [x] **Export**: 数据导出
- [x] **Import**: 数据导入
- [x] **Detail**: 详情查看

#### 列表功能
- [x] **分页支持**: 自动检测 `PageList<T>` 类型
- [x] **排序**: 支持列排序配置
- [x] **筛选**: 基于查询 DTO 自动生成筛选表单
- [x] **批量操作**: 批量删除、批量表单操作
- [x] **快速编辑**: 行内编辑功能
- [x] **导出功能**: 
  - [x] 导出当前页
  - [x] 导出全部数据
- [x] **导入功能**:
  - [x] Excel 上传解析
  - [x] 导入预览
  - [x] 数据验证

### 2. 列类型支持

#### 基础列类型
- [x] **文本列**: 默认字符串显示
- [x] **数值列**: 数字类型显示
- [x] **日期列**: `DateTime`/`DateTimeOffset` 类型
- [x] **布尔列**: `bool` 类型显示为开关
- [x] **枚举列**: `enum` 类型显示为映射

#### 特殊列类型
- [x] **头像列**: `AvatarColumnAttribute`
- [x] **图片列**: `[DataType(DataType.ImageUrl)]`
- [x] **链接列**: `LinkColumnAttribute`
- [x] **模板列**: `TplColumnAttribute`
- [x] **列表列**: `List<T>` 类型
- [x] **标签列**: `TagsColumnAttribute`
- [x] **遍历列**: `EachColumnAttribute`
- [x] **JSON 列**: JSON 数据显示
- [x] **徽章列**: `BadgeAttribute`

#### 列配置特性
- [x] **列忽略**: `IgnoreColumnAttribute`
- [x] **列排序**: 支持配置排序
- [x] **默认隐藏**: 主键、密码字段自动隐藏
- [x] **固定列**: 支持列固定
- [x] **背景色阶**: 数值列背景色渐变

### 3. 表单字段支持

#### 基础字段类型
- [x] **文本字段**: `string` 类型
- [x] **数值字段**: `int`/`long`/`float`/`double` 类型
- [x] **布尔字段**: `bool` 类型显示为开关
- [x] **日期时间字段**: `DateTime`/`DateTimeOffset` 类型
- [x] **密码字段**: `[DataType(DataType.Password)]`
- [x] **枚举字段**: `enum` 类型显示为下拉选择

#### 高级字段类型
- [x] **图片上传**: `AmisInputImageFieldAttribute`
- [x] **下拉选择**: `AmisSelectFieldAttribute`
- [x] **树形选择**: `AmisTreeSelectFieldAttribute`
- [x] **输入树**: `AmisInputTreeFieldAttribute`
- [x] **文本域**: `AmisTextareaFieldAttribute`
- [x] **数值输入**: `AmisNumberFieldAttribute`
- [x] **日期选择**: `AmisDateFieldAttribute`
- [x] **时间选择**: `AmisTimeFieldAttribute`
- [x] **日期时间**: `AmisDatetimeFieldAttribute`
- [x] **数组字段**: `AmisArrayFieldAttribute`
- [x] **表格字段**: `AmisTableFieldAttribute`
- [x] **穿梭框**: `AmisTransferFieldAttribute`
- [x] **Excel 上传**: `AmisInputExcelFieldAttribute`
- [x] **自定义字段**: `AmisFieldAttribute`

#### 字段验证支持
- [x] **必填验证**: `[Required]`
- [x] **长度验证**: `[StringLength]`
- [x] **范围验证**: `[Range]`
- [x] **正则验证**: `[RegularExpression]`
- [x] **特殊类型验证**:
  - [x] `DataType.EmailAddress`
  - [x] `DataType.Url`
  - [x] `DataType.PhoneNumber`
  - [x] `DataType.PostalCode`
  - [x] `DataType.ImageUrl`
- [x] **自定义错误消息**
- [x] **字段描述**: `[Description]`

### 4. 搜索筛选功能

#### 支持的筛选类型
- [x] **文本搜索**: `string` 类型
- [x] **布尔筛选**: `bool` 类型显示为开关
- [x] **枚举筛选**: `enum` 类型显示为下拉选择
- [x] **日期范围**: `DateTime[]` 类型
- [ ] **数值范围**: `int[]` 类型（待实现）

### 5. 操作按钮支持

#### 行操作
- [x] **查看**: 详情查看
- [x] **编辑**: 数据编辑
- [x] **删除**: 数据删除
- [x] **自定义操作**: `OperationAttribute`
- [x] **权限控制**: 基于权限显示/隐藏按钮

#### 头部操作
- [x] **新增**: 添加新数据
- [x] **批量导入**: Excel 批量导入
- [x] **导出**: 数据导出
- [x] **批量操作**: 批量删除等

### 6. 统计图表功能

- [x] **自动检测**: 自动识别统计方法
- [x] **日期筛选**: 支持日期范围选择
- [x] **图表网格**: 响应式图表布局
- [x] **多图表支持**: 同页面多个图表展示

#### 统计方法识别规则
- 方法名包含: `statistics`、`chart`、`report`、`analytics`
- 特性标记: `[Display(Name="统计")]`、`[Display(Name="图表")]`
- HTTP GET 方法

### 7. 缓存机制

- [x] **配置缓存**: 生成的 AMIS 配置自动缓存
- [x] **滑动过期**: 30 分钟滑动过期时间
- [x] **缓存键管理**: 基于控制器类型生成缓存键

## 使用示例

### 基础 CRUD 控制器

```csharp
[ApiController]
[Route("api/[controller]")]
[DisplayName("用户管理")]
public class UserController : ControllerBase
{
    [HttpGet]
    public async Task<PageList<UserDto>> GetUsers([FromQuery] UserQueryDto query)
    {
        // 实现列表查询
    }

    [HttpPost]
    public async Task<ApiResponse> CreateUser([FromBody] CreateUserDto dto)
    {
        // 实现用户创建
    }

    [HttpPut("{id}")]
    public async Task<ApiResponse> UpdateUser(int id, [FromBody] UpdateUserDto dto)
    {
        // 实现用户更新
    }

    [HttpDelete("{id}")]
    public async Task<ApiResponse> DeleteUser(int id)
    {
        // 实现用户删除
    }
}
```

### 数据传输对象示例

```csharp
public class UserDto
{
    [IgnoreColumn]
    public int Id { get; set; }

    [Display(Name = "用户名")]
    [Required(ErrorMessage = "用户名不能为空")]
    public string Username { get; set; }

    [Display(Name = "邮箱")]
    [DataType(DataType.EmailAddress)]
    public string Email { get; set; }

    [Display(Name = "头像")]
    [AvatarColumn]
    public string Avatar { get; set; }

    [Display(Name = "状态")]
    public UserStatus Status { get; set; }

    [Display(Name = "创建时间")]
    [DateColumn]
    public DateTime CreatedAt { get; set; }
}
```

### 查询对象示例

```csharp
public class UserQueryDto
{
    [Display(Name = "用户名")]
    public string Username { get; set; }

    [Display(Name = "状态")]
    public UserStatus? Status { get; set; }

    [Display(Name = "创建时间")]
    public DateTime[] CreatedAt { get; set; }
}
```

## 配置和扩展

### 服务注册

```csharp
services.AddAmis();
```

### 自定义字段工厂

```csharp
public class CustomFieldFactory : AmisFieldAttributeFactoryBase<CustomFieldAttribute>
{
    protected override JObject CreateFieldConfig(CustomFieldAttribute attribute, PropertyInfo property)
    {
        // 实现自定义字段配置
    }
}
```

## 待实现功能

### 列表功能
- [ ] 根据权限控制操作按钮
- [ ] 单元格样式自定义
- [ ] CSV 导出功能

### 表单功能
- [ ] 文件上传字段
- [ ] 城市选择器
- [ ] 颜色选择器
- [ ] 键值对编辑器
- [ ] 地理位置选择
- [ ] 评分组件
- [ ] 签名组件
- [ ] 日期范围验证
- [ ] 并发控制

### 搜索功能
- [ ] 数值范围筛选 (`int[]` 类型)

### 权限控制
- [ ] 表单字段权限控制
- [ ] 操作按钮权限控制

## 技术栈

- **.NET 9**: 基础框架
- **ASP.NET Core**: Web 框架
- **AMIS**: 前端 UI 框架
- **Newtonsoft.Json**: JSON 处理
- **反射机制**: 动态配置生成
- **特性驱动**: 声明式配置

## 项目结构

```
CodeSpirit.Amis/
├── AmisGenerator.cs              # 核心生成器
├── AmisCRUDConfigBuilder.cs      # CRUD 配置构建器
├── StatisticsConfigBuilder.cs    # 统计配置构建器
├── AmisContext.cs               # 上下文管理
├── CrudActions.cs               # CRUD 操作定义
├── Attributes/                  # 特性定义
│   ├── Columns/                # 列特性
│   ├── FormFields/             # 表单字段特性
│   └── Buttons/                # 按钮特性
├── Form/                       # 表单相关
│   └── Fields/                 # 字段工厂
├── Helpers/                    # 帮助类
└── Extensions/                 # 扩展方法
```