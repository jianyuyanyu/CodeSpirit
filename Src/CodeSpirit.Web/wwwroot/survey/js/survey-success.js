/**
 * 问卷提交成功页面脚本
 * 处理成功页面的交互效果和动画
 */

// 页面加载完成后的初始化
document.addEventListener('DOMContentLoaded', function() {
    // 添加页面加载动画
    document.body.style.opacity = '0';
    document.body.style.transition = 'opacity 0.5s ease-in-out';
    
    setTimeout(() => {
        document.body.style.opacity = '1';
    }, 100);
    
    // 模拟完成时间计算
    const completionTime = localStorage.getItem('survey-start-time');
    if (completionTime) {
        const startTime = parseInt(completionTime);
        const endTime = Date.now();
        const duration = Math.round((endTime - startTime) / 1000);
        
        const minutes = Math.floor(duration / 60);
        const seconds = duration % 60;
        const timeText = minutes > 0 ? `${minutes}分${seconds}秒` : `${seconds}秒`;
        
        const completionTimeElement = document.getElementById('completion-time');
        if (completionTimeElement) {
            // 添加数字动画效果
            animateNumber(completionTimeElement, timeText);
        }
        localStorage.removeItem('survey-start-time');
    }
    
    // 添加统计数字动画
    animateStatNumbers();
    
    // 添加成功音效（可选）
    try {
        // 创建音频上下文
        const audioContext = new (window.AudioContext || window.webkitAudioContext)();
        
        // 播放成功提示音
        function playSuccessSound() {
            const oscillator = audioContext.createOscillator();
            const gainNode = audioContext.createGain();
            
            oscillator.connect(gainNode);
            gainNode.connect(audioContext.destination);
            
            oscillator.frequency.setValueAtTime(523.25, audioContext.currentTime); // C5
            oscillator.frequency.setValueAtTime(659.25, audioContext.currentTime + 0.1); // E5
            oscillator.frequency.setValueAtTime(783.99, audioContext.currentTime + 0.2); // G5
            
            gainNode.gain.setValueAtTime(0.1, audioContext.currentTime);
            gainNode.gain.exponentialRampToValueAtTime(0.01, audioContext.currentTime + 0.5);
            
            oscillator.start(audioContext.currentTime);
            oscillator.stop(audioContext.currentTime + 0.5);
        }
        
        // 延迟播放音效
        setTimeout(playSuccessSound, 800);
    } catch (error) {
        // 忽略音频错误
        console.log('音频播放不可用');
    }
    
    // 添加按钮点击动画
    const buttons = document.querySelectorAll('.survey-success-btn');
    buttons.forEach(button => {
        button.addEventListener('click', function(e) {
            // 创建点击波纹效果
            const ripple = document.createElement('span');
            const rect = this.getBoundingClientRect();
            const size = Math.max(rect.width, rect.height);
            const x = e.clientX - rect.left - size / 2;
            const y = e.clientY - rect.top - size / 2;
            
            ripple.style.cssText = `
                position: absolute;
                width: ${size}px;
                height: ${size}px;
                left: ${x}px;
                top: ${y}px;
                background: rgba(255,255,255,0.3);
                border-radius: 50%;
                transform: scale(0);
                animation: buttonRipple 0.6s ease-out;
                pointer-events: none;
            `;
            
            this.style.position = 'relative';
            this.style.overflow = 'hidden';
            this.appendChild(ripple);
            
            setTimeout(() => {
                ripple.remove();
            }, 600);
        });
    });
    
    // 添加卡片悬停效果
    const successCard = document.querySelector('.survey-success-card');
    if (successCard) {
        successCard.addEventListener('mouseenter', function() {
            this.style.transform = 'translateY(-5px) scale(1.02)';
        });
        
        successCard.addEventListener('mouseleave', function() {
            this.style.transform = 'translateY(0) scale(1)';
        });
    }
    
    // 添加粒子点击效果
    const particles = document.querySelectorAll('.particle');
    particles.forEach(particle => {
        particle.addEventListener('click', function() {
            createClickEffect(this);
        });
    });
});

/**
 * 数字动画效果
 * @param {HTMLElement} element - 目标元素
 * @param {string} finalText - 最终文本
 */
function animateNumber(element, finalText) {
    element.textContent = '0秒';
    
    setTimeout(() => {
        element.style.transition = 'all 0.5s ease-out';
        element.textContent = finalText;
        element.style.transform = 'scale(1.1)';
        
        setTimeout(() => {
            element.style.transform = 'scale(1)';
        }, 200);
    }, 800);
}

/**
 * 统计数字动画
 */
function animateStatNumbers() {
    const statNumbers = document.querySelectorAll('.stat-number');
    
    statNumbers.forEach((element, index) => {
        if (element.textContent === '+1') {
            setTimeout(() => {
                let count = 0;
                const interval = setInterval(() => {
                    count++;
                    element.textContent = `+${count}`;
                    
                    if (count >= 1) {
                        clearInterval(interval);
                        element.style.transform = 'scale(1.1)';
                        setTimeout(() => {
                            element.style.transform = 'scale(1)';
                        }, 200);
                    }
                }, 100);
            }, 1200 + index * 200);
        }
    });
}

/**
 * 创建点击效果
 * @param {HTMLElement} element - 被点击的元素
 */
function createClickEffect(element) {
    const rect = element.getBoundingClientRect();
    const effect = document.createElement('div');
    
    effect.style.cssText = `
        position: fixed;
        left: ${rect.left + rect.width / 2}px;
        top: ${rect.top + rect.height / 2}px;
        width: 20px;
        height: 20px;
        background: radial-gradient(circle, rgba(255,255,255,0.8) 0%, transparent 70%);
        border-radius: 50%;
        pointer-events: none;
        z-index: 9999;
        animation: clickExpand 0.6s ease-out forwards;
    `;
    
    document.body.appendChild(effect);
    
    setTimeout(() => {
        effect.remove();
    }, 600);
}

// 添加点击扩散动画
const style = document.createElement('style');
style.textContent = `
    @keyframes clickExpand {
        0% {
            transform: translate(-50%, -50%) scale(0);
            opacity: 1;
        }
        100% {
            transform: translate(-50%, -50%) scale(3);
            opacity: 0;
        }
    }
`;
document.head.appendChild(style);
