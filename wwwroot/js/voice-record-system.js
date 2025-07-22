/**
 * Voice Record System for Zela
 * Handles voice recording popup and loading animations
 */

class VoiceRecordSystem {
    constructor() {
        this.isRecording = false;
        this.isCancelled = false;
        this.mediaRecorder = null;
        this.audioChunks = [];
        this.recordingTimer = null;
        this.recordingStartTime = null;
        this.currentLanguage = 'vi';

        // DOM elements
        this.voiceBtn = null;
        this.voiceDropdown = null;
        this.startVoiceRecordBtn = null;
        this.closeVoiceDropdownBtn = null;
        this.langSelect = null;
        this.chatInput = null;

        // Popup elements
        this.recordingPopup = null;
        this.loadingOverlay = null;

        this.initializeElements();
        this.createPopupElements();
        this.setupEventListeners();

        console.log('🎤 VoiceRecordSystem initialized');
    }

    initializeElements() {
        this.voiceBtn = document.getElementById('voiceRecordBtn');
        this.voiceDropdown = document.getElementById('voiceDropdown');
        this.startVoiceRecordBtn = document.getElementById('startVoiceRecord');
        this.closeVoiceDropdownBtn = document.getElementById('closeVoiceDropdown');
        this.langSelect = document.getElementById('voiceLangSelect');
        this.chatInput = document.getElementById('groupChatInput');
    }

    createPopupElements() {
        // Find chat-panel container
        const chatPanel = document.querySelector('.chat-panel');
        if (!chatPanel) {
            console.error('❌ Chat panel not found, retrying in 500ms...');
            // Retry after a short delay in case chat-panel is not loaded yet
            setTimeout(() => this.createPopupElements(), 500);
            return;
        }

        // Create recording popup
        this.recordingPopup = document.createElement('div');
        this.recordingPopup.className = 'recording-popup';
        this.recordingPopup.innerHTML = `
            <div class="recording-icon">
                <i class="bi bi-mic-fill"></i>
            </div>
            <div class="recording-title">Đang ghi âm</div>
            <div class="recording-subtitle">Nói rõ ràng để có kết quả tốt nhất</div>
            <div class="recording-timer" id="recordingTimer">00:00</div>
            <div class="recording-controls">
                <button class="stop-record-btn" id="stopRecordBtn">
                    <i class="bi bi-stop-fill"></i> Dừng ghi âm
                </button>
                <button class="cancel-record-btn" id="cancelRecordBtn">
                    <i class="bi bi-x-lg"></i> Hủy
                </button>
            </div>
        `;

        // Create loading overlay
        this.loadingOverlay = document.createElement('div');
        this.loadingOverlay.className = 'loading-overlay';
        this.loadingOverlay.innerHTML = `
            <div class="loading-container">
                <div class="loading-spinner"></div>
                <div class="loading-title">Đang xử lý giọng nói</div>
                <div class="loading-subtitle">AI đang chuyển đổi giọng nói thành văn bản...</div>
                <div class="loading-progress">
                    <div class="loading-progress-bar"></div>
                </div>
                <div class="loading-dots">
                    <div class="loading-dot"></div>
                    <div class="loading-dot"></div>
                    <div class="loading-dot"></div>
                </div>
            </div>
        `;

        // Add to chat-panel instead of body
        chatPanel.appendChild(this.recordingPopup);
        chatPanel.appendChild(this.loadingOverlay);

        // Set chat-panel to relative positioning if not already
        if (getComputedStyle(chatPanel).position === 'static') {
            chatPanel.style.position = 'relative';
        }

        console.log('✅ Popup elements created in chat-panel');
    }

    setupEventListeners() {
        // Voice button click
        if (this.voiceBtn) {
            this.voiceBtn.addEventListener('click', (e) => {
                e.stopPropagation();
                if (this.isRecording) {
                    this.stopRecording();
                } else {
                    this.toggleDropdown();
                }
            });
        }

        // Close dropdown button
        if (this.closeVoiceDropdownBtn) {
            this.closeVoiceDropdownBtn.addEventListener('click', () => {
                this.hideDropdown();
            });
        }

        // Start recording button
        if (this.startVoiceRecordBtn) {
            this.startVoiceRecordBtn.addEventListener('click', () => {
                this.startRecording();
            });
        }

        // Language select change
        if (this.langSelect) {
            this.langSelect.addEventListener('change', (e) => {
                this.currentLanguage = e.target.value;
            });
        }

        // Close dropdown when clicking outside
        document.addEventListener('click', (e) => {
            if (!e.target.closest('.voice-record-dropdown')) {
                this.hideDropdown();
            }
        });

        // Recording popup controls
        document.getElementById('stopRecordBtn')?.addEventListener('click', () => {
            this.stopRecording();
        });

        document.getElementById('cancelRecordBtn')?.addEventListener('click', () => {
            this.cancelRecording();
        });

        // Keyboard shortcuts
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape' && this.isRecording) {
                this.cancelRecording();
            }
        });
    }

    toggleDropdown() {
        if (this.voiceDropdown) {
            const isVisible = this.voiceDropdown.style.display === 'block';
            this.voiceDropdown.style.display = isVisible ? 'none' : 'block';
        }
    }

    hideDropdown() {
        if (this.voiceDropdown) {
            this.voiceDropdown.style.display = 'none';
        }
    }

    async startRecording() {
        if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
            this.showNotification('Trình duyệt không hỗ trợ ghi âm!', 'error');
            return;
        }

        try {
            // Request microphone permission
            const stream = await navigator.mediaDevices.getUserMedia({
                audio: {
                    echoCancellation: true,
                    noiseSuppression: true,
                    autoGainControl: true,
                    sampleRate: 44100
                }
            });

            // Setup MediaRecorder
            this.mediaRecorder = new MediaRecorder(stream);
            this.audioChunks = [];

            // Setup event handlers
            this.mediaRecorder.ondataavailable = (event) => {
                if (event.data.size > 0) {
                    this.audioChunks.push(event.data);
                }
            };

            this.mediaRecorder.onstop = async () => {
                await this.processAudioRecording();
            };

            // Start recording
            this.mediaRecorder.start();
            this.isRecording = true;
            this.isCancelled = false;
            this.recordingStartTime = new Date();

            // Update UI
            this.updateRecordingUI();
            this.showRecordingPopup();
            this.startRecordingTimer();

            console.log('🎤 Recording started');

        } catch (err) {
            console.error('Recording error:', err);
            this.showNotification('Không thể truy cập microphone!', 'error');
        }
    }

    stopRecording() {
        if (this.mediaRecorder && this.isRecording) {
            this.mediaRecorder.stop();
            this.isRecording = false;

            // Stop all tracks
            if (this.mediaRecorder.stream) {
                this.mediaRecorder.stream.getTracks().forEach(track => track.stop());
            }

            // Update UI
            this.updateRecordingUI();
            this.hideRecordingPopup();
            this.stopRecordingTimer();

            console.log('🛑 Recording stopped');
        }
    }

    cancelRecording() {
        if (this.isRecording) {
            console.log('❌ Cancelling recording...');

            // Set a flag to indicate cancellation
            this.isCancelled = true;

            // Temporarily remove the onstop handler to prevent processing
            const originalOnStop = this.mediaRecorder.onstop;
            this.mediaRecorder.onstop = null;

            // Stop recording without processing
            if (this.mediaRecorder && this.mediaRecorder.state === 'recording') {
                this.mediaRecorder.stop();
            }

            // Stop all tracks
            if (this.mediaRecorder && this.mediaRecorder.stream) {
                this.mediaRecorder.stream.getTracks().forEach(track => track.stop());
            }

            // Restore the onstop handler
            this.mediaRecorder.onstop = originalOnStop;

            this.isRecording = false;
            this.audioChunks = [];

            // Update UI
            this.updateRecordingUI();
            this.hideRecordingPopup();
            this.stopRecordingTimer();

            // Show cancellation notification
            this.showNotification('Đã hủy ghi âm', 'info');

            console.log('❌ Recording cancelled successfully');
        }
    }

    updateRecordingUI() {
        if (this.voiceBtn) {
            if (this.isRecording) {
                this.voiceBtn.classList.add('text-danger');
                this.voiceBtn.title = 'Đang ghi âm... Bấm để dừng';
            } else {
                this.voiceBtn.classList.remove('text-danger');
                this.voiceBtn.title = 'Ghi âm giọng nói';
            }
        }

        if (this.startVoiceRecordBtn) {
            if (this.isRecording) {
                this.startVoiceRecordBtn.textContent = 'Đang ghi âm...';
                this.startVoiceRecordBtn.disabled = true;
            } else {
                this.startVoiceRecordBtn.textContent = 'Bắt đầu ghi âm';
                this.startVoiceRecordBtn.disabled = false;
            }
        }
    }

    showRecordingPopup() {
        if (this.recordingPopup) {
            this.recordingPopup.classList.add('show');
        }
    }

    hideRecordingPopup() {
        if (this.recordingPopup) {
            this.recordingPopup.classList.remove('show');
        }
    }

    showLoadingOverlay() {
        if (this.loadingOverlay) {
            this.loadingOverlay.classList.add('show');
        }
    }

    hideLoadingOverlay() {
        if (this.loadingOverlay) {
            this.loadingOverlay.classList.remove('show');
        }
    }

    startRecordingTimer() {
        this.recordingTimer = setInterval(() => {
            this.updateRecordingTimer();
        }, 1000);
    }

    stopRecordingTimer() {
        if (this.recordingTimer) {
            clearInterval(this.recordingTimer);
            this.recordingTimer = null;
        }
    }

    updateRecordingTimer() {
        if (this.recordingStartTime) {
            const elapsed = Math.floor((new Date() - this.recordingStartTime) / 1000);
            const minutes = Math.floor(elapsed / 60).toString().padStart(2, '0');
            const seconds = (elapsed % 60).toString().padStart(2, '0');

            const timerElement = document.getElementById('recordingTimer');
            if (timerElement) {
                timerElement.textContent = `${minutes}:${seconds}`;
            }
        }
    }

    async processAudioRecording() {
        // Check if recording was cancelled
        if (this.isCancelled) {
            console.log('🎤 Recording was cancelled, skipping processing');
            return;
        }

        if (this.audioChunks.length === 0) {
            this.showNotification('Không có dữ liệu ghi âm!', 'error');
            return;
        }

        const audioBlob = new Blob(this.audioChunks, { type: 'audio/webm' });
        const formData = new FormData();
        formData.append('audio', audioBlob);
        formData.append('language', this.currentLanguage);

        try {
            // Show loading overlay
            this.showLoadingOverlay();

            // Send to server
            const response = await fetch('/GroupChat/VoiceToText', {
                method: 'POST',
                body: formData
            });

            // Hide loading overlay
            this.hideLoadingOverlay();

            if (response.ok) {
                const data = await response.json();
                if (data && data.text) {
                    // Insert text into chat input
                    if (this.chatInput) {
                        this.chatInput.value = data.text;
                        this.chatInput.focus();

                        // Trigger input event to update any listeners
                        this.chatInput.dispatchEvent(new Event('input', { bubbles: true }));
                    }

                    this.showNotification('Chuyển đổi giọng nói thành công!', 'success');
                } else {
                    this.showNotification('Không nhận diện được nội dung!', 'warning');
                }
            } else {
                this.showNotification('Có lỗi khi nhận diện giọng nói!', 'error');
            }
        } catch (err) {
            console.error('Processing error:', err);
            this.hideLoadingOverlay();
            this.showNotification('Lỗi gửi file ghi âm!', 'error');
        }
    }

    showNotification(message, type = 'info') {
        // Create notification element
        const notification = document.createElement('div');
        notification.className = `voice-notification voice-notification-${type}`;
        notification.innerHTML = `
            <div class="notification-content">
                <i class="bi bi-${this.getNotificationIcon(type)}"></i>
                <span>${message}</span>
            </div>
        `;

        // Add styles
        notification.style.cssText = `
            position: fixed;
            top: 20px;
            right: 20px;
            background: var(--bg-primary);
            border: 2px solid var(--border-color);
            border-radius: 12px;
            padding: 15px 20px;
            box-shadow: 0 8px 32px rgba(255, 90, 87, 0.15);
            z-index: 10001;
            transform: translateX(100%);
            transition: transform 0.3s ease;
            max-width: 300px;
            backdrop-filter: blur(10px);
        `;

        // Add notification content styles
        const content = notification.querySelector('.notification-content');
        content.style.cssText = `
            display: flex;
            align-items: center;
            gap: 10px;
            color: var(--text-primary);
            font-weight: 600;
        `;

        // Add type-specific styles
        if (type === 'success') {
            notification.style.borderColor = 'var(--success-color)';
            notification.style.boxShadow = '0 8px 32px rgba(40, 167, 69, 0.15)';
        } else if (type === 'error') {
            notification.style.borderColor = 'var(--danger-color)';
            notification.style.boxShadow = '0 8px 32px rgba(255, 90, 87, 0.15)';
        } else if (type === 'warning') {
            notification.style.borderColor = 'var(--warning-color)';
            notification.style.boxShadow = '0 8px 32px rgba(255, 193, 7, 0.15)';
        }

        // Add to body
        document.body.appendChild(notification);

        // Animate in
        setTimeout(() => {
            notification.style.transform = 'translateX(0)';
        }, 100);

        // Remove after 4 seconds
        setTimeout(() => {
            notification.style.transform = 'translateX(100%)';
            setTimeout(() => {
                if (notification.parentNode) {
                    notification.parentNode.removeChild(notification);
                }
            }, 300);
        }, 4000);
    }

    getNotificationIcon(type) {
        switch (type) {
            case 'success': return 'check-circle-fill';
            case 'error': return 'exclamation-circle-fill';
            case 'warning': return 'exclamation-triangle-fill';
            default: return 'info-circle-fill';
        }
    }

    // Public methods for external use
    getCurrentLanguage() {
        return this.currentLanguage;
    }

    setLanguage(language) {
        this.currentLanguage = language;
        if (this.langSelect) {
            this.langSelect.value = language;
        }
    }

    isCurrentlyRecording() {
        return this.isRecording;
    }
}

// Global instance
window.voiceRecordSystem = null;

// Initialize function
function initializeVoiceRecordSystem() {
    if (!window.voiceRecordSystem) {
        window.voiceRecordSystem = new VoiceRecordSystem();
    }
    return window.voiceRecordSystem;
}

// Auto-initialize when DOM is ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initializeVoiceRecordSystem);
} else {
    initializeVoiceRecordSystem();
} 