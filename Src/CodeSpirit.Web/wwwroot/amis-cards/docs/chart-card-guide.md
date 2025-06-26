# 图表卡片 (chart) 使用指南

## 概述

图表卡片是 CodeSpirit Amis Cards 中用于展示数据可视化的核心组件，基于 ECharts 构建，支持多种图表类型。适用于数据分析、趋势展示、统计报表等场景。

## 基本用法

### 最简单的图表卡片

```javascript
{
    id: 'basic-chart',
    type: 'chart',
    title: '基础图表',
    chartType: 'line',
    height: 300,
    series: [
        {
            name: '数据系列',
            data: [120, 132, 101, 134, 90, 230, 210]
        }
    ],
    xAxisData: ['周一', '周二', '周三', '周四', '周五', '周六', '周日']
}
```

### 带主题的图表卡片

```javascript
{
    id: 'themed-chart',
    type: 'chart',
    title: '访问量趋势',
    subtitle: '最近7天数据',
    size: 'large',
    theme: 'info',
    chartType: 'line',
    height: 400,
    series: [
        {
            name: '访问量',
            data: [1200, 1320, 1010, 1340, 900, 2300, 2100],
            smooth: true,
            areaStyle: {}
        }
    ],
    xAxisData: ['周一', '周二', '周三', '周四', '周五', '周六', '周日']
}
```

## 支持的图表类型

### 1. 折线图 (line)

基于演示代码中的访问量趋势示例：

```javascript
{
    id: 'traffic-chart',
    type: 'chart',
    title: '访问量趋势',
    chartType: 'line',
    height: 300,
    series: [
        {
            name: '访问量',
            data: [120, 132, 101, 134, 90, 230, 210],
            smooth: true,
            lineStyle: {
                width: 3,
                color: '#007bff'
            },
            areaStyle: {
                color: {
                    type: 'linear',
                    x: 0, y: 0, x2: 0, y2: 1,
                    colorStops: [
                        { offset: 0, color: 'rgba(0, 123, 255, 0.3)' },
                        { offset: 1, color: 'rgba(0, 123, 255, 0.05)' }
                    ]
                }
            }
        }
    ],
    xAxisData: ['周一', '周二', '周三', '周四', '周五', '周六', '周日']
}
```

### 2. 柱状图 (bar)

```javascript
{
    id: 'sales-bar-chart',
    type: 'chart',
    title: '月度销售额',
    subtitle: '各产品线销售对比',
    chartType: 'bar',
    height: 350,
    series: [
        {
            name: '电子产品',
            data: [2340, 2670, 2450, 2880, 3200, 3100, 2950],
            itemStyle: { color: '#007bff' }
        },
        {
            name: '服装',
            data: [1560, 1890, 1700, 2100, 2300, 2200, 2050],
            itemStyle: { color: '#28a745' }
        },
        {
            name: '图书',
            data: [880, 920, 850, 1100, 1200, 1150, 1080],
            itemStyle: { color: '#ffc107' }
        }
    ],
    xAxisData: ['1月', '2月', '3月', '4月', '5月', '6月', '7月']
}
```

### 3. 饼图 (pie)

```javascript
{
    id: 'category-pie-chart',
    type: 'chart',
    title: '销售分类占比',
    subtitle: '按产品类别统计',
    chartType: 'pie',
    height: 400,
    series: [
        {
            name: '销售占比',
            type: 'pie',
            radius: ['40%', '70%'],
            avoidLabelOverlap: false,
            label: {
                show: false,
                position: 'center'
            },
            emphasis: {
                label: {
                    show: true,
                    fontSize: '30',
                    fontWeight: 'bold'
                }
            },
            labelLine: {
                show: false
            },
            data: [
                { value: 1048, name: '电子产品', itemStyle: { color: '#007bff' } },
                { value: 735, name: '服装', itemStyle: { color: '#28a745' } },
                { value: 580, name: '图书', itemStyle: { color: '#ffc107' } },
                { value: 484, name: '家居', itemStyle: { color: '#17a2b8' } },
                { value: 300, name: '运动', itemStyle: { color: '#dc3545' } }
            ]
        }
    ]
}
```

### 4. 面积图 (area)

```javascript
{
    id: 'area-chart',
    type: 'chart',
    title: '用户增长趋势',
    chartType: 'area',
    height: 300,
    series: [
        {
            name: '新增用户',
            data: [1200, 1500, 1800, 2100, 2400, 2700, 3000],
            areaStyle: {
                color: {
                    type: 'linear',
                    x: 0, y: 0, x2: 0, y2: 1,
                    colorStops: [
                        { offset: 0, color: 'rgba(0, 123, 255, 0.8)' },
                        { offset: 1, color: 'rgba(0, 123, 255, 0.1)' }
                    ]
                }
            }
        },
        {
            name: '活跃用户',
            data: [800, 1000, 1200, 1400, 1600, 1800, 2000],
            areaStyle: {
                color: {
                    type: 'linear',
                    x: 0, y: 0, x2: 0, y2: 1,
                    colorStops: [
                        { offset: 0, color: 'rgba(40, 167, 69, 0.8)' },
                        { offset: 1, color: 'rgba(40, 167, 69, 0.1)' }
                    ]
                }
            }
        }
    ],
    xAxisData: ['1月', '2月', '3月', '4月', '5月', '6月', '7月']
}
```

### 5. 散点图 (scatter)

```javascript
{
    id: 'scatter-chart',
    type: 'chart',
    title: '用户年龄与消费分布',
    chartType: 'scatter',
    height: 400,
    series: [
        {
            name: '男性用户',
            type: 'scatter',
            data: [
                [25, 3200], [28, 4500], [32, 5200], [35, 4800],
                [40, 6200], [45, 5800], [50, 4200], [55, 3500]
            ],
            itemStyle: { color: '#007bff' }
        },
        {
            name: '女性用户',
            type: 'scatter',
            data: [
                [22, 2800], [26, 3900], [30, 4200], [33, 4600],
                [38, 5100], [42, 5500], [48, 4900], [52, 3800]
            ],
            itemStyle: { color: '#dc3545' }
        }
    ],
    xAxis: {
        name: '年龄',
        type: 'value',
        min: 20,
        max: 60
    },
    yAxis: {
        name: '消费金额',
        type: 'value',
        min: 0
    }
}
```

## 数据源配置

### 静态数据

```javascript
{
    id: 'static-chart',
    type: 'chart',
    title: '静态数据图表',
    chartType: 'bar',
    series: [
        {
            name: '销售额',
            data: [1200, 1500, 1800, 2100, 2400]
        }
    ],
    xAxisData: ['Q1', 'Q2', 'Q3', 'Q4', 'Q5']
}
```

### API 数据源

```javascript
{
    id: 'api-chart',
    type: 'chart',
    title: '动态数据图表',
    subtitle: '实时更新数据',
    api: '/api/charts/sales-trend',
    chartType: 'line',
    height: 350,
    interval: 30000  // 30秒自动刷新
}
```

### 复杂API配置

```javascript
{
    id: 'complex-api-chart',
    type: 'chart',
    title: '业务数据分析',
    api: {
        method: 'post',
        url: '/api/charts/business-analysis',
        data: {
            startDate: '${startDate}',
            endDate: '${endDate}',
            category: '${category}'
        }
    },
    chartType: 'line',
    height: 400,
    dataMapping: {
        series: '${data.series}',
        xAxisData: '${data.categories}'
    }
}
```

## 高级配置

### 多轴图表

```javascript
{
    id: 'multi-axis-chart',
    type: 'chart',
    title: '销售额与增长率',
    height: 400,
    config: {
        xAxis: {
            type: 'category',
            data: ['1月', '2月', '3月', '4月', '5月', '6月']
        },
        yAxis: [
            {
                type: 'value',
                name: '销售额',
                position: 'left',
                axisLabel: {
                    formatter: '{value} 万元'
                }
            },
            {
                type: 'value',
                name: '增长率',
                position: 'right',
                axisLabel: {
                    formatter: '{value} %'
                }
            }
        ],
        series: [
            {
                name: '销售额',
                type: 'bar',
                yAxisIndex: 0,
                data: [2340, 2670, 2450, 2880, 3200, 3100],
                itemStyle: { color: '#007bff' }
            },
            {
                name: '增长率',
                type: 'line',
                yAxisIndex: 1,
                data: [15.2, 14.1, -8.2, 17.5, 11.1, -3.1],
                itemStyle: { color: '#dc3545' }
            }
        ]
    }
}
```

### 自定义配置

```javascript
{
    id: 'custom-chart',
    type: 'chart',
    title: '自定义图表配置',
    height: 350,
    config: {
        title: {
            text: '主标题',
            subtext: '副标题',
            left: 'center'
        },
        tooltip: {
            trigger: 'axis',
            axisPointer: {
                type: 'cross',
                label: {
                    backgroundColor: '#6a7985'
                }
            }
        },
        legend: {
            data: ['邮件营销', '联盟广告', '视频广告', '直接访问', '搜索引擎'],
            bottom: 0
        },
        grid: {
            left: '3%',
            right: '4%',
            bottom: '10%',
            containLabel: true
        },
        xAxis: {
            type: 'category',
            boundaryGap: false,
            data: ['周一', '周二', '周三', '周四', '周五', '周六', '周日']
        },
        yAxis: {
            type: 'value'
        },
        series: [
            {
                name: '邮件营销',
                type: 'line',
                stack: '总量',
                data: [120, 132, 101, 134, 90, 230, 210]
            },
            {
                name: '联盟广告',
                type: 'line',
                stack: '总量',
                data: [220, 182, 191, 234, 290, 330, 310]
            }
        ]
    }
}
```

## 响应式配置

### 基本响应式

```javascript
{
    id: 'responsive-chart',
    type: 'chart',
    title: '响应式图表',
    chartType: 'bar',
    height: 300,
    responsive: true,
    series: [
        {
            name: '数据',
            data: [120, 200, 150, 80, 70, 110, 130]
        }
    ],
    xAxisData: ['周一', '周二', '周三', '周四', '周五', '周六', '周日']
}
```

### 设备适配

```javascript
{
    config: {
        media: [
            {
                query: { maxWidth: 768 },
                option: {
                    grid: { left: 10, right: 10 },
                    legend: { bottom: 0 },
                    xAxis: {
                        axisLabel: { rotate: 45 }
                    }
                }
            },
            {
                query: { minWidth: 768 },
                option: {
                    grid: { left: 60, right: 60 },
                    legend: { top: 0 }
                }
            }
        ]
    }
}
```

## 交互功能

### 数据缩放

```javascript
{
    config: {
        dataZoom: [
            {
                type: 'slider',
                start: 0,
                end: 100
            },
            {
                type: 'inside',
                start: 0,
                end: 100
            }
        ]
    }
}
```

### 工具箱

```javascript
{
    config: {
        toolbox: {
            feature: {
                saveAsImage: { title: '保存图片' },
                dataView: { title: '数据视图' },
                magicType: {
                    type: ['line', 'bar'],
                    title: { line: '折线图', bar: '柱状图' }
                },
                restore: { title: '还原' }
            }
        }
    }
}
```

### 图例配置

```javascript
{
    config: {
        legend: {
            type: 'scroll',
            orient: 'horizontal',
            left: 'center',
            bottom: 0,
            data: ['系列1', '系列2', '系列3']
        }
    }
}
```

## 实际应用示例

### 基于演示代码的混合图表

```javascript
const mixedChartsExample = [
    // 访问量趋势图
    {
        id: 'traffic-chart',
        type: 'chart',
        title: '访问量趋势',
        chartType: 'line',
        height: 300,
        series: [
            {
                name: '访问量',
                data: [120, 132, 101, 134, 90, 230, 210],
                smooth: true,
                areaStyle: {
                    color: {
                        type: 'linear',
                        x: 0, y: 0, x2: 0, y2: 1,
                        colorStops: [
                            { offset: 0, color: 'rgba(0, 123, 255, 0.3)' },
                            { offset: 1, color: 'rgba(0, 123, 255, 0.05)' }
                        ]
                    }
                }
            }
        ],
        xAxisData: ['周一', '周二', '周三', '周四', '周五', '周六', '周日']
    },
    
    // CPU使用率监控
    {
        id: 'cpu-chart',
        type: 'chart',
        title: 'CPU 使用率监控',
        subtitle: '实时系统性能',
        chartType: 'line',
        height: 250,
        api: '/api/system/cpu-usage',
        interval: 5000,  // 5秒刷新
        config: {
            yAxis: {
                max: 100,
                axisLabel: {
                    formatter: '{value}%'
                }
            },
            series: [{
                type: 'line',
                smooth: true,
                symbol: 'none',
                lineStyle: {
                    color: '#dc3545',
                    width: 2
                },
                areaStyle: {
                    color: {
                        type: 'linear',
                        x: 0, y: 0, x2: 0, y2: 1,
                        colorStops: [
                            { offset: 0, color: 'rgba(220, 53, 69, 0.3)' },
                            { offset: 1, color: 'rgba(220, 53, 69, 0.05)' }
                        ]
                    }
                }
            }]
        }
    },
    
    // 内存使用监控
    {
        id: 'memory-chart',
        type: 'chart',
        title: '内存使用监控',
        chartType: 'area',
        height: 250,
        series: [
            {
                name: '已使用',
                data: [2.1, 2.3, 2.5, 2.7, 2.4, 2.6, 2.8],
                areaStyle: { color: 'rgba(23, 162, 184, 0.3)' }
            },
            {
                name: '缓存',
                data: [1.2, 1.4, 1.6, 1.5, 1.3, 1.7, 1.8],
                areaStyle: { color: 'rgba(40, 167, 69, 0.3)' }
            }
        ],
        xAxisData: ['00:00', '04:00', '08:00', '12:00', '16:00', '20:00', '24:00']
    }
];
```

### 业务仪表板图表

```javascript
const businessDashboardCharts = {
    // 销售趋势
    salesTrend: {
        id: 'sales-trend',
        type: 'chart',
        title: '销售趋势分析',
        subtitle: '最近12个月数据',
        chartType: 'line',
        height: 400,
        api: '/api/charts/sales-trend',
        config: {
            tooltip: {
                trigger: 'axis',
                formatter: function(params) {
                    let result = params[0].name + '<br/>';
                    params.forEach(item => {
                        result += item.marker + item.seriesName + ': ¥' + 
                                  item.value.toLocaleString() + '<br/>';
                    });
                    return result;
                }
            },
            legend: {
                data: ['线上销售', '线下销售', '总销售'],
                bottom: 0
            },
            series: [
                {
                    name: '线上销售',
                    type: 'line',
                    smooth: true,
                    itemStyle: { color: '#007bff' }
                },
                {
                    name: '线下销售',
                    type: 'line',
                    smooth: true,
                    itemStyle: { color: '#28a745' }
                },
                {
                    name: '总销售',
                    type: 'line',
                    smooth: true,
                    lineStyle: { width: 3 },
                    itemStyle: { color: '#ffc107' }
                }
            ]
        }
    },
    
    // 地区分布
    regionDistribution: {
        id: 'region-chart',
        type: 'chart',
        title: '销售地区分布',
        chartType: 'pie',
        height: 350,
        api: '/api/charts/region-distribution',
        config: {
            tooltip: {
                trigger: 'item',
                formatter: '{a} <br/>{b}: {c} ({d}%)'
            },
            legend: {
                orient: 'vertical',
                left: 10,
                data: ['华北', '华东', '华南', '华中', '西南', '西北', '东北']
            },
            series: [
                {
                    name: '销售额',
                    type: 'pie',
                    radius: ['50%', '70%'],
                    center: ['60%', '50%'],
                    avoidLabelOverlap: false,
                    label: {
                        show: false
                    },
                    emphasis: {
                        label: {
                            show: true,
                            fontSize: '14',
                            fontWeight: 'bold'
                        }
                    }
                }
            ]
        }
    }
};
```

## 配置参数参考

### 基本配置

| 参数 | 类型 | 默认值 | 描述 |
|------|------|--------|------|
| id | string | - | 卡片唯一标识（必填） |
| type | string | 'chart' | 卡片类型（必填） |
| title | string | - | 卡片标题 |
| subtitle | string | - | 卡片副标题 |
| size | string | 'large' | 卡片尺寸 |
| theme | string | 'default' | 主题 |
| height | number | 300 | 图表高度 |

### 图表配置

| 参数 | 类型 | 默认值 | 描述 |
|------|------|--------|------|
| chartType | string | 'line' | 图表类型：line, bar, pie, area, scatter |
| series | array | [] | 数据系列 |
| xAxisData | array | [] | X轴数据 |
| config | object | {} | ECharts完整配置 |

### 数据源配置

| 参数 | 类型 | 默认值 | 描述 |
|------|------|--------|------|
| api | string/object | - | API数据源 |
| interval | number | - | 自动刷新间隔（毫秒） |
| dataMapping | object | - | 数据映射配置 |

### 系列配置 (series)

| 参数 | 类型 | 默认值 | 描述 |
|------|------|--------|------|
| name | string | - | 系列名称 |
| type | string | - | 系列类型 |
| data | array | [] | 系列数据 |
| itemStyle | object | {} | 图形样式 |
| lineStyle | object | {} | 线条样式（折线图） |
| areaStyle | object | {} | 区域样式（面积图） |

## 样式定制

### CSS 变量

```css
:root {
    /* 图表卡片基础样式 */
    --chart-card-background: #ffffff;
    --chart-card-border: 1px solid #e9ecef;
    --chart-card-border-radius: 8px;
    --chart-card-padding: 1.5rem;
    --chart-card-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
    
    /* 图表容器样式 */
    --chart-container-background: transparent;
    --chart-container-border-radius: 4px;
    
    /* 图表默认颜色 */
    --chart-color-primary: #007bff;
    --chart-color-success: #28a745;
    --chart-color-warning: #ffc107;
    --chart-color-danger: #dc3545;
    --chart-color-info: #17a2b8;
}
```

### 主题适配

```css
/* 深色主题 */
.amis-cards-theme-dark .chart-card {
    background-color: #2d3748;
    border-color: #4a5568;
}

.amis-cards-theme-dark .chart-container {
    filter: invert(0.9) hue-rotate(180deg);
}

/* 响应式样式 */
@media (max-width: 768px) {
    .chart-card {
        padding: 1rem;
    }
    
    .chart-container {
        height: 250px !important;
    }
}
```

## 最佳实践

### 1. 图表选择
- **折线图**：适用于时间序列数据、趋势分析
- **柱状图**：适用于分类数据对比
- **饼图**：适用于比例数据展示
- **面积图**：适用于累积数据展示
- **散点图**：适用于相关性分析

### 2. 数据设计
- 合理设计数据结构
- 避免数据点过多
- 提供数据缩放功能
- 考虑数据更新频率

### 3. 视觉设计
- 选择合适的颜色搭配
- 保持图表风格一致
- 添加必要的图例和标注
- 确保文字清晰可读

### 4. 性能优化
- 控制数据量大小
- 使用懒加载机制
- 合理设置刷新间隔
- 优化渲染性能

## 常见问题

### Q: 如何处理大量数据点？
A: 使用数据采样、数据缩放(dataZoom)功能，或者分页加载数据。

### Q: 如何自定义图表颜色？
A: 在series中设置itemStyle.color，或者在全局config中设置color数组。

### Q: 如何实现图表的动态更新？
A: 使用API数据源配合interval属性，或者通过事件触发数据更新。

### Q: 移动端图表显示异常？
A: 设置responsive属性为true，并配置相应的media查询适配不同设备。

## 参考资源

- [ECharts 官方文档](https://echarts.apache.org/zh/index.html)
- [ECharts 配置项手册](https://echarts.apache.org/zh/option.html)
- [演示页面](../demo/index.html)
- [配置示例](../configs/card-configs.js) 