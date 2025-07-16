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

/**
 * Lấy User-ID của người đang truy cập cho phần video-call.
 * Ưu tiên lấy từ DOM → sessionStorage → sinh tạm (fallback).
 *
 * Trả về: Number (userId)
 */
function getCurrentUserId() {
    // 1️⃣  ƯU TIÊN LẤY TỪ DOM (do server Razor render)
    //    Ví dụ Room.cshtml có:
    //      <body data-user-id="@User.Id"> 
    const userIdElement = document.querySelector('[data-user-id]');
    if (userIdElement) {
        return parseInt(userIdElement.getAttribute('data-user-id'));
    }

    // 2️⃣  THỬ LẤY TỪ sessionStorage (đã lưu khi login/page trước)
    const sessionUserId = sessionStorage.getItem('userId');
    if (sessionUserId) {
        return parseInt(sessionUserId);
    }

    // 3️⃣  FALLBACK: KHÔNG TÌM THẤY → TẠO ID TẠM (client-only)
    //    Không dùng cho production vì:
    //      • Không trùng khớp DB
    //      • Mất khi reload
    console.warn('User ID not found, using temporary ID');
    return Math.floor(Math.random() * 1000000);  // ← ID tạm (0–999 999)
}

// ======================================================
// ===============  UI HELPERS – LOADING  ===============
// ======================================================
/**
 * Hiển thị overlay loading ở giữa màn hình.
 * @param {string} message - Thông báo hiển thị dưới spinner (mặc định: "Đang tải...")
 */
function showLoading(message = 'Đang tải...') {
    // Tìm overlay đã có sẵn trong DOM; nếu chưa có thì tạo mới.
    //  - Ưu tiên dùng template HTML (nếu Room.cshtml đã render sẵn)
    //  - Nếu không tìm thấy => fallback sang hàm createLoadingOverlay()
    const loadingDiv = document.getElementById('videocall-loading-overlay') || createLoadingOverlay();

    // Cập nhật nội dung thông báo mỗi lần hiển thị
    loadingDiv.querySelector('.videocall-loading-message').textContent = message;

    // Hiển thị overlay (flex giúp căn giữa spinner + text)
    loadingDiv.style.display = 'flex';
}

/**
 * Ẩn overlay loading nếu đang hiển thị.
 * Lưu ý: chỉ ẩn (`display: none`) chứ không xoá khỏi DOM,
 *        để lần sau có thể tái sử dụng ngay.
 */
function hideLoading() {
    const loadingDiv = document.getElementById('videocall-loading-overlay');
    if (loadingDiv) {
        loadingDiv.style.display = 'none';
    }
}

/**
 * Tạo overlay loading mới (chỉ gọi khi không tìm thấy element trong DOM).
 * Trả về element vừa tạo để hàm gọi có thể tiếp tục thao tác.
 */
function createLoadingOverlay() {
    // 1️⃣  Tạo phần tử container
    const overlay = document.createElement('div');
    overlay.id = 'videocall-loading-overlay';
    overlay.className = 'videocall-loading-overlay';

    // 2️⃣  Bơm HTML bên trong: spinner + message
    overlay.innerHTML = `
        <div class="videocall-loading-content">
            <div class="videocall-loading-spinner"></div>
            <div class="videocall-loading-message">Đang tải...</div>
        </div>
    `;

    // Chỉ set display, không set style khác
    overlay.style.display = 'flex';
    // 4️⃣  Gắn overlay vào cuối <body>
    document.body.appendChild(overlay);
    // 5️⃣  Trả về element để caller có thể sử dụng ngay
    return overlay;
}

// ======================================================
// ============  UI HELPERS – ERROR NOTIFICATION ========
// ======================================================

/**
 * Hiển thị thông báo lỗi (toast) ở góc phải trên màn hình.
 *
 * @param {string}  message      - Nội dung lỗi cần hiển thị.
 * @param {boolean} isTemporary  - Nếu true, tự động ẩn sau 5 s.
 */
function showError(message, isTemporary = false) {
    // 1️⃣  Tìm div thông báo đã có; nếu chưa có thì tạo mới (lazy-create)
    const errorDiv = document.getElementById('error-notification') || createErrorNotification();
    // 2️⃣  Cập nhật nội dung lỗi
    errorDiv.querySelector('.error-message').textContent = message;
    // 3️⃣  Hiển thị (block = inline-block với chiều rộng tự động)
    errorDiv.style.display = 'block';
    // 4️⃣  Nếu tạm thời ➜ tự động ẩn sau 5 giây
    if (isTemporary) {
        setTimeout(() => hideError(), 5000);
    }
}

/**
 * Ẩn thông báo lỗi nếu đang hiển thị.
 * Không xoá khỏi DOM – giữ lại để tái sử dụng và tránh tạo mới.
 */
function hideError() {
    const errorDiv = document.getElementById('error-notification');
    if (errorDiv) {
        errorDiv.style.display = 'none';
    }
}

/**
 * Tạo phần tử thông báo lỗi mới.
 * Hàm chỉ được gọi khi không tìm thấy template trong DOM.
 *
 * @returns {HTMLElement}  Phần tử vừa được tạo.
 */
function createErrorNotification() {
    // 1️⃣  Tạo container
    const errorDiv = document.createElement('div');
    errorDiv.id = 'error-notification';

    // 2️⃣  Bơm HTML: icon ⚠️, text, nút đóng ×
    errorDiv.innerHTML = `
        <div class="error-content">
            <i class="error-icon">⚠️</i>
            <span class="error-message"></span>
            <button class="error-close" onclick="hideError()"></button>
        </div>
    `;
    // 3️⃣  Gán CSS inline (đảm bảo hiển thị kể cả thiếu stylesheet ngoài)
    errorDiv.style.cssText = `
        position: fixed; top: 20px; right: 20px; background: #ff4444;
        color: white; padding: 15px; border-radius: 5px; display: none;
        z-index: 10000; max-width: 400px; box-shadow: 0 4px 6px rgba(0,0,0,0.1);
    `;
    // 4️⃣  Thêm vào DOM & trả về
    document.body.appendChild(errorDiv);
    return errorDiv;
}

// ======== CẬP-NHẬT BADGE TRẠNG THÁI KẾT NỐI =========
function updateConnectionStatus(status) {
    connectionState = status; // Lưu trạng thái hiện tại để module khác tra cứu
    // Tìm phần tử badge (#connection-status) render sẵn trong Room.cshtml.
    // Nếu không có (page khác) ➜ tự tạo bằng createConnectionStatus().
    const statusDiv = document.getElementById('connection-status') || createConnectionStatus();
    // Hai node con cần cập nhật: icon & text
    const statusText = statusDiv.querySelector('.status-text');
    const statusIcon = statusDiv.querySelector('.status-icon');

    // Đổi UI theo 4 trạng thái chính
    switch (status) {
        case 'connected':    // Khi SignalR & WebRTC đều OK
            statusText.textContent = 'Đã kết nối';
            statusIcon.textContent = '🟢';
            statusDiv.className = 'connection-status connected'; // CSS đổi màu nền/vòng sáng
            break;
        case 'connecting':   // Đang xin quyền camera, đang start SignalR…
            statusText.textContent = 'Đang kết nối...';
            statusIcon.textContent = '🟡';
            statusDiv.className = 'connection-status connecting';
            break;
        case 'disconnected': // Mất mạng hoặc server đóng
            statusText.textContent = 'Mất kết nối';
            statusIcon.textContent = '🔴';
            statusDiv.className = 'connection-status disconnected';
            break;
        case 'reconnecting': // SignalR onreconnecting
            statusText.textContent = 'Đang kết nối lại...';
            statusIcon.textContent = '🟡';
            statusDiv.className = 'connection-status reconnecting';
            break;
    }
}

// ======== FALLBACK – TẠO BADGE NẾU TRANG CHƯA CÓ =========
function createConnectionStatus() {
    const statusDiv = document.createElement('div');
    statusDiv.id = 'connection-status';
    statusDiv.innerHTML = `
        <span class="status-icon"></span>
        <span class="status-text"></span>
    `;
    // Để CSS xử lý tất cả các vị trí và kiểu dáng
    // statusDiv.style.cssText đã xóa để tránh xung đột CSS
    document.body.appendChild(statusDiv);
    return statusDiv;
}

// ======== KHI DOM ĐÃ SẴN SÀNG ========
document.addEventListener('DOMContentLoaded', () => {
    // Lấy ID user hiện tại từ HTML
    currentUserId = getCurrentUserId();
    console.log('Current User ID:', currentUserId);

    // Setup các nút điều khiển (mute, camera, screen share...)
    setupControls();

    // Chờ Room.cshtml gọi initializeVideoCall() để tránh double init
    console.log('✅ videocall.js DOM ready, waiting for Room.cshtml to initialize...');
});

// ======== KHỞI TẠO VIDEO CALL ========
// -------------------------------------------------------------
// Cờ chống khởi tạo lặp – đảm bảo initializeVideoCall chỉ
// chạy một luồng tại một thời điểm (tránh double-click, race-condition)
// -------------------------------------------------------------
let isInitializing = false;
async function initializeVideoCall() {
    // 1️⃣  Không thực thi nếu đã có luồng khởi tạo khác
    if (isInitializing) {
        console.log('⚠️ initializeVideoCall already in progress, skipping...');
        return;
    }

    // 2️⃣  Chỉ tiếp tục khi HubConnection đang hoàn toàn NGẮT (Disconnected)
    if (connection.state !== signalR.HubConnectionState.Disconnected) {
        console.log('⚠️ Connection not in Disconnected state:', connection.state);
        return;
    }

    // 3️⃣  Đặt cờ "đang khởi tạo" để khóa các lời gọi khác
    isInitializing = true;
    try {
        // 4️⃣  Hiển thị overlay loading cho người dùng
        showLoading('Đang khởi tạo cuộc họp...');
        // 5️⃣  Gọi hàm trung tâm start() (mở cam ➜ kết nối SignalR ➜ JoinRoom)
        await start();
        // 6️⃣  Khởi tạo thành công ➜ ẩn overlay
        hideLoading();
        console.log('✅ Video call initialized successfully');
    } catch (error) {
        // ❌ Gặp lỗi ➜ ẩn overlay & hiển thị thông báo thân thiện
        hideLoading();
        handleError(error, 'Không thể khởi tạo cuộc họp');
    } finally {
        // 7️⃣  Luôn hạ cờ để lần sau có thể khởi tạo lại
        isInitializing = false;
    }
}

// ======== 1. HÀM KHỞI ĐỘNG CUỘC GỌI ========
async function start() {
    try {
        // 🟡 0. Báo UI: đang kết nối
        updateConnectionStatus('connecting');

        // 1️⃣ LẤY CAMERA + MIC  (có cơ chế thử-lại & fallback)
        showLoading('Đang truy cập camera và microphone...');
        localStream = await getUserMediaWithRetry(); // xin quyền ⇢ stream
        addVideo(localStream, 'self');   // hiển thị video của chính mình

        // 2️⃣ ĐĂNG KÝ CÁC SỰ KIỆN SignalR (Peers, Signal, CallEnded…)
        setupSignalREvents();

        // 3️⃣ MỞ KẾT NỐI SIGNALR  (tự retry & back-off)
        showLoading('Đang kết nối đến server...');
        await connectSignalRWithRetry();

        // 4️⃣ THAM GIA PHÒNG (JoinRoom) + theo dõi user
        showLoading('Đang tham gia phòng họp...');
        const meetingCode = document.getElementById('video-grid')?.dataset?.meetingCode;
        if (!meetingCode) {
            throw new Error(ERROR_TYPES.MEETING_NOT_FOUND);
        }

        // Join room with user ID for tracking
        await connection.invoke('JoinRoom', meetingCode, currentUserId);
        console.log('Joined room', meetingCode, 'with user ID', currentUserId);
        
        // Lấy session ID từ meeting service
        try {
            const response = await fetch(`/Meeting/GetActiveSession?code=${encodeURIComponent(meetingCode)}`);
            if (response.ok) {
                const sessionData = await response.json();
                if (sessionData.sessionId) {
                    window.currentSessionId = sessionData.sessionId;
                    console.log('Got session ID from meeting service:', window.currentSessionId);
                } else {
                    // Fallback: generate a temporary session ID
                    window.currentSessionId = 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
                        const r = Math.random() * 16 | 0;
                        const v = c == 'x' ? r : (r & 0x3 | 0x8);
                        return v.toString(16);
                    });
                    console.log('Generated temporary session ID:', window.currentSessionId);
                }
            } else {
                // Fallback: generate a temporary session ID
                window.currentSessionId = 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
                    const r = Math.random() * 16 | 0;
                    const v = c == 'x' ? r : (r & 0x3 | 0x8);
                    return v.toString(16);
                });
                console.log('Generated temporary session ID (fallback):', window.currentSessionId);
            }
        } catch (error) {
            console.error('Error getting session ID:', error);
            // Fallback: generate a temporary session ID
            window.currentSessionId = 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
                const r = Math.random() * 16 | 0;
                const v = c == 'x' ? r : (r & 0x3 | 0x8);
                return v.toString(16);
            });
            console.log('Generated temporary session ID (error fallback):', window.currentSessionId);
        }
        
        // Lưu thông tin room và session để chat system có thể sử dụng
        window.currentRoomId = meetingCode;
        window.currentUserId = currentUserId;

        // 🟢 Thành công: cập-nhật badge & tắt loading
        updateConnectionStatus('connected');
        hideLoading();

    } catch (error) {
        // 🔴 Gặp lỗi bất kỳ → báo UI "mất kết nối"
        updateConnectionStatus('disconnected');
        throw error;
    }
}

// ======== RETRY MECHANISM FOR MEDIA ACCESS ========
// Hàm này thực hiện chiến lược fallback để lấy quyền truy cập camera/microphone
// Nếu không lấy được cả hai, sẽ thử từng cái một
async function getUserMediaWithRetry(constraints = { video: true, audio: true }) {
    let lastError; // Lưu lỗi cuối cùng để phân loại sau này

    // BƯỚC 1: Thử lấy cả video và audio (trường hợp lý tưởng nhất)
    try {
        // getUserMedia() là API của browser để xin quyền truy cập camera/mic
        // await đợi user cho phép hoặc từ chối
        return await navigator.mediaDevices.getUserMedia(constraints);
    } catch (error) {
        // Nếu lỗi, lưu lại để phân loại sau
        lastError = error;
        // console.warn() in ra warning (không dừng chương trình)
        console.warn('Failed to get video+audio, trying audio only:', error);
    }

    // BƯỚC 2: Nếu lỗi → thử chỉ lấy audio (bỏ video)
    try {
        // Chỉ xin quyền microphone, không xin camera
        const audioStream = await navigator.mediaDevices.getUserMedia({ audio: true, video: false });
        // showError() hiển thị thông báo lỗi cho user biết
        showError('Không thể truy cập camera. Chỉ có âm thanh.', true);
        // Trả về stream chỉ có audio
        return audioStream;
    } catch (error) {
        // Nếu audio cũng lỗi, tiếp tục thử video
        console.warn('Failed to get audio, trying video only:', error);
    }

    // BƯỚC 3: Nếu lỗi → thử chỉ lấy video (bỏ audio)
    try {
        // Chỉ xin quyền camera, không xin microphone
        const videoStream = await navigator.mediaDevices.getUserMedia({ video: true, audio: false });
        // Thông báo cho user biết chỉ có video
        showError('Không thể truy cập microphone. Chỉ có video.', true);
        // Trả về stream chỉ có video
        return videoStream;
    } catch (error) {
        // Nếu video cũng lỗi, in error và tiếp tục
        console.error('Failed to get any media:', error);
    }

    // BƯỚC 4: Nếu tất cả đều lỗi → phân loại lỗi để xử lý phù hợp
    if (lastError.name === 'NotAllowedError') {
        // User đã từ chối cấp quyền truy cập
        throw new Error(ERROR_TYPES.MEDIA_ACCESS_DENIED);
    } else if (lastError.name === 'NotFoundError') {
        // Không tìm thấy thiết bị camera/mic
        throw new Error(ERROR_TYPES.MEDIA_NOT_FOUND);
    } else {
        // Lỗi khác (mạng, hệ thống...)
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
/**
 * Thiết lập tất cả các sự kiện SignalR để xử lý kết nối realtime
 * Hàm này được gọi một lần khi khởi tạo video call để đăng ký các event handler
 * Các sự kiện bao gồm: kết nối lại, quản lý peer, tín hiệu WebRTC, và thống kê
 */
function setupSignalREvents() {
    // ====== Sự kiện kết nối lại (tự động khi mất kết nối mạng) ======
    connection.onreconnecting(() => {
        // Khi SignalR đang cố gắng kết nối lại với server (mất mạng tạm thời)
        console.log('SignalR reconnecting...');
        updateConnectionStatus('reconnecting'); // Cập nhật trạng thái UI
        showError('Mất kết nối, đang thử kết nối lại...', true); // Hiện thông báo lỗi cho người dùng
    });

    connection.onreconnected(() => {
        // Khi SignalR đã kết nối lại thành công
        console.log('SignalR reconnected');
        updateConnectionStatus('connected'); // Cập nhật trạng thái UI
        hideError(); // Ẩn thông báo lỗi
    });

    connection.onclose(() => {
        // Khi kết nối SignalR bị đóng hoàn toàn (không thể tự động kết nối lại)
        console.log('SignalR connection closed');
        updateConnectionStatus('disconnected'); // Cập nhật trạng thái UI
        showError('Mất kết nối đến server', false); // Hiện thông báo lỗi cố định
    });

    // ====== Sự kiện liên quan đến phòng họp (meeting) ======
    connection.on('Peers', list => {
        // Nhận danh sách các peerId (người tham gia khác) khi vừa vào phòng
        // Server gửi danh sách này để client tạo kết nối WebRTC với từng người
        console.log('Peers:', list);
        try {
            list.forEach(id => initPeer(id, true)); // Tạo peer connection với từng người (initiator = true)
        } catch (error) {
            // Nếu có lỗi khi tạo kết nối với peer nào đó
            console.error('Error initializing peers:', error);
            showError('Lỗi khi kết nối với người tham gia khác', true);
        }
    });

    connection.on('NewPeer', id => {
        // Khi có người mới vào phòng, server gửi sự kiện này cho các client còn lại
        // Client sẽ tạo kết nối WebRTC với người mới (initiator = false)
        console.log('NewPeer:', id);
        try {
            initPeer(id, false);
        } catch (error) {
            // Nếu có lỗi khi tạo kết nối với peer mới
            console.error('Error initializing new peer:', error);
            showError('Lỗi khi kết nối với người tham gia mới', true);
        }
    });

    connection.on('Signal', (from, data) => {
        // ====== GIẢI THÍCH CHI TIẾT SỰ KIỆN SIGNAL ======

        // 'from': peerId (connectionId) của người gửi tín hiệu
        //         Đây là ID duy nhất mà server SignalR gán cho mỗi client khi kết nối
        //         Ví dụ: "abc123-def456-ghi789" (được tạo tự động bởi SignalR)

        // 'data': Nội dung tín hiệu WebRTC, có thể là:
        //         - SDP Offer: Khi peer A muốn kết nối với peer B
        //         - SDP Answer: Khi peer B chấp nhận kết nối từ peer A  
        //         - ICE Candidate: Thông tin về đường truyền mạng (IP, port, protocol)

        // Nhận tín hiệu WebRTC (SDP, ICE candidate) từ server, do peer khác gửi lên
        // 'from' là peerId của người gửi, 'data' là nội dung tín hiệu
        try {
            // Kiểm tra xem có tồn tại peer connection với 'from' không
            // peers là object global chứa tất cả peer connections hiện tại
            // Key = connectionId, Value = SimplePeer object
            if (peers[from]) {
                // Chuyển tín hiệu vào đúng SimplePeer object
                // SimplePeer sẽ tự động xử lý tín hiệu này để thiết lập kết nối WebRTC
                peers[from].signal(data); // Chuyển tín hiệu này vào đối tượng SimplePeer tương ứng
            }
        } catch (error) {
            // Nếu có lỗi khi xử lý tín hiệu
            console.error('Error handling signal:', error);
        }
    });

    connection.on('CallEnded', () => {
        // Khi cuộc gọi kết thúc (ai đó bấm kết thúc hoặc server đóng phòng)
        showError('Cuộc gọi đã kết thúc', false); // Hiện thông báo cho người dùng
        setTimeout(() => {
            stopAll(); // Dừng toàn bộ kết nối, giải phóng tài nguyên
            window.location.href = '/Meeting/Index'; // Chuyển về trang danh sách phòng họp
        }, 2000); // Đợi 2 giây cho người dùng đọc thông báo
    });

    // ======== Sự kiện thống kê cuộc gọi (mới) ========
    connection.on('CallHistory', (history) => {
        // Nhận lịch sử các cuộc gọi (nếu server gửi về)
        // Có thể dùng để hiển thị lịch sử trong UI
        console.log('Call History:', history);
    });

    connection.on('CallStatistics', (stats) => {
        // Nhận thống kê realtime về cuộc gọi (ví dụ: bitrate, số người tham gia, v.v.)
        // Có thể dùng để hiển thị thông tin chất lượng cuộc gọi cho người dùng
        console.log('Call Statistics:', stats);
    });
}

// ======== 2. KHỞI TẠO PEER VỚI ERROR HANDLING ========
// Hàm này tạo kết nối peer-to-peer với một người khác trong cuộc gọi video
// peerId: ID của người cần kết nối (ví dụ: 'user123')
// initiator: true nếu bạn là người khởi tạo kết nối, false nếu bạn là người nhận
function initPeer(peerId, initiator) {
    // Nếu đã có kết nối với peer này rồi thì không tạo lại nữa (tránh duplicate)
    if (peers[peerId]) return; // đã khởi tạo rồi

    try {
        // Tạo một đối tượng SimplePeer để kết nối WebRTC
        const peer = new SimplePeer({
            initiator,  // Bạn là người khởi tạo (offer) hay không (answer)
            stream: localStream,  // Stream video/audio của bạn để gửi cho peer
            config: { iceServers: ICE_SERVERS } // Danh sách STUN/TURN server để hỗ trợ kết nối
        });

        // ===== Error handling cho peer =====
        // Nếu có lỗi trong quá trình kết nối hoặc truyền dữ liệu
        peer.on('error', (error) => {
            console.error(`Peer ${peerId} error:`, error);
            handlePeerError(peerId, error); // Xử lý lỗi và dọn dẹp
        });

        // ===== Theo dõi trạng thái kết nối =====
        // Khi kết nối peer-to-peer thành công
        peer.on('connect', () => {
            console.log(`Peer ${peerId} connected`);
        });

        // Khi kết nối peer-to-peer bị đóng (peer rời phòng hoặc mất kết nối)
        peer.on('close', () => {
            console.log(`Peer ${peerId} connection closed`);
            removePeer(peerId);  // Xóa peer khỏi danh sách và UI
        });

        // ===== 2.1 Khi có offer/answer/ICE mới =====
        // Đăng ký sự kiện 'signal' của SimplePeer
        // Sự kiện này được kích hoạt khi SimplePeer tạo ra một tín hiệu WebRTC mới (offer, answer, hoặc ICE candidate)
        peer.on('signal', data => {
            // Gửi tín hiệu này lên server (SignalR) để chuyển tiếp cho peer còn lại (người cần kết nối)
            // peerId là ID của người nhận, data là nội dung tín hiệu
            connection.invoke('Signal', peerId, data).catch(error => {
                // Nếu gửi tín hiệu lên server thất bại, log lỗi và báo lỗi cho user
                console.error('Error sending signal:', error);
                showError('Lỗi khi gửi tín hiệu', true);
            });
        });

        // ===== 2.2 Khi nhận stream video/audio từ peer =====
        // Đăng ký sự kiện 'stream' của SimplePeer
        // Sự kiện này được kích hoạt khi kết nối WebRTC thành công và nhận được stream video/audio từ peer bên kia
        peer.on('stream', stream => {
            try {
                // Gọi hàm addVideo để hiển thị video của peer lên giao diện (UI)
                // stream: MediaStream chứa video/audio của peer
                // peerId: ID của người gửi stream này
                addVideo(stream, peerId);
            } catch (error) {
                // Nếu có lỗi khi hiển thị video, log lỗi và báo lỗi cho user
                console.error('Error adding video:', error);
                showError('Lỗi khi hiển thị video', true);
            }
        });

        // Lưu peer vào object để quản lý (truy cập lại khi cần)
        peers[peerId] = peer;

    } catch (error) {
        // Nếu có lỗi khi khởi tạo peer, log lỗi và báo cho user
        console.error('Error initializing peer:', error);
        showError('Lỗi khi kết nối với người tham gia', true);
    }
}

// ======== PEER ERROR HANDLING ========
// Hàm này xử lý lỗi khi kết nối peer-to-peer với một người bị lỗi
// Được gọi khi có lỗi trong quá trình kết nối hoặc duy trì kết nối với peer
function handlePeerError(peerId, error) {
    // Log lỗi chi tiết ra console để developer debug
    // peerId: ID của người bị lỗi (ví dụ: 'user123')
    // error: Thông tin lỗi chi tiết từ WebRTC
    console.error(`Peer ${peerId} error:`, error);

    // Xóa peer bị lỗi khỏi danh sách và dọn dẹp tài nguyên
    // removePeer() sẽ: xóa video, hủy kết nối, xóa khỏi peers object
    removePeer(peerId);

    // Hiển thị thông báo thân thiện cho user
    // Thông báo này sẽ tự động ẩn sau 5 giây (isTemporary = true)
    showError(`Mất kết nối với một người tham gia`, true);

    // Thử kết nối lại sau 5 giây (optional feature)
    setTimeout(() => {
        // Chỉ thử kết nối lại nếu SignalR vẫn connected
        // Tránh thử kết nối khi đã mất kết nối server
        if (connectionState === 'connected') {
            console.log(`Attempting to reconnect to peer ${peerId}`);
            // TODO: Có thể implement logic reconnect peer ở đây
            // Ví dụ: gọi lại initPeer(peerId, false) để tạo kết nối mới
        }
    }, 5000); // Đợi 5 giây trước khi thử kết nối lại
}

// ======== XÓA PEER VÀ VIDEO ========
// Hàm này xóa hoàn toàn kết nối peer và video của một người khỏi cuộc gọi
// Được gọi khi người đó rời phòng, bị lỗi kết nối, hoặc bạn rời phòng
function removePeer(peerId) {
    // Kiểm tra xem có kết nối peer với người này không
    if (peers[peerId]) {
        try {
            // Hủy kết nối WebRTC với người này
            // destroy() sẽ: dừng streams, đóng connection, giải phóng tài nguyên
            peers[peerId].destroy();
        } catch (error) {
            // Nếu có lỗi khi hủy kết nối, log lỗi nhưng không dừng
            console.error('Error destroying peer:', error);
        }
        // Xóa peer khỏi danh sách peers object
        // Ví dụ: peers = { 'user123': peer, 'user456': peer } 
        // Sau delete: peers = { 'user456': peer }
        delete peers[peerId];
    }

    // Tìm và xóa video element của người này khỏi màn hình
    // container-user123, container-user456, etc.
    const container = document.getElementById('container-' + peerId);
    if (container) {
        // Xóa container video khỏi DOM
        // User sẽ không còn thấy video của người này
        container.remove();

        // Cập nhật layout video grid sau khi xóa
        // Điều chỉnh kích thước video còn lại cho phù hợp
        updateVideoGridLayout();
    }
}

/// ======== 3. HIỂN THỊ VIDEO VỚI ERROR HANDLING ========
// Hàm này tạo và hiển thị video element để xem camera của chính mình và người khác
function addVideo(stream, id) {
    try {
        // Tìm container chính chứa tất cả video
        const grid = document.getElementById('video-grid');
        if (!grid) {
            throw new Error('Video grid not found');
        }

        // Tìm container cho video này (ví dụ: container-self, container-user123)
        let container = document.getElementById('container-' + id);
        let video;

        // Nếu container chưa tồn tại → tạo mới
        if (!container) {
            // Tạo div container cho video
            container = document.createElement('div');
            container.className = 'video-container';  // CSS class để style
            container.id = 'container-' + id;   // ID duy nhất

            // Tạo element video
            video = document.createElement('video');
            video.id = id;   // ID video: self, user123
            video.autoplay = true;  // Tự động phát khi có stream
            video.playsInline = true;  // Phát inline, không fullscreen
            video.muted = (id === 'self'); // Tắt âm video của chính mình (tránh echo)

            // Xử lý lỗi khi video không phát được
            video.onerror = (e) => {
                console.error('Video element error:', e);
                showError('Lỗi khi phát video', true);
            };

            // Gắn video vào container, rồi gắn container vào grid
            container.appendChild(video);
            grid.appendChild(container);
        } else {
            // Nếu container đã tồn tại → lấy video element bên trong
            video = container.querySelector('video');
        }

        // Gán stream vào video để hiển thị
        video.srcObject = stream;

        // Lắng nghe khi stream kết thúc (người dùng rời phòng)
        stream.addEventListener('ended', () => {
            console.log(`Stream ${id} ended`);
            // Chỉ xóa video người khác, không xóa video của chính mình
            if (id !== 'self') {
                removePeer(id);  // Xóa video người khác khỏi màn hình
            }
        });

    } catch (error) {

        // Xử lý lỗi nếu có vấn đề khi tạo/hiển thị video
        console.error('Error adding video:', error);
        showError('Lỗi khi hiển thị video', true);
    }
}

// ======== 4. DỪNG TẤT CẢ VỚI TRACKING ========
/**
 * Hàm dọn dẹp toàn bộ tài nguyên khi kết thúc cuộc gọi video
 * Được gọi khi: người dùng rời phòng, cuộc gọi kết thúc, hoặc có lỗi nghiêm trọng
 */
function stopAll() {
    try {
        // ====== BƯỚC 1: THÔNG BÁO CHO SERVER BIẾT USER ĐÃ RỜI PHÒNG ======
        const meetingCode = document.getElementById('video-grid')?.dataset?.meetingCode;
        if (meetingCode && currentUserId) {
            // Gọi SignalR để thông báo server user đã rời phòng
            // Server sẽ: cập nhật database, thông báo cho các user khác, dọn dẹp session
            connection.invoke('LeaveRoom', meetingCode, currentUserId).catch(error => {
                console.error('Error tracking leave:', error);
            });
        }

        // ====== BƯỚC 2: DỪNG LOCAL STREAM (CAMERA + MICROPHONE) ======
        if (localStream) {
            // localStream chứa video track (camera) và audio track (microphone)
            localStream.getTracks().forEach(track => {
                try {
                    // Dừng từng track riêng biệt
                    // track.stop() sẽ: tắt camera/microphone, giải phóng tài nguyên hardware
                    track.stop();
                } catch (error) {
                    console.error('Error stopping track:', error);
                }
            });
            localStream = null; // Xóa reference để garbage collector dọn dẹp
        }

        // ====== BƯỚC 3: DỪNG SCREEN SHARE STREAM (NẾU ĐANG CHIA SẺ MÀN HÌNH) ======
        if (screenStream) {
            // screenStream chứa video track của màn hình được chia sẻ
            screenStream.getTracks().forEach(track => {
                try {
                    // Dừng screen sharing track
                    // track.stop() sẽ: dừng chia sẻ màn hình, giải phóng tài nguyên
                    track.stop();
                } catch (error) {
                    console.error('Error stopping screen track:', error);
                }
            });
            screenStream = null; // Xóa reference
        }

        // ====== BƯỚC 4: ĐÓNG TẤT CẢ PEER CONNECTIONS (WEBRTC) ======
        Object.keys(peers).forEach(peerId => {
            try {
                // peers[peerId] là SimplePeer object cho mỗi kết nối peer-to-peer
                // peer.destroy() sẽ: đóng WebRTC connection, dừng stream, giải phóng tài nguyên
                peers[peerId].destroy();
            } catch (error) {
                console.error('Error destroying peer:', error);
            }
            delete peers[peerId]; // Xóa peer khỏi object peers
        });

        // ====== BƯỚC 5: DỌN DẸP UI - XÓA TẤT CẢ VIDEO ELEMENTS ======
        const grid = document.getElementById('video-grid');
        if (grid) {
            // Xóa tất cả video containers trong grid
            // Bao gồm: video của chính mình và video của các peer khác
            grid.innerHTML = '';
        }

        console.log('All connections stopped');

    } catch (error) {
        console.error('Error stopping all:', error);
    }
}

// ======== 5. THIẾT LẬP CÁC NÚT ĐIỀU KHIỂN VỚI ERROR HANDLING ========
/**
 * Hàm thiết lập tất cả các nút điều khiển trong cuộc gọi video
 * Được gọi sau khi video call system đã khởi tạo xong
 * Mục đích: Gắn event listeners cho các button để user có thể tương tác
 */
function setupControls() {
    try {
        // ====== BƯỚC 1: LẤY REFERENCES ĐẾN CÁC BUTTON ELEMENTS ======
        // Sử dụng document.getElementById() để tìm các button theo ID
        // Các ID này phải khớp với HTML trong Room.cshtml
        const toggleMicBtn = document.getElementById('toggle-mic');      // Nút bật/tắt microphone
        const toggleCamBtn = document.getElementById('toggle-cam');      // Nút bật/tắt camera
        const shareScreenBtn = document.getElementById('share-screen');  // Nút chia sẻ màn hình
        const leaveBtn = document.getElementById('btnLeave');            // Nút rời phòng
        const endBtn = document.getElementById('btnEnd');                // Nút kết thúc cuộc họp (chỉ host)

        // ====== BƯỚC 2: THIẾT LẬP NÚT BẬT/TẮT MICROPHONE ======
        if (toggleMicBtn) {  // Kiểm tra button có tồn tại không
            // Đăng ký event listener cho sự kiện 'click'
            // Arrow function () => {} sẽ được gọi khi user click button
            toggleMicBtn.addEventListener('click', () => {
                try {
                    // Kiểm tra localStream có tồn tại không (camera/mic đã được lấy chưa)
                    if (localStream) {
                        // Lấy audio track từ stream (microphone)
                        // getAudioTracks() trả về array, [0] lấy track đầu tiên
                        const audioTrack = localStream.getAudioTracks()[0];
                        
                        if (audioTrack) {  // Kiểm tra có audio track không
                            // Bật/tắt microphone bằng cách thay đổi thuộc tính enabled
                            // !audioTrack.enabled: đảo ngược trạng thái hiện tại
                            audioTrack.enabled = !audioTrack.enabled;
                            
                            // Cập nhật text button dựa trên trạng thái mới
                            // Nếu enabled = true → "Tắt mic", nếu false → "Bật mic"
                            toggleMicBtn.textContent = audioTrack.enabled ? 'Tắt mic' : 'Bật mic';
                            
                            // Thêm/xóa class 'active' để thay đổi style button
                            // classList.toggle(class, condition): thêm class nếu condition = true
                            toggleMicBtn.classList.toggle('active', !audioTrack.enabled);
                        }
                    }
                } catch (error) {
                    // Xử lý lỗi nếu có vấn đề khi điều khiển microphone
                    console.error('Error toggling microphone:', error);
                    showError('Lỗi khi điều khiển microphone', true);
                }
            });
        }

        // ====== BƯỚC 3: THIẾT LẬP NÚT BẬT/TẮT CAMERA ======
        if (toggleCamBtn) {
            toggleCamBtn.addEventListener('click', () => {
                try {
                    if (localStream) {
                        // Lấy video track từ stream (camera)
                        const videoTrack = localStream.getVideoTracks()[0];
                        
                        if (videoTrack) {
                            // Bật/tắt camera tương tự như microphone
                            videoTrack.enabled = !videoTrack.enabled;
                            
                            // Cập nhật text và style button
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

        // ====== BƯỚC 4: THIẾT LẬP NÚT CHIA SẺ MÀN HÌNH ======
        if (shareScreenBtn) {
            // Sử dụng async function vì startScreenShare() là async
            shareScreenBtn.addEventListener('click', async () => {
                try {
                    // Kiểm tra xem đang chia sẻ màn hình chưa
                    if (screenStream) {
                        // Nếu đang chia sẻ → dừng chia sẻ
                        stopScreenShare(shareScreenBtn);
                    } else {
                        // Nếu chưa chia sẻ → bắt đầu chia sẻ
                        await startScreenShare(shareScreenBtn);
                    }
                } catch (error) {
                    console.error('Screen share error:', error);
                    // Gọi hàm xử lý lỗi riêng cho screen sharing
                    handleScreenShareError(error);
                }
            });
        }

        // ====== BƯỚC 5: THIẾT LẬP NÚT RỜI PHÒNG ======
        if (leaveBtn) {
            leaveBtn.addEventListener('click', () => {
                try {
                    // Hiển thị confirm dialog để user xác nhận
                    // confirm() trả về true nếu user click OK, false nếu Cancel
                    if (confirm('Bạn có chắc muốn rời cuộc họp?')) {
                        // Gọi hàm dọn dẹp tất cả tài nguyên
                        stopAll();
                        // Chuyển hướng về trang danh sách phòng họp
                        window.location.href = '/Meeting/Index';
                    }
                    // Nếu user click Cancel → không làm gì cả
                } catch (error) {
                    console.error('Error leaving meeting:', error);
                    showError('Lỗi khi rời cuộc họp', true);
                }
            });
        }

        // ====== BƯỚC 6: THIẾT LẬP NÚT KẾT THÚC CUỘC HỌP (CHỈ HOST) ======
        if (endBtn) {
            // Sử dụng async vì connection.invoke() trả về Promise
            endBtn.addEventListener('click', async () => {
                try {
                    // Xác nhận với user (chỉ host mới có quyền kết thúc)
                    if (confirm('Bạn có chắc muốn kết thúc cuộc họp cho tất cả mọi người?')) {
                        // Lấy meeting code từ HTML data attribute
                        const code = document.getElementById('video-grid')?.dataset?.meetingCode;
                        
                        if (code) {
                            // Gọi method EndRoom trên server để kết thúc cuộc họp
                            // Server sẽ thông báo cho tất cả user khác
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
        // Xử lý lỗi chung nếu có vấn đề khi thiết lập controls
        console.error('Error setting up controls:', error);
        showError('Lỗi khi thiết lập điều khiển', false);
    }
}

// ======== SCREEN SHARING WITH ERROR HANDLING ========
/**
 * Hàm bắt đầu chia sẻ màn hình trong cuộc gọi video
 * Được gọi khi user click nút "Chia sẻ màn hình"
 * Mục đích: Thay thế video camera bằng video màn hình để chia sẻ nội dung
 */
async function startScreenShare(button) {
    try {
        // ====== BƯỚC 1: HIỂN THỊ LOADING VÀ XIN QUYỀN CHIA SẺ ======
        showLoading('Đang khởi tạo chia sẻ màn hình...');

        // Sử dụng getDisplayMedia() API để xin quyền chia sẻ màn hình
        // Đây là Web API mới thay thế cho getUserMedia() cho screen sharing
        screenStream = await navigator.mediaDevices.getDisplayMedia({
            video: true,    // Xin quyền chia sẻ video (màn hình)
            audio: true     // Xin quyền chia sẻ audio (âm thanh hệ thống)
            // Lưu ý: audio: true có thể không hoạt động trên tất cả browser
        });

        // ====== BƯỚC 2: LẤY VIDEO TRACK TỪ SCREEN STREAM ======
        // getVideoTracks() trả về array các video tracks
        // [0] lấy track đầu tiên (thường chỉ có 1 track khi chia sẻ màn hình)
        const screenTrack = screenStream.getVideoTracks()[0];

        // ====== BƯỚC 3: THAY THẾ VIDEO TRACK HIỆN TẠI ======
        // replaceTrack() sẽ thay thế camera video bằng screen video
        // Tất cả peer connections sẽ nhận được video màn hình thay vì camera
        await replaceTrack(screenTrack);

        // ====== BƯỚC 4: CẬP NHẬT UI - THAY ĐỔI TRẠNG THÁI BUTTON ======
        button.textContent = 'Dừng chia sẻ';  // Thay đổi text button
        button.classList.add('active');       // Thêm class để thay đổi style

        // ====== BƯỚC 5: ĐĂNG KÝ EVENT HANDLER CHO SCREEN SHARE ENDED ======
        // onended: Sự kiện được kích hoạt khi user dừng chia sẻ từ browser UI
        // Ví dụ: User click "Stop sharing" trong browser popup
        screenTrack.onended = () => {
            console.log('Screen share ended by user');
            // Gọi hàm dừng chia sẻ để dọn dẹp và khôi phục camera
            stopScreenShare(button);
        };

        // ====== BƯỚC 6: ẨN LOADING KHI HOÀN THÀNH ======
        hideLoading();

    } catch (error) {
        // ====== XỬ LÝ LỖI ======
        hideLoading();  // Ẩn loading dù có lỗi hay không
        throw error;    // Throw lại error để caller xử lý
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
/**
 * Hàm thay thế video track trong cuộc gọi video
 * Được gọi khi user bắt đầu hoặc dừng chia sẻ màn hình
 * Mục đích: Thay thế camera video bằng screen video (hoặc ngược lại)
 * 
 * LUỒNG HOẠT ĐỘNG:
 * 1. startScreenShare() → replaceTrack(screenTrack) → Thay camera bằng screen
 * 2. stopScreenShare() → replaceTrack(cameraTrack) → Thay screen bằng camera
 * 
 * QUAN HỆ VỚI WEBRTC:
 * - Thay đổi track trong localStream (MediaStream)
 * - Cập nhật tất cả peer connections (RTCPeerConnection)
 * - Đảm bảo tất cả participants nhận được video mới
 * 
 * BIẾN GLOBAL ĐƯỢC SỬ DỤNG:
 * - localStream: MediaStream hiện tại (camera + microphone)
 * - peers: Object chứa tất cả peer connections
 */
async function replaceTrack(newTrack) {
    try {
        // ====== BƯỚC 1: THAY THẾ TRACK TRONG LOCALSTREAM ======
        // Lấy video track cũ từ localStream (camera hoặc screen hiện tại)
        // Optional chaining (?.) để tránh lỗi nếu localStream = null
        const oldTrack = localStream?.getVideoTracks()[0];
        
        if (oldTrack) {
            // Xóa track cũ khỏi localStream
            // removeTrack() chỉ xóa track khỏi stream, không dừng track
            localStream.removeTrack(oldTrack);
            
            // Dừng track cũ để giải phóng tài nguyên
            // stop() sẽ tắt camera hoặc dừng screen sharing
            oldTrack.stop();
        }

        // Thêm track mới vào localStream
        // addTrack() thêm track vào stream để sử dụng
        if (localStream) {
            localStream.addTrack(newTrack);
        }

        // ====== BƯỚC 2: THAY THẾ TRACK TRONG TẤT CẢ PEER CONNECTIONS ======
        // Object.values(peers) lấy tất cả SimplePeer objects
        // map() tạo array các promises cho việc thay track
        const replacePromises = Object.values(peers).map(async (peer) => {
            try {
                // Lấy RTCPeerConnection từ SimplePeer object
                // _pc là thuộc tính internal của SimplePeer chứa WebRTC connection
                const sender = peer._pc?.getSenders()?.find(s =>
                    // Tìm sender có track cùng loại với newTrack (video)
                    s.track && s.track.kind === newTrack.kind
                );
                
                if (sender) {
                    // Thay thế track trong peer connection
                    // replaceTrack() sẽ gửi track mới đến peer bên kia
                    await sender.replaceTrack(newTrack);
                }
            } catch (error) {
                // Xử lý lỗi riêng cho từng peer
                // Lỗi một peer không ảnh hưởng peers khác
                console.error('Error replacing track for peer:', error);
            }
        });

        // Đợi tất cả promises hoàn thành
        // Promise.all() đợi tất cả peer connections được cập nhật
        await Promise.all(replacePromises);

    } catch (error) {
        // Xử lý lỗi chung nếu có vấn đề khi thay track
        console.error('Error replacing track:', error);
        showError('Lỗi khi thay đổi video', true);
    }
}

// ======== 7. DỪNG CHIA SẺ MÀN HÌNH VỚI ERROR HANDLING ========
/**
 * Hàm dừng chia sẻ màn hình và khôi phục camera
 * Được gọi khi user click nút "Dừng chia sẻ" hoặc browser tự động dừng
 * Mục đích: Chuyển từ screen video về camera video
 * 
 * LUỒNG HOẠT ĐỘNG:
 * 1. Dừng screen sharing tracks → Giải phóng tài nguyên
 * 2. Lấy lại camera stream → Khôi phục video camera
 * 3. Thay thế track → Cập nhật tất cả peer connections
 * 4. Cập nhật UI → Reset button state
 * 
 * QUAN HỆ VỚI CÁC HÀM KHÁC:
 * - startScreenShare(): Hàm đối nghịch, bắt đầu chia sẻ màn hình
 * - replaceTrack(): Thay thế video track trong peer connections
 * - setupControls(): Gọi hàm này khi click button
 */
function stopScreenShare(button) {
    try {
        // ====== BƯỚC 1: KIỂM TRA VÀ DỪNG SCREEN STREAM ======
        // Kiểm tra xem có đang chia sẻ màn hình không
        if (!screenStream) return;  // Nếu không có → thoát sớm

        // Dừng tất cả tracks trong screen stream
        // getTracks() trả về array các MediaStreamTrack (video, audio)
        screenStream.getTracks().forEach(track => {
            try {
                // Dừng từng track riêng biệt
                // track.stop() sẽ: dừng chia sẻ màn hình, giải phóng tài nguyên
                track.stop();
            } catch (error) {
                // Xử lý lỗi riêng cho từng track
                // Lỗi một track không ảnh hưởng tracks khác
                console.error('Error stopping screen track:', error);
            }
        });
        
        // Xóa reference đến screen stream
        screenStream = null;  // Cho phép garbage collector dọn dẹp

        // ====== BƯỚC 2: KHÔI PHỤC CAMERA VIDEO ======
        // Sử dụng getUserMedia() để lấy lại camera stream
        // Promise-based approach thay vì async/await để xử lý lỗi tốt hơn
        navigator.mediaDevices.getUserMedia({ video: true })
            .then(async (camStream) => {
                // Lấy video track từ camera stream
                const camTrack = camStream.getVideoTracks()[0];
                
                // Thay thế screen track bằng camera track
                // replaceTrack() sẽ cập nhật tất cả peer connections
                await replaceTrack(camTrack);
                
                // ====== BƯỚC 3: CẬP NHẬT UI - RESET BUTTON STATE ======
                button.textContent = 'Chia sẻ màn hình';  // Reset text button
                button.classList.remove('active');        // Xóa class active
            })
            .catch(error => {
                // ====== XỬ LÝ LỖI KHI KHÔNG LẤY ĐƯỢC CAMERA ======
                console.error('Error getting camera back:', error);
                showError('Không thể khôi phục camera', true);
                
                // Reset button state dù có lỗi
                button.textContent = 'Chia sẻ màn hình';
                button.classList.remove('active');
            });

    } catch (error) {
        // ====== XỬ LÝ LỖI CHUNG ======
        console.error('Error stopping screen share:', error);
        showError('Lỗi khi dừng chia sẻ màn hình', true);
    }
}

// ======== GENERAL ERROR HANDLER ========
/**
 * Hàm xử lý lỗi chung cho toàn bộ hệ thống video call
 * Được gọi khi có bất kỳ lỗi nào xảy ra trong quá trình hoạt động
 * Mục đích: Chuyển đổi lỗi kỹ thuật thành thông báo thân thiện với user
 * 
 * LUỒNG HOẠT ĐỘNG:
 * 1. Nhận error object và context → Phân tích loại lỗi
 * 2. Map lỗi kỹ thuật → Thông báo user-friendly
 * 3. Hiển thị thông báo lỗi → User hiểu được vấn đề
 * 
 * QUAN HỆ VỚI CÁC HÀM KHÁC:
 * - showError(): Hiển thị thông báo lỗi trên UI
 * - ERROR_MESSAGES: Object chứa mapping lỗi → thông báo
 * - ERROR_TYPES: Enum định nghĩa các loại lỗi
 */
function handleError(error, context = '') {
    // ====== BƯỚC 1: LOG LỖI CHI TIẾT CHO DEVELOPER ======
    // Ghi log đầy đủ thông tin lỗi để developer debug
    // error: Error object chứa thông tin lỗi
    // context: String mô tả ngữ cảnh xảy ra lỗi (optional)
    console.error('Error:', error, 'Context:', context);

    // ====== BƯỚC 2: KHỞI TẠO THÔNG BÁO LỖI MẶC ĐỊNH ======
    // Thông báo mặc định nếu không map được lỗi cụ thể
    let errorMessage = 'Đã xảy ra lỗi không xác định';

    // ====== BƯỚC 3: MAP LỖI KỸ THUẬT THÀNH THÔNG BÁO USER-FRIENDLY ======
    
    // Kiểm tra 1: Lỗi có message và có trong ERROR_MESSAGES không
    if (error.message && ERROR_MESSAGES[error.message]) {
        // Sử dụng thông báo đã được định nghĩa sẵn
        // ERROR_MESSAGES là object chứa mapping: error.message → user-friendly message
        errorMessage = ERROR_MESSAGES[error.message];
    } 
    // Kiểm tra 2: Lỗi NotAllowedError (user từ chối quyền truy cập)
    else if (error.name === 'NotAllowedError') {
        // Lỗi này xảy ra khi user từ chối cấp quyền camera/microphone
        // Sử dụng thông báo từ ERROR_TYPES.MEDIA_ACCESS_DENIED
        errorMessage = ERROR_MESSAGES[ERROR_TYPES.MEDIA_ACCESS_DENIED];
    } 
    // Kiểm tra 3: Lỗi NotFoundError (không tìm thấy thiết bị)
    else if (error.name === 'NotFoundError') {
        // Lỗi này xảy ra khi không tìm thấy camera/microphone
        // Sử dụng thông báo từ ERROR_TYPES.MEDIA_NOT_FOUND
        errorMessage = ERROR_MESSAGES[ERROR_TYPES.MEDIA_NOT_FOUND];
    } 
    // Kiểm tra 4: Có context được cung cấp
    else if (context) {
        // Tạo thông báo tùy chỉnh dựa trên context
        // Kết hợp context với error.message hoặc thông báo mặc định
        errorMessage = `${context}: ${error.message || 'Lỗi không xác định'}`;
    }

    // ====== BƯỚC 4: HIỂN THỊ THÔNG BÁO LỖI CHO USER ======
    // Gọi showError() để hiển thị thông báo trên UI
    // false = không tự động ẩn (user phải đóng thủ công)
    showError(errorMessage, false);
}

// ======== WINDOW ERROR HANDLER ========
/**
 * Hai event handler này bắt tất cả lỗi chưa được xử lý trong ứng dụng
 * Đây là "safety net" - lưới an toàn để bắt lỗi mà developer quên handle
 * 
 * MỤC ĐÍCH:
 * - Bắt lỗi JavaScript chưa được try-catch
 * - Bắt Promise rejection chưa được .catch()
 * - Đảm bảo user luôn thấy thông báo lỗi thân thiện
 * - Tránh ứng dụng crash mà không có feedback
 */

// ====== 1. ERROR EVENT HANDLER ======
window.addEventListener('error', (event) => {
    // ====== BẮT LỖI JAVASCRIPT CHƯA ĐƯỢC XỬ LÝ ======
    
    // event.error: Error object chứa thông tin lỗi
    // Ví dụ: ReferenceError, TypeError, SyntaxError, etc.
    console.error('Global error:', event.error);
    
    // Chuyển lỗi kỹ thuật thành thông báo user-friendly
    handleError(event.error, 'Lỗi hệ thống');
    
    // ====== CÁC LOẠI LỖI CÓ THỂ BẮT ======
    // - ReferenceError: Biến chưa được định nghĩa
    // - TypeError: Gọi method trên null/undefined
    // - SyntaxError: Lỗi cú pháp JavaScript
    // - RangeError: Lỗi về range (array index, etc.)
    // - URIError: Lỗi về URL encoding/decoding
});

// ====== 2. UNHANDLED REJECTION EVENT HANDLER ======
window.addEventListener('unhandledrejection', (event) => {
    // ====== BẮT PROMISE REJECTION CHƯA ĐƯỢC XỬ LÝ ======
    
    // event.reason: Lý do Promise bị reject
    // Có thể là Error object hoặc string/object khác
    console.error('Unhandled promise rejection:', event.reason);
    
    // Chuyển lỗi thành thông báo user-friendly
    handleError(event.reason, 'Lỗi xử lý');
    
    // Ngăn browser hiển thị error message mặc định
    // Nếu không có dòng này, browser sẽ hiển thị "Uncaught (in promise)"
    event.preventDefault();
});

// ======== PAGE VISIBILITY HANDLING ========
/**
 * Event handler xử lý khi user chuyển tab hoặc ẩn/hiện browser
 * Được kích hoạt khi user: chuyển tab, minimize browser, hoặc quay lại tab
 * Mục đích: Tối ưu hiệu suất và tự động kết nối lại khi cần thiết
 * 
 * LUỒNG HOẠT ĐỘNG:
 * 1. User chuyển tab → document.hidden = true → Pause video (tiết kiệm CPU)
 * 2. User quay lại tab → document.hidden = false → Resume video + check connection
 * 3. Nếu mất kết nối → Tự động thử kết nối lại SignalR
 * 
 * QUAN HỆ VỚI CÁC HÀM KHÁC:
 * - connectSignalRWithRetry(): Kết nối lại SignalR khi cần
 * - showError()/hideError(): Hiển thị thông báo trạng thái
 * - connectionState: Biến global theo dõi trạng thái kết nối
 */
document.addEventListener('visibilitychange', () => {
    // ====== BƯỚC 1: KIỂM TRA TRẠNG THÁI HIỂN THỊ ======
    if (document.hidden) {
        // ====== KHI PAGE BỊ ẨN (CHUYỂN TAB/MINIMIZE) ======
        console.log('Page hidden - pausing video');
        
        // TODO: Có thể thêm logic pause video để tiết kiệm CPU
        // Ví dụ: localStream.getVideoTracks().forEach(track => track.enabled = false);
        // Hiện tại chỉ log, chưa implement pause video
    } else {
        // ====== KHI PAGE ĐƯỢC HIỂN THỊ LẠI (QUAY LẠI TAB) ======
        console.log('Page visible - resuming video');
        
        // ====== BƯỚC 2: KIỂM TRA TRẠNG THÁI KẾT NỐI ======
        if (connectionState === 'disconnected') {
            // ====== NẾU ĐANG MẤT KẾT NỐI → THỬ KẾT NỐI LẠI ======
            
            // Hiển thị thông báo cho user biết đang thử kết nối lại
            showError('Đang thử kết nối lại...', true);
            
            // ====== BƯỚC 3: KIỂM TRA XEM CÓ ĐANG KẾT NỐI KHÔNG ======
            if (!isConnecting) {
                // ====== CHƯA CÓ KẾT NỐI ĐANG TIẾN HÀNH → BẮT ĐẦU KẾT NỐI LẠI ======
                
                // Gọi hàm kết nối lại SignalR (không khởi tạo lại toàn bộ)
                connectSignalRWithRetry().then(() => {
                    // ====== KẾT NỐI LẠI THÀNH CÔNG ======
                    console.log('✅ Reconnected successfully');
                    hideError(); // Ẩn thông báo "đang thử kết nối"
                }).catch(error => {
                    // ====== KẾT NỐI LẠI THẤT BẠI ======
                    console.error('Error reconnecting:', error);
                    showError('Không thể kết nối lại', false); // Thông báo lỗi vĩnh viễn
                });
            } else {
                // ====== ĐANG CÓ KẾT NỐI TIẾN HÀNH → KHÔNG LÀM GÌ ======
                console.log('⏳ Connection already in progress, skipping reconnect');
                // Không gọi connectSignalRWithRetry() để tránh race condition
            }
        }
        // ====== NẾU ĐANG KẾT NỐI BÌNH THƯỜNG → KHÔNG LÀM GÌ ======
        // Chỉ resume video (nếu đã pause) và tiếp tục bình thường
    }
});

// ======== CLEANUP ON PAGE UNLOAD ========
/**
 * Event handler xử lý khi user rời khỏi trang (đóng tab, refresh, navigate)
 * Được kích hoạt trước khi trang bị unload
 * Mục đích: Dọn dẹp tài nguyên và thông báo server user đã rời phòng
 * 
 * LUỒNG HOẠT ĐỘNG:
 * 1. User đóng tab/refresh → beforeunload event được trigger
 * 2. Gọi stopAll() → Dọn dẹp tất cả tài nguyên
 * 3. Thông báo server → Server cập nhật database và thông báo user khác
 * 
 * QUAN HỆ VỚI CÁC HÀM KHÁC:
 * - stopAll(): Hàm dọn dẹp chính, gọi LeaveRoom trên server
 * - MeetingHub.LeaveRoom(): Server method xử lý user rời phòng
 */
window.addEventListener('beforeunload', () => {
    // ====== GỌI HÀM DỌN DẸP CHÍNH ======
    stopAll(); // Dọn dẹp tất cả: peer connections, media streams, SignalR
    
    // LƯU Ý: Không cần return false vì modern browsers không cho phép
    // custom message trong beforeunload dialog nữa
});

// ======== HELPER FUNCTIONS FOR STATISTICS ========
/**
 * Hàm lấy lịch sử cuộc gọi từ server
 * Được gọi khi cần hiển thị thông tin về các cuộc gọi trước đó
 * Mục đích: Hiển thị danh sách các cuộc gọi đã tham gia
 * 
 * LUỒNG HOẠT ĐỘNG:
 * 1. Lấy meetingCode từ DOM → Kiểm tra có tồn tại không
 * 2. Gọi server method GetCallHistory → Nhận dữ liệu lịch sử
 * 3. Server trả về → Client xử lý và hiển thị
 * 
 * QUAN HỆ VỚI SERVER:
 * - MeetingHub.GetCallHistory(): Server method trả về call history
 * - CallSession model: Database entity lưu thông tin cuộc gọi
 * - SignalR event 'CallHistory': Nhận dữ liệu từ server
 */
function getCallHistory() {
    // ====== BƯỚC 1: LẤY MEETING CODE TỪ DOM ======
    const meetingCode = document.getElementById('video-grid')?.dataset?.meetingCode;
    
    // ====== BƯỚC 2: KIỂM TRA VÀ GỌI SERVER ======
    if (meetingCode) {
        // Gọi SignalR method để lấy lịch sử cuộc gọi
        connection.invoke('GetCallHistory', meetingCode).catch(error => {
            // ====== XỬ LÝ LỖI KHI GỌI SERVER ======
            console.error('Error getting call history:', error);
            // Có thể thêm showError() để thông báo user
        });
    }
    // ====== NẾU KHÔNG CÓ MEETING CODE → KHÔNG LÀM GÌ ======
    // Có thể xảy ra khi gọi hàm này ở trang không phải meeting room
}

/**
 * Hàm lấy thống kê cuộc gọi từ server
 * Được gọi khi cần hiển thị thông tin thống kê về cuộc gọi hiện tại
 * Mục đích: Hiển thị metrics như thời gian, số người tham gia, chất lượng
 * 
 * LUỒNG HOẠT ĐỘNG:
 * 1. Lấy meetingCode từ DOM → Kiểm tra có tồn tại không
 * 2. Gọi server method GetCallStatistics → Nhận dữ liệu thống kê
 * 3. Server trả về → Client xử lý và hiển thị
 * 
 * QUAN HỆ VỚI SERVER:
 * - MeetingHub.GetCallStatistics(): Server method trả về call stats
 * - AnalyticsEvent model: Database entity lưu thông tin thống kê
 * - SignalR event 'CallStatistics': Nhận dữ liệu từ server
 * 
 * DỮ LIỆU CÓ THỂ BAO GỒM:
 * - Thời gian cuộc gọi
 * - Số người tham gia
 * - Chất lượng video/audio
 * - Số lần reconnect
 * - Bandwidth usage
 */
function getCallStatistics() {
    // ====== BƯỚC 1: LẤY MEETING CODE TỪ DOM ======
    const meetingCode = document.getElementById('video-grid')?.dataset?.meetingCode;
    
    // ====== BƯỚC 2: KIỂM TRA VÀ GỌI SERVER ======
    if (meetingCode) {
        // Gọi SignalR method để lấy thống kê cuộc gọi
        connection.invoke('GetCallStatistics', meetingCode).catch(error => {
            // ====== XỬ LÝ LỖI KHI GỌI SERVER ======
            console.error('Error getting call statistics:', error);
            // Có thể thêm showError() để thông báo user
        });
    }
    // ====== NẾU KHÔNG CÓ MEETING CODE → KHÔNG LÀM GÌ ======
    // Có thể xảy ra khi gọi hàm này ở trang không phải meeting room
}

// ======== QUALITY CONTROL INTEGRATION FUNCTIONS ========
/**
 * Hàm kết nối Quality Control System với Video Call System
 * Được gọi từ Room.cshtml sau khi cả hai hệ thống đã khởi tạo
 * Mục đích: Thiết lập communication giữa hai hệ thống độc lập
 * 
 * LUỒNG HOẠT ĐỘNG:
 * 1. Room.cshtml khởi tạo qualityController → Gọi setQualityController()
 * 2. Lưu reference và thiết lập callback functions
 * 3. Khi user thay đổi quality → Callback được trigger → Cập nhật video
 * 
 * QUAN HỆ VỚI CÁC HÀM KHÁC:
 * - updateVideoQualityForPeers(): Được gọi khi quality thay đổi
 * - qualityController.onQualityChange: Callback từ quality control system
 * - qualityController.onStatsUpdate: Callback cho connection stats
 */
function setQualityController(controller) {
    // ====== BƯỚC 1: LƯU REFERENCE ĐẾN QUALITY CONTROLLER ======
    qualityController = controller; // Lưu reference để các hàm khác có thể sử dụng

    // ====== BƯỚC 2: THIẾT LẬP CALLBACK FUNCTIONS ======
    if (qualityController) {
        // ====== CALLBACK 1: KHI USER THAY ĐỔI QUALITY SETTING ======
        qualityController.onQualityChange = (type, quality) => {
            // type: 'video' hoặc 'audio' (hiện tại chỉ xử lý video)
            // quality: 'low', 'medium', 'high', 'auto'
            console.log(`📊 Quality changed: ${type} -> ${quality}`);
            
            // ====== CHỈ XỬ LÝ VIDEO QUALITY HIỆN TẠI ======
            if (type === 'video' && localStream) {
                // Gọi hàm cập nhật video quality cho tất cả peers
                updateVideoQualityForPeers(quality);
            }
            // TODO: Có thể thêm xử lý audio quality sau này
            // if (type === 'audio' && localStream) {
            //     updateAudioQualityForPeers(quality);
            // }
        };

        // ====== CALLBACK 2: KHI CONNECTION STATS CẬP NHẬT ======
        qualityController.onStatsUpdate = (stats) => {
            // stats: Object chứa thông tin về connection quality
            // Ví dụ: { bandwidth: 1500, latency: 50, packetLoss: 0.1 }
            console.log('📊 Connection stats updated:', stats);
            
            // Có thể thêm logic xử lý stats nếu cần
            // Ví dụ: Auto-adjust quality dựa trên network conditions
            // if (stats.bandwidth < 500) {
            //     // Tự động giảm quality nếu bandwidth thấp
            //     updateVideoQualityForPeers('low');
            // }
        };
    }
}

/**
 * Hàm cập nhật chất lượng video cho tất cả peer connections
 * Được gọi khi user thay đổi video quality setting
 * Mục đích: Áp dụng video constraints mới cho local stream
 * 
 * LUỒNG HOẠT ĐỘNG:
 * 1. User thay đổi quality → onQualityChange callback
 * 2. Gọi updateVideoQualityForPeers() → Lấy quality profile
 * 3. Áp dụng constraints → Cập nhật video track
 * 4. WebRTC tự động sync → Tất cả peers nhận được video mới
 * 
 * QUAN HỆ VỚI WEBRTC:
 * - applyConstraints(): WebRTC API để thay đổi video constraints
 * - getVideoTracks(): Lấy video track từ MediaStream
 * - Tự động sync với tất cả peer connections
 * 
 * QUALITY PROFILES (từ quality-control.js):
 * - low: 640x360, 20fps
 * - medium: 854x480, 24fps  
 * - high: 1280x720, 30fps
 * - auto: Tự động điều chỉnh
 */
function updateVideoQualityForPeers(quality) {
    // ====== BƯỚC 1: KIỂM TRA ĐIỀU KIỆN ======
    if (!qualityController || !localStream) return; 
    // Không có controller hoặc chưa có local stream

    // ====== BƯỚC 2: LẤY QUALITY PROFILE ======
    const profile = qualityController.qualityProfiles[quality];
    if (!profile || quality === 'auto') return; 
    // Không có profile hoặc auto mode (không cần thay đổi thủ công)

    // ====== BƯỚC 3: ÁP DỤNG CONSTRAINTS CHO VIDEO TRACK ======
    const videoTrack = localStream.getVideoTracks()[0]; // Lấy video track đầu tiên
    if (videoTrack) {
        // Lấy constraints từ quality profile
        const constraints = profile.video;
        
        // Áp dụng constraints mới cho video track
        videoTrack.applyConstraints({
            width: { ideal: constraints.width },      // Chiều rộng video
            height: { ideal: constraints.height },    // Chiều cao video
            frameRate: { ideal: constraints.frameRate } // FPS (frames per second)
        }).then(() => {
            // ====== THÀNH CÔNG ======
            console.log(`✅ Applied video constraints: ${constraints.width}x${constraints.height}@${constraints.frameRate}fps`);
            
            // WebRTC sẽ tự động:
            // 1. Cập nhật local video stream
            // 2. Gửi video stream mới đến tất cả peers
            // 3. Peers nhận được video với quality mới
            // 4. Không cần reload hay reconnect
        }).catch(error => {
            // ====== THẤT BẠI ======
            console.warn('⚠️ Failed to apply video constraints:', error);
            
            // Có thể xảy ra khi:
            // - Camera không hỗ trợ resolution này
            // - Browser không hỗ trợ applyConstraints API
            // - Hardware không đủ mạnh
            // - User chưa cấp quyền camera
        });
    }
    // ====== NẾU KHÔNG CÓ VIDEO TRACK → KHÔNG LÀM GÌ ======
    // Có thể xảy ra khi user chưa bật camera
}

// ======== CONNECTION METRICS FUNCTION ========
/**
 * Hàm lấy thông tin metrics về trạng thái kết nối hiện tại
 * Được gọi bởi các hệ thống khác để kiểm tra tình trạng video call
 * Mục đích: Cung cấp thông tin realtime về số lượng peers, trạng thái kết nối, và media streams
 * 
 * LUỒNG HOẠT ĐỘNG:
 * 1. Đếm số lượng peer connections hiện tại
 * 2. Kiểm tra trạng thái SignalR connection
 * 3. Kiểm tra trạng thái local media stream
 * 4. Đếm số lượng video/audio tracks
 * 5. Trả về object chứa tất cả thông tin
 * 
 * QUAN HỆ VỚI CÁC HÀM KHÁC:
 * - Quality Control System: Sử dụng để hiển thị connection stats
 * - Recording System: Kiểm tra có stream để record không
 * - Stats Sidebar: Hiển thị thông tin realtime
 * - Error Handling: Kiểm tra trạng thái trước khi thực hiện actions
 * 
 * BIẾN GLOBAL ĐƯỢC SỬ DỤNG:
 * - peers: Object chứa tất cả peer connections
 * - connectionState: Trạng thái SignalR connection
 * - localStream: MediaStream của user hiện tại
 */
function getConnectionMetrics() {
    // ====== BƯỚC 1: ĐẾM SỐ LƯỢNG PEER CONNECTIONS ======
    const peerCount = Object.keys(peers).length;
    // peers = { "peer1": SimplePeerObject, "peer2": SimplePeerObject, ... }
    // Object.keys(peers) = ["peer1", "peer2", ...]
    // .length = Số lượng peer connections hiện tại
    
    // ====== BƯỚC 2: LẤY TRẠNG THÁI SIGNALR CONNECTION ======
    // connectionState có thể là: 'connected', 'connecting', 'disconnected', 'reconnecting'
    // Được cập nhật bởi updateConnectionStatus() function
    
    // ====== BƯỚC 3: KIỂM TRA TRẠNG THÁI LOCAL STREAM ======
    const localStreamActive = localStream && localStream.active;
    // localStream: MediaStream object từ getUserMedia()
    // .active: Boolean cho biết stream có đang hoạt động không
    // Có thể false khi: user tắt camera/mic, browser suspend, network issues
    
    // ====== BƯỚC 4: ĐẾM SỐ LƯỢNG MEDIA TRACKS ======
    const streamTracks = localStream ? {
        video: localStream.getVideoTracks().length,  // Số video tracks (thường là 1)
        audio: localStream.getAudioTracks().length   // Số audio tracks (thường là 1)
    } : null;
    // getVideoTracks(): Trả về array các video tracks
    // getAudioTracks(): Trả về array các audio tracks
    // .length: Số lượng tracks của mỗi loại
    
    // ====== BƯỚC 5: TRẢ VỀ OBJECT CHỨA TẤT CẢ METRICS ======
    return {
        peerCount: peerCount,                    // Số người tham gia cuộc gọi
        connectionState: connectionState,        // Trạng thái kết nối server
        localStreamActive: localStreamActive,    // Stream có hoạt động không
        streamTracks: streamTracks               // Số lượng video/audio tracks
    };
}

// ======== EXPOSE FUNCTIONS FOR QUALITY CONTROLLER ========
// Mở rộng (expose) các biến và hàm để các file JavaScript khác có thể sử dụng
// Cho phép quality-control.js, recording-system.js, etc. truy cập vào dữ liệu của videocall.js

// Cho phép file khác truy cập danh sách tất cả peer connections
// Ví dụ: quality-control.js có thể dùng window.peers để điều chỉnh chất lượng cho từng peer
window.peers = peers;

// Cho phép file khác lấy stream video/audio hiện tại thông qua function
// Dùng function thay vì expose trực tiếp để bảo mật và linh hoạt hơn
// File khác có thể gọi: window.localStream() để lấy stream hiện tại
window.localStream = () => localStream;


// ======== UPDATE VIDEO GRID LAYOUT ========
// Hàm này cập nhật thông tin số lượng video trong grid và hỗ trợ CSS điều chỉnh layout
// Được gọi khi thêm hoặc xóa video để đảm bảo layout hiển thị đúng
function updateVideoGridLayout() {
    // Tìm container chính chứa tất cả video
    const videoGrid = document.getElementById('video-grid');
    if (!videoGrid) return; // Nếu không tìm thấy → thoát

    // Đếm số lượng video containers hiện tại
    // querySelectorAll('.video-container') tìm tất cả element có class video-container
    // .length trả về số lượng video
    const videoCount = videoGrid.querySelectorAll('.video-container').length;

    // Cập nhật data-count attribute với số lượng video
    // CSS có thể dùng data-count để điều chỉnh layout
    // Ví dụ: data-count="1" → 1 video, data-count="3" → 3 video
    videoGrid.setAttribute('data-count', videoCount);
}


