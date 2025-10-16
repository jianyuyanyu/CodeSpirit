/**
 * 练习开始页面 - 基于AMIS框架
 * 包含租户信息、练习统计、可参加的练习列表等功能
 * 支持移动端适配，前端获取用户信息
 */
(function () {
    'use strict';
    
    // Initialize TokenManager
    if (typeof TokenManager !== 'undefined') {
        TokenManager.initClientMode(window.tenantId, 'exam');
        
        // Check auth
        if (!TokenManager.isAuthenticated()) {
            window.location.href = `/${window.tenantId}/exam/login`;
            return;
        }
    }
    
    let amis, amisInstance = null;
    
    // Initialize AMIS when available
    function initAmis() {
        if (typeof amisRequire !== 'undefined') {
            amis = amisRequire('amis/embed');
            loadPage();
        } else {
            setTimeout(initAmis, 100);
        }
    }
    
    // App data
    const appData = {
        tenant: { name: '练习平台' },
        practiceStatistics: {
            totalPracticeCount: 0,
            correctCount: 0,
            incorrectCount: 0,
            correctRate: 0
        },
        availablePractices: []
    };
    
    // API request function
    async function apiRequest(url, options = {}) {
        try {
            const token = TokenManager?.getToken();
            const response = await fetch(window.ExamApiManager.transformUrl(url), {
                ...options,
                headers: {
                    'Authorization': token ? 'Bearer ' + token : '',
                    'TenantId': window.tenantId,
                    'Content-Type': 'application/json',
                    ...options.headers
                }
            });
            
            if (response.status === 401) {
                window.location.href = `/${window.tenantId}/exam/login`;
                throw new Error('认证失败');
            }
            
            if (!response.ok) {
                throw new Error(`HTTP ${response.status}`);
            }
            
            const result = await response.json();
            if (!result) {
                throw new Error('返回数据为空');
            }
            
            // 如果返回的是直接数据，则直接返回
            if (result.recordId || result.id) {
                return result;
            }
            
            // 否则检查标准响应格式
            if (result.status !== 0) {
                throw new Error(result.msg || '请求失败');
            }
            
            return result.data || result;
        } catch (error) {
            console.error('API请求失败:', error);
            throw error;
        }
    }
    
    // Build page config using Amis Card components
    function buildPageConfig() {
        return {
            type: "page",
            title: "",
            className: "practice-app-page",
            data: {
                tenant: appData.tenant,
                practiceStatistics: appData.practiceStatistics,
                availablePractices: appData.availablePractices,
                now: new Date()
            },
            body: [
                // 顶部信息栏
                {
                    type: "flex",
                    className: "bg-primary text-white p-3",
                    justify: "space-between",
                    alignItems: "center",
                    items: [
                        {
                            type: "tpl",
                            tpl: "<i class='fa fa-leaf mr-2'></i>${tenant.name}"
                        },
                        {
                            type: "flex",
                            alignItems: "center",
                            items: [
                                {
                                    type: "tpl",
                                    tpl: "<i class='fa fa-clock-o mr-2'></i>${now | date:'HH:mm'}"
                                },
                                {
                                    type: "button",
                                    icon: "fa fa-sign-out",
                                    level: "link",
                                    className: "text-white ml-3",
                                    tooltip: "退出登录",
                                    onEvent: {
                                        click: {
                                            actions: [
                                                {
                                                    actionType: "custom",
                                                    script: "window.handleLogout()"
                                                }
                                            ]
                                        }
                                    }
                                }
                            ]
                        }
                    ]
                },
                
                // 练习统计卡片
                {
                    type: "panel",
                    title: "练习统计",
                    className: "m-3",
                    body: [
                        {
                            type: "html",
                            html: `
                                <div class="row">
                                    <div class="col-md-6 mb-3">
                                        <div class="card text-center border-primary">
                                            <div class="card-body p-3">
                                                <i class="fa fa-trophy text-primary" style="font-size: 2rem; margin-bottom: 10px; display: block;"></i>
                                                <h3 class="text-primary mb-1">\${practiceStatistics.totalPracticeCount || 0}</h3>
                                                <small class="text-muted">总练习次数</small>
                                            </div>
                                        </div>
                                    </div>
                                    <div class="col-md-6 mb-3">
                                        <div class="card text-center border-success">
                                            <div class="card-body p-3">
                                                <i class="fa fa-percent text-success" style="font-size: 2rem; margin-bottom: 10px; display: block;"></i>
                                                <h3 class="text-success mb-1">\${practiceStatistics.correctRate || 0}%</h3>
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
                                                    <strong>\${practiceStatistics.correctCount || 0}</strong>
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
                                                    <strong>\${practiceStatistics.incorrectCount || 0}</strong>
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
                
                // 可参加的练习
                {
                    type: "panel",
                    title: "可参加的练习",
                    className: "m-3",
                    body: {
                        type: "list",
                        source: "${availablePractices}",
                        listItem: {
                            type: "html",
                            className: "mb-4",
                            html: `
                                <div class="practice-item-card">
                                    <div class="practice-card-header">
                                        <div class="practice-title-section">
                                            <h4 class="practice-title">\${name}</h4>
                                            <span class="difficulty-badge \${difficulty == 1 ? 'difficulty-easy' : difficulty == 2 ? 'difficulty-medium' : 'difficulty-hard'}">
                                                \${difficulty == 1 ? '简单' : difficulty == 2 ? '中等' : '困难'}
                                            </span>
                                        </div>
                                    </div>
                                    
                                    <div class="practice-card-body">
                                        <p class="practice-description">\${description || '暂无描述'}</p>
                                        
                                        <div class="practice-info-grid">
                                            <div class="info-item">
                                                <div class="info-icon">
                                                    <i class="fa fa-cog"></i>
                                                </div>
                                                <div class="info-content">
                                                    <span class="info-label">练习模式</span>
                                                    <span class="info-value">\${practiceMode == 1 ? '练习模式' : '考试模式'}</span>
                                                </div>
                                            </div>
                                            
                                            <div class="info-item">
                                                <div class="info-icon">
                                                    <i class="fa fa-clock-o"></i>
                                                </div>
                                                <div class="info-content">
                                                    <span class="info-label">时间限制</span>
                                                    <span class="info-value">\${timeLimit ? timeLimit + '分钟' : '不限时'}</span>
                                                </div>
                                            </div>
                                            
                                            <div class="info-item">
                                                <div class="info-icon">
                                                    <i class="fa fa-question-circle"></i>
                                                </div>
                                                <div class="info-content">
                                                    <span class="info-label">题目数量</span>
                                                    <span class="info-value">\${questionCount || 0} 题</span>
                                                </div>
                                            </div>
                                        </div>
                                    </div>
                                    
                                    <div class="practice-card-footer">
                                        <div class="practice-meta">
                                            <i class="fa fa-calendar"></i>
                                            <span>创建时间: \${createdAtFormatted || '未知'}</span>
                                        </div>
                                        <button class="practice-start-btn" onclick="window.startPractice('\${id}')">
                                            <i class="fa fa-play"></i>
                                            <span>开始练习</span>
                                        </button>
                                    </div>
                                </div>
                            `
                        },
                        placeholder: "暂无可参加的练习"
                    }
                }
            ]
        };
    }
    
    // Start practice
    window.startPractice = async function(practiceId) {
        try {
            showLoading(true);
            const result = await apiRequest(`/exam/api/client/practice/${practiceId}/start`, {
                method: 'POST'
            });
            
            if (result && result.recordId) {
                window.location.href = `/${window.tenantId}/exam/practice/${practiceId}?recordId=${result.recordId}`;
            } else {
                throw new Error('开始练习失败：未获取到练习记录ID');
            }
        } catch (error) {
            console.error('开始练习失败:', error);
            alert('开始练习失败: ' + error.message);
        } finally {
            showLoading(false);
        }
    };
    
    // Handle logout
    window.handleLogout = function() {
        if (TokenManager) {
            TokenManager.clearAuth();
        }
        window.location.href = `/${window.tenantId}/exam/login`;
    };
    
    // Format date for display
    function formatDate(dateString) {
        if (!dateString) return '未知';
        try {
            return new Date(dateString).toLocaleDateString('zh-CN');
        } catch (e) {
            return '未知';
        }
    }

    // Load data
    async function loadData() {
        try {
            showLoading(true);
            
            const [stats, practices] = await Promise.all([
                apiRequest(`/exam/api/client/practice/analysis`).catch(() => ({})),
                apiRequest(`/exam/api/client/practice/settings`).catch(() => ([]))
            ]);
            
            Object.assign(appData.practiceStatistics, stats);
            // Format dates in practice data
            appData.availablePractices = practices.map(practice => ({
                ...practice,
                createdAtFormatted: formatDate(practice.createdAt)
            }));
            
            renderPage();
        } catch (error) {
            console.error('加载数据失败:', error);
            showError('页面加载失败: ' + error.message);
        } finally {
            showLoading(false);
        }
    }
    
    // Render page
    function renderPage() {
        const pageConfig = buildPageConfig();
        const renderData = {
            tenant: appData.tenant,
            practiceStatistics: appData.practiceStatistics,
            availablePractices: appData.availablePractices,
            now: new Date().toISOString()
        };
        
        if (amisInstance) {
            amisInstance.updateProps({
                ...pageConfig,
                data: renderData
            });
        } else {
            amisInstance = amis.embed('#root', pageConfig, renderData);
        }
    }
    
    // Show loading
    function showLoading(show) {
        const loadingEl = document.getElementById('loading');
        if (loadingEl) {
            loadingEl.style.display = show ? 'flex' : 'none';
        }
    }
    
    // Show error
    function showError(message) {
        const errorConfig = {
            type: "page",
            className: "practice-app-page",
            body: [
                {
                    type: "container",
                    className: "practice-error",
                    body: [
                        {
                            type: "tpl",
                            tpl: `<div class='error-content'><h3>加载失败</h3><p>${message}</p><button onclick='window.location.reload()'>重新加载</button></div>`
                        }
                    ]
                }
            ]
        };
        
        if (amisInstance) {
            amisInstance.updateProps(errorConfig);
        } else if (amis) {
            amisInstance = amis.embed('#root', errorConfig);
        }
    }
    
    // Load page
    async function loadPage() {
        try {
            await loadData();
            
            // Update time every minute
            setInterval(() => {
                if (amisInstance) {
                    amisInstance.updateProps({
                        data: {
                            ...amisInstance.props.data,
                            now: new Date()
                        }
                    });
                }
            }, 60000);
        } catch (error) {
            console.error('页面初始化失败:', error);
            showError('页面初始化失败');
        }
    }
    
    // Initialize when DOM ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initAmis);
    } else {
        initAmis();
    }
    
})(); 