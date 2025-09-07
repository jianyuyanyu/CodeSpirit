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
     * 初始化按钮交互效果
     */
    function initButtonInteractions() {
        // 监听按钮点击事件
        document.addEventListener('click', function(event) {
            const button = event.target.closest('.survey-participate-btn');
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
     * 初始化问卷列表
     */
    function initSurveyList() {
        try {
            console.log('开始初始化问卷列表...');
            
            // 获取租户信息
            getTenantInfo();
        const amisSchema = {
            type: 'page',
            title: '问卷调查',
            subTitle: '参与公开问卷调查，分享您的宝贵意见',
            body: [
                {
                    type: 'crud',
                    name: 'surveyList',
                    api: {
                        method: 'get',
                        url: '/survey/api/app/surveys/public',
                        data: {
                            '&': '$$' // 传递所有查询参数
                            // 租户参数将通过 requestAdaptor 动态添加
                        },
                        adaptor: function(payload) {
                            // 处理API响应数据
                            if (payload.status === 0 && payload.data) {
                                // 更新统计数字
                                updateSurveyCount(payload.data.length);
                                
                                // 为每个问卷项添加参与链接
                                const processedData = payload.data.map(item => {
                                    // 动态获取租户ID
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
                                    
                                    // 构建参与问卷的URL（优先使用访问码）
                                    let participateUrl;
                                    const identifier = item.publicAccessCode || item.id; // 优先使用访问码，回退到ID
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
                                        items: processedData,
                                        total: processedData.length
                                    }
                                };
                            }
                            // 更新统计数字
                            updateSurveyCount(0);
                            return payload;
                        }
                    },
                    filter: {
                        title: '搜索问卷',
                        submitText: '搜索',
                        body: [
                            {
                                type: 'input-text',
                                name: 'title',
                                label: '问卷标题',
                                placeholder: '请输入问卷标题关键词',
                                clearable: true
                            },
                            {
                                type: 'select',
                                name: 'categoryName',
                                label: '问卷分类',
                                placeholder: '请选择问卷分类',
                                source: {
                                    method: 'get',
                                    url: '/survey/api/app/surveys/categories'
                                    // 租户参数将通过 requestAdaptor 动态添加
                                },
                                clearable: true
                            }
                        ]
                    },
                    columns: [
                        {
                            name: 'title',
                            label: '问卷标题',
                            type: 'text',
                            searchable: true,
                            width: 300,
                            className: 'survey-title-column'
                        },
                        {
                            name: 'description',
                            label: '问卷描述',
                            type: 'text',
                            width: 250,
                            breakpoint: '*',
                            className: 'survey-desc-column',
                            tpl: '${description | truncate:80}'
                        },
                        {
                            name: 'categoryName',
                            label: '分类',
                            type: 'text',
                            width: 120,
                            className: 'survey-category-column'
                        },
                        {
                            name: 'questionCount',
                            label: '题目数量',
                            type: 'tpl',
                            width: 100,
                            className: 'survey-count-column',
                            tpl: '<span class="badge badge-info">${questionCount}题</span>'
                        },
                        {
                            name: 'estimatedMinutes',
                            label: '预计时间',
                            type: 'tpl',
                            width: 100,
                            className: 'survey-time-column',
                            tpl: '<span class="text-muted">${estimatedMinutes}分钟</span>'
                        },
                        {
                            name: 'publishedAt',
                            label: '发布时间',
                            type: 'datetime',
                            width: 150,
                            format: 'YYYY-MM-DD HH:mm',
                            className: 'survey-date-column'
                        },
                        {
                            name: 'expiresAt',
                            label: '过期时间',
                            type: 'datetime',
                            width: 150,
                            format: 'YYYY-MM-DD HH:mm',
                            className: 'survey-expire-column',
                            tpl: '${expiresAt ? (expiresAt | date:"YYYY-MM-DD HH:mm") : "无限期"}'
                        },
                        {
                            type: 'operation',
                            label: '操作',
                            width: 150,
                            className: 'survey-action-column',
                            buttons: [
                                {
                                    type: 'button',
                                    label: '参与问卷',
                                    level: 'primary',
                                    size: 'sm',
                                    actionType: 'link',
                                    link: '${participateUrl | raw}',
                                    blank: false,
                                    visibleOn: '${canParticipate}',
                                    className: 'survey-action-btn primary survey-participate-btn',
                                    tooltip: '点击参与问卷调查'
                                },
                                {
                                    type: 'button',
                                    label: '已过期',
                                    level: 'default',
                                    size: 'sm',
                                    disabled: true,
                                    visibleOn: '${isExpired}',
                                    className: 'survey-action-btn disabled',
                                    tooltip: '问卷已过期，无法参与'
                                },
                                {
                                    type: 'button',
                                    label: '不可用',
                                    level: 'default',
                                    size: 'sm',
                                    disabled: true,
                                    visibleOn: '${!canParticipate && !isExpired}',
                                    className: 'survey-action-btn disabled',
                                    tooltip: '问卷当前不可参与'
                                }
                            ]
                        }
                    ],
                    headerToolbar: [
                        {
                            type: 'tpl',
                            tpl: '<div class="survey-list-header"><i class="fas fa-poll"></i> 问卷列表</div>',
                            className: 'survey-header-title'
                        },
                        'filter-toggler',
                        'reload',
                        {
                            type: 'columns-toggler',
                            align: 'right',
                            className: 'survey-columns-toggle'
                        }
                    ],
                    footerToolbar: [
                        'statistics', 
                        'pagination'
                    ],
                    perPage: 10,
                    perPageAvailable: [10, 20, 50],
                    loadDataOnce: false,
                    className: 'survey-list-table',
                    placeholder: {
                        type: 'tpl',
                        tpl: `
                            <div class="survey-empty">
                                <i class="fas fa-poll"></i>
                                <h4>暂无问卷</h4>
                                <p>当前没有可参与的问卷调查</p>
                            </div>
                        `,
                        className: 'survey-empty-state'
                    },
                    loadingConfig: {
                        show: true,
                        className: 'survey-loading'
                    }
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
    function init() {
        // 等待DOM加载完成
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', function() {
                initSurveyList();
                initButtonInteractions();
            });
        } else {
            initSurveyList();
            initButtonInteractions();
        }
    }

    // 暴露全局方法
    window.SurveyList = {
        init: init,
        reload: function() {
            if (amisInstance) {
                try {
                    const surveyListComponent = amisInstance.getComponentByName?.('surveyList');
                    if (surveyListComponent && surveyListComponent.reload) {
                        surveyListComponent.reload();
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
