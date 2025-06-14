/**
 * 我的练习历史页面
 * 显示用户的练习记录、统计信息和操作功能
 */

(function() {
    'use strict';

    // 常量定义
    const CONSTANTS = {
        MAX_RETRY_COUNT: 3,
        RETRY_DELAY: 1000,
        PAGE_SIZE: 10
    };

    // 全局变量
    let isInitialized = false;
    let practiceRecords = [];
    let practiceStats = null;
    let currentPage = 1;
    let totalPages = 1;
    let searchKeyword = '';
    let statusFilter = '';
    let amis = amisRequire('amis/embed');
    let amisInstance = null;

    /**
     * 加载练习统计数据
     * @returns {Promise<boolean>} 加载是否成功
     */
    async function loadPracticeStatistics() {
        try {
            console.log('[我的练习] 开始加载练习统计数据');
            
            const response = await fetch('/exam/api/client/practice/analysis', {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': 'Bearer ' + (window.TokenManager ? window.TokenManager.getToken() : ''),
                    'X-Forwarded-With': 'CodeSpirit'
                }
            });

            if (!response.ok) {
                throw new Error(`HTTP error! Status: ${response.status}`);
            }

            const data = await response.json();
            
            if (data.status === 'success' || data.status === 0) {
                practiceStats = data.data || data.result;
                console.log('[我的练习] 练习统计数据加载成功:', practiceStats);
                return true;
            } else {
                throw new Error(data.message || '获取练习统计失败');
            }
        } catch (error) {
            console.error('[我的练习] 加载练习统计数据失败:', error);
            practiceStats = getDefaultStats();
            return false;
        }
    }

    /**
     * 获取默认统计数据
     * @returns {Object} 默认统计数据
     */
    function getDefaultStats() {
        return {
            totalPracticeCount: 0,
            correctCount: 0,
            incorrectCount: 0,
            correctRate: 0,
            averageTimeSpent: 0
        };
    }

    /**
     * 处理统计数据，添加格式化字段
     * @param {Object} stats - 原始统计数据
     * @returns {Object} 处理后的统计数据
     */
    function processStats(stats) {
        let correctRatePercent = 0;
        
        if (stats.correctRate !== undefined && stats.correctRate !== null) {
            // 根据实际API返回格式处理正确率
            // API返回的correctRate是百分比数值（如12.5表示12.5%），直接使用即可
            let rawRate = parseFloat(stats.correctRate) || 0;
            
            // 边界检查：确保正确率在合理范围内
            if (rawRate < 0) {
                correctRatePercent = 0;
                console.warn('[我的练习] 正确率异常值(小于0):', rawRate, '已修正为0');
            } else if (rawRate > 100) {
                correctRatePercent = 100;
                console.warn('[我的练习] 正确率异常值(大于100):', rawRate, '已修正为100');
            } else {
                correctRatePercent = Math.round(rawRate);
            }
            
            console.log('[我的练习] 正确率处理: 原始值', stats.correctRate, '-> 显示值', correctRatePercent + '%');
        }
        
        const processedStats = {
            ...stats,
            correctRatePercent: correctRatePercent
        };
        
        return processedStats;
    }

    /**
     * 加载练习记录列表
     * @param {number} page - 页码
     * @param {number} pageSize - 每页大小
     * @returns {Promise<boolean>} 加载是否成功
     */
    async function loadPracticeRecords(page = 1, pageSize = CONSTANTS.PAGE_SIZE) {
        try {
            console.log('[我的练习] 开始加载练习记录列表');
            
            const queryParams = new URLSearchParams({
                page: page.toString(),
                pageSize: pageSize.toString()
            });

            if (searchKeyword) {
                queryParams.append('keywords', searchKeyword);
                console.log('[我的练习] 添加搜索关键词:', searchKeyword);
            }
            if (statusFilter) {
                queryParams.append('status', statusFilter);
                console.log('[我的练习] 添加状态筛选:', statusFilter);
            }

            const url = `/exam/api/client/practice/records?${queryParams}`;
            console.log('[我的练习] 请求URL:', url);

            const response = await fetch(url, {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': 'Bearer ' + (window.TokenManager ? window.TokenManager.getToken() : ''),
                    'X-Forwarded-With': 'CodeSpirit'
                }
            });

            if (!response.ok) {
                throw new Error(`HTTP error! Status: ${response.status}`);
            }

            const data = await response.json();
            console.log('[我的练习] API返回数据:', data);
            
            if (data.status === 0 || data.status === 'success') {
                const result = data.data;
                practiceRecords = result.items || [];
                currentPage = page;
                
                // 计算总页数
                const totalCount = result.total || 0;
                totalPages = Math.ceil(totalCount / pageSize);
                
                console.log('[我的练习] 练习记录加载成功:', {
                    records: practiceRecords.length,
                    currentPage,
                    totalPages,
                    totalCount
                });
                return true;
            } else {
                throw new Error(data.msg || data.message || '获取练习记录失败');
            }
        } catch (error) {
            console.error('[我的练习] 加载练习记录失败:', error);
            practiceRecords = [];
            return false;
        }
    }

    /**
     * 处理练习记录数据
     * @param {Array} records - 原始记录数据
     * @returns {Array} 处理后的记录数据
     */
    function processPracticeRecords(records) {
        return records.map(record => {
            // 根据API返回的实际字段进行映射
            const processedRecord = {
                id: record.id,
                practiceName: record.practiceName,
                practiceId: record.practiceId, // 用于重新练习
                studentId: record.studentId,
                studentName: record.studentName,
                startTime: record.startTime,
                endTime: record.endTime,
                duration: record.duration,
                score: record.score || 0,
                totalScore: record.totalScore || 0,
                totalQuestions: record.totalQuestions || 0,
                correctCount: record.correctCount || 0,
                createdTime: record.createdTime
            };

            // 添加计算字段
            processedRecord.statusText = getStatusText(getRecordStatus(processedRecord));
            processedRecord.statusClass = getStatusClass(getRecordStatus(processedRecord));
            processedRecord.scoreClass = getScoreClass(processedRecord.score, processedRecord.totalScore);
            processedRecord.formattedStartTime = formatDateTime(processedRecord.startTime);
            processedRecord.formattedEndTime = processedRecord.endTime ? formatDateTime(processedRecord.endTime) : '-';
            processedRecord.scoreDisplay = processedRecord.totalScore > 0 ? `${processedRecord.score}/${processedRecord.totalScore}` : '-';
            processedRecord.practiceSettingId = processedRecord.practiceId; // 兼容字段

            return processedRecord;
        });
    }

    /**
     * 获取记录状态
     * @param {Object} record - 练习记录
     * @returns {string} 状态值
     */
    function getRecordStatus(record) {
        // 根据API返回的实际数据判断状态
        if (record.endTime && record.endTime !== null) {
            return 'Completed';
        } else if (record.startTime && record.startTime !== null) {
            return 'InProgress';
        } else {
            return 'NotStarted';
        }
    }

    /**
     * 获取状态文本
     * @param {string} status - 状态值
     * @returns {string} 状态文本
     */
    function getStatusText(status) {
        const statusMap = {
            'InProgress': '进行中',
            'Completed': '已完成',
            'NotStarted': '未开始'
        };
        return statusMap[status] || status;
    }

    /**
     * 获取状态样式类
     * @param {string} status - 状态值
     * @returns {string} 样式类名
     */
    function getStatusClass(status) {
        const classMap = {
            'InProgress': 'in-progress',
            'Completed': 'completed',
            'NotStarted': 'not-started'
        };
        return classMap[status] || '';
    }

    /**
     * 获取分数样式类
     * @param {number} score - 得分
     * @param {number} totalScore - 总分
     * @returns {string} 样式类名
     */
    function getScoreClass(score, totalScore) {
        if (totalScore <= 0) return '';
        
        const percentage = (score / totalScore) * 100;
        if (percentage >= 80) return 'high';
        if (percentage >= 60) return 'medium';
        return 'low';
    }

    /**
     * 格式化日期时间
     * @param {string} dateTime - 日期时间字符串
     * @returns {string} 格式化后的日期时间
     */
    function formatDateTime(dateTime) {
        if (!dateTime) return '-';
        
        try {
            const date = new Date(dateTime);
            return date.toLocaleString('zh-CN', {
                year: 'numeric',
                month: '2-digit',
                day: '2-digit',
                hour: '2-digit',
                minute: '2-digit'
            });
        } catch (error) {
            console.error('[我的练习] 日期格式化失败:', error);
            return dateTime;
        }
    }

    /**
     * 搜索和筛选练习记录
     * @param {string} keyword - 搜索关键词
     * @param {string} status - 状态筛选
     */
    async function searchAndFilterPracticeRecords(keyword = '', status = '') {
        console.log('[我的练习] 执行搜索和筛选:', { keyword, status });
        
        searchKeyword = keyword;
        statusFilter = status;
        currentPage = 1;
        
        showLoading('正在搜索...');
        const success = await loadPracticeRecords(currentPage);
        hideLoading();
        
        if (success) {
            updatePracticeRecordsTable();
            console.log('[我的练习] 搜索完成，更新表格数据');
        } else {
            console.error('[我的练习] 搜索失败');
        }
    }

    /**
     * 搜索练习记录
     * @param {string} keyword - 搜索关键词
     */
    async function searchPracticeRecords(keyword) {
        await searchAndFilterPracticeRecords(keyword, statusFilter);
    }

    /**
     * 筛选练习记录
     * @param {string} status - 状态筛选
     */
    async function filterPracticeRecords(status) {
        await searchAndFilterPracticeRecords(searchKeyword, status);
    }

    /**
     * 切换页面
     * @param {number} page - 目标页码
     */
    async function changePage(page) {
        if (page < 1 || page > totalPages || page === currentPage) {
            return;
        }
        
        showLoading('正在加载...');
        const success = await loadPracticeRecords(page);
        hideLoading();
        
        if (success) {
            updatePracticeRecordsTable();
        }
    }

    /**
     * 获取首页URL
     * @returns {string} 首页URL
     */
    function getHomeUrl() {
        const tenantId = window.tenantId || '';
        return tenantId ? `/${tenantId}/exam/app` : '/exam/app';
    }

    /**
     * 获取练习中心URL
     * @returns {string} 练习中心URL
     */
    function getPracticeUrl() {
        const tenantId = window.tenantId || '';
        return tenantId ? `/${tenantId}/exam/practice` : '/exam/practice';
    }

    /**
     * 查看练习详情
     * @param {string} recordId - 练习记录ID
     */
    function viewPracticeDetail(recordId) {
        const tenantId = window.tenantId || '';
        const url = tenantId ? `/${tenantId}/exam/practice-result/${recordId}` : `/exam/practice-result/${recordId}`;
        window.open(url, '_blank');
    }

    /**
     * 重新练习
     * @param {string} practiceId - 练习设置ID
     */
    function retryPractice(practiceId) {
        const tenantId = window.tenantId || '';
        const url = tenantId ? `/${tenantId}/exam/practice/${practiceId}` : `/exam/practice/${practiceId}`;
        window.location.href = url;
    }

    /**
     * 统一的数据更新函数
     * @param {Object} newData - 要更新的数据
     * @param {boolean} forceUpdateProps - 是否强制使用updateProps
     */
    function updateAmisData(newData, forceUpdateProps = true) {
        if (!window.amisInstance) {
            console.warn('[我的练习] AMIS实例不存在，无法更新数据');
            return;
        }

        console.log('[我的练习] 更新AMIS数据:', newData);

        try {
            if (forceUpdateProps && window.amisInstance.updateProps) {
                const mergedData = mergeData(newData);
                console.log('[我的练习] 使用updateProps更新，合并后的数据:', mergedData);
                window.amisInstance.updateProps({
                    data: mergedData
                });
            } else if (window.amisInstance.updateData) {
                console.log('[我的练习] 使用updateData更新');
                window.amisInstance.updateData(newData);
            } else {
                console.warn('[我的练习] 没有可用的数据更新方法');
            }
        } catch (error) {
            console.error('[我的练习] 数据更新失败:', error);
            // 如果updateProps失败，尝试使用updateData作为备用方案
            if (forceUpdateProps && window.amisInstance.updateData) {
                console.log('[我的练习] updateProps失败，尝试使用updateData');
                try {
                    window.amisInstance.updateData(newData);
                } catch (fallbackError) {
                    console.error('[我的练习] updateData也失败了:', fallbackError);
                }
            }
        }
    }

    /**
     * 同时更新统计数据和练习记录
     */
    function updateAllData() {
        if (!window.amisInstance) {
            console.warn('[我的练习] AMIS实例不存在，无法更新数据');
            return;
        }

        const processedRecords = processPracticeRecords(practiceRecords);
        const processedStats = practiceStats ? processStats(practiceStats) : processStats(getDefaultStats());
        
        const allData = {
            stats: processedStats,
            practiceRecords: processedRecords,
            pagination: {
                currentPage,
                totalPages,
                hasNext: currentPage < totalPages,
                hasPrev: currentPage > 1
            }
        };
        
        console.log('[我的练习] 同时更新所有数据:', allData);
        updateAmisData(allData);
    }

    /**
     * 更新练习记录表格
     */
    function updatePracticeRecordsTable() {
        const processedRecords = processPracticeRecords(practiceRecords);
        
        const updateData = {
            practiceRecords: processedRecords,
            pagination: {
                currentPage,
                totalPages,
                hasNext: currentPage < totalPages,
                hasPrev: currentPage > 1
            }
        };
        
        console.log('[我的练习] 更新练习记录数据:', updateData);
        updateAmisData(updateData);
    }

    /**
     * 安全地获取当前AMIS实例的数据
     * @returns {Object} 当前数据对象
     */
    function getCurrentData() {
        try {
            if (window.amisInstance && window.amisInstance.props && window.amisInstance.props.data) {
                return window.amisInstance.props.data;
            }
        } catch (error) {
            console.warn('[我的练习] 获取当前数据失败:', error);
        }
        
        // 返回默认数据结构
        return {
            stats: processStats(getDefaultStats()),
            practiceRecords: [],
            pagination: {
                currentPage: 1,
                totalPages: 1,
                hasNext: false,
                hasPrev: false
            }
        };
    }

    /**
     * 安全地合并数据
     * @param {Object} newData - 新数据
     * @returns {Object} 合并后的数据
     */
    function mergeData(newData) {
        const currentData = getCurrentData();
        return {
            ...currentData,
            ...newData
        };
    }

    /**
     * 暴露全局方法
     */
    function exposeGlobalMethods() {
        window.getHomeUrl = getHomeUrl;
        window.getPracticeUrl = getPracticeUrl;
        window.searchAndFilterPracticeRecords = searchAndFilterPracticeRecords;
        window.searchPracticeRecords = searchPracticeRecords;
        window.filterPracticeRecords = filterPracticeRecords;
        window.changePage = changePage;
        window.viewPracticeDetail = viewPracticeDetail;
        window.retryPractice = retryPractice;
    }

    // 我的练习历史页面配置
    function getPracticeHistoryPageConfig(initialStats) {
        const tenantId = window.tenantId || '';
        const homeUrl = tenantId ? `/${tenantId}/exam/app` : '/exam/app';
        const practiceUrl = tenantId ? `/${tenantId}/exam/practice` : '/exam/practice';
        
        return {
            type: 'page',
            className: 'practice-history-page',
            data: {
                stats: initialStats || processStats(getDefaultStats()),
                practiceRecords: [],
                pagination: {
                    currentPage: 1,
                    totalPages: 1,
                    hasNext: false,
                    hasPrev: false
                }
            },
            body: [
                // 头部固定区域
                {
                    type: 'container',
                    className: 'practice-history-header-container',
                    style: {
                        backgroundColor: '#fff',
                        borderBottom: '1px solid #e9ecef',
                        padding: '12px 24px',
                        marginBottom: '16px'
                    },
                    body: [
                        {
                            type: 'container',
                            className: 'practice-history-client-header',
                            body: [
                                {
                                    type: 'flex',
                                    justify: 'space-between',
                                    className: 'w-full header-container',
                                    items: [
                                        {
                                            type: 'tpl',
                                            tpl: '<div class="logo"><img src="' + (window.siteSettings ? window.siteSettings.logoUrl : '/logo.png') + '" /><span>' + (window.siteSettings ? window.siteSettings.clientAppName : '考试系统') + '</span></div>',
                                            className: 'client-logo'
                                        },
                                        {
                                            type: 'flex',
                                            justify: 'flex-end',
                                            alignItems: 'center',
                                            className: 'user-info-container',
                                            gap: 'lg',
                                            items: [
                                                {
                                                    type: 'breadcrumb',
                                                    className: 'header-breadcrumb',
                                                    items: [
                                                        {
                                                            label: '首页',
                                                            href: homeUrl,
                                                            icon: 'fa fa-home'
                                                        },
                                                        {
                                                            label: '我的练习',
                                                            icon: 'fa fa-history'
                                                        }
                                                    ]
                                                }
                                            ]
                                        }
                                    ]
                                }
                            ]
                        }
                    ]
                },
                // 内容区域
                {
                    type: 'container',
                    className: 'practice-history-content-container',
                    body: [
                        // 统计面板 - 参考practice-start.js的设计
                        {
                            type: 'panel',
                            className: 'practice-history-panel',
                            header: {
                                type: 'tpl',
                                tpl: '<div class="practice-history-panel-title">练习统计</div>'
                            },
                            body: [
                                {
                                    type: 'html',
                                    html: `
                                        <div class="row">
                                            <div class="col-md-6 mb-3">
                                                <div class="card text-center border-primary">
                                                    <div class="card-body p-3">
                                                        <i class="fa fa-trophy text-primary" style="font-size: 2rem; margin-bottom: 10px; display: block;"></i>
                                                        <h3 class="text-primary mb-1">\${stats.totalPracticeCount || 0}</h3>
                                                        <small class="text-muted">总练习次数</small>
                                                    </div>
                                                </div>
                                            </div>
                                            <div class="col-md-6 mb-3">
                                                <div class="card text-center border-success">
                                                    <div class="card-body p-3">
                                                        <i class="fa fa-percent text-success" style="font-size: 2rem; margin-bottom: 10px; display: block;"></i>
                                                        <h3 class="text-success mb-1">\${stats.correctRatePercent || 0}%</h3>
                                                        <small class="text-muted">正确率</small>
                                                    </div>
                                                </div>
                                            </div>
                                        </div>
                                        <div class="row">
                                            <div class="col-md-6 mb-3">
                                                <div class="card bg-light">
                                                    <div class="card-body p-3 d-flex align-items-center">
                                                        <i class="fa fa-check-circle text-success mr-3" style="font-size: 1.5rem;"></i>
                                                        <div>
                                                            <strong>\${stats.correctCount || 0}</strong>
                                                            <div class="text-muted small">正确题数</div>
                                                        </div>
                                                    </div>
                                                </div>
                                            </div>
                                            <div class="col-md-6 mb-3">
                                                <div class="card bg-light">
                                                    <div class="card-body p-3 d-flex align-items-center">
                                                        <i class="fa fa-times-circle text-danger mr-3" style="font-size: 1.5rem;"></i>
                                                        <div>
                                                            <strong>\${stats.incorrectCount || 0}</strong>
                                                            <div class="text-muted small">错误题数</div>
                                                        </div>
                                                    </div>
                                                </div>
                                            </div>
                                        </div>
                                    `
                                }
                            ]
                        },
                        // 练习记录面板
                        {
                            type: 'panel',
                            className: 'practice-history-panel',
                            header: {
                                type: 'tpl',
                                tpl: '<div class="practice-history-panel-title">练习记录</div>'
                            },
                            body: [
                                // 搜索和筛选区域
                                {
                                    type: 'container',
                                    className: 'search-filter-container mb-3',
                                    style: {
                                        padding: '16px',
                                        backgroundColor: '#f8f9fa',
                                        borderRadius: '6px',
                                        border: '1px solid #e9ecef'
                                    },
                                    body: [
                                        {
                                            type: 'form',
                                            className: 'search-filter-form',
                                            wrapWithPanel: false,
                                            mode: 'inline',
                                            onEvent: {
                                                submitSucc: {
                                                    actions: [
                                                        {
                                                            actionType: 'custom',
                                                            script: `
                                                                const formData = event.data;
                                                                console.log('搜索表单数据:', formData);
                                                                
                                                                // 执行搜索和筛选
                                                                if (window.searchAndFilterPracticeRecords) {
                                                                    window.searchAndFilterPracticeRecords(
                                                                        formData.searchKeyword || '', 
                                                                        formData.statusFilter || ''
                                                                    );
                                                                }
                                                            `
                                                        }
                                                    ]
                                                }
                                            },
                                            body: [
                                                {
                                                    type: 'group',
                                                    body: [
                                                        {
                                                            type: 'input-text',
                                                            name: 'searchKeyword',
                                                            placeholder: '搜索练习名称...',
                                                            clearable: true,
                                                            className: 'mr-2',
                                                            style: {
                                                                width: '250px'
                                                            }
                                                        },
                                                        {
                                                            type: 'select',
                                                            name: 'statusFilter',
                                                            placeholder: '选择状态',
                                                            clearable: true,
                                                            className: 'mr-2',
                                                            style: {
                                                                width: '150px'
                                                            },
                                                            options: [
                                                                { label: '全部状态', value: '' },
                                                                { label: '已完成', value: 'Completed' },
                                                                { label: '进行中', value: 'InProgress' },
                                                                { label: '未开始', value: 'NotStarted' }
                                                            ]
                                                        },
                                                        {
                                                            type: 'submit',
                                                            label: '搜索',
                                                            level: 'primary'
                                                        }
                                                    ]
                                                }
                                            ]
                                        }
                                    ]
                                },
                                // 练习记录表格
                                {
                                    type: 'table',
                                    id: 'practice-records-table',
                                    className: 'practice-records-table',
                                    source: '${practiceRecords}',
                                    columns: [
                                        {
                                            name: 'practiceName',
                                            label: '练习名称',
                                            type: 'text'
                                        },
                                        {
                                            name: 'formattedStartTime',
                                            label: '开始时间',
                                            type: 'text'
                                        },
                                        {
                                            name: 'formattedEndTime',
                                            label: '结束时间',
                                            type: 'text'
                                        },
                                        {
                                            name: 'scoreDisplay',
                                            label: '得分',
                                            type: 'tpl',
                                            tpl: '<span class="score-display ${scoreClass}">${scoreDisplay}</span>'
                                        },
                                        {
                                            name: 'statusText',
                                            label: '状态',
                                            type: 'tpl',
                                            tpl: '<span class="status-badge ${statusClass}">${statusText}</span>'
                                        },
                                        {
                                            name: 'actions',
                                            label: '操作',
                                            type: 'tpl',
                                            tpl: '<div class="action-buttons"><button class="action-btn view" onclick="viewPracticeDetail(\'${id}\')">查看详情</button><button class="action-btn retry" onclick="retryPractice(\'${practiceId}\')">重新练习</button></div>'
                                        }
                                    ],
                                    placeholder: {
                                        type: 'container',
                                        className: 'empty-state',
                                        body: [
                                            {
                                                type: 'tpl',
                                                tpl: '<div class="empty-state-icon">📝</div><div class="empty-state-title">暂无练习记录</div><div class="empty-state-description">您还没有参加过任何练习</div><a href="/' + window.tenantId + '/exam/practice" class="empty-state-action">开始练习</a>'
                                            }
                                        ]
                                    }
                                }
                            ]
                        }
                    ]
                }
            ]
        };
    }

    /**
     * 初始化Amis实例
     */
    function initializeAmis() {
        console.log('[我的练习] 开始初始化Amis实例');
        
        // 初始化数据
        const initialData = {
            stats: processStats(getDefaultStats()),
            practiceRecords: [],
            pagination: {
                currentPage: 1,
                totalPages: 1,
                hasNext: false,
                hasPrev: false
            }
        };

        // 初始化AMIS
        let amisInstance = amis.embed(
            '#root',
            getPracticeHistoryPageConfig(initialData.stats),
            {
                location: history.location,
                data: initialData, // 在环境中也设置数据
                locale: 'zh-CN',
                context: {
                    WEB_HOST: window.webHost || ''
                }
            },
            {
                updateLocation: (location) => {
                    history.push(location);
                },
                jumpTo: (to) => {
                    history.push(to);
                },
                requestAdaptor: (api) => {
                    var token = window.TokenManager ? window.TokenManager.getToken() : '';
                    return {
                        ...api,
                        headers: {
                            ...api.headers,
                            'Authorization': 'Bearer ' + token,
                            'X-Forwarded-With': 'CodeSpirit'
                        }
                    };
                },
                responseAdaptor: function (api, payload, query, request, response) {
                    if (response.status === 403) {
                        alert('您没有权限访问此页面！');
                        window.location.href = `/${window.tenantId}/exam/app`;
                        return { msg: '您没有权限访问此页面，请联系管理员！' };
                    }
                    else if (response.status === 401) {
                        window.location.href = `/${window.tenantId}/exam/login`;
                        return { msg: '登录过期！' };
                    }
                    return payload;
                },
                theme: 'antd'
            }
        );

        // 设置全局AMIS实例
        window.amisInstance = amisInstance;

        // 暴露全局方法
        exposeGlobalMethods();
        
        console.log('[我的练习] Amis实例初始化完成');
    }

    /**
     * 显示加载状态
     * @param {string} text - 加载提示文本
     */
    function showLoading(text = '正在加载我的练习...') {
        const loadingElement = document.getElementById('loading');
        if (loadingElement) {
            loadingElement.style.display = 'flex';
            const textElement = loadingElement.querySelector('.practice-history-loading-text');
            if (textElement) {
                textElement.textContent = text;
            }
        }
    }

    /**
     * 隐藏加载状态
     */
    function hideLoading() {
        const loadingElement = document.getElementById('loading');
        if (loadingElement) {
            loadingElement.style.display = 'none';
        }
    }

    // 页面加载完成后初始化
    window.addEventListener('DOMContentLoaded', async function() {
        console.log('[我的练习] 页面加载完成，开始初始化...');
        
        showLoading('正在加载页面资源...');
        
        try {
            
            // 初始化Amis
            initializeAmis();
            
            showLoading('正在加载练习数据...');
            
            // 并行加载统计数据和练习记录
            const [statsSuccess, recordsSuccess] = await Promise.all([
                loadPracticeStatistics(),
                loadPracticeRecords(1)
            ]);
            
            // 使用统一的数据更新函数
            if (statsSuccess || recordsSuccess) {
                updateAllData();
            } else {
                // 如果都失败了，至少更新默认数据
                updateAmisData({
                    stats: processStats(getDefaultStats()),
                    practiceRecords: [],
                    pagination: {
                        currentPage: 1,
                        totalPages: 1,
                        hasNext: false,
                        hasPrev: false
                    }
                });
            }
            
            isInitialized = true;
            hideLoading();
            console.log('[我的练习] 初始化完成');
            
        } catch (error) {
            hideLoading();
            console.error('[我的练习] 初始化过程中出错:', error);
            alert('页面加载失败，请刷新页面重试');
        }
    });

    // 页面卸载时的清理
    window.addEventListener('beforeunload', function() {
        console.log('[我的练习] 页面即将卸载，执行清理...');
    });

})(); 