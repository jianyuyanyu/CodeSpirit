/**
 * 考试监控大屏 ExamMonitor2 专用脚本
 * @description 基于 AmisCards 和 monitor-dashboard.js API 实现的动态监控大屏
 * @version 2.0.0
 */

(function() {
    'use strict';

    // 全局变量
    let examMonitorInstance = null;
    let currentTheme = 'default';
    let refreshTimer = null;
    let lastUpdateTime = null;

    /**
     * 页面初始化
     */
    document.addEventListener('DOMContentLoaded', function() {
        console.log('[考试监控大屏] ExamMonitor2 页面加载完成');
        console.log('[考试监控大屏] 配置信息:', {
            tenantId: window.tenantId,
            examId: window.examId,
            tenantName: window.tenantName,
            examName: window.examName
        });

        // 验证必要参数
        if (!window.tenantId || !window.examId) {
            window.AmisCardsLayout.showError(
                '参数错误',
                '缺少必要的租户ID或考试ID',
                'exam-monitor-root'
            );
            return;
        }

        // 初始化租户模式（参考 monitor-dashboard.js）
        if (window.TokenManager && window.TokenManager.initTenantMode) {
            window.TokenManager.initTenantMode(window.tenantId);
            console.log(`[考试监控大屏] 已初始化租户模式：${window.tenantId}`);
        }

        // 使用布局提供的依赖检查
        window.AmisCardsLayout.checkDependencies(function(success) {
            if (success) {
                initExamMonitor2();
            } else {
                window.AmisCardsLayout.showError(
                    '依赖加载失败',
                    '请检查网络连接或脚本加载情况',
                    'exam-monitor-root'
                );
            }
        });
    });

    /**
     * 初始化考试监控大屏
     */
    function initExamMonitor2() {
        try {
            console.log('[考试监控大屏] 开始初始化 ExamMonitor2');

            // 应用主题
            window.AmisCardsLayout.applyTheme(currentTheme);

            // 创建 AmisCards 实例（参考 monitor-dashboard.html）
            examMonitorInstance = window.AmisCards.create({
                container: '#exam-monitor-root',
                theme: currentTheme,
                config: {
                    pageTitle: '考试监控大屏',
                    pageSchema: createPageSchema(),
                    autoRefresh: true,
                    refreshInterval: parseInt(window.AmisCardsConfig.refreshInterval) || 30000
                }
            });

            console.log('[考试监控大屏] AmisCards 实例已创建');

            // 注册渲染器
            window.AmisCardsLayout.registerRenderers(examMonitorInstance);

            // 等待实例准备就绪后加载数据
            window.AmisCardsLayout.waitForInstanceReady(examMonitorInstance, () => {
                loadExamMonitorData();
            });

            // 启动自动刷新
            startAutoRefresh();

            console.log('[考试监控大屏] ExamMonitor2 初始化完成');

        } catch (error) {
            console.error('[考试监控大屏] 初始化失败:', error);
            window.AmisCardsLayout.showError('初始化失败', error.message, 'exam-monitor-root');
        }
    }



    /**
     * 创建页面配置 Schema
     */
    function createPageSchema() {
        return {
            type: 'page',
            title: {
                type: 'tpl',
                tpl: '<div class="exam-monitor-title"><i class="fa fa-desktop"></i> ${name || "考试监控大屏"}</div>',
                className: 'exam-monitor-page-title'
            },
            initApi: `/exam/api/exam/Monitor/exam/${window.examId}`,
            subTitle: {
                type: 'tpl',
                tpl: `<div class="exam-monitor-subtitle">实时监控考试进度 · 智能防作弊检测 · 数据可视化分析</div>
                <div class="exam-monitor-info">
                    <span class="exam-monitor-info-item">
                        <i class="fa fa-calendar"></i> 考试ID: <span class="info-value">\${id || "${window.examId}"}</span>
                    </span>
                    <span class="exam-monitor-info-item">
                        <i class="fa fa-building"></i> 学校: <span class="info-value">\${tenantName || "${window.tenantName || '--'}"}</span>
                    </span>
                    <span class="exam-monitor-info-item">
                        <i class="fa fa-clock"></i> 状态: <span class="info-value info-highlight">\${status || "进行中"}</span>
                    </span>
                    <span class="exam-monitor-info-item">
                        <i class="fa fa-users"></i> 在线: <span class="info-value info-highlight">\${onlineCount || 0}/\${totalParticipants || 0}</span>
                    </span>
                    <span class="exam-monitor-info-item">
                        <i class="fa fa-sync"></i> 更新: <span class="info-value">\${lastUpdate || "--"}</span>
                    </span>
                </div>`,
                className: 'exam-monitor-page-subtitle-wrapper'
            },
            className: 'exam-monitor-dashboard-page amis-cards-page',
            toolbar: [
                {
                    type: 'button',
                    icon: 'fa fa-expand',
                    tooltip: '全屏模式',
                    className: 'mr-2',
                    onEvent: {
                        click: {
                            actions: [
                                {
                                    actionType: 'custom',
                                    script: 'window.AmisCardsLayout.toggleFullscreen()'
                                }
                            ]
                        }
                    }
                },
                {
                    type: 'button',
                    icon: 'fa fa-sync',
                    tooltip: '手动刷新',
                    className: 'mr-2',
                    onEvent: {
                        click: {
                            actions: [
                                {
                                    actionType: 'custom',
                                    script: 'window.ExamMonitor2.refreshData()'
                                }
                            ]
                        }
                    }
                },
                {
                    type: 'button',
                    icon: 'fa fa-palette',
                    tooltip: '切换主题',
                    onEvent: {
                        click: {
                            actions: [
                                {
                                    actionType: 'custom',
                                    script: 'window.ExamMonitor2.toggleTheme()'
                                }
                            ]
                        }
                    }
                }
            ],
            body: []
        };
    }

    /**
     * 加载考试监控数据
     */
    async function loadExamMonitorData() {
        try {
            console.log('[考试监控大屏] 开始加载监控数据');
            
            // 显示加载状态
            window.AmisCardsLayout.showLoading('正在加载考试数据...', 'exam-monitor-root');

            // 生成卡片配置
            const cards = generateMonitorCards();

            // 渲染卡片
            await examMonitorInstance.render(cards);

            // 隐藏加载状态
            setTimeout(() => {
                window.AmisCardsLayout.hideLoadingState('exam-monitor-root');
            }, 100);

        } catch (error) {
            console.error('[考试监控大屏] 加载监控数据失败:', error);
            window.AmisCardsLayout.showError(
                '数据加载失败',
                error.message || '请检查网络连接或联系管理员',
                'exam-monitor-root'
            );
        }
    }

    /**
     * 生成监控卡片配置
     */
    function generateMonitorCards(examData) {
        const cards = [
            // 基础统计卡片
            {
                id: 'total-participants',
                type: 'stat',
                title: '参考人数',
                subtitle: '考试总体参与统计',
                theme: 'info',
                data: {
                    value: "${totalParticipants}",
                    label: '总人数',
                    unit: '人',
                    formatter: 'integer',
                    icon: 'users',
                    iconColor: '#17a2b8',
                    iconSize: 'lg',
                    iconPosition: 'left',
                    iconBackground: 'rgba(23, 162, 184, 0.1)',
                    iconBorder: true
                }
            },
            {
                id: 'online-count',
                type: 'stat',
                title: '在线人数',
                subtitle: '实时在线统计',
                theme: 'success',
                data: {
                    value: "${onlineCount}",
                    label: '在线',
                    unit: '人',
                    formatter: 'integer',
                    target: "${totalParticipants}",
                    showProgress: true,
                    icon: 'wifi',
                    iconColor: '#28a745',
                    iconSize: 'lg',
                    iconPosition: 'left',
                    iconBackground: 'rgba(40, 167, 69, 0.1)',
                    iconBorder: true,
                    description: '在线率: ${totalParticipants > 0 ? ROUND((onlineCount || 0) / totalParticipants * 100) : 0}%'
                }
            },
            {
                id: 'submitted-count',
                type: 'stat',
                title: '已交卷',
                subtitle: '提交情况统计',
                theme: 'warning',
                data: {
                    value: "${submittedCount}",
                    label: '已提交',
                    unit: '人',
                    formatter: 'integer',
                    target: "${totalParticipants}",
                    showProgress: true,
                    icon: 'check-circle',
                    iconColor: '#ffc107',
                    iconSize: 'lg',
                    iconPosition: 'left',
                    iconBackground: 'rgba(255, 193, 7, 0.1)',
                    iconBorder: true,
                    description: '提交率: ${totalParticipants > 0 ? ROUND((submittedCount || 0) / totalParticipants * 100) : 0}%'
                }
            },
            {
                id: 'suspicious-count',
                type: 'stat',
                title: '风险预警',
                subtitle: '异常行为检测',
                theme: 'danger',
                data: {
                    value: "${suspiciousCount}",
                    label: '风险用户',
                    unit: '人',
                    formatter: 'integer',
                    icon: 'exclamation-triangle',
                    iconColor: '#dc3545',
                    iconSize: 'lg',
                    iconPosition: 'left',
                    iconBackground: 'rgba(220, 53, 69, 0.1)',
                    iconBorder: true,
                    description: '${suspiciousCount > 0 ? "需要关注异常行为" : "暂无异常"}'
                }
            }
        ];

        cards.push({
            id: 'students-table',
            type: 'table',
            title: '考生监控',
            subtitle: '实时监控考生状态',
            source: '${students}',
            columns: [
                {
                    name: 'name',
                    label: '姓名',
                    width: 120,
                    type: 'text'
                },
                {
                    name: 'studentNumber',
                    label: '学号',
                    width: 150,
                    type: 'text'
                },
                {
                    name: 'ipAddress',
                    label: 'IP地址',
                    width: 130,
                    type: 'text'
                },
                {
                    name: 'statusText',
                    label: '状态',
                    width: 100,
                    type: 'mapping',
                    map: {
                        '考试中': '<span class="label label-info">考试中</span>',
                        '已提交': '<span class="label label-success">已提交</span>',
                        '已评分': '<span class="label label-warning">已评分</span>',
                        '未开始': '<span class="label label-secondary">未开始</span>',
                        '*': '<span class="label label-default">${statusText || "未知"}</span>'
                    }
                },
                {
                    name: 'progressPercentage',
                    label: '进度',
                    width: 120,
                    type: 'progress',
                    showLabel: true,
                    stripe: true,
                    animate: true
                },
                {
                    name: 'screenSwitchCount',
                    label: '切屏次数',
                    width: 100,
                    type: 'text',
                    className: '${screenSwitchCount > 5 ? "text-danger" : screenSwitchCount > 2 ? "text-warning" : "text-success"}'
                }
            ],
            //data: {
            //    items: examData.students,
            //    total: examData.students.length
            //},
            showPager: true,
            pageSize: 20
        });

        return cards;
    }

    /**
     * 启动自动刷新
     */
    function startAutoRefresh() {
        if (refreshTimer) {
            clearInterval(refreshTimer);
        }

        const interval = parseInt(window.AmisCardsConfig.refreshInterval) || 30000;
        refreshTimer = setInterval(() => {
            console.log('[考试监控大屏] 自动刷新数据');
            //loadExamMonitorData();
        }, interval);

        console.log(`[考试监控大屏] 自动刷新已启动，间隔: ${interval}ms`);
    }

    /**
     * 停止自动刷新
     */
    function stopAutoRefresh() {
        if (refreshTimer) {
            clearInterval(refreshTimer);
            refreshTimer = null;
            console.log('[考试监控大屏] 自动刷新已停止');
        }
    }



    /**
     * 手动刷新数据
     */
    function refreshData() {
        console.log('[考试监控大屏] 手动刷新数据');
        // loadExamMonitorData();
    }

    /**
     * 切换主题
     */
    async function toggleTheme() {
        const themes = ['default', 'dark'];
        const currentIndex = themes.indexOf(currentTheme);
        const nextIndex = (currentIndex + 1) % themes.length;
        const newTheme = themes[nextIndex];
        
        try {
            // 应用页面主题（使用通用方法）
            await window.AmisCardsLayout.switchThemeAdvanced(currentTheme, newTheme, document.body, {
                duration: 300,
                easing: 'ease'
            });
            
            // 更新 AmisCards 实例主题
            if (examMonitorInstance && typeof examMonitorInstance.setTheme === 'function') {
                await examMonitorInstance.setTheme(newTheme, true);
            }
            
            currentTheme = newTheme;
            console.log('[考试监控大屏] 主题已切换到:', currentTheme);
            
        } catch (error) {
            console.error('[考试监控大屏] 主题切换失败:', error);
        }
    }

    /**
     * 页面卸载处理
     */
    window.addEventListener('beforeunload', function() {
        stopAutoRefresh();
    });

    // 导出全局函数供页面工具栏调用
    window.ExamMonitor2 = {
        refreshData: refreshData,
        toggleTheme: toggleTheme,
        startAutoRefresh: startAutoRefresh,
        stopAutoRefresh: stopAutoRefresh
    };

    console.log('[考试监控大屏] ExamMonitor2 脚本加载完成');

})(); 