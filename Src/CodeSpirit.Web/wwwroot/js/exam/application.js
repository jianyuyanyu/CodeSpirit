/**
 * 考试应用页面 - 基于AMIS框架
 * 包含租户信息、考生信息、公告通知、导航菜单等功能
 * 支持移动端适配，前端获取用户信息
 */
(function () {
    'use strict';
    
    // 确保TokenManager已初始化
    TokenManager.initClientMode(window.tenantId, 'exam');
    
    // 检查用户认证状态
    if (!TokenManager.isAuthenticated()) {
        window.location.href = `/${window.tenantId}/exam/login`;
        return;
    }
    
    let amis = amisRequire('amis/embed');
    let amisInstance = null;
    
    // 全局数据存储
    const appData = {
        tenant: { id: '', name: '', logo: '', description: '' },
        student: { 
            name: '', 
            idCard: '', 
            gender: '', 
            examNumber: '', 
            studentId: '', 
            phone: '', 
            avatar: '',
            displayName: '',
            studentGroups: [],
            userId: 0,
            candidateId: 0
        },
        announcements: [],
        availableExams: []
    };
    
    /**
     * 通用API请求函数
     */
    async function apiRequest(url, options = {}) {
        try {
            const token = TokenManager.getToken();
            const response = await fetch(url, {
                ...options,
                headers: {
                    'Authorization': token ? 'Bearer ' + token : '',
                    'TenantId': window.tenantId,
                    'X-Forwarded-With': 'CodeSpirit',
                    'Content-Type': 'application/json',
                    ...options.headers
                }
            });
            
            if (response.status === 401) {
                window.location.href = `/${window.tenantId}/exam/login`;
                throw new Error('认证失败，请重新登录');
            }
            
            if (!response.ok) {
                throw new Error(`HTTP ${response.status}: ${response.statusText}`);
            }
            
            const result = await response.json();
            if (result.status !== 0) {
                throw new Error(result.msg || '请求失败');
            }
            
            return result.data;
        } catch (error) {
            console.error(`API请求失败 [${url}]:`, error);
            throw error;
        }
    }
    
    /**
     * 构建AMIS页面配置
     */
    function buildPageConfig() {
        return {
            type: "page",
            title: "",
            className: "exam-app-page",
            data: {
                // 初始数据，将通过API更新
                tenant: appData.tenant,
                student: appData.student,
                announcements: appData.announcements,
                availableExams: appData.availableExams,
                now: new Date()
            },
            body: [
                // 租户信息条
                {
                    type: "container",
                    className: "tenant-info-bar",
                    body: [
                        {
                            type: "tpl",
                            tpl: "<div class='tenant-name'><i class='fa fa-building'></i> ${tenant.name || '考试平台'}</div>"
                        },
                        {
                            type: "flex",
                            justify: "flex-end",
                            alignItems: "center",
                            className: "tenant-actions",
                            items: [
                                {
                                    type: "tpl", 
                                    tpl: "<div class='current-time'><i class='fa fa-clock-o'></i> ${now | date:'HH:mm'}</div>",
                                    className: "current-time"
                                },
                                {
                                    type: "html",
                                    html: `
                                        <div class="logout-btn" onclick="window.handleLogout()" title="退出登录">
                                            <i class="fa fa-sign-out"></i>
                                        </div>
                                    `
                                }
                            ]
                        }
                    ]
                },
                
                // 考生信息卡片
                {
                    type: "container",
                    className: "student-info-card",
                    body: [
                        {
                            type: "flex",
                            justify: "flex-start",
                            alignItems: "center",
                            items: [
                                // 头像区域
                                {
                                    type: "container",
                                    className: "flex-shrink-0",
                                    body: [
                                        // 头像图片（如果有的话）
                                        {
                                            type: "avatar",
                                            src: "${student.avatar}",
                                            text: "${student.displayName || student.name || '用户' | substring:0:1}",
                                            className: "student-avatar",
                                            size: 60
                                        }
                                    ]
                                },
                                // 信息区域
                                {
                                    type: "container",
                                    className: "flex-grow-1 ml-3",
                                    body: [
                                        {
                                            type: "grid",
                                            columns: [
                                                {
                                                    md: 6,
                                                    body: {
                                                        type: "tpl",
                                                        tpl: "<div class='student-info-item'><span class='info-label'>姓名:</span><span class='info-value'>\${student.displayName || student.name || '未知'}</span></div>"
                                                    }
                                                },
                                                {
                                                    md: 6,
                                                    body: {
                                                        type: "tpl",
                                                        tpl: "<div class='student-info-item'><span class='info-label'>性别:</span><span class='info-value'>\${student.gender || '未知'}</span></div>"
                                                    }
                                                },
                                                {
                                                    md: 6,
                                                    body: {
                                                        type: "tpl",
                                                        tpl: "<div class='student-info-item'><span class='info-label'>准考证:</span><span class='info-value'>\${student.examNumber || '未设置'}</span></div>"
                                                    }
                                                },
                                                {
                                                    md: 6,
                                                    body: {
                                                        type: "tpl",
                                                        tpl: "<div class='student-info-item'><span class='info-label'>学号:</span><span class='info-value'>\${student.studentId || '未设置'}</span></div>"
                                                    }
                                                },
                                                {
                                                    md: 6,
                                                    body: {
                                                        type: "tpl",
                                                        tpl: "<div class='student-info-item'><span class='info-label'>手机:</span><span class='info-value'>\${student.phone || '未设置'}</span></div>"
                                                    }
                                                },
                                                {
                                                    md: 6,
                                                    body: {
                                                        type: "tpl",
                                                        tpl: "<div class='student-info-item'><span class='info-label'>身份证:</span><span class='info-value'>\${student.idCard}</span></div>"
                                                    }
                                                }
                                            ]
                                        }
                                    ]
                                }
                            ]
                        }
                    ]
                },
                
                // 当前考试面板
                {
                    type: "container",
                    className: "current-exams-section",
                    visibleOn: "${availableExams && availableExams.length > 0}",
                    body: [
                        {
                            type: "container",
                            className: "current-exams-header",
                            body: [
                                {
                                    type: "tpl",
                                    tpl: "<i class='fa fa-graduation-cap'></i> 当前考试"
                                }
                            ]
                        },
                        {
                            type: "container",
                            className: "current-exams-content",
                            body: [
                                {
                                    type: "each",
                                    name: "availableExams",
                                    items: {
                                        type: "container",
                                        className: "exam-item",
                                        body: [
                                            {
                                                type: "flex",
                                                justify: "space-between",
                                                alignItems: "center",
                                                items: [
                                                    {
                                                        type: "container",
                                                        className: "flex-grow-1",
                                                        body: [
                                                            {
                                                                type: "tpl",
                                                                tpl: "<div class='exam-title'>\${name}</div>"
                                                            },
                                                            {
                                                                type: "tpl",
                                                                tpl: "<div class='exam-info'><span class='exam-time'><i class='fa fa-clock-o'></i> \${startTimeFormatted || startTime} - \${endTimeFormatted || endTime}</span><span class='exam-duration'><i class='fa fa-hourglass-half'></i> \${duration}分钟</span><span class='exam-score'><i class='fa fa-star'></i> \${totalScore}分</span></div>"
                                                            },
                                                            {
                                                                type: "tpl",
                                                                tpl: "<div class='exam-description'>\${description || '暂无描述'}</div>",
                                                                visibleOn: "${description}"
                                                            }
                                                        ]
                                                    },
                                                    {
                                                        type: "html",
                                                        html: `
                                                            <div class="exam-actions">
                                                                <div class="exam-status-badge \${status === '未开始' || status === '进行中' ? 'status-available' : (status === '已结束' ? 'status-ended' : 'status-unknown')}">
                                                                    \${status}
                                                                </div>
                                                                <button class="exam-start-btn" onclick="window.goToExamStart('\${id}')" 
                                                                        \${status === '未开始' || status === '进行中' ? '' : 'disabled'}>
                                                                    <i class="fa fa-play"></i>
                                                                    \${status === '未开始' ? '开始考试' : (status === '进行中' ? '继续考试' : '考试已结束')}
                                                                </button>
                                                            </div>
                                                        `
                                                    }
                                                ]
                                            }
                                        ]
                                    },
                                    placeholder: {
                                        type: "container",
                                        className: "exam-empty",
                                        body: [
                                            {
                                                type: "tpl",
                                                tpl: "<i class='fa fa-calendar-times-o'></i><div>暂无可参加的考试</div>"
                                            }
                                        ]
                                    }
                                }
                            ]
                        }
                    ]
                },
                
                // 公告通知区域
                {
                    type: "container",
                    className: "announcement-section",
                    body: [
                        {
                            type: "container",
                            className: "announcement-header",
                            body: [
                                {
                                    type: "tpl",
                                    tpl: "<i class='fa fa-bullhorn'></i> 公告通知"
                                }
                            ]
                        },
                        {
                            type: "container",
                            className: "announcement-content",
                            body: [
                                {
                                    type: "each",
                                    name: "announcements",
                                    items: {
                                        type: "container",
                                        className: "announcement-item",
                                        body: [
                                            {
                                                type: "flex",
                                                justify: "space-between",
                                                alignItems: "flex-start",
                                                items: [
                                                    {
                                                        type: "container",
                                                        className: "flex-grow-1",
                                                        body: [
                                                            {
                                                                type: "tpl",
                                                                tpl: "<div class='announcement-title'>\${title}</div>"
                                                            },
                                                            {
                                                                type: "tpl",
                                                                tpl: "<div class='announcement-content-text'>\${content}</div>"
                                                            }
                                                        ]
                                                    },
                                                    {
                                                        type: "tpl",
                                                        tpl: "<span class='announcement-time'>\${publishTime | date:'MM-DD HH:mm'}</span>"
                                                    }
                                                ]
                                            }
                                        ]
                                    },
                                    placeholder: {
                                        type: "container",
                                        className: "announcement-empty",
                                        body: [
                                            {
                                                type: "tpl",
                                                tpl: "<i class='fa fa-info-circle'></i><div>暂无公告通知</div>"
                                            }
                                        ]
                                    }
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
                                    <div class="nav-menu-item nav-practice" onclick="window.navigateTo('practice')" data-animate="0">
                                        <i class="fa fa-pencil nav-menu-icon"></i>
                                        <div class="nav-menu-text">开始练习</div>
                                    </div>
                                    <div class="nav-menu-item nav-exam" onclick="window.navigateTo('exam')" data-animate="1">
                                        <i class="fa fa-graduation-cap nav-menu-icon"></i>
                                        <div class="nav-menu-text">开始考试</div>
                                    </div>
                                    <div class="nav-menu-item nav-my-exams" onclick="window.navigateTo('my-exams')" data-animate="2">
                                        <i class="fa fa-file-text nav-menu-icon"></i>
                                        <div class="nav-menu-text">我的考试</div>
                                    </div>
                                    <div class="nav-menu-item nav-my-practice" onclick="window.navigateTo('my-practice')" data-animate="3">
                                        <i class="fa fa-history nav-menu-icon"></i>
                                        <div class="nav-menu-text">我的练习</div>
                                    </div>
                                    <div class="nav-menu-item nav-wrong-questions" onclick="window.navigateTo('wrong-questions')" data-animate="4">
                                        <i class="fa fa-exclamation-triangle nav-menu-icon"></i>
                                        <div class="nav-menu-text">错题管理</div>
                                    </div>
                                    <div class="nav-menu-item nav-profile" onclick="window.navigateTo('profile')" data-animate="5">
                                        <i class="fa fa-user nav-menu-icon"></i>
                                        <div class="nav-menu-text">个人中心</div>
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
            'exam': `/${window.tenantId}/exam/start`,
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
        // 显示确认对话框
        if (confirm('🚪 确定要退出登录吗？\n\n退出后需要重新输入账号密码才能登录。')) {
            try {
                // 清除Token
                TokenManager.clearToken();
                
                // 显示退出动画
                const logoutBtn = document.querySelector('.logout-btn');
                if (logoutBtn) {
                    logoutBtn.style.transform = 'scale(0.8) rotate(180deg)';
                    logoutBtn.style.opacity = '0.5';
                    logoutBtn.innerHTML = '<i class="fa fa-spinner fa-spin"></i>';
                }
                
                // 页面渐出效果
                document.body.style.opacity = '0.7';
                document.body.style.transform = 'scale(0.95)';
                document.body.style.filter = 'blur(2px)';
                
                // 延迟跳转到登录页
                setTimeout(() => {
                    window.location.href = `/${window.tenantId}/exam/login`;
                }, 500);
                
            } catch (error) {
                console.error('退出登录失败:', error);
                // 即使出错也要跳转到登录页
                window.location.href = `/${window.tenantId}/exam/login`;
            }
        }
    };
    
    /**
     * 跳转到考试开始页面
     * @param {string|number} examId 考试ID
     */
    function goToExamStart(examId) {
        if (!examId) {
            console.error('考试ID不能为空');
            return;
        }
        
        // 确保examId为字符串类型，去除可能的引号
        const cleanExamId = String(examId).replace(/['"]/g, '');
        const url = `/${window.tenantId}/exam/start/${cleanExamId}`;
        console.log('跳转到考试开始页面:', url, '考试ID:', cleanExamId);
        window.location.href = url;
    }

    // 将函数暴露到全局
    window.goToExamStart = goToExamStart;

    /**
     * 显示功能提示（用于新功能）
     */
    function showFeatureTip() {
        // 可以在这里添加新功能的介绍提示
        const isFirstVisit = !localStorage.getItem('exam-app-visited');
        if (isFirstVisit) {
            setTimeout(() => {
                console.log('🎉 欢迎使用考试管理平台！新增了错题管理和个人中心功能。');
                localStorage.setItem('exam-app-visited', 'true');
            }, 2000);
        }
    }
    
    /**
     * 增强导航菜单动画效果
     */
    function enhanceNavigationAnimations() {
        // 为导航菜单项添加交错动画
        setTimeout(() => {
            const menuItems = document.querySelectorAll('[data-animate]');
            menuItems.forEach((item, index) => {
                const delay = index * 120; // 稍微延长间隔以适应6个菜单项
                setTimeout(() => {
                    item.style.animation = `fadeInUp 0.6s var(--exam-bounce) both`;
                    item.style.opacity = '1';
                }, delay);
            });
        }, 800);
        
        // 初始隐藏菜单项
        const menuItems = document.querySelectorAll('[data-animate]');
        menuItems.forEach(item => {
            item.style.opacity = '0';
        });
        
        // 添加鼠标移动视差效果
        document.addEventListener('mousemove', handleMouseMove);
    }
    
    /**
     * 处理鼠标移动视差效果
     */
    function handleMouseMove(e) {
        // 检测是否为触摸设备，如果是则跳过视差效果
        if (window.matchMedia('(hover: none) and (pointer: coarse)').matches) {
            return;
        }
        
        const cards = document.querySelectorAll('.student-info-card, .announcement-section');
        const { clientX: x, clientY: y } = e;
        const { innerWidth: width, innerHeight: height } = window;
        
        cards.forEach(card => {
            const rect = card.getBoundingClientRect();
            const cardCenterX = rect.left + rect.width / 2;
            const cardCenterY = rect.top + rect.height / 2;
            
            const deltaX = (x - cardCenterX) / width * 8;
            const deltaY = (y - cardCenterY) / height * 8;
            
            card.style.transform = `translateX(${deltaX}px) translateY(${deltaY}px)`;
        });
    }
    
    /**
     * 加载初始数据
     */
    async function loadInitialData() {
        try {
            // 首先加载租户信息，如果失败则不继续
            const tenantInfo = await loadTenantInfo();
            Object.assign(appData.tenant, tenantInfo);
            
            // 然后并行加载其他数据
            const [studentInfo, announcements, availableExams] = await Promise.all([
                loadStudentInfo().catch(error => {
                    console.warn('加载学生信息失败:', error);
                    return { 
                        name: '未知用户', 
                        displayName: '未知用户',
                        idCard: '',
                        gender: '',
                        examNumber: '',
                        studentId: '',
                        phone: '',
                        avatar: '',
                        studentGroups: [],
                        userId: 0,
                        candidateId: 0
                    };
                }),
                loadAnnouncements().catch(error => {
                    console.warn('加载公告失败:', error);
                    return [];
                }),
                loadAvailableExams().catch(error => {
                    console.warn('加载可用考试失败:', error);
                    return [];
                })
            ]);
            
            // 更新应用数据
            Object.assign(appData.student, studentInfo);
            appData.announcements = announcements;
            appData.availableExams = availableExams;
            
            // 更新AMIS数据
            if (amisInstance) {
                amisInstance.updateProps({
                    data: {
                        ...appData,
                        now: new Date()
                    }
                });
            }
            
            // 更新页面标题
            if (studentInfo.name) {
                document.title = `${studentInfo.name} - 考试管理平台`;
            }
            
            // 在控制台显示考生信息摘要
            console.log('👤 当前考生信息摘要:');
            console.log(`   姓名: ${studentInfo.name || '未设置'}`);
            console.log(`   学号: ${studentInfo.studentId || '未设置'}`);
            console.log(`   准考证: ${studentInfo.examNumber || '未设置'}`);
            console.log(`   考生组: ${studentInfo.studentGroups?.length ? studentInfo.studentGroups.join(', ') : '未分组'}`);
            console.log(`   用户ID: ${studentInfo.userId || '未知'}`);
            console.log(`   考生ID: ${studentInfo.candidateId || '未知'}`);
            
        } catch (error) {
            console.error('加载初始数据失败:', error);
            
            // 如果是租户相关错误，显示更明确的错误信息
            if (error.message.includes('租户')) {
                showError(error.message);
            } else {
                showError('加载数据失败，请刷新页面重试');
            }
        }
    }
    
    /**
     * 加载租户信息
     * 参考login.js中的实现方式
     */
    async function loadTenantInfo() {
        try {
            const response = await fetch(`/identity/api/identity/tenants/${window.tenantId}/login-config`, {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Forwarded-With': 'CodeSpirit'
                }
            });
            
            // 处理HTTP错误状态
            if (response.status === 404) {
                throw new Error('租户不存在或已停用');
            } else if (response.status === 403) {
                throw new Error('您没有权限访问此租户');
            } else if (response.status >= 500) {
                throw new Error('服务器内部错误，请稍后重试');
            } else if (!response.ok) {
                throw new Error(`HTTP ${response.status}: ${response.statusText}`);
            }
            
            const result = await response.json();
            
            // 处理业务错误
            if (result.status !== 0) {
                throw new Error(result.msg || '获取租户配置失败');
            }
            
            // 转换为应用页面需要的格式
            const tenantConfig = result.data;
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
     * 加载考生信息 - 使用考试API获取
     */
    async function loadStudentInfo() {
        try {
            console.log('🔍 正在从考试API获取考生信息...');
            const profile = await apiRequest('/exam/api/exam/client/profile');
            console.log('✅ 考生信息获取成功:', profile);
            
            const studentInfo = {
                name: profile.name || '',
                displayName: profile.name || '',
                idCard: profile.idNo || '',
                gender: profile.gender || '',
                examNumber: profile.admissionTicket || '',
                studentId: profile.studentNumber || '',
                phone: profile.phoneNumber || '',
                avatar: '', // API中没有头像字段
                studentGroups: profile.studentGroups || [],
                userId: profile.userId || 0,
                candidateId: profile.id || 0
            };
            
            console.log('📋 处理后的考生信息:', studentInfo);
            return studentInfo;
            
        } catch (error) {
            console.warn('⚠️ 考试API获取考生信息失败:', error);
            
            // 如果考试API失败，尝试使用身份API作为备用
            try {
                console.log('🔄 尝试使用身份API作为备用...');
                const identityProfile = await apiRequest('/identity/api/identity/profile');
                console.log('✅ 身份API获取成功:', identityProfile);
                
                const fallbackInfo = {
                    name: identityProfile.userName || identityProfile.name || '',
                    displayName: identityProfile.displayName || identityProfile.userName || identityProfile.name || '',
                    idCard: identityProfile.idCard || '',
                    gender: identityProfile.gender || '',
                    examNumber: identityProfile.examNumber || '',
                    studentId: identityProfile.studentId || identityProfile.employeeId || '',
                    phone: identityProfile.phone || identityProfile.phoneNumber || '',
                    avatar: identityProfile.avatar || '',
                    studentGroups: [],
                    userId: identityProfile.id || 0,
                    candidateId: 0
                };
                
                console.log('📋 备用API处理后的信息:', fallbackInfo);
                return fallbackInfo;
                
            } catch (fallbackError) {
                console.error('❌ 备用身份API也失败:', fallbackError);
                console.log('📋 使用默认用户信息');
                
                return { 
                    name: '未知用户',
                    displayName: '未知用户',
                    idCard: '',
                    gender: '',
                    examNumber: '',
                    studentId: '',
                    phone: '',
                    avatar: '',
                    studentGroups: [],
                    userId: 0,
                    candidateId: 0
                };
            }
        }
    }
    
    /**
     * 加载可用考试列表
     */
    async function loadAvailableExams() {
        try {
            const data = await apiRequest('/exam/api/exam/client/available');
            
            // 处理考试数据，优化时间显示格式
            const processedData = (data || []).map(exam => ({
                ...exam,
                // 格式化时间显示，将"2025-06-09 12:00:00"转换为"06-09 12:00"格式
                startTimeFormatted: formatExamTime(exam.startTime),
                endTimeFormatted: formatExamTime(exam.endTime),
                // 保留原始时间用于比较
                startTimeOriginal: exam.startTime,
                endTimeOriginal: exam.endTime
            }));
            
            console.log('📅 处理后的考试数据:', processedData);
            return processedData;
        } catch (error) {
            console.warn('加载可用考试失败:', error);
            return [];
        }
    }
    
    /**
     * 格式化考试时间显示
     * @param {string} timeString - 时间字符串，格式如"2025-06-09 12:00:00"
     * @returns {string} 格式化后的时间，如"06-09 12:00"
     */
    function formatExamTime(timeString) {
        if (!timeString) return '';
        
        try {
            // 解析时间字符串
            const date = new Date(timeString.replace(/-/g, '/'));  // 兼容性处理
            if (isNaN(date.getTime())) {
                return timeString; // 如果解析失败，返回原字符串
            }
            
            const month = String(date.getMonth() + 1).padStart(2, '0');
            const day = String(date.getDate()).padStart(2, '0');
            const hour = String(date.getHours()).padStart(2, '0');
            const minute = String(date.getMinutes()).padStart(2, '0');
            
            return `${month}-${day} ${hour}:${minute}`;
        } catch (error) {
            console.warn('时间格式化失败:', timeString, error);
            return timeString;
        }
    }

    /**
     * 加载公告信息（模拟数据）
     */
    async function loadAnnouncements() {
        try {
            // 尝试从API加载公告
            // return await apiRequest('/exam/api/announcements');
            
            // 暂时使用模拟数据
            return [
                {
                    id: 1,
                    title: "重要通知：考试系统维护",
                    content: "系统将在本周日凌晨2:00-4:00进行维护，请合理安排考试时间。",
                    publishTime: new Date(Date.now() - 2 * 24 * 60 * 60 * 1000),
                    priority: "high"
                },
                {
                    id: 2,
                    title: "考试规则提醒",
                    content: "请考生严格遵守考试纪律，诚信考试，违规行为将被记录。",
                    publishTime: new Date(Date.now() - 5 * 24 * 60 * 60 * 1000),
                    priority: "normal"
                },
                {
                    id: 3,
                    title: "技术支持联系方式",
                    content: "如遇技术问题，请联系技术支持：400-123-4567",
                    publishTime: new Date(Date.now() - 7 * 24 * 60 * 60 * 1000),
                    priority: "normal"
                }
            ];
        } catch (error) {
            console.warn('加载公告信息失败:', error);
            return [];
        }
    }
    

    
    /**
     * 显示错误信息
     * 参考login.js中的错误显示方式
     */
    function showError(message) {
        showLoading(false);
        
        const errorHTML = `
            <div class="exam-error">
                <div class="error-content">
                    <div class="error-icon">
                        <i class="fa fa-exclamation-triangle"></i>
                    </div>
                    <h3>🚫 无法加载考试应用</h3>
                    <p>${message}</p>
                    <div class="error-actions">
                        <button onclick="location.reload()" class="btn btn-primary">
                            <i class="fa fa-refresh"></i> 重新加载
                        </button>
                        <a href="/${window.tenantId}/exam" class="btn btn-secondary">
                            <i class="fa fa-arrow-left"></i> 返回考试首页
                        </a>
                    </div>
                    <div class="error-tips">
                        <p>💡 遇到问题？</p>
                        <ul>
                            <li>检查网络连接是否正常</li>
                            <li>确认您有考试系统的访问权限</li>
                            <li>联系管理员获取帮助</li>
                        </ul>
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
            
            // 构建AMIS配置
            const amisConfig = buildPageConfig();
            
            // 初始化AMIS
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
            
            // 加载数据
            await loadInitialData();
            
            // 设置定时更新时间显示
            setInterval(() => {
                if (amisInstance) {
                    amisInstance.updateProps({
                        data: {
                            now: new Date()
                        }
                    });
                }
            }, 30000); // 每30秒更新一次时间
            
            // 增强导航菜单动画效果
            enhanceNavigationAnimations();
            
            // 显示功能提示
            showFeatureTip();
            
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