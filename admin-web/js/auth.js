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
        return this.currentUser.roles && this.currentUser.roles.includes('Admin');
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
            alert('Admin access required');
            window.location.href = '/admin-web/pages/dashboard.html';
            return false;
        }
        return true;
    }
}

export const auth = new AuthService();
