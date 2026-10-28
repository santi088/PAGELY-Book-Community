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
                email: response.email,
                userId: response.userId,
                roles: []
            };
            // Fetch user details with roles
            await this.loadCurrentUser();
            return response;
        } catch (error) {
            throw error;
        }
    }
    
    async loadCurrentUser() {
        try {
            const user = await api.get('/api/me');
            this.currentUser = user;
            return user;
        } catch (error) {
            this.currentUser = null;
            api.setAuthToken(null);
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
    
    requireAdmin() {
        if (!this.requireAuth()) return false;
        if (!this.isAdmin()) {
            // Don't spam alerts, show error in page instead
            console.error('Admin access required - user roles:', this.currentUser?.roles);
            // Redirect to dashboard with error
            if (!window.location.pathname.includes('dashboard.html')) {
                window.location.href = 'dashboard.html?error=admin_required';
            }
            return false;
        }
        return true;
    }
}

export const auth = new AuthService();
