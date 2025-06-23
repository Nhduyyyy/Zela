// Add Member Functionality
$(document).ready(function() {
    console.log('=== Add Member Script Loading ===');
    
    let selectedUsers = [];
    let currentGroupId = null;
    let addMemberModal = null;

    // Check if modal element exists
    const modalElement = document.getElementById('addMemberModal');
    console.log('Modal element found:', modalElement);
    
    // Check if Bootstrap is available
    console.log('Bootstrap Modal available:', typeof bootstrap !== 'undefined' && bootstrap.Modal);
    
    // Check if jQuery is available
    console.log('jQuery available:', typeof $ !== 'undefined');

    // Initialize add member modal
    function initializeModal() {
        const modalElement = document.getElementById('addMemberModal');
        if (modalElement && !addMemberModal) {
            try {
                addMemberModal = new bootstrap.Modal(modalElement);
                console.log('Add member modal initialized successfully');
            } catch (error) {
                console.error('Error initializing modal:', error);
            }
        } else if (!modalElement) {
            console.error('Modal element not found!');
        } else {
            console.log('Modal already initialized');
        }
    }

    // Initialize modal on page load
    initializeModal();

    // Handle add member button click - use event delegation for dynamically loaded content
    $(document).on('click', '#btnAddMember', function(e) {
        e.preventDefault();
        console.log('=== Add member button clicked ===');
        
        // Re-initialize modal if needed
        if (!addMemberModal) {
            console.log('Re-initializing modal...');
            initializeModal();
        }

        // Get current group ID from the sidebar
        const sidebarElement = document.querySelector('.chat-info-panel');
        console.log('Sidebar element found:', sidebarElement);
        
        if (sidebarElement) {
            currentGroupId = sidebarElement.dataset.groupId;
            console.log('Current group ID:', currentGroupId);
        }
        
        if (!currentGroupId) {
            console.error('No group ID found');
            showToast('Không thể xác định nhóm hiện tại', 'error');
            return;
        }

        // Reset modal state
        resetModal();
        
        // Show modal
        if (addMemberModal) {
            console.log('Showing modal...');
            addMemberModal.show();
            console.log('Modal shown successfully');
        } else {
            console.error('Modal not initialized - cannot show');
        }
    });

    // Handle search button click
    $(document).on('click', '#searchUserBtn', function() {
        console.log('Search button clicked');
        performSearch();
    });

    // Handle Enter key in search input
    $(document).on('keypress', '#searchUserInput', function(e) {
        if (e.which === 13) {
            e.preventDefault();
            performSearch();
        }
    });

    // Handle submit add members
    $(document).on('click', '#submitAddMembers', function() {
        console.log('Submit button clicked');
        if (selectedUsers.length === 0) {
            showToast('Vui lòng chọn ít nhất một người dùng', 'warning');
            return;
        }

        addMembersToGroup();
    });

    // Handle user selection
    $(document).on('click', '.user-item .btn-select', function() {
        const userId = $(this).data('user-id');
        const userName = $(this).data('user-name');
        const userAvatar = $(this).data('user-avatar');

        // Check if user is already selected
        const existingIndex = selectedUsers.findIndex(u => u.userId === userId);
        
        if (existingIndex === -1) {
            // Add user to selected list
            selectedUsers.push({
                userId: userId,
                userName: userName,
                userAvatar: userAvatar
            });
            
            // Update UI
            updateSelectedUsersList();
            updateSubmitButton();
            
            // Change button to "Đã chọn"
            $(this).removeClass('btn-outline-primary').addClass('btn-success')
                   .html('<i class="bi bi-check me-1"></i>Đã chọn')
                   .prop('disabled', true);
        }
    });

    // Handle remove selected user
    $(document).on('click', '.selected-user-item .btn-remove', function() {
        const userId = $(this).data('user-id');
        
        // Remove from selected users
        selectedUsers = selectedUsers.filter(u => u.userId !== userId);
        
        // Update UI
        updateSelectedUsersList();
        updateSubmitButton();
        
        // Reset button in search results
        $(`.user-item .btn-select[data-user-id="${userId}"]`)
            .removeClass('btn-success').addClass('btn-outline-primary')
            .html('<i class="bi bi-plus me-1"></i>Chọn')
            .prop('disabled', false);
    });

    // Perform search
    function performSearch() {
        const searchTerm = $('#searchUserInput').val().trim();
        
        if (!searchTerm) {
            showToast('Vui lòng nhập từ khóa tìm kiếm', 'warning');
            return;
        }

        if (searchTerm.length < 2) {
            showToast('Từ khóa tìm kiếm phải có ít nhất 2 ký tự', 'warning');
            return;
        }

        // Show loading
        showLoading(true);
        hideResults();

        // Call API
        $.ajax({
            url: '/GroupChat/SearchUsers',
            type: 'GET',
            data: {
                searchTerm: searchTerm,
                groupId: currentGroupId
            },
            success: function(users) {
                showLoading(false);
                
                if (users && users.length > 0) {
                    displaySearchResults(users);
                } else {
                    showNoResults();
                }
            },
            error: function(xhr) {
                showLoading(false);
                let errorMessage = 'Có lỗi xảy ra khi tìm kiếm';
                if (xhr.responseJSON && xhr.responseJSON.message) {
                    errorMessage = xhr.responseJSON.message;
                }
                showToast(errorMessage, 'error');
            }
        });
    }

    // Display search results
    function displaySearchResults(users) {
        const userList = $('#userList');
        userList.empty();

        users.forEach(user => {
            const isSelected = selectedUsers.some(u => u.userId === user.userId);
            const userHtml = `
                <div class="list-group-item user-item d-flex align-items-center">
                    <img src="${user.avatarUrl || '/images/default-avatar.jpeg'}" 
                         class="rounded-circle me-3" width="40" height="40" 
                         alt="Ảnh đại diện ${user.fullName}">
                    <div class="flex-fill">
                        <h6 class="mb-0">${user.fullName}</h6>
                        <small class="text-muted">${user.email}</small>
                    </div>
                    <button class="btn ${isSelected ? 'btn-success' : 'btn-outline-primary'} btn-sm btn-select"
                            data-user-id="${user.userId}"
                            data-user-name="${user.fullName}"
                            data-user-avatar="${user.avatarUrl || '/images/default-avatar.jpeg'}"
                            ${isSelected ? 'disabled' : ''}>
                        <i class="bi ${isSelected ? 'bi-check' : 'bi-plus'} me-1"></i>
                        ${isSelected ? 'Đã chọn' : 'Chọn'}
                    </button>
                </div>
            `;
            userList.append(userHtml);
        });

        $('#searchResults').show();
    }

    // Update selected users list
    function updateSelectedUsersList() {
        const selectedList = $('#selectedUserList');
        selectedList.empty();

        if (selectedUsers.length === 0) {
            $('#selectedUsers').hide();
            return;
        }

        selectedUsers.forEach(user => {
            const userHtml = `
                <div class="list-group-item selected-user-item d-flex align-items-center">
                    <img src="${user.userAvatar}" 
                         class="rounded-circle me-3" width="40" height="40" 
                         alt="Ảnh đại diện ${user.userName}">
                    <div class="flex-fill">
                        <h6 class="mb-0">${user.userName}</h6>
                    </div>
                    <button class="btn btn-outline-danger btn-sm btn-remove"
                            data-user-id="${user.userId}">
                        <i class="bi bi-x me-1"></i>Bỏ chọn
                    </button>
                </div>
            `;
            selectedList.append(userHtml);
        });

        $('#selectedUsers').show();
    }

    // Update submit button state
    function updateSubmitButton() {
        const submitBtn = $('#submitAddMembers');
        if (selectedUsers.length > 0) {
            submitBtn.prop('disabled', false)
                    .html(`<i class="bi bi-person-plus me-1"></i>Thêm ${selectedUsers.length} thành viên`);
        } else {
            submitBtn.prop('disabled', true)
                    .html('<i class="bi bi-person-plus me-1"></i>Thêm thành viên');
        }
    }

    // Add members to group
    function addMembersToGroup() {
        const submitBtn = $('#submitAddMembers');
        const originalText = submitBtn.html();
        
        submitBtn.prop('disabled', true)
                .html('<i class="bi bi-hourglass-split me-1"></i>Đang thêm...');

        // Add each user to group
        const promises = selectedUsers.map(user => {
            return $.ajax({
                url: '/GroupChat/AddMember',
                type: 'POST',
                data: {
                    groupId: currentGroupId,
                    userId: user.userId
                }
            });
        });

        Promise.all(promises)
            .then(() => {
                showToast(`Đã thêm ${selectedUsers.length} thành viên vào nhóm thành công!`, 'success');
                if (addMemberModal) {
                    addMemberModal.hide();
                }
                
                // Reload group sidebar to show new members
                if (typeof loadGroupSidebar === 'function') {
                    loadGroupSidebar(currentGroupId);
                }
                
                // Reload page after a short delay to update member count
                setTimeout(() => {
                    window.location.reload();
                }, 1500);
            })
            .catch((error) => {
                let errorMessage = 'Có lỗi xảy ra khi thêm thành viên';
                if (error.responseJSON && error.responseJSON.message) {
                    errorMessage = error.responseJSON.message;
                }
                showToast(errorMessage, 'error');
            })
            .finally(() => {
                submitBtn.prop('disabled', false).html(originalText);
            });
    }

    // Reset modal state
    function resetModal() {
        selectedUsers = [];
        $('#searchUserInput').val('');
        $('#searchResults').hide();
        $('#selectedUsers').hide();
        $('#noResults').hide();
        $('#searchLoading').hide();
        updateSubmitButton();
    }

    // Show loading
    function showLoading(show) {
        if (show) {
            $('#searchLoading').show();
        } else {
            $('#searchLoading').hide();
        }
    }

    // Hide results
    function hideResults() {
        $('#searchResults').hide();
        $('#noResults').hide();
    }

    // Show no results
    function showNoResults() {
        $('#noResults').show();
    }

    // Reset modal when closed
    $(document).on('hidden.bs.modal', '#addMemberModal', function() {
        resetModal();
    });

    // Toast function (reuse from create-group.js if available)
    function showToast(message, type = 'info') {
        if (typeof window.showToast === 'function') {
            window.showToast(message, type);
        } else {
            // Fallback toast implementation
            const toastClass = type === 'success' ? 'alert-success' : 
                             type === 'warning' ? 'alert-warning' : 
                             type === 'error' ? 'alert-danger' : 'alert-info';
            
            const toastHtml = `
                <div class="alert ${toastClass} alert-dismissible fade show position-fixed" 
                     style="top: 20px; right: 20px; z-index: 9999;" role="alert">
                    ${message}
                    <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
                </div>
            `;
            
            $('body').append(toastHtml);
            
            // Auto remove after 5 seconds
            setTimeout(() => {
                $('.alert').alert('close');
            }, 5000);
        }
    }

    // Debug: Log when script is loaded
    console.log('=== Add member script loaded successfully ===');
    
    // Test button click after a delay
    setTimeout(() => {
        const testButton = document.getElementById('btnAddMember');
        console.log('Test button found after delay:', testButton);
        if (testButton) {
            console.log('Button text:', testButton.textContent);
            console.log('Button HTML:', testButton.outerHTML);
        }
    }, 2000);
}); 