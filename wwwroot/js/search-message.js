(()=>{
    let searchMessageInput = null;
    let searchMessageResults = null;
    let searchMessageDebounce = null;
    let searchResults = [];
    let currentResultIndex = 0;
    
    // Khởi tạo kết nối SignalR (signalR đã được load trước qua CDN hoặc script tag)
    let connection = new signalR.HubConnectionBuilder()
        .withUrl('/chathub')
        .build();

    connection.start().catch(err => console.error('SignalR error:', err));
    
    // Lấy ID người đang chat
    function getCurrentFriendId() {
        return Number(currentFriendId);
    }

    // Hiển thị đang tìm kiếm
    function showSearchLoading() {
        if (searchMessageResults) {
            searchMessageResults.innerHTML = '';
        }
    }

    // Hiển thị lỗi
    function showSearchError() {
        if (searchMessageResults) {
            searchMessageResults.innerHTML = '';
        }
    }

    // Hiển thị kết quả JSON
    function renderSearchMessageResults(messages) {
        searchResults = messages;
        currentResultIndex = 0;

        if (!searchMessageResults) return;

        searchMessageResults.innerHTML = '';
        if (!messages || messages.length === 0) {
            searchMessageResults.innerHTML = '<div class="text-muted small">Không có kết quả phù hợp.</div>';
            return;
        }

        const list = document.createElement('ul');
        list.classList.add('search-results-list');

        messages.forEach((msg, index) => {
            const li = document.createElement('li');
            li.classList.add('search-result-item');
            li.innerHTML = `
            <strong>${msg.senderName}</strong><br>
            <span>${escapeHtml(msg.content)}</span><br>
            <small class="text-muted">${formatDateTime(msg.sentAt)}</small>
        `;
            li.addEventListener('click', () => scrollToMessage(msg.messageId));
            list.appendChild(li);
        });

        searchMessageResults.appendChild(list);
    }

    // Cuộn tới tin nhắn cụ thể
    function scrollToMessage(messageId) {
        const el = document.querySelector(`[data-message-id="${messageId}"]`);
        if (el) {
            el.scrollIntoView({ behavior: 'smooth', block: 'center' });
            el.classList.add('highlight');
            setTimeout(() => el.classList.remove('highlight'), 2000);
        } else {
            console.error("không thể cuộn tới tin nhắn gốc");
        }
    }

    // Thực hiện gọi API tìm kiếm
    function doSearchMessage() {
        const keyword = searchMessageInput?.value.trim();
        const friendId = getCurrentFriendId();

        if (!keyword) {
            searchMessageResults.innerHTML = '';
            return;
        }
        if (!friendId) {
            searchMessageResults.innerHTML = '<div class="text-muted small">Vui lòng chọn một cuộc trò chuyện để tìm kiếm.</div>';
            return;
        }

        showSearchLoading();

        connection.invoke('SearchMessages', friendId, keyword)
            .then((messages) => {
                renderSearchMessageResults(messages);
            })
            .catch(err => {
                console.error('Lỗi khi tìm kiếm:', err);
                showSearchError();
            });
    }

    // Khởi tạo sau DOM ready
    function setupSearchMessage() {
        searchMessageInput = document.getElementById('searchMessageInput');
        searchMessageResults = document.getElementById('searchMessageResults');
        const searchMessageBtn = document.getElementById('searchMessageBtn');

        // FIX: Kiểm tra elements tồn tại với log debug
        if (!searchMessageInput || !searchMessageResults) {
            console.warn('Search message elements not found:', {
                input: !!searchMessageInput,
                results: !!searchMessageResults
            });
            return;
        }

        console.log('Search message initialized successfully');

        searchMessageInput.addEventListener('input', function () {
            clearTimeout(searchMessageDebounce);
            const keyword = this.value.trim();
            if (!keyword) {
                searchMessageResults.innerHTML = '<div <a>Không có kết quả phù hợp</a></div>';
                return;
            }

            searchMessageDebounce = setTimeout(() => {
                doSearchMessage();
            }, 1000); // ⏱ Delay 1s
        });

        searchMessageBtn?.addEventListener('click', function (e) {
            e.preventDefault();
            doSearchMessage();
        });
    }

    // Tránh XSS đơn giản
    function escapeHtml(str) {
        if (!str) return '';
        return str.replace(/[&<>"']/g, function (m) {
            return ({
                '&': '&amp;', '<': '&lt;', '>': '&gt;',
                '"': '&quot;', "'": '&#039;'
            })[m];
        });
    }

    // Format thời gian
    function formatDateTime(iso) {
        try {
            const d = new Date(iso);
            return d.toLocaleString('vi-VN', {
                hour: '2-digit', minute: '2-digit',
                day: '2-digit', month: '2-digit', year: 'numeric'
            });
        } catch (error) {
            return iso; // Fallback nếu format lỗi
        }
    }

    // Hiển thị sidebar tìm kiếm tin nhắn
    function showSearchMessageSidebar() {
        const sidebar = document.getElementById('searchMessageSidebar');
        if (sidebar) {
            sidebar.classList.remove('hidden');
            sidebar.classList.add('active'); // tuỳ vào CSS mà bạn dùng
        } else {
            console.warn('Sidebar phần tử #searchMessageSidebar không tồn tại');
        }
    }

    // Bắt sự kiện khi nhấn nút bi-search
    const biSearchBtn = document.getElementById('biSearchBtn');
    if (biSearchBtn) {
        biSearchBtn.addEventListener('click', function (e) {
            e.preventDefault();
            showSearchMessageSidebar();
        });
    } else {
        console.warn('Không tìm thấy nút bi-search với id #biSearchBtn');
    }

    // Init
    document.addEventListener('DOMContentLoaded', function() {
        setupSearchMessage();
        console.log('DOM loaded: setupSearchMessage has been called successfully.');
    });
    window.setupSearchMessage = setupSearchMessage;
})();
