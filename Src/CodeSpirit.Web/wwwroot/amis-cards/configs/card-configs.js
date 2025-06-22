/**
 * CodeSpirit Amis Cards V2.0 - 卡片配置
 * 定义各种卡片类型的默认配置和预设模板
 * 
 * @version 2.0.0
 * @author CodeSpirit Team
 */

// 确保命名空间存在
window.AmisCards = window.AmisCards || {};

/**
 * 卡片配置定义
 */
const CardConfigs = {
    /**
     * 默认配置
     */
    defaults: {
        // 基础配置
        size: 'medium',
        theme: 'default',
        autoRefresh: false,
        refreshInterval: 30000,
        showRefreshButton: true,
        showFullscreenButton: false,
        showSettingsButton: false,
        
        // 样式配置
        style: {
            borderRadius: '6px',
            boxShadow: '0 2px 8px rgba(0,0,0,0.1)',
            transition: 'all 0.3s ease'
        },
        
        // 权限配置
        permissions: [],
        
        // 数据配置
        data: {
            api: null,
            params: {},
            options: {}
        }
    },

    /**
     * 统计卡片默认配置
     */
    stat: {
        type: 'stat',
        title: '统计卡片',
        subtitle: '',
        size: 'medium',
        theme: 'default',
        data: {
            value: 0,
            label: '',
            unit: '',
            prefix: '',
            suffix: '',
            formatter: null,
            trend: null,
            target: null,
            showProgress: false,
            animateValue: true,
            animationDuration: 2000,
            description: ''
        }
    },

    /**
     * 图表卡片默认配置
     */
    chart: {
        type: 'chart',
        title: '图表卡片',
        subtitle: '',
        size: 'large',
        theme: 'default',
        data: {
            chartType: 'line',
            api: null,
            config: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        display: true,
                        position: 'top'
                    },
                    tooltip: {
                        enabled: true,
                        mode: 'index',
                        intersect: false
                    }
                },
                scales: {
                    x: {
                        display: true,
                        grid: {
                            display: true
                        }
                    },
                    y: {
                        display: true,
                        grid: {
                            display: true
                        }
                    }
                }
            }
        }
    },

    /**
     * 信息卡片默认配置
     */
    info: {
        type: 'info',
        title: '信息卡片',
        subtitle: '',
        size: 'medium',
        theme: 'info',
        data: {
            content: '',
            markdown: false,
            actions: []
        }
    },

    /**
     * 操作卡片默认配置
     */
    action: {
        type: 'action',
        title: '操作卡片',
        subtitle: '',
        size: 'medium',
        theme: 'primary',
        data: {
            layout: 'grid',
            actions: []
        }
    },

    /**
     * 表单卡片默认配置
     */
    form: {
        type: 'form',
        title: '表单卡片',
        subtitle: '',
        size: 'large',
        theme: 'default',
        data: {
            mode: 'horizontal',
            api: null,
            submitText: '提交',
            resetText: '重置',
            body: []
        }
    },

    /**
     * 表格卡片默认配置
     */
    table: {
        type: 'table',
        title: '表格卡片',
        subtitle: '',
        size: 'large',
        theme: 'default',
        data: {
            api: null,
            columns: [],
            pagination: true,
            pageSize: 10,
            showHeader: true,
            striped: true,
            bordered: false,
            hover: true,
            selectable: false,
            searchable: true,
            sortable: true
        }
    }
};

/**
 * 预设模板
 */
const CardTemplates = {
    /**
     * 统计卡片模板
     */
    statTemplates: {
        // 基础数字统计
        basicNumber: {
            ...CardConfigs.stat,
            title: '基础统计',
            data: {
                ...CardConfigs.stat.data,
                value: 1234,
                label: '总数',
                formatter: 'integer'
            }
        },

        // 货币统计
        currency: {
            ...CardConfigs.stat,
            title: '收入统计',
            theme: 'success',
            data: {
                ...CardConfigs.stat.data,
                value: 98765.43,
                label: '今日收入',
                formatter: 'currency'
            }
        },

        // 百分比统计
        percentage: {
            ...CardConfigs.stat,
            title: '完成率',
            theme: 'info',
            data: {
                ...CardConfigs.stat.data,
                value: 87.5,
                label: '任务完成率',
                formatter: 'percentage'
            }
        },

        // 带趋势的统计
        withTrend: {
            ...CardConfigs.stat,
            title: '用户增长',
            theme: 'primary',
            data: {
                ...CardConfigs.stat.data,
                value: 2468,
                label: '新增用户',
                formatter: 'integer',
                trend: {
                    direction: 'up',
                    value: 12.5,
                    period: '较昨日',
                    percentage: true
                }
            }
        },

        // 带进度条的统计
        withProgress: {
            ...CardConfigs.stat,
            title: '销售目标',
            theme: 'warning',
            data: {
                ...CardConfigs.stat.data,
                value: 7500,
                label: '当前销售额',
                formatter: 'currency',
                target: 10000,
                showProgress: true
            }
        }
    },

    /**
     * 图表卡片模板
     */
    chartTemplates: {
        // 折线图
        lineChart: {
            ...CardConfigs.chart,
            title: '趋势图',
            data: {
                ...CardConfigs.chart.data,
                chartType: 'line',
                config: {
                    ...CardConfigs.chart.data.config,
                    datasets: [{
                        label: '数据趋势',
                        data: [65, 59, 80, 81, 56, 55, 40],
                        borderColor: 'rgb(75, 192, 192)',
                        backgroundColor: 'rgba(75, 192, 192, 0.2)',
                        tension: 0.1
                    }],
                    labels: ['1月', '2月', '3月', '4月', '5月', '6月', '7月']
                }
            }
        },

        // 柱状图
        barChart: {
            ...CardConfigs.chart,
            title: '对比图',
            data: {
                ...CardConfigs.chart.data,
                chartType: 'bar',
                config: {
                    ...CardConfigs.chart.data.config,
                    datasets: [{
                        label: '销售额',
                        data: [12, 19, 3, 5, 2, 3, 7],
                        backgroundColor: [
                            'rgba(255, 99, 132, 0.2)',
                            'rgba(54, 162, 235, 0.2)',
                            'rgba(255, 205, 86, 0.2)',
                            'rgba(75, 192, 192, 0.2)',
                            'rgba(153, 102, 255, 0.2)',
                            'rgba(255, 159, 64, 0.2)',
                            'rgba(199, 199, 199, 0.2)'
                        ],
                        borderColor: [
                            'rgba(255, 99, 132, 1)',
                            'rgba(54, 162, 235, 1)',
                            'rgba(255, 205, 86, 1)',
                            'rgba(75, 192, 192, 1)',
                            'rgba(153, 102, 255, 1)',
                            'rgba(255, 159, 64, 1)',
                            'rgba(199, 199, 199, 1)'
                        ],
                        borderWidth: 1
                    }],
                    labels: ['周一', '周二', '周三', '周四', '周五', '周六', '周日']
                }
            }
        },

        // 饼图
        pieChart: {
            ...CardConfigs.chart,
            title: '分布图',
            data: {
                ...CardConfigs.chart.data,
                chartType: 'pie',
                config: {
                    ...CardConfigs.chart.data.config,
                    datasets: [{
                        label: '数据分布',
                        data: [300, 50, 100, 80, 120],
                        backgroundColor: [
                            '#FF6384',
                            '#36A2EB',
                            '#FFCE56',
                            '#4BC0C0',
                            '#9966FF'
                        ]
                    }],
                    labels: ['红色', '蓝色', '黄色', '绿色', '紫色']
                }
            }
        }
    },

    /**
     * 信息卡片模板
     */
    infoTemplates: {
        // 基础信息
        basicInfo: {
            ...CardConfigs.info,
            title: '系统通知',
            theme: 'info',
            data: {
                ...CardConfigs.info.data,
                content: '<h5>欢迎使用 AmisCards</h5><p>这是一个基于 Amis 的卡片系统，提供丰富的卡片类型和配置选项。</p>'
            }
        },

        // 警告信息
        warningInfo: {
            ...CardConfigs.info,
            title: '重要提醒',
            theme: 'warning',
            data: {
                ...CardConfigs.info.data,
                content: '<h5>⚠️ 注意事项</h5><p>请注意系统维护时间，避免在维护期间进行重要操作。</p>'
            }
        },

        // 成功信息
        successInfo: {
            ...CardConfigs.info,
            title: '操作成功',
            theme: 'success',
            data: {
                ...CardConfigs.info.data,
                content: '<h5>✅ 操作完成</h5><p>您的操作已成功完成，系统已自动保存相关数据。</p>'
            }
        }
    },

    /**
     * 操作卡片模板
     */
    actionTemplates: {
        // 基础操作
        basicActions: {
            ...CardConfigs.action,
            title: '快速操作',
            data: {
                ...CardConfigs.action.data,
                layout: 'grid',
                actions: [
                    {
                        type: 'button',
                        label: '新建',
                        level: 'primary',
                        icon: 'fa fa-plus',
                        actionType: 'dialog',
                        dialog: {
                            title: '新建项目',
                            body: '新建对话框内容'
                        }
                    },
                    {
                        type: 'button',
                        label: '编辑',
                        level: 'info',
                        icon: 'fa fa-edit',
                        actionType: 'drawer',
                        drawer: {
                            title: '编辑项目',
                            body: '编辑抽屉内容'
                        }
                    },
                    {
                        type: 'button',
                        label: '删除',
                        level: 'danger',
                        icon: 'fa fa-trash',
                        actionType: 'ajax',
                        confirmText: '确定要删除吗？',
                        api: 'delete:/api/items/${id}'
                    }
                ]
            }
        },

        // 管理操作
        adminActions: {
            ...CardConfigs.action,
            title: '管理操作',
            theme: 'warning',
            data: {
                ...CardConfigs.action.data,
                layout: 'list',
                actions: [
                    {
                        type: 'button',
                        label: '用户管理',
                        level: 'default',
                        icon: 'fa fa-users',
                        actionType: 'url',
                        url: '/admin/users'
                    },
                    {
                        type: 'button',
                        label: '系统设置',
                        level: 'default',
                        icon: 'fa fa-cogs',
                        actionType: 'url',
                        url: '/admin/settings'
                    },
                    {
                        type: 'button',
                        label: '日志查看',
                        level: 'default',
                        icon: 'fa fa-file-text',
                        actionType: 'url',
                        url: '/admin/logs'
                    }
                ]
            }
        }
    }
};

/**
 * 获取默认配置
 * @param {string} type 卡片类型
 * @returns {Object} 默认配置
 */
function getDefaultConfig(type) {
    const config = CardConfigs[type] || {};
    return window.AmisCards.Utils.deepClone({
        ...CardConfigs.defaults,
        ...config
    });
}

/**
 * 获取预设模板
 * @param {string} type 卡片类型
 * @param {string} template 模板名称
 * @returns {Object} 模板配置
 */
function getTemplate(type, template) {
    const templates = CardTemplates[`${type}Templates`];
    if (!templates || !templates[template]) {
        throw new Error(`模板不存在: ${type}.${template}`);
    }
    
    return window.AmisCards.Utils.deepClone(templates[template]);
}

/**
 * 合并配置
 * @param {Object} baseConfig 基础配置
 * @param {Object} customConfig 自定义配置
 * @returns {Object} 合并后的配置
 */
function mergeConfig(baseConfig, customConfig) {
    return window.AmisCards.Utils.deepMerge(
        window.AmisCards.Utils.deepClone(baseConfig),
        customConfig
    );
}

/**
 * 验证配置
 * @param {Object} config 配置对象
 * @returns {Object} 验证结果
 */
function validateConfig(config) {
    const errors = [];
    const warnings = [];
    
    // 必填字段检查
    if (!config.type) {
        errors.push('缺少必填字段: type');
    }
    
    if (!config.id) {
        warnings.push('建议设置唯一的 id 字段');
    }
    
    // 类型检查
    const supportedTypes = Object.keys(CardConfigs).filter(key => key !== 'defaults');
    if (config.type && !supportedTypes.includes(config.type)) {
        errors.push(`不支持的卡片类型: ${config.type}`);
    }
    
    // 大小检查
    const supportedSizes = ['small', 'medium', 'large'];
    if (config.size && !supportedSizes.includes(config.size)) {
        warnings.push(`不推荐的大小值: ${config.size}`);
    }
    
    // 主题检查
    const supportedThemes = ['default', 'primary', 'success', 'warning', 'danger', 'info'];
    if (config.theme && !supportedThemes.includes(config.theme)) {
        warnings.push(`不推荐的主题值: ${config.theme}`);
    }
    
    return {
        valid: errors.length === 0,
        errors,
        warnings
    };
}

/**
 * 生成配置文档
 * @param {string} type 卡片类型
 * @returns {Object} 配置文档
 */
function generateConfigDocs(type) {
    const config = CardConfigs[type];
    if (!config) {
        throw new Error(`卡片类型不存在: ${type}`);
    }
    
    const docs = {
        type,
        description: `${type} 卡片配置文档`,
        properties: {},
        examples: []
    };
    
    // 生成属性文档
    function generatePropertyDocs(obj, prefix = '') {
        Object.keys(obj).forEach(key => {
            const value = obj[key];
            const propName = prefix ? `${prefix}.${key}` : key;
            
            if (typeof value === 'object' && value !== null && !Array.isArray(value)) {
                generatePropertyDocs(value, propName);
            } else {
                docs.properties[propName] = {
                    type: typeof value,
                    default: value,
                    description: `${propName} 属性`
                };
            }
        });
    }
    
    generatePropertyDocs(config);
    
    // 添加示例
    const templates = CardTemplates[`${type}Templates`];
    if (templates) {
        Object.keys(templates).forEach(templateName => {
            docs.examples.push({
                name: templateName,
                description: `${templateName} 示例`,
                config: templates[templateName]
            });
        });
    }
    
    return docs;
}

// 导出配置
window.AmisCards.CardConfigs = CardConfigs;
window.AmisCards.CardTemplates = CardTemplates;

// 导出工具函数
window.AmisCards.getDefaultConfig = getDefaultConfig;
window.AmisCards.getTemplate = getTemplate;
window.AmisCards.mergeConfig = mergeConfig;
window.AmisCards.validateConfig = validateConfig;
window.AmisCards.generateConfigDocs = generateConfigDocs;

console.log('[AmisCards.CardConfigs] 卡片配置模块已加载'); 