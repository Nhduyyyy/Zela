// ======== CẤU HÌNH STUN/TURN ========
const ICE_SERVERS = [
    { urls: 'stun:stun.l.google.com:19302' },
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

// ======== KHỞI TẠO KẾT NỐI SIGNALR ========
const connection = new signalR.HubConnectionBuilder()
    .withUrl('/meetingHub')
    .build();

// ======== KHI DOM ĐÃ SẴN SÀNG ========
document.addEventListener('DOMContentLoaded', () => {
    setupControls();
    start().catch(err => console.error('Error starting call:', err));
});

// ======== 1. HÀM KHỞI ĐỘNG CUỘC GỌI ========
async function start() {
    try {
        // 1.1 LẤY STREAM CAMERA + MIC
        localStream = await navigator.mediaDevices.getUserMedia({ video: true, audio: true });
        addVideo(localStream, 'self');

        // 1.2 ĐĂNG KÝ CÁC EVENT TỪ HUB
        connection.on('Peers', list => {
            console.log('Peers:', list);
            list.forEach(id => initPeer(id, true));
        });
        connection.on('NewPeer', id => {
            console.log('NewPeer:', id);
            initPeer(id, false);
        });
        connection.on('Signal', (from, data) => {
            if (peers[from]) peers[from].signal(data);
        });
        connection.on('CallEnded', () => {
            alert('Cuộc gọi đã kết thúc');
            stopAll();
            window.location.href = '/Meeting/Index';
        });

        // 1.3 KẾT NỐI SIGNALR
        await connection.start();
        console.log('SignalR connected');

        // 1.4 JOIN ROOM
        const meetingCode = document.getElementById('video-grid').dataset.meetingCode;
        await connection.invoke('JoinRoom', meetingCode);
        console.log('Joined room', meetingCode);

    } catch (e) {
        console.error('Failed to start video call:', e);
    }
}

// ======== 2. KHỞI TẠO PEER ========
function initPeer(peerId, initiator) {
    if (peers[peerId]) return; // đã khởi tạo rồi

    const peer = new SimplePeer({
        initiator,
        stream: localStream,
        config: { iceServers: ICE_SERVERS }
    });

    // 2.1 Khi có offer/answer/ICE mới
    peer.on('signal', data => {
        connection.invoke('Signal', peerId, data).catch(console.error);
    });

    // 2.2 Khi nhận stream của peer
    peer.on('stream', stream => addVideo(stream, peerId));

    peers[peerId] = peer;
}

// ======== 3. HIỂN THỊ VIDEO ========
function addVideo(stream, id) {
    const grid = document.getElementById('video-grid');
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

        container.appendChild(video);
        grid.appendChild(container);
    } else {
        video = container.querySelector('video');
    }

    video.srcObject = stream;
}

// ======== 4. DỪNG VÀ GIẢI PHÓNG TẤT CẢ ========
function stopAll() {
    // dừng camera/mic
    if (localStream) {
        localStream.getTracks().forEach(t => t.stop());
    }
    // dừng screen share
    if (screenStream) {
        screenStream.getTracks().forEach(t => t.stop());
    }
    // destroy peers
    Object.values(peers).forEach(p => p.destroy());
    Object.keys(peers).forEach(k => delete peers[k]);
}

// ======== 5. CÀI ĐẶT CÁC NÚT ĐIỀU KHIỂN ========
function setupControls() {
    const btnMic   = document.getElementById('toggle-mic');
    const btnCam   = document.getElementById('toggle-cam');
    const btnShare = document.getElementById('share-screen');
    const btnLeave = document.getElementById('btnLeave');
    const btnEnd   = document.getElementById('btnEnd');

    if (!btnMic || !btnCam || !btnShare || !btnLeave) {
        console.error('Thiếu button điều khiển trong DOM');
        return;
    }

    // 5.1 Tắt/Mở mic
    btnMic.addEventListener('click', () => {
        const track = localStream.getAudioTracks()[0];
        if (!track) return;
        track.enabled = !track.enabled;
        btnMic.textContent = track.enabled ? 'Tắt mic' : 'Mở mic';
        btnMic.classList.toggle('active', !track.enabled);
    });

    // 5.2 Tắt/Mở camera
    btnCam.addEventListener('click', () => {
        const track = localStream.getVideoTracks()[0];
        if (!track) return;
        track.enabled = !track.enabled;
        btnCam.textContent = track.enabled ? 'Tắt cam' : 'Mở cam';
        btnCam.classList.toggle('active', !track.enabled);
    });

    // 5.3 Chia sẻ/Dừng chia sẻ màn hình
    btnShare.addEventListener('click', async () => {
        if (!screenStream) {
            try {
                screenStream = await navigator.mediaDevices.getDisplayMedia({ video: true });
                const screenTrack = screenStream.getVideoTracks()[0];
                replaceTrack(screenTrack);
                btnShare.textContent = 'Dừng chia sẻ';
                btnShare.classList.add('active');
                screenTrack.onended = () => stopScreenShare(btnShare);
            } catch (e) {
                console.error('Không thể chia sẻ màn hình:', e);
            }
        } else {
            stopScreenShare(btnShare);
        }
    });

    // 5.4 Rời cuộc gọi
    btnLeave.addEventListener('click', () => {
        stopAll();
        window.location.href = '/Meeting/Index';
    });

    // 5.5 Kết thúc cuộc gọi (chỉ host có nút)
    btnEnd?.addEventListener('click', async () => {
        const code = document.getElementById('video-grid').dataset.meetingCode;
        await connection.invoke('EndRoom', code).catch(console.error);
    });
}

// ======== 6. THAY THẾ VIDEO TRACK CHO SCREEN SHARE ========
function replaceTrack(newTrack) {
    // 6.1 Thay trong localStream
    const old = localStream.getVideoTracks()[0];
    if (old) { localStream.removeTrack(old); old.stop(); }
    localStream.addTrack(newTrack);

    // 6.2 Thay cho từng peer
    Object.values(peers).forEach(p => {
        const sender = p._pc.getSenders().find(s => s.track && s.track.kind === newTrack.kind);
        if (sender) sender.replaceTrack(newTrack);
    });
}

// ======== 7. DỪNG CHIA SẺ MÀN HÌNH ========
function stopScreenShare(button) {
    if (!screenStream) return;
    screenStream.getTracks().forEach(t => t.stop());
    screenStream = null;

    // Lấy lại camera
    navigator.mediaDevices.getUserMedia({ video: true }).then(camStream => {
        const camTrack = camStream.getVideoTracks()[0];
        replaceTrack(camTrack);
        button.textContent = 'Chia sẻ màn hình';
        button.classList.remove('active');
    }).catch(console.error);
}

