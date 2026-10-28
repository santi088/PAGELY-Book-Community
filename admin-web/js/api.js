const API_BASE_URL = localStorage.getItem('apiBaseUrl') || 'http://localhost:5242';

export class ApiClient {
    constructor() {
        this.baseUrl = API_BASE_URL;
    }
    
    setBaseUrl(url) {
        this.baseUrl = url;
        localStorage.setItem('apiBaseUrl', url);
    }
    
    getAuthToken() {
        return localStorage.getItem('authToken');
    }
    
    setAuthToken(token) {
        if (token) {
            localStorage.setItem('authToken', token);
        } else {
            localStorage.removeItem('authToken');
        }
    }
    
    getAuthHeaders() {
        const headers = {
            'Content-Type': 'application/json'
        };
        const token = this.getAuthToken();
        if (token) {
            headers['Authorization'] = `Bearer ${token}`;
        }
        return headers;
    }
    
    async request(endpoint, options = {}) {
        const url = `${this.baseUrl}${endpoint}`;
        const config = {
            headers: this.getAuthHeaders(),
            ...options
        };
        
        if (config.body && typeof config.body === 'object') {
            config.body = JSON.stringify(config.body);
        }
        
        try {
            const response = await fetch(url, config);
            
            // Handle 204 No Content
            if (response.status === 204) {
                return { success: true };
            }
            
            let data;
            const contentType = response.headers.get('content-type');
            if (contentType && contentType.includes('application/json')) {
                data = await response.json();
            } else {
                data = await response.text();
            }
            
            if (!response.ok) {
                const error = data?.message || data?.title || data || 'An error occurred';
                const errorMsg = typeof error === 'string' ? error : JSON.stringify(error);
                // Don't throw for 401/403 to avoid loops, let caller handle
                const err = new Error(errorMsg);
                err.status = response.status;
                throw err;
            }
            
            return data;
        } catch (error) {
            if (error.name === 'TypeError' && error.message.includes('fetch')) {
                throw new Error('Network error. Please check if the API is running.');
            }
            throw error;
        }
    }
    
    get(endpoint) {
        return this.request(endpoint, { method: 'GET' });
    }
    
    post(endpoint, body) {
        return this.request(endpoint, { method: 'POST', body });
    }
    
    put(endpoint, body) {
        return this.request(endpoint, { method: 'PUT', body });
    }
    
    delete(endpoint) {
        return this.request(endpoint, { method: 'DELETE' });
    }
}

export const api = new ApiClient();
