function openProfileDialog() {
    console.log('openProfileDialog called');

    // Kiểm tra xem jQuery đã được load chưa
    if (typeof $ === 'undefined') {
        console.error('jQuery is not loaded');
        return;
    }

    // Kiểm tra xem Bootstrap đã được load chưa
    if (typeof $.fn.modal === 'undefined') {
        console.error('Bootstrap is not loaded');
        return;
    }

    // Xóa alert messages cũ
    $('.modal-body .alert').remove();
    $('#loadingOverlay').remove();

    // Hiển thị dialog
    $('#profileDialog').modal('show');

    // Lấy thông tin profile từ server
    $.ajax({
        url: '/Profile/GetProfileData',
        type: 'GET',
        dataType: 'json',
        success: function(response) {
            console.log('Profile data received:', response);

            if (response.success && response.data) {
                // Cập nhật form với dữ liệu mới
                updateProfileForm(response.data);
            } else {
                console.error('Failed to get profile data:', response.message);
                $('.modal-body').prepend('<div class="alert alert-danger">Không thể tải thông tin profile: ' + (response.message || 'Lỗi không xác định') + '</div>');
            }
        },
        error: function(xhr, status, error) {
            console.error('Error fetching profile:', {
                status: status,
                error: error,
                response: xhr.responseText
            });
            $('.modal-body').prepend('<div class="alert alert-danger">Không thể tải thông tin profile. Vui lòng thử lại sau.</div>');
        }
    });
}

function clearProfileAlerts() {
    $('.modal-body .alert').remove();
}

// Thêm event handler cho khi modal được đóng
$(document).ready(function() {
    $('#profileDialog').on('hidden.bs.modal', function () {
        clearProfileAlerts();
    });
});

function updateProfileForm(profileData) {
    // Cập nhật các trường input
    $('#profileForm input[name="UserId"]').val(profileData.userId);
    $('#profileForm input[name="Email"]').val(profileData.email);
    $('#profileForm input[name="FullName"]').val(profileData.fullName);
    $('#profileForm input[name="AvatarUrl"]').val(profileData.avatarUrl);

    // Cập nhật ảnh preview
    if (profileData.avatarUrl) {
        $('#avatarPreview').attr('src', profileData.avatarUrl);
    } else {
        $('#avatarPreview').attr('src', '/images/default-avatar.jpeg');
    }

    // Cập nhật các trường chỉ đọc
    var accountStatus = profileData.isPremium ? "Premium" : "Thường";
    $('#profileForm .form-control-plaintext').eq(0).text(accountStatus);

    // Format dates
    var createdAt = new Date(profileData.createdAt).toLocaleString('vi-VN');
    var lastLoginAt = new Date(profileData.lastLoginAt).toLocaleString('vi-VN');

    $('#profileForm .form-control-plaintext').eq(1).text(createdAt);
    $('#profileForm .form-control-plaintext').eq(2).text(lastLoginAt);
}

function saveProfile() {
    console.log('saveProfile called');

    // Xóa alert messages cũ
    $('.modal-body .alert').remove();

    // Validate form
    var form = $('#profileForm')[0];
    if (!form.checkValidity()) {
        form.reportValidity();
        return;
    }

    // Disable nút và hiển thị loading
    var saveButton = $('.btn-primary');
    var originalText = saveButton.text();
    saveButton.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>Đang lưu...');

    // Lấy dữ liệu từ form
    var formData = new FormData($('#profileForm')[0]);

    // Thêm file ảnh nếu có
    var avatarFile = $('#avatarFile')[0].files[0];
    if (avatarFile) {
        formData.append('AvatarFile', avatarFile);
    }

    console.log('Form data being sent...');

    // Gửi request cập nhật
    $.ajax({
        url: '/Profile/UpdateProfile',
        type: 'POST',
        data: formData,
        processData: false,
        contentType: false,
        success: function(response) {
            console.log('Update response:', response);
            // Khôi phục nút
            saveButton.prop('disabled', false).text(originalText);

            if (response.success) {
                // Hiển thị thông báo thành công
                $('.modal-body').prepend('<div class="alert alert-success alert-dismissible fade show" role="alert"><i class="bi bi-check-circle me-2"></i>' + response.message + '<button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button></div>');

                // Tự động đóng dialog sau 2 giây
                setTimeout(function() {
                    $('#profileDialog').modal('hide');
                    // Cập nhật thông tin hiển thị trên sidebar
                    location.reload();
                }, 2000);
            } else {
                // Hiển thị thông báo lỗi
                $('.modal-body').prepend('<div class="alert alert-danger alert-dismissible fade show" role="alert"><i class="bi bi-exclamation-triangle me-2"></i>' + response.message + '<button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button></div>');
            }
        },
        error: function(xhr, status, error) {
            console.error('Error updating profile:', {
                status: status,
                error: error,
                response: xhr.responseText
            });
            // Khôi phục nút
            saveButton.prop('disabled', false).text(originalText);
            $('.modal-body').prepend('<div class="alert alert-danger alert-dismissible fade show" role="alert"><i class="bi bi-exclamation-triangle me-2"></i>Có lỗi xảy ra khi cập nhật thông tin<button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button></div>');
        }
    });
}

// Wait for DOM to be ready before adding event listeners
document.addEventListener('DOMContentLoaded', function() {
    const avatarFileInput = document.getElementById('avatarFile');
    if (avatarFileInput) {
        avatarFileInput.addEventListener('change', function(e) {
            const file = e.target.files[0];
            if (file) {
                // Validate file type
                if (!file.type.startsWith('image/')) {
                    alert('Vui lòng chọn file ảnh hợp lệ');
                    return;
                }

                // Validate file size (max 5MB)
                if (file.size > 5 * 1024 * 1024) {
                    alert('Kích thước ảnh không được vượt quá 5MB');
                    return;
                }

                // Preview image
                const reader = new FileReader();
                reader.onload = function(e) {
                    const avatarPreview = document.getElementById('avatarPreview');
                    const avatarUrl = document.getElementById('AvatarUrl');
                    if (avatarPreview) avatarPreview.src = e.target.result;
                    if (avatarUrl) avatarUrl.value = e.target.result;
                };
                reader.readAsDataURL(file);
            }
        });
    }
}); 