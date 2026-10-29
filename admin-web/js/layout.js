import { auth } from './auth.js';

export function renderLayout(pageTitle, pageDescription = '') {
    const userInfo = auth.currentUser
        ? `<div class="user-info">${auth.currentUser.email}</div>`
        : '<div class="user-info">Not logged in</div>';

    return `
        <div class="app-shell">
            <aside class="sidebar" aria-label="Primary navigation">
                <div class="sidebar-header">
                    <h2>Book Community Admin</h2>
                </div>
                <nav class="sidebar-nav">
                    <a href="dashboard.html" class="nav-item" data-page="dashboard">Dashboard</a>
                    <a href="books.html" class="nav-item" data-page="books">Books</a>
                    <a href="genres.html" class="nav-item" data-page="genres">Genres</a>
                    <a href="users.html" class="nav-item" data-page="users">Users</a>
                    <a href="reviews.html" class="nav-item" data-page="reviews">Reviews</a>
                    <a href="reports.html" class="nav-item" data-page="reports">Reports</a>
                </nav>
                <div class="sidebar-footer">
                    ${userInfo}
                    <button class="btn btn-ghost btn-sm btn-logout" id="btnLogout" type="button">Log out</button>
                </div>
            </aside>
            <main class="main-content">
                <header class="page-header">
                    <div>
                        <h1>${pageTitle}</h1>
                        ${pageDescription ? `<p>${pageDescription}</p>` : ''}
                    </div>
                </header>
                <div id="pageContent"></div>
            </main>
        </div>
    `;
}

export function initLayout() {
    const currentPage = window.location.pathname.split('/').pop().replace('.html', '');
    document.querySelectorAll('.nav-item').forEach(item => {
        if (item.dataset.page === currentPage) {
            item.classList.add('active');
            item.setAttribute('aria-current', 'page');
        }
    });

    const btnLogout = document.getElementById('btnLogout');
    if (btnLogout) {
        btnLogout.addEventListener('click', () => {
            auth.logout();
        });
    }
}
