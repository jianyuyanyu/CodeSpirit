# 表格卡片 (table) 使用指南

## 概述

表格卡片是 CodeSpirit Amis Cards 中用于展示表格数据的核心组件，基于 Amis CRUD 组件构建，提供丰富的数据展示、搜索、分页、排序和操作功能。适用于数据管理界面、列表展示、报表查看等场景。

## 基本用法

### 最简单的表格卡片

```javascript
{
    id: 'basic-table',
    type: 'table',
    title: '用户列表',
    columns: [
        { name: 'name', label: '姓名', type: 'text' },
        { name: 'email', label: '邮箱', type: 'text' },
        { name: 'status', label: '状态', type: 'text' }
    ],
    data: {
        items: [
            { name: '张三', email: 'zhangsan@example.com', status: '正常' },
            { name: '李四', email: 'lisi@example.com', status: '正常' }
        ],
        total: 2
    }
}
```

### 使用 API 数据源

```javascript
{
    id: 'api-table',
    type: 'table',
    title: '用户管理',
    subtitle: '系统用户列表',
    api: '/api/users',
    columns: [
        { name: 'id', label: 'ID', width: 80 },
        { name: 'name', label: '姓名', sortable: true },
        { name: 'email', label: '邮箱', copyable: true },
        { name: 'status', label: '状态', type: 'status' },
        { name: 'createTime', label: '创建时间', type: 'datetime' }
    ],
    showPager: true,
    perPage: 20
}
```

## 数据源配置

### 静态数据

```javascript
{
    id: 'static-table',
    type: 'table',
    title: '静态数据表格',
    data: {
        items: [
            {
                id: 1,
                name: '张三',
                email: 'zhangsan@example.com',
                status: 1,
                createTime: '2024-01-15 10:30:00'
            },
            {
                id: 2,
                name: '李四',
                email: 'lisi@example.com',
                status: 1,
                createTime: '2024-01-14 15:20:00'
            }
        ],
        total: 2
    },
    columns: [
        { name: 'id', label: 'ID', width: 80 },
        { name: 'name', label: '姓名' },
        { name: 'email', label: '邮箱' },
        { name: 'status', label: '状态', type: 'mapping', map: {
            1: '<span class="label label-success">正常</span>',
            0: '<span class="label label-danger">禁用</span>'
        }},
        { name: 'createTime', label: '创建时间', type: 'datetime' }
    ],
    showPager: false
}
```

### API 数据源

```javascript
{
    id: 'api-table',
    type: 'table',
    title: '用户列表',
    api: '/api/users',
    interval: 30000,  // 30秒自动刷新
    columns: [
        { name: 'name', label: '姓名', sortable: true },
        { name: 'email', label: '邮箱', copyable: true },
        { name: 'status', label: '状态', type: 'status', statusMap: {
            1: '<span class="label label-success">正常</span>',
            0: '<span class="label label-danger">禁用</span>'
        }}
    ],
    showPager: true,
    perPage: 20
}
```

### Source 数据源（从上下文获取）

```javascript
{
    id: 'source-table',
    type: 'table',
    title: '商品列表',
    source: '${products}',  // 从上下文中获取products数据
    columns: [
        { name: 'id', label: '商品ID', width: 100 },
        { name: 'name', label: '商品名称', sortable: true },
        { name: 'category', label: '分类', type: 'mapping', map: {
            'electronics': '电子产品',
            'clothing': '服装',
            'books': '图书'
        }},
        { name: 'price', label: '价格', type: 'tpl', tpl: '￥${price}' },
        { name: 'inventory', label: '库存', sortable: true }
    ],
    showPager: true,
    perPage: 15
}
```

## 列配置

### 基本列类型

```javascript
const columns = [
    // 文本列
    { 
        name: 'name', 
        label: '姓名', 
        type: 'text',
        width: 120
    },
    
    // 数字列
    { 
        name: 'age', 
        label: '年龄', 
        type: 'number',
        sortable: true
    },
    
    // 日期时间列
    { 
        name: 'createTime', 
        label: '创建时间', 
        type: 'datetime',
        format: 'YYYY-MM-DD HH:mm:ss'
    },
    
    // 状态列
    { 
        name: 'status', 
        label: '状态', 
        type: 'status',
        statusMap: {
            1: '<span class="label label-success">正常</span>',
            0: '<span class="label label-danger">禁用</span>'
        }
    },
    
    // 映射列
    { 
        name: 'type', 
        label: '类型', 
        type: 'mapping',
        map: {
            'admin': '管理员',
            'user': '普通用户',
            'guest': '访客'
        }
    },
    
    // 模板列
    { 
        name: 'avatar', 
        label: '头像', 
        type: 'tpl',
        tpl: '<img src="${avatar}" alt="${name}" style="width: 40px; height: 40px; border-radius: 50%;">'
    },
    
    // 图片列
    { 
        name: 'image', 
        label: '图片', 
        type: 'image',
        width: 100,
        thumbMode: 'cover'
    },
    
    // 链接列
    { 
        name: 'website', 
        label: '网站', 
        type: 'link',
        href: '${website}',
        blank: true
    },
    
    // 可复制列
    { 
        name: 'email', 
        label: '邮箱', 
        type: 'text',
        copyable: true
    }
];
```

### 高级列配置

```javascript
const advancedColumns = [
    // 带排序的列
    {
        name: 'name',
        label: '姓名',
        sortable: true,
        searchable: true
    },
    
    // 条件显示的列
    {
        name: 'salary',
        label: '薪资',
        type: 'number',
        visibleOn: '${role === "admin"}'
    },
    
    // 自定义渲染的列
    {
        name: 'score',
        label: '评分',
        type: 'tpl',
        tpl: '${score | stars}',  // 使用过滤器
        width: 100
    },
    
    // 进度条列
    {
        name: 'progress',
        label: '进度',
        type: 'progress',
        showLabel: true
    },
    
    // 标签列
    {
        name: 'tags',
        label: '标签',
        type: 'each',
        items: {
            type: 'tag',
            label: '${item}'
        }
    }
];
```

## 搜索功能

### 基本搜索配置

```javascript
{
    id: 'searchable-table',
    type: 'table',
    title: '用户管理',
    api: '/api/users',
    showSearch: true,
    searchFields: [
        {
            name: 'name',
            label: '姓名',
            type: 'input-text',
            placeholder: '请输入姓名'
        },
        {
            name: 'email',
            label: '邮箱',
            type: 'input-text',
            placeholder: '请输入邮箱'
        },
        {
            name: 'status',
            label: '状态',
            type: 'select',
            options: [
                { label: '全部', value: '' },
                { label: '正常', value: 1 },
                { label: '禁用', value: 0 }
            ]
        },
        {
            name: 'dateRange',
            label: '创建时间',
            type: 'input-date-range',
            format: 'YYYY-MM-DD'
        }
    ],
    columns: [
        // ... 列配置
    ]
}
```

### 高级搜索配置

```javascript
{
    searchFields: [
        // 文本搜索
        {
            name: 'keyword',
            label: '关键词',
            type: 'input-text',
            placeholder: '搜索姓名、邮箱、手机号',
            clearable: true
        },
        
        // 下拉选择
        {
            name: 'department',
            label: '部门',
            type: 'select',
            source: '/api/departments',
            clearable: true
        },
        
        // 多选
        {
            name: 'roles',
            label: '角色',
            type: 'select',
            multiple: true,
            options: [
                { label: '管理员', value: 'admin' },
                { label: '编辑', value: 'editor' },
                { label: '用户', value: 'user' }
            ]
        },
        
        // 日期范围
        {
            name: 'createTimeRange',
            label: '注册时间',
            type: 'input-date-range',
            format: 'YYYY-MM-DD'
        },
        
        // 数字范围
        {
            name: 'ageRange',
            label: '年龄范围',
            type: 'input-number-range',
            min: 0,
            max: 120
        }
    ]
}
```

## 操作功能

### 行操作按钮

```javascript
{
    id: 'action-table',
    type: 'table',
    title: '用户管理',
    api: '/api/users',
    columns: [
        { name: 'name', label: '姓名' },
        { name: 'email', label: '邮箱' },
        { name: 'status', label: '状态' }
    ],
    rowActions: [
        {
            type: 'button',
            label: '查看',
            level: 'link',
            icon: 'fa fa-eye',
            actionType: 'dialog',
            dialog: {
                title: '用户详情',
                size: 'lg',
                body: {
                    type: 'form',
                    mode: 'horizontal',
                    body: [
                        { name: 'name', label: '姓名', type: 'static' },
                        { name: 'email', label: '邮箱', type: 'static' },
                        { name: 'status', label: '状态', type: 'static' }
                    ]
                }
            }
        },
        {
            type: 'button',
            label: '编辑',
            level: 'link',
            icon: 'fa fa-edit',
            actionType: 'drawer',
            drawer: {
                title: '编辑用户',
                size: 'lg',
                body: {
                    type: 'form',
                    api: 'put:/api/users/${id}',
                    body: [
                        { name: 'name', label: '姓名', type: 'input-text', required: true },
                        { name: 'email', label: '邮箱', type: 'input-email', required: true },
                        { name: 'status', label: '状态', type: 'switch' }
                    ]
                }
            }
        },
        {
            type: 'button',
            label: '删除',
            level: 'link',
            className: 'text-danger',
            icon: 'fa fa-trash',
            actionType: 'ajax',
            api: 'delete:/api/users/${id}',
            confirmText: '确认要删除该用户吗？'
        }
    ]
}
```

### 表格工具栏

```javascript
{
    tableToolbar: [
        {
            type: 'button',
            label: '新增用户',
            level: 'primary',
            icon: 'fa fa-plus',
            actionType: 'drawer',
            drawer: {
                title: '新增用户',
                body: {
                    type: 'form',
                    api: 'post:/api/users',
                    body: [
                        { name: 'name', label: '姓名', type: 'input-text', required: true },
                        { name: 'email', label: '邮箱', type: 'input-email', required: true },
                        { name: 'password', label: '密码', type: 'input-password', required: true }
                    ]
                }
            }
        },
        {
            type: 'button',
            label: '导入数据',
            level: 'info',
            icon: 'fa fa-upload',
            actionType: 'dialog',
            dialog: {
                title: '批量导入用户',
                body: {
                    type: 'form',
                    api: 'post:/api/users/import',
                    body: [
                        {
                            name: 'file',
                            label: '选择文件',
                            type: 'input-file',
                            accept: '.xlsx,.xls,.csv',
                            required: true
                        }
                    ]
                }
            }
        },
        {
            type: 'button',
            label: '导出数据',
            level: 'default',
            icon: 'fa fa-download',
            actionType: 'download',
            api: '/api/users/export'
        }
    ]
}
```

### 批量操作

```javascript
{
    bulkActions: [
        {
            type: 'button',
            label: '批量启用',
            level: 'success',
            icon: 'fa fa-check',
            actionType: 'ajax',
            api: 'post:/api/users/batch-enable',
            confirmText: '确认要启用选中的用户吗？'
        },
        {
            type: 'button',
            label: '批量禁用',
            level: 'warning',
            icon: 'fa fa-ban',
            actionType: 'ajax',
            api: 'post:/api/users/batch-disable',
            confirmText: '确认要禁用选中的用户吗？'
        },
        {
            type: 'button',
            label: '批量删除',
            level: 'danger',
            icon: 'fa fa-trash',
            actionType: 'ajax',
            api: 'delete:/api/users/batch',
            confirmText: '确认要删除选中的用户吗？此操作不可恢复！'
        }
    ]
}
```

## 分页配置

### 基本分页

```javascript
{
    showPager: true,
    perPage: 20,
    perPageAvailable: [10, 20, 50, 100]
}
```

### 高级分页配置

```javascript
{
    showPager: true,
    perPage: 20,
    perPageAvailable: [10, 20, 50, 100],
    defaultParams: {
        perPage: 20,
        orderBy: 'createTime',
        orderDir: 'desc'
    },
    // 无限滚动分页
    loadType: 'more',
    // 简单分页（只有上一页下一页）
    simplePagination: false
}
```

## 表格样式配置

### 基本样式

```javascript
{
    tableConfig: {
        striped: true,          // 斑马纹
        bordered: false,        // 边框
        size: 'sm',            // 表格大小：sm、md、lg
        resizable: true,        // 可调整列宽
        columnsTogglable: false // 可切换列显示
    }
}
```

### 响应式配置

```javascript
{
    responsive: true,
    // 在小屏幕上隐藏某些列
    columns: [
        { name: 'name', label: '姓名', breakpoint: 'xs' },  // 在超小屏幕上始终显示
        { name: 'email', label: '邮箱', breakpoint: 'sm' }, // 在小屏幕及以上显示
        { name: 'phone', label: '电话', breakpoint: 'md' }, // 在中等屏幕及以上显示
        { name: 'address', label: '地址', breakpoint: 'lg' } // 在大屏幕及以上显示
    ]
}
```

## 实际应用示例

### 基于演示代码的表格示例

```javascript
const recentUsersTable = {
    id: 'recent-users',
    type: 'table',
    title: '最近用户',
    columns: [
        {
            name: 'name',
            label: '用户名',
            type: 'text'
        },
        {
            name: 'email',
            label: '邮箱',
            type: 'text'
        },
        {
            name: 'status',
            label: '状态',
            type: 'status',
            statusMap: {
                1: '<span class="label label-success">正常</span>',
                0: '<span class="label label-danger">禁用</span>'
            }
        },
        {
            name: 'createTime',
            label: '注册时间',
            type: 'datetime'
        }
    ],
    // 使用静态数据
    data: {
        items: [
            {
                name: '张三',
                email: 'zhangsan@example.com',
                status: 1,
                createTime: '2024-01-15 10:30:00'
            },
            {
                name: '李四',
                email: 'lisi@example.com',
                status: 1,
                createTime: '2024-01-14 15:20:00'
            },
            {
                name: '王五',
                email: 'wangwu@example.com',
                status: 0,
                createTime: '2024-01-13 09:15:00'
            }
        ],
        total: 3
    },
    showPager: false
};
```

### 高级表格配置示例

```javascript
const advancedOrderTable = {
    id: 'order-management',
    type: 'table',
    title: '订单管理',
    subtitle: '订单数据管理系统',
    api: '/api/orders',
    interval: 60000,  // 1分钟自动刷新
    
    // 搜索配置
    showSearch: true,
    searchFields: [
        {
            name: 'orderNo',
            label: '订单号',
            type: 'input-text',
            placeholder: '请输入订单号'
        },
        {
            name: 'status',
            label: '订单状态',
            type: 'select',
            options: [
                { label: '全部', value: '' },
                { label: '待支付', value: 'pending' },
                { label: '已支付', value: 'paid' },
                { label: '已发货', value: 'shipped' },
                { label: '已完成', value: 'completed' },
                { label: '已取消', value: 'cancelled' }
            ]
        },
        {
            name: 'dateRange',
            label: '下单时间',
            type: 'input-date-range',
            format: 'YYYY-MM-DD'
        }
    ],
    
    // 列配置
    columns: [
        { name: 'orderNo', label: '订单号', width: 160, copyable: true },
        { name: 'customerName', label: '客户姓名', sortable: true },
        { name: 'amount', label: '订单金额', type: 'tpl', tpl: '￥${amount}', sortable: true },
        { name: 'status', label: '状态', type: 'mapping', map: {
            'pending': '<span class="label label-warning">待支付</span>',
            'paid': '<span class="label label-info">已支付</span>',
            'shipped': '<span class="label label-primary">已发货</span>',
            'completed': '<span class="label label-success">已完成</span>',
            'cancelled': '<span class="label label-danger">已取消</span>'
        }},
        { name: 'createTime', label: '下单时间', type: 'datetime', sortable: true },
        { name: 'updateTime', label: '更新时间', type: 'datetime' }
    ],
    
    // 行操作
    rowActions: [
        { type: 'button', label: '查看', level: 'link', icon: 'fa fa-eye' },
        { type: 'button', label: '编辑', level: 'link', icon: 'fa fa-edit' },
        { type: 'button', label: '删除', level: 'link', className: 'text-danger', icon: 'fa fa-trash' }
    ],
    
    // 工具栏
    tableToolbar: [
        { type: 'button', label: '新建订单', level: 'primary', icon: 'fa fa-plus' },
        { type: 'button', label: '导出数据', level: 'default', icon: 'fa fa-download' }
    ],
    
    // 批量操作
    bulkActions: [
        { type: 'button', label: '批量确认', level: 'success' },
        { type: 'button', label: '批量取消', level: 'danger' }
    ],
    
    // 分页配置
    showPager: true,
    perPage: 20,
    perPageAvailable: [10, 20, 50, 100],
    
    // 表格样式
    tableConfig: {
        striped: true,
        bordered: false,
        size: 'sm',
        resizable: true
    }
};
```

## 配置参数参考

### 基本配置

| 参数 | 类型 | 默认值 | 描述 |
|------|------|--------|------|
| id | string | - | 卡片唯一标识（必填） |
| type | string | 'table' | 卡片类型（必填） |
| title | string | - | 卡片标题 |
| subtitle | string | - | 卡片副标题 |
| size | string | 'large' | 卡片尺寸 |
| theme | string | 'default' | 主题 |

### 数据源配置

| 参数 | 类型 | 默认值 | 描述 |
|------|------|--------|------|
| api | string | - | API接口地址 |
| source | string | - | 从上下文获取数据 |
| data | object | - | 静态数据 |
| interval | number | - | 自动刷新间隔（毫秒） |

### 列配置 (columns)

| 参数 | 类型 | 默认值 | 描述 |
|------|------|--------|------|
| name | string | - | 字段名（必填） |
| label | string | - | 列标题（必填） |
| type | string | 'text' | 列类型 |
| width | number | - | 列宽度 |
| sortable | boolean | false | 是否可排序 |
| searchable | boolean | false | 是否可搜索 |
| copyable | boolean | false | 是否可复制 |
| breakpoint | string | - | 响应式断点 |

### 搜索配置

| 参数 | 类型 | 默认值 | 描述 |
|------|------|--------|------|
| showSearch | boolean | false | 是否显示搜索栏 |
| searchFields | array | [] | 搜索字段配置 |

### 分页配置

| 参数 | 类型 | 默认值 | 描述 |
|------|------|--------|------|
| showPager | boolean | true | 是否显示分页 |
| perPage | number | 20 | 每页显示条数 |
| perPageAvailable | array | [10,20,50,100] | 可选每页条数 |

### 操作配置

| 参数 | 类型 | 默认值 | 描述 |
|------|------|--------|------|
| rowActions | array | [] | 行操作按钮 |
| tableToolbar | array | [] | 表格工具栏按钮 |
| bulkActions | array | [] | 批量操作按钮 |
| operationWidth | number | 120 | 操作列宽度 |

### 表格样式配置 (tableConfig)

| 参数 | 类型 | 默认值 | 描述 |
|------|------|--------|------|
| striped | boolean | true | 是否显示斑马纹 |
| bordered | boolean | false | 是否显示边框 |
| size | string | 'sm' | 表格大小：sm、md、lg |
| resizable | boolean | true | 是否可调整列宽 |
| columnsTogglable | boolean | false | 是否可切换列显示 |

## 样式定制

### CSS 变量

```css
:root {
    /* 表格基础样式 */
    --table-card-background: #ffffff;
    --table-card-border: 1px solid #e9ecef;
    --table-card-border-radius: 8px;
    --table-card-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
    
    /* 表头样式 */
    --table-header-background: #f8f9fa;
    --table-header-color: #495057;
    --table-header-font-weight: 600;
    
    /* 表格行样式 */
    --table-row-hover-background: #f5f5f5;
    --table-stripe-background: #f9f9f9;
    
    /* 操作按钮样式 */
    --table-action-button-spacing: 0.5rem;
    
    /* 分页样式 */
    --table-pagination-margin: 1rem 0;
}
```

### 自定义样式示例

```css
/* 自定义表格卡片样式 */
.custom-table-card {
    border-radius: 12px;
    overflow: hidden;
}

.custom-table-card .table-header {
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    color: white;
}

.custom-table-card .table tbody tr:hover {
    background-color: rgba(0, 123, 255, 0.05);
}

/* 响应式表格样式 */
@media (max-width: 768px) {
    .table-card .table-responsive {
        font-size: 0.875rem;
    }
    
    .table-card .btn-group-sm .btn {
        padding: 0.25rem 0.5rem;
        font-size: 0.75rem;
    }
}
```

## 最佳实践

### 1. 数据设计
- 合理设计API接口返回格式
- 使用统一的分页和排序参数
- 提供合适的默认排序

### 2. 列配置
- 重要信息放在前面的列
- 合理设置列宽度
- 使用合适的列类型

### 3. 性能优化
- 合理设置分页大小
- 避免一次加载过多数据
- 使用虚拟滚动处理大数据量

### 4. 用户体验
- 提供清晰的加载状态
- 合理的错误处理
- 友好的空数据提示

## 常见问题

### Q: 如何处理大量数据？
A: 使用分页、虚拟滚动，合理设置每页显示条数。

### Q: 如何自定义列显示？
A: 使用 type 为 'tpl' 的列，通过模板自定义显示内容。

### Q: 如何实现列的条件显示？
A: 使用 visibleOn 属性根据条件控制列的显示。

### Q: 如何处理图片列的显示？
A: 使用 type 为 'image' 的列，设置合适的 width 和 thumbMode。

## 参考资源

- [Amis CRUD 组件文档](https://aisuda.bce.baidu.com/amis/zh-CN/components/crud)
- [Amis Table 组件文档](https://aisuda.bce.baidu.com/amis/zh-CN/components/table)
- [演示页面](../demo/index.html)
- [配置示例](../configs/card-configs.js) 