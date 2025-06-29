/**
 * Stats Sidebar Management
 * Handles sidebar display and real-time statistics updates
 */

class StatsSidebar {
    constructor() {
        this.sidebar = null;
        this.overlay = null;
        this.isOpen = false;
        this.refreshInterval = null;
        this.meetingCode = '';
        this.currentUserId = 0;
        this.signalRConnection = null;
        
        this.init();
    }
    
    init() {
        // Get elements
        this.sidebar = document.getElementById('stats-sidebar');
        this.overlay = document.getElementById('sidebar-overlay');
        this.toggleBtn = document.getElementById('toggle-stats');
        this.closeBtn = document.getElementById('close-stats');
        this.retryBtn = document.getElementById('retry-stats');
        
        // Get meeting info
        this.meetingCode = window.meetingCode || '';
        this.currentUserId = parseInt(document.querySelector('.videocall-container')?.dataset.userId || '0');
        
        if (!this.sidebar || !this.overlay) {
            console.error('Stats sidebar elements not found');
            return;
        }
        
        this.bindEvents();
        this.initSignalR();
    }
    
    bindEvents() {
        // Toggle sidebar
        if (this.toggleBtn) {
            this.toggleBtn.addEventListener('click', () => this.show());
        }
        
        // Close sidebar
        if (this.closeBtn) {
            this.closeBtn.addEventListener('click', () => this.hide());
        }
        
        // Overlay click to close
        if (this.overlay) {
            this.overlay.addEventListener('click', () => this.hide());
        }
        
        // Retry button
        if (this.retryBtn) {
            this.retryBtn.addEventListener('click', () => this.loadStats());
        }
        
        // ESC key to close
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape' && this.isOpen) {
                this.hide();
            }
        });
    }
    
    initSignalR() {
        // Try to use existing SignalR connection if available
        if (window.connection && window.connection.state === signalR.HubConnectionState.Connected) {
            this.signalRConnection = window.connection;
            this.setupSignalRListeners();
        } else {
            // Wait for connection to be established
            setTimeout(() => {
                if (window.connection) {
                    this.signalRConnection = window.connection;
                    this.setupSignalRListeners();
                }
            }, 2000);
        }
    }
    
    setupSignalRListeners() {
        if (!this.signalRConnection) return;
        
        try {
            // Listen for stats updates
            this.signalRConnection.on('StatsUpdated', (meetingCode) => {
                if (meetingCode === this.meetingCode && this.isOpen) {
                    console.log('📊 Stats update received from SignalR');
                    this.loadStats();
                }
            });
            
            console.log('📊 SignalR listeners setup for stats updates');
        } catch (error) {
            console.error('Error setting up SignalR listeners:', error);
        }
    }
    
    show() {
        if (this.isOpen) return;
        
        // Close other sidebars first
        this.closeAllSidebars();
        
        this.isOpen = true;
        this.sidebar.classList.add('open');
        this.overlay.classList.add('show');
        
        // Update button state
        const toggleBtn = document.getElementById('toggle-stats');
        if (toggleBtn) {
            toggleBtn.innerHTML = '📊 Ẩn thống kê';
            toggleBtn.classList.add('active');
        }
        
        // Load stats immediately
        this.loadStats();
        
        // Start auto-refresh every 10 seconds
        this.startAutoRefresh();
        
        console.log('📊 Stats sidebar opened');
    }
    
    hide() {
        if (!this.isOpen) return;
        
        this.isOpen = false;
        this.sidebar.classList.remove('open');
        this.overlay.classList.remove('show');
        
        // Update button state
        const toggleBtn = document.getElementById('toggle-stats');
        if (toggleBtn) {
            toggleBtn.innerHTML = '📊 Xem thống kê';
            toggleBtn.classList.remove('active');
        }
        
        // Stop auto-refresh
        this.stopAutoRefresh();
        
        console.log('📊 Stats sidebar closed');
    }
    
    closeAllSidebars() {
        // Close quality sidebar
        const qualitySidebar = document.getElementById('quality-sidebar');
        if (qualitySidebar) qualitySidebar.classList.remove('open');
        
        // Close recording sidebar
        const recordingSidebar = document.getElementById('recording-sidebar');
        if (recordingSidebar) recordingSidebar.classList.remove('open');
        
        // Hide overlay
        const overlay = document.getElementById('sidebar-overlay');
        if (overlay) overlay.classList.remove('show');
        
        // Reset button states
        const qualityBtn = document.getElementById('toggle-quality');
        if (qualityBtn) {
            qualityBtn.innerHTML = '⚙️ Chất lượng';
            qualityBtn.classList.remove('active');
        }
        
        const recordingBtn = document.getElementById('toggle-recording');
        if (recordingBtn) {
            recordingBtn.innerHTML = '🎥 Ghi hình';
            recordingBtn.classList.remove('active');
        }
    }
    
    startAutoRefresh() {
        this.stopAutoRefresh(); // Clear any existing interval
        
        this.refreshInterval = setInterval(() => {
            if (this.isOpen) {
                this.loadStats();
            }
        }, 10000); // Refresh every 10 seconds
    }
    
    stopAutoRefresh() {
        if (this.refreshInterval) {
            clearInterval(this.refreshInterval);
            this.refreshInterval = null;
        }
    }
    
    async loadStats() {
        if (!this.meetingCode) {
            this.showError('Không có mã phòng');
            return;
        }
        
        this.showLoading();
        
        try {
            const response = await fetch(`/Meeting/GetRoomStatsData?code=${encodeURIComponent(this.meetingCode)}`, {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json'
                }
            });
            
            if (!response.ok) {
                throw new Error(`HTTP ${response.status}`);
            }
            
            const data = await response.json();
            this.renderStats(data);
            
        } catch (error) {
            console.error('Error loading stats:', error);
            this.showError('Không thể tải thống kê. Vui lòng thử lại.');
        }
    }
    
    showLoading() {
        const loading = document.getElementById('stats-loading');
        const data = document.getElementById('stats-data');
        const error = document.getElementById('stats-error');
        
        if (loading) loading.style.display = 'block';
        if (data) data.style.display = 'none';
        if (error) error.style.display = 'none';
    }
    
    showError(message) {
        const loading = document.getElementById('stats-loading');
        const data = document.getElementById('stats-data');
        const error = document.getElementById('stats-error');
        
        if (loading) loading.style.display = 'none';
        if (data) data.style.display = 'none';
        if (error) {
            error.style.display = 'block';
            const errorMsg = error.querySelector('p');
            if (errorMsg) errorMsg.textContent = `❌ ${message}`;
        }
    }
    
    renderStats(data) {
        const loading = document.getElementById('stats-loading');
        const statsData = document.getElementById('stats-data');
        const error = document.getElementById('stats-error');
        
        if (loading) loading.style.display = 'none';
        if (error) error.style.display = 'none';
        if (!statsData) return;
        
        if (!data.hasActiveSession) {
            statsData.innerHTML = `
                <div class="stat-card">
                    <h4>🏠 Thông tin phòng</h4>
                    <div class="stat-info">
                        <p><strong>Tên phòng:</strong> ${data.roomInfo.name}</p>
                        <p><strong>Mã phòng:</strong> ${data.roomInfo.password}</p>
                        <p><strong>Tạo lúc:</strong> ${this.formatDateTime(data.roomInfo.createdAt)}</p>
                    </div>
                </div>
                <div class="stat-card">
                    <div class="no-participants">
                        <p>💤 Hiện tại không có cuộc họp nào đang diễn ra</p>
                    </div>
                </div>
            `;
        } else {
            statsData.innerHTML = this.renderActiveSession(data);
        }
        
        statsData.style.display = 'block';
        
        // Add refresh time indicator
        const refreshTime = document.createElement('div');
        refreshTime.className = 'refresh-time';
        refreshTime.textContent = `Cập nhật lúc: ${new Date().toLocaleTimeString('vi-VN')}`;
        statsData.appendChild(refreshTime);
    }
    
    renderActiveSession(data) {
        const session = data.session;
        const participants = data.participants;
        
        return `
            <div class="stat-card">
                <h4>🏠 Thông tin phòng</h4>
                <div class="stat-info">
                    <p><strong>Tên phòng:</strong> ${data.roomInfo.name}</p>
                    <p><strong>Mã phòng:</strong> ${data.roomInfo.password}</p>
                    <p><strong>Trạng thái:</strong> <span class="live-indicator">🔴 LIVE</span></p>
                </div>
            </div>
            
            <div class="stat-card">
                <h4>⏱️ Thời gian cuộc họp</h4>
                <div class="stat-info">
                    <p><strong>Bắt đầu:</strong> ${this.formatDateTime(session.startedAt)}</p>
                    <p><strong>Thời lượng:</strong> ${session.durationMinutes} phút</p>
                    <p><strong>Đang ghi hình:</strong> ${session.hasRecording ? '✅ Có' : '❌ Không'}</p>
                </div>
            </div>
            
            <div class="stat-card">
                <h4>👥 Tổng quan người tham gia</h4>
                <div class="stat-info">
                    <p><strong>Tổng số người tham gia:</strong> ${participants.total}</p>
                    <p><strong>Đang online:</strong> ${participants.active} người</p>
                    <p><strong>Đã rời:</strong> ${participants.left} người</p>
                </div>
            </div>
            
            ${participants.active > 0 ? this.renderActiveParticipants(participants.activeList) : ''}
            ${participants.left > 0 ? this.renderLeftParticipants(participants.leftList) : ''}
        `;
    }
    
    renderActiveParticipants(activeList) {
        if (!activeList || activeList.length === 0) return '';
        
        const participantsHtml = activeList.map(participant => `
            <div class="participant-item">
                <div>
                    <div class="participant-name ${participant.userId === this.currentUserId ? 'current-user' : ''}">
                        ${participant.userName}${participant.userId === this.currentUserId ? ' (Bạn)' : ''}
                    </div>
                    <div class="participant-time">
                        Tham gia lúc: ${this.formatDateTime(participant.joinTime)}
                    </div>
                </div>
                <div class="participant-duration">
                    ${participant.durationMinutes} phút
                </div>
            </div>
        `).join('');
        
        return `
            <div class="stat-card">
                <h4>🟢 Đang tham gia (${activeList.length})</h4>
                <div class="participants-list">
                    ${participantsHtml}
                </div>
            </div>
        `;
    }
    
    renderLeftParticipants(leftList) {
        if (!leftList || leftList.length === 0) return '';
        
        // Only show last 5 people who left
        const recentLeft = leftList.slice(0, 5);
        
        const participantsHtml = recentLeft.map(participant => `
            <div class="participant-item">
                <div>
                    <div class="participant-name">
                        ${participant.userName}
                    </div>
                    <div class="participant-time">
                        ${this.formatDateTime(participant.joinTime)} - ${this.formatDateTime(participant.leaveTime)}
                    </div>
                </div>
                <div class="participant-duration">
                    ${participant.durationMinutes} phút
                </div>
            </div>
        `).join('');
        
        return `
            <div class="stat-card">
                <h4>🔴 Đã rời (${leftList.length})</h4>
                <div class="participants-list">
                    ${participantsHtml}
                    ${leftList.length > 5 ? `<div class="no-participants">... và ${leftList.length - 5} người khác</div>` : ''}
                </div>
            </div>
        `;
    }
    
    formatDateTime(dateString) {
        try {
            const date = new Date(dateString);
            return date.toLocaleString('vi-VN', {
                year: 'numeric',
                month: '2-digit',
                day: '2-digit',
                hour: '2-digit',
                minute: '2-digit',
                second: '2-digit'
            });
        } catch (error) {
            return dateString;
        }
    }
    
    // Public methods
    refresh() {
        if (this.isOpen) {
            this.loadStats();
        }
    }
    
    destroy() {
        this.stopAutoRefresh();
        
        // Remove SignalR listeners if connection exists
        if (this.signalRConnection) {
            try {
                this.signalRConnection.off('StatsUpdated');
            } catch (error) {
                console.error('Error removing SignalR listeners:', error);
            }
        }
    }
}

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    // Wait a bit for other scripts to initialize
    setTimeout(() => {
        window.statsSidebar = new StatsSidebar();
        console.log('📊 Stats Sidebar initialized');
    }, 1000);
});

// Export for global access
window.StatsSidebar = StatsSidebar; 