// Create Group Functionality
$(document).ready(function() {
    // Debug: Check if modal exists
    const modalElement = document.getElementById('createGroupModal');
    if (!modalElement) {
        console.error('Modal element not found!');
        return;
    }

    // Initialize create group modal
    const createGroupModal = new bootstrap.Modal(modalElement);

    // Debug: Check modal backdrop
    $('#createGroupBtn').on('click', function() {
        console.log('Create group button clicked');
        console.log('Modal element:', modalElement);
        console.log('Modal backdrop elements:', document.querySelectorAll('.modal-backdrop'));
    });

    // Handle create group button click
    $('#submitCreateGroup').on('click', function() {
        const name = $('#groupName').val().trim();
        const description = $('#groupDescription').val().trim();

        // Validation
        if (!name) {
            showToast('Vui lòng nhập tên nhóm', 'warning');
            return;
        }

        if (name.length > 100) {
            showToast('Tên nhóm không được vượt quá 100 ký tự', 'warning');
            return;
        }

        if (description.length > 50) {
            showToast('Mô tả không được vượt quá 50 ký tự', 'warning');
            return;
        }

        // Disable button và hiển thị loading
        const $btn = $(this);
        const originalText = $btn.html();
        $btn.prop('disabled', true).html('<i class="bi bi-hourglass-split me-1"></i>Đang tạo...');

        // Gọi API tạo nhóm
        $.ajax({
            url: '/GroupChat/CreateGroup',
            type: 'POST',
            data: {
                name: name,
                description: description
            },
            success: function(response) {
                if (response.success) {
                    showToast('Tạo nhóm thành công!', 'success');
                    createGroupModal.hide();
                    $('#createGroupForm')[0].reset();

                    // Reload trang nếu đang ở trang GroupChat
                    const currentPath = window.location.pathname;
                    if (currentPath.includes('/GroupChat')) {
                        setTimeout(() => {
                            window.location.reload();
                        }, 1000);
                    }
                } else {
                    showToast(response.message || 'Có lỗi xảy ra khi tạo nhóm', 'error');
                }
            },
            error: function(xhr) {
                let errorMessage = 'Có lỗi xảy ra khi tạo nhóm';
                if (xhr.responseJSON && xhr.responseJSON.message) {
                    errorMessage = xhr.responseJSON.message;
                }
                showToast(errorMessage, 'error');
            },
            complete: function() {
                // Restore button
                $btn.prop('disabled', false).html(originalText);
            }
        });
    });

    // Reset form khi đóng modal
    $('#createGroupModal').on('hidden.bs.modal', function() {
        $('#createGroupForm')[0].reset();
        $('#submitCreateGroup').prop('disabled', false).html('<i class="bi bi-check-circle me-1"></i>Tạo nhóm');

        // Debug: Clean up any duplicate backdrops
        const backdrops = document.querySelectorAll('.modal-backdrop');
        if (backdrops.length > 1) {
            console.log('Found multiple backdrops, cleaning up...');
            backdrops.forEach((backdrop, index) => {
                if (index > 0) {
                    backdrop.remove();
                }
            });
        }
    });

    // Handle Enter key in form
    $('#createGroupForm').on('keypress', function(e) {
        if (e.which === 13) {
            e.preventDefault();
            $('#submitCreateGroup').click();
        }
    });

    // Hàm hiển thị toast
    function showToast(message, type = 'info') {
        const toast = $('#groupToast');
        const toastBody = $('#toastMessage');

        // Set message
        toastBody.text(message);

        // Set icon và class dựa trên type
        const icon = toast.find('.toast-header i');
        icon.removeClass().addClass('bi me-2');

        switch(type) {
            case 'success':
                icon.addClass('bi-check-circle-fill text-success');
                break;
            case 'warning':
                icon.addClass('bi-exclamation-triangle-fill text-warning');
                break;
            case 'error':
                icon.addClass('bi-x-circle-fill text-danger');
                break;
            default:
                icon.addClass('bi-info-circle-fill text-info');
        }

        // Show toast
        const bsToast = new bootstrap.Toast(toast[0]);
        bsToast.show();
    }

    // Expose showToast function globally
    window.showToast = showToast;
}); 