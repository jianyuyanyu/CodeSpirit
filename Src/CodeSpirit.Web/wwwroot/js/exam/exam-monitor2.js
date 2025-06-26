/**
 * 考试监控大屏 ExamMonitor2 专用脚本
 * @description 基于 AmisCards 和 monitor-dashboard.js API 实现的动态监控大屏
 * @version 2.0.0
 */

(function() {
    'use strict';

    // 全局变量
    let examMonitorInstance = null;

    /**
     * 页面初始化
     */
    document.addEventListener('DOMContentLoaded', function() {
        
        // 验证必要参数
        if (!window.tenantId || !window.examId) {
            window.AmisCardsLayout.showError(
                '参数错误',
                '缺少必要的租户ID或考试ID',
                'exam-monitor-root'
            );
            return;
        }

        // 初始化租户模式
        if (window.TokenManager && window.TokenManager.initTenantMode) {
            window.TokenManager.initTenantMode(window.tenantId);
        }

        // 使用布局提供的依赖检查
        window.AmisCardsLayout.checkDependencies(function(success) {
            if (success) {
                initExamMonitor();
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
    function initExamMonitor() {
        try {
            // 创建 AmisCards 实例
            examMonitorInstance = window.AmisCards.create({
                container: '#exam-monitor-root',
                // theme: currentTheme,
                config: {
                    pageTitle: '考试监控大屏',
                    pageSchema: createPageSchema(),
                    autoRefresh: true,
                    refreshInterval: parseInt(window.AmisCardsConfig.refreshInterval) || 30000
                }
            });

            // 注册渲染器
            window.AmisCardsLayout.registerRenderers(examMonitorInstance);

            // 等待实例准备就绪后加载数据
            window.AmisCardsLayout.waitForInstanceReady(examMonitorInstance, () => {
                loadExamMonitorData();
            });

        } catch (error) {
            console.error('[考试监控大屏] 初始化失败:', error);
            window.AmisCardsLayout.showError('初始化失败', error.message, 'exam-monitor-root');
        }
    }

    /**
     * 创建页面配置 Schema
     */
    function createPageSchema() {
        return window.AmisCardsLayout.createDefaultPageSchema({
            pageId: 'exam-monitor-page',
            pageTitle: '<i class="fa fa-desktop"></i> ${name || "考试监控大屏"}',
            pageClass: 'exam-monitor-dashboard-page amis-cards-page',
            pageTitleClass: 'exam-monitor-page-title exam-monitor-title',
            initApi: `/exam/api/exam/Monitor/exam/${window.examId}`,
            features: [
                { icon: 'fa fa-chart-line', text: '实时监控考试进度' },
                { icon: 'fa fa-shield-alt', text: '智能防作弊检测' },
                { icon: 'fa fa-chart-bar', text: '数据可视化分析' },
                { icon: 'fa fa-clock', text: '服务器时间: ${serverTime}' }
            ],
            subTitleClass: 'exam-monitor-page-subtitle-wrapper',
            instanceReference: 'window.examMonitorInstance'
        });
    }

    /**
     * 加载考试监控数据
     */
    async function loadExamMonitorData() {
        try {

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
    function generateMonitorCards() {
        const cards = [];
        
        // 考试监控信息网格卡片
        cards.push({
            id: 'exam-monitor-info-grid',
            type: 'info-grid',
            title: '考试基本信息',
            subtitle: '实时更新的考试概览数据',
            theme: 'info',
            grid: {
                columns: 4,
                gap: '1.25rem'
            },
            items: [
                {
                    label: '考试编号',
                    value: "${id || '" + window.examId + "'}",
                    icon: 'id-card',
                    iconColor: '#9b59b6'
                },
                {
                    label: '学校机构',
                    value: "${tenantName || '" + (window.tenantName || '暂无信息') + "'}",
                    icon: 'university',
                    iconColor: '#f39c12'
                },
                {
                    label: '考试状态',
                    value: "${status || '进行中'}",
                    icon: 'play-circle',
                    iconColor: '#27ae60',
                    highlight: true
                },
                {
                    label: '在线情况',
                    value: "${onlineCount || 0}/${totalParticipants || 0}",
                    icon: 'users',
                    iconColor: '#e67e22',
                    highlight: true
                },
                {
                    label: '开始时间',
                    value: "${startTime || '待定'}",
                    icon: 'play-circle',
                    iconColor: '#27ae60'
                },
                {
                    label: '结束时间',
                    value: "${endTime || '待定'}",
                    icon: 'stop-circle',
                    iconColor: '#e74c3c'
                },
                {
                    label: '考试时长',
                    value: "${duration ? duration + '分钟' : '未设定'}",
                    icon: 'hourglass-half',
                    iconColor: '#f1c40f'
                },
                {
                    label: '最近更新',
                    value: "${lastUpdate | fromNow}",
                    icon: 'sync-alt',
                    iconColor: '#16a085'
                }
            ]
        });

        // 统计卡片
        cards.push({
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
                iconBorder: true,
                description: '最近更新: ${lastUpdate}'
            }
        });
        
        cards.push({
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
        });
        
        cards.push({
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
        });
        
        cards.push({
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
        });

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
            showPager: true,
            pageSize: 20
        });

        return cards;
    }

    // 导出全局变量供工具栏调用
    window.examMonitorInstance = examMonitorInstance;

})(); 