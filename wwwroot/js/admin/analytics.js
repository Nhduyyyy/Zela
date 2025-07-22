// =================================
// ZELA ADMIN - ANALYTICS DASHBOARD
// JavaScript cho trang Analytics
// =================================

// Global variables
let charts = {};
let currentTimeRange = 'month';

// Initialize analytics dashboard
document.addEventListener('DOMContentLoaded', function() {
    initializeCharts();
    loadChartData();
    setupEventListeners();
});

// Setup event listeners
function setupEventListeners() {
    // Time range selector
    const timeRangeSelect = document.getElementById('timeRange');
    if (timeRangeSelect) {
        timeRangeSelect.addEventListener('change', function() {
            currentTimeRange = this.value;
            loadChartData();
        });
    }
}

// Initialize all charts
function initializeCharts() {
    // User Growth Chart
    const userGrowthCtx = document.getElementById('userGrowthChart');
    if (userGrowthCtx) {
        charts.userGrowth = new Chart(userGrowthCtx, {
            type: 'line',
            data: {
                labels: [],
                datasets: [{
                    label: 'Người dùng mới',
                    data: [],
                    borderColor: '#92140C',
                    backgroundColor: 'rgba(146, 20, 12, 0.1)',
                    borderWidth: 3,
                    fill: true,
                    tension: 0.4
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        display: false
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        grid: {
                            color: 'rgba(0,0,0,0.1)'
                        }
                    },
                    x: {
                        grid: {
                            display: false
                        }
                    }
                }
            }
        });
    }

    // Revenue Chart
    const revenueCtx = document.getElementById('revenueChart');
    if (revenueCtx) {
        charts.revenue = new Chart(revenueCtx, {
            type: 'bar',
            data: {
                labels: [],
                datasets: [{
                    label: 'Doanh thu (VNĐ)',
                    data: [],
                    backgroundColor: 'rgba(59, 130, 246, 0.8)',
                    borderColor: '#3B82F6',
                    borderWidth: 1
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        display: false
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        grid: {
                            color: 'rgba(0,0,0,0.1)'
                        },
                        ticks: {
                            callback: function(value) {
                                return new Intl.NumberFormat('vi-VN').format(value);
                            }
                        }
                    },
                    x: {
                        grid: {
                            display: false
                        }
                    }
                }
            }
        });
    }

    // Activity Chart
    const activityCtx = document.getElementById('activityChart');
    if (activityCtx) {
        charts.activity = new Chart(activityCtx, {
            type: 'line',
            data: {
                labels: [],
                datasets: [{
                    label: 'Admin Actions',
                    data: [],
                    borderColor: '#92140C',
                    backgroundColor: 'rgba(146, 20, 12, 0.1)',
                    borderWidth: 2,
                    fill: false
                }, {
                    label: 'User Actions',
                    data: [],
                    borderColor: '#10B981',
                    backgroundColor: 'rgba(16, 185, 129, 0.1)',
                    borderWidth: 2,
                    fill: false
                }, {
                    label: 'Errors',
                    data: [],
                    borderColor: '#EF4444',
                    backgroundColor: 'rgba(239, 68, 68, 0.1)',
                    borderWidth: 2,
                    fill: false
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        position: 'top',
                        labels: {
                            usePointStyle: true,
                            padding: 20
                        }
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        grid: {
                            color: 'rgba(0,0,0,0.1)'
                        }
                    },
                    x: {
                        grid: {
                            display: false
                        }
                    }
                }
            }
        });
    }

    // Content Distribution Chart
    const contentCtx = document.getElementById('contentChart');
    if (contentCtx) {
        charts.content = new Chart(contentCtx, {
            type: 'doughnut',
            data: {
                labels: ['Tin nhắn', 'Cuộc họp', 'Tệp tin', 'Bài kiểm tra', 'Nhóm chat'],
                datasets: [{
                    data: [0, 0, 0, 0, 0],
                    backgroundColor: [
                        '#92140C',
                        '#3B82F6',
                        '#10B981',
                        '#F59E0B',
                        '#8B5CF6'
                    ],
                    borderWidth: 2,
                    borderColor: '#fff'
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        position: 'bottom',
                        labels: {
                            usePointStyle: true,
                            padding: 20
                        }
                    }
                }
            }
        });
    }
}

// Load chart data from server
function loadChartData() {
    // Load user growth data
    loadUserGrowthData();
    
    // Load revenue data
    loadRevenueData();
    
    // Load activity data
    loadActivityData();
    
    // Load content distribution data
    loadContentDistributionData();
}

// Load user growth data
function loadUserGrowthData() {
    fetch(`/Admin/GetDashboardChartData?range=${currentTimeRange}`)
        .then(response => response.json())
        .then(data => {
            if (charts.userGrowth) {
                charts.userGrowth.data.labels = data.labels;
                charts.userGrowth.data.datasets[0].data = data.userSeries;
                charts.userGrowth.update();
            }
        })
        .catch(error => {
            console.error('Error loading user growth data:', error);
        });
}

// Load revenue data
function loadRevenueData() {
    fetch(`/Admin/GetRevenueStatsChart?range=${currentTimeRange}`)
        .then(response => response.json())
        .then(data => {
            if (charts.revenue) {
                charts.revenue.data.labels = data.labels;
                charts.revenue.data.datasets[0].data = data.revenueSeries;
                charts.revenue.update();
            }
        })
        .catch(error => {
            console.error('Error loading revenue data:', error);
        });
}

// Load activity data
function loadActivityData() {
    fetch(`/Admin/GetSystemLogsChart?range=${currentTimeRange}`)
        .then(response => response.json())
        .then(data => {
            if (charts.activity) {
                charts.activity.data.labels = data.labels;
                charts.activity.data.datasets[0].data = data.adminSeries;
                charts.activity.data.datasets[1].data = data.userSeries;
                charts.activity.data.datasets[2].data = data.errorSeries;
                charts.activity.update();
            }
        })
        .catch(error => {
            console.error('Error loading activity data:', error);
        });
}

// Load content distribution data
function loadContentDistributionData() {
    // Get data from page elements
    const messages = parseInt(document.querySelector('.metric-card:nth-child(6) .metric-number').textContent.replace(/,/g, '')) || 0;
    const meetings = parseInt(document.querySelector('.metric-card:nth-child(7) .metric-number').textContent.replace(/,/g, '')) || 0;
    const files = parseInt(document.querySelector('.metric-card:nth-child(8) .metric-number').textContent.replace(/,/g, '')) || 0;
    const quizzes = parseInt(document.querySelector('.metric-card:nth-child(9) .metric-number').textContent.replace(/,/g, '')) || 0;
    const groups = parseInt(document.querySelector('.metric-card:nth-child(6) .metric-change span').textContent.split(' ')[0]) || 0;

    if (charts.content) {
        charts.content.data.datasets[0].data = [messages, meetings, files, quizzes, groups];
        charts.content.update();
    }
}

// Update chart function (for button clicks)
function updateChart(chartType, range) {
    // Update button states
    const buttons = document.querySelectorAll(`[onclick*="${chartType}"]`);
    buttons.forEach(btn => btn.classList.remove('active'));
    event.target.classList.add('active');

    // Update time range and reload data
    currentTimeRange = range;
    
    switch (chartType) {
        case 'userGrowth':
            loadUserGrowthData();
            break;
        case 'revenue':
            loadRevenueData();
            break;
        case 'activity':
            loadActivityData();
            break;
    }
}

// Auto-refresh data every 5 minutes
setInterval(() => {
    loadChartData();
}, 5 * 60 * 1000);

// Export analytics data
function exportAnalyticsData() {
    const data = {
        timestamp: new Date().toISOString(),
        timeRange: currentTimeRange,
        metrics: {
            totalUsers: document.querySelector('.metric-card:nth-child(1) .metric-number').textContent,
            activeUsers: document.querySelector('.metric-card:nth-child(2) .metric-number').textContent,
            premiumUsers: document.querySelector('.metric-card:nth-child(3) .metric-number').textContent,
            totalRevenue: document.querySelector('.metric-card:nth-child(4) .metric-number').textContent,
            successfulTransactions: document.querySelector('.metric-card:nth-child(5) .metric-number').textContent,
            totalMessages: document.querySelector('.metric-card:nth-child(6) .metric-number').textContent,
            totalMeetings: document.querySelector('.metric-card:nth-child(7) .metric-number').textContent,
            totalFiles: document.querySelector('.metric-card:nth-child(8) .metric-number').textContent,
            totalQuizzes: document.querySelector('.metric-card:nth-child(9) .metric-number').textContent
        }
    };

    const blob = new Blob([JSON.stringify(data, null, 2)], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `analytics-${new Date().toISOString().split('T')[0]}.json`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
}

// Print analytics report
function printAnalyticsReport() {
    window.print();
}

// Add smooth animations to metric cards
function animateMetrics() {
    const metricCards = document.querySelectorAll('.metric-card');
    metricCards.forEach((card, index) => {
        setTimeout(() => {
            card.style.opacity = '0';
            card.style.transform = 'translateY(20px)';
            card.style.transition = 'all 0.5s ease';
            
            setTimeout(() => {
                card.style.opacity = '1';
                card.style.transform = 'translateY(0)';
            }, 100);
        }, index * 100);
    });
}

// Initialize animations when page loads
document.addEventListener('DOMContentLoaded', function() {
    setTimeout(animateMetrics, 500);
}); 