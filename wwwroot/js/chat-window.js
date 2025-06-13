let currentFriendId = null;

// Kết nối SignalR
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/chathub")
    .build();

// Nhận message mới (realtime, từ chính mình hoặc bạn bè)
connection.on("ReceiveMessage", function (msg) {
    console.log("SignalR nhận được:", msg);

    let currentId = Number(currentFriendId);

    if (currentId && (msg.senderId === currentId || msg.recipientId === currentId)) {
        // FIX: Chỉ append vào nội dung bên trong chat-content, không tạo nested
        $('.chat-content').append(renderMessage(msg));
        scrollToBottom();
    }
});


connection.start().catch(err => console.error(err.toString()));

// Khi click chọn một người bạn → load lịch sử chat
$(document).on('click', '.friend-item', function () {
    currentFriendId = Number($(this).data('id'));

    $('.friend-item').removeClass('active');
    $(this).addClass('active');

    $('#chat-friend-name').text($(this).find('.friend-name').text());

    // Hiện toàn bộ phần thông tin người chat nếu đang bị ẩn
    $('.chat-user-info').removeClass('d-none').show(); // nếu dùng Bootstrap
    // hoặc: $('.chat-user-info').show();
    // Ẩn tất cả user trong phần info
    $('.chat-user-info .chat-user').hide();

    // Hiện đúng người được chọn
    $('.chat-user-info .chat-user[data-id="' + currentFriendId + '"]').show();

    // Ẩn placeholder nếu có
    $('.chat-content .no-chat-placeholder').hide();

    // FIX: Chỉ load nội dung messages, không load cả wrapper chat-content
    $.get('/Chat/GetMessages', { friendId: currentFriendId }, function(html) {
        // Nếu server trả về có chứa <div class="chat-content">, chỉ lấy nội dung bên trong
        let $response = $(html);
        if ($response.hasClass('chat-content')) {
            $('.chat-content').html($response.html());
        } else {
            $('.chat-content').html(html);
        }
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

    connection.invoke("SendMessage", currentFriendId, content)
        .then(() => {
            console.log("Tin nhắn đã gửi thành công");
        })
        .catch(err => console.error(err.toString()));

    $('.chat-input-bar input').val('');
}

// Render tin nhắn mới - FIX: Chỉ trả về nội dung message, không có wrapper chat-content
function renderMessage(msg) {
    let isMine = msg.senderId === currentUserId;
    let side = isMine ? 'right' : 'left';

    if (isMine) {
        return `<div class="message ${side}">
            <div class="message-content">
                <span class="message-time">${msg.sentAt.substring(11, 16)}</span>
                <span class="message-bubble">${msg.content}</span>
            </div>
        </div>`;
    } else {
        return `<div class="message ${side}">
            <img src="${msg.avatarUrl}" class="message-avatar" />
            <div class="message-content">
                <span class="message-time">${msg.sentAt.substring(11, 16)}</span>
                <span class="message-bubble">${msg.content}</span>
            </div>
        </div>`;
    }
}

function scrollToBottom() {
    let chatContent = $('.chat-content');
    chatContent.scrollTop(chatContent[0].scrollHeight);
}

$(document).on('click', '.btn-video-call', function() {
    let peerId = $(this).data('peer');
    window.location.href = '/VideoCall/Room?userId=' + peerId;
});
