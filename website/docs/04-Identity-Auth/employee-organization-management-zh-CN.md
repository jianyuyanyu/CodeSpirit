# 职工管理及组织结构管理功能说明

## 概述

本文档介绍了 CodeSpirit.IdentityApi 中新增的职工管理和组织结构管理功能。这两个模块为系统提供了完整的组织架构和人员信息管理能力，支持多租户、软删除、审计追踪等核心特性。

## 功能特性

### 1. 组织结构管理（部门管理）

#### 核心功能
- **部门信息管理**：创建、修改、删除、查询部门信息
- **树形结构**：支持多层级部门树形结构
- **部门负责人**：可为每个部门指定负责人
- **批量操作**：支持批量导入和导出部门信息
- **状态管理**：支持启用/停用部门
- **多租户隔离**：同一租户内部门编码唯一

#### 数据模型

**Department 实体**
```csharp
public class Department : IMultiTenant, ICreationAuditable, IUpdateAuditable, ISoftDeleteAuditable
{
    public long Id { get; set; }                    // 部门ID
    public string TenantId { get; set; }            // 租户ID
    public string Code { get; set; }                // 部门编码（租户内唯一）
    public string Name { get; set; }                // 部门名称
    public long? ParentId { get; set; }             // 父部门ID
    public long? ManagerId { get; set; }            // 部门负责人ID
    public string? Description { get; set; }        // 部门描述
    public int SortOrder { get; set; }              // 排序号
    public bool IsActive { get; set; }              // 是否启用
    
    // 导航属性
    public Department? Parent { get; set; }         // 父部门
    public ICollection<Department> Children { get; set; }   // 子部门
    public Employee? Manager { get; set; }          // 负责人
    public ICollection<Employee> Employees { get; set; }    // 部门员工
    
    // 审计字段（继承自接口）
}
```

#### API 端点

**基础CRUD操作**
- `GET /api/departments` - 获取部门列表（支持分页和查询）
- `GET /api/departments/{id}` - 获取部门详情
- `POST /api/departments` - 创建部门
- `PUT /api/departments/{id}` - 更新部门
- `DELETE /api/departments/{id}` - 删除部门（软删除）

**特殊操作**
- `GET /api/departments/tree` - 获取部门树形结构
- `GET /api/departments/by-parent/{parentId}` - 获取指定父部门的子部门
- `POST /api/departments/batch` - 批量创建部门
- `GET /api/departments/import-template` - 下载导入模板
- `POST /api/departments/import` - 批量导入部门

#### 查询条件

**DepartmentQueryDto**
- `Name` - 部门名称（模糊匹配）
- `Code` - 部门编码（模糊匹配）
- `ParentId` - 父部门ID
- `IsActive` - 是否启用
- `Page` - 页码
- `PerPage` - 每页数量

### 2. 职工管理

#### 核心功能
- **职工信息管理**：创建、修改、删除、查询职工信息
- **部门关联**：职工与部门的关联管理
- **用户绑定**：职工可关联系统用户账号
- **在职状态**：支持在职、离职、休假等多种状态
- **批量操作**：支持批量导入和导出职工信息
- **多租户隔离**：同一租户内工号唯一

#### 数据模型

**Employee 实体**
```csharp
public class Employee : IMultiTenant, ICreationAuditable, IUpdateAuditable, ISoftDeleteAuditable
{
    public long Id { get; set; }                    // 职工ID
    public string TenantId { get; set; }            // 租户ID
    public string EmployeeNo { get; set; }          // 工号（租户内唯一）
    public string Name { get; set; }                // 姓名
    public long DepartmentId { get; set; }          // 部门ID
    public long? UserId { get; set; }               // 关联用户ID
    public string? Email { get; set; }              // 邮箱
    public string? Phone { get; set; }              // 电话
    public string? IdNo { get; set; }               // 身份证号
    public DateTime? BirthDate { get; set; }        // 出生日期
    public string? Gender { get; set; }             // 性别
    public string? Position { get; set; }           // 职位
    public DateTime? HireDate { get; set; }         // 入职日期
    public DateTime? TerminationDate { get; set; }  // 离职日期
    public EmploymentStatus EmploymentStatus { get; set; }  // 在职状态
    public string? Address { get; set; }            // 地址
    public string? Remarks { get; set; }            // 备注
    public bool IsActive { get; set; }              // 是否启用
    
    // 导航属性
    public Department Department { get; set; }      // 所属部门
    public ApplicationUser? User { get; set; }      // 关联用户
    
    // 审计字段（继承自接口）
}
```

**EmploymentStatus 枚举**
```csharp
public enum EmploymentStatus
{
    [Display(Name = "在职")]
    Active = 1,
    
    [Display(Name = "试用期")]
    Probation = 2,
    
    [Display(Name = "离职")]
    Resigned = 3,
    
    [Display(Name = "休假")]
    OnLeave = 4
}
```

#### API 端点

**基础CRUD操作**
- `GET /api/employees` - 获取职工列表（支持分页和查询）
- `GET /api/employees/{id}` - 获取职工详情
- `POST /api/employees` - 创建职工
- `PUT /api/employees/{id}` - 更新职工
- `DELETE /api/employees/{id}` - 删除职工（软删除）

**特殊操作**
- `GET /api/employees/by-department/{departmentId}` - 获取指定部门的职工
- `POST /api/employees/batch` - 批量创建职工
- `GET /api/employees/import-template` - 下载导入模板
- `POST /api/employees/import` - 批量导入职工

#### 查询条件

**EmployeeQueryDto**
- `Name` - 姓名（模糊匹配）
- `EmployeeNo` - 工号（模糊匹配）
- `DepartmentId` - 部门ID
- `Position` - 职位（模糊匹配）
- `EmploymentStatus` - 在职状态
- `IsActive` - 是否启用
- `Page` - 页码
- `PerPage` - 每页数量

## 技术实现

### 1. 数据库设计

#### 索引设计
- **租户感知的唯一索引**：确保部门编码和工号在租户内唯一
- **性能优化索引**：为常用查询字段创建索引
- **复合索引**：优化多条件查询性能

#### 关系设计
- 部门自引用关系（父子部门）
- 部门与职工的一对多关系
- 职工与用户的可选一对一关系
- 使用 `DeleteBehavior.Restrict` 防止意外级联删除

### 2. 服务层实现

#### BaseCRUDIService 继承
- 复用标准CRUD操作
- 自动处理软删除
- 自动设置审计字段
- 自动应用多租户过滤

#### 自定义业务逻辑
- 部门树形结构构建
- 职工部门关联验证
- 批量导入数据验证
- 业务规则校验

### 3. AutoMapper 配置

完整的 DTO 映射配置：
- Entity → Dto（查询）
- CreateDto → Entity（创建）
- UpdateDto → Entity（更新）
- BatchImportItemDto → Entity（批量导入）

### 4. 全局过滤器

自动应用的查询过滤器：
- **软删除过滤**：默认不查询已删除数据
- **多租户过滤**：自动按当前租户过滤
- **可禁用过滤**：支持管理员查看全部数据

## 使用示例

### 1. 创建部门

```http
POST /api/departments
Content-Type: application/json

{
  "code": "TECH",
  "name": "技术部",
  "parentId": null,
  "description": "负责技术研发工作",
  "sortOrder": 1,
  "isActive": true
}
```

### 2. 创建职工

```http
POST /api/employees
Content-Type: application/json

{
  "employeeNo": "EMP001",
  "name": "张三",
  "departmentId": 1,
  "email": "zhangsan@example.com",
  "phone": "13800138000",
  "position": "高级工程师",
  "hireDate": "2024-01-01",
  "employmentStatus": 1,
  "isActive": true
}
```

### 3. 查询部门职工

```http
GET /api/employees/by-department/1?page=1&perPage=20
```

### 4. 获取部门树

```http
GET /api/departments/tree
```

返回示例：
```json
[
  {
    "id": 1,
    "code": "ROOT",
    "name": "总部",
    "children": [
      {
        "id": 2,
        "code": "TECH",
        "name": "技术部",
        "children": [
          {
            "id": 3,
            "code": "DEV",
            "name": "开发组",
            "children": []
          }
        ]
      }
    ]
  }
]
```

### 5. 批量导入

#### 下载模板
```http
GET /api/employees/import-template
```

#### 导入数据
```http
POST /api/employees/import
Content-Type: multipart/form-data

file: employees.xlsx
```

## 权限控制

所有API端点都需要认证授权：
- 需要有效的 JWT Token
- 自动应用租户隔离
- 可配置基于角色的权限控制

## 最佳实践

### 1. 部门管理
- 规划好部门层级结构
- 使用有意义的部门编码
- 定期清理无效部门
- 谨慎删除有职工的部门

### 2. 职工管理
- 确保工号唯一性
- 及时更新职工状态
- 维护职工与用户的关联关系
- 使用批量导入提高效率

### 3. 数据迁移
- 使用导出功能备份数据
- 批量导入前验证数据格式
- 大量数据分批导入
- 保留导入日志

## 扩展功能

可以基于当前实现扩展以下功能：
1. 部门权限继承
2. 职工调岗记录
3. 职工考勤管理
4. 组织架构图可视化
5. 职工绩效评估

## 相关文档

- [CodeSpirit.Core核心框架](../01-Core-Docs/CodeSpirit.Core核心框架.md)
- [CodeSpirit.Authorization权限组件详解](CodeSpirit.Authorization权限组件详解.md)
- [CodeSpirit.IdentityApi身份认证服务](CodeSpirit.IdentityApi身份认证服务.md)
- [多租户架构设计](../05-Multi-Tenancy/)

## 版本历史

| 版本 | 日期 | 说明 |
|------|------|------|
| 1.0.0 | 2025-10-01 | 初始版本，实现基础的部门和职工管理功能 |

## 常见问题

**Q: 删除部门时提示有关联职工？**  
A: 需要先将职工转移到其他部门或删除职工，再删除部门。

**Q: 工号重复怎么办？**  
A: 工号在租户内必须唯一，系统会自动验证并拒绝重复的工号。

**Q: 如何批量修改职工部门？**  
A: 可以导出数据，修改部门ID后重新导入，或使用批量更新API。

**Q: 离职员工数据如何处理？**  
A: 建议将 EmploymentStatus 更新为 Resigned，设置 TerminationDate，而不是删除记录。

## 技术支持

如有问题或建议，请联系开发团队。

