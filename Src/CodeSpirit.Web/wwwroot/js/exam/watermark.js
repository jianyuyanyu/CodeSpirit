/**
 * 水印管理器 - 独立模块
 * 负责在线考试系统的全局水印创建、管理和性能监控
 * @module WatermarkManager
 * @version 1.0.0
 */
(function(global) {
    'use strict';

    // 性能监控对象
    const PerformanceMonitor = {
        startTime: null,
        endTime: null,
        
        start: function(label) {
            this.startTime = performance.now();
            console.log(`[水印性能] ${label || '开始'} - 时间戳: ${this.startTime.toFixed(2)}ms`);
        },
        
        end: function(label) {
            this.endTime = performance.now();
            const duration = this.endTime - this.startTime;
            console.log(`[水印性能] ${label || '完成'} - 耗时: ${duration.toFixed(2)}ms`);
            return duration;
        },
        
        mark: function(label) {
            const currentTime = performance.now();
            const sinceStart = this.startTime ? currentTime - this.startTime : 0;
            console.log(`[水印性能] ${label} - 距离开始: ${sinceStart.toFixed(2)}ms`);
            return currentTime;
        }
    };

    // 优化后的全局水印管理器
    const WatermarkManager = {
        // 水印状态管理
        state: {
            isCreating: false,
            isCreated: false,
            currentText: '',
            cachedImage: null,
            observer: null,
            retryCount: 0,
            maxRetries: 5,
            initStartTime: null,
            totalInitTime: 0
        },
        
        // 防抖定时器
        debounceTimer: null,
        
        // 配置选项
        config: {
            canvasWidth: 300,
            canvasHeight: 150,
            fontSize: '16px',
            fontFamily: 'Arial, sans-serif',
            fillStyle: 'rgba(0, 0, 0, 0.1)',
            rotationAngle: -Math.PI / 6, // -30度
            debounceDelay: 100,
            retryDelay: 1000
        },
        
        // 初始化方法
        init: function() {
            PerformanceMonitor.start('水印管理器初始化');
            this.state.initStartTime = performance.now();
            
            console.log('[水印管理器] 开始初始化...');
            
            // 设置页面卸载时的清理
            this.setupCleanup();
            
            PerformanceMonitor.end('水印管理器初始化');
            console.log('[水印管理器] 初始化完成');
            
            return this;
        },
        
        // 生成水印图片（带缓存和性能监控）
        generateWatermarkImage: function(text) {
            const cacheCheckStart = performance.now();
            
            // 如果文本相同且已有缓存，直接返回
            if (this.state.cachedImage && this.state.currentText === text) {
                const cacheCheckTime = performance.now() - cacheCheckStart;
                console.log(`[水印性能] 缓存命中 - 耗时: ${cacheCheckTime.toFixed(2)}ms`);
                return this.state.cachedImage;
            }
            
            PerformanceMonitor.start('水印图片生成');
            
            // 创建水印画布
            const canvas = document.createElement('canvas');
            const ctx = canvas.getContext('2d');
            
            PerformanceMonitor.mark('Canvas创建完成');
            
            // 设置画布尺寸
            canvas.width = this.config.canvasWidth;
            canvas.height = this.config.canvasHeight;
            
            // 设置字体样式
            ctx.font = `${this.config.fontSize} ${this.config.fontFamily}`;
            ctx.fillStyle = this.config.fillStyle;
            ctx.textAlign = 'center';
            ctx.textBaseline = 'middle';
            
            PerformanceMonitor.mark('字体样式设置完成');
            
            // 旋转画布
            ctx.translate(this.config.canvasWidth / 2, this.config.canvasHeight / 2);
            ctx.rotate(this.config.rotationAngle);
            
            PerformanceMonitor.mark('画布变换完成');
            
            // 绘制水印文本
            ctx.fillText(text, 0, 0);
            
            PerformanceMonitor.mark('文本绘制完成');
            
            // 缓存图片
            const watermarkImage = canvas.toDataURL();
            this.state.cachedImage = watermarkImage;
            this.state.currentText = text;
            
            PerformanceMonitor.mark('图片转换完成');
            
            const totalTime = PerformanceMonitor.end('水印图片生成');
            console.log(`[水印性能] 图片生成统计 - 文本长度: ${text.length}, 总耗时: ${totalTime.toFixed(2)}ms`);
            
            return watermarkImage;
        },
        
        // 创建水印DOM元素
        createWatermarkElement: function(watermarkImage) {
            PerformanceMonitor.start('DOM元素创建');
            
            const watermarkContainer = document.createElement('div');
            watermarkContainer.id = 'global-watermark';
            
            // 使用对象形式设置样式，性能更好
            const styles = {
                position: 'fixed',
                top: '0',
                left: '0',
                width: '100vw',
                height: '100vh',
                pointerEvents: 'none',
                zIndex: '9999',
                overflow: 'hidden',
                userSelect: 'none',
                backgroundImage: `url(${watermarkImage})`,
                backgroundRepeat: 'repeat',
                backgroundSize: `${this.config.canvasWidth}px ${this.config.canvasHeight}px`
            };
            
            Object.assign(watermarkContainer.style, styles);
            
            PerformanceMonitor.mark('样式设置完成');
            
            // 防止水印被修改
            Object.defineProperty(watermarkContainer.style, 'display', {
                value: 'block',
                writable: false,
                configurable: false
            });
            
            PerformanceMonitor.end('DOM元素创建');
            
            return watermarkContainer;
        },
        
        // 设置监听器
        setupObserver: function() {
            PerformanceMonitor.start('监听器设置');
            
            // 如果已经有监听器，先清除
            if (this.state.observer) {
                this.state.observer.disconnect();
            }
            
            // 创建新的监听器，使用防抖机制
            this.state.observer = new MutationObserver((mutations) => {
                let needsRecreate = false;
                
                for (const mutation of mutations) {
                    if (mutation.type === 'childList') {
                        for (const node of mutation.removedNodes) {
                            if (node.id === 'global-watermark') {
                                needsRecreate = true;
                                break;
                            }
                        }
                        if (needsRecreate) break;
                    }
                }
                
                if (needsRecreate && !this.state.isCreating) {
                    console.log('[水印] 检测到水印被移除，准备重新创建...');
                    this.debouncedCreate();
                }
            });
            
            // 只监听document.body的直接子元素变化，减少性能开销
            this.state.observer.observe(document.body, {
                childList: true,
                subtree: false // 不监听子树，提高性能
            });
            
            PerformanceMonitor.end('监听器设置');
        },
        
        // 防抖创建水印
        debouncedCreate: function() {
            // 清除之前的定时器
            if (this.debounceTimer) {
                clearTimeout(this.debounceTimer);
            }
            
            // 设置新的定时器
            this.debounceTimer = setTimeout(() => {
                this.createWatermark();
            }, this.config.debounceDelay);
        },
        
        // 获取水印数据
        getWatermarkData: function() {
            const examName = global.globalData?.exam?.name || '';
            const studentName = global.globalData?.profile?.name || '';
            const idNo = global.globalData?.profile?.idNo || '';
            
            return {
                examName,
                studentName,
                idNo,
                isComplete: !!(examName && studentName && idNo)
            };
        },
        
        // 主要的创建水印方法
        createWatermark: function() {
            const overallStart = performance.now();
            PerformanceMonitor.start('完整水印创建流程');
            
            try {
                // 防止重复创建
                if (this.state.isCreating) {
                    console.log('[水印] 正在创建中，跳过重复调用');
                    return;
                }
                
                PerformanceMonitor.mark('开始数据获取');
                
                // 获取水印数据
                const data = this.getWatermarkData();
                
                // 如果关键信息还未加载完成，进行有限次数的重试
                if (!data.isComplete) {
                    if (this.state.retryCount < this.state.maxRetries) {
                        this.state.retryCount++;
                        console.log(`[水印] 等待数据加载完成... (${this.state.retryCount}/${this.state.maxRetries})`);
                        setTimeout(() => this.createWatermark(), this.config.retryDelay);
                    } else {
                        console.warn('[水印] 达到最大重试次数，停止创建水印');
                        console.warn('[水印] 缺失数据:', {
                            examName: data.examName,
                            studentName: data.studentName,
                            idNo: data.idNo
                        });
                    }
                    return;
                }
                
                // 重置重试计数
                this.state.retryCount = 0;
                this.state.isCreating = true;
                
                PerformanceMonitor.mark('数据获取完成');
                
                // 生成水印文本
                const watermarkText = `${data.examName}-${data.studentName}-${data.idNo}`;
                
                // 如果水印文本没有变化且已经存在，则不需要重新创建
                const existingWatermark = document.getElementById('global-watermark');
                if (existingWatermark && this.state.currentText === watermarkText && this.state.isCreated) {
                    console.log('[水印] 水印已存在且内容未变化，跳过创建');
                    this.state.isCreating = false;
                    PerformanceMonitor.end('完整水印创建流程');
                    return;
                }
                
                console.log('[水印] 生成水印文本：', watermarkText);
                
                PerformanceMonitor.mark('开始移除现有水印');
                
                // 移除现有水印
                if (existingWatermark) {
                    existingWatermark.remove();
                }
                
                PerformanceMonitor.mark('现有水印移除完成');
                
                // 使用RequestAnimationFrame优化DOM操作
                requestAnimationFrame(() => {
                    const domOperationStart = performance.now();
                    
                    try {
                        PerformanceMonitor.mark('开始生成水印图片');
                        
                        // 生成水印图片（使用缓存）
                        const watermarkImage = this.generateWatermarkImage(watermarkText);
                        
                        PerformanceMonitor.mark('开始创建DOM元素');
                        
                        // 创建水印元素
                        const watermarkContainer = this.createWatermarkElement(watermarkImage);
                        
                        PerformanceMonitor.mark('开始添加到页面');
                        
                        // 将水印添加到页面
                        document.body.appendChild(watermarkContainer);
                        
                        PerformanceMonitor.mark('开始设置监听器');
                        
                        // 设置监听器
                        this.setupObserver();
                        
                        // 更新状态
                        this.state.isCreated = true;
                        this.state.isCreating = false;
                        
                        // 计算总的初始化时间
                        if (this.state.initStartTime) {
                            this.state.totalInitTime = performance.now() - this.state.initStartTime;
                        }
                        
                        const domOperationTime = performance.now() - domOperationStart;
                        const overallTime = performance.now() - overallStart;
                        
                        PerformanceMonitor.end('完整水印创建流程');
                        
                        console.log(`[水印性能] 创建完成统计:`);
                        console.log(`  - DOM操作耗时: ${domOperationTime.toFixed(2)}ms`);
                        console.log(`  - 总体耗时: ${overallTime.toFixed(2)}ms`);
                        console.log(`  - 从初始化到完成: ${this.state.totalInitTime.toFixed(2)}ms`);
                        console.log(`  - 水印文本: ${watermarkText}`);
                        console.log('[水印] 水印创建完成');
                        
                    } catch (error) {
                        console.error('[水印] DOM操作时出错：', error);
                        this.state.isCreating = false;
                    }
                });
                
            } catch (error) {
                console.error('[水印] 创建水印时出错：', error);
                this.state.isCreating = false;
                PerformanceMonitor.end('完整水印创建流程');
            }
        },
        
        // 设置清理机制
        setupCleanup: function() {
            // 页面卸载时清理水印资源
            global.addEventListener('beforeunload', () => {
                this.destroy();
            });
            
            // 页面隐藏时也可以选择清理（可选）
            global.addEventListener('pagehide', () => {
                this.destroy();
            });
        },
        
        // 清理资源
        destroy: function() {
            PerformanceMonitor.start('资源清理');
            
            if (this.state.observer) {
                this.state.observer.disconnect();
                this.state.observer = null;
            }
            
            if (this.debounceTimer) {
                clearTimeout(this.debounceTimer);
                this.debounceTimer = null;
            }
            
            const watermark = document.getElementById('global-watermark');
            if (watermark) {
                watermark.remove();
            }
            
            // 重置状态
            this.state = {
                isCreating: false,
                isCreated: false,
                currentText: '',
                cachedImage: null,
                observer: null,
                retryCount: 0,
                maxRetries: 5,
                initStartTime: null,
                totalInitTime: 0
            };
            
            PerformanceMonitor.end('资源清理');
            console.log('[水印管理器] 资源清理完成');
        },
        
        // 获取性能统计信息
        getPerformanceStats: function() {
            return {
                isCreated: this.state.isCreated,
                isCreating: this.state.isCreating,
                currentText: this.state.currentText,
                totalInitTime: this.state.totalInitTime,
                retryCount: this.state.retryCount,
                hasCachedImage: !!this.state.cachedImage
            };
        }
    };

    // 创建水印的便捷方法
    function createWatermark() {
        WatermarkManager.createWatermark();
    }

    // 优化后的初始化水印函数
    function initializeWatermark() {
        console.log('[水印] 开始初始化水印系统...');
        const initStart = performance.now();
        
        // 使用防抖机制，避免多次调用
        WatermarkManager.debouncedCreate();
        
        const initTime = performance.now() - initStart;
        console.log(`[水印性能] 初始化调用完成 - 耗时: ${initTime.toFixed(2)}ms`);
    }

    // 暴露到全局作用域
    global.WatermarkManager = WatermarkManager.init();
    global.createWatermark = createWatermark;
    global.initializeWatermark = initializeWatermark;
    
    // 性能监控也暴露到全局，供调试使用
    global.WatermarkPerformanceMonitor = PerformanceMonitor;
    
    console.log('[水印模块] 水印管理器已加载完成');

})(window);