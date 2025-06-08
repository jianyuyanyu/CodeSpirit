/**
 * 屏幕切换检测器
 * 用于监控考试过程中的屏幕切换和焦点丢失等异常行为
 */

(function(window) {
    'use strict';

    /**
     * 屏幕切换检测器类
     */
    class ScreenSwitchDetector {
        constructor(options = {}) {
            this.options = {
                // 是否启用检测
                enabled: true,
                // 检测间隔（毫秒）
                interval: 1000,
                // 警告阈值（次数）
                warningThreshold: 3,
                // 是否自动提交
                autoSubmit: false,
                // 自动提交阈值
                autoSubmitThreshold: 5,
                // 回调函数
                onSwitch: null,
                onWarning: null,
                onAutoSubmit: null,
                // Debug模式
                debug: false,
                ...options
            };

            this.switchCount = 0;
            this.isActive = false;
            this.lastActiveTime = Date.now();
            this.detectionTimer = null;
            this.events = [];

            this.init();
        }

        /**
         * 初始化检测器
         */
        init() {
            if (!this.options.enabled) {
                console.log('📊 屏幕切换检测器已禁用');
                return;
            }

            this.bindEvents();
            this.startDetection();
            
            if (this.options.debug) {
                console.log('🔍 屏幕切换检测器已启动', this.options);
            }
        }

        /**
         * 绑定事件监听器
         */
        bindEvents() {
            // 页面可见性API
            document.addEventListener('visibilitychange', () => {
                this.handleVisibilityChange();
            });

            // 窗口焦点事件
            window.addEventListener('focus', () => {
                this.handleWindowFocus();
            });

            window.addEventListener('blur', () => {
                this.handleWindowBlur();
            });

            // 鼠标离开/进入窗口
            document.addEventListener('mouseleave', () => {
                this.handleMouseLeave();
            });

            document.addEventListener('mouseenter', () => {
                this.handleMouseEnter();
            });

            // 键盘事件（Alt+Tab检测）
            document.addEventListener('keydown', (e) => {
                this.handleKeyDown(e);
            });

            // 页面卸载前
            window.addEventListener('beforeunload', () => {
                this.handleBeforeUnload();
            });

            // 页面隐藏前
            document.addEventListener('pagehide', () => {
                this.handlePageHide();
            });
        }

        /**
         * 开始检测
         */
        startDetection() {
            if (this.detectionTimer) {
                clearInterval(this.detectionTimer);
            }

            this.detectionTimer = setInterval(() => {
                this.performDetection();
            }, this.options.interval);

            this.isActive = true;
            this.lastActiveTime = Date.now();
        }

        /**
         * 停止检测
         */
        stopDetection() {
            if (this.detectionTimer) {
                clearInterval(this.detectionTimer);
                this.detectionTimer = null;
            }
            this.isActive = false;
        }

        /**
         * 执行检测逻辑
         */
        performDetection() {
            const now = Date.now();
            const timeSinceLastActive = now - this.lastActiveTime;

            // 检查页面是否处于非活动状态过长时间
            if (timeSinceLastActive > 30000) { // 30秒
                this.recordSuspiciousActivity('长时间非活动状态', {
                    duration: timeSinceLastActive,
                    timestamp: now
                });
            }

            // 检查窗口尺寸变化（可能的全屏切换）
            this.checkWindowSizeChange();
        }

        /**
         * 处理页面可见性变化
         */
        handleVisibilityChange() {
            const isHidden = document.hidden;
            const timestamp = Date.now();

            if (isHidden) {
                this.recordSwitchEvent('页面隐藏', timestamp);
                this.logDebug('📱 页面已隐藏');
            } else {
                this.recordSwitchEvent('页面显示', timestamp);
                this.lastActiveTime = timestamp;
                this.logDebug('📱 页面已显示');
            }
        }

        /**
         * 处理窗口获得焦点
         */
        handleWindowFocus() {
            const timestamp = Date.now();
            this.recordSwitchEvent('窗口获得焦点', timestamp);
            this.lastActiveTime = timestamp;
            this.logDebug('🔍 窗口获得焦点');
        }

        /**
         * 处理窗口失去焦点
         */
        handleWindowBlur() {
            const timestamp = Date.now();
            this.recordSwitchEvent('窗口失去焦点', timestamp);
            this.logDebug('🔍 窗口失去焦点');
        }

        /**
         * 处理鼠标离开
         */
        handleMouseLeave() {
            const timestamp = Date.now();
            this.recordSuspiciousActivity('鼠标离开窗口', { timestamp });
            this.logDebug('🖱️ 鼠标离开窗口');
        }

        /**
         * 处理鼠标进入
         */
        handleMouseEnter() {
            const timestamp = Date.now();
            this.lastActiveTime = timestamp;
            this.logDebug('🖱️ 鼠标进入窗口');
        }

        /**
         * 处理键盘事件
         */
        handleKeyDown(e) {
            const timestamp = Date.now();
            
            // Alt+Tab 检测
            if (e.altKey && e.keyCode === 9) {
                this.recordSwitchEvent('Alt+Tab切换', timestamp);
                this.logDebug('⌨️ 检测到Alt+Tab');
            }

            // Windows键
            if (e.keyCode === 91 || e.keyCode === 92) {
                this.recordSuspiciousActivity('Windows键按下', { timestamp, keyCode: e.keyCode });
                this.logDebug('⌨️ 检测到Windows键');
            }

            // Ctrl+Alt+Del (某些情况下能检测到)
            if (e.ctrlKey && e.altKey && e.keyCode === 46) {
                this.recordSuspiciousActivity('Ctrl+Alt+Del', { timestamp });
                this.logDebug('⌨️ 检测到Ctrl+Alt+Del');
            }

            this.lastActiveTime = timestamp;
        }

        /**
         * 处理页面卸载前
         */
        handleBeforeUnload() {
            this.recordSwitchEvent('页面即将卸载', Date.now());
            this.logDebug('📄 页面即将卸载');
        }

        /**
         * 处理页面隐藏
         */
        handlePageHide() {
            this.recordSwitchEvent('页面隐藏事件', Date.now());
            this.logDebug('📄 页面隐藏事件');
        }

        /**
         * 检查窗口尺寸变化
         */
        checkWindowSizeChange() {
            const currentSize = {
                width: window.innerWidth,
                height: window.innerHeight,
                outerWidth: window.outerWidth,
                outerHeight: window.outerHeight
            };

            if (!this.lastWindowSize) {
                this.lastWindowSize = currentSize;
                return;
            }

            const sizeChanged = 
                this.lastWindowSize.width !== currentSize.width ||
                this.lastWindowSize.height !== currentSize.height;

            if (sizeChanged) {
                this.recordSuspiciousActivity('窗口尺寸变化', {
                    before: this.lastWindowSize,
                    after: currentSize,
                    timestamp: Date.now()
                });
                this.logDebug('📐 窗口尺寸发生变化');
            }

            this.lastWindowSize = currentSize;
        }

        /**
         * 记录切换事件
         */
        recordSwitchEvent(type, timestamp) {
            this.switchCount++;
            
            const event = {
                type: 'switch',
                subType: type,
                timestamp: timestamp,
                count: this.switchCount
            };

            this.events.push(event);
            this.checkThresholds();

            // 触发回调
            if (typeof this.options.onSwitch === 'function') {
                this.options.onSwitch(event, this.switchCount);
            }

            console.warn(`⚠️ 屏幕切换检测: ${type} (第${this.switchCount}次)`);
        }

        /**
         * 记录可疑活动
         */
        recordSuspiciousActivity(type, data) {
            const event = {
                type: 'suspicious',
                subType: type,
                data: data,
                timestamp: Date.now()
            };

            this.events.push(event);
            this.logDebug(`🚨 可疑活动: ${type}`, data);
        }

        /**
         * 检查阈值
         */
        checkThresholds() {
            // 警告阈值
            if (this.switchCount === this.options.warningThreshold) {
                this.triggerWarning();
            }

            // 自动提交阈值
            if (this.options.autoSubmit && this.switchCount >= this.options.autoSubmitThreshold) {
                this.triggerAutoSubmit();
            }
        }

        /**
         * 触发警告
         */
        triggerWarning() {
            const message = `您已经切换屏幕${this.switchCount}次！请专注于考试，避免切换到其他应用程序。`;
            
            if (typeof this.options.onWarning === 'function') {
                this.options.onWarning(this.switchCount, message);
            } else {
                alert(message);
            }

            console.warn('⚠️ 触发切换警告', { count: this.switchCount });
        }

        /**
         * 触发自动提交
         */
        triggerAutoSubmit() {
            const message = `检测到过多的屏幕切换行为，系统将自动提交您的考试。`;
            
            if (typeof this.options.onAutoSubmit === 'function') {
                this.options.onAutoSubmit(this.switchCount, message);
            } else {
                alert(message);
                // 这里可以调用考试提交逻辑
            }

            console.error('🚨 触发自动提交', { count: this.switchCount });
        }

        /**
         * 获取统计信息
         */
        getStatistics() {
            return {
                switchCount: this.switchCount,
                events: this.events,
                isActive: this.isActive,
                lastActiveTime: this.lastActiveTime,
                startTime: this.startTime || Date.now()
            };
        }

        /**
         * 重置计数器
         */
        reset() {
            this.switchCount = 0;
            this.events = [];
            this.lastActiveTime = Date.now();
            this.logDebug('🔄 检测器已重置');
        }

        /**
         * 销毁检测器
         */
        destroy() {
            this.stopDetection();
            // 这里可以移除事件监听器，但由于它们绑定在document和window上，
            // 在页面卸载时会自动清理，所以暂时不处理
            this.logDebug('💥 检测器已销毁');
        }

        /**
         * Debug日志
         */
        logDebug(message, data = null) {
            if (this.options.debug) {
                if (data) {
                    console.log(message, data);
                } else {
                    console.log(message);
                }
            }
        }
    }

    // 导出到全局
    window.ScreenSwitchDetector = ScreenSwitchDetector;

    // 如果有配置，自动初始化
    if (window.CS_CONFIG && window.CS_CONFIG.security && window.CS_CONFIG.security.enableScreenSwitchDetection) {
        window.addEventListener('DOMContentLoaded', () => {
            const detectorOptions = {
                enabled: true,
                debug: window.CS_CONFIG.isDevelopment,
                onSwitch: (event, count) => {
                    // 可以在这里发送统计数据到服务器
                    console.log('屏幕切换事件', { event, count });
                },
                onWarning: (count, message) => {
                    // 使用更友好的提示方式
                    if (window.amis && window.amis.toast) {
                        window.amis.toast.warning(message);
                    } else {
                        alert(message);
                    }
                },
                onAutoSubmit: (count, message) => {
                    // 自动提交逻辑
                    if (window.amis && window.amis.toast) {
                        window.amis.toast.error(message);
                    } else {
                        alert(message);
                    }
                    
                    // 这里可以调用考试提交函数
                    if (typeof window.submitExam === 'function') {
                        window.submitExam('auto', '检测到异常切屏行为');
                    }
                }
            };

            window.screenSwitchDetector = new ScreenSwitchDetector(detectorOptions);
            console.log('🔍 屏幕切换检测器已自动初始化');
        });
    }

})(window); 