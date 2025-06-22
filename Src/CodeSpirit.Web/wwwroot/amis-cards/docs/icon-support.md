# AmisCards 图标支持功能

AmisCards V2.0 新增了强大的图标支持功能，基于 Amis 框架的官方图标体系，为统计卡片提供了丰富的视觉表现力。

## 功能概述

### 支持的图标类型

1. **FontAwesome 图标**：完整支持 FontAwesome 4.x 图标库
2. **URL 图标**：支持 HTTP/HTTPS URL、协议相对 URL 和 Data URL
3. **自定义图标**：通过 CSS 类名支持自定义图标字体

### 图标配置选项

- **图标位置**：支持左、右、上、下四个位置
- **图标尺寸**：提供 5 种预设尺寸（xs, sm, md, lg, xl）
- **图标样式**：支持颜色、背景、边框等样式定制
- **主题适配**：自动适配不同主题的配色方案

## 基本使用

### 简单图标配置

```javascript
{
    id: 'user-count',
    type: 'stat',
    title: '用户统计',
    data: {
        value: 1234,
        label: '总用户数',
        icon: 'users'  // FontAwesome 图标
    }
}
```

### 完整图标配置

```javascript
{
    id: 'advanced-stat',
    type: 'stat',
    title: '高级统计',
    data: {
        value: 9876,
        label: '活跃用户',
        
        // 图标基本设置
        icon: 'user-friends',
        iconColor: '#007bff',
        iconSize: 'lg',
        iconPosition: 'left',
        
        // 图标样式设置
        iconBackground: 'rgba(0, 123, 255, 0.1)',
        iconBorder: true
    }
}
```

## 图标位置

### 左侧图标（默认）

```javascript
{
    data: {
        icon: 'chart-bar',
        iconPosition: 'left'  // 默认值，可省略
    }
}
```

适用场景：
- 标准的统计卡片
- 数值较短的情况
- 需要保持布局紧凑的场景

### 右侧图标

```javascript
{
    data: {
        icon: 'arrow-right',
        iconPosition: 'right'
    }
}
```

适用场景：
- 数值较长需要更多空间
- 表示方向或流程的统计
- 与左侧内容形成平衡

### 顶部图标

```javascript
{
    data: {
        icon: 'crown',
        iconPosition: 'top',
        iconSize: 'xl'  // 顶部图标建议使用较大尺寸
    }
}
```

适用场景：
- 需要突出图标的重要性
- 创建视觉焦点
- 适合重要指标展示

### 底部图标

```javascript
{
    data: {
        icon: 'info-circle',
        iconPosition: 'bottom'
    }
}
```

适用场景：
- 辅助信息的展示
- 不干扰主要数值的阅读
- 装饰性图标

## 图标尺寸

### 尺寸对照表

| 尺寸 | 容器大小 | 字体大小 | 适用场景 |
|------|---------|---------|----------|
| xs   | 24×24px | 12px    | 紧凑布局、移动端 |
| sm   | 32×32px | 16px    | 小型卡片 |
| md   | 48×48px | 24px    | 标准卡片（默认） |
| lg   | 64×64px | 32px    | 大型卡片 |
| xl   | 80×80px | 40px    | 重要指标、顶部图标 |

### 尺寸选择建议

```javascript
// 移动端或紧凑布局
{ iconSize: 'xs' }

// 标准桌面端
{ iconSize: 'md' }

// 重要指标突出显示
{ iconSize: 'lg' }

// 顶部图标或特殊场景
{ iconSize: 'xl' }
```

## 图标样式

### 颜色配置

```javascript
{
    data: {
        icon: 'heart',
        iconColor: '#e74c3c',           // 直接指定颜色
        iconColor: 'var(--danger)',     // 使用 CSS 变量
        iconColor: 'currentColor'       // 继承当前文本颜色
    }
}
```

### 背景配置

```javascript
{
    data: {
        icon: 'shield-alt',
        iconBackground: '#f8f9fa',                    // 纯色背景
        iconBackground: 'rgba(0, 123, 255, 0.1)',    // 半透明背景
        iconBackground: 'linear-gradient(45deg, #007bff, #28a745)'  // 渐变背景
    }
}
```

### 边框配置

```javascript
{
    data: {
        icon: 'user-shield',
        iconBorder: true,                    // 启用边框
        iconColor: '#007bff',               // 边框颜色会自动匹配图标颜色
        iconBackground: 'rgba(0, 123, 255, 0.1)'
    }
}
```

## 图标类型详解

### FontAwesome 图标

支持多种写法：

```javascript
// 简写形式（推荐）
{ icon: 'users' }

// 标准形式
{ icon: 'fa-users' }

// 完整形式
{ icon: 'fa fa-users' }
```

常用图标示例：

```javascript
// 用户相关
{ icon: 'user' }          // 单个用户
{ icon: 'users' }         // 多个用户
{ icon: 'user-plus' }     // 新增用户
{ icon: 'user-check' }    // 验证用户

// 数据相关
{ icon: 'chart-bar' }     // 柱状图
{ icon: 'chart-line' }    // 折线图
{ icon: 'chart-pie' }     // 饼图
{ icon: 'database' }      // 数据库

// 系统相关
{ icon: 'server' }        // 服务器
{ icon: 'cpu' }          // 处理器
{ icon: 'memory' }       // 内存
{ icon: 'hdd' }          // 硬盘

// 业务相关
{ icon: 'dollar-sign' }   // 金钱
{ icon: 'shopping-cart' } // 购物车
{ icon: 'truck' }        // 物流
{ icon: 'calendar' }     // 日历
```

### URL 图标

```javascript
// HTTP/HTTPS URL
{
    icon: 'https://cdn.example.com/icons/custom-icon.svg'
}

// 协议相对 URL
{
    icon: '//cdn.example.com/icons/custom-icon.png'
}

// Data URL（SVG）
{
    icon: 'data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iMjQiIGhlaWdodD0iMjQiIHZpZXdCb3g9IjAgMCAyNCAyNCIgZmlsbD0ibm9uZSIgeG1sbnM9Imh0dHA6Ly93d3cudzMub3JnLzIwMDAvc3ZnIj4KPHBhdGggZD0iTTEyIDJMMTMuMDkgOC4yNkwyMCA5TDEzLjA5IDE1Ljc0TDEyIDIyTDEwLjkxIDE1Ljc0TDQgOUwxMC45MSA4LjI2TDEyIDJaIiBmaWxsPSIjMjhhNzQ1Ii8+Cjwvc3ZnPgo='
}
```

## 主题适配

### 自动主题适配

图标会自动适配卡片主题：

```javascript
{
    theme: 'primary',
    data: {
        icon: 'star',
        iconBackground: true  // 会自动使用主题色的浅色背景
    }
}
```

### 主题色对照

| 主题 | 图标颜色 | 背景颜色 |
|------|---------|----------|
| primary | #007bff | rgba(0, 123, 255, 0.1) |
| success | #28a745 | rgba(40, 167, 69, 0.1) |
| warning | #ffc107 | rgba(255, 193, 7, 0.1) |
| danger | #dc3545 | rgba(220, 53, 69, 0.1) |
| info | #17a2b8 | rgba(23, 162, 184, 0.1) |

### 深色主题适配

```javascript
// 深色主题下的图标会自动调整
{
    theme: 'dark',
    data: {
        icon: 'moon',
        iconColor: '#ffffff',
        iconBackground: 'rgba(255, 255, 255, 0.1)'
    }
}
```

## 响应式设计

### 移动端适配

```javascript
{
    data: {
        icon: 'mobile-alt',
        iconSize: 'sm',  // 移动端使用较小尺寸
        iconPosition: 'left'  // 避免使用 top/bottom 位置
    }
}
```

### 断点适配

系统会在不同屏幕尺寸下自动调整图标：

- **小屏幕（<576px）**：xl → lg，lg → md
- **中屏幕（576px-768px）**：保持原始尺寸
- **大屏幕（>768px）**：保持原始尺寸

## 性能优化

### 图标加载优化

1. **优先使用 FontAwesome**
   ```javascript
   // 推荐：无需额外网络请求
   { icon: 'users' }
   
   // 避免：需要网络请求
   { icon: 'https://example.com/icon.svg' }
   ```

2. **URL 图标优化**
   ```javascript
   // 使用 CDN
   { icon: 'https://cdn.jsdelivr.net/npm/@fortawesome/fontawesome-free@5.15.4/svgs/solid/users.svg' }
   
   // 使用 Data URL（小图标）
   { icon: 'data:image/svg+xml;base64,...' }
   ```

3. **图标缓存**
   ```javascript
   // 系统会自动缓存已加载的图标
   // 相同 URL 的图标只会加载一次
   ```

### 渲染性能

1. **批量设置图标**
   ```javascript
   // 推荐：批量渲染
   await cards.render([
       { icon: 'user' },
       { icon: 'chart' },
       { icon: 'server' }
   ]);
   
   // 避免：单独渲染
   await cards.render([{ icon: 'user' }]);
   await cards.render([{ icon: 'chart' }]);
   ```

2. **避免频繁更新**
   ```javascript
   // 推荐：一次性更新所有属性
   card.updateData({
       value: 1234,
       icon: 'users',
       iconColor: '#007bff'
   });
   
   // 避免：多次更新
   card.updateData({ value: 1234 });
   card.updateData({ icon: 'users' });
   card.updateData({ iconColor: '#007bff' });
   ```

## 常见问题

### Q: 图标不显示怎么办？

A: 检查以下几点：
1. FontAwesome 是否正确加载
2. 图标名称是否正确
3. URL 图标是否可访问
4. 检查控制台是否有错误信息

### Q: 如何自定义图标字体？

A: 可以通过 CSS 类名支持：

```javascript
{
    icon: 'custom-icon-class',  // 自定义 CSS 类
    iconColor: '#007bff'
}
```

```css
.custom-icon-class::before {
    content: '\f123';  /* 自定义字体的 Unicode */
    font-family: 'CustomIconFont';
}
```

### Q: 图标在不同主题下显示异常？

A: 确保使用相对颜色值：

```javascript
// 推荐：使用主题相关的颜色
{
    iconColor: 'var(--amis-cards-primary)',
    iconBackground: 'var(--amis-cards-gray-100)'
}

// 避免：使用固定颜色
{
    iconColor: '#007bff',  // 在深色主题下可能不合适
    iconBackground: '#ffffff'
}
```

## 最佳实践

### 1. 图标选择原则

- **语义化**：选择与数据内容相关的图标
- **一致性**：同类数据使用相似的图标风格
- **简洁性**：避免过于复杂的图标

### 2. 布局建议

- **左侧图标**：适合大多数场景
- **顶部图标**：用于重要指标
- **右侧图标**：平衡长数值
- **底部图标**：辅助信息

### 3. 尺寸选择

- **移动端**：优先使用 xs、sm
- **桌面端**：标准使用 md、lg
- **重要指标**：可使用 xl

### 4. 颜色搭配

- **主题一致**：与卡片主题保持一致
- **对比度**：确保图标清晰可见
- **无障碍性**：考虑色盲用户的需求

## 示例集合

### 业务场景示例

```javascript
// 用户统计
{
    icon: 'users',
    iconColor: '#007bff',
    iconSize: 'lg',
    iconPosition: 'left',
    iconBackground: 'rgba(0, 123, 255, 0.1)'
}

// 销售额
{
    icon: 'dollar-sign',
    iconColor: '#28a745',
    iconSize: 'lg',
    iconPosition: 'left',
    iconBackground: 'rgba(40, 167, 69, 0.1)'
}

// 系统性能
{
    icon: 'tachometer-alt',
    iconColor: '#17a2b8',
    iconSize: 'md',
    iconPosition: 'left',
    iconBackground: 'rgba(23, 162, 184, 0.1)'
}

// 错误率
{
    icon: 'exclamation-triangle',
    iconColor: '#dc3545',
    iconSize: 'md',
    iconPosition: 'left',
    iconBackground: 'rgba(220, 53, 69, 0.1)'
}
```

通过这些丰富的图标配置选项，AmisCards 能够为您的数据展示提供更加直观和美观的视觉体验。 