let currentFriendId = null;

// Kết nối SignalR
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/chathub")
    .build();

// Nhận message mới (realtime, từ chính mình hoặc bạn bè)
connection.on("ReceiveMessage", function (msg) {
    console.log("SignalR nhận được:", msg, "currentFriendId:", currentFriendId, typeof currentFriendId, "senderId:", msg.senderId, typeof msg.senderId);

    // Ép kiểu currentFriendId về số để so sánh chắc chắn đúng
    let currentId = Number(currentFriendId);

    // Dùng đúng key camelCase như server trả về!
    if (currentId && (msg.senderId === currentId || msg.recipientId === currentId)) {
        $('.chat-content').append(renderMessage(msg));
        scrollToBottom();
    }
});

connection.start().catch(err => console.error(err.toString()));

// Khi click chọn một người bạn → load lịch sử chat
$(document).on('click', '.friend-item', function() {
    currentFriendId = Number($(this).data('id')); // ép kiểu về số ở đây luôn!
    $('.friend-item').removeClass('active');
    $(this).addClass('active');
    $('#chat-friend-name').text($(this).find('.friend-name').text());
    // AJAX load toàn bộ lịch sử chat cũ
    $.get('/Chat/GetMessages', { friendId: currentFriendId }, function(html) {
        $('.chat-content').html(html);
        scrollToBottom();
    });
});

// Gửi tin nhắn (nhấn Enter hoặc bấm icon gửi)
$(document).on('click', '.bi-send', function() {
    sendMessage();
});
$('.chat-input-bar input').on('keypress', function(e) {
    if (e.which === 13) sendMessage();
});

function sendMessage() {
    let content = $('.chat-input-bar input').val();
    if (!content.trim() || !currentFriendId) return;
    connection.invoke("SendMessage", currentFriendId, content);
    $('.chat-input-bar input').val('');
}

// Render tin nhắn mới
function renderMessage(msg) {
    let side = msg.isMine ? 'right' : 'left'; // camelCase
    return `<div class="message ${side}">
        <img src="${msg.avatarUrl}" class="message-avatar" />
        <span class="message-text">${msg.content}</span>
        <span class="message-time">${msg.sentAt.substring(11,16)}</span>
    </div>`;
}

function scrollToBottom() {
    let chatContent = $('.chat-content');
    chatContent.scrollTop(chatContent[0].scrollHeight);
}

$(document).on('click', '.btn-video-call', function() {
    let peerId = $(this).data('peer');
    window.location.href = '/VideoCall/Room?userId=' + peerId;
});
