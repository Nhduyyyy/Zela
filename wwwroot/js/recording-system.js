// Recording & Screenshots System for Zela Video Call
// Handles video recording, audio recording, and screenshot capture

class RecordingSystem {
    constructor() {
        this.mediaRecorder = null;
        this.recordedChunks = [];
        this.isRecording = false;
        this.isPaused = false;
        this.recordingStartTime = null;
        this.recordingStopTime = null;
        this.recordingTimer = null;
        this.currentRecordingId = null;
        
        // Recording settings
        this.recordingSettings = {
            video: {
                high: { 
                    videoBitsPerSecond: 2500000, 
                    width: 1920, 
                    height: 1080, 
                    frameRate: 30,
                    videoBitrate: 2500000
                },
                medium: { 
                    videoBitsPerSecond: 1500000, 
                    width: 1280, 
                    height: 720, 
                    frameRate: 30,
                    videoBitrate: 1500000
                },
                low: { 
                    videoBitsPerSecond: 800000, 
                    width: 854, 
                    height: 480, 
                    frameRate: 24,
                    videoBitrate: 800000
                },
                audioOnly: { 
                    videoBitsPerSecond: 0, 
                    width: 0, 
                    height: 0, 
                    frameRate: 0,
                    videoBitrate: 0
                }
            },
            audio: {
                high: { 
                    audioBitsPerSecond: 128000, 
                    audioBitrate: 128000,
                    sampleRate: 48000,
                    channels: 2
                },
                medium: { 
                    audioBitsPerSecond: 96000, 
                    audioBitrate: 96000,
                    sampleRate: 44100,
                    channels: 2
                },
                low: { 
                    audioBitsPerSecond: 64000, 
                    audioBitrate: 64000,
                    sampleRate: 44100,
                    channels: 1
                },
                audioOnly: { 
                    audioBitsPerSecond: 128000, 
                    audioBitrate: 128000,
                    sampleRate: 48000,
                    channels: 2
                }
            }
        };
        
        this.currentQuality = 'medium';
        
        // Screenshot settings
        this.screenshotSettings = {
            format: 'png', // png, jpeg, webp
            quality: 0.95,
            includeAudio: false
        };
        
        // Storage settings
        this.storageSettings = {
            uploadToCloud: true,
            saveLocally: true,
            maxLocalFiles: 10,
            autoDelete: true,
            retentionDays: 7
        };
        
        // Recording history
        this.recordingHistory = JSON.parse(localStorage.getItem('zela-recordings') || '[]');
        
        // Callbacks
        this.onRecordingStart = null;
        this.onRecordingStop = null;
        this.onRecordingPause = null;
        this.onRecordingResume = null;
        this.onScreenshot = null;
        this.onError = null;
        
        this.initializeRecordingSystem();
    }

    // ======== INITIALIZATION ========
    initializeRecordingSystem() {
        this.createRecordingUI();
        this.setupRecordingEventListeners();
        this.loadRecordingPreferences();
        console.log('🎬 Recording System initialized');
    }

    // ======== UI CREATION ========
    createRecordingUI() {
        console.log('🎬 Creating Recording UI...');
        // Get recording content container
        const recordingContent = document.getElementById('recording-content');
        if (!recordingContent) {
            console.error('Recording content container not found');
            return;
        }
        
        // Create recording control content
        recordingContent.innerHTML = `
            
            <div style="margin-bottom: 30px; text-align: center;">
                <div id="audio-indicator" style="width: 60px; height: 60px; border-radius: 50%; background: linear-gradient(135deg, #22c55e, #16a34a); margin: 0 auto 15px; display: flex; align-items: center; justify-content: center; font-size: 24px; animation: pulse 2s infinite;">
                    🎤
                </div>
                <p style="color: rgba(255,255,255,0.8); margin: 0; font-size: 14px;" id="recording-status">Microphone Ready</p>
                <div style="color: white; font-weight: 600; font-size: 18px; margin-top: 10px;" id="recording-timer">00:00:00</div>
            </div>
            
            <div style="margin-bottom: 25px;">
                <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 12px; margin-bottom: 15px;">
                    <button id="start-recording-btn" style="padding: 15px; background: rgba(34,197,94,0.2); color: white; border: 2px solid rgba(34,197,94,0.5); border-radius: 12px; font-weight: 600; cursor: pointer; font-size: 14px; backdrop-filter: blur(10px); transition: all 0.3s ease;">
                        🎬 Bắt đầu ghi
                    </button>
                    <button id="stop-recording-btn" style="padding: 15px; background: rgba(239,68,68,0.2); color: white; border: 2px solid rgba(239,68,68,0.5); border-radius: 12px; font-weight: 600; cursor: pointer; font-size: 14px; backdrop-filter: blur(10px); transition: all 0.3s ease;" disabled>
                        ⏹️ Dừng ghi
                    </button>
                </div>
                <button id="pause-recording-btn" style="width: 100%; padding: 15px; background: rgba(251,191,36,0.2); color: white; border: 2px solid rgba(251,191,36,0.5); border-radius: 12px; font-weight: 600; cursor: pointer; font-size: 14px; backdrop-filter: blur(10px); transition: all 0.3s ease;" disabled>
                    ⏸️ Tạm dừng
                </button>
            </div>
            
            <div style="margin-bottom: 25px;">
                <label style="color: white; font-weight: 600; font-size: 16px; margin-bottom: 15px; display: block;">📸 Chụp ảnh màn hình:</label>
                <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 10px; margin-bottom: 10px;">
                    <button id="screenshot-all-btn" style="padding: 12px; background: rgba(59,130,246,0.2); color: white; border: 2px solid rgba(59,130,246,0.5); border-radius: 8px; font-weight: 600; cursor: pointer; font-size: 12px; backdrop-filter: blur(10px); transition: all 0.3s ease;">
                        📷 Toàn bộ
                    </button>
                    <button id="screenshot-self-btn" style="padding: 12px; background: rgba(139,69,19,0.2); color: white; border: 2px solid rgba(139,69,19,0.5); border-radius: 8px; font-weight: 600; cursor: pointer; font-size: 12px; backdrop-filter: blur(10px); transition: all 0.3s ease;">
                        🤳 Bản thân
                    </button>
                </div>
                <button id="screenshot-participant-btn" style="width: 100%; padding: 12px; background: rgba(168,85,247,0.2); color: white; border: 2px solid rgba(168,85,247,0.5); border-radius: 8px; font-weight: 600; cursor: pointer; font-size: 12px; backdrop-filter: blur(10px); transition: all 0.3s ease;">
                    👥 Người khác
                </button>
            </div>
            
            <div style="margin-bottom: 25px;">
                <label style="color: white; font-weight: 600; font-size: 16px; margin-bottom: 10px; display: block;">Chất lượng ghi hình:</label>
                <select id="recording-quality-select" style="width: 100%; padding: 12px; border: 2px solid rgba(255,255,255,0.3); border-radius: 12px; background: rgba(255,255,255,0.1); color: white; font-size: 14px; backdrop-filter: blur(10px); margin-bottom: 10px;">
                    <option value="high" style="background: #f5576c; color: white;">Cao (2.5 Mbps)</option>
                    <option value="medium" selected style="background: #f5576c; color: white;">Trung bình (1.5 Mbps)</option>
                    <option value="low" style="background: #f5576c; color: white;">Thấp (0.8 Mbps)</option>
                    <option value="audioOnly" style="background: #f5576c; color: white;">Chỉ âm thanh</option>
                </select>
                
                <label style="color: white; font-weight: 600; font-size: 16px; margin-bottom: 10px; display: block;">Định dạng ảnh:</label>
                <select id="screenshot-format-select" style="width: 100%; padding: 12px; border: 2px solid rgba(255,255,255,0.3); border-radius: 12px; background: rgba(255,255,255,0.1); color: white; font-size: 14px; backdrop-filter: blur(10px);">
                    <option value="png" style="background: #f5576c; color: white;">PNG (Chất lượng cao)</option>
                    <option value="jpeg" style="background: #f5576c; color: white;">JPEG (Nhỏ gọn)</option>
                    <option value="webp" style="background: #f5576c; color: white;">WebP (Tối ưu)</option>
                </select>
            </div>

            <div style="margin-bottom: 25px;">
                <label style="color: white; font-weight: 600; font-size: 16px; margin-bottom: 15px; display: block;">⚙️ Cài đặt lưu trữ:</label>
                <div style="display: flex; flex-direction: column; gap: 10px;">
                    <label style="color: white; font-size: 14px; display: flex; align-items: center; gap: 10px;">
                        <input type="checkbox" id="upload-cloud-checkbox" checked style="width: 16px; height: 16px;">
                        ☁️ Tự động tải lên cloud
                    </label>
                    <label style="color: white; font-size: 14px; display: flex; align-items: center; gap: 10px;">
                        <input type="checkbox" id="save-local-checkbox" checked style="width: 16px; height: 16px;">
                        💾 Lưu file cục bộ
                    </label>
                    <label style="color: white; font-size: 14px; display: flex; align-items: center; gap: 10px;">
                        <input type="checkbox" id="auto-delete-checkbox" style="width: 16px; height: 16px;">
                        🗑️ Tự động xóa sau 7 ngày
                    </label>
                </div>
            </div>
        `;

        console.log('🎬 Recording control content created');
    }

    // ======== SIDEBAR MANAGEMENT ========
    showSidebar() {
        // Close other sidebars first
        this.closeAllSidebars();
        
        const sidebar = document.getElementById('recording-sidebar');
        const overlay = document.getElementById('sidebar-overlay');
        
        if (sidebar && overlay) {
            sidebar.classList.add('open');
            overlay.classList.add('show');
            
            // Update button state
            const toggleBtn = document.getElementById('toggle-recording');
            if (toggleBtn) {
                toggleBtn.innerHTML = '🎥 Ẩn ghi hình';
                toggleBtn.classList.add('active');
            }
        }
    }
    
    hideSidebar() {
        const sidebar = document.getElementById('recording-sidebar');
        const overlay = document.getElementById('sidebar-overlay');
        
        if (sidebar && overlay) {
            sidebar.classList.remove('open');
            overlay.classList.remove('show');
            
            // Update button state
            const toggleBtn = document.getElementById('toggle-recording');
            if (toggleBtn) {
                toggleBtn.innerHTML = '🎥 Ghi hình';
                toggleBtn.classList.remove('active');
            }
        }
    }
    
    closeAllSidebars() {
        // Close stats sidebar
        const statsSidebar = document.getElementById('stats-sidebar');
        if (statsSidebar) statsSidebar.classList.remove('open');
        
        // Close quality sidebar
        const qualitySidebar = document.getElementById('quality-sidebar');
        if (qualitySidebar) qualitySidebar.classList.remove('open');
        
        // Hide overlay
        const overlay = document.getElementById('sidebar-overlay');
        if (overlay) overlay.classList.remove('show');
        
        // Reset button states
        const statsBtn = document.getElementById('toggle-stats');
        if (statsBtn) {
            statsBtn.innerHTML = '📊 Xem thống kê';
            statsBtn.classList.remove('active');
        }
        
        const qualityBtn = document.getElementById('toggle-quality');
        if (qualityBtn) {
            qualityBtn.innerHTML = '⚙️ Chất lượng';
            qualityBtn.classList.remove('active');
        }
    }

    // ======== EVENT LISTENERS ========
    setupRecordingEventListeners() {
        // Toggle sidebar visibility
        const toggleBtn = document.getElementById('toggle-recording');
        if (toggleBtn) {
            toggleBtn.addEventListener('click', () => {
                const sidebar = document.getElementById('recording-sidebar');
                if (sidebar && sidebar.classList.contains('open')) {
                    this.hideSidebar();
                } else {
                    this.showSidebar();
                }
            });
        }
        
        // Close sidebar button
        const closeBtn = document.getElementById('close-recording');
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

        // Recording controls
        document.getElementById('start-recording-btn')?.addEventListener('click', () => {
            this.startRecording();
        });

        document.getElementById('pause-recording-btn')?.addEventListener('click', () => {
            this.togglePauseRecording();
        });

        document.getElementById('stop-recording-btn')?.addEventListener('click', () => {
            this.stopRecording();
        });

        // Screenshot controls
        document.getElementById('screenshot-all-btn')?.addEventListener('click', () => {
            this.takeScreenshot('all');
        });

        document.getElementById('screenshot-self-btn')?.addEventListener('click', () => {
            this.takeScreenshot('self');
        });

        document.getElementById('screenshot-participant-btn')?.addEventListener('click', () => {
            this.takeScreenshot('participant');
        });

        // Settings
        document.getElementById('recording-quality-select')?.addEventListener('change', (e) => {
            this.setRecordingQuality(e.target.value);
        });

        document.getElementById('screenshot-format-select')?.addEventListener('change', (e) => {
            this.setScreenshotFormat(e.target.value);
        });

        // Storage settings
        document.getElementById('upload-cloud-checkbox')?.addEventListener('change', (e) => {
            this.storageSettings.uploadToCloud = e.target.checked;
            this.saveRecordingPreferences();
        });

        document.getElementById('save-local-checkbox')?.addEventListener('change', (e) => {
            this.storageSettings.saveLocally = e.target.checked;
            this.saveRecordingPreferences();
        });

        document.getElementById('auto-delete-checkbox')?.addEventListener('change', (e) => {
            this.storageSettings.autoDelete = e.target.checked;
            this.saveRecordingPreferences();
        });
    }

    // ======== RECORDING FUNCTIONS ========
    async startRecording() {
        try {
            if (this.isRecording) {
                console.warn('Recording already in progress');
                return;
            }

            // Get the video grid element for recording
            const videoGrid = document.getElementById('video-grid');
            if (!videoGrid) {
                throw new Error('Video grid not found');
            }

            // Create stream from video grid
            const stream = await this.captureVideoGrid();
            
            // Check if stream has audio
            this.checkStreamAudio(stream);
            
            // Setup MediaRecorder
            const options = this.getRecordingOptions();
            this.mediaRecorder = new MediaRecorder(stream, options);
            this.recordedChunks = [];

            // Setup event handlers
            this.mediaRecorder.ondataavailable = (event) => {
                if (event.data.size > 0) {
                    this.recordedChunks.push(event.data);
                }
            };

            this.mediaRecorder.onstop = () => {
                this.handleRecordingComplete();
            };

            this.mediaRecorder.onerror = (error) => {
                console.error('MediaRecorder error:', error);
                this.handleRecordingError(error);
            };

            // Start recording
            this.mediaRecorder.start(1000); // Collect data every second
            this.isRecording = true;
            this.recordingStartTime = new Date();
            this.currentRecordingId = this.generateRecordingId();

            // Update UI
            this.updateRecordingUI('recording');
            this.startRecordingTimer();

            // Trigger callback
            if (this.onRecordingStart) {
                this.onRecordingStart(this.currentRecordingId);
            }

            console.log('🎬 Recording started:', this.currentRecordingId);
            
            // Show notification about audio capture
            this.showRecordingNotification('🎤 Ghi hình với âm thanh đã bắt đầu!', 'success');

        } catch (error) {
            console.error('Failed to start recording:', error);
            
            // Reset recording state
            this.isRecording = false;
            this.mediaRecorder = null;
            this.updateRecordingUI('idle');
            
            // Show user-friendly error message
            let errorMessage = 'Không thể bắt đầu ghi hình. ';
            
            if (error.name === 'NotAllowedError') {
                errorMessage += 'Vui lòng cấp quyền chia sẻ màn hình và microphone để ghi âm thanh.';
            } else if (error.name === 'NotSupportedError') {
                errorMessage += 'Trình duyệt không hỗ trợ ghi hình.';
            } else if (error.message.includes('Video grid')) {
                errorMessage += 'Không tìm thấy video để ghi.';
            } else {
                errorMessage += 'Vui lòng thử lại.';
            }
            
            this.showRecordingNotification(errorMessage, 'error');
            this.handleRecordingError(error);
        }
    }

    async captureVideoGrid() {
        const videoGrid = document.getElementById('video-grid');
        
        if (!videoGrid) {
            throw new Error('Video grid element not found');
        }
        
        try {
            // Option 1: Try screen capture API first (most reliable)
            console.log('🎯 Attempting screen capture...');
            const screenStream = await navigator.mediaDevices.getDisplayMedia({
                video: {
                    mediaSource: 'screen',
                    frameRate: { ideal: 30, max: 60 },
                    width: { ideal: 1920, max: 1920 },
                    height: { ideal: 1080, max: 1080 }
                },
                audio: {
                    echoCancellation: true,
                    noiseSuppression: true,
                    sampleRate: 44100
                }
            });

            // Try to get microphone audio to combine with screen capture
            try {
                console.log('🎤 Attempting to capture microphone audio...');
                const micStream = await navigator.mediaDevices.getUserMedia({
                    audio: {
                        echoCancellation: true,
                        noiseSuppression: true,
                        sampleRate: 44100,
                        volume: 1.0
                    }
                });

                // Combine screen video with microphone audio
                const combinedStream = await this.combineVideoAndAudio(screenStream, micStream);
                console.log('✅ Screen capture + microphone audio successful');
                return combinedStream;

            } catch (micError) {
                console.warn('Microphone access failed, using screen audio only:', micError);
                return screenStream;
            }

        } catch (screenError) {
            console.warn('Screen capture failed:', screenError);
            
            try {
                // Option 2: Try to capture video grid directly + microphone
                if (videoGrid.captureStream && typeof videoGrid.captureStream === 'function') {
                    console.log('🎯 Attempting video grid captureStream...');
                    const videoStream = videoGrid.captureStream(30);
                    
                    if (videoStream && videoStream.getTracks().length > 0) {
                        // Try to add microphone audio
                        try {
                            const micStream = await navigator.mediaDevices.getUserMedia({
                                audio: {
                                    echoCancellation: true,
                                    noiseSuppression: true,
                                    sampleRate: 44100
                                }
                            });
                            
                            const combinedStream = await this.combineVideoAndAudio(videoStream, micStream);
                            console.log('✅ Video grid capture + microphone successful');
                            return combinedStream;
                            
                        } catch (micError) {
                            console.warn('Microphone access failed, using video only:', micError);
                            return videoStream;
                        }
                    }
                }
                
                // Option 3: Canvas-based capture as ultimate fallback
                console.log('🎯 Attempting canvas capture...');
                const canvasStream = this.createCanvasStream();
                
                if (canvasStream && canvasStream.getTracks().length > 0) {
                    // Try to add microphone audio to canvas
                    try {
                        const micStream = await navigator.mediaDevices.getUserMedia({
                            audio: {
                                echoCancellation: true,
                                noiseSuppression: true,
                                sampleRate: 44100
                            }
                        });
                        
                        const combinedStream = await this.combineVideoAndAudio(canvasStream, micStream);
                        console.log('✅ Canvas capture + microphone successful');
                        return combinedStream;
                        
                    } catch (micError) {
                        console.warn('Microphone access failed, using canvas video only:', micError);
                        return canvasStream;
                    }
                }
                
                throw new Error('All capture methods failed');
                
            } catch (fallbackError) {
                console.error('All capture methods failed:', fallbackError);
                throw new Error('Unable to capture video stream. Please try again or check your browser permissions.');
            }
        }
    }

    async combineVideoAndAudio(videoStream, audioStream) {
        try {
            const combinedStream = new MediaStream();
            
            // Add video tracks
            videoStream.getVideoTracks().forEach(track => {
                combinedStream.addTrack(track);
            });
            
            // Add audio tracks from microphone
            audioStream.getAudioTracks().forEach(track => {
                combinedStream.addTrack(track);
            });
            
            // Also add screen audio if available
            videoStream.getAudioTracks().forEach(track => {
                // Use Web Audio API to mix screen audio with microphone
                combinedStream.addTrack(track);
            });
            
            console.log('🔊 Combined stream tracks:', {
                videoTracks: combinedStream.getVideoTracks().length,
                audioTracks: combinedStream.getAudioTracks().length
            });
            
            return combinedStream;
            
        } catch (error) {
            console.error('Failed to combine streams:', error);
            // Fallback to video stream only
            return videoStream;
        }
    }

    checkStreamAudio(stream) {
        const audioTracks = stream.getAudioTracks();
        const videoTracks = stream.getVideoTracks();
        const audioIndicator = document.getElementById('audio-indicator');
        
        console.log('🔍 Stream analysis:', {
            audioTracks: audioTracks.length,
            videoTracks: videoTracks.length,
            audioEnabled: audioTracks.some(track => track.enabled),
            audioSettings: audioTracks.map(track => ({
                kind: track.kind,
                enabled: track.enabled,
                muted: track.muted,
                label: track.label
            }))
        });
        
        if (audioTracks.length === 0) {
            this.showRecordingNotification('⚠️ Không có âm thanh - chỉ ghi hình ảnh', 'warning');
            if (audioIndicator) {
                audioIndicator.innerHTML = '<i class="bi bi-mic-mute text-warning"></i><span>Không có âm thanh</span>';
            }
        } else if (audioTracks.length === 1) {
            this.showRecordingNotification('🎤 Ghi hình với microphone', 'info');
            if (audioIndicator) {
                audioIndicator.innerHTML = '<i class="bi bi-mic text-success"></i><span>Microphone</span>';
            }
        } else {
            this.showRecordingNotification('🔊 Ghi hình với nhiều nguồn âm thanh', 'info');
            if (audioIndicator) {
                audioIndicator.innerHTML = '<i class="bi bi-soundwave text-success"></i><span>Nhiều nguồn âm thanh</span>';
            }
        }
    }

    createCanvasStream() {
        try {
            const canvas = document.createElement('canvas');
            const ctx = canvas.getContext('2d');
            const videoGrid = document.getElementById('video-grid');
            
            if (!videoGrid) {
                throw new Error('Video grid not found for canvas capture');
            }
            
            // Set canvas size based on video grid
            const rect = videoGrid.getBoundingClientRect();
            canvas.width = Math.max(rect.width, 640); // Minimum 640px
            canvas.height = Math.max(rect.height, 480); // Minimum 480px

            // Validate canvas size
            if (canvas.width === 0 || canvas.height === 0) {
                throw new Error('Invalid canvas dimensions');
            }

            // Create a stream from canvas
            const stream = canvas.captureStream(30);
            
            if (!stream || stream.getTracks().length === 0) {
                throw new Error('Failed to create canvas stream');
            }

            // Draw initial frame
            this.drawVideoGridToCanvas(canvas, ctx, videoGrid);

            // Animate canvas to capture video grid
            const captureFrame = () => {
                if (!this.isRecording) return;

                try {
                    this.drawVideoGridToCanvas(canvas, ctx, videoGrid);
                    requestAnimationFrame(captureFrame);
                } catch (error) {
                    console.error('Error in canvas capture frame:', error);
                }
            };

            captureFrame();
            
            console.log('Canvas stream created:', { width: canvas.width, height: canvas.height });
            return stream;
            
        } catch (error) {
            console.error('Failed to create canvas stream:', error);
            throw error;
        }
    }

    drawVideoGridToCanvas(canvas, ctx, videoGrid) {
        // Clear canvas with background color
        ctx.fillStyle = getComputedStyle(videoGrid).backgroundColor || '#000000';
        ctx.fillRect(0, 0, canvas.width, canvas.height);

        // Draw video grid content
        const videos = videoGrid.querySelectorAll('video');
        const gridRect = videoGrid.getBoundingClientRect();
        
        videos.forEach((video) => {
            if (video.videoWidth > 0 && video.videoHeight > 0 && !video.paused) {
                try {
                    const videoRect = video.getBoundingClientRect();
                    
                    const x = videoRect.left - gridRect.left;
                    const y = videoRect.top - gridRect.top;
                    
                    // Ensure coordinates are within canvas bounds
                    if (x < canvas.width && y < canvas.height && 
                        x + videoRect.width > 0 && y + videoRect.height > 0) {
                        
                        ctx.drawImage(video, x, y, videoRect.width, videoRect.height);
                    }
                } catch (error) {
                    console.warn('Error drawing video to canvas:', error);
                }
            }
        });

        // Add timestamp overlay
        ctx.fillStyle = 'rgba(0, 0, 0, 0.7)';
        ctx.fillRect(10, 10, 150, 25);
        ctx.fillStyle = 'white';
        ctx.font = '12px Arial';
        ctx.fillText(new Date().toLocaleTimeString(), 15, 27);
    }

    getRecordingOptions() {
        const videoSettings = this.recordingSettings.video[this.currentQuality];
        const audioSettings = this.recordingSettings.audio[this.currentQuality];
        
        const options = {
            mimeType: 'video/mp4;codecs=h264,aac'
        };

        // Try different MIME types if the preferred one is not supported - prioritize MP4 with audio
        const mimeTypes = [
            'video/mp4;codecs=h264,aac',
            'video/webm;codecs=vp9,opus',
            'video/webm;codecs=vp8,opus',
            'video/mp4;codecs=h264',
            'video/mp4',
            'video/webm;codecs=vp9',
            'video/webm;codecs=vp8',
            'video/webm'
        ];

        for (let mimeType of mimeTypes) {
            if (MediaRecorder.isTypeSupported(mimeType)) {
                options.mimeType = mimeType;
                console.log('📹 Using MIME type:', mimeType);
                break;
            }
        }

        // Video bitrate settings
        if (videoSettings && videoSettings.videoBitsPerSecond > 0) {
            options.videoBitsPerSecond = videoSettings.videoBitsPerSecond;
        }
        
        // Audio bitrate settings - IMPORTANT for audio quality
        if (audioSettings && audioSettings.audioBitsPerSecond > 0) {
            options.audioBitsPerSecond = audioSettings.audioBitsPerSecond;
        }

        // Additional MediaRecorder options for better quality
        if (this.currentQuality !== 'audioOnly') {
            options.bitsPerSecond = (videoSettings.videoBitsPerSecond || 0) + (audioSettings.audioBitsPerSecond || 0);
        }

        console.log('🎛️ Recording options:', options);
        return options;
    }

    togglePauseRecording() {
        if (!this.isRecording || !this.mediaRecorder) return;

        if (this.isPaused) {
            this.mediaRecorder.resume();
            this.isPaused = false;
            this.updateRecordingUI('recording');
            this.startRecordingTimer();
            
            if (this.onRecordingResume) {
                this.onRecordingResume(this.currentRecordingId);
            }
            
            console.log('🎬 Recording resumed');
        } else {
            this.mediaRecorder.pause();
            this.isPaused = true;
            this.updateRecordingUI('paused');
            this.stopRecordingTimer();
            
            if (this.onRecordingPause) {
                this.onRecordingPause(this.currentRecordingId);
            }
            
            console.log('⏸️ Recording paused');
        }
    }

    stopRecording() {
        if (!this.isRecording || !this.mediaRecorder) return;

        try {
            this.recordingStopTime = new Date(); // Set stop time
            this.mediaRecorder.stop();
            this.isRecording = false;
            this.isPaused = false;
            
            // Stop all tracks
            if (this.mediaRecorder.stream) {
                this.mediaRecorder.stream.getTracks().forEach(track => track.stop());
            }

            this.updateRecordingUI('stopped');
            this.stopRecordingTimer();

            console.log('🛑 Recording stopped');

        } catch (error) {
            console.error('Error stopping recording:', error);
            this.handleRecordingError(error);
        }
    }

    async handleRecordingComplete() {
        try {
            if (this.recordedChunks.length === 0) {
                throw new Error('No recorded data available');
            }

            // Create blob from recorded chunks with proper content type
            const originalMimeType = this.mediaRecorder.mimeType || 'video/webm';
            let fixedMimeType = originalMimeType;
            
            // Fix content type to match backend allowed types
            if (originalMimeType.includes('mp4')) {
                fixedMimeType = 'video/mp4';
            } else if (originalMimeType.includes('webm')) {
                fixedMimeType = 'video/webm';
            } else {
                // Fallback to webm for unknown types
                fixedMimeType = 'video/webm';
            }
            
            const blob = new Blob(this.recordedChunks, {
                type: fixedMimeType
            });

            console.log('🔍 Blob debug info:', {
                originalMimeType: originalMimeType,
                fixedMimeType: fixedMimeType,
                blobType: blob.type,
                blobSize: blob.size,
                recordedChunksLength: this.recordedChunks.length
            });

            // Collect technical metadata
            const videoSettings = this.recordingSettings.video[this.currentQuality];
            const audioSettings = this.recordingSettings.audio[this.currentQuality];
            const durationSeconds = this.getRecordingDuration();
            
            const metadata = {
                // Video specs
                resolution: `${videoSettings.width}x${videoSettings.height}`,
                frameRate: videoSettings.frameRate,
                videoBitrate: videoSettings.videoBitrate,
                videoCodec: this.extractCodec(this.mediaRecorder.mimeType, 'video'),
                
                // Audio specs  
                audioBitrate: audioSettings.audioBitrate,
                audioCodec: this.extractCodec(this.mediaRecorder.mimeType, 'audio'),
                sampleRate: audioSettings.sampleRate || 44100,
                
                // Recording info
                quality: this.currentQuality,
                mimeType: this.mediaRecorder.mimeType,
                recordingMethod: 'MediaRecorder API',
                browserInfo: this.getBrowserInfo(),
                
                // Performance metrics
                chunks: this.recordedChunks.length,
                averageChunkSize: Math.round(blob.size / this.recordedChunks.length),
                
                // Timing
                startTime: this.recordingStartTime,
                endTime: this.recordingStopTime,
                actualDuration: durationSeconds
            };

            // Generate recording metadata
            const recording = {
                id: this.currentRecordingId,
                filename: this.generateFilename('recording'),
                blob: blob,
                size: blob.size,
                duration: durationSeconds,
                timestamp: this.recordingStartTime,
                quality: this.currentQuality,
                mimeType: this.mediaRecorder.mimeType,
                metadata: metadata
            };

            // Generate thumbnail for video
            recording.thumbnail = await this.generateThumbnail(recording);

            // Upload thumbnail if exists
            if (recording.thumbnail) {
                const thumbnailFile = {
                    blob: recording.thumbnail.blob,
                    filename: this.generateFilename('thumbnail').replace(/\.[^/.]+$/, ".jpg"), // Force .jpg extension
                    size: recording.thumbnail.blob.size
                };
                
                try {
                    await this.uploadToCloud(thumbnailFile);
                    recording.thumbnailUrl = thumbnailFile.cloudUrl;
                } catch (error) {
                    console.warn('Failed to upload thumbnail:', error);
                }
            }

            // Save recording
            await this.saveRecording(recording);

            // Add to history
            this.addToHistory(recording);

            // Trigger callback
            if (this.onRecordingStop) {
                this.onRecordingStop(recording);
            }

            // Show success notification
            this.showRecordingNotification('✅ Ghi hình hoàn tất!', 'success');

        } catch (error) {
            console.error('Error handling recording completion:', error);
            this.handleRecordingError(error);
        }
    }

    // ======== SCREENSHOT FUNCTIONS ========
    async takeScreenshot(type = 'all') {
        try {
            let canvas;

            switch (type) {
                case 'all':
                    canvas = await this.captureVideoGridToCanvas();
                    break;
                case 'self':
                    canvas = await this.captureSelfVideo();
                    break;
                case 'participant':
                    canvas = await this.captureParticipantSelection();
                    break;
                default:
                    canvas = await this.captureVideoGridToCanvas();
            }

            if (!canvas) {
                throw new Error('Failed to capture screenshot');
            }

            // Convert canvas to blob
            const blob = await this.canvasToBlob(canvas);
            
            // Generate screenshot metadata
            const screenshot = {
                id: this.generateScreenshotId(),
                filename: this.generateFilename('screenshot'),
                blob: blob,
                size: blob.size,
                timestamp: new Date(),
                type: type,
                format: this.screenshotSettings.format
            };

            // Save screenshot
            await this.saveScreenshot(screenshot);

            // Show preview
            this.showScreenshotPreview(screenshot);

            // Trigger callback
            if (this.onScreenshot) {
                this.onScreenshot(screenshot);
            }

            console.log('📸 Screenshot captured:', screenshot.id);

        } catch (error) {
            console.error('Failed to take screenshot:', error);
            this.handleRecordingError(error);
        }
    }

    async captureVideoGridToCanvas() {
        const videoGrid = document.getElementById('video-grid');
        const canvas = document.createElement('canvas');
        const ctx = canvas.getContext('2d');

        // Get video grid dimensions
        const rect = videoGrid.getBoundingClientRect();
        canvas.width = rect.width;
        canvas.height = rect.height;

        // Capture background
        ctx.fillStyle = getComputedStyle(videoGrid).backgroundColor || '#f0f0f0';
        ctx.fillRect(0, 0, canvas.width, canvas.height);

        // Capture all video elements
        const videos = videoGrid.querySelectorAll('video');
        
        for (let video of videos) {
            if (video.videoWidth > 0 && video.videoHeight > 0) {
                const videoRect = video.getBoundingClientRect();
                const x = videoRect.left - rect.left;
                const y = videoRect.top - rect.top;
                
                ctx.drawImage(video, x, y, videoRect.width, videoRect.height);
                
                // Add user info overlay if exists
                const userInfo = video.parentElement.querySelector('.user-info');
                if (userInfo) {
                    ctx.fillStyle = 'rgba(0, 0, 0, 0.7)';
                    ctx.fillRect(x + 10, y + videoRect.height - 30, userInfo.offsetWidth + 10, 20);
                    
                    ctx.fillStyle = 'white';
                    ctx.font = '12px Arial';
                    ctx.fillText(userInfo.textContent, x + 15, y + videoRect.height - 15);
                }
            }
        }

        return canvas;
    }

    async captureSelfVideo() {
        const selfVideo = document.querySelector('#video-grid video[data-user="self"]') ||
                          document.querySelector('#video-grid .video-container:first-child video');
        
        if (!selfVideo || selfVideo.videoWidth === 0) {
            throw new Error('Self video not found or not ready');
        }

        const canvas = document.createElement('canvas');
        const ctx = canvas.getContext('2d');
        
        canvas.width = selfVideo.videoWidth;
        canvas.height = selfVideo.videoHeight;
        
        ctx.drawImage(selfVideo, 0, 0);
        
        return canvas;
    }

    async captureParticipantSelection() {
        // Show participant selection dialog
        const participantVideos = document.querySelectorAll('#video-grid video:not([data-user="self"])');
        
        if (participantVideos.length === 0) {
            throw new Error('No participants found');
        }

        // For now, capture the first participant
        // TODO: Implement participant selection dialog
        const video = participantVideos[0];
        const canvas = document.createElement('canvas');
        const ctx = canvas.getContext('2d');
        
        canvas.width = video.videoWidth;
        canvas.height = video.videoHeight;
        
        ctx.drawImage(video, 0, 0);
        
        return canvas;
    }

    async canvasToBlob(canvas) {
        return new Promise((resolve) => {
            canvas.toBlob((blob) => {
                resolve(blob);
            }, `image/${this.screenshotSettings.format}`, this.screenshotSettings.quality);
        });
    }

    // ======== FILE MANAGEMENT ========
    async saveRecording(recording) {
        try {
            // Save locally if enabled
            if (this.storageSettings.saveLocally) {
                await this.saveFileLocally(recording);
            }

            // Upload to cloud if enabled
            if (this.storageSettings.uploadToCloud) {
                await this.uploadToCloud(recording);
            }

        } catch (error) {
            console.error('Error saving recording:', error);
            throw error;
        }
    }

    async saveScreenshot(screenshot) {
        try {
            // Save locally if enabled
            if (this.storageSettings.saveLocally) {
                await this.saveFileLocally(screenshot);
            }

            // Upload to cloud if enabled
            if (this.storageSettings.uploadToCloud) {
                await this.uploadToCloud(screenshot);
            }

        } catch (error) {
            console.error('Error saving screenshot:', error);
            throw error;
        }
    }

    async saveFileLocally(file) {
        // Create download link
        const url = URL.createObjectURL(file.blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = file.filename;
        
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        
        // Clean up URL
        setTimeout(() => URL.revokeObjectURL(url), 1000);
        
        console.log('💾 File saved locally:', file.filename);
    }

    async uploadToCloud(file) {
        try {
            // Upload using the backend API endpoint
            const formData = new FormData();
            formData.append('file', file.blob, file.filename);
            formData.append('type', file.duration ? 'recording' : 'screenshot');
            
            // Get meeting code from URL or video grid
            const meetingCode = this.getMeetingCode();
            formData.append('meetingCode', meetingCode);
            
            // Add duration and metadata for recordings
            if (file.duration) {
                formData.append('duration', file.duration);
                if (file.metadata) {
                    formData.append('metadata', JSON.stringify(file.metadata));
                }
                if (file.thumbnailUrl) {
                    formData.append('thumbnailUrl', file.thumbnailUrl);
                }
            }
            
            // Let backend auto-detect active session for this meeting
            // No need to send sessionId from frontend
            
            console.log('📤 Starting upload to cloud...', {
                filename: file.filename,
                size: file.blob.size,
                type: file.blob.type,
                contentType: file.blob.type,
                isRecording: !!file.duration,
                meetingCode: meetingCode,
                autoDetectSession: true,
                formDataEntries: Array.from(formData.entries()).map(([key, value]) => 
                    key === 'file' ? [key, `File(${value.name}, ${value.size}bytes, ${value.type})`] : [key, value]
                )
            });

            const response = await fetch('/Meeting/UploadRecording', {
                method: 'POST',
                body: formData,
                credentials: 'same-origin' // Include session cookies
            });
            
            if (!response.ok) {
                const errorText = await response.text();
                console.error('❌ Upload response error:', errorText);
                
                try {
                    const errorData = JSON.parse(errorText);
                    throw new Error(errorData.error || `HTTP ${response.status}: ${response.statusText}`);
                } catch (parseError) {
                    throw new Error(`HTTP ${response.status}: ${errorText || response.statusText}`);
                }
            }
            
            const result = await response.json();
            console.log('✅ Upload response:', result);
            
            if (result.success) {
                file.cloudUrl = result.url;
                console.log('☁️ File uploaded to cloud:', result.url);
            } else {
                throw new Error(result.error || 'Upload was not successful');
            }
            
        } catch (error) {
            console.warn('Cloud upload failed:', error);
            
            // Show user-friendly error message
            this.showRecordingNotification(
                `⚠️ Lỗi tải lên cloud: ${error.message}. File vẫn được lưu cục bộ.`, 
                'warning'
            );
            
            // Don't throw error, just log it so local save still works
        }
    }

    getMeetingCode() {
        // Try to get meeting code from video grid data attribute
        const videoGrid = document.getElementById('video-grid');
        if (videoGrid && videoGrid.dataset.meetingCode) {
            return videoGrid.dataset.meetingCode;
        }
        
        // Fallback: try to get from URL
        const urlParams = new URLSearchParams(window.location.search);
        const codeParam = urlParams.get('code');
        if (codeParam) {
            return codeParam;
        }
        
        // Fallback: try to get from URL path
        const pathMatch = window.location.pathname.match(/\/Room\/(.+)$/);
        if (pathMatch) {
            return pathMatch[1];
        }
        
        // Final fallback: generate a default identifier
        return 'unknown-meeting';
    }

    // ======== UI UPDATES ========
    updateRecordingUI(state) {
        const startBtn = document.getElementById('start-recording-btn');
        const pauseBtn = document.getElementById('pause-recording-btn');
        const stopBtn = document.getElementById('stop-recording-btn');
        const indicator = document.getElementById('recording-indicator');

        switch (state) {
            case 'recording':
                startBtn.disabled = true;
                pauseBtn.disabled = false;
                stopBtn.disabled = false;
                
                indicator.innerHTML = '<i class="bi bi-record-circle recording-pulse"></i><span>Đang ghi hình...</span>';
                indicator.className = 'status-indicator recording';
                
                pauseBtn.innerHTML = '<i class="bi bi-pause-circle"></i><span>Tạm dừng</span>';
                break;

            case 'paused':
                pauseBtn.innerHTML = '<i class="bi bi-play-circle"></i><span>Tiếp tục</span>';
                indicator.innerHTML = '<i class="bi bi-pause-circle"></i><span>Đã tạm dừng</span>';
                indicator.className = 'status-indicator paused';
                break;

            case 'stopped':
                startBtn.disabled = false;
                pauseBtn.disabled = true;
                stopBtn.disabled = true;
                
                indicator.innerHTML = '<i class="bi bi-circle"></i><span>Sẵn sàng ghi hình</span>';
                indicator.className = 'status-indicator ready';
                
                pauseBtn.innerHTML = '<i class="bi bi-pause-circle"></i><span>Tạm dừng</span>';
                break;
        }
    }

    startRecordingTimer() {
        this.stopRecordingTimer(); // Clear any existing timer
        
        this.recordingTimer = setInterval(() => {
            const duration = this.getRecordingDuration();
            const timerElement = document.getElementById('recording-timer');
            if (timerElement) {
                timerElement.textContent = this.formatDuration(duration);
            }
        }, 1000);
    }

    stopRecordingTimer() {
        if (this.recordingTimer) {
            clearInterval(this.recordingTimer);
            this.recordingTimer = null;
        }
    }

    getRecordingDuration() {
        if (!this.recordingStartTime) return 0;
        return Math.floor((new Date() - this.recordingStartTime) / 1000);
    }

    formatDuration(seconds) {
        const hours = Math.floor(seconds / 3600);
        const minutes = Math.floor((seconds % 3600) / 60);
        const secs = seconds % 60;
        
        return `${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
    }

    showRecordingNotification(message, type = 'info') {
        // Integration with existing notification system
        if (window.showError) {
            window.showError(message, true);
        } else {
            console.log(message);
        }
    }

    showScreenshotPreview(screenshot) {
        // Create preview modal
        const modal = document.createElement('div');
        modal.className = 'screenshot-preview-modal';
        modal.innerHTML = `
            <div class="preview-content">
                <div class="preview-header">
                    <h6>📸 Screenshot đã chụp</h6>
                    <button class="close-btn" onclick="this.parentElement.parentElement.parentElement.remove()">×</button>
                </div>
                <div class="preview-image">
                    <img src="${URL.createObjectURL(screenshot.blob)}" alt="Screenshot" />
                </div>
                <div class="preview-actions">
                    <button class="btn-download" onclick="window.recordingSystem.downloadFile('${screenshot.id}')">
                        <i class="bi bi-download"></i> Tải xuống
                    </button>
                    <button class="btn-share" onclick="window.recordingSystem.shareFile('${screenshot.id}')">
                        <i class="bi bi-share"></i> Chia sẻ
                    </button>
                </div>
            </div>
        `;
        
        document.body.appendChild(modal);
        
        // Auto-remove after 10 seconds
        setTimeout(() => {
            if (modal.parentElement) {
                modal.remove();
            }
        }, 10000);
    }

    // ======== HISTORY MANAGEMENT ========
    addToHistory(file) {
        const historyItem = {
            id: file.id,
            filename: file.filename,
            size: file.size,
            timestamp: file.timestamp,
            type: file.duration ? 'recording' : 'screenshot',
            duration: file.duration || null,
            cloudUrl: file.cloudUrl || null
        };

        this.recordingHistory.unshift(historyItem);
        
        // Limit history size
        if (this.recordingHistory.length > this.storageSettings.maxLocalFiles) {
            this.recordingHistory = this.recordingHistory.slice(0, this.storageSettings.maxLocalFiles);
        }

        this.saveRecordingHistory();
        this.updateHistoryUI();
    }

    updateHistoryUI() {
        const historyList = document.getElementById('recording-history-list');
        const historyCount = document.getElementById('history-count');
        
        if (!historyList || !historyCount) return;

        historyCount.textContent = `(${this.recordingHistory.length})`;

        if (this.recordingHistory.length === 0) {
            historyList.innerHTML = `
                <div class="empty-history">
                    <i class="bi bi-folder2-open"></i>
                    <span>Chưa có bản ghi nào</span>
                </div>
            `;
            return;
        }

        historyList.innerHTML = this.recordingHistory.map(item => `
            <div class="history-item" data-id="${item.id}">
                <div class="item-icon">
                    <i class="bi bi-${item.type === 'recording' ? 'film' : 'image'}"></i>
                </div>
                <div class="item-details">
                    <div class="item-name">${item.filename}</div>
                    <div class="item-meta">
                        ${new Date(item.timestamp).toLocaleString()} • ${this.formatFileSize(item.size)}
                        ${item.duration ? ` • ${this.formatDuration(item.duration)}` : ''}
                    </div>
                </div>
                <div class="item-actions">
                    <button class="action-btn" onclick="window.recordingSystem.downloadFile('${item.id}')" title="Tải xuống">
                        <i class="bi bi-download"></i>
                    </button>
                    <button class="action-btn" onclick="window.recordingSystem.shareFile('${item.id}')" title="Chia sẻ">
                        <i class="bi bi-share"></i>
                    </button>
                    <button class="action-btn danger" onclick="window.recordingSystem.deleteFile('${item.id}')" title="Xóa">
                        <i class="bi bi-trash"></i>
                    </button>
                </div>
            </div>
        `).join('');
    }

    // ======== UTILITY FUNCTIONS ========
    generateRecordingId() {
        return 'rec_' + Date.now() + '_' + Math.random().toString(36).substr(2, 9);
    }

    generateScreenshotId() {
        return 'shot_' + Date.now() + '_' + Math.random().toString(36).substr(2, 9);
    }

    generateFilename(type) {
        const timestamp = new Date().toISOString().replace(/[:.]/g, '-').slice(0, 19);
        let extension;
        
        if (type === 'recording') {
            // Determine extension based on current recording mime type
            const mimeType = this.mediaRecorder?.mimeType || 'video/mp4';
            if (mimeType.includes('mp4')) {
                extension = 'mp4';
            } else if (mimeType.includes('webm')) {
                extension = 'webm';
            } else {
                extension = 'mp4'; // Default to mp4
            }
        } else {
            extension = this.screenshotSettings.format;
        }
        
        return `zela-${type}-${timestamp}.${extension}`;
    }

    // ======== THUMBNAIL GENERATION ========
    async generateThumbnail(recording) {
        try {
            // Create video element to extract frame
            const video = document.createElement('video');
            video.preload = 'metadata';
            video.muted = true;
            
            // Wait for video to load
            await new Promise((resolve, reject) => {
                video.onloadeddata = resolve;
                video.onerror = reject;
                video.src = URL.createObjectURL(recording.blob);
            });

            // Seek to middle of video for thumbnail
            const seekTime = Math.min(recording.duration / 2, 5); // Max 5 seconds in
            video.currentTime = seekTime;

            // Wait for seek to complete
            await new Promise((resolve) => {
                video.onseeked = resolve;
            });

            // Create canvas and draw video frame
            const canvas = document.createElement('canvas');
            const ctx = canvas.getContext('2d');
            
            // Set thumbnail size (maintain aspect ratio)
            const maxWidth = 320;
            const maxHeight = 180;
            const aspectRatio = video.videoWidth / video.videoHeight;
            
            if (aspectRatio > maxWidth / maxHeight) {
                canvas.width = maxWidth;
                canvas.height = maxWidth / aspectRatio;
            } else {
                canvas.width = maxHeight * aspectRatio;
                canvas.height = maxHeight;
            }

            ctx.drawImage(video, 0, 0, canvas.width, canvas.height);

            // Convert to blob
            const thumbnailBlob = await new Promise((resolve) => {
                canvas.toBlob(resolve, 'image/jpeg', 0.8);
            });

            // Clean up
            URL.revokeObjectURL(video.src);

            return {
                blob: thumbnailBlob,
                url: URL.createObjectURL(thumbnailBlob),
                width: canvas.width,
                height: canvas.height
            };

        } catch (error) {
            console.warn('Failed to generate thumbnail:', error);
            return null;
        }
    }

    // ======== UTILITY FUNCTIONS ========
    extractCodec(mimeType, type) {
        try {
            if (!mimeType || !mimeType.includes('codecs=')) {
                return type === 'video' ? 'unknown' : 'unknown';
            }
            
            const codecsMatch = mimeType.match(/codecs=([^;]+)/);
            if (!codecsMatch) return 'unknown';
            
            const codecs = codecsMatch[1].replace(/"/g, '').split(',');
            
            if (type === 'video') {
                // Common video codecs
                const videoCodec = codecs.find(c => 
                    c.includes('h264') || c.includes('vp8') || c.includes('vp9') || 
                    c.includes('av01') || c.includes('avc1')
                );
                return videoCodec ? videoCodec.trim() : 'unknown';
            } else {
                // Common audio codecs
                const audioCodec = codecs.find(c => 
                    c.includes('opus') || c.includes('aac') || c.includes('mp3') ||
                    c.includes('vorbis') || c.includes('pcm')
                );
                return audioCodec ? audioCodec.trim() : 'unknown';
            }
        } catch (error) {
            console.warn('Failed to extract codec:', error);
            return 'unknown';
        }
    }

    getBrowserInfo() {
        try {
            const userAgent = navigator.userAgent;
            let browser = 'unknown';
            let version = 'unknown';
            
            if (userAgent.includes('Chrome') && !userAgent.includes('Edg')) {
                browser = 'Chrome';
                version = userAgent.match(/Chrome\/([0-9.]+)/)?.[1] || 'unknown';
            } else if (userAgent.includes('Firefox')) {
                browser = 'Firefox';
                version = userAgent.match(/Firefox\/([0-9.]+)/)?.[1] || 'unknown';
            } else if (userAgent.includes('Safari') && !userAgent.includes('Chrome')) {
                browser = 'Safari';
                version = userAgent.match(/Version\/([0-9.]+)/)?.[1] || 'unknown';
            } else if (userAgent.includes('Edg')) {
                browser = 'Edge';
                version = userAgent.match(/Edg\/([0-9.]+)/)?.[1] || 'unknown';
            }
            
            return {
                browser: browser,
                version: version,
                platform: navigator.platform,
                userAgent: userAgent
            };
        } catch (error) {
            console.warn('Failed to get browser info:', error);
            return {
                browser: 'unknown',
                version: 'unknown',
                platform: 'unknown',
                userAgent: navigator.userAgent || 'unknown'
            };
        }
    }

    formatFileSize(bytes) {
        if (bytes === 0) return '0 Bytes';
        const k = 1024;
        const sizes = ['Bytes', 'KB', 'MB', 'GB'];
        const i = Math.floor(Math.log(bytes) / Math.log(k));
        return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
    }

    setRecordingQuality(quality) {
        this.currentQuality = quality;
        this.saveRecordingPreferences();
        console.log('🎥 Recording quality set to:', quality);
    }

    setScreenshotFormat(format) {
        this.screenshotSettings.format = format;
        this.saveRecordingPreferences();
        console.log('📸 Screenshot format set to:', format);
    }

    handleRecordingError(error) {
        console.error('Recording error:', error);
        
        // Reset recording state
        this.isRecording = false;
        this.isPaused = false;
        this.updateRecordingUI('stopped');
        this.stopRecordingTimer();

        // Show error notification
        this.showRecordingNotification('❌ Lỗi ghi hình: ' + error.message, 'error');
        
        if (this.onError) {
            this.onError(error);
        }
    }

    // ======== PREFERENCES ========
    saveRecordingPreferences() {
        try {
            const preferences = {
                quality: this.currentQuality,
                screenshotSettings: this.screenshotSettings,
                storageSettings: this.storageSettings
            };
            localStorage.setItem('zela-recording-preferences', JSON.stringify(preferences));
        } catch (error) {
            console.warn('Failed to save recording preferences:', error);
        }
    }

    loadRecordingPreferences() {
        try {
            const preferences = JSON.parse(localStorage.getItem('zela-recording-preferences') || '{}');
            
            if (preferences.quality) {
                this.setRecordingQuality(preferences.quality);
                document.getElementById('recording-quality-select').value = preferences.quality;
            }
            
            if (preferences.screenshotSettings) {
                Object.assign(this.screenshotSettings, preferences.screenshotSettings);
                document.getElementById('screenshot-format-select').value = this.screenshotSettings.format;
            }
            
            if (preferences.storageSettings) {
                Object.assign(this.storageSettings, preferences.storageSettings);
                document.getElementById('upload-cloud-checkbox').checked = this.storageSettings.uploadToCloud;
                document.getElementById('save-local-checkbox').checked = this.storageSettings.saveLocally;
                document.getElementById('auto-delete-checkbox').checked = this.storageSettings.autoDelete;
            }
            
        } catch (error) {
            console.warn('Failed to load recording preferences:', error);
        }
    }

    saveRecordingHistory() {
        try {
            localStorage.setItem('zela-recordings', JSON.stringify(this.recordingHistory));
        } catch (error) {
            console.warn('Failed to save recording history:', error);
        }
    }

    // ======== PUBLIC API ========
    async downloadFile(fileId) {
        const file = this.recordingHistory.find(item => item.id === fileId);
        if (!file || !file.cloudUrl) {
            console.warn('File not found or no download URL:', fileId);
            this.showRecordingNotification('❌ File không tồn tại hoặc không có URL tải xuống', 'error');
            return;
        }

        try {
            // Create a temporary download link with proper download attribute
            const downloadLink = document.createElement('a');
            downloadLink.href = file.cloudUrl;
            downloadLink.download = file.filename || 'download';
            // Don't use target="_blank" to avoid opening new tab
            
            // Start automatic download monitoring
            this.startDownloadMonitoring(fileId, file.filename);
            
            // Force download without opening new tab
            document.body.appendChild(downloadLink);
            downloadLink.click();
            document.body.removeChild(downloadLink);
            
            this.showRecordingNotification('📥 Bắt đầu tải xuống...', 'success');
            
        } catch (error) {
            console.error('Download failed:', error);
            this.showRecordingNotification('❌ Lỗi tải xuống: ' + error.message, 'error');
        }
    }

    startDownloadMonitoring(fileId, filename) {
        // Monitor page visibility and focus to detect if download started
        let downloadDetected = false;
        let timeoutId = null;
        
        const cleanup = () => {
            if (timeoutId) clearTimeout(timeoutId);
            document.removeEventListener('visibilitychange', visibilityHandler);
            window.removeEventListener('focus', focusHandler);
            window.removeEventListener('blur', blurHandler);
        };
        
        const detectSuccess = () => {
            if (!downloadDetected) {
                downloadDetected = true;
                cleanup();
                this.showRecordingNotification(`✅ Tải xuống "${filename}" thành công!`, 'success');
            }
        };
        
        const detectFailure = () => {
            if (!downloadDetected) {
                downloadDetected = true;
                cleanup();
                this.showRecordingNotification(`❌ Tải xuống "${filename}" thất bại`, 'error');
            }
        };
        
        // Monitor visibility changes (user might switch to Downloads folder)
        const visibilityHandler = () => {
            if (document.hidden) {
                // User might be checking downloads - assume success
                setTimeout(detectSuccess, 2000);
            }
        };
        
        // Monitor window focus (download dialog might steal focus)
        const blurHandler = () => {
            // Window lost focus - might be download dialog
            setTimeout(() => {
                if (document.hasFocus()) {
                    detectSuccess();
                }
            }, 1000);
        };
        
        const focusHandler = () => {
            // Window regained focus after download dialog
            setTimeout(detectSuccess, 500);
        };
        
        document.addEventListener('visibilitychange', visibilityHandler);
        window.addEventListener('focus', focusHandler);
        window.addEventListener('blur', blurHandler);
        
        // Fallback: assume failure if no indicators after 15 seconds
        timeoutId = setTimeout(detectFailure, 15000);
    }

    shareFile(fileId) {
        const file = this.recordingHistory.find(item => item.id === fileId);
        if (file && file.cloudUrl) {
            navigator.clipboard.writeText(file.cloudUrl);
            this.showRecordingNotification('📋 Link đã được copy!', 'success');
        } else {
            console.warn('File not found or no share URL:', fileId);
        }
    }

    deleteFile(fileId) {
        const index = this.recordingHistory.findIndex(item => item.id === fileId);
        if (index !== -1) {
            this.recordingHistory.splice(index, 1);
            this.saveRecordingHistory();
            this.updateHistoryUI();
            this.showRecordingNotification('🗑️ File đã được xóa', 'info');
        }
    }

    destroy() {
        this.stopRecording();
        this.stopRecordingTimer();
        
        const panel = document.getElementById('recording-control-panel');
        if (panel) {
            panel.remove();
        }
        
        console.log('🎬 Recording System destroyed');
    }
}

// ======== GLOBAL INSTANCE ========
window.recordingSystem = null;

// ======== INITIALIZATION FUNCTION ========
function initializeRecordingSystem() {
    if (!window.recordingSystem) {
        window.recordingSystem = new RecordingSystem();
    }
    return window.recordingSystem;
}

// ======== EXPORT FOR MODULE USAGE ========
if (typeof module !== 'undefined' && module.exports) {
    module.exports = { RecordingSystem, initializeRecordingSystem };
} 