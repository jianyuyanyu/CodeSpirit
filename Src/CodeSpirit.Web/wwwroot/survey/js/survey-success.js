/**
 * 问卷提交成功页面脚本
 * 处理成功页面的交互效果和动画
 */

// 页面加载完成后的初始化
document.addEventListener('DOMContentLoaded', function() {
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
            completionTimeElement.textContent = timeText;
        }
        localStorage.removeItem('survey-start-time');
    }
    
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
});
