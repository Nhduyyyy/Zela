/**
 * Real-time Subtitle System
 * Xử lý audio recording và transcription cho video call
 */

class SubtitleSystem {
    constructor() {
        // Cấu hình
        this.config = {
            chunkDuration: 3000, // 3 giây (tăng từ 2 giây)
            sampleRate: 16000,
            language: 'vi', // ISO-639-1 format cho OpenAI
            maxSubtitleLength: 100,
            subtitleDisplayTime: 15000 // 15 giây (tăng từ 5 giây)
        };
        
        // State
        this.isEnabled = false;
        this.isRecording = false;
        this.audioContext = null;
        this.mediaRecorder = null;
        this.audioChunks = [];
        this.subtitleHistory = [];
        this.currentUserId = null;
        this.sessionId = null;
        this.activeSubtitleUsers = new Set(); // Track users who have subtitles enabled
        
        // DOM elements
        this.subtitleContainer = null;
        this.toggleButton = null;
        this.subtitleHistoryContainer = null;
        
        // Voice activity detection
        this.analyser = null;
        this.voiceThreshold = 0.05; // Giảm threshold để nhạy hơn
        this.isSpeaking = false;
        
        console.log('🎬 SubtitleSystem initialized');
    }

    async init() {
        try {
            // Khởi tạo DOM elements
            this.initDOMElements();
            
            // Lấy user ID từ page
            this.currentUserId = this.getUserId();
            
            if (!this.currentUserId) {
                console.warn('⚠️ User ID not found');
                return;
            }
            
            // Khởi tạo audio context trước
            await this.initAudioContext();
            
            // Đợi sessionId từ videocall.js
            await this.waitForSessionId();
            
            // Load user preference
            await this.loadUserPreference();
            
            // Khởi tạo SignalR connection cho subtitle sharing
            this.initSignalR();
            
            console.log('✅ SubtitleSystem initialized successfully');
            
        } catch (error) {
            console.error('❌ Error initializing SubtitleSystem:', error);
        }
    }

    async waitForSessionId() {
        // Đợi tối đa 10 giây để lấy sessionId
        let attempts = 0;
        const maxAttempts = 100; // 100 * 100ms = 10 giây
        
        while (attempts < maxAttempts) {
            this.sessionId = this.getSessionId();
            
            if (this.sessionId && this.sessionId !== "00000000-0000-0000-0000-000000000000") {
                console.log('✅ Session ID found:', this.sessionId);
                return;
            }
            
            if (attempts % 10 === 0) { // Log mỗi 1 giây
                console.log(`⏳ Waiting for session ID... (${attempts + 1}/${maxAttempts})`);
            }
            
            await new Promise(resolve => setTimeout(resolve, 100));
            attempts++;
        }
        
        console.warn('⚠️ Session ID not found after 10 seconds, using fallback');
        this.sessionId = "00000000-0000-0000-0000-000000000000";
    }

    // Method để update sessionId khi có thay đổi
    updateSessionId(newSessionId) {
        if (newSessionId && newSessionId !== this.sessionId) {
            console.log('🔄 Updating session ID:', newSessionId);
            this.sessionId = newSessionId;
            
            // Re-initialize các components cần sessionId
            this.initSignalR();
        }
    }

    initDOMElements() {
        // Tạo subtitle container
        this.createSubtitleContainer();
        
        // Tìm toggle button
        this.toggleButton = document.querySelector('#subtitleToggle');
        
        // Tạo subtitle history container
        this.subtitleHistoryContainer = document.querySelector('#subtitleHistory');
        
        // Lắng nghe sessionId changes
        this.setupSessionIdListener();
        
        console.log('🎬 DOM elements initialized');
    }

    setupSessionIdListener() {
        // Lắng nghe khi window.currentSessionId thay đổi
        let lastSessionId = window.currentSessionId;
        
        setInterval(() => {
            if (window.currentSessionId && window.currentSessionId !== lastSessionId) {
                console.log('🔄 Session ID changed from', lastSessionId, 'to', window.currentSessionId);
                this.updateSessionId(window.currentSessionId);
                lastSessionId = window.currentSessionId;
            }
        }, 500); // Check mỗi 500ms
    }

    initSignalR() {
        try {
            // Sử dụng connection từ videocall.js
            if (window.signalRConnection) {
                this.connection = window.signalRConnection;
                
                // Lắng nghe subtitle từ user khác
                this.connection.on('ReceiveSubtitle', (subtitle) => {
                    console.log('📺 Received subtitle from other user:', subtitle);
                    this.displaySubtitleFromOtherUser(subtitle);
                });
                
                // Lắng nghe user join/leave với subtitle
                this.connection.on('UserSubtitleToggled', (userId, enabled) => {
                    if (enabled) {
                        this.activeSubtitleUsers.add(userId);
                        console.log('👤 User enabled subtitles:', userId);
                    } else {
                        this.activeSubtitleUsers.delete(userId);
                        console.log('👤 User disabled subtitles:', userId);
                    }
                });
                
                console.log('🔗 SignalR connected for subtitle sharing');
                
                // Join subtitle group nếu có sessionId
                if (this.sessionId && this.sessionId !== "00000000-0000-0000-0000-000000000000") {
                    this.connection.invoke('JoinSubtitleGroup', this.sessionId);
                }
            } else {
                console.warn('⚠️ SignalR connection not available');
            }
                
        } catch (error) {
            console.error('❌ Error initializing SignalR:', error);
        }
    }

    async initAudioContext() {
        try {
            console.log('🎤 Initializing audio context...');
            
            // Tạo Audio Context
            this.audioContext = new (window.AudioContext || window.webkitAudioContext)();
            
            // Xin quyền truy cập microphone với cấu hình tối ưu cho AI
            const stream = await navigator.mediaDevices.getUserMedia({
                audio: {
                    echoCancellation: true,
                    noiseSuppression: true,
                    autoGainControl: true,
                    sampleRate: this.config.sampleRate,
                    channelCount: 1,
                    volume: 1.0
                }
            });
            
            console.log('🎤 Microphone access granted');
            
            // Tạo MediaRecorder với format tương thích
            const mimeType = MediaRecorder.isTypeSupported('audio/webm;codecs=opus') 
                ? 'audio/webm;codecs=opus' 
                : 'audio/webm';
                
            this.mediaRecorder = new MediaRecorder(stream, {
                mimeType: mimeType
            });
            
            console.log('🎤 MediaRecorder created with mimeType:', mimeType);
            
            // Tạo analyser để detect voice activity
            this.analyser = this.audioContext.createAnalyser();
            this.analyser.fftSize = 256;
            this.analyser.smoothingTimeConstant = 0.8;
            this.microphone = this.audioContext.createMediaStreamSource(stream);
            this.microphone.connect(this.analyser);
            
            // Voice activity detection
            this.voiceThreshold = 0.1;
            this.isSpeaking = false;
            
            console.log('✅ Audio context initialized successfully');
            
        } catch (error) {
            console.error('❌ Error initializing audio context:', error);
            
            // Hiển thị thông báo lỗi cho user
            this.showNotification('Lỗi: Không thể truy cập microphone', 'error');
            
            // Không throw error để không crash app
            this.mediaRecorder = null;
            this.analyser = null;
        }
    }

    createSubtitleContainer() {
        // Tạo container cho subtitle
        this.subtitleContainer = document.createElement('div');
        this.subtitleContainer.id = 'subtitle-container';
        this.subtitleContainer.className = 'subtitle-overlay';
        this.subtitleContainer.style.cssText = `
            position: fixed;
            bottom: 100px;
            left: 50%;
            transform: translateX(-50%);
            background: rgba(0, 0, 0, 0.8);
            color: white;
            padding: 10px 20px;
            border-radius: 20px;
            font-size: 16px;
            font-weight: 500;
            max-width: 80%;
            text-align: center;
            z-index: 1000;
            display: none;
            backdrop-filter: blur(10px);
            box-shadow: 0 4px 20px rgba(0, 0, 0, 0.3);
        `;
        
        document.body.appendChild(this.subtitleContainer);
        
        // Tạo language selector
        this.createLanguageSelector();
        
        console.log('📺 Subtitle container created');
    }

    createLanguageSelector() {
        // Tạo language selector
        this.languageSelector = document.createElement('div');
        this.languageSelector.id = 'language-selector';
        this.languageSelector.style.cssText = `
            position: fixed;
            bottom: 160px;
            left: 50%;
            transform: translateX(-50%);
            background: rgba(0, 0, 0, 0.9);
            color: white;
            padding: 15px;
            border-radius: 10px;
            font-size: 14px;
            z-index: 1001;
            display: none;
            backdrop-filter: blur(10px);
            box-shadow: 0 4px 20px rgba(0, 0, 0, 0.3);
            min-width: 200px;
        `;
        
        // Tạo label
        const label = document.createElement('div');
        label.textContent = '🌍 Chọn ngôn ngữ phụ đề:';
        label.style.cssText = `
            margin-bottom: 10px;
            font-weight: bold;
            text-align: center;
        `;
        this.languageSelector.appendChild(label);
        
        // Tạo select dropdown
        const select = document.createElement('select');
        select.id = 'language-select';
        select.style.cssText = `
            background: rgba(255, 255, 255, 0.1);
            color: white;
            border: 1px solid rgba(255, 255, 255, 0.3);
            border-radius: 5px;
            padding: 8px 12px;
            font-size: 14px;
            outline: none;
            width: 100%;
            cursor: pointer;
        `;
        
        // Thêm các ngôn ngữ
        const languages = this.getSupportedLanguages();
        Object.entries(languages).forEach(([code, name]) => {
            const option = document.createElement('option');
            option.value = code;
            option.textContent = `${name} (${code.toUpperCase()})`;
            if (code === this.config.language) {
                option.selected = true;
            }
            select.appendChild(option);
        });
        
        // Event listener
        select.addEventListener('change', (e) => {
            this.setLanguage(e.target.value);
        });
        
        this.languageSelector.appendChild(select);
        
        // Tạo close button
        const closeButton = document.createElement('button');
        closeButton.textContent = '✕';
        closeButton.style.cssText = `
            position: absolute;
            top: 5px;
            right: 10px;
            background: none;
            border: none;
            color: white;
            font-size: 16px;
            cursor: pointer;
            padding: 0;
            width: 20px;
            height: 20px;
            display: flex;
            align-items: center;
            justify-content: center;
        `;
        closeButton.addEventListener('click', () => {
            this.languageSelector.style.display = 'none';
        });
        
        this.languageSelector.appendChild(closeButton);
        document.body.appendChild(this.languageSelector);
        
        console.log('🌍 Language selector created');
    }

    async loadUserPreference() {
        try {
            const response = await fetch(`/Meeting/GetUserSubtitlePreference?sessionId=${this.sessionId}`);
            const data = await response.json();
            
            this.isEnabled = data.enabled;
            console.log('🎛️ User subtitle preference loaded:', this.isEnabled);
            
        } catch (error) {
            console.error('❌ Error loading user preference:', error);
            this.isEnabled = false;
        }
    }

    async toggleSubtitles() {
        try {
            this.isEnabled = !this.isEnabled;
            
            const response = await fetch('/Meeting/ToggleSubtitles', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    sessionId: this.sessionId || "00000000-0000-0000-0000-000000000000",
                    enabled: this.isEnabled
                })
            });
            
            const data = await response.json();
            
            if (data.success) {
                console.log('🎛️ Subtitle toggled:', this.isEnabled);
                
                if (this.isEnabled) {
                    // Hiển thị language selector khi bật phụ đề
                    if (this.languageSelector) {
                        this.languageSelector.style.display = 'block';
                        setTimeout(() => {
                            this.languageSelector.style.display = 'none';
                        }, 15000); // Hiển thị 15 giây (tăng từ 5 giây)
                    }
                    
                    // Kiểm tra và khởi tạo lại audio nếu cần
                    if (!this.mediaRecorder) {
                        console.log('🔄 Re-initializing audio context...');
                        await this.initAudioContext();
                    }
                    
                    this.startRecording();
                } else {
                    this.stopRecording();
                }
                
                // Hiển thị thông báo
                this.showNotification(
                    this.isEnabled ? 'Phụ đề đã bật' : 'Phụ đề đã tắt',
                    this.isEnabled ? 'success' : 'info'
                );
            }
            
        } catch (error) {
            console.error('❌ Error toggling subtitles:', error);
            this.showNotification('Lỗi khi bật/tắt phụ đề', 'error');
        }
    }

    // Thay đổi ngôn ngữ
    async setLanguage(language) {
        this.config.language = language;
        console.log(`🌍 Language changed to: ${language}`);
        
        // Hiển thị language selector
        if (this.languageSelector) {
            this.languageSelector.style.display = 'block';
            setTimeout(() => {
                this.languageSelector.style.display = 'none';
            }, 15000); // Hiển thị 15 giây (tăng từ 3 giây)
        }
        
        // Lưu preference
        try {
            await fetch('/Meeting/SaveLanguagePreference', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    sessionId: this.sessionId || "00000000-0000-0000-0000-000000000000",
                    language: language
                })
            });
        } catch (error) {
            console.error('❌ Error saving language preference:', error);
        }
        
        this.showNotification(`🌍 Ngôn ngữ đã đổi sang: ${this.getSupportedLanguages()[language]}`, 'success');
    }

    // Translate text
    async translateText(text, targetLanguage) {
        try {
            const response = await fetch('/Meeting/TranslateText', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    text: text,
                    targetLanguage: targetLanguage,
                    sourceLanguage: 'vi' // Giả sử source là tiếng Việt
                })
            });
            
            const data = await response.json();
            
            if (data.success) {
                return data.translatedText;
            } else {
                console.error('❌ Translation failed:', data.error);
                return text; // Fallback to original text
            }
        } catch (error) {
            console.error('❌ Error translating text:', error);
            return text; // Fallback to original text
        }
    }

    // Các ngôn ngữ được support
    getSupportedLanguages() {
        return {
            'vi': 'Tiếng Việt',
            'en': 'English',
            'zh': '中文',
            'ja': '日本語',
            'ko': '한국어',
            'fr': 'Français',
            'de': 'Deutsch',
            'es': 'Español'
        };
    }

    startRecording() {
        if (this.isRecording || !this.isEnabled) return;
        
        try {
            console.log('🎙️ Starting audio recording...');
            
            // Kiểm tra MediaRecorder đã được khởi tạo chưa
            if (!this.mediaRecorder) {
                console.error('❌ MediaRecorder not initialized');
                this.showNotification('Lỗi: MediaRecorder chưa được khởi tạo', 'error');
                return;
            }
            
            // Kiểm tra trạng thái MediaRecorder
            if (this.mediaRecorder.state === 'inactive') {
                console.log('🔄 MediaRecorder is inactive, trying to start...');
            }
            
            this.isRecording = true;
            this.audioChunks = [];
            
            // Bắt đầu recording
            this.mediaRecorder.start();
            
            // Xử lý data available
            this.mediaRecorder.ondataavailable = (event) => {
                if (event.data.size > 0) {
                    this.audioChunks.push(event.data);
                }
            };
            
            // Xử lý khi recording stop
            this.mediaRecorder.onstop = () => {
                this.processAudioChunk();
            };
            
            // Tự động stop sau mỗi chunk duration
            this.recordingInterval = setInterval(() => {
                if (this.mediaRecorder && this.mediaRecorder.state === 'recording') {
                    this.mediaRecorder.stop();
                    this.mediaRecorder.start();
                }
            }, this.config.chunkDuration);
            
            console.log('✅ Audio recording started');
            
        } catch (error) {
            console.error('❌ Error starting recording:', error);
            this.isRecording = false;
        }
    }

    stopRecording() {
        if (!this.isRecording) return;
        
        try {
            console.log('🛑 Stopping audio recording...');
            
            this.isRecording = false;
            
            // Stop recording
            if (this.mediaRecorder && this.mediaRecorder.state === 'recording') {
                this.mediaRecorder.stop();
            }
            
            // Clear interval
            if (this.recordingInterval) {
                clearInterval(this.recordingInterval);
                this.recordingInterval = null;
            }
            
            console.log('✅ Audio recording stopped');
            
        } catch (error) {
            console.error('❌ Error stopping recording:', error);
        }
    }

    // Kiểm tra voice activity
    checkVoiceActivity() {
        const dataArray = new Uint8Array(this.analyser.frequencyBinCount);
        this.analyser.getByteFrequencyData(dataArray);
        
        // Tính trung bình amplitude
        const average = dataArray.reduce((a, b) => a + b) / dataArray.length;
        const normalizedAverage = average / 255;
        
        const wasSpeaking = this.isSpeaking;
        this.isSpeaking = normalizedAverage > this.voiceThreshold;
        
        // Debug logging - chỉ log mỗi 10 lần để không spam
        if (Math.random() < 0.1) { // 10% chance
            console.log(`🔊 Audio level: ${normalizedAverage.toFixed(3)} (threshold: ${this.voiceThreshold})`);
        }
        
        // Log khi bắt đầu/dừng nói
        if (!wasSpeaking && this.isSpeaking) {
            console.log('🎤 Voice detected');
        } else if (wasSpeaking && !this.isSpeaking) {
            console.log('🔇 Voice stopped');
        }
        
        return this.isSpeaking;
    }

    async processAudioChunk() {
        if (this.audioChunks.length === 0) return;
        
        try {
            // Tạm thời bỏ qua voice activity để test
            // if (!this.checkVoiceActivity()) {
            //     console.log('🔇 No voice detected, skipping...');
            //     this.audioChunks = [];
            //     return;
            // }
            
            console.log('🎵 Processing audio chunk (bypassing voice detection)...');
            
            // Tạo blob từ audio chunks
            const audioBlob = new Blob(this.audioChunks, { type: this.mediaRecorder.mimeType });
            this.audioChunks = [];
            
            // Kiểm tra kích thước audio (chỉ xử lý nếu có đủ audio)
            if (audioBlob.size < 500) { // Giảm threshold để nhạy hơn
                console.log('🔇 Audio chunk too small, skipping...');
                return;
            }
            
            // Chuyển thành base64
            const base64Audio = await this.blobToBase64(audioBlob);
            
            // Debug sessionId và speakerId trước khi gửi
            console.log('🔍 [DEBUG] Current sessionId:', this.sessionId);
            console.log('🔍 [DEBUG] Current speakerId:', this.currentUserId);
            console.log('🔍 [DEBUG] window.currentSessionId:', window.currentSessionId);
            console.log('🔍 [DEBUG] window.currentUserId:', window.currentUserId);
            
            // Đảm bảo có valid sessionId và speakerId
            const sessionId = this.sessionId || window.currentSessionId || "00000000-0000-0000-0000-000000000000";
            const speakerId = this.currentUserId || window.currentUserId || 0;
            
            console.log(`🎵 Processing audio chunk (${audioBlob.size} bytes)...`);
            console.log(`🎵 [DEBUG] Final sessionId: ${sessionId}`);
            console.log(`🎵 [DEBUG] Final speakerId: ${speakerId}`);
            
            // Gửi đến server để transcribe
            const response = await fetch('/Meeting/TranscribeAudio', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    audioData: base64Audio,
                    sessionId: sessionId,
                    language: this.config.language,
                    speakerId: speakerId
                })
            });
            
            const data = await response.json();
            
            if (data.success && data.subtitle) {
                console.log('📝 Transcribed text:', data.subtitle.originalText);
                this.displaySubtitle(data.subtitle);
                
                // Gửi subtitle đến các user khác qua SignalR
                if (this.connection && this.connection.state === 'Connected') {
                    this.connection.invoke('SendSubtitle', this.sessionId, data.subtitle);
                }
            } else {
                console.warn('⚠️ No text transcribed from audio');
                
                // Hiển thị thông báo cho user khi không dịch được
                const errorMessage = data.error || 'Không thể nhận diện giọng nói';
                this.showNotification(`🔇 ${errorMessage}`, 'warning');
                
                // Hiển thị subtitle "Không thể nhận diện" trong 3 giây
                this.displaySubtitle({
                    originalText: '🔇 Không thể nhận diện giọng nói',
                    translatedText: '🔇 Speech not recognized',
                    confidence: 0,
                    speakerId: speakerId,
                    timestamp: new Date().toISOString()
                });
            }
            
        } catch (error) {
            console.error('❌ Error processing audio chunk:', error);
        }
    }

    async displaySubtitle(subtitle) {
        try {
            // Debug subtitleContainer
            console.log('🔍 [DEBUG] subtitleContainer:', this.subtitleContainer);
            
            // Đảm bảo subtitleContainer tồn tại
            if (!this.subtitleContainer) {
                console.log('🔄 Creating subtitle container...');
                this.createSubtitleContainer();
            }
            
            // Thêm vào history
            this.subtitleHistory.push(subtitle);
            
            // Giới hạn history
            if (this.subtitleHistory.length > 10) {
                this.subtitleHistory.shift();
            }
            
            // Translate text nếu cần
            let displayText = subtitle.originalText;
            if (this.config.language !== 'vi' && subtitle.originalText) {
                try {
                    displayText = await this.translateText(subtitle.originalText, this.config.language);
                    console.log(`🌍 Translated: "${subtitle.originalText}" → "${displayText}"`);
                } catch (error) {
                    console.error('❌ Translation error:', error);
                    displayText = subtitle.originalText; // Fallback
                }
            }
            
            // Hiển thị subtitle với animation mượt mà
            this.subtitleContainer.textContent = displayText;
            this.subtitleContainer.style.display = 'block';
            this.subtitleContainer.style.opacity = '0';
            this.subtitleContainer.style.transform = 'translateY(20px)';
            
            // Animation fade in
            setTimeout(() => {
                this.subtitleContainer.style.transition = 'all 0.3s ease';
                this.subtitleContainer.style.opacity = '1';
                this.subtitleContainer.style.transform = 'translateY(0)';
            }, 50);
            
            // Tự động ẩn sau 15 giây với animation fade out
            setTimeout(() => {
                this.subtitleContainer.style.transition = 'all 0.3s ease';
                this.subtitleContainer.style.opacity = '0';
                this.subtitleContainer.style.transform = 'translateY(-20px)';
                
                setTimeout(() => {
                    this.subtitleContainer.style.display = 'none';
                }, 300);
            }, this.config.subtitleDisplayTime);
            
            console.log('📺 Subtitle displayed:', displayText);
            
        } catch (error) {
            console.error('❌ Error displaying subtitle:', error);
        }
    }

    showNotification(message, type = 'info') {
        // Tạo notification element
        const notification = document.createElement('div');
        notification.className = `subtitle-notification ${type}`;
        notification.textContent = message;
        
        // Chọn màu dựa trên type
        let backgroundColor;
        switch (type) {
            case 'success':
                backgroundColor = '#4CAF50';
                break;
            case 'error':
                backgroundColor = '#f44336';
                break;
            case 'warning':
                backgroundColor = '#ff9800';
                break;
            default:
                backgroundColor = '#2196F3';
        }
        
        notification.style.cssText = `
            position: fixed;
            top: 20px;
            right: 20px;
            background: ${backgroundColor};
            color: white;
            padding: 10px 20px;
            border-radius: 5px;
            font-size: 14px;
            z-index: 1001;
            animation: slideIn 0.3s ease;
            box-shadow: 0 2px 10px rgba(0,0,0,0.2);
        `;
        
        document.body.appendChild(notification);
        
        // Tự động xóa sau 3 giây
        setTimeout(() => {
            notification.remove();
        }, 3000);
    }

    async blobToBase64(blob) {
        return new Promise((resolve, reject) => {
            const reader = new FileReader();
            reader.onload = () => {
                const base64 = reader.result.split(',')[1];
                resolve(base64);
            };
            reader.onerror = reject;
            reader.readAsDataURL(blob);
        });
    }

    getSessionId() {
        // Lấy session ID từ videocall.js hoặc fallback
        const sessionId = window.currentSessionId || 
                         document.querySelector('[data-session-id]')?.dataset.sessionId;
        
        // Log để debug
        console.log('🔍 Session ID from getSessionId():', sessionId);
        
        return sessionId || "00000000-0000-0000-0000-000000000000";
    }

    getUserId() {
        // Lấy user ID từ data attribute hoặc global variable
        return document.querySelector('[data-user-id]')?.dataset.userId || window.currentUserId;
    }

    // Public methods
    enable() {
        this.isEnabled = true;
        this.startRecording();
    }

    disable() {
        this.isEnabled = false;
        this.stopRecording();
    }

    // Hiển thị language selector
    showLanguageSelector() {
        if (this.languageSelector) {
            this.languageSelector.style.display = 'block';
            console.log('🌍 Language selector shown');
        } else {
            console.warn('⚠️ Language selector not initialized');
        }
    }

    // Ẩn language selector
    hideLanguageSelector() {
        if (this.languageSelector) {
            this.languageSelector.style.display = 'none';
            console.log('🌍 Language selector hidden');
        }
    }

    destroy() {
        this.stopRecording();
        
        if (this.subtitleContainer) {
            this.subtitleContainer.remove();
        }
        
        if (this.audioContext) {
            this.audioContext.close();
        }
        
        if (this.connection) {
            this.connection.stop();
        }
        
        console.log('🗑️ Subtitle System destroyed');
    }

    displaySubtitleFromOtherUser(subtitle) {
        try {
            // Hiển thị subtitle từ user khác với màu khác
            const speakerName = subtitle.speakerName || `User ${subtitle.speakerId}`;
            const displayText = `${speakerName}: ${subtitle.originalText}`;
            
            // Thêm vào history
            this.subtitleHistory.push({
                ...subtitle,
                displayText: displayText,
                isFromOtherUser: true
            });
            
            // Giới hạn history
            if (this.subtitleHistory.length > 10) {
                this.subtitleHistory.shift();
            }
            
            // Hiển thị subtitle với màu khác cho user khác
            this.subtitleContainer.textContent = displayText;
            this.subtitleContainer.style.display = 'block';
            this.subtitleContainer.style.opacity = '0';
            this.subtitleContainer.style.transform = 'translateY(20px)';
            this.subtitleContainer.style.color = '#4CAF50'; // Màu xanh cho user khác
            
            // Animation fade in
            setTimeout(() => {
                this.subtitleContainer.style.transition = 'all 0.3s ease';
                this.subtitleContainer.style.opacity = '1';
                this.subtitleContainer.style.transform = 'translateY(0)';
            }, 50);
            
            // Tự động ẩn sau 5 giây với animation fade out
            setTimeout(() => {
                this.subtitleContainer.style.transition = 'all 0.3s ease';
                this.subtitleContainer.style.opacity = '0';
                this.subtitleContainer.style.transform = 'translateY(-20px)';
                
                setTimeout(() => {
                    this.subtitleContainer.style.display = 'none';
                    this.subtitleContainer.style.color = ''; // Reset màu
                }, 300);
            }, this.config.subtitleDisplayTime);
            
            console.log('📺 Subtitle from other user displayed:', displayText);
            
        } catch (error) {
            console.error('❌ Error displaying subtitle from other user:', error);
        }
    }
}

// CSS Animation
const style = document.createElement('style');
style.textContent = `
    @keyframes slideIn {
        from {
            transform: translateX(100%);
            opacity: 0;
        }
        to {
            transform: translateX(0);
            opacity: 1;
        }
    }
`;
document.head.appendChild(style);

// Export cho global use
window.SubtitleSystem = SubtitleSystem; 