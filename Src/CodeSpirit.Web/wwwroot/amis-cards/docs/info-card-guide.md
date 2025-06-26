# 信息卡片 (info) 使用指南

## 概述

信息卡片是 CodeSpirit Amis Cards 中用于展示静态信息内容的组件，支持多种信息展示格式，包括富文本、属性列表、描述列表等。适用于系统通知、帮助说明、详细信息展示等场景。

## 基本用法

### 最简单的信息卡片

```javascript
{
    id: 'basic-info',
    type: 'info',
    title: '基本信息',
    content: '这是一个简单的信息卡片，用于展示文本内容。'
}
```

### 带主题的信息卡片

```javascript
{
    id: 'themed-info',
    type: 'info',
    title: '系统通知',
    subtitle: '重要信息公告',
    theme: 'warning',
    content: '系统将于今晚进行维护升级，请提前保存您的工作。'
}
```

## 信息类型

### 1. 纯文本 (text)

```javascript
{
    id: 'text-info',
    type: 'info',
    title: '文本信息',
    infoType: 'text',
    content: '这是一段纯文本信息，会按照原始格式显示。'
}
```

### 2. HTML 内容 (html)

```javascript
{
    id: 'html-info',
    type: 'info',
    title: '系统通知',
    subtitle: '重要信息公告',
    theme: 'warning',
    infoType: 'html',
    content: `
        <div class="info-content">
            <h5>系统维护通知</h5>
            <p>系统将于今晚23:00-01:00进行维护升级，期间可能影响正常使用。</p>
            <p><strong>维护时间：</strong>2024年1月15日 23:00-01:00</p>
            <p><strong>影响范围：</strong>考试系统、用户管理</p>
        </div>
    `,
    actions: [
        {
            label: '查看详情',
            type: 'button',
            level: 'info',
            actionType: 'dialog',
            dialog: {
                title: '维护详情',
                body: '详细的维护说明...'
            }
        }
    ]
}
```

### 3. 属性列表 (properties)

基于演示代码中的系统信息示例：

```javascript
{
    id: 'system-info',
    type: 'info',
    title: '系统信息',
    subtitle: '当前系统状态',
    theme: 'info',
    infoType: 'properties',
    properties: [
        { label: '系统版本', content: 'CodeSpirit v2.0' },
        { label: '运行环境', content: '.NET 9.0' },
        { label: '启动时间', content: '2024-01-15 09:30:00' },
        { label: '运行时长', content: '15天 8小时' },
        { label: '服务器', content: 'Windows Server 2022' },
        { label: '数据库', content: 'SQL Server 2022' }
    ]
}
```

### 4. 列表信息 (list)

```javascript
{
    id: 'feature-list',
    type: 'info',
    title: '功能特性',
    infoType: 'list',
    items: [
        '支持多种卡片类型',
        '响应式设计',
        '主题切换',
        '图标支持',
        '数据绑定',
        '事件处理'
    ]
}
```

### 5. 定义列表 (definition)

```javascript
{
    id: 'definition-info',
    type: 'info',
    title: '术语定义',
    infoType: 'definition',
    definitions: [
        {
            term: 'Amis',
            description: '一个低代码前端框架，通过 JSON 配置生成页面'
        },
        {
            term: 'Cards',
            description: '基于卡片设计理念的UI组件，用于数据展示'
        },
        {
            term: 'Renderer',
            description: '渲染器，负责将配置转换为实际的UI组件'
        }
    ]
}
```

### 6. JSON 格式 (json)

```javascript
{
    id: 'json-info',
    type: 'info',
    title: '配置信息',
    infoType: 'json',
    data: {
        version: '2.0.0',
        environment: 'production',
        features: ['charts', 'tables', 'stats'],
        config: {
            theme: 'default',
            debug: false
        }
    }
}
```

### 7. Markdown 格式 (markdown)

```javascript
{
    id: 'markdown-info',
    type: 'info',
    title: '使用说明',
    infoType: 'markdown',
    content: `
# 快速开始

## 安装依赖

\`\`\`bash
npm install amis-cards
\`\`\`

## 基本使用

1. 引入样式文件
2. 创建卡片实例
3. 渲染到页面

> **注意**: 请确保已正确配置 Amis 环境

**支持的功能**:
- 统计卡片
- 图表卡片  
- 表格卡片
- 信息卡片
    `
}
```

## 操作按钮

### 基本操作按钮

```javascript
{
    id: 'info-with-actions',
    type: 'info',
    title: '操作示例',
    content: '您可以通过下面的按钮执行相关操作。',
    actions: [
        {
            label: '确认',
            type: 'button',
            level: 'primary',
            actionType: 'ajax',
            api: '/api/confirm'
        },
        {
            label: '取消',
            type: 'button',
            level: 'default',
            actionType: 'close'
        }
    ]
}
```

### 对话框操作

```javascript
{
    actions: [
        {
            label: '查看详情',
            type: 'button',
            level: 'info',
            actionType: 'dialog',
            dialog: {
                title: '详细信息',
                size: 'lg',
                body: {
                    type: 'form',
                    mode: 'horizontal',
                    body: [
                        { name: 'name', label: '名称', type: 'static' },
                        { name: 'description', label: '描述', type: 'static' }
                    ]
                }
            }
        }
    ]
}
```

### 抽屉操作

```javascript
{
    actions: [
        {
            label: '编辑配置',
            type: 'button',
            level: 'primary',
            actionType: 'drawer',
            drawer: {
                title: '编辑信息',
                size: 'lg',
                body: {
                    type: 'form',
                    api: 'put:/api/info/${id}',
                    body: [
                        { name: 'title', label: '标题', type: 'input-text' },
                        { name: 'content', label: '内容', type: 'textarea' }
                    ]
                }
            }
        }
    ]
}
```

### 页面跳转

```javascript
{
    actions: [
        {
            label: '访问官网',
            type: 'button',
            level: 'link',
            actionType: 'url',
            url: 'https://example.com',
            blank: true
        }
    ]
}
```

## 实际应用示例

### 基于演示代码的系统信息

```javascript
const systemInfoCard = {
    id: 'system-info',
    type: 'info',
    title: '系统信息',
    subtitle: '当前系统运行状态',
    theme: 'info',
    infoType: 'properties',
    properties: [
        {
            label: '系统版本',
            content: 'CodeSpirit v2.0'
        },
        {
            label: '运行环境',
            content: '.NET 9.0'
        },
        {
            label: '启动时间',
            content: '2024-01-15 09:30:00'
        },
        {
            label: '运行时长',
            content: '15天 8小时'
        }
    ]
};
```

### 通知公告信息

```javascript
const noticeInfoCard = {
    id: 'system-notice',
    type: 'info',
    title: '系统通知',
    subtitle: '重要信息公告',
    theme: 'warning',
    infoType: 'html',
    content: `
        <div class="notice-content">
            <div class="notice-header">
                <i class="fa fa-exclamation-triangle"></i>
                <h5>系统维护通知</h5>
            </div>
            <div class="notice-body">
                <p>为了提升系统性能和稳定性，我们将进行例行维护。</p>
                <ul>
                    <li><strong>维护时间：</strong>2024年1月15日 23:00 - 01:00</li>
                    <li><strong>影响范围：</strong>考试系统、用户管理</li>
                    <li><strong>预计时长：</strong>2小时</li>
                </ul>
                <p class="text-muted">维护期间可能出现服务不可用，请提前保存重要数据。</p>
            </div>
        </div>
    `,
    actions: [
        {
            label: '了解详情',
            type: 'button',
            level: 'info',
            icon: 'fa fa-info-circle',
            actionType: 'dialog',
            dialog: {
                title: '维护详情',
                size: 'lg',
                body: '详细的维护计划和注意事项...'
            }
        },
        {
            label: '订阅通知',
            type: 'button',
            level: 'primary',
            icon: 'fa fa-bell',
            actionType: 'ajax',
            api: 'post:/api/subscribe-notice'
        }
    ]
};
```

### 帮助说明信息

```javascript
const helpInfoCard = {
    id: 'help-info',
    type: 'info',
    title: '使用帮助',
    subtitle: 'AmisCards 快速入门',
    theme: 'primary',
    infoType: 'markdown',
    content: `
## 快速开始

### 1. 创建实例

\`\`\`javascript
const cards = AmisCards.create({
    container: '#container',
    theme: 'default'
});
\`\`\`

### 2. 渲染卡片

\`\`\`javascript
await cards.render([
    {
        id: 'demo',
        type: 'stat',
        title: '演示卡片',
        data: { value: 100 }
    }
]);
\`\`\`

### 3. 常用配置

- **theme**: 主题配色
- **size**: 卡片尺寸
- **autoRefresh**: 自动刷新

> 💡 **提示**: 更多配置选项请参考完整文档

### 联系我们

如有问题，请联系技术支持：support@codespirit.com
    `,
    actions: [
        {
            label: '查看完整文档',
            type: 'button',
            level: 'primary',
            actionType: 'url',
            url: '/docs',
            blank: true
        }
    ]
};
```

### 状态报告信息

```javascript
const statusReportCard = {
    id: 'status-report',
    type: 'info',
    title: '系统状态报告',
    subtitle: '实时监控数据',
    theme: 'success',
    infoType: 'properties',
    properties: [
        {
            label: '服务状态',
            content: '<span class="label label-success">正常运行</span>',
            type: 'html'
        },
        {
            label: 'CPU 使用率',
            content: '15.6%',
            description: '当前系统负载较低'
        },
        {
            label: '内存使用率',
            content: '45.2%',
            description: '内存使用正常'
        },
        {
            label: '磁盘空间',
            content: '2.8TB / 5.0TB',
            description: '剩余空间充足'
        },
        {
            label: '网络延迟',
            content: '12ms',
            description: '网络连接良好'
        },
        {
            label: '最后检查时间',
            content: '2024-01-15 14:30:00'
        }
    ],
    actions: [
        {
            label: '刷新状态',
            type: 'button',
            level: 'info',
            icon: 'fa fa-refresh',
            actionType: 'ajax',
            api: '/api/system/status/refresh'
        },
        {
            label: '详细报告',
            type: 'button',
            level: 'default',
            icon: 'fa fa-file-alt',
            actionType: 'download',
            api: '/api/system/status/report'
        }
    ]
};
```

## 配置参数参考

### 基本配置

| 参数 | 类型 | 默认值 | 描述 |
|------|------|--------|------|
| id | string | - | 卡片唯一标识（必填） |
| type | string | 'info' | 卡片类型（必填） |
| title | string | - | 卡片标题 |
| subtitle | string | - | 卡片副标题 |
| theme | string | 'info' | 主题：default, primary, success, warning, danger, info |
| size | string | 'medium' | 尺寸：small, medium, large |

### 信息内容配置

| 参数 | 类型 | 默认值 | 描述 |
|------|------|--------|------|
| infoType | string | 'text' | 信息类型：text, html, properties, list, definition, json, markdown |
| content | string | - | 文本或HTML内容 |
| properties | array | [] | 属性列表（infoType为properties时使用） |
| items | array | [] | 列表项（infoType为list时使用） |
| definitions | array | [] | 定义列表（infoType为definition时使用） |
| data | object | - | JSON数据（infoType为json时使用） |

### 属性列表配置 (properties)

| 参数 | 类型 | 默认值 | 描述 |
|------|------|--------|------|
| label | string | - | 属性标签（必填） |
| content | string | - | 属性值（必填） |
| type | string | 'text' | 内容类型：text, html |
| description | string | - | 属性描述 |

### 操作按钮配置 (actions)

| 参数 | 类型 | 默认值 | 描述 |
|------|------|--------|------|
| label | string | - | 按钮文本（必填） |
| type | string | 'button' | 按钮类型 |
| level | string | 'default' | 按钮级别：primary, info, success, warning, danger, default, link |
| icon | string | - | 按钮图标 |
| actionType | string | - | 操作类型：ajax, dialog, drawer, url, close |
| api | string | - | API地址（actionType为ajax时使用） |
| url | string | - | 跳转地址（actionType为url时使用） |
| dialog | object | - | 对话框配置（actionType为dialog时使用） |
| drawer | object | - | 抽屉配置（actionType为drawer时使用） |

## 样式定制

### CSS 变量

```css
:root {
    /* 信息卡片基础样式 */
    --info-card-background: #ffffff;
    --info-card-border: 1px solid #e9ecef;
    --info-card-border-radius: 8px;
    --info-card-padding: 1.5rem;
    --info-card-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
    
    /* 标题样式 */
    --info-title-font-size: 1.25rem;
    --info-title-font-weight: 600;
    --info-title-color: #333;
    --info-subtitle-font-size: 0.9rem;
    --info-subtitle-color: #666;
    
    /* 内容样式 */
    --info-content-font-size: 0.9rem;
    --info-content-line-height: 1.6;
    --info-content-color: #555;
    
    /* 属性列表样式 */
    --info-property-label-width: 120px;
    --info-property-label-color: #666;
    --info-property-value-color: #333;
    --info-property-spacing: 0.75rem;
    
    /* 操作按钮样式 */
    --info-action-spacing: 0.5rem;
    --info-action-margin-top: 1rem;
}
```

### 主题样式

```css
/* 成功主题 */
.amis-cards-theme-success .info-card {
    border-left: 4px solid #28a745;
}

.amis-cards-theme-success .info-card .card-header {
    background-color: rgba(40, 167, 69, 0.1);
    color: #155724;
}

/* 警告主题 */
.amis-cards-theme-warning .info-card {
    border-left: 4px solid #ffc107;
}

.amis-cards-theme-warning .info-card .card-header {
    background-color: rgba(255, 193, 7, 0.1);
    color: #856404;
}

/* 危险主题 */
.amis-cards-theme-danger .info-card {
    border-left: 4px solid #dc3545;
}

.amis-cards-theme-danger .info-card .card-header {
    background-color: rgba(220, 53, 69, 0.1);
    color: #721c24;
}
```

### 自定义样式示例

```css
/* 自定义信息卡片样式 */
.custom-info-card {
    background: linear-gradient(135deg, #f5f7fa 0%, #c3cfe2 100%);
    border-radius: 12px;
    box-shadow: 0 4px 20px rgba(0, 0, 0, 0.1);
}

.custom-info-card .info-content {
    background: rgba(255, 255, 255, 0.9);
    border-radius: 8px;
    padding: 1rem;
    margin-top: 1rem;
}

/* 属性列表自定义样式 */
.info-properties .property-item {
    border-bottom: 1px solid #f0f0f0;
    padding: 0.75rem 0;
}

.info-properties .property-item:last-child {
    border-bottom: none;
}

.info-properties .property-label {
    font-weight: 600;
    color: #495057;
}

.info-properties .property-value {
    color: #6c757d;
}

/* 响应式样式 */
@media (max-width: 768px) {
    .info-card {
        padding: 1rem;
    }
    
    .info-properties .property-item {
        flex-direction: column;
        align-items: flex-start;
    }
    
    .info-properties .property-label {
        margin-bottom: 0.25rem;
    }
}
```

## 最佳实践

### 1. 内容组织
- 使用清晰的标题和副标题
- 合理组织信息层次结构
- 避免信息过载

### 2. 视觉设计
- 选择合适的主题颜色
- 保持一致的样式风格
- 注意内容的可读性

### 3. 交互设计
- 提供有意义的操作按钮
- 使用合适的操作类型
- 考虑用户的操作流程

### 4. 内容更新
- 定期更新过时信息
- 使用动态数据源
- 提供刷新机制

## 常见问题

### Q: 如何在信息卡片中显示格式化文本？
A: 使用 infoType 为 'html' 或 'markdown'，可以支持富文本格式。

### Q: 如何实现属性列表的动态更新？
A: 可以结合 API 数据源，通过轮询或事件触发更新属性列表。

### Q: 如何自定义属性列表的显示样式？
A: 通过 CSS 变量或自定义 CSS 类来调整属性列表的样式。

### Q: 操作按钮如何与父组件通信？
A: 使用 actionType 为 'ajax' 或自定义事件处理器来实现组件间通信。

## 参考资源

- [Amis 文档 - 静态展示](https://aisuda.bce.baidu.com/amis/zh-CN/components/static)
- [Markdown 语法指南](https://www.markdownguide.org/)
- [演示页面](../demo/index.html)
- [配置示例](../configs/card-configs.js) 