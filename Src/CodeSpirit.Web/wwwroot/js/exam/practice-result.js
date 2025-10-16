/**
 * 练习详情页面
 * 显示练习结果、答题详情和统计信息
 */

(function() {
    'use strict';

    // 常量定义
    const CONSTANTS = {
        MAX_RETRY_COUNT: 3,
        RETRY_DELAY: 1000
    };

    // 全局变量
    let isInitialized = false;
    let practiceResult = null;
    let recordId = null;
    let amis = amisRequire('amis/embed');
    let amisInstance = null;

    /**
     * 从URL获取练习记录ID
     * @returns {string|null} 练习记录ID
     */
    function getRecordIdFromUrl() {
        const pathParts = window.location.pathname.split('/');
        const resultIndex = pathParts.indexOf('practice-result');
        if (resultIndex !== -1 && resultIndex + 1 < pathParts.length) {
            return pathParts[resultIndex + 1];
        }
        return null;
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
     * 获取练习历史URL
     * @returns {string} 练习历史URL
     */
    function getPracticeHistoryUrl() {
        const tenantId = window.tenantId || '';
        return tenantId ? `/${tenantId}/exam/practice-history` : '/exam/practice-history';
    }

    /**
     * 加载练习结果数据
     * @returns {Promise<boolean>} 加载是否成功
     */
    async function loadPracticeResult() {
        try {
            console.log('[练习详情] 开始加载练习结果数据, recordId:', recordId);
            
            const response = await fetch(window.ExamApiManager.transformUrl(`/exam/api/client/practice/result/${recordId}`), {
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
            console.log('[练习详情] API返回数据:', data);
            
            if (data.status === 0 || data.status === 'success') {
                practiceResult = data.data;
                console.log('[练习详情] 练习结果加载成功:', practiceResult);
                return true;
            } else {
                throw new Error(data.msg || data.message || '获取练习结果失败');
            }
        } catch (error) {
            console.error('[练习详情] 加载练习结果失败:', error);
            practiceResult = null;
            return false;
        }
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
            console.error('[练习详情] 日期格式化失败:', error);
            return dateTime;
        }
    }

    /**
     * 格式化持续时间
     * @param {number} minutes - 分钟数
     * @returns {string} 格式化后的时间
     */
    function formatDuration(minutes) {
        if (!minutes || minutes <= 0) return '0分钟';
        
        const hours = Math.floor(minutes / 60);
        const mins = minutes % 60;
        
        if (hours > 0) {
            return `${hours}小时${mins}分钟`;
        } else {
            return `${mins}分钟`;
        }
    }

    /**
     * 获取正确率百分比
     * @param {number} correctCount - 正确题数
     * @param {number} totalCount - 总题数
     * @returns {number} 正确率百分比
     */
    function getCorrectRatePercent(correctCount, totalCount) {
        if (!totalCount || totalCount <= 0) return 0;
        return Math.round((correctCount / totalCount) * 100);
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
     * 返回练习历史
     */
    function goBackToHistory() {
        window.location.href = getPracticeHistoryUrl();
    }

    /**
     * 暴露全局方法
     */
    function exposeGlobalMethods() {
        window.retryPractice = retryPractice;
        window.goBackToHistory = goBackToHistory;
        
        // 为AMIS按钮提供包装函数
        window.handleRetryPractice = function() {
            console.log('[练习详情] 点击重新练习按钮');
            const practiceSettingId = practiceResult?.practiceSettingId || 0;
            console.log('[练习详情] 练习设置ID:', practiceSettingId);
            retryPractice(practiceSettingId);
        };
        
        window.handleGoBackToHistory = function() {
            console.log('[练习详情] 点击返回练习历史按钮');
            goBackToHistory();
        };
    }

    // 练习详情页面配置
    function getPracticeResultPageConfig() {
        const tenantId = window.tenantId || '';
        const homeUrl = tenantId ? `/${tenantId}/exam/app` : '/exam/app';
        const practiceUrl = tenantId ? `/${tenantId}/exam/practice` : '/exam/practice';
        const historyUrl = tenantId ? `/${tenantId}/exam/practice-history` : '/exam/practice-history';
        
        return {
            type: 'page',
            className: 'practice-result-page',
            data: {
                result: practiceResult || {}
            },
            body: [
                // 头部固定区域（与练习历史页面保持一致）
                {
                    type: 'container',
                    className: 'practice-result-header-container',
                    style: {
                        backgroundColor: '#fff',
                        borderBottom: '1px solid #e9ecef',
                        padding: '12px 24px',
                        marginBottom: '16px'
                    },
                    body: [
                        {
                            type: 'container',
                            className: 'practice-result-client-header',
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
                                                            href: historyUrl,
                                                            icon: 'fa fa-history'
                                                        },
                                                        {
                                                            label: '练习详情',
                                                            icon: 'fa fa-file-text'
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
                    className: 'practice-result-content-container',
                    body: [
                        // 练习基本信息面板
                        {
                            type: 'panel',
                            className: 'practice-result-panel',
                            style: {
                                marginBottom: '20px'
                            },
                            header: {
                                type: 'tpl',
                                tpl: '<div class="practice-result-panel-title"><i class="fa fa-trophy"></i> 练习结果</div>'
                            },
                            body: [
                                {
                                    type: 'tpl',
                                    tpl: `
                                        <div class="practice-result-summary" style="padding: 20px;">
                                            <div class="row">
                                                <div class="col-md-8">
                                                    <h3 class="practice-name" style="color: #2c3e50; margin-bottom: 20px;">
                                                        <i class="fa fa-book text-primary"></i> \${result.name || '练习'}
                                                    </h3>
                                                    <div class="practice-info">
                                                        <div class="info-item" style="margin-bottom: 12px; padding: 8px; background: #f8f9fa; border-radius: 4px;">
                                                            <i class="fa fa-calendar text-info"></i>
                                                            <span style="margin-left: 8px;"><strong>开始时间：</strong>\${result.formattedStartTime || '-'}</span>
                                                        </div>
                                                        <div class="info-item" style="margin-bottom: 12px; padding: 8px; background: #f8f9fa; border-radius: 4px;">
                                                            <i class="fa fa-clock-o text-warning"></i>
                                                            <span style="margin-left: 8px;"><strong>结束时间：</strong>\${result.formattedEndTime || '-'}</span>
                                                        </div>
                                                        <div class="info-item" style="margin-bottom: 12px; padding: 8px; background: #f8f9fa; border-radius: 4px;">
                                                            <i class="fa fa-hourglass-half text-success"></i>
                                                            <span style="margin-left: 8px;"><strong>用时：</strong>\${result.formattedDuration || '-'}</span>
                                                        </div>
                                                    </div>
                                                </div>
                                                <div class="col-md-4">
                                                    <div class="score-display" style="text-align: center; padding: 20px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); border-radius: 10px; color: white;">
                                                        <div class="score-number" style="font-size: 3rem; font-weight: bold; line-height: 1;">\${result.score || 0}</div>
                                                        <div class="score-total" style="font-size: 1.2rem; opacity: 0.9;">/ \${result.totalScore || 0}</div>
                                                        <div class="score-label" style="font-size: 0.9rem; margin-top: 8px; opacity: 0.8;">总分</div>
                                                    </div>
                                                </div>
                                            </div>
                                        </div>
                                    `
                                }
                            ]
                        },
                        // 统计信息面板
                        {
                            type: 'panel',
                            className: 'practice-result-panel',
                            style: {
                                marginBottom: '20px'
                            },
                            header: {
                                type: 'tpl',
                                tpl: '<div class="practice-result-panel-title"><i class="fa fa-bar-chart"></i> 答题统计</div>'
                            },
                            body: [
                                {
                                    type: 'tpl',
                                    tpl: `
                                        <div style="padding: 20px;">
                                            <div class="row">
                                                <div class="col-md-3 mb-3">
                                                    <div class="card text-center border-primary" style="transition: transform 0.2s;">
                                                        <div class="card-body p-3">
                                                            <i class="fa fa-list text-primary" style="font-size: 2rem; margin-bottom: 10px; display: block;"></i>
                                                            <h3 class="text-primary mb-1">\${result.totalCount || 0}</h3>
                                                            <small class="text-muted">总题数</small>
                                                        </div>
                                                    </div>
                                                </div>
                                                <div class="col-md-3 mb-3">
                                                    <div class="card text-center border-success" style="transition: transform 0.2s;">
                                                        <div class="card-body p-3">
                                                            <i class="fa fa-check-circle text-success" style="font-size: 2rem; margin-bottom: 10px; display: block;"></i>
                                                            <h3 class="text-success mb-1">\${result.correctCount || 0}</h3>
                                                            <small class="text-muted">正确题数</small>
                                                        </div>
                                                    </div>
                                                </div>
                                                <div class="col-md-3 mb-3">
                                                    <div class="card text-center border-danger" style="transition: transform 0.2s;">
                                                        <div class="card-body p-3">
                                                            <i class="fa fa-times-circle text-danger" style="font-size: 2rem; margin-bottom: 10px; display: block;"></i>
                                                            <h3 class="text-danger mb-1">\${result.incorrectCount || 0}</h3>
                                                            <small class="text-muted">错误题数</small>
                                                        </div>
                                                    </div>
                                                </div>
                                                <div class="col-md-3 mb-3">
                                                    <div class="card text-center border-info" style="transition: transform 0.2s;">
                                                        <div class="card-body p-3">
                                                            <i class="fa fa-percent text-info" style="font-size: 2rem; margin-bottom: 10px; display: block;"></i>
                                                            <h3 class="text-info mb-1">\${result.correctRatePercent || 0}%</h3>
                                                            <small class="text-muted">正确率</small>
                                                        </div>
                                                    </div>
                                                </div>
                                            </div>
                                        </div>
                                    `
                                }
                            ]
                        },
                        // 操作按钮面板
                        {
                            type: 'panel',
                            className: 'practice-result-panel',
                            style: {
                                textAlign: 'center',
                                padding: '20px'
                            },
                            body: [
                                {
                                    type: 'tpl',
                                    tpl: `
                                        <div class="result-actions" style="display: flex; gap: 12px; justify-content: center; margin-top: 20px;">
                                            <button type="button" class="btn btn-primary btn-md" onclick="window.handleRetryPractice && window.handleRetryPractice()">
                                                <i class="fa fa-refresh"></i> 重新练习
                                            </button>
                                            <button type="button" class="btn btn-secondary btn-md" onclick="window.handleGoBackToHistory && window.handleGoBackToHistory()">
                                                <i class="fa fa-arrow-left"></i> 返回练习历史
                                            </button>
                                            <a href="${practiceUrl}" class="btn btn-info btn-md">
                                                <i class="fa fa-edit"></i> 练习中心
                                            </a>
                                        </div>
                                    `
                                }
                            ]
                        }
                    ]
                }
            ]
        };
    }

    /**
     * 获取默认练习结果数据
     */
    function getDefaultPracticeResult() {
        return {
            id: 0,
            name: '练习',
            practiceSettingId: 0,
            startTime: null,
            endTime: null,
            score: 0,
            totalScore: 0,
            correctCount: 0,
            totalCount: 0,
            formattedStartTime: '-',
            formattedEndTime: '-',
            formattedDuration: '-',
            incorrectCount: 0,
            correctRatePercent: 0
        };
    }

    /**
     * 处理练习结果数据
     */
    function processPracticeResult() {
        if (!practiceResult) {
            practiceResult = getDefaultPracticeResult();
            console.warn('[练习详情] 使用默认练习结果数据');
            return;
        }

        // 添加格式化字段
        practiceResult.formattedStartTime = formatDateTime(practiceResult.startTime);
        practiceResult.formattedEndTime = formatDateTime(practiceResult.endTime);
        
        // 计算用时（如果有开始和结束时间）
        if (practiceResult.startTime && practiceResult.endTime) {
            const start = new Date(practiceResult.startTime);
            const end = new Date(practiceResult.endTime);
            const durationMinutes = Math.round((end - start) / (1000 * 60));
            practiceResult.formattedDuration = formatDuration(durationMinutes);
        } else {
            practiceResult.formattedDuration = formatDuration(practiceResult.duration);
        }
        
        // 计算统计数据
        const totalCount = practiceResult.totalCount || 0;
        const correctCount = practiceResult.correctCount || 0;
        
        practiceResult.incorrectCount = totalCount - correctCount;
        practiceResult.correctRatePercent = getCorrectRatePercent(correctCount, totalCount);

        console.log('[练习详情] 处理后的结果数据:', practiceResult);
    }

    /**
     * 更新页面数据
     */
    function updatePageData() {
        if (window.amisInstance && practiceResult) {
            console.log('[练习详情] 更新页面数据:', practiceResult);
            
            try {
                if (window.amisInstance.updateProps) {
                    window.amisInstance.updateProps({
                        data: {
                            result: practiceResult
                        }
                    });
                    console.log('[练习详情] 使用updateProps更新数据成功');
                } else if (window.amisInstance.updateData) {
                    window.amisInstance.updateData({
                        result: practiceResult
                    });
                    console.log('[练习详情] 使用updateData更新数据成功');
                } else {
                    console.warn('[练习详情] 没有可用的数据更新方法');
                }
            } catch (error) {
                console.error('[练习详情] 数据更新失败:', error);
            }
        } else {
            console.warn('[练习详情] 无法更新数据 - amisInstance:', !!window.amisInstance, 'practiceResult:', !!practiceResult);
        }
    }

    /**
     * 初始化Amis实例
     */
    function initializeAmis() {
        console.log('[练习详情] 开始初始化Amis实例');
        
        // 初始化AMIS
        let amisInstance = amis.embed(
            '#root',
            getPracticeResultPageConfig(),
            {
                location: history.location,
                data: {
                    result: practiceResult || {}
                },
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
        
        console.log('[练习详情] Amis实例初始化完成');
    }

    /**
     * 显示加载状态
     * @param {string} text - 加载提示文本
     */
    function showLoading(text = '正在加载练习详情...') {
        const loadingElement = document.getElementById('loading');
        if (loadingElement) {
            loadingElement.style.display = 'flex';
            const textElement = loadingElement.querySelector('.practice-result-loading-text');
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
        console.log('[练习详情] 页面加载完成，开始初始化...');
        
        // 获取记录ID
        recordId = getRecordIdFromUrl();
        if (!recordId) {
            alert('练习记录ID不存在！');
            window.location.href = getPracticeHistoryUrl();
            return;
        }

        showLoading('正在加载页面资源...');
        
        try {
            // 初始化Amis
            initializeAmis();
            
            showLoading('正在加载练习详情...');
            
            // 加载练习结果数据
            const success = await loadPracticeResult();
            
            // 无论是否成功，都处理数据并更新页面
            processPracticeResult();
            updatePageData();
            
            if (!success) {
                console.warn('[练习详情] 加载练习详情失败，使用默认数据显示页面');
                // 可以选择显示警告，但不阻止页面显示
                // alert('加载练习详情失败，显示默认数据！');
            }
            
            isInitialized = true;
            hideLoading();
            console.log('[练习详情] 初始化完成');
            
        } catch (error) {
            hideLoading();
            console.error('[练习详情] 初始化过程中出错:', error);
            alert('页面加载失败，请刷新页面重试');
        }
    });

    // 页面卸载时的清理
    window.addEventListener('beforeunload', function() {
        console.log('[练习详情] 页面即将卸载，执行清理...');
    });

})(); 