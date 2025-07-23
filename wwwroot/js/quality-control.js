// Quality Control System for Zela Video Call
// Manages audio/video quality with auto-adjustment and manual controls

class QualityController {
    constructor() {
        this.qualityProfiles = {
            auto: { name: 'Tự động', video: 'auto', audio: 'auto' },
            high: { 
                name: 'Cao (720p)', 
                video: { width: 1280, height: 720, frameRate: 30, bitrate: 1500 },
                audio: { bitrate: 128, sampleRate: 48000 }
            },
            medium: { 
                name: 'Trung bình (480p)', 
                video: { width: 854, height: 480, frameRate: 24, bitrate: 1000 },
                audio: { bitrate: 96, sampleRate: 44100 }
            },
            low: { 
                name: 'Thấp (360p)', 
                video: { width: 640, height: 360, frameRate: 20, bitrate: 600 },
                audio: { bitrate: 64, sampleRate: 44100 }
            },
            minimal: { 
                name: 'Tối thiểu (240p)', 
                video: { width: 426, height: 240, frameRate: 15, bitrate: 300 },
                audio: { bitrate: 32, sampleRate: 22050 }
            }
        };

        this.currentVideoQuality = 'auto';
        this.currentAudioQuality = 'auto';
        this.isAutoAdjustEnabled = true;
        this.connectionStats = {
            bandwidth: 0,
            packetLoss: 0,
            latency: 0,
            frameRate: 0,
            resolution: { width: 0, height: 0 }
        };
        
        // Quality monitoring intervals
        this.monitoringInterval = null;
        this.adjustmentCooldown = null;
        this.lastAdjustmentTime = 0;
        
        // Quality change callbacks
        this.onQualityChange = null;
        this.onStatsUpdate = null;
        
        this.initializeQualityControl();
    }

    // ======== INITIALIZATION ========
    initializeQualityControl() {
        this.createQualityControlUI();
        this.startQualityMonitoring();
        this.loadQualityPreferences();
        console.log('🎥 Quality Control System initialized');
    }

    // ======== UI CREATION ========
    createQualityControlUI() {
        console.log('🎥 Creating Quality Control UI...');
        // Get quality content container
        const qualityContent = document.getElementById('quality-content');
        if (!qualityContent) {
            console.error('Quality content container not found');
            return;
        }
        
        // Create quality control content
        qualityContent.innerHTML = `
            
            <div style="margin-bottom: 25px;">
                <label style="color: white; font-weight: 600; font-size: 16px; margin-bottom: 10px; display: block;">Chất lượng video:</label>
                <select id="video-quality-select" style="width: 100%; padding: 12px; border: 2px solid rgba(255,255,255,0.3); border-radius: 12px; background: rgba(255,255,255,0.1); color: white; font-size: 14px; backdrop-filter: blur(10px);">
                    ${this.generateQualityOptions()}
                </select>
            </div>
            
            <div style="margin-bottom: 25px;">
                <label style="color: white; font-weight: 600; font-size: 16px; margin-bottom: 10px; display: block;">Chất lượng audio:</label>
                <select id="audio-quality-select" style="width: 100%; padding: 12px; border: 2px solid rgba(255,255,255,0.3); border-radius: 12px; background: rgba(255,255,255,0.1); color: white; font-size: 14px; backdrop-filter: blur(10px);">
                    ${this.generateQualityOptions()}
                </select>
            </div>

            <div style="margin-bottom: 25px;">
                <label style="color: white; font-weight: 600; font-size: 16px; margin-bottom: 15px; display: flex; align-items: center; gap: 10px;">
                    <input type="checkbox" id="auto-adjust-checkbox" ${this.isAutoAdjustEnabled ? 'checked' : ''} style="width: 18px; height: 18px;">
                    Tự động điều chỉnh chất lượng
                </label>
                <small style="color: rgba(255,255,255,0.7); font-size: 12px;">Điều chỉnh chất lượng dựa trên kết nối mạng</small>
            </div>

            <div style="margin-bottom: 25px;">
                <label style="color: white; font-weight: 600; font-size: 16px; margin-bottom: 15px; display: block;">📊 Thống kê kết nối:</label>
                <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 15px;" id="connection-stats">
                    <div style="background: rgba(255,255,255,0.1); padding: 12px; border-radius: 8px; text-align: center;">
                        <div style="color: rgba(255,255,255,0.7); font-size: 12px;">Bandwidth</div>
                        <div style="color: white; font-weight: 600;" id="bandwidth-value">-- kbps</div>
                    </div>
                    <div style="background: rgba(255,255,255,0.1); padding: 12px; border-radius: 8px; text-align: center;">
                        <div style="color: rgba(255,255,255,0.7); font-size: 12px;">Độ trễ</div>
                        <div style="color: white; font-weight: 600;" id="latency-value">-- ms</div>
                    </div>
                    <div style="background: rgba(255,255,255,0.1); padding: 12px; border-radius: 8px; text-align: center;">
                        <div style="color: rgba(255,255,255,0.7); font-size: 12px;">Frame Rate</div>
                        <div style="color: white; font-weight: 600;" id="framerate-value">-- fps</div>
                    </div>
                    <div style="background: rgba(255,255,255,0.1); padding: 12px; border-radius: 8px; text-align: center;">
                        <div style="color: rgba(255,255,255,0.7); font-size: 12px;">Độ phân giải</div>
                        <div style="color: white; font-weight: 600;" id="resolution-value">--x--</div>
                    </div>
                </div>
            </div>

            <div style="margin-bottom: 25px;">
                <div style="background: rgba(255,255,255,0.1); padding: 20px; border-radius: 12px; text-align: center;" id="quality-indicator">
                    <div style="font-size: 24px; margin-bottom: 10px;" id="quality-icon">📶</div>
                    <div style="color: white; font-weight: 600; margin-bottom: 5px;" id="quality-status">Đang kiểm tra...</div>
                    <div style="color: rgba(255,255,255,0.7); font-size: 12px;" id="quality-detail">Vui lòng chờ</div>
                </div>
            </div>
        `;

        console.log('🎥 Quality control content created');
        this.setupQualityEventListeners();
    }

    generateQualityOptions() {
        return Object.entries(this.qualityProfiles)
            .map(([key, profile]) => `<option value="${key}">${profile.name}</option>`)
            .join('');
    }

    // ======== SIDEBAR MANAGEMENT ========
    showSidebar() {
        // Close other sidebars first
        this.closeAllSidebars();
        
        const sidebar = document.getElementById('quality-sidebar');
        const overlay = document.getElementById('sidebar-overlay');
        
        if (sidebar && overlay) {
            sidebar.classList.add('open');
            overlay.classList.add('show');
            
            // Update button state
            const toggleBtn = document.getElementById('toggle-quality');
            if (toggleBtn) {
                toggleBtn.innerHTML = '⚙️ Ẩn chất lượng';
                toggleBtn.classList.add('active');
            }
        }
    }
    
    hideSidebar() {
        const sidebar = document.getElementById('quality-sidebar');
        const overlay = document.getElementById('sidebar-overlay');
        
        if (sidebar && overlay) {
            sidebar.classList.remove('open');
            overlay.classList.remove('show');
            
            // Update button state
            const toggleBtn = document.getElementById('toggle-quality');
            if (toggleBtn) {
                toggleBtn.innerHTML = '⚙️ Chất lượng';
                toggleBtn.classList.remove('active');
            }
        }
    }
    
    closeAllSidebars() {
        // Close stats sidebar
        const statsSidebar = document.getElementById('stats-sidebar');
        if (statsSidebar) statsSidebar.classList.remove('open');
        
        // Close recording sidebar
        const recordingSidebar = document.getElementById('recording-sidebar');
        if (recordingSidebar) recordingSidebar.classList.remove('open');
        
        // Hide overlay
        const overlay = document.getElementById('sidebar-overlay');
        if (overlay) overlay.classList.remove('show');
        
        // Reset button states
        const statsBtn = document.getElementById('toggle-stats');
        if (statsBtn) {
            statsBtn.innerHTML = '📊 Xem thống kê';
            statsBtn.classList.remove('active');
        }
        
        const recordingBtn = document.getElementById('toggle-recording');
        if (recordingBtn) {
            recordingBtn.innerHTML = '🎥 Ghi hình';
            recordingBtn.classList.remove('active');
        }
    }

    // ======== EVENT LISTENERS ========
    setupQualityEventListeners() {
        // Toggle sidebar visibility
        const toggleBtn = document.getElementById('toggle-quality');
        if (toggleBtn) {
            toggleBtn.addEventListener('click', () => {
                const sidebar = document.getElementById('quality-sidebar');
                if (sidebar && sidebar.classList.contains('open')) {
                    this.hideSidebar();
                } else {
                    this.showSidebar();
                }
            });
        }
        
        // Close sidebar button
        const closeBtn = document.getElementById('close-quality');
        if (closeBtn) {
            closeBtn.addEventListener('click', () => {
                this.hideSidebar();
            });
        }
        
        // Overlay click to close
        const overlay = document.getElementById('sidebar-overlay');
        if (overlay) {
            overlay.addEventListener('click', () => {
                this.hideSidebar();
            });
        }

        // Video quality change
        document.getElementById('video-quality-select')?.addEventListener('change', (e) => {
            this.setVideoQuality(e.target.value);
        });

        // Audio quality change
        document.getElementById('audio-quality-select')?.addEventListener('change', (e) => {
            this.setAudioQuality(e.target.value);
        });

        // Auto adjustment toggle
        document.getElementById('auto-adjust-checkbox')?.addEventListener('change', (e) => {
            this.setAutoAdjustment(e.target.checked);
        });
    }

    // ======== QUALITY MONITORING ========
    startQualityMonitoring() {
        if (this.monitoringInterval) {
            clearInterval(this.monitoringInterval);
        }

        this.monitoringInterval = setInterval(() => {
            this.checkConnectionQuality();
            this.updateQualityIndicators();
            
            if (this.isAutoAdjustEnabled) {
                this.autoAdjustQuality();
            }
        }, 2000); // Check every 2 seconds

        console.log('📊 Quality monitoring started');
    }

    stopQualityMonitoring() {
        if (this.monitoringInterval) {
            clearInterval(this.monitoringInterval);
            this.monitoringInterval = null;
        }
        console.log('📊 Quality monitoring stopped');
    }

    // ======== CONNECTION QUALITY CHECK ========
    async checkConnectionQuality() {
        try {
            // Estimate bandwidth using multiple methods
            const bandwidth = await this.estimateBandwidth();
            const latency = await this.measureLatency();
            const stats = await this.getPeerConnectionStats();

            this.connectionStats = {
                bandwidth: bandwidth,
                latency: latency,
                packetLoss: stats.packetLoss || 0,
                frameRate: stats.frameRate || 0,
                resolution: stats.resolution || { width: 0, height: 0 }
            };

            // Update UI
            this.updateStatsDisplay();
            
            // Trigger callback if set
            if (this.onStatsUpdate) {
                this.onStatsUpdate(this.connectionStats);
            }

        } catch (error) {
            console.warn('Error checking connection quality:', error);
        }
    }

    async estimateBandwidth() {
        // Simple bandwidth estimation using download test
        try {
            const startTime = Date.now();
            const response = await fetch('/favicon.ico?' + Math.random(), { cache: 'no-cache' });
            const endTime = Date.now();
            const duration = (endTime - startTime) / 1000; // seconds
            const bytes = parseInt(response.headers.get('content-length') || '1024');
            const bandwidth = (bytes * 8) / duration / 1000; // kbps
            
            return Math.max(bandwidth, 100); // Minimum 100 kbps
        } catch (error) {
            console.warn('Bandwidth estimation failed:', error);
            return 1000; // Default fallback
        }
    }

    async measureLatency() {
        try {
            const startTime = Date.now();
            await fetch('/favicon.ico?' + Math.random(), { cache: 'no-cache' });
            const endTime = Date.now();
            return endTime - startTime;
        } catch (error) {
            console.warn('Latency measurement failed:', error);
            return 100; // Default fallback
        }
    }

    async getPeerConnectionStats() {
        // Get WebRTC peer connection statistics
        const stats = {
            packetLoss: 0,
            frameRate: 0,
            resolution: { width: 0, height: 0 }
        };

        try {
            // This would be integrated with existing peer connections
            // For now, return mock data
            if (window.peers && Object.keys(window.peers).length > 0) {
                const firstPeer = Object.values(window.peers)[0];
                if (firstPeer && firstPeer._pc) {
                    const reports = await firstPeer._pc.getStats();
                    
                    reports.forEach(report => {
                        if (report.type === 'inbound-rtp' && report.mediaType === 'video') {
                            stats.frameRate = report.framesPerSecond || 0;
                            stats.packetLoss = (report.packetsLost / (report.packetsReceived + report.packetsLost)) * 100 || 0;
                        }
                        
                        if (report.type === 'track' && report.kind === 'video') {
                            stats.resolution.width = report.frameWidth || 0;
                            stats.resolution.height = report.frameHeight || 0;
                        }
                    });
                }
            }
        } catch (error) {
            console.warn('Error getting peer connection stats:', error);
        }

        return stats;
    }

    // ======== QUALITY ADJUSTMENT ========
    autoAdjustQuality() {
        if (!this.isAutoAdjustEnabled) return;
        
        const now = Date.now();
        if (now - this.lastAdjustmentTime < 10000) return; // Cooldown 10 seconds

        const { bandwidth, latency, packetLoss } = this.connectionStats;
        let recommendedQuality = this.getRecommendedQuality(bandwidth, latency, packetLoss);

        if (recommendedQuality !== this.currentVideoQuality) {
            try {
            this.setVideoQuality(recommendedQuality, true);
            this.lastAdjustmentTime = now;
            } catch (error) {
                showError('Lỗi tự động điều chỉnh chất lượng: ' + (error?.message || error), true);
            }
        }
    }

    getRecommendedQuality(bandwidth, latency, packetLoss) {
        // Quality decision algorithm
        if (bandwidth < 300 || latency > 500 || packetLoss > 5) {
            return 'minimal';
        } else if (bandwidth < 600 || latency > 300 || packetLoss > 3) {
            return 'low';
        } else if (bandwidth < 1000 || latency > 200 || packetLoss > 1) {
            return 'medium';
        } else {
            return 'high';
        }
    }

    // ======== QUALITY SETTERS ========
    async setVideoQuality(quality, isAutoAdjust = false) {
        if (!this.qualityProfiles[quality]) {
            showError('Chất lượng video không hợp lệ!', true);
            return;
        }

        this.currentVideoQuality = quality;
        
        try {
            if (quality === 'auto') {
                this.isAutoAdjustEnabled = true;
                document.getElementById('auto-adjust-checkbox').checked = true;
            } else {
                // Apply specific quality constraints
                await this.applyVideoConstraints(this.qualityProfiles[quality].video);
                
                if (!isAutoAdjust) {
                    this.isAutoAdjustEnabled = false;
                    document.getElementById('auto-adjust-checkbox').checked = false;
                }
            }

            // Update UI
            document.getElementById('video-quality-select').value = quality;
            this.updateQualityInfo('video', quality);
            
            // Save preference
            this.saveQualityPreference('video', quality);
            
            // Trigger callback
            if (this.onQualityChange) {
                this.onQualityChange('video', quality);
            }

            console.log(`📹 Video quality set to: ${this.qualityProfiles[quality].name}`);
            
        } catch (error) {
            showError('Lỗi khi áp dụng chất lượng video: ' + (error?.message || error), true);
        }
    }

    async setAudioQuality(quality) {
        if (!this.qualityProfiles[quality]) {
            showError('Chất lượng audio không hợp lệ!', true);
            return;
        }

        this.currentAudioQuality = quality;
        
        try {
            if (quality !== 'auto') {
                await this.applyAudioConstraints(this.qualityProfiles[quality].audio);
            }

            // Update UI
            document.getElementById('audio-quality-select').value = quality;
            this.updateQualityInfo('audio', quality);
            
            // Save preference
            this.saveQualityPreference('audio', quality);
            
            // Trigger callback
            if (this.onQualityChange) {
                this.onQualityChange('audio', quality);
            }

            console.log(`🎵 Audio quality set to: ${this.qualityProfiles[quality].name}`);
            
        } catch (error) {
            showError('Lỗi khi áp dụng chất lượng audio: ' + (error?.message || error), true);
        }
    }

    setAutoAdjustment(enabled) {
        this.isAutoAdjustEnabled = enabled;
        
        if (enabled) {
            console.log('🤖 Auto quality adjustment enabled');
            this.setVideoQuality('auto');
        } else {
            console.log('🎛️ Manual quality control enabled');
        }
        
        this.saveQualityPreference('autoAdjust', enabled);
    }

    // ======== APPLY CONSTRAINTS ========
    async applyVideoConstraints(constraints) {
        // Get local stream from videocall.js
        const localStreamFunc = window.localStream;
        const localStreamObj = typeof localStreamFunc === 'function' ? localStreamFunc() : localStreamFunc;
        
        if (!localStreamObj) return;

        const videoTrack = localStreamObj.getVideoTracks()[0];
        if (!videoTrack) return;

        try {
            await videoTrack.applyConstraints({
                width: { ideal: constraints.width },
                height: { ideal: constraints.height },
                frameRate: { ideal: constraints.frameRate }
            });

            // Update peer connections if they exist
            if (window.peers) {
                Object.values(window.peers).forEach(peer => {
                    if (peer._pc) {
                        const sender = peer._pc.getSenders().find(s => 
                            s.track && s.track.kind === 'video'
                        );
                        if (sender && sender.setParameters) {
                            const params = sender.getParameters();
                            if (params.encodings && params.encodings[0]) {
                                params.encodings[0].maxBitrate = constraints.bitrate * 1000;
                                sender.setParameters(params);
                            }
                        }
                    }
                });
            }

        } catch (error) {
            showError('Không thể áp dụng cấu hình video: ' + (error?.message || error), true);
            throw error;
        }
    }

    async applyAudioConstraints(constraints) {
        // Get local stream from videocall.js
        const localStreamFunc = window.localStream;
        const localStreamObj = typeof localStreamFunc === 'function' ? localStreamFunc() : localStreamFunc;
        
        if (!localStreamObj) return;

        const audioTrack = localStreamObj.getAudioTracks()[0];
        if (!audioTrack) return;

        try {
            await audioTrack.applyConstraints({
                sampleRate: { ideal: constraints.sampleRate }
            });

            // Update peer connections for audio bitrate
            if (window.peers) {
                Object.values(window.peers).forEach(peer => {
                    if (peer._pc) {
                        const sender = peer._pc.getSenders().find(s => 
                            s.track && s.track.kind === 'audio'
                        );
                        if (sender && sender.setParameters) {
                            const params = sender.getParameters();
                            if (params.encodings && params.encodings[0]) {
                                params.encodings[0].maxBitrate = constraints.bitrate * 1000;
                                sender.setParameters(params);
                            }
                        }
                    }
                });
            }

        } catch (error) {
            showError('Không thể áp dụng cấu hình audio: ' + (error?.message || error), true);
            throw error;
        }
    }

    // ======== UI UPDATES ========
    updateStatsDisplay() {
        const { bandwidth, latency, frameRate, resolution } = this.connectionStats;

        const bandwidthEl = document.getElementById('bandwidth-value');
        const latencyEl = document.getElementById('latency-value');
        const framerateEl = document.getElementById('framerate-value');
        const resolutionEl = document.getElementById('resolution-value');

        if (bandwidthEl) bandwidthEl.textContent = `${Math.round(bandwidth)} kbps`;
        if (latencyEl) latencyEl.textContent = `${Math.round(latency)} ms`;
        if (framerateEl) framerateEl.textContent = `${Math.round(frameRate)} fps`;
        if (resolutionEl) resolutionEl.textContent = `${resolution.width}x${resolution.height}`;
    }

    updateQualityIndicators() {
        const { bandwidth, latency, packetLoss } = this.connectionStats;
        const qualityScore = this.calculateQualityScore(bandwidth, latency, packetLoss);
        
        const iconElement = document.getElementById('quality-icon');
        const statusElement = document.getElementById('quality-status');
        const detailElement = document.getElementById('quality-detail');
        
        if (iconElement && statusElement && detailElement) {
            if (qualityScore >= 80) {
                iconElement.innerHTML = '<i class="bi bi-wifi" style="color: var(--success);"></i>';
                statusElement.textContent = 'Kết nối tuyệt vời';
                detailElement.textContent = 'Chất lượng cuộc gọi cao';
            } else if (qualityScore >= 60) {
                iconElement.innerHTML = '<i class="bi bi-wifi-2" style="color: var(--warning);"></i>';
                statusElement.textContent = 'Kết nối tốt';
                detailElement.textContent = 'Chất lượng cuộc gọi ổn định';
            } else if (qualityScore >= 40) {
                iconElement.innerHTML = '<i class="bi bi-wifi-1" style="color: var(--warning-alt);"></i>';
                statusElement.textContent = 'Kết nối trung bình';
                detailElement.textContent = 'Có thể gặp một số vấn đề';
            } else {
                iconElement.innerHTML = '<i class="bi bi-wifi-off" style="color: var(--danger);"></i>';
                statusElement.textContent = 'Kết nối kém';
                detailElement.textContent = 'Chất lượng cuộc gọi thấp';
            }
        }
    }

    calculateQualityScore(bandwidth, latency, packetLoss) {
        let score = 100;
        
        // Bandwidth scoring (0-40 points)
        if (bandwidth < 200) score -= 40;
        else if (bandwidth < 500) score -= 25;
        else if (bandwidth < 1000) score -= 10;
        
        // Latency scoring (0-30 points)
        if (latency > 500) score -= 30;
        else if (latency > 300) score -= 20;
        else if (latency > 150) score -= 10;
        
        // Packet loss scoring (0-30 points)
        if (packetLoss > 5) score -= 30;
        else if (packetLoss > 3) score -= 20;
        else if (packetLoss > 1) score -= 10;
        
        return Math.max(0, score);
    }

    updateQualityInfo(type, quality) {
        const infoElement = document.getElementById(`${type}-quality-info`);
        if (!infoElement) return;
        
        const profile = this.qualityProfiles[quality];
        
        if (quality === 'auto') {
            infoElement.textContent = 'Tự động điều chỉnh theo kết nối';
        } else if (profile) {
            const constraints = profile[type];
            if (constraints) {
                if (type === 'video') {
                    infoElement.textContent = `${constraints.width}x${constraints.height} @ ${constraints.frameRate}fps`;
                } else if (type === 'audio') {
                    infoElement.textContent = `${constraints.bitrate}kbps @ ${constraints.sampleRate/1000}kHz`;
                }
            }
        }
    }

    showQualityError(message) {
        // Integrate with existing error notification system
        if (window.showError) {
            window.showError(message, true);
        } else {
            console.error(message);
        }
    }

    // ======== PREFERENCES ========
    saveQualityPreference(key, value) {
        try {
            const preferences = JSON.parse(localStorage.getItem('zela-quality-preferences') || '{}');
            preferences[key] = value;
            localStorage.setItem('zela-quality-preferences', JSON.stringify(preferences));
        } catch (error) {
            console.warn('Error saving quality preference:', error);
        }
    }

    loadQualityPreferences() {
        try {
            const preferences = JSON.parse(localStorage.getItem('zela-quality-preferences') || '{}');
            
            if (preferences.video) {
                this.setVideoQuality(preferences.video);
            }
            
            if (preferences.audio) {
                this.setAudioQuality(preferences.audio);
            }
            
            if (typeof preferences.autoAdjust === 'boolean') {
                this.setAutoAdjustment(preferences.autoAdjust);
            }
            
        } catch (error) {
            console.warn('Error loading quality preferences:', error);
        }
    }

    // ======== PUBLIC API ========
    getConnectionStats() {
        return { ...this.connectionStats };
    }

    getCurrentQuality() {
        return {
            video: this.currentVideoQuality,
            audio: this.currentAudioQuality,
            autoAdjust: this.isAutoAdjustEnabled
        };
    }

    destroy() {
        this.stopQualityMonitoring();
        
        const panel = document.getElementById('quality-control-panel');
        if (panel) {
            panel.remove();
        }
        
        console.log('🎥 Quality Control System destroyed');
    }
}

// ======== ERROR HANDLING (giống videocall.js) ========
function showError(message, isTemporary = true) {
    let errorDiv = document.getElementById('error-notification');
    if (!errorDiv) {
        // Nếu chưa có, tạo mới
        errorDiv = document.createElement('div');
        errorDiv.id = 'error-notification';
        errorDiv.style.position = 'fixed';
        errorDiv.style.top = '30px';
        errorDiv.style.right = '30px';
        errorDiv.style.zIndex = 9999;
        errorDiv.innerHTML = `
            <div class="error-content" style="background: #ff5e62; color: white; padding: 18px 32px; border-radius: 16px; font-size: 1.2em; box-shadow: 0 4px 16px rgba(0,0,0,0.15); display: flex; align-items: center; gap: 16px;">
                <span class="error-icon">⚠️</span>
                <span class="error-message"></span>
                <button class="error-close" style="background: none; border: none; color: white; font-size: 1.5em; margin-left: 12px; cursor: pointer;">&times;</button>
            </div>
        `;
        document.body.appendChild(errorDiv);
    }
    errorDiv.style.display = 'block';
    errorDiv.querySelector('.error-message').textContent = message;
    errorDiv.querySelector('.error-close').onclick = hideError;
    if (isTemporary) {
        setTimeout(hideError, 5000);
    }
}
function hideError() {
    const errorDiv = document.getElementById('error-notification');
    if (errorDiv) errorDiv.style.display = 'none';
}

// ======== GLOBAL INSTANCE ========
window.qualityController = null;

// ======== INITIALIZATION FUNCTION ========
function initializeQualityControl() {
    if (!window.qualityController) {
        window.qualityController = new QualityController();
    }
    return window.qualityController;
}

// ======== EXPORT FOR MODULE USAGE ========
if (typeof module !== 'undefined' && module.exports) {
    module.exports = { QualityController, initializeQualityControl };
} 