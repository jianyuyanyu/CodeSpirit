/**
 * 监考大屏表格卡片使用示例
 * 基于MonitorDashboard.cshtml的表格应用
 */

// 初始化Cards SDK
const cardsSDK = new CodeSpiritCards.SDK({
    container: '#dashboard-cards',
    baseUrl: '/exam/api',
    autoRefresh: true,
    refreshInterval: 10000
});

// 考生监控表格卡片配置
const studentsMonitorTableCard = {
    id: 'students-monitor-table',
    type: 'table',
    title: '考生实时监控',
    subtitle: '考试进行中的考生状态监控',
    size: 'large',
    style: { 
        theme: 'primary',
        height: 500 
    },
    data: {
        source: '${students}',
        api: `/exam/Monitor/exam/${window.examId}`,
        columns: [
            {
                name: 'studentName',
                label: '考生姓名',
                type: 'text',
                sortable: true,
                width: 120
            },
            {
                name: 'studentNumber',
                label: '学号',
                type: 'text',
                hideOnMobile: true,
                width: 100
            },
            {
                name: 'status',
                label: '状态',
                type: 'status',
                sortable: true,
                width: 80,
                map: {
                    'online': '<span class="label label-success">在线</span>',
                    'offline': '<span class="label label-danger">离线</span>',
                    'submitted': '<span class="label label-info">已交卷</span>',
                    'suspicious': '<span class="label label-warning">异常</span>'
                }
            },
            {
                name: 'progress',
                label: '答题进度',
                type: 'progress',
                showLabel: true,
                width: 120
            },
            {
                name: 'score',
                label: '当前得分',
                type: 'number',
                precision: 1,
                width: 80,
                hideOnMobile: true
            },
            {
                name: 'lastActiveTime',
                label: '最后活跃',
                type: 'datetime',
                format: 'HH:mm:ss',
                width: 100
            },
            {
                name: 'suspiciousCount',
                label: '异常次数',
                type: 'number',
                width: 80,
                className: 'text-center'
            },
            {
                type: 'operation',
                label: '操作',
                width: 120,
                buttons: [
                    {
                        type: 'button',
                        label: '详情',
                        size: 'sm',
                        actionType: 'dialog',
                        dialog: {
                            title: '考生详情',
                            size: 'lg',
                            body: {
                                type: 'service',
                                api: `/exam/api/exam/Monitor/student/\${id}`,
                                body: [
                                    {
                                        type: 'property',
                                        title: '基本信息',
                                        column: 2,
                                        items: [
                                            {
                                                label: '姓名',
                                                content: '${studentName}'
                                            },
                                            {
                                                label: '学号',
                                                content: '${studentNumber}'
                                            },
                                            {
                                                label: '班级',
                                                content: '${className}'
                                            },
                                            {
                                                label: '状态',
                                                content: '${status}'
                                            }
                                        ]
                                    }
                                ]
                            }
                        }
                    },
                    {
                        type: 'button',
                        label: '强制交卷',
                        size: 'sm',
                        level: 'danger',
                        confirmText: '确定要强制该考生交卷吗？',
                        actionType: 'ajax',
                        api: {
                            method: 'post',
                            url: `/exam/api/exam/ForceSubmit/student/\${id}`
                        }
                    }
                ]
            }
        ],
        filter: {
            title: '',
            body: [
                {
                    type: 'input-text',
                    name: 'keywords',
                    placeholder: '搜索考生姓名或学号...',
                    addOn: {
                        label: '搜索',
                        type: 'submit'
                    }
                },
                {
                    type: 'select',
                    name: 'status',
                    placeholder: '筛选状态',
                    options: [
                        { label: '全部', value: '' },
                        { label: '在线', value: 'online' },
                        { label: '离线', value: 'offline' },
                        { label: '已交卷', value: 'submitted' },
                        { label: '异常', value: 'suspicious' }
                    ]
                }
            ]
        },
        headerToolbar: [
            'filter-toggler',
            'columns-toggler',
            {
                type: 'export-excel',
                label: '导出数据',
                api: `/exam/api/exam/Export/students/${window.examId}`
            },
            {
                type: 'button',
                label: '刷新数据',
                actionType: 'reload',
                target: 'students-monitor-table'
            }
        ],
        perPage: 20,
        perPageAvailable: [10, 20, 50, 100]
    },
    wrapWithService: true,
    serviceConfig: {
        api: `/exam/api/exam/Monitor/exam/${window.examId}`,
        interval: 5000 // 5秒更新一次
    }
};

// 考试统计摘要表格
const examSummaryTableCard = {
    id: 'exam-summary-table',
    type: 'table',
    title: '考试统计摘要',
    subtitle: '各项指标统计汇总',
    size: 'medium',
    style: { theme: 'success' },
    data: {
        api: `/exam/api/exam/Statistics/${window.examId}`,
        columns: [
            {
                name: 'metric',
                label: '统计项目',
                type: 'text',
                width: 150
            },
            {
                name: 'value',
                label: '数值',
                type: 'number',
                className: 'text-center',
                width: 100
            },
            {
                name: 'percentage',
                label: '百分比',
                type: 'progress',
                showLabel: true,
                width: 150
            },
            {
                name: 'trend',
                label: '趋势',
                type: 'text',
                width: 80,
                tpl: '<span class="${trend > 0 ? "text-success" : trend < 0 ? "text-danger" : "text-muted"}">${trend > 0 ? "↗" : trend < 0 ? "↘" : "→"}</span>'
            }
        ],
        headerToolbar: [
            {
                type: 'button',
                label: '详细报告',
                actionType: 'url',
                url: `/exam/report/${window.examId}`
            }
        ]
    }
};

// 异常行为监控表格
const suspiciousActivityTableCard = {
    id: 'suspicious-activity-table',
    type: 'table',
    title: '异常行为监控',
    subtitle: '实时监控可疑操作和违规行为',
    size: 'large',
    style: { theme: 'danger', height: 400 },
    data: {
        api: `/exam/api/exam/Monitor/suspicious/${window.examId}`,
        columns: [
            {
                name: 'timestamp',
                label: '时间',
                type: 'datetime',
                format: 'HH:mm:ss',
                width: 80
            },
            {
                name: 'studentName',
                label: '考生',
                type: 'text',
                width: 100
            },
            {
                name: 'activityType',
                label: '行为类型',
                type: 'text',
                width: 120
            },
            {
                name: 'severity',
                label: '严重程度',
                type: 'status',
                width: 80,
                map: {
                    'low': '<span class="label label-info">轻微</span>',
                    'medium': '<span class="label label-warning">中等</span>',
                    'high': '<span class="label label-danger">严重</span>'
                }
            },
            {
                name: 'description',
                label: '描述',
                type: 'text',
                hideOnMobile: true
            },
            {
                type: 'operation',
                label: '操作',
                width: 100,
                buttons: [
                    {
                        type: 'button',
                        label: '处理',
                        size: 'sm',
                        level: 'warning',
                        actionType: 'dialog',
                        dialog: {
                            title: '处理异常行为',
                            body: '处理异常行为的表单...'
                        }
                    }
                ]
            }
        ],
        headerToolbar: [
            {
                type: 'button',
                label: '标记全部已处理',
                level: 'info',
                confirmText: '确定要标记所有异常行为为已处理吗？'
            }
        ]
    },
    autoRefresh: true,
    refreshInterval: 3000 // 3秒刷新一次，异常行为需要更频繁的监控
};

// 渲染监考大屏
async function renderMonitorDashboard() {
    try {
        await cardsSDK.render('#dashboard-container', [
            studentsMonitorTableCard,
            examSummaryTableCard,
            suspiciousActivityTableCard
        ]);
        
        console.log('监考大屏表格卡片渲染完成');
    } catch (error) {
        console.error('渲染失败:', error);
    }
}

// 手动刷新所有表格
async function refreshAllTables() {
    try {
        await cardsSDK.refresh('students-monitor-table');
        await cardsSDK.refresh('exam-summary-table');
        await cardsSDK.refresh('suspicious-activity-table');
        
        console.log('所有表格数据已刷新');
    } catch (error) {
        console.error('刷新失败:', error);
    }
}

// 导出所有数据
function exportAllData() {
    const examId = window.examId;
    const tenantId = window.tenantId;
    
    // 创建导出链接
    const exportUrl = `/exam/api/exam/Export/all/${examId}?tenantId=${tenantId}`;
    
    // 创建隐藏的下载链接
    const link = document.createElement('a');
    link.href = exportUrl;
    link.download = `exam_${examId}_monitor_data.xlsx`;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
}

// 监听表格事件
cardsSDK.eventBus.on('card-rendered', (data) => {
    console.log('表格卡片渲染完成:', data);
    
    // 如果是考生监控表格，添加特殊处理
    if (data.id === 'students-monitor-table') {
        console.log('考生监控表格已就绪');
    }
});

cardsSDK.eventBus.on('card-updated', (data) => {
    console.log('表格数据更新:', data);
    
    // 更新页面标题显示最新更新时间
    const now = new Date().toLocaleTimeString();
    document.title = `监考大屏 - 最后更新: ${now}`;
});

// 全局函数导出，供页面调用
window.renderMonitorDashboard = renderMonitorDashboard;
window.refreshAllTables = refreshAllTables;
window.exportAllData = exportAllData;

// 页面加载完成后初始化
document.addEventListener('DOMContentLoaded', () => {
    console.log('监考大屏表格卡片示例已加载');
    
    // 如果有全局变量，自动渲染
    if (window.examId && window.tenantId) {
        setTimeout(renderMonitorDashboard, 1000);
    }
}); 