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
                            "maxWidth": "480px",
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
                                            "url": "/identity/api/identity/auth/tenant/login",
                                            "requestAdaptor": function(api) {
                                                // 添加租户信息到请求头
                                                api.headers = api.headers || {};
                                                api.headers['TenantId'] = tenant.tenantId;
                                                api.headers['X-Tenant-Path'] = window.location.pathname;
                                                api.headers['X-Forwarded-With'] = 'CodeSpirit';
                                                api.headers['Content-Type'] = 'application/json';
                                                
                                                // 确保请求体包含租户ID
                                                if (api.data && !api.data.tenantId) {
                                                    api.data.tenantId = tenant.tenantId;
                                                }
                                                
                                                return api;
                                            },
                                            "adaptor": function(payload, response, api) {
                                                if (payload.status === 0) {
                                                    // 显示成功动画
                                                    showSuccessAnimation();
                                                    
                                                    // 保存token
                                                    if (payload.data && payload.data.token) {
                                                        TokenManager.setToken(payload.data.token);
                                                    }
                                                    
                                                    // 登录成功，重定向到租户主页
                                                    const redirectUrl = `/${tenant.tenantId}/admin`;
                                                    setTimeout(() => {
                                                        window.location.href = redirectUrl;
                                                    }, 1500);
                                                } else {
                                                    // 记录详细错误信息
                                                    console.error('租户平台登录失败:', payload.msg);
                                                    // 显示错误动画
                                                    showErrorAnimation();
                                                }
                                                return payload;
                                            }
                                        },
                                        "body": [
                                            {
                                                "type": "alert",
                                                "level": "info",
                                                "body": `正在登录租户"${tenant.displayName || tenant.name}"的管理平台`,
                                                "className": "mb-3 tenant-info-alert",
                                                "showIcon": true,
                                                "icon": "fa fa-info-circle"
                                            },
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
                                                "clearable": true,
                                                "description": "请使用本租户下的用户账号",
                                                "validationApi": "",
                                                "validations": {
                                                    "minLength": 2,
                                                    "maxLength": 50
                                                },
                                                "validateOnChange": false,
                                                "validationErrors": {
                                                    "minLength": "用户名至少需要2个字符",
                                                    "maxLength": "用户名最多50个字符",
                                                    "isRequired": "请输入用户名"
                                                }
                                            },
                                            {
                                                "type": "input-password",
                                                "name": "password",
                                                "label": "密码",
                                                "placeholder": "请输入密码",
                                                "required": true,
                                                "className": "tenant-input-field",
                                                "prefixIcon": "fa fa-lock",
                                                "clearable": true,
                                                "revealPassword": true,
                                                "validationApi": "",
                                                "validations": {
                                                    "minLength": 6,
                                                    "maxLength": 128
                                                },
                                                "validateOnChange": false,
                                                "validationErrors": {
                                                    "minLength": "密码至少需要6个字符",
                                                    "maxLength": "密码最多128个字符",
                                                    "isRequired": "请输入密码"
                                                }
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
                                                        "option": "记住登录状态",
                                                        "className": "remember-checkbox"
                                                    },
                                                    {
                                                        "type": "button",
                                                        "label": "忘记密码?",
                                                        "level": "link",
                                                        "size": "sm",
                                                        "className": "forgot-password-link",
                                                        "actionType": "url",
                                                        "url": `/${tenant.tenantId}/forgot-password`,
                                                        "icon": "fa fa-question-circle"
                                                    }
                                                ]
                                            },
                                            {
                                                "type": "button",
                                                "label": "立即登录",
                                                "level": "primary",
                                                "block": true,
                                                "actionType": "submit",
                                                "className": "tenant-login-btn",
                                                "size": "lg",
                                                "icon": "fa fa-sign-in-alt",
                                                "loadingOn": "${formSubmitting}",
                                                "disabledOn": "${formSubmitting}"
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
        
        // 添加键盘事件监听
        addKeyboardListeners();
        
        // 添加输入框焦点动画
        addInputAnimations();
        
        // 添加页面滚动动画
        addScrollAnimations();
        
        // 添加表单验证增强
        addFormValidationEnhancements();
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
                         onerror="this.src='/logo.png'" 
                         loading="lazy" />
                </div>
                <h1 class="tenant-title">${displayName}</h1>
                ${description ? `<p class="tenant-description">${description}</p>` : ''}
                <div class="login-welcome">
                    <h3>🔐 安全登录</h3>
                    <p>请使用您的账户凭据安全登录系统</p>
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
                "label": "企业单点登录(SSO)",
                "level": "default",
                "block": true,
                "className": "sso-login-btn mt-3",
                "icon": "fa fa-building",
                "actionType": "url",
                "url": `/${tenant.tenantId}/sso/login`,
                "tooltip": "通过企业统一身份认证登录"
            });
        }
        
        if (config.enableSmsLogin) {
            options.push({
                "type": "button",
                "label": "手机验证码登录",
                "level": "default",
                "block": true,
                "className": "sms-login-btn mt-2",
                "icon": "fa fa-mobile-alt",
                "actionType": "url",
                "url": `/${tenant.tenantId}/sms-login`,
                "tooltip": "通过手机短信验证码登录"
            });
        }
        
        if (config.enableQrLogin) {
            options.push({
                "type": "button",
                "label": "扫码登录",
                "level": "default",
                "block": true,
                "className": "qr-login-btn mt-2",
                "icon": "fa fa-qrcode",
                "actionType": "url",
                "url": `/${tenant.tenantId}/qr-login`,
                "tooltip": "使用移动端APP扫码登录"
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
                    "title": "或使用其他方式",
                    "titleClassName": "text-center"
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
                    <a href="/${tenant.tenantId}/help" title="查看帮助文档">
                        <i class="fa fa-question-circle"></i> 帮助中心
                    </a>
                    <span class="separator">|</span>
                    <a href="/${tenant.tenantId}/contact" title="联系技术支持">
                        <i class="fa fa-headset"></i> 联系我们
                    </a>
                    <span class="separator">|</span>
                    <a href="/login" title="返回主站登录">
                        <i class="fa fa-home"></i> 主站登录
                    </a>
                </div>
                <div class="footer-copyright">
                    <p>&copy; ${currentYear} ${companyName}. All rights reserved. | Powered by CodeSpirit</p>
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
                root.style.setProperty('--tenant-primary-gradient', 
                    `linear-gradient(135deg, ${theme.primaryColor} 0%, ${theme.secondaryColor || theme.primaryColor} 100%)`);
            }
            
            if (theme.secondaryColor) {
                root.style.setProperty('--tenant-secondary-color', theme.secondaryColor);
            }
            
            if (theme.accentColor) {
                root.style.setProperty('--tenant-accent-color', theme.accentColor);
            }
            
            if (theme.backgroundColor) {
                root.style.setProperty('--tenant-bg-gradient', 
                    `linear-gradient(135deg, ${theme.backgroundColor} 0%, ${theme.primaryColor || '#667eea'} 100%)`);
            }
            
            if (theme.borderRadius) {
                root.style.setProperty('--tenant-radius-small', `${theme.borderRadius}px`);
                root.style.setProperty('--tenant-radius-medium', `${theme.borderRadius + 4}px`);
                root.style.setProperty('--tenant-radius-large', `${theme.borderRadius + 8}px`);
            }
            
            // 应用自定义CSS
            if (theme.customCss) {
                let style = document.getElementById('tenant-custom-style');
                if (style) {
                    style.remove();
                }
                style = document.createElement('style');
                style.id = 'tenant-custom-style';
                style.textContent = theme.customCss;
                document.head.appendChild(style);
            }
            
            // 更新页面标题和图标
            if (theme.title) {
                document.title = `${theme.title} - 登录`;
            }
            
            if (theme.favicon) {
                updateFavicon(theme.favicon);
            }
            
            console.log('租户主题应用成功');
            
        } catch (error) {
            console.warn('应用租户主题失败:', error);
        }
    }
    
    /**
     * 更新网站图标
     * @param {string} faviconUrl 图标URL
     */
    function updateFavicon(faviconUrl) {
        const link = document.querySelector("link[rel*='icon']") || document.createElement('link');
        link.type = 'image/x-icon';
        link.rel = 'shortcut icon';
        link.href = faviconUrl;
        document.getElementsByTagName('head')[0].appendChild(link);
    }
    
    /**
     * 添加键盘事件监听
     */
    function addKeyboardListeners() {
        document.addEventListener('keydown', function(e) {
            // 按ESC键清空表单
            if (e.key === 'Escape') {
                const inputs = document.querySelectorAll('.tenant-input-field input');
                inputs.forEach(input => {
                    if (input.type !== 'hidden') {
                        input.value = '';
                    }
                });
            }
        });
    }
    
    /**
     * 添加输入框动画效果
     */
    function addInputAnimations() {
        // 使用事件代理，因为输入框是动态生成的
        document.addEventListener('focus', function(e) {
            if (e.target.matches('.tenant-input-field input')) {
                e.target.closest('.tenant-input-field').classList.add('focused');
            }
        }, true);
        
        document.addEventListener('blur', function(e) {
            if (e.target.matches('.tenant-input-field input')) {
                e.target.closest('.tenant-input-field').classList.remove('focused');
            }
        }, true);
        
        // 输入值变化时的动画
        document.addEventListener('input', function(e) {
            if (e.target.matches('.tenant-input-field input')) {
                const field = e.target.closest('.tenant-input-field');
                if (e.target.value) {
                    field.classList.add('has-value');
                } else {
                    field.classList.remove('has-value');
                }
            }
        });
    }
    
    /**
     * 添加滚动动画效果
     */
    function addScrollAnimations() {
        const observer = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    entry.target.classList.add('visible');
                }
            });
        }, {
            threshold: 0.1,
            rootMargin: '0px 0px -50px 0px'
        });
        
        // 观察需要动画的元素
        setTimeout(() => {
            const animatedElements = document.querySelectorAll('.tenant-branding, .tenant-login-panel, .alternative-login-options');
            animatedElements.forEach(el => observer.observe(el));
        }, 100);
    }
    
    /**
     * 显示成功登录动画
     */
    function showSuccessAnimation() {
        const successOverlay = document.createElement('div');
        successOverlay.className = 'login-success-overlay';
        successOverlay.innerHTML = `
            <div class="success-content">
                <div class="success-icon">
                    <i class="fa fa-check-circle"></i>
                </div>
                <h3>登录成功！</h3>
                <p>正在跳转到管理平台...</p>
                <div class="success-spinner">
                    <div class="spinner-dot"></div>
                    <div class="spinner-dot"></div>
                    <div class="spinner-dot"></div>
                </div>
            </div>
        `;
        
        document.body.appendChild(successOverlay);
        
        // 添加成功动画样式
        const style = document.createElement('style');
        style.textContent = `
            .login-success-overlay {
                position: fixed;
                top: 0;
                left: 0;
                width: 100%;
                height: 100%;
                background: var(--tenant-primary-gradient);
                z-index: 10000;
                display: flex;
                align-items: center;
                justify-content: center;
                animation: fadeInSuccess 0.5s ease-out;
            }
            
            .success-content {
                text-align: center;
                color: white;
                animation: slideUpSuccess 0.8s ease-out;
            }
            
            .success-icon {
                font-size: 80px;
                margin-bottom: 20px;
                animation: bounceSuccess 1s ease-out;
            }
            
            .success-content h3 {
                font-size: 28px;
                margin: 0 0 10px 0;
                font-weight: 600;
            }
            
            .success-content p {
                font-size: 16px;
                margin: 0 0 20px 0;
                opacity: 0.9;
            }
            
            .success-spinner {
                display: flex;
                justify-content: center;
                gap: 5px;
            }
            
            .spinner-dot {
                width: 8px;
                height: 8px;
                background: white;
                border-radius: 50%;
                animation: pulseSuccess 1.4s ease-in-out infinite both;
            }
            
            .spinner-dot:nth-child(1) { animation-delay: -0.32s; }
            .spinner-dot:nth-child(2) { animation-delay: -0.16s; }
            
            @keyframes fadeInSuccess {
                from { opacity: 0; }
                to { opacity: 1; }
            }
            
            @keyframes slideUpSuccess {
                from { transform: translateY(30px); opacity: 0; }
                to { transform: translateY(0); opacity: 1; }
            }
            
            @keyframes bounceSuccess {
                0%, 20%, 50%, 80%, 100% { transform: translateY(0); }
                40% { transform: translateY(-20px); }
                60% { transform: translateY(-10px); }
            }
            
            @keyframes pulseSuccess {
                0%, 80%, 100% { transform: scale(0); }
                40% { transform: scale(1); }
            }
        `;
        document.head.appendChild(style);
    }
    
    /**
     * 显示错误动画
     */
    function showErrorAnimation() {
        const loginForm = document.querySelector('.tenant-login-form');
        if (loginForm) {
            loginForm.classList.add('shake-error');
            setTimeout(() => {
                loginForm.classList.remove('shake-error');
            }, 600);
        }
        
        // 添加错误动画样式
        if (!document.getElementById('error-animation-style')) {
            const style = document.createElement('style');
            style.id = 'error-animation-style';
            style.textContent = `
                @keyframes shakeError {
                    0%, 100% { transform: translateX(0); }
                    10%, 30%, 50%, 70%, 90% { transform: translateX(-5px); }
                    20%, 40%, 60%, 80% { transform: translateX(5px); }
                }
                
                .shake-error {
                    animation: shakeError 0.6s ease-in-out;
                }
            `;
            document.head.appendChild(style);
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
        
        // 添加页面加载进度效果
        if (show) {
            const progressBar = document.createElement('div');
            progressBar.id = 'loading-progress';
            progressBar.style.cssText = `
                position: fixed;
                top: 0;
                left: 0;
                width: 0%;
                height: 3px;
                background: linear-gradient(90deg, var(--tenant-primary-color), var(--tenant-accent-color));
                z-index: 10001;
                transition: width 0.3s ease;
            `;
            document.body.appendChild(progressBar);
            
            // 模拟加载进度
            let progress = 0;
            const interval = setInterval(() => {
                progress += Math.random() * 30;
                if (progress > 90) progress = 90;
                progressBar.style.width = progress + '%';
            }, 200);
            
            // 保存interval用于清理
            progressBar._interval = interval;
        } else {
            const progressBar = document.getElementById('loading-progress');
            if (progressBar) {
                clearInterval(progressBar._interval);
                progressBar.style.width = '100%';
                setTimeout(() => {
                    progressBar.remove();
                }, 300);
            }
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
                    <h3>🚫 访问受限</h3>
                    <p>${message}</p>
                    <div class="error-actions">
                        <button onclick="location.reload()" class="btn btn-primary">
                            <i class="fa fa-refresh"></i> 重新加载
                        </button>
                        <a href="/login" class="btn btn-secondary">
                            <i class="fa fa-home"></i> 返回主登录页
                        </a>
                    </div>
                    <div class="error-tips">
                        <p>💡 遇到问题？</p>
                        <ul>
                            <li>检查网络连接是否正常</li>
                            <li>确认租户地址是否正确</li>
                            <li>联系管理员获取帮助</li>
                        </ul>
                    </div>
                </div>
            </div>
        `;
        
        document.getElementById('root').innerHTML = errorHTML;
        
        // 添加错误页面样式
        if (!document.getElementById('error-page-style')) {
            const style = document.createElement('style');
            style.id = 'error-page-style';
            style.textContent = `
                .error-tips {
                    margin-top: 30px;
                    padding-top: 20px;
                    border-top: 1px solid rgba(255, 255, 255, 0.2);
                    text-align: left;
                }
                
                .error-tips p {
                    margin: 0 0 10px 0;
                    font-weight: 500;
                    color: var(--tenant-text-light);
                }
                
                .error-tips ul {
                    margin: 0;
                    padding-left: 20px;
                    color: var(--tenant-text-light);
                    opacity: 0.8;
                }
                
                .error-tips li {
                    margin-bottom: 5px;
                    font-size: 14px;
                }
            `;
            document.head.appendChild(style);
        }
    }
    
    /**
     * 添加表单验证增强功能
     */
    function addFormValidationEnhancements() {
        // 监听表单提交事件
        document.addEventListener('submit', function(e) {
            const form = e.target.closest('form');
            if (form && form.classList.contains('tenant-login-form')) {
                // 添加提交动画
                addSubmitAnimation(form);
            }
        });
        
        // 实时验证用户名格式
        document.addEventListener('input', function(e) {
            if (e.target.matches('input[name="userName"]')) {
                validateUserName(e.target);
            }
            
            if (e.target.matches('input[name="password"]')) {
                validatePassword(e.target);
            }
        });
        
        // 监听表单错误事件
        document.addEventListener('DOMNodeInserted', function(e) {
            if (e.target.classList && e.target.classList.contains('cxd-Form-feedback')) {
                enhanceErrorMessage(e.target);
            }
        });
    }
    
    /**
     * 验证用户名格式
     * @param {HTMLElement} input 输入框元素
     */
    function validateUserName(input) {
        const value = input.value.trim();
        const container = input.closest('.tenant-input-field');
        
        // 清除之前的状态
        container.classList.remove('valid', 'invalid');
        
        if (value.length === 0) {
            return;
        }
        
        // 用户名格式验证
        const isEmail = /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value);
        const isPhone = /^1[3-9]\d{9}$/.test(value);
        const isUsername = /^[a-zA-Z0-9_\u4e00-\u9fa5]{2,50}$/.test(value);
        
        if (isEmail || isPhone || isUsername) {
            container.classList.add('valid');
            showFieldSuccess(container, getValidationSuccessMessage(value, isEmail, isPhone));
        } else {
            container.classList.add('invalid');
            showFieldError(container, '请输入有效的用户名、手机号或邮箱地址');
        }
    }
    
    /**
     * 验证密码强度
     * @param {HTMLElement} input 输入框元素
     */
    function validatePassword(input) {
        const value = input.value;
        const container = input.closest('.tenant-input-field');
        
        // 清除之前的状态
        container.classList.remove('valid', 'invalid', 'weak', 'medium', 'strong');
        
        if (value.length === 0) {
            return;
        }
        
        const strength = calculatePasswordStrength(value);
        
        if (value.length < 6) {
            container.classList.add('invalid');
            showFieldError(container, '密码至少需要6个字符');
            return;
        }
        
        container.classList.add('valid', strength.level);
        showPasswordStrength(container, strength);
    }
    
    /**
     * 计算密码强度
     * @param {string} password 密码
     * @returns {Object} 强度信息
     */
    function calculatePasswordStrength(password) {
        let score = 0;
        const checks = {
            length: password.length >= 8,
            lowercase: /[a-z]/.test(password),
            uppercase: /[A-Z]/.test(password),
            numbers: /\d/.test(password),
            symbols: /[^A-Za-z0-9]/.test(password)
        };
        
        Object.values(checks).forEach(check => check && score++);
        
        if (score >= 4) return { level: 'strong', text: '强', color: '#52c41a', score };
        if (score >= 3) return { level: 'medium', text: '中', color: '#faad14', score };
        return { level: 'weak', text: '弱', color: '#ff4d4f', score };
    }
    
    /**
     * 获取验证成功消息
     * @param {string} value 输入值
     * @param {boolean} isEmail 是否为邮箱
     * @param {boolean} isPhone 是否为手机号
     * @returns {string} 成功消息
     */
    function getValidationSuccessMessage(value, isEmail, isPhone) {
        if (isEmail) return '✓ 邮箱格式正确';
        if (isPhone) return '✓ 手机号格式正确';
        return '✓ 用户名格式正确';
    }
    
    /**
     * 显示字段错误
     * @param {HTMLElement} container 容器元素
     * @param {string} message 错误消息
     */
    function showFieldError(container, message) {
        removeFieldMessage(container);
        
        const errorEl = document.createElement('div');
        errorEl.className = 'field-validation-message error-message';
        errorEl.innerHTML = `<i class="fa fa-exclamation-circle"></i> ${message}`;
        container.appendChild(errorEl);
        
        // 添加震动动画
        container.classList.add('validation-shake');
        setTimeout(() => container.classList.remove('validation-shake'), 600);
    }
    
    /**
     * 显示字段成功
     * @param {HTMLElement} container 容器元素
     * @param {string} message 成功消息
     */
    function showFieldSuccess(container, message) {
        removeFieldMessage(container);
        
        const successEl = document.createElement('div');
        successEl.className = 'field-validation-message success-message';
        successEl.innerHTML = `<i class="fa fa-check-circle"></i> ${message}`;
        container.appendChild(successEl);
    }
    
    /**
     * 显示密码强度
     * @param {HTMLElement} container 容器元素
     * @param {Object} strength 强度信息
     */
    function showPasswordStrength(container, strength) {
        removeFieldMessage(container);
        
        const strengthEl = document.createElement('div');
        strengthEl.className = 'field-validation-message strength-message';
        strengthEl.innerHTML = `
            <div class="password-strength-indicator">
                <span class="strength-text" style="color: ${strength.color}">
                    <i class="fa fa-shield-alt"></i> 密码强度: ${strength.text}
                </span>
                <div class="strength-bar">
                    <div class="strength-fill ${strength.level}" style="width: ${(strength.score / 5) * 100}%; background: ${strength.color}"></div>
                </div>
            </div>
        `;
        container.appendChild(strengthEl);
    }
    
    /**
     * 移除字段消息
     * @param {HTMLElement} container 容器元素
     */
    function removeFieldMessage(container) {
        const existing = container.querySelector('.field-validation-message');
        if (existing) {
            existing.remove();
        }
    }
    
    /**
     * 增强错误消息显示
     * @param {HTMLElement} errorElement 错误元素
     */
    function enhanceErrorMessage(errorElement) {
        if (errorElement.textContent.includes('[object Object]')) {
            errorElement.textContent = '输入格式不正确，请检查后重试';
            errorElement.style.color = '#ff4d4f';
            errorElement.style.fontSize = '12px';
            errorElement.style.marginTop = '4px';
        }
    }
    
    /**
     * 添加提交动画
     * @param {HTMLElement} form 表单元素
     */
    function addSubmitAnimation(form) {
        const submitBtn = form.querySelector('.tenant-login-btn .cxd-Button');
        if (submitBtn) {
            submitBtn.classList.add('submitting');
            
            // 添加加载动画
            const originalText = submitBtn.textContent;
            submitBtn.innerHTML = '<i class="fa fa-spinner fa-spin"></i> 登录中...';
            
            // 5秒后恢复（防止卡死）
            setTimeout(() => {
                if (submitBtn.classList.contains('submitting')) {
                    submitBtn.classList.remove('submitting');
                    submitBtn.innerHTML = originalText;
                }
            }, 5000);
        }
    }
})(); 