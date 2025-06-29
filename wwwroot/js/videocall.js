// ======== CẤU HÌNH STUN/TURN ========
const ICE_SERVERS = [
    { urls: 'stun:stun.l.google.com:19302' },
    { urls: 'stun:stun1.l.google.com:19302' },
    { urls: 'stun:stun2.l.google.com:19302' },
    {
        urls: 'turn:YOUR_TURN_SERVER:3478',
        username: 'TURN_USERNAME',
        credential: 'TURN_CREDENTIAL'
    }
];

// ======== BIẾN TOÀN CỤC ========
let localStream;         // camera + mic
let screenStream = null; // stream chia sẻ màn hình
const peers = {};        // lưu các SimplePeer theo peerId
let connectionState = 'disconnected'; // Connection state tracking
let retryCount = 0;      // Retry counter
const MAX_RETRIES = 5;   // Maximum retry attempts
let currentUserId = null; // Current user ID for tracking

// ======== CONNECTION LOCK ========
let isConnecting = false; // Prevent race conditions

// ======== QUALITY CONTROL INTEGRATION ========
let qualityController = null; // Will be set from quality-control.js

// ======== ERROR TYPES ========
const ERROR_TYPES = {
    MEDIA_ACCESS_DENIED: 'MEDIA_ACCESS_DENIED',
    MEDIA_NOT_FOUND: 'MEDIA_NOT_FOUND',
    SIGNALR_CONNECTION_FAILED: 'SIGNALR_CONNECTION_FAILED',
    PEER_CONNECTION_FAILED: 'PEER_CONNECTION_FAILED',
    SCREEN_SHARE_FAILED: 'SCREEN_SHARE_FAILED',
    MEETING_NOT_FOUND: 'MEETING_NOT_FOUND',
    NETWORK_ERROR: 'NETWORK_ERROR'
};

// ======== ERROR MESSAGES ========
const ERROR_MESSAGES = {
    [ERROR_TYPES.MEDIA_ACCESS_DENIED]: 'Vui lòng cho phép truy cập camera và microphone để tham gia cuộc họp.',
    [ERROR_TYPES.MEDIA_NOT_FOUND]: 'Không tìm thấy camera hoặc microphone. Kiểm tra thiết bị của bạn.',
    [ERROR_TYPES.SIGNALR_CONNECTION_FAILED]: 'Không thể kết nối đến server. Kiểm tra kết nối mạng.',
    [ERROR_TYPES.PEER_CONNECTION_FAILED]: 'Mất kết nối với người tham gia khác. Đang thử kết nối lại...',
    [ERROR_TYPES.SCREEN_SHARE_FAILED]: 'Không thể chia sẻ màn hình. Thử lại sau.',
    [ERROR_TYPES.MEETING_NOT_FOUND]: 'Phòng họp không tồn tại hoặc đã kết thúc.',
    [ERROR_TYPES.NETWORK_ERROR]: 'Lỗi kết nối mạng. Đang thử kết nối lại...'
};

// ======== KHỞI TẠO KẾT NỐI SIGNALR ========
const connection = new signalR.HubConnectionBuilder()
    .withUrl('/meetingHub')
    .withAutomaticReconnect([0, 2000, 10000, 30000])
    .build();

// Expose connection globally for other components
window.connection = connection;

// ======== GET USER ID ========
function getCurrentUserId() {
    // Try to get from session or any available source
    const userIdElement = document.querySelector('[data-user-id]');
    if (userIdElement) {
        return parseInt(userIdElement.getAttribute('data-user-id'));
    }
    
    // Alternative: check if it's stored somewhere else
    const sessionUserId = sessionStorage.getItem('userId');
    if (sessionUserId) {
        return parseInt(sessionUserId);
    }
    
    // Fallback: generate a temporary ID (not ideal for production)
    console.warn('User ID not found, using temporary ID');
    return Math.floor(Math.random() * 1000000);
}

// ======== UI HELPERS ========
function showLoading(message = 'Đang tải...') {
    const loadingDiv = document.getElementById('loading-overlay') || createLoadingOverlay();
    loadingDiv.querySelector('.loading-message').textContent = message;
    loadingDiv.style.display = 'flex';
}

function hideLoading() {
    const loadingDiv = document.getElementById('loading-overlay');
    if (loadingDiv) {
        loadingDiv.style.display = 'none';
    }
}

function createLoadingOverlay() {
    const overlay = document.createElement('div');
    overlay.id = 'loading-overlay';
    overlay.innerHTML = `
        <div class="loading-content">
            <div class="loading-spinner"></div>
            <div class="loading-message">Đang tải...</div>
        </div>
    `;
    overlay.style.cssText = `
        position: fixed; top: 0; left: 0; width: 100%; height: 100%;
        background: rgba(0,0,0,0.8); display: flex; align-items: center;
        justify-content: center; z-index: 9999; color: white;
    `;
    document.body.appendChild(overlay);
    return overlay;
}

function showError(message, isTemporary = false) {
    const errorDiv = document.getElementById('error-notification') || createErrorNotification();
    errorDiv.querySelector('.error-message').textContent = message;
    errorDiv.style.display = 'block';
    
    if (isTemporary) {
        setTimeout(() => hideError(), 5000);
    }
}

function hideError() {
    const errorDiv = document.getElementById('error-notification');
    if (errorDiv) {
        errorDiv.style.display = 'none';
    }
}

function createErrorNotification() {
    const errorDiv = document.createElement('div');
    errorDiv.id = 'error-notification';
    errorDiv.innerHTML = `
        <div class="error-content">
            <i class="error-icon">⚠️</i>
            <span class="error-message"></span>
            <button class="error-close" onclick="hideError()">×</button>
        </div>
    `;
    errorDiv.style.cssText = `
        position: fixed; top: 20px; right: 20px; background: #ff4444;
        color: white; padding: 15px; border-radius: 5px; display: none;
        z-index: 10000; max-width: 400px; box-shadow: 0 4px 6px rgba(0,0,0,0.1);
    `;
    document.body.appendChild(errorDiv);
    return errorDiv;
}

function updateConnectionStatus(status) {
    connectionState = status;
    const statusDiv = document.getElementById('connection-status') || createConnectionStatus();
    const statusText = statusDiv.querySelector('.status-text');
    const statusIcon = statusDiv.querySelector('.status-icon');
    
    switch (status) {
        case 'connected':
            statusText.textContent = 'Đã kết nối';
            statusIcon.textContent = '🟢';
            statusDiv.className = 'connection-status connected';
            break;
        case 'connecting':
            statusText.textContent = 'Đang kết nối...';
            statusIcon.textContent = '🟡';
            statusDiv.className = 'connection-status connecting';
            break;
        case 'disconnected':
            statusText.textContent = 'Mất kết nối';
            statusIcon.textContent = '🔴';
            statusDiv.className = 'connection-status disconnected';
            break;
        case 'reconnecting':
            statusText.textContent = 'Đang kết nối lại...';
            statusIcon.textContent = '🟡';
            statusDiv.className = 'connection-status reconnecting';
            break;
    }
}

function createConnectionStatus() {
    const statusDiv = document.createElement('div');
    statusDiv.id = 'connection-status';
    statusDiv.innerHTML = `
        <span class="status-icon"></span>
        <span class="status-text"></span>
    `;
    // Let CSS handle all positioning and styling
    // statusDiv.style.cssText removed to prevent CSS conflicts
    document.body.appendChild(statusDiv);
    return statusDiv;
}

// ======== KHI DOM ĐÃ SẴN SÀNG ========
document.addEventListener('DOMContentLoaded', () => {
    currentUserId = getCurrentUserId();
    console.log('Current User ID:', currentUserId);
    
    setupControls();
    
    // NOTE: initializeVideoCall() is called from Room.cshtml to avoid double initialization
    console.log('✅ videocall.js DOM ready, waiting for Room.cshtml to initialize...');
});

// ======== KHỞI TẠO VIDEO CALL ========
let isInitializing = false; // Flag to prevent double initialization
async function initializeVideoCall() {
    // Prevent double initialization
    if (isInitializing) {
        console.log('⚠️ initializeVideoCall already in progress, skipping...');
        return;
    }
    
    if (connection.state !== signalR.HubConnectionState.Disconnected) {
        console.log('⚠️ Connection not in Disconnected state:', connection.state);
        return;
    }
    
    isInitializing = true;
    try {
        showLoading('Đang khởi tạo cuộc họp...');
        await start();
        hideLoading();
        console.log('✅ Video call initialized successfully');
    } catch (error) {
        hideLoading();
        handleError(error, 'Không thể khởi tạo cuộc họp');
    } finally {
        isInitializing = false;
    }
}

// ======== 1. HÀM KHỞI ĐỘNG CUỘC GỌI ========
async function start() {
    try {
        updateConnectionStatus('connecting');
        
        // 1.1 LẤY STREAM CAMERA + MIC với retry
        showLoading('Đang truy cập camera và microphone...');
        localStream = await getUserMediaWithRetry();
        addVideo(localStream, 'self');

        // 1.2 ĐĂNG KÝ CÁC EVENT TỪ HUB
        setupSignalREvents();

        // 1.3 KẾT NỐI SIGNALR với retry
        showLoading('Đang kết nối đến server...');
        await connectSignalRWithRetry();
        
        // 1.4 JOIN ROOM với user tracking
        showLoading('Đang tham gia phòng họp...');
        const meetingCode = document.getElementById('video-grid')?.dataset?.meetingCode;
        if (!meetingCode) {
            throw new Error(ERROR_TYPES.MEETING_NOT_FOUND);
        }
        
        // Join room with user ID for tracking
        await connection.invoke('JoinRoom', meetingCode, currentUserId);
        console.log('Joined room', meetingCode, 'with user ID', currentUserId);
        
        updateConnectionStatus('connected');
        hideLoading();

    } catch (error) {
        updateConnectionStatus('disconnected');
        throw error;
    }
}

// ======== RETRY MECHANISM FOR MEDIA ACCESS ========
async function getUserMediaWithRetry(constraints = { video: true, audio: true }) {
    let lastError;
    
    // Try with both video and audio
    try {
        return await navigator.mediaDevices.getUserMedia(constraints);
    } catch (error) {
        lastError = error;
        console.warn('Failed to get video+audio, trying audio only:', error);
    }
    
    // Try with audio only
    try {
        const audioStream = await navigator.mediaDevices.getUserMedia({ audio: true, video: false });
        showError('Không thể truy cập camera. Chỉ có âm thanh.', true);
        return audioStream;
    } catch (error) {
        console.warn('Failed to get audio, trying video only:', error);
    }
    
    // Try with video only
    try {
        const videoStream = await navigator.mediaDevices.getUserMedia({ video: true, audio: false });
        showError('Không thể truy cập microphone. Chỉ có video.', true);
        return videoStream;
    } catch (error) {
        console.error('Failed to get any media:', error);
    }
    
    // If all fail, determine error type
    if (lastError.name === 'NotAllowedError') {
        throw new Error(ERROR_TYPES.MEDIA_ACCESS_DENIED);
    } else if (lastError.name === 'NotFoundError') {
        throw new Error(ERROR_TYPES.MEDIA_NOT_FOUND);
    } else {
        throw new Error(ERROR_TYPES.NETWORK_ERROR);
    }
}

// ======== SIGNALR CONNECTION WITH RETRY ========
async function connectSignalRWithRetry() {
    // Prevent race conditions with global lock
    if (isConnecting) {
        console.log('⏳ Connection already in progress, waiting...');
        // Wait for existing connection attempt to complete
        while (isConnecting) {
            await new Promise(resolve => setTimeout(resolve, 100));
        }
        // Check final state after waiting
        if (connection.state === signalR.HubConnectionState.Connected) {
            console.log('✅ Connection completed by another process');
            return;
        }
    }
    
    // Set lock
    isConnecting = true;
    let attempts = 0;
    
    try {
        while (attempts < MAX_RETRIES) {
            try {
                // Double-check connection state with lock
                if (connection.state === signalR.HubConnectionState.Connected) {
                    console.log('✅ SignalR already connected');
                    return;
                } else if (connection.state === signalR.HubConnectionState.Connecting) {
                    console.log('⏳ Connection already in progress, waiting...');
                    await new Promise(resolve => setTimeout(resolve, 1000));
                    continue;
                } else if (connection.state === signalR.HubConnectionState.Disconnected) {
                    console.log('🔄 Starting SignalR connection...');
                    await connection.start();
                    console.log('✅ SignalR connected successfully');
                    retryCount = 0; // Reset retry count on success
                    return;
                } else {
                    // Reconnecting or other states - wait
                    console.log(`⏳ SignalR in ${connection.state} state, waiting...`);
                    await new Promise(resolve => setTimeout(resolve, 1000));
                    continue;
                }
            } catch (error) {
                attempts++;
                console.warn(`❌ SignalR connection attempt ${attempts} failed:`, error);
                
                if (attempts >= MAX_RETRIES) {
                    throw new Error(ERROR_TYPES.SIGNALR_CONNECTION_FAILED);
                }
                
                // Stop connection if it's in a bad state
                try {
                    if (connection.state !== signalR.HubConnectionState.Disconnected) {
                        console.log('🛑 Stopping connection in bad state...');
                        await connection.stop();
                    }
                } catch (stopError) {
                    console.warn('⚠️ Error stopping connection:', stopError);
                }
                
                // Exponential backoff
                const delay = Math.pow(2, attempts) * 1000;
                showLoading(`Đang thử kết nối lại... (${attempts}/${MAX_RETRIES})`);
                await new Promise(resolve => setTimeout(resolve, delay));
            }
        }
    } finally {
        // Always release lock
        isConnecting = false;
    }
}

// ======== SETUP SIGNALR EVENTS ========
function setupSignalREvents() {
    // Connection events
    connection.onreconnecting(() => {
        console.log('SignalR reconnecting...');
        updateConnectionStatus('reconnecting');
        showError('Mất kết nối, đang thử kết nối lại...', true);
    });

    connection.onreconnected(() => {
        console.log('SignalR reconnected');
        updateConnectionStatus('connected');
        hideError();
    });

    connection.onclose(() => {
        console.log('SignalR connection closed');
        updateConnectionStatus('disconnected');
        showError('Mất kết nối đến server', false);
    });

    // Meeting events
        connection.on('Peers', list => {
            console.log('Peers:', list);
        try {
            list.forEach(id => initPeer(id, true));
        } catch (error) {
            console.error('Error initializing peers:', error);
            showError('Lỗi khi kết nối với người tham gia khác', true);
        }
        });

        connection.on('NewPeer', id => {
            console.log('NewPeer:', id);
        try {
            initPeer(id, false);
        } catch (error) {
            console.error('Error initializing new peer:', error);
            showError('Lỗi khi kết nối với người tham gia mới', true);
        }
        });

        connection.on('Signal', (from, data) => {
        try {
            if (peers[from]) {
                peers[from].signal(data);
            }
        } catch (error) {
            console.error('Error handling signal:', error);
        }
        });

        connection.on('CallEnded', () => {
        showError('Cuộc gọi đã kết thúc', false);
        setTimeout(() => {
            stopAll();
            window.location.href = '/Meeting/Index';
        }, 2000);
        });

    // ======== NEW: STATISTICS EVENTS ========
    connection.on('CallHistory', (history) => {
        console.log('Call History:', history);
        // You can display this in UI if needed
    });

    connection.on('CallStatistics', (stats) => {
        console.log('Call Statistics:', stats);
        // You can display this in UI if needed
    });
}

// ======== 2. KHỞI TẠO PEER VỚI ERROR HANDLING ========
function initPeer(peerId, initiator) {
    if (peers[peerId]) return; // đã khởi tạo rồi

    try {
    const peer = new SimplePeer({
        initiator,
        stream: localStream,
        config: { iceServers: ICE_SERVERS }
    });

        // Error handling for peer
        peer.on('error', (error) => {
            console.error(`Peer ${peerId} error:`, error);
            handlePeerError(peerId, error);
        });

        // Connection state monitoring
        peer.on('connect', () => {
            console.log(`Peer ${peerId} connected`);
        });

        peer.on('close', () => {
            console.log(`Peer ${peerId} connection closed`);
            removePeer(peerId);
        });

    // 2.1 Khi có offer/answer/ICE mới
    peer.on('signal', data => {
            connection.invoke('Signal', peerId, data).catch(error => {
                console.error('Error sending signal:', error);
                showError('Lỗi khi gửi tín hiệu', true);
            });
    });

    // 2.2 Khi nhận stream của peer
        peer.on('stream', stream => {
            try {
                addVideo(stream, peerId);
            } catch (error) {
                console.error('Error adding video:', error);
                showError('Lỗi khi hiển thị video', true);
            }
        });

    peers[peerId] = peer;

    } catch (error) {
        console.error('Error initializing peer:', error);
        showError('Lỗi khi kết nối với người tham gia', true);
    }
}

// ======== PEER ERROR HANDLING ========
function handlePeerError(peerId, error) {
    console.error(`Peer ${peerId} error:`, error);
    
    // Remove failed peer
    removePeer(peerId);
    
    // Show user-friendly error
    showError(`Mất kết nối với một người tham gia`, true);
    
    // Optional: Attempt to reconnect
    setTimeout(() => {
        if (connectionState === 'connected') {
            console.log(`Attempting to reconnect to peer ${peerId}`);
            // Could implement peer reconnection logic here
        }
    }, 5000);
}

function removePeer(peerId) {
    if (peers[peerId]) {
        try {
            peers[peerId].destroy();
        } catch (error) {
            console.error('Error destroying peer:', error);
        }
        delete peers[peerId];
    }
    
    // Remove video element
    const container = document.getElementById('container-' + peerId);
    if (container) {
        container.remove();
        updateVideoGridLayout();
    }
}

// ======== 3. HIỂN THỊ VIDEO VỚI ERROR HANDLING ========
function addVideo(stream, id) {
    try {
    const grid = document.getElementById('video-grid');
        if (!grid) {
            throw new Error('Video grid not found');
        }

    let container = document.getElementById('container-' + id);
    let video;

    if (!container) {
        container = document.createElement('div');
        container.className = 'video-container';
        container.id = 'container-' + id;

        video = document.createElement('video');
        video.id = id;
        video.autoplay = true;
        video.playsInline = true;
            video.muted = (id === 'self'); // Mute own video

            // Add error handling for video element
            video.onerror = (e) => {
                console.error('Video element error:', e);
                showError('Lỗi khi phát video', true);
            };

        container.appendChild(video);
        grid.appendChild(container);
    } else {
        video = container.querySelector('video');
    }

    video.srcObject = stream;
        
        // Add stream ended handler
        stream.addEventListener('ended', () => {
            console.log(`Stream ${id} ended`);
            if (id !== 'self') {
                removePeer(id);
            }
        });

    } catch (error) {
        console.error('Error adding video:', error);
        showError('Lỗi khi hiển thị video', true);
    }
}

// ======== 4. DỪNG TẤT CẢ VỚI TRACKING ========
function stopAll() {
    try {
        // Track user leave before stopping
        const meetingCode = document.getElementById('video-grid')?.dataset?.meetingCode;
        if (meetingCode && currentUserId) {
            connection.invoke('LeaveRoom', meetingCode, currentUserId).catch(error => {
                console.error('Error tracking leave:', error);
            });
        }

        // Stop local stream
    if (localStream) {
            localStream.getTracks().forEach(track => {
                try {
                    track.stop();
                } catch (error) {
                    console.error('Error stopping track:', error);
    }
            });
            localStream = null;
        }

        // Stop screen stream
    if (screenStream) {
            screenStream.getTracks().forEach(track => {
                try {
                    track.stop();
                } catch (error) {
                    console.error('Error stopping screen track:', error);
                }
            });
            screenStream = null;
        }

        // Close all peer connections
        Object.keys(peers).forEach(peerId => {
            try {
                peers[peerId].destroy();
            } catch (error) {
                console.error('Error destroying peer:', error);
            }
            delete peers[peerId];
        });

        // Clear video grid
        const grid = document.getElementById('video-grid');
        if (grid) {
            grid.innerHTML = '';
        }

        console.log('All connections stopped');

    } catch (error) {
        console.error('Error stopping all:', error);
    }
}

// ======== 5. THIẾT LẬP CÁC NÚT ĐIỀU KHIỂN VỚI ERROR HANDLING ========
function setupControls() {
    try {
        const toggleMicBtn = document.getElementById('toggle-mic');
        const toggleCamBtn = document.getElementById('toggle-cam');
        const shareScreenBtn = document.getElementById('share-screen');
        const leaveBtn = document.getElementById('btnLeave');
        const endBtn = document.getElementById('btnEnd');

        if (toggleMicBtn) {
            toggleMicBtn.addEventListener('click', () => {
                try {
                    if (localStream) {
                        const audioTrack = localStream.getAudioTracks()[0];
                        if (audioTrack) {
                            audioTrack.enabled = !audioTrack.enabled;
                            toggleMicBtn.textContent = audioTrack.enabled ? 'Tắt mic' : 'Bật mic';
                            toggleMicBtn.classList.toggle('active', !audioTrack.enabled);
                        }
                    }
                } catch (error) {
                    console.error('Error toggling microphone:', error);
                    showError('Lỗi khi điều khiển microphone', true);
                }
            });
        }

        if (toggleCamBtn) {
            toggleCamBtn.addEventListener('click', () => {
                try {
                    if (localStream) {
                        const videoTrack = localStream.getVideoTracks()[0];
                        if (videoTrack) {
                            videoTrack.enabled = !videoTrack.enabled;
                            toggleCamBtn.textContent = videoTrack.enabled ? 'Tắt cam' : 'Bật cam';
                            toggleCamBtn.classList.toggle('active', !videoTrack.enabled);
                        }
                    }
                } catch (error) {
                    console.error('Error toggling camera:', error);
                    showError('Lỗi khi điều khiển camera', true);
                }
            });
        }

        if (shareScreenBtn) {
            shareScreenBtn.addEventListener('click', async () => {
                try {
                    if (screenStream) {
                        stopScreenShare(shareScreenBtn);
                    } else {
                        await startScreenShare(shareScreenBtn);
                    }
                } catch (error) {
                    console.error('Screen share error:', error);
                    handleScreenShareError(error);
        }
    });
        }

        if (leaveBtn) {
            leaveBtn.addEventListener('click', () => {
                try {
                    if (confirm('Bạn có chắc muốn rời cuộc họp?')) {
        stopAll();
        window.location.href = '/Meeting/Index';
                    }
                } catch (error) {
                    console.error('Error leaving meeting:', error);
                    showError('Lỗi khi rời cuộc họp', true);
                }
            });
        }

        if (endBtn) {
            endBtn.addEventListener('click', async () => {
                try {
                    if (confirm('Bạn có chắc muốn kết thúc cuộc họp cho tất cả mọi người?')) {
                        const code = document.getElementById('video-grid')?.dataset?.meetingCode;
                        if (code) {
                            await connection.invoke('EndRoom', code);
                        }
                    }
                } catch (error) {
                    console.error('Error ending meeting:', error);
                    showError('Lỗi khi kết thúc cuộc họp', true);
                }
            });
        }

    } catch (error) {
        console.error('Error setting up controls:', error);
        showError('Lỗi khi thiết lập điều khiển', false);
    }
}

// ======== SCREEN SHARING WITH ERROR HANDLING ========
async function startScreenShare(button) {
    try {
        showLoading('Đang khởi tạo chia sẻ màn hình...');
        
        screenStream = await navigator.mediaDevices.getDisplayMedia({ 
            video: true,
            audio: true // Try to capture system audio
        });
        
        const screenTrack = screenStream.getVideoTracks()[0];
        await replaceTrack(screenTrack);
        
        button.textContent = 'Dừng chia sẻ';
        button.classList.add('active');
        
        // Handle screen share ended by user
        screenTrack.onended = () => {
            console.log('Screen share ended by user');
            stopScreenShare(button);
        };
        
        hideLoading();
        
    } catch (error) {
        hideLoading();
        throw error;
    }
}

function handleScreenShareError(error) {
    if (error.name === 'NotAllowedError') {
        showError('Bạn đã từ chối chia sẻ màn hình', true);
    } else if (error.name === 'NotFoundError') {
        showError('Không tìm thấy màn hình để chia sẻ', true);
    } else {
        showError('Lỗi khi chia sẻ màn hình', true);
    }
}

// ======== 6. THAY THẾ VIDEO TRACK CHO SCREEN SHARE VỚI ERROR HANDLING ========
async function replaceTrack(newTrack) {
    try {
    // 6.1 Thay trong localStream
        const oldTrack = localStream?.getVideoTracks()[0];
        if (oldTrack) {
            localStream.removeTrack(oldTrack);
            oldTrack.stop();
        }
        
        if (localStream) {
    localStream.addTrack(newTrack);
        }

    // 6.2 Thay cho từng peer
        const replacePromises = Object.values(peers).map(async (peer) => {
            try {
                const sender = peer._pc?.getSenders()?.find(s => 
                    s.track && s.track.kind === newTrack.kind
                );
                if (sender) {
                    await sender.replaceTrack(newTrack);
                }
            } catch (error) {
                console.error('Error replacing track for peer:', error);
            }
        });

        await Promise.all(replacePromises);
        
    } catch (error) {
        console.error('Error replacing track:', error);
        showError('Lỗi khi thay đổi video', true);
    }
}

// ======== 7. DỪNG CHIA SẺ MÀN HÌNH VỚI ERROR HANDLING ========
function stopScreenShare(button) {
    try {
    if (!screenStream) return;
        
        // Stop screen sharing tracks
        screenStream.getTracks().forEach(track => {
            try {
                track.stop();
            } catch (error) {
                console.error('Error stopping screen track:', error);
            }
        });
    screenStream = null;

        // Get camera back
        navigator.mediaDevices.getUserMedia({ video: true })
            .then(async (camStream) => {
        const camTrack = camStream.getVideoTracks()[0];
                await replaceTrack(camTrack);
                button.textContent = 'Chia sẻ màn hình';
                button.classList.remove('active');
            })
            .catch(error => {
                console.error('Error getting camera back:', error);
                showError('Không thể khôi phục camera', true);
        button.textContent = 'Chia sẻ màn hình';
        button.classList.remove('active');
            });
            
    } catch (error) {
        console.error('Error stopping screen share:', error);
        showError('Lỗi khi dừng chia sẻ màn hình', true);
    }
}

// ======== GENERAL ERROR HANDLER ========
function handleError(error, context = '') {
    console.error('Error:', error, 'Context:', context);
    
    let errorMessage = 'Đã xảy ra lỗi không xác định';
    
    // Map specific errors to user-friendly messages
    if (error.message && ERROR_MESSAGES[error.message]) {
        errorMessage = ERROR_MESSAGES[error.message];
    } else if (error.name === 'NotAllowedError') {
        errorMessage = ERROR_MESSAGES[ERROR_TYPES.MEDIA_ACCESS_DENIED];
    } else if (error.name === 'NotFoundError') {
        errorMessage = ERROR_MESSAGES[ERROR_TYPES.MEDIA_NOT_FOUND];
    } else if (context) {
        errorMessage = `${context}: ${error.message || 'Lỗi không xác định'}`;
    }
    
    showError(errorMessage, false);
}

// ======== WINDOW ERROR HANDLER ========
window.addEventListener('error', (event) => {
    console.error('Global error:', event.error);
    handleError(event.error, 'Lỗi hệ thống');
});

window.addEventListener('unhandledrejection', (event) => {
    console.error('Unhandled promise rejection:', event.reason);
    handleError(event.reason, 'Lỗi xử lý');
    event.preventDefault();
});

// ======== PAGE VISIBILITY HANDLING ========
document.addEventListener('visibilitychange', () => {
    if (document.hidden) {
        console.log('Page hidden - pausing video');
        // Optionally pause video when page is hidden
    } else {
        console.log('Page visible - resuming video');
        // Resume video when page becomes visible
        if (connectionState === 'disconnected') {
            showError('Đang thử kết nối lại...', true);
            // Only reconnect SignalR, don't re-initialize everything
            if (!isConnecting) {
                connectSignalRWithRetry().then(() => {
                    console.log('✅ Reconnected successfully');
                    hideError();
                }).catch(error => {
                    console.error('Error reconnecting:', error);
                    showError('Không thể kết nối lại', false);
                });
            } else {
                console.log('⏳ Connection already in progress, skipping reconnect');
            }
        }
    }
});

// ======== CLEANUP ON PAGE UNLOAD ========
window.addEventListener('beforeunload', () => {
    stopAll();
});

// ======== HELPER FUNCTIONS FOR STATISTICS ========
function getCallHistory() {
    const meetingCode = document.getElementById('video-grid')?.dataset?.meetingCode;
    if (meetingCode) {
        connection.invoke('GetCallHistory', meetingCode).catch(error => {
            console.error('Error getting call history:', error);
        });
    }
}

function getCallStatistics() {
    const meetingCode = document.getElementById('video-grid')?.dataset?.meetingCode;
    if (meetingCode) {
        connection.invoke('GetCallStatistics', meetingCode).catch(error => {
            console.error('Error getting call statistics:', error);
        });
    }
}

// ======== QUALITY CONTROL INTEGRATION FUNCTIONS ========
function setQualityController(controller) {
    qualityController = controller;
    
    // Set up quality change callbacks
    if (qualityController) {
        qualityController.onQualityChange = (type, quality) => {
            console.log(`📊 Quality changed: ${type} -> ${quality}`);
            if (type === 'video' && localStream) {
                updateVideoQualityForPeers(quality);
            }
        };
        
        qualityController.onStatsUpdate = (stats) => {
            console.log('📊 Connection stats updated:', stats);
            // Additional stats processing if needed
        };
    }
}

function updateVideoQualityForPeers(quality) {
    if (!qualityController || !localStream) return;
    
    const profile = qualityController.qualityProfiles[quality];
    if (!profile || quality === 'auto') return;
    
    // Apply constraints to local stream
    const videoTrack = localStream.getVideoTracks()[0];
    if (videoTrack) {
        const constraints = profile.video;
        videoTrack.applyConstraints({
            width: { ideal: constraints.width },
            height: { ideal: constraints.height },
            frameRate: { ideal: constraints.frameRate }
        }).then(() => {
            console.log(`✅ Applied video constraints: ${constraints.width}x${constraints.height}@${constraints.frameRate}fps`);
        }).catch(error => {
            console.warn('⚠️ Failed to apply video constraints:', error);
        });
    }
}

function getConnectionMetrics() {
    return {
        peerCount: Object.keys(peers).length,
        connectionState: connectionState,
        localStreamActive: localStream && localStream.active,
        streamTracks: localStream ? {
            video: localStream.getVideoTracks().length,
            audio: localStream.getAudioTracks().length
        } : null
    };
}

// ======== EXPOSE FUNCTIONS FOR QUALITY CONTROLLER ========
window.peers = peers; // Make peers accessible to quality controller
window.localStream = () => localStream; // Function to get current localStream

// ======== UPDATE VIDEO GRID LAYOUT ========
function updateVideoGridLayout() {
    const videoGrid = document.getElementById('video-grid');
    if (!videoGrid) return;
    
    const videoCount = videoGrid.querySelectorAll('.video-container').length;
    videoGrid.setAttribute('data-count', videoCount);
}


