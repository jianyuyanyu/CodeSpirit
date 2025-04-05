/**
 * 切屏检测模块
 * 用于监控和记录考试过程中的切屏行为
 * @module ScreenSwitchDetector
 */
(function() {
    'use strict';
    
    // 创建命名空间
    const ScreenSwitchDetector = window.ScreenSwitchDetector = {};
    
    // 私有变量
    let isInitialized = false;
    let notificationTimeout = null;
    
    // 配置项
    const CONFIG = {
        warningDuration: 5000,      // 警告显示时间(毫秒)
        fadeOutDuration: 300,       // 淡出动画时间(毫秒)
        retryDelay: 3000,           // 初始化重试延迟(毫秒)
        initialWarningDelay: 5000   // 初始警告显示延迟(毫秒)
    };
    
    /**
     * 创建并显示通知
     * @param {string} title - 通知标题
     * @param {string} message - 通知消息
     * @param {string} type - 通知类型 (warning|error|info)
     * @param {number} [duration=5000] - 显示时间(毫秒)
     * @returns {HTMLElement} 创建的通知元素
     * @private
     */
    function createNotification(title, message, type = 'warning', duration = CONFIG.warningDuration) {
        try {
            // 清除现有通知超时
            if (notificationTimeout) {
                clearTimeout(notificationTimeout);
                notificationTimeout = null;
            }
            
            // 移除所有现有的通知
            const existingNotifications = document.querySelectorAll('.custom-notification');
            existingNotifications.forEach(notification => {
                notification.classList.add('fade-out');
                setTimeout(() => {
                    if (document.body.contains(notification)) {
                        document.body.removeChild(notification);
                    }
                }, CONFIG.fadeOutDuration);
            });
            
            // 创建新的通知元素
            const notification = document.createElement('div');
            notification.className = `custom-notification ${type}`;
            notification.innerHTML = `
                <div class="notification-title">${title}</div>
                <div class="notification-body">${message}</div>
            `;
            
            // 添加到页面
            document.body.appendChild(notification);
            
            // 添加显示动画
            setTimeout(() => notification.classList.add('show'), 10);
            
            // 设置自动关闭
            if (duration > 0) {
                notificationTimeout = setTimeout(() => {
                    notification.classList.remove('show');
                    notification.classList.add('fade-out');
                    
                    setTimeout(() => {
                        if (document.body.contains(notification)) {
                            document.body.removeChild(notification);
                        }
                    }, CONFIG.fadeOutDuration);
                }, duration);
            }
            
            return notification;
        } catch (error) {
            console.error("[通知系统] 创建通知失败:", error);
            return null;
        }
    }
    
    /**
     * 更新切屏次数的DOM显示
     * @private
     */
    function updateScreenSwitchCountDisplay() {
        const count = window.globalData?.exam?.screenSwitchCount || 0;
        const allowedCount = window.globalData?.exam?.allowedScreenSwitchCount || 0;
        
        // 更新当前切屏次数显示
        const countElements = document.querySelectorAll('.screen-switch-value');
        if (countElements && countElements.length > 0) {
            countElements.forEach(el => el.textContent = String(count));
        }
        
        // 更新允许切屏次数显示
        const allowedElements = document.querySelectorAll('.allowed-switch-value');
        if (allowedElements && allowedElements.length > 0) {
            allowedElements.forEach(el => el.textContent = String(allowedCount));
        }
        
        // 根据切屏情况更新图标状态
        const iconElements = document.querySelectorAll('.screen-switch-icon');
        if (iconElements && iconElements.length > 0) {
            iconElements.forEach(el => {
                // 移除所有状态类
                el.classList.remove('warning', 'danger', 'normal');
                
                // 设置当前状态
                if (allowedCount > 0) {
                    if (count > allowedCount) {
                        el.classList.add('danger');
                        el.setAttribute('title', `警告：已超出允许的切屏次数(${allowedCount}次)`);
                    } else if (count > (allowedCount * 0.7)) {
                        el.classList.add('warning');
                        el.setAttribute('title', `注意：您的切屏次数已接近限制(${count}/${allowedCount})`);
                    } else {
                        el.classList.add('normal');
                        el.setAttribute('title', `当前切屏次数/允许次数：${count}/${allowedCount}`);
                    }
                } else {
                    if (count > 3) {
                        el.classList.add('warning');
                        el.setAttribute('title', `警告：频繁切屏可能会被判定为作弊行为(${count}次)`);
                    } else {
                        el.classList.add('normal');
                        el.setAttribute('title', `当前切屏次数：${count}次`);
                    }
                }
            });
        }
    }
    
    /**
     * 显示切屏警告
     * @param {string} message - 警告消息文本
     * @returns {boolean} - 操作是否成功
     * @public
     */
    ScreenSwitchDetector.showWarning = function(message) {
        try {
            // 默认消息
            const warningMessage = message || "警告：系统已记录您的切屏行为！频繁切屏可能会被判定为作弊行为。";
            
            // 创建警告通知
            createNotification('切屏警告', warningMessage, 'warning');
            return true;
        } catch (error) {
            console.error("[切屏警告] 显示警告时出错:", error);
            return false;
        }
    };
    
    /**
     * 记录切屏事件并发送到服务器
     * @returns {Promise<boolean>} 操作是否成功的Promise
     * @public
     */
    ScreenSwitchDetector.recordScreenSwitch = function() {
        return new Promise(async (resolve) => {
            try {
                console.log("[切屏检测] 检测到切屏行为");
                
                // 获取记录ID - 从全局变量获取
                const recordId = window.globalData?.exam?.recordId;
                if (!recordId) {
                    console.error("[切屏检测] 无法获取考试记录ID");
                    
                    // 显示警告
                    ScreenSwitchDetector.showWarning("警告：系统检测到切屏行为！请勿频繁切换窗口。");
                    resolve(false);
                    return;
                }
                
                // 获取允许的切屏次数
                const allowedCount = window.globalData?.exam?.allowedScreenSwitchCount || 0;
                
                // 更新计数(使用全局变量)
                window.screenSwitchCount = (window.screenSwitchCount || 0) + 1;
                
                // 同步到globalData
                if (window.globalData && window.globalData.exam) {
                    window.globalData.exam.screenSwitchCount = window.screenSwitchCount;
                }
                
                console.log("[切屏检测] 当前切屏次数:", window.screenSwitchCount);
                
                // 更新DOM显示
                updateScreenSwitchCountDisplay();
                
                // 显示警告信息，根据切屏次数和允许次数调整内容
                if (allowedCount > 0) {
                    if (window.screenSwitchCount > allowedCount) {
                        // 超过允许次数
                        ScreenSwitchDetector.showWarning(`严重警告：您已超出允许的切屏次数(${allowedCount}次)！此行为已被记录为作弊嫌疑。`);
                    } else {
                        // 未超过允许次数
                        ScreenSwitchDetector.showWarning(`警告：系统已记录您的切屏行为！您已切屏 ${window.screenSwitchCount} 次，允许切屏 ${allowedCount} 次。`);
                    }
                } else {
                    // 没有明确的允许次数
                    ScreenSwitchDetector.showWarning();
                }
                
                try {
                    // 发送切屏记录到服务器
                    const response = await fetch(`/exam/api/exam/client/${recordId}/screen-switch`, {
                        method: 'POST',
                        headers: {
                            'Content-Type': 'application/json',
                            'Authorization': 'Bearer ' + localStorage.getItem('token'),
                            'X-Forwarded-With': 'CodeSpirit'
                        }
                    });
                    
                    if (!response.ok) {
                        throw new Error(`服务器响应错误：${response.status} ${response.statusText}`);
                    }
                    
                    const data = await response.json();
                    
                    if (data.status === 0) {
                        console.log("[切屏检测] 切屏记录已成功发送到服务器");
                        
                        // 如果服务器返回了更新后的切屏次数，更新本地计数
                        if (data.data && typeof data.data.screenSwitchCount === 'number') {
                            window.screenSwitchCount = data.data.screenSwitchCount;
                            
                            if (window.globalData && window.globalData.exam) {
                                window.globalData.exam.screenSwitchCount = data.data.screenSwitchCount;
                            }
                            
                            // 更新DOM显示
                            updateScreenSwitchCountDisplay();
                        }
                        
                        resolve(true);
                    } else {
                        console.error("[切屏检测] 记录切屏失败:", data.msg);
                        resolve(false);
                    }
                } catch (error) {
                    console.error("[切屏检测] 发送切屏记录时出错:", error);
                    resolve(false);
                }
            } catch (error) {
                console.error("[切屏检测] 记录切屏过程中发生错误:", error);
                resolve(false);
            }
        });
    };
    
    /**
     * 事件处理函数：visibilitychange事件
     * @public
     */
    ScreenSwitchDetector.handleVisibilityChange = function() {
        if (document.visibilityState === 'hidden') {
            ScreenSwitchDetector.recordScreenSwitch();
        }
    };
    
    /**
     * 事件处理函数：窗口失去焦点事件
     * @public
     */
    ScreenSwitchDetector.handleWindowBlur = function() {
        ScreenSwitchDetector.recordScreenSwitch();
    };
    
    /**
     * 初始化切屏检测
     * 设置事件监听并初始化UI
     * @public
     */
    ScreenSwitchDetector.setup = function() {
        // 避免重复初始化
        if (isInitialized) {
            console.warn("[切屏检测] 已经初始化，忽略重复调用");
            return;
        }
        
        console.log("[切屏检测] 正在设置切屏检测...");
        
        try {
            // 检查是否已有记录ID
            if (!window.globalData?.exam?.recordId) {
                console.warn("[切屏检测] 记录ID未设置，将在3秒后重试");
                setTimeout(ScreenSwitchDetector.setup, CONFIG.retryDelay);
                return;
            }
            
            // 移除可能存在的旧事件监听器
            document.removeEventListener('visibilitychange', ScreenSwitchDetector.handleVisibilityChange);
            window.removeEventListener('blur', ScreenSwitchDetector.handleWindowBlur);
            
            // 添加事件监听
            document.addEventListener('visibilitychange', ScreenSwitchDetector.handleVisibilityChange);
            window.addEventListener('blur', ScreenSwitchDetector.handleWindowBlur);
            
            // 初始化切屏次数显示
            window.screenSwitchCount = window.globalData?.exam?.screenSwitchCount || 0;
            
            // 更新DOM显示
            updateScreenSwitchCountDisplay();
            
            // 如果有允许的切屏次数，显示初始提示
            const allowedCount = window.globalData?.exam?.allowedScreenSwitchCount || 0;
            if (allowedCount > 0) {
                // 设定时间后显示切屏限制提示
                setTimeout(() => {
                    ScreenSwitchDetector.showWarning(`本次考试允许切屏 ${allowedCount} 次，超过将被记录为作弊嫌疑。当前已切屏 ${window.screenSwitchCount} 次。`);
                }, CONFIG.initialWarningDelay);
            }
            
            // 标记为已初始化
            isInitialized = true;
            
            console.log("[切屏检测] 切屏检测已成功启用");
        } catch (error) {
            console.error("[切屏检测] 设置切屏检测时发生错误:", error);
            // 初始化失败，重置标志
            isInitialized = false;
        }
    };
    
    /**
     * 创建通知的辅助函数，供其他模块调用
     * @param {string} title - 通知标题
     * @param {string} message - 通知消息
     * @param {string} type - 通知类型 (warning|error|info|success)
     * @param {number} [duration=5000] - 显示时间(毫秒)，0表示不自动关闭
     * @returns {HTMLElement} 创建的通知元素
     * @public
     */
    ScreenSwitchDetector.createNotification = function(title, message, type = 'info', duration = CONFIG.warningDuration) {
        return createNotification(title, message, type, duration);
    };
    
    // 对外暴露兼容的全局函数
    window.showScreenSwitchWarning = ScreenSwitchDetector.showWarning;
    window.recordScreenSwitch = ScreenSwitchDetector.recordScreenSwitch;
    window.setupScreenSwitchDetection = ScreenSwitchDetector.setup;
    window.handleVisibilityChange = ScreenSwitchDetector.handleVisibilityChange;
    window.handleWindowBlur = ScreenSwitchDetector.handleWindowBlur;
    window.createCustomNotification = ScreenSwitchDetector.createNotification;
    
})(); 