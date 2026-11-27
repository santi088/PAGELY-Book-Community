import { api } from './api.js';

export class AuthService {
    constructor() {
        this.currentUser = null;
    }
    
    async login(email, password) {
        try {
            const response = await api.post('/api/auth/login', { email, password });
            api.setAuthToken(response.token);
            this.currentUser = {
                email: response.Email || response.email,
                userId: response.UserId || response.userId,
                roles: response.Roles || response.roles || []
            };
            // Fetch user details with roles (non-fatal if endpoint missing)
            try {
                await this.loadCurrentUser();
            } catch (e) {
                console.warn('loadCurrentUser failed, using login response data:', e.message);
            }
            return response;
        } catch (error) {
            throw error;
        }
    }
    
    async loadCurrentUser() {
        try {
            const user = await api.get('/api/auth/me');
            this.currentUser = {
                email: user.email || user.Email,
                userId: user.id || user.userId || user.Id,
                roles: user.roles || user.Roles || []
            };
            return this.currentUser;
        } catch (error) {
            console.warn('Failed to load current user:', error.message);
            this.currentUser = null;
            throw error;
        }
    }
    
    logout() {
        api.setAuthToken(null);
        this.currentUser = null;
        window.location.href = '/admin-web/pages/login.html';
    }
    
    isAuthenticated() {
        return !!api.getAuthToken();
    }
    
    isAdmin() {
        if (!this.currentUser) return false;
        const roles = this.currentUser.roles || this.currentUser.Roles || [];
        return Array.isArray(roles) && roles.includes('Admin');
    }
    
    requireAuth() {
        if (!this.isAuthenticated()) {
            window.location.href = '/admin-web/pages/login.html';
            return false;
        }
        return true;
    }
    
    async requireAdmin() {
        if (!this.requireAuth()) return false;
        // Try to load current user if not already loaded
        if (!this.currentUser) {
            try {
                await this.loadCurrentUser();
            } catch (e) {
                console.error('Failed to load current user:', e);
                return false;
            }
        }
        if (!this.isAdmin()) {
            console.error('Admin access required - user roles:', this.currentUser?.roles);
            if (!window.location.pathname.includes('dashboard.html')) {
                window.location.href = 'dashboard.html?error=admin_required';
            }
            return false;
        }
        return true;
    }
}

export const auth = new AuthService();
