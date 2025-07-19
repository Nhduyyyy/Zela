// Group Manage Modal JavaScript
document.addEventListener('DOMContentLoaded', function() {
    const editGroupForm = document.getElementById('editGroupForm');
    
    if (editGroupForm) {
        editGroupForm.addEventListener('submit', function(e) {
            // Show loading state
            const submitBtn = this.querySelector('button[type="submit"]');
            const originalText = submitBtn.innerHTML;
            submitBtn.innerHTML = '<i class="bi bi-hourglass-split me-2"></i>Đang xử lý...';
            submitBtn.disabled = true;
            
            // Let the form submit normally (redirect will happen)
            // The loading state will be cleared when page reloads
        });
    }
    
    // Handle tab switching
    const tabButtons = document.querySelectorAll('[data-bs-toggle="pill"]');
    tabButtons.forEach(button => {
        button.addEventListener('click', function() {
            // Remove active class from all tabs
            tabButtons.forEach(btn => btn.classList.remove('active'));
            // Add active class to clicked tab
            this.classList.add('active');
        });
    });
    
    // Show TempData messages if they exist
    function showTempDataMessages() {
        // Check for success message
        const successMessage = document.querySelector('[data-tempdata-success]');
        if (successMessage) {
            const message = successMessage.getAttribute('data-tempdata-success');
            if (message) {
                showAlert('success', message);
                // Clear the attribute
                successMessage.removeAttribute('data-tempdata-success');
            }
        }
        
        // Check for error message
        const errorMessage = document.querySelector('[data-tempdata-error]');
        if (errorMessage) {
            const message = errorMessage.getAttribute('data-tempdata-error');
            if (message) {
                showAlert('danger', message);
                // Clear the attribute
                errorMessage.removeAttribute('data-tempdata-error');
            }
        }
    }
    
    // Function to show alerts
    function showAlert(type, message) {
        // Remove existing alerts
        const existingAlerts = document.querySelectorAll('.alert');
        existingAlerts.forEach(alert => alert.remove());
        
        // Create new alert
        const alertDiv = document.createElement('div');
        alertDiv.className = `alert alert-${type} alert-dismissible fade show`;
        alertDiv.innerHTML = `
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        `;
        
        // Insert alert at the top of the modal body
        const modalBody = document.querySelector('#groupManageModal .modal-body');
        if (modalBody) {
            modalBody.insertBefore(alertDiv, modalBody.firstChild);
        }
    }
    
    // Initialize messages on page load
    showTempDataMessages();
}); 