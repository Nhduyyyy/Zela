document.addEventListener('DOMContentLoaded', () => {
    const currentPath = window.location.pathname.toLowerCase();

    document.querySelectorAll('.zela-nav a').forEach(link => {
        const href = link.getAttribute('href').toLowerCase();
        if (currentPath.includes(href)) {
            link.classList.add('active');
            link.style.backgroundColor = '#096B68';
            link.style.borderLeft = '4px solid #FFFBDE';
        }
    });
});
