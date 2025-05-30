(function () {
    let amis = amisRequire('amis/embed');
    
    // 清除之前的token
    TokenManager.clearToken();
    
    // 添加全屏样式类
    document.body.classList.add('tenant-login-body');
    
    // 获取租户ID
    const tenantId = window.tenantId;
    if (!tenantId) {
        console.error('租户ID不存在');
        showError('租户ID不存在，请检查URL');
        return;
    }
    
    // 初始化页面
    initializeTenantLogin();
    
    // 页面卸载时清理样式
    window.addEventListener('beforeunload', function() {
        document.body.classList.remove('tenant-login-body');
    });
    
    /**
     * 初始化租户登录页面
     * 1. 获取租户信息
     * 2. 应用租户主题
     * 3. 渲染登录表单
     */
    async function initializeTenantLogin() {
        try {
            showLoading(true);
            
            // 获取租户信息
            const tenant = await fetchTenantInfo(tenantId);
            
            if (!tenant) {
                showError('租户不存在或已停用');
                return;
            }
            
            // 应用租户主题
            applyTenantTheme(tenant.themeConfig);
            
            // 渲染登录页面
            renderLoginPage(tenant);
            
            showLoading(false);
            
        } catch (error) {
            console.error('初始化租户登录页面失败:', error);
            showError('加载失败，请刷新页面重试');
        }
    }
    
    /**
     * 获取租户信息
     * @param {string} tenantId 租户ID
     * @returns {Promise<Object>} 租户信息
     */
    async function fetchTenantInfo(tenantId) {
        const response = await fetch(`/identity/api/identity/tenants/${tenantId}/login-config`, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
                'X-Forwarded-With': 'CodeSpirit'
            }
        });
        
        if (!response.ok) {
            throw new Error(`HTTP ${response.status}: ${response.statusText}`);
        }
        
        const result = await response.json();
        
        if (result.status !== 0) {
            throw new Error(result.msg || '获取租户信息失败');
        }
        
        return result.data;
    }
    
    /**
     * 渲染登录页面
     * @param {Object} tenant 租户信息
     */
    function renderLoginPage(tenant) {
        const amisJSON = {
            "type": "page",
            "title": "",
            "className": "tenant-login-page",
            "body": {
                "type": "flex",
                "justify": "center",
                "alignItems": "center",
                "className": "tenant-login-container",
                "items": [
                    {
                        "type": "container",
                        "className": "tenant-login-content",
                        "style": {
                            "maxWidth": "500px",
                            "width": "100%",
                            "margin": "0 auto"
                        },
                        "body": [
                            // 租户品牌区域
                            {
                                "type": "container",
                                "className": "tenant-branding",
                                "body": [
                                    {
                                        "type": "tpl",
                                        "tpl": buildTenantBrandingTpl(tenant),
                                        "className": "tenant-brand-info"
                                    }
                                ]
                            },
                            // 登录表单区域
                            {
                                "type": "panel",
                                "className": "tenant-login-panel",
                                "body": [
                                    {
                                        "type": "form",
                                        "title": "",
                                        "className": "tenant-login-form",
                                        "wrapWithPanel": false,
                                        "api": {
                                            "method": "post",
                                            "url": "/identity/api/identity/auth/login",
                                            "requestAdaptor": function(api) {
                                                // 添加租户信息到请求头
                                                api.headers = api.headers || {};
                                                api.headers['TenantId'] = tenant.tenantId;
                                                api.headers['X-Tenant-Path'] = window.location.pathname;
                                                api.headers['X-Forwarded-With'] = 'CodeSpirit';
                                                return api;
                                            },
                                            "adaptor": function(payload, response, api) {
                                                if (payload.status === 0) {
                                                    // 保存token
                                                    if (payload.data && payload.data.token) {
                                                        TokenManager.setToken(payload.data.token);
                                                    }
                                                    
                                                    // 登录成功，重定向到租户主页
                                                    const redirectUrl = `/${tenant.tenantId}/admin` || '/';
                                                    setTimeout(() => {
                                                        window.location.href = redirectUrl;
                                                    }, 1000);
                                                }
                                                return payload;
                                            }
                                        },
                                        "body": [
                                            {
                                                "type": "hidden",
                                                "name": "tenantId",
                                                "value": tenant.tenantId
                                            },
                                            {
                                                "type": "input-text",
                                                "name": "userName",
                                                "label": "用户名",
                                                "placeholder": "请输入用户名/手机号/邮箱",
                                                "required": true,
                                                "className": "tenant-input-field",
                                                "prefixIcon": "fa fa-user",
                                                "clearable": true
                                            },
                                            {
                                                "type": "input-password",
                                                "name": "password",
                                                "label": "密码",
                                                "placeholder": "请输入密码",
                                                "required": true,
                                                "className": "tenant-input-field",
                                                "prefixIcon": "fa fa-lock",
                                                "clearable": true
                                            },
                                            {
                                                "type": "flex",
                                                "justify": "space-between",
                                                "alignItems": "center",
                                                "className": "login-options",
                                                "items": [
                                                    {
                                                        "type": "checkbox",
                                                        "name": "rememberMe",
                                                        "option": "记住我",
                                                        "className": "remember-checkbox"
                                                    },
                                                    {
                                                        "type": "button",
                                                        "label": "忘记密码?",
                                                        "level": "link",
                                                        "size": "sm",
                                                        "className": "forgot-password-link",
                                                        "actionType": "url",
                                                        "url": `/${tenant.tenantId}/forgot-password`
                                                    }
                                                ]
                                            },
                                            {
                                                "type": "button",
                                                "label": "登录",
                                                "level": "primary",
                                                "block": true,
                                                "actionType": "submit",
                                                "className": "tenant-login-btn",
                                                "size": "lg"
                                            }
                                        ]
                                    }
                                ]
                            },
                            // 其他登录选项
                            buildAlternativeLoginOptions(tenant),
                            // 页脚信息
                            {
                                "type": "tpl",
                                "tpl": buildFooterTpl(tenant),
                                "className": "tenant-login-footer"
                            }
                        ]
                    }
                ]
            }
        };
        
        // 初始化AMIS
        const amisScoped = amis.embed('#root', amisJSON, {
            location: history.location,
            data: {
                tenant: tenant
            },
            context: {
                WEB_HOST: window.webHost,
                TENANT_ID: tenant.tenantId
            },
            // 全局请求适配器
            requestAdaptor: (api) => {
                const token = TokenManager.getToken();
                return {
                    ...api,
                    headers: {
                        ...api.headers,
                        'Authorization': token ? 'Bearer ' + token : '',
                        'X-Forwarded-With': 'CodeSpirit'
                    }
                };
            }
        }, { 
            theme: 'antd',
            locale: 'zh-CN'
        });
    }
    
    /**
     * 构建租户品牌信息模板
     * @param {Object} tenant 租户信息
     * @returns {string} HTML模板
     */
    function buildTenantBrandingTpl(tenant) {
        const logoUrl = tenant.logoUrl || '/logo.png';
        const displayName = tenant.displayName || tenant.name || '租户登录';
        const description = tenant.description || '';
        
        return `
            <div class="tenant-brand-header">
                <div class="tenant-logo-container">
                    <img src="${logoUrl}" alt="${displayName}" class="tenant-logo" 
                         onerror="this.src='/logo.png'" />
                </div>
                <h1 class="tenant-title">${displayName}</h1>
                ${description ? `<p class="tenant-description">${description}</p>` : ''}
                <div class="login-welcome">
                    <h3>欢迎登录</h3>
                    <p>请使用您的账户凭据登录系统</p>
                </div>
            </div>
        `;
    }
    
    /**
     * 构建其他登录选项
     * @param {Object} tenant 租户信息
     * @returns {Object} AMIS组件配置
     */
    function buildAlternativeLoginOptions(tenant) {
        const config = tenant.configuration || {};
        const options = [];
        
        // 根据租户配置显示不同的登录选项
        if (config.enableSsoLogin) {
            options.push({
                "type": "button",
                "label": "单点登录(SSO)",
                "level": "default",
                "block": true,
                "className": "sso-login-btn mt-3",
                "icon": "fa fa-sign-in-alt",
                "actionType": "url",
                "url": `/${tenant.tenantId}/sso/login`
            });
        }
        
        if (config.enableSmsLogin) {
            options.push({
                "type": "button",
                "label": "短信验证码登录",
                "level": "default",
                "block": true,
                "className": "sms-login-btn mt-2",
                "icon": "fa fa-mobile-alt",
                "actionType": "url",
                "url": `/${tenant.tenantId}/sms-login`
            });
        }
        
        if (options.length === 0) {
            return { "type": "tpl", "tpl": "" };
        }
        
        return {
            "type": "container",
            "className": "alternative-login-options",
            "body": [
                {
                    "type": "divider",
                    "title": "其他登录方式"
                },
                ...options
            ]
        };
    }
    
    /**
     * 构建页脚模板
     * @param {Object} tenant 租户信息
     * @returns {string} HTML模板
     */
    function buildFooterTpl(tenant) {
        const currentYear = new Date().getFullYear();
        const companyName = tenant.displayName || tenant.name;
        
        return `
            <div class="tenant-footer">
                <div class="footer-links">
                    <a href="/${tenant.tenantId}/help">帮助中心</a>
                    <span class="separator">|</span>
                    <a href="/${tenant.tenantId}/contact">联系我们</a>
                    <span class="separator">|</span>
                    <a href="/login">主站登录</a>
                </div>
                <div class="footer-copyright">
                    <p>&copy; ${currentYear} ${companyName}. All rights reserved.</p>
                </div>
            </div>
        `;
    }
    
    /**
     * 应用租户主题
     * @param {string|Object} themeConfig 主题配置
     */
    function applyTenantTheme(themeConfig) {
        if (!themeConfig) return;
        
        try {
            const theme = typeof themeConfig === 'string' ? JSON.parse(themeConfig) : themeConfig;
            const root = document.documentElement;
            
            // 应用主题颜色
            if (theme.primaryColor) {
                root.style.setProperty('--tenant-primary-color', theme.primaryColor);
            }
            
            if (theme.backgroundColor) {
                root.style.setProperty('--tenant-bg-color', theme.backgroundColor);
            }
            
            if (theme.textColor) {
                root.style.setProperty('--tenant-text-color', theme.textColor);
            }
            
            if (theme.borderColor) {
                root.style.setProperty('--tenant-border-color', theme.borderColor);
            }
            
            if (theme.logoUrl) {
                root.style.setProperty('--tenant-logo-url', `url('${theme.logoUrl}')`);
            }
            
            // 应用自定义CSS
            if (theme.customCss) {
                const style = document.createElement('style');
                style.id = 'tenant-custom-style';
                style.textContent = theme.customCss;
                document.head.appendChild(style);
            }
            
            // 更新页面标题
            if (theme.title) {
                document.title = theme.title;
            }
            
        } catch (error) {
            console.warn('应用租户主题失败:', error);
        }
    }
    
    /**
     * 显示/隐藏加载状态
     * @param {boolean} show 是否显示
     */
    function showLoading(show) {
        const loadingEl = document.getElementById('loading');
        if (loadingEl) {
            loadingEl.style.display = show ? 'flex' : 'none';
        }
    }
    
    /**
     * 显示错误信息
     * @param {string} message 错误消息
     */
    function showError(message) {
        showLoading(false);
        
        const errorHTML = `
            <div class="tenant-error">
                <div class="error-content">
                    <div class="error-icon">
                        <i class="fa fa-exclamation-triangle"></i>
                    </div>
                    <h3>访问受限</h3>
                    <p>${message}</p>
                    <div class="error-actions">
                        <button onclick="location.reload()" class="btn btn-primary">
                            <i class="fa fa-refresh"></i> 重试
                        </button>
                        <a href="/login" class="btn btn-secondary">
                            <i class="fa fa-home"></i> 返回主登录页
                        </a>
                    </div>
                </div>
            </div>
        `;
        
        document.getElementById('root').innerHTML = errorHTML;
    }
})(); 