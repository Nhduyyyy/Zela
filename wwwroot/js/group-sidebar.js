// group-sidebar.js
$(function() {
    // Hover hiện nút action
    $(document).on('mouseenter', '.group-member-item', function() {
        console.log('[DEBUG] mouseenter .group-member-item', this);
        $(this).find('.member-actions').show();
    });
    $(document).on('mouseleave', '.group-member-item', function() {
        console.log('[DEBUG] mouseleave .group-member-item', this);
        $(this).find('.member-actions').hide();
    });

    // Kick member
    $(document).on('click', '.btn-kick-member', function() {
        console.log('[DEBUG] click .btn-kick-member', this);
        if (!confirm('Bạn có chắc muốn kick thành viên này khỏi nhóm?')) return;
        var groupId = $('.chat-info-panel').data('group-id');
        var userId = $(this).data('user-id');
        console.log('[DEBUG] KickMember groupId:', groupId, 'userId:', userId);
        $.post('/GroupChat/KickMember', { groupId: groupId, userId: userId }, function(res) {
            console.log('[DEBUG] KickMember response:', res);
            alert(res.message);
            if (res.success) location.reload();
        });
    });

    // Ban member - mở modal
    $(document).on('click', '.btn-ban-member', function() {
        console.log('[DEBUG] click .btn-ban-member', this);
        var userId = $(this).data('user-id');
        var userName = $(this).data('user-name');
        console.log('[DEBUG] BanMember open modal userId:', userId, 'userName:', userName);
        $('#banUserId').val(userId);
        $('#banUserName').val(userName);
        $('#banHours').val(1);
        var modal = new bootstrap.Modal(document.getElementById('banMemberModal'));
        modal.show();
    });

    // Xác nhận ban
    $(document).on('click', '#confirmBanBtn', function() {
        console.log('[DEBUG] click #confirmBanBtn');
        var groupId = $('.chat-info-panel').data('group-id');
        var userId = $('#banUserId').val();
        var banHours = $('#banHours').val();
        console.log('[DEBUG] BanMember submit groupId:', groupId, 'userId:', userId, 'banHours:', banHours);
        $.post('/GroupChat/BanMember', { groupId: groupId, userId: userId, banHours: banHours }, function(res) {
            console.log('[DEBUG] BanMember response:', res);
            alert(res.message);
            if (res.success) location.reload();
        });
    });

    // Unban member
    $(document).on('click', '.btn-unban-member', function() {
        console.log('[DEBUG] click .btn-unban-member', this);
        if (!confirm('Bạn có chắc muốn gỡ ban thành viên này?')) return;
        var groupId = $('.chat-info-panel').data('group-id');
        var userId = $(this).data('user-id');
        console.log('[DEBUG] UnbanMember groupId:', groupId, 'userId:', userId);
        $.post('/GroupChat/UnbanMember', { groupId: groupId, userId: userId }, function(res) {
            console.log('[DEBUG] UnbanMember response:', res);
            alert(res.message);
            if (res.success) location.reload();
        });
    });
}); 