document.addEventListener('DOMContentLoaded', function() {
    const searchInput = document.getElementById('searchInput');
    const resultsBody = document.getElementById('resultsBody');
    let searchTimeout;

    searchInput.addEventListener('input', function() {
        clearTimeout(searchTimeout);
        const query = this.value.toLowerCase().trim();

        searchTimeout = setTimeout(function() {
            const rows = resultsBody.getElementsByTagName('tr');

            for (let i = 0; i < rows.length; i++) {
                const row = rows[i];
                const cells = row.getElementsByTagName('td');
                const userId = cells[0].textContent.toLowerCase();
                const userName = cells[1].textContent.toLowerCase();

                if (query === '') {
                    row.style.display = '';
                } else {
                    if (userId.includes(query) || userName.includes(query)) {
                        row.style.display = '';
                    } else {
                        row.style.display = 'none';
                    }
                }
            }
        }, 300);
    });

    const forms = document.querySelectorAll('form[action*="SendRequest"]');
    forms.forEach(form => {
        form.addEventListener('submit', function(e) {
            e.preventDefault();

            const formData = new FormData(form);
            const userId2 = formData.get('userId2');
            const button = form.querySelector('button');
            const td = form.closest('td');

            button.disabled = true;
            button.textContent = 'Đang xử lý...';

            fetch('/Friendship/SendRequest', {
                method: 'POST',
                body: formData,
                headers: {
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
                }
            })
                .then(response => {
                    if (response.redirected) {
                        window.location.href = response.url;
                        return;
                    }
                    return response.text();
                })
                .then(html => {
                    if (html) {
                        td.innerHTML = '<span class="text-secondary">Đang chờ</span>';
                    }
                })
                .catch(error => {
                    console.error('Error:', error);
                    button.disabled = false;
                    button.textContent = 'Kết bạn';
                    alert('Có lỗi xảy ra khi gửi lời mời kết bạn');
                });
        });
    });
});

function getStatusHtml(role) {
    switch (role) {
        case 0: return '<span class="text-muted">Chưa là bạn</span>';
        case 1: return '<span class="text-info">Đã gửi lời mời</span>';
        case 2: return '<span class="text-warning">Có lời mời đến</span>';
        case 3: return '<span class="text-success">Đã là bạn</span>';
        default: return '';
    }
}

function getActionHtml(user) {
    switch (user.role) {
        case 0:
            return `
                <form action="/Friendship/SendRequest" method="post" class="mb-0">
                    <input type="hidden" name="userId2" value="${user.userId}" />
                    <button type="submit" class="btn btn-sm btn-primary">Kết bạn</button>
                </form>`;
        case 1:
            return '<span class="text-secondary">Đang chờ</span>';
        case 2:
            return '<span class="text-secondary">Chờ phản hồi</span>';
        case 3:
            return `
                <form action="/Friendship/RemoveFriend" method="post" class="mb-0">
                    <input type="hidden" name="friendId" value="${user.userId}" />
                    <button type="submit" class="btn btn-sm btn-danger">Hủy bạn</button>
                </form>`;
        default:
            return '';
    }
}
