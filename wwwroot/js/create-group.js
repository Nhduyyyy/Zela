$(function() {
    const $input = $('#groupLinkInput');
    const $btn = $('#groupActionBtn');
    const $icon = $('#groupActionIcon');
    const $text = $('#groupActionText');
    let isJoin = false;

    function isGroupLink(val) {
        return /^https?:\/\//i.test(val.trim());
    }

    $input.on('input', function() {
        const val = $input.val();
        const shouldJoin = isGroupLink(val);
        if (shouldJoin !== isJoin) {
            isJoin = shouldJoin;
            $btn.animate({
                opacity: 0.2,
                transform: 'scale(0.95)'
            }, 100, function() {
                if (isJoin) {
                    $btn.removeClass('btn-primary').addClass('btn-success group-join-animate');
                    $icon.removeClass('bi-plus-circle').addClass('bi-link-45deg');
                    $text.text('Tham gia');
                    $btn.attr('data-bs-target', '#joinGroupModal');
                } else {
                    $btn.removeClass('btn-success group-join-animate').addClass('btn-primary');
                    $icon.removeClass('bi-link-45deg').addClass('bi-plus-circle');
                    $text.text('Tạo nhóm');
                    $btn.attr('data-bs-target', '#createGroupModal');
                }
                $btn.animate({
                    opacity: 1,
                    transform: 'scale(1)'
                }, 180);
            });
        }
    });

    $btn.on('click', function() {
        if (isJoin) {
            const val = $input.val().trim();
            // Giả sử link nhóm có dạng .../GroupChat/Join?groupId=123
            const match = val.match(/groupId=(\d+)/i);
            if (match) {
                const groupId = match[1];
                window.location.href = `/GroupChat/Join?groupId=${groupId}`;
            } else {
                // Nếu không đúng định dạng, có thể thông báo lỗi
                alert('Link nhóm không hợp lệ!');
            }
        }
    });
}); 