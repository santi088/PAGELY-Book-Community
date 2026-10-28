# PAGELY Book Community Admin Frontend

Static HTML/CSS/Vanilla JS admin interface for the Book Community API.

## Prerequisites
- Backend API running (see root docs/WINDOWS_SETUP.md or README)
- Modern web browser
- Local HTTP server (do not use file://)

## Setup

### 1) Start PostgreSQL and API (Backend)
Follow [../docs/WINDOWS_SETUP.md](../docs/WINDOWS_SETUP.md) or root README.

### 2) Start Frontend Server

**Windows (PowerShell):**
```powershell
cd admin-web
python -m http.server 8080
```
Or use `npx serve . -p 8080`

**Fedora/Linux:**
```bash
cd admin-web
python3 -m http.server 8080
```

### 3) Configure API URL
Default is `http://localhost:5242`. To change:
- Open browser console and run: `localStorage.setItem('apiBaseUrl', 'http://your-api-url')`
- Or edit `js/api.js`

### 4) Access
Open http://localhost:8080 in your browser. Navigate to pages/login.html or index will redirect.

### 5) Login
Use an admin account created in the backend (via seeding or direct DB/registration + role assignment). No default admin credentials are provided.

## Pages
- Dashboard - Stats overview
- Books - List books (view)
- Genres - List/manage genres
- Users - List users, suspend/reactivate
- Reports - View and moderate reports
- Reviews - Info page (moderation via Reports)

## Notes
- All data comes from the real API
- Admin role required for protected pages
- JWT tokens stored in localStorage (development only - see security notes)
- CORS must be configured on backend for browser requests from different origins
- No fake data

## Troubleshooting
- **CORS errors**: Backend needs CORS for http://localhost:8080
- **401/403**: Check token and admin role
- **Connection errors**: Verify API is running on correct URL
