/**
 * 问卷列表页面JavaScript
 * 负责初始化问卷列表、处理搜索和交互
 */

(function() {
    'use strict';

    // 全局变量
    let amisInstance = null;
    let currentFilters = {};
    let currentTenantId = null;

    /**
     * 从URL路径解析租户ID
     */
    function getTenantIdFromPath() {
        try {
            const pathname = window.location.pathname;
            const pathParts = pathname.split('/').filter(part => part.length > 0);
            
            // 支持路径格式：/{tenantId}/survey
            // 其中 tenantId 是可选的
            
            const surveyIndex = pathParts.indexOf('survey');
            if (surveyIndex > 0) {
                // 检查 /{tenantId}/survey 格式
                // 租户ID在survey之前的位置
                return pathParts[surveyIndex - 1];
            }
            
            return null;
        } catch (error) {
            console.warn('从URL路径解析租户ID失败:', error);
            return null;
        }
    }

    /**
     * 获取租户信息
     */
    function getTenantInfo() {
        try {
            // 优先从URL路径获取租户ID（用于公开问卷）
            currentTenantId = getTenantIdFromPath();
            
            // 如果路径中没有，尝试从页面数据获取
            if (!currentTenantId && window.surveyPageData && window.surveyPageData.tenantId) {
                currentTenantId = window.surveyPageData.tenantId;
            }
            
            // 如果还是没有，尝试从TokenManager获取（用于登录用户）
            if (!currentTenantId && window.TokenManager && typeof window.TokenManager.getCurrentTenantId === 'function') {
                currentTenantId = window.TokenManager.getCurrentTenantId();
            }
            
            // 最后尝试从platformInfo获取
            if (!currentTenantId && window.platformInfo && window.platformInfo.tenantId) {
                currentTenantId = window.platformInfo.tenantId;
            }
            
            console.log('当前租户ID:', currentTenantId, '来源: URL路径解析');
        } catch (error) {
            console.warn('获取租户信息失败:', error);
        }
    }

    /**
     * 更新问卷统计数字
     */
    function updateSurveyCount(count) {
        try {
            const countElement = document.getElementById('survey-count');
            if (countElement) {
                // 添加动画效果
                countElement.style.transform = 'scale(0.8)';
                countElement.style.opacity = '0.5';
                
                setTimeout(() => {
                    countElement.textContent = count;
                    countElement.style.transform = 'scale(1)';
                    countElement.style.opacity = '1';
                }, 150);
            }
        } catch (error) {
            console.warn('更新问卷统计失败:', error);
        }
    }

    /**
     * 检测是否为移动设备
     */
    function isMobileDevice() {
        return window.innerWidth <= 768 || /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent);
    }

    /**
     * 初始化按钮交互效果
     */
    function initButtonInteractions() {
        // 监听按钮点击事件
        document.addEventListener('click', function(event) {
            const button = event.target.closest('.survey-participate-btn, .survey-card-item-btn');
            if (button) {
                // 添加点击动画效果
                button.classList.add('btn-clicked');
                
                // 创建波纹效果
                const ripple = document.createElement('span');
                const rect = button.getBoundingClientRect();
                const size = Math.max(rect.width, rect.height);
                const x = event.clientX - rect.left - size / 2;
                const y = event.clientY - rect.top - size / 2;
                
                ripple.style.cssText = `
                    position: absolute;
                    width: ${size}px;
                    height: ${size}px;
                    left: ${x}px;
                    top: ${y}px;
                    background: rgba(255,255,255,0.3);
                    border-radius: 50%;
                    transform: scale(0);
                    animation: rippleAnimation 0.6s ease-out;
                    pointer-events: none;
                    z-index: 1;
                `;
                
                button.style.position = 'relative';
                button.style.overflow = 'hidden';
                button.appendChild(ripple);
                
                // 清理动画
                setTimeout(() => {
                    button.classList.remove('btn-clicked');
                    if (ripple.parentNode) {
                        ripple.remove();
                    }
                }, 600);
            }
        });
        
        // 添加ripple动画的CSS
        if (!document.getElementById('survey-ripple-styles')) {
            const style = document.createElement('style');
            style.id = 'survey-ripple-styles';
            style.textContent = `
                @keyframes rippleAnimation {
                    to {
                        transform: scale(2);
                        opacity: 0;
                    }
                }
            `;
            document.head.appendChild(style);
        }
    }

    /**
     * 创建移动端卡片布局
     */
    function createMobileCardLayout(surveys) {
        if (!Array.isArray(surveys) || surveys.length === 0) {
            return `
                <div class="survey-empty">
                    <i class="fas fa-poll"></i>
                    <h4>暂无问卷</h4>
                    <p>当前没有可参与的问卷调查</p>
                </div>
            `;
        }

        return surveys.map(survey => {
            const canParticipate = survey.canParticipate;
            const isExpired = survey.isExpired;
            
            let actionButton = '';
            if (canParticipate && !isExpired) {
                actionButton = `
                    <a href="${survey.participateUrl}" class="survey-card-item-btn primary">
                        <i class="fas fa-play"></i>
                        参与问卷
                    </a>
                `;
            } else if (isExpired) {
                actionButton = `
                    <button class="survey-card-item-btn disabled" disabled>
                        <i class="fas fa-clock"></i>
                        已过期
                    </button>
                `;
            } else {
                actionButton = `
                    <button class="survey-card-item-btn disabled" disabled>
                        <i class="fas fa-ban"></i>
                        不可用
                    </button>
                `;
            }

            return `
                <div class="survey-card-item">
                    <div class="survey-card-item-header">
                        <h3 class="survey-card-item-title">${survey.title || '未命名问卷'}</h3>
                        ${survey.categoryName ? `<span class="survey-card-item-category">${survey.categoryName}</span>` : ''}
                    </div>
                    ${survey.description ? `<div class="survey-card-item-description">${survey.description}</div>` : ''}
                    <div class="survey-card-item-meta">
                        <div class="survey-card-item-questions">
                            <i class="fas fa-list"></i>
                            <span>${survey.questionCount || 0}题</span>
                        </div>
                        <div class="survey-card-item-time">
                            <i class="fas fa-clock"></i>
                            <span>${survey.estimatedMinutes || 5}分钟</span>
                        </div>
                    </div>
                    <div class="survey-card-item-actions">
                        ${actionButton}
                    </div>
                </div>
            `;
        }).join('');
    }

    /**
     * 切换显示模式（表格/卡片）
     */
    function toggleDisplayMode() {
        const container = document.getElementById('survey-list-container');
        if (!container) return;

        if (isMobileDevice()) {
            container.classList.add('survey-card-mode');
        } else {
            container.classList.remove('survey-card-mode');
        }
    }

    /**
     * 初始化问卷列表
     */
    function initSurveyList() {
        try {
            console.log('开始初始化问卷列表...');
            
            // 获取租户信息
            getTenantInfo();
        const amisSchema = {
            type: 'page',
            title: '',
            body: [
                // 搜索过滤器
                {
                    type: 'form',
                    name: 'searchForm',
                    mode: 'horizontal',
                    wrapWithPanel: false,
                    className: 'survey-search-form',
                    target: 'surveyService',
                    body: [
                        {
                            type: 'container',
                            className: 'survey-search-container',
                            body: [
                                {
                                    type: 'input-text',
                                    name: 'title',
                                    label: false,
                                    placeholder: '搜索问卷标题...',
                                    clearable: true,
                                    className: 'survey-search-input',
                                    addOn: {
                                        type: 'button',
                                        icon: 'fa fa-search',
                                        level: 'primary',
                                        actionType: 'submit',
                                        className: 'survey-search-btn'
                                    }
                                },
                                {
                                    type: 'select',
                                    name: 'categoryName',
                                    label: false,
                                    placeholder: '选择分类',
                                    source: {
                                        method: 'get',
                                        url: '/survey/api/app/surveys/categories'
                                    },
                                    clearable: true,
                                    className: 'survey-category-select',
                                    submitOnChange: true
                                }
                            ]
                        }
                    ],
                    actions: []
                },
                
                // 数据服务组件
                {
                    type: 'service',
                    id: 'surveyService',
                    name: 'surveyService',
                    api: {
                        method: 'get',
                        url: '/survey/api/app/surveys/public',
                        data: {
                            title: '${title}',
                            categoryName: '${categoryName}'
                        },
                        adaptor: function(payload) {
                            if (payload.status === 0 && payload.data) {
                                updateSurveyCount(payload.data.length);
                                
                                const processedData = payload.data.map(item => {
                                    let tenantId = currentTenantId;
                                    if (!tenantId) {
                                        tenantId = getTenantIdFromPath();
                                        if (!tenantId && window.surveyPageData && window.surveyPageData.tenantId) {
                                            tenantId = window.surveyPageData.tenantId;
                                        }
                                        if (!tenantId && window.currentTenantId) {
                                            tenantId = window.currentTenantId;
                                        }
                                    }
                                    
                                    let participateUrl;
                                    const identifier = item.publicAccessCode || item.id;
                                    if (tenantId) {
                                        participateUrl = `/${tenantId}/survey/participate/${identifier}`;
                                    } else {
                                        participateUrl = `/survey/participate/${identifier}`;
                                    }
                                    
                                    return {
                                        ...item,
                                        participateUrl: participateUrl
                                    };
                                });
                                
                                return {
                                    status: 0,
                                    msg: payload.msg || '获取成功',
                                    data: {
                                        surveys: processedData,
                                        total: processedData.length
                                    }
                                };
                            }
                            updateSurveyCount(0);
                            return payload;
                        }
                    },
                    body: [
                        // 移动端卡片布局
                        {
                            type: 'each',
                            name: 'surveys',
                            placeholder: '暂无问卷数据',
                            className: 'survey-cards-container',
                            items: {
                                type: 'container',
                                className: 'survey-card-wrapper',
                                body: [
                                    {
                                        type: 'container',
                                        className: 'survey-card-modern',
                                        body: [
                                            // 卡片头部
                                            {
                                                type: 'container',
                                                className: 'survey-card-header',
                                                body: [
                                                    {
                                                        type: 'container',
                                                        className: 'survey-card-title-section',
                                                        body: [
                                                            {
                                                                type: 'tpl',
                                                                className: 'survey-card-title',
                                                                tpl: '<h3>${title}</h3>'
                                                            },
                                                            {
                                                                type: 'tpl',
                                                                className: 'survey-card-category',
                                                                tpl: '${categoryName}',
                                                                visibleOn: '${categoryName}'
                                                            }
                                                        ]
                                                    }
                                                ]
                                            },
                                            
                                            // 卡片描述
                                            {
                                                type: 'tpl',
                                                className: 'survey-card-description',
                                                tpl: '${description | truncate:120}',
                                                visibleOn: '${description}'
                                            },
                                            
                                            // 卡片元信息
                                            {
                                                type: 'container',
                                                className: 'survey-card-meta',
                                                body: [
                                                    {
                                                        type: 'tpl',
                                                        className: 'survey-meta-item',
                                                        tpl: '<i class="fas fa-list-ul"></i><span>${questionCount || 0}题</span>'
                                                    },
                                                    {
                                                        type: 'tpl',
                                                        className: 'survey-meta-item',
                                                        tpl: '<i class="fas fa-clock"></i><span>${estimatedMinutes || 5}分钟</span>'
                                                    },
                                                    {
                                                        type: 'tpl',
                                                        className: 'survey-meta-item',
                                                        tpl: '<i class="fas fa-calendar"></i><span>${publishedAt | date:"MM-DD"}</span>',
                                                        visibleOn: '${publishedAt}'
                                                    }
                                                ]
                                            },
                                            
                                            // 卡片操作按钮
                                            {
                                                type: 'container',
                                                className: 'survey-card-actions',
                                                body: [
                                                    {
                                                        type: 'tpl',
                                                        className: 'survey-btn-wrapper',
                                                        tpl: '<a href="${participateUrl}" class="survey-btn survey-btn-primary"><i class="fas fa-play"></i><span>参与问卷</span></a>',
                                                        visibleOn: '${canParticipate && !isExpired}'
                                                    },
                                                    {
                                                        type: 'tpl',
                                                        className: 'survey-btn-wrapper',
                                                        tpl: '<button class="survey-btn survey-btn-disabled" disabled><i class="fas fa-clock"></i><span>已过期</span></button>',
                                                        visibleOn: '${isExpired}'
                                                    },
                                                    {
                                                        type: 'tpl',
                                                        className: 'survey-btn-wrapper',
                                                        tpl: '<button class="survey-btn survey-btn-disabled" disabled><i class="fas fa-ban"></i><span>不可用</span></button>',
                                                        visibleOn: '${!canParticipate && !isExpired}'
                                                    }
                                                ]
                                            }
                                        ]
                                    }
                                ]
                            }
                        },
                        
                        // 空状态
                        {
                            type: 'tpl',
                            visibleOn: '${!surveys || surveys.length === 0}',
                            className: 'survey-empty-state',
                            tpl: `
                                <div class="survey-empty">
                                    <i class="fas fa-poll"></i>
                                    <h4>暂无问卷</h4>
                                    <p>当前没有可参与的问卷调查</p>
                                </div>
                            `
                        }
                    ]
                }
            ]
        };

        // 渲染AMIS页面
        const amis = amisRequire('amis/embed');
        
        // AMIS 配置选项
        const amisOptions = {
            locale: 'zh-CN',
            theme: 'antd'
        };

        // AMIS 事件处理器
        const amisHandlers = {
            requestAdaptor: function(api) {
                // 添加认证头和其他必要信息
                let token = null;
                try {
                    token = window.TokenManager?.getToken?.() || null;
                } catch (e) {
                    console.warn('获取Token失败:', e);
                }
                
                // 动态获取当前租户ID
                let tenantId = currentTenantId;
                if (!tenantId) {
                    // 重新尝试获取租户信息
                    tenantId = getTenantIdFromPath();
                    if (!tenantId && window.surveyPageData && window.surveyPageData.tenantId) {
                        tenantId = window.surveyPageData.tenantId;
                    }
                    if (!tenantId && window.TokenManager && typeof window.TokenManager.getCurrentTenantId === 'function') {
                        tenantId = window.TokenManager.getCurrentTenantId();
                    }
                    if (!tenantId && window.currentTenantId) {
                        tenantId = window.currentTenantId;
                    }
                }
                
                // 构建请求头
                const headers = {
                    ...api.headers,
                    'Authorization': token ? 'Bearer ' + token : '',
                    'X-Forwarded-With': 'CodeSpirit',
                    'Content-Type': 'application/json'
                };
                
                // 添加租户ID头部（如果存在）
                if (tenantId) {
                    headers['X-Tenant-Id'] = tenantId;
                }
                
                // 确保所有请求都包含租户参数
                const data = { ...api.data };
                if (tenantId && !data.tenantId) {
                    data.tenantId = tenantId;
                }
                
                return {
                    ...api,
                    headers: headers,
                    data: data
                };
            },

            responseAdaptor: function(api, payload, query, request, response) {
                // 处理错误响应
                if (response.status === 401) {
                    window.location.href = '/login';
                    return { msg: '登录过期！' };
                } else if (response.status === 403) {
                    return { msg: '您没有权限访问此页面！' };
                }
                
                return payload;
            }
        };

        // 初始化 AMIS 实例
        amisInstance = amis.embed('#survey-list-container', amisSchema, amisOptions, amisHandlers);

        // 绑定事件监听器
        bindEventListeners();
        
        // 初始化显示模式
        toggleDisplayMode();
        
        console.log('问卷列表初始化完成');
        
        } catch (error) {
            console.error('初始化问卷列表失败:', error);
            
            // 显示错误信息
            const container = document.getElementById('survey-list-container');
            if (container) {
                container.innerHTML = `
                    <div class="survey-error-container">
                        <div class="survey-error-icon">
                            <i class="fas fa-exclamation-triangle"></i>
                        </div>
                        <h4 class="survey-error-title">加载失败</h4>
                        <p class="survey-error-message">问卷列表加载失败，请刷新页面重试</p>
                        <button class="survey-retry-btn" onclick="window.SurveyList.reload()">
                            <i class="fas fa-redo"></i> 重新加载
                        </button>
                    </div>
                `;
            }
        }
    }


    /**
     * 绑定事件监听器
     */
    function bindEventListeners() {
        // 监听页面可见性变化，自动刷新数据
        document.addEventListener('visibilitychange', function() {
            if (!document.hidden && amisInstance) {
                // 页面重新可见时刷新数据
                setTimeout(() => {
                    // 使用 AMIS 的正确方式刷新组件
                    try {
                        const surveyListComponent = amisInstance.getComponentByName?.('surveyList');
                        if (surveyListComponent && surveyListComponent.reload) {
                            surveyListComponent.reload();
                        }
                    } catch (error) {
                        console.warn('刷新组件失败:', error);
                    }
                }, 500);
            }
        });

        // 监听窗口大小变化，切换显示模式
        let resizeTimeout;
        window.addEventListener('resize', function() {
            clearTimeout(resizeTimeout);
            resizeTimeout = setTimeout(() => {
                toggleDisplayMode();
                
                // 如果从桌面端切换到移动端，重新加载数据以应用卡片布局
                if (amisInstance) {
                    try {
                        const surveyServiceComponent = amisInstance.getComponentById?.('surveyService');
                        if (surveyServiceComponent && surveyServiceComponent.reload) {
                            surveyServiceComponent.reload();
                        }
                    } catch (error) {
                        console.warn('切换显示模式时刷新失败:', error);
                    }
                }
            }, 300);
        });

        // 监听设备方向变化（移动设备）
        window.addEventListener('orientationchange', function() {
            setTimeout(() => {
                toggleDisplayMode();
                if (amisInstance) {
                    try {
                        const surveyServiceComponent = amisInstance.getComponentById?.('surveyService');
                        if (surveyServiceComponent && surveyServiceComponent.reload) {
                            surveyServiceComponent.reload();
                        }
                    } catch (error) {
                        console.warn('方向变化时刷新失败:', error);
                    }
                }
            }, 500);
        });
    }

    /**
     * 格式化时间显示
     */
    function formatTime(dateString) {
        if (!dateString) return '无限期';
        
        const date = new Date(dateString);
        const now = new Date();
        const diff = date.getTime() - now.getTime();
        
        if (diff < 0) {
            return '<span class="text-danger">已过期</span>';
        }
        
        const days = Math.ceil(diff / (1000 * 60 * 60 * 24));
        if (days <= 7) {
            return `<span class="text-warning">${days}天后过期</span>`;
        }
        
        return date.toLocaleDateString('zh-CN');
    }

    /**
     * 处理参与问卷点击
     */
    function handleParticipateSurvey(surveyId) {
        // 可以在这里添加额外的逻辑，比如统计、确认等
        console.log('参与问卷:', surveyId);
        
        // 跳转到问卷参与页面
        window.location.href = `/survey/participate/${surveyId}`;
    }

    /**
     * 显示问卷详情预览
     */
    function showSurveyPreview(survey) {
        // 可以实现问卷预览功能
        console.log('预览问卷:', survey);
    }

    /**
     * 页面初始化
     */
    // 页面加载动画
    function addPageLoadAnimation() {
        // 添加页面淡入效果
        document.body.style.opacity = '0';
        document.body.style.transition = 'opacity 0.8s ease-in-out';
        
        setTimeout(() => {
            document.body.style.opacity = '1';
        }, 100);
        
        // 为卡片添加交错动画
        const observer = new IntersectionObserver((entries) => {
            entries.forEach((entry, index) => {
                if (entry.isIntersecting) {
                    setTimeout(() => {
                        entry.target.style.opacity = '1';
                        entry.target.style.transform = 'translateY(0)';
                    }, index * 120);
                    observer.unobserve(entry.target);
                }
            });
        }, { threshold: 0.1 });
        
        // 观察所有卡片
        setTimeout(() => {
            const cards = document.querySelectorAll('.survey-card-modern');
            cards.forEach(card => {
                card.style.opacity = '0';
                card.style.transform = 'translateY(40px)';
                card.style.transition = 'opacity 0.8s ease, transform 0.8s ease';
                observer.observe(card);
            });
        }, 600);
    }
    
    // 添加搜索动画效果
    function enhanceSearchInteraction() {
        setTimeout(() => {
            const searchForm = document.querySelector('.survey-search-form');
            const searchInput = document.querySelector('.survey-search-input');
            const categorySelect = document.querySelector('.survey-category-select');
            const searchBtn = document.querySelector('.survey-search-btn');
            
            if (searchForm) {
                searchForm.style.opacity = '0';
                searchForm.style.transform = 'translateY(-20px)';
                searchForm.style.transition = 'opacity 0.6s ease, transform 0.6s ease';
                
                setTimeout(() => {
                    searchForm.style.opacity = '1';
                    searchForm.style.transform = 'translateY(0)';
                }, 200);
            }
            
            if (searchInput) {
                searchInput.addEventListener('focus', function() {
                    this.style.transform = 'scale(1.02)';
                });
                
                searchInput.addEventListener('blur', function() {
                    this.style.transform = 'scale(1)';
                });
            }
            
            if (categorySelect) {
                categorySelect.addEventListener('focus', function() {
                    this.style.transform = 'scale(1.02)';
                });
                
                categorySelect.addEventListener('blur', function() {
                    this.style.transform = 'scale(1)';
                });
            }
            
            if (searchBtn) {
                searchBtn.addEventListener('mousedown', function() {
                    this.style.transform = 'scale(0.95)';
                });
                
                searchBtn.addEventListener('mouseup', function() {
                    this.style.transform = 'scale(1)';
                });
            }
        }, 300);
    }
    
    // 添加统计卡片动画
    function animateStatsCards() {
        setTimeout(() => {
            const statItems = document.querySelectorAll('.survey-stat-item');
            statItems.forEach((item, index) => {
                item.style.opacity = '0';
                item.style.transform = 'translateY(30px) scale(0.9)';
                item.style.transition = 'opacity 0.6s ease, transform 0.6s ease';
                
                setTimeout(() => {
                    item.style.opacity = '1';
                    item.style.transform = 'translateY(0) scale(1)';
                }, index * 150 + 400);
            });
        }, 100);
    }

    function init() {
        // 等待DOM加载完成
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', function() {
                initSurveyList();
                initButtonInteractions();
                addPageLoadAnimation();
                enhanceSearchInteraction();
                animateStatsCards();
            });
        } else {
            initSurveyList();
            initButtonInteractions();
            addPageLoadAnimation();
            enhanceSearchInteraction();
            animateStatsCards();
        }
    }

    // 暴露全局方法
    window.SurveyList = {
        init: init,
        reload: function() {
            if (amisInstance) {
                try {
                    const surveyServiceComponent = amisInstance.getComponentById?.('surveyService');
                    if (surveyServiceComponent && surveyServiceComponent.reload) {
                        surveyServiceComponent.reload();
                    } else {
                        // 备用方案：重新初始化
                        initSurveyList();
                    }
                } catch (error) {
                    console.warn('刷新失败，尝试重新初始化:', error);
                    initSurveyList();
                }
            }
        },
        handleParticipateSurvey: handleParticipateSurvey,
        showSurveyPreview: showSurveyPreview
    };

    // 自动初始化
    init();

})();
