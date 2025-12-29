/**
 * 考试系统主页 - 基于AMIS框架
 * 包含租户信息、考生信息、导航菜单等功能
 * 支持移动端适配
 */
(function () {
    'use strict';
    
    // 确保TokenManager已初始化
    TokenManager.initClientMode(window.tenantId, 'exam');
    
    // 检查用户认证状态
    if (!TokenManager.isAuthenticated()) {
        // 保存当前页面URL用于登录后跳转
        const currentUrl = encodeURIComponent(window.location.pathname + window.location.search);
        window.location.href = `/${window.tenantId}/exam/login?redirect=${currentUrl}`;
        return;
    }
    
    let amis = amisRequire('amis/embed');
    let amisInstance = null;
    
    // 全局数据存储
    const appData = {
        tenant: { id: '', name: '', logo: '', description: '' },
        student: { 
            name: '', 
            displayName: '',
            avatar: ''
        }
    };
    
    /**
     * 通用API请求函数
     * 使用ExamApiManager进行统一的API请求处理
     */
    async function apiRequest(url, options = {}) {
        return window.ExamApiManager.request(url, options);
    }
    
    /**
     * 构建AMIS页面配置
     */
    function buildPageConfig() {
        return {
            type: "page",
            title: "",
            className: "exam-index-page",
            data: {
                tenant: appData.tenant,
                student: appData.student,
                now: new Date()
            },
            body: [
                // 租户信息条
                {
                    type: "container",
                    className: "tenant-info-bar",
                    body: [
                        {
                            type: "flex",
                            justify: "flex-start",
                            alignItems: "center",
                            className: "tenant-info-content",
                            items: [
                                {
                                    type: "tpl",
                                    tpl: "<div class='tenant-name'><i class='fa fa-building'></i> ${tenant.name || '考试平台'}</div>"
                                },
                                {
                                    type: "tpl", 
                                    tpl: "<div class='current-time'><i class='fa fa-clock-o'></i> ${now | date:'HH:mm'}</div>",
                                    className: "current-time"
                                },
                                {
                                    type: "html",
                                    html: `
                                        <div class="logout-container">
                                            <div class="logout-btn" onclick="window.handleLogout()" title="安全退出">
                                                <i class="fa fa-sign-out logout-icon"></i>
                                                <span class="logout-text">退出</span>
                                            </div>
                                        </div>
                                    `
                                }
                            ]
                        }
                    ]
                },
                
                // 考生信息卡片（简化版）
                {
                    type: "container",
                    className: "student-info-card",
                    body: [
                        {
                            type: "flex",
                            justify: "flex-start",
                            alignItems: "center",
                            items: [
                                {
                                    type: "avatar",
                                    src: "${student.avatar}",
                                    text: "${student.displayName || student.name || '用户' | substring:0:1}",
                                    className: "student-avatar",
                                    size: 50
                                },
                                {
                                    type: "container",
                                    className: "flex-grow-1 ml-3",
                                    body: [
                                        {
                                            type: "tpl",
                                            tpl: "<div class='student-name'>${student.displayName || student.name || '未知用户'}</div>"
                                        },
                                        {
                                            type: "tpl",
                                            tpl: "<div class='student-welcome'>欢迎使用考试系统</div>"
                                        }
                                    ]
                                }
                            ]
                        }
                    ]
                },
                
                // 导航菜单
                {
                    type: "container",
                    className: "nav-menu-section",
                    body: [
                        {
                            type: "html",
                            html: `
                                <div class="nav-menu-grid">
                                    <div class="nav-menu-item nav-exam" onclick="window.navigateTo('exam')" data-animate="0">
                                        <i class="fa fa-graduation-cap nav-menu-icon"></i>
                                        <div class="nav-menu-text">我的考试</div>
                                    </div>
                                    <div class="nav-menu-item nav-practice" onclick="window.navigateTo('practice')" data-animate="1">
                                        <i class="fa fa-pencil nav-menu-icon"></i>
                                        <div class="nav-menu-text">开始练习</div>
                                    </div>
                                    <div class="nav-menu-item nav-my-practice" onclick="window.navigateTo('my-practice')" data-animate="2">
                                        <i class="fa fa-history nav-menu-icon"></i>
                                        <div class="nav-menu-text">我的练习</div>
                                    </div>
                                    <div class="nav-menu-item nav-wrong-questions" onclick="window.navigateTo('wrong-questions')" data-animate="3">
                                        <i class="fa fa-exclamation-triangle nav-menu-icon"></i>
                                        <div class="nav-menu-text">错题管理</div>
                                    </div>
                                </div>
                            `
                        }
                    ]
                }
            ]
        };
    }
    
    /**
     * 导航到指定页面
     */
    window.navigateTo = function(page) {
        const routes = {
            'practice': `/${window.tenantId}/exam/practice`,
            'exam': `/${window.tenantId}/exam/app`,
            'my-exams': `/${window.tenantId}/exam/history`,
            'my-practice': `/${window.tenantId}/exam/practice-history`,
            'wrong-questions': `/${window.tenantId}/exam/wrong-questions`,
            'profile': `/${window.tenantId}/exam/profile`
        };
        
        if (routes[page]) {
            // 添加点击反馈动画
            const clickedItem = event.target.closest('.nav-menu-item');
            if (clickedItem) {
                clickedItem.style.transform = 'scale(0.95)';
                setTimeout(() => {
                    window.location.href = routes[page];
                }, 150);
            } else {
                window.location.href = routes[page];
            }
        }
    };
    
    /**
     * 处理退出登录
     */
    window.handleLogout = function() {
        showLogoutConfirmDialog();
    };

    /**
     * 显示退出登录确认对话框
     */
    function showLogoutConfirmDialog() {
        const overlay = document.createElement('div');
        overlay.className = 'logout-overlay';
        
        const dialog = document.createElement('div');
        dialog.className = 'logout-dialog';
        dialog.innerHTML = `
            <div class="logout-dialog-header">
                <div class="logout-dialog-icon">
                    <i class="fa fa-sign-out"></i>
                </div>
                <h3>确认退出登录</h3>
            </div>
            <div class="logout-dialog-body">
                <p>🔒 您确定要退出当前账户吗？</p>
                <p class="logout-dialog-note">退出后需要重新输入账号密码才能登录</p>
            </div>
            <div class="logout-dialog-actions">
                <button class="logout-cancel-btn" onclick="window.closeLogoutDialog()">
                    <i class="fa fa-times"></i> 取消
                </button>
                <button class="logout-confirm-btn" onclick="window.confirmLogout()">
                    <i class="fa fa-sign-out"></i> 确认退出
                </button>
            </div>
        `;
        
        overlay.appendChild(dialog);
        document.body.appendChild(overlay);
        
        setTimeout(() => {
            overlay.classList.add('show');
            dialog.classList.add('show');
        }, 10);
        
        overlay.addEventListener('click', (e) => {
            if (e.target === overlay) {
                window.closeLogoutDialog();
            }
        });
        
        const handleEsc = (e) => {
            if (e.key === 'Escape') {
                window.closeLogoutDialog();
                document.removeEventListener('keydown', handleEsc);
            }
        };
        document.addEventListener('keydown', handleEsc);
    }

    /**
     * 关闭退出登录对话框
     */
    window.closeLogoutDialog = function() {
        const overlay = document.querySelector('.logout-overlay');
        if (overlay) {
            overlay.classList.remove('show');
            setTimeout(() => {
                document.body.removeChild(overlay);
            }, 300);
        }
    };

    /**
     * 确认退出登录
     */
    window.confirmLogout = function() {
        try {
            window.closeLogoutDialog();
            
            const logoutBtn = document.querySelector('.logout-btn');
            if (logoutBtn) {
                logoutBtn.classList.add('logging-out');
                logoutBtn.innerHTML = '<i class="fa fa-spinner fa-spin logout-icon"></i><span class="logout-text">退出中...</span>';
            }
            
            document.body.style.transition = 'all 0.5s ease';
            document.body.style.opacity = '0.7';
            document.body.style.transform = 'scale(0.98)';
            document.body.style.filter = 'blur(1px)';
            
            TokenManager.clearToken();
            
            setTimeout(() => {
                window.location.href = `/${window.tenantId}/exam/login`;
            }, 800);
            
        } catch (error) {
            console.error('退出登录失败:', error);
            window.location.href = `/${window.tenantId}/exam/login`;
        }
    };
    
    /**
     * 增强导航菜单动画效果
     */
    function enhanceNavigationAnimations() {
        setTimeout(() => {
            const menuItems = document.querySelectorAll('[data-animate]');
            menuItems.forEach((item, index) => {
                const delay = index * 120;
                setTimeout(() => {
                    item.style.animation = `fadeInUp 0.6s var(--exam-bounce) both`;
                    item.style.opacity = '1';
                }, delay);
            });
        }, 800);
        
        const menuItems = document.querySelectorAll('[data-animate]');
        menuItems.forEach(item => {
            item.style.opacity = '0';
        });
    }
    
    /**
     * 加载初始数据
     */
    async function loadInitialData() {
        try {
            const tenantInfo = await loadTenantInfo();
            Object.assign(appData.tenant, tenantInfo);
            
            const studentInfo = await loadStudentInfo().catch(error => {
                console.warn('加载学生信息失败:', error);
                return { 
                    name: '未知用户',
                    displayName: '未知用户',
                    avatar: ''
                };
            });
            
            Object.assign(appData.student, studentInfo);
            
            if (amisInstance) {
                amisInstance.updateProps({
                    data: {
                        ...appData,
                        now: new Date()
                    }
                });
            }
            
            if (studentInfo.name) {
                document.title = `${studentInfo.name} - 考试系统`;
            }
            
        } catch (error) {
            console.error('加载初始数据失败:', error);
            showError('加载数据失败，请刷新页面重试');
        }
    }
    
    /**
     * 加载租户信息
     */
    async function loadTenantInfo() {
        try {
            const tenantConfig = await window.ExamApiManager.getLoginConfig(window.tenantId);
            
            return {
                id: window.tenantId,
                name: tenantConfig.displayName || tenantConfig.name || '考试平台',
                logo: tenantConfig.logoUrl || '/logo.png',
                description: tenantConfig.description || ''
            };
        } catch (error) {
            console.warn('加载租户信息失败:', error);
            return { 
                id: window.tenantId,
                name: '考试平台', 
                logo: '/logo.png',
                description: '' 
            };
        }
    }
    
    /**
     * 加载考生信息
     */
    async function loadStudentInfo() {
        try {
            const profile = await apiRequest('/exam/api/exam/client/profile');
            
            return {
                name: profile.name || '',
                displayName: profile.name || '',
                avatar: ''
            };
        } catch (error) {
            console.warn('加载考生信息失败:', error);
            try {
                const identityProfile = await apiRequest('/identity/api/identity/profile');
                return {
                    name: identityProfile.userName || identityProfile.name || '',
                    displayName: identityProfile.displayName || identityProfile.userName || identityProfile.name || '',
                    avatar: identityProfile.avatar || ''
                };
            } catch (fallbackError) {
                return { 
                    name: '未知用户',
                    displayName: '未知用户',
                    avatar: ''
                };
            }
        }
    }
    
    /**
     * 显示错误信息
     */
    function showError(message) {
        showLoading(false);
        
        const errorHTML = `
            <div class="exam-error">
                <div class="error-content">
                    <div class="error-icon">
                        <i class="fa fa-exclamation-triangle"></i>
                    </div>
                    <h3>🚫 无法加载页面</h3>
                    <p>${message}</p>
                    <div class="error-actions">
                        <button onclick="location.reload()" class="btn btn-primary">
                            <i class="fa fa-refresh"></i> 重新加载
                        </button>
                    </div>
                </div>
            </div>
        `;
        
        document.getElementById('root').innerHTML = errorHTML;
    }
    
    /**
     * 显示/隐藏加载状态
     */
    function showLoading(show) {
        const loading = document.getElementById('loading');
        if (loading) {
            loading.style.display = show ? 'flex' : 'none';
        }
    }
    
    /**
     * 初始化页面
     */
    async function initPage() {
        try {
            showLoading(true);
            
            const amisConfig = buildPageConfig();
            
            amisInstance = amis.embed('#root', amisConfig, {
                location: history.location,
                data: {
                    ...appData,
                    now: new Date()
                },
                context: {
                    WEB_HOST: window.webHost,
                    TENANT_ID: window.tenantId
                },
                requestAdaptor: (api) => {
                    const token = TokenManager.getToken();
                    return {
                        ...api,
                        headers: {
                            ...api.headers,
                            'Authorization': token ? 'Bearer ' + token : '',
                            'TenantId': window.tenantId,
                            'X-Forwarded-With': 'CodeSpirit'
                        }
                    };
                }
            }, {
                theme: 'antd',
                locale: 'zh-CN'
            });
            
            await loadInitialData();
            
            setInterval(() => {
                if (amisInstance) {
                    amisInstance.updateProps({
                        data: {
                            now: new Date()
                        }
                    });
                }
            }, 30000);
            
            enhanceNavigationAnimations();
            
        } catch (error) {
            console.error('页面初始化失败:', error);
            showError('页面加载失败，请刷新重试');
        } finally {
            showLoading(false);
        }
    }
    
    // 页面加载完成后初始化
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initPage);
    } else {
        initPage();
    }
    
})();

