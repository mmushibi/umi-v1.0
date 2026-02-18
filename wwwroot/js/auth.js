/**
 * Umi Health POS - Authentication Module
 * Handles authentication, session management, and role-based access control
 */

class UmiAuth {
    constructor() {
        this.apiBase = this.getApiBase();
        this.isProduction = this.isProductionMode();
        this.sessionKey = 'umihealth_user_session';
        this.tokenKey = 'umihealth_auth_token';
        this.refreshTokenKey = 'umihealth_refresh_token';
    }

    getApiBase() {
        // In production, use the same origin or configured API base
        if (window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1') {
            return window.location.origin;
        }
        return window.location.origin;
    }

    isProductionMode() {
        return !['localhost', '127.0.0.1'].includes(window.location.hostname) && 
               window.location.protocol !== 'file:';
    }

    // Get current authentication token
    getToken() {
        return localStorage.getItem(this.tokenKey) || sessionStorage.getItem(this.tokenKey);
    }

    // Get refresh token
    getRefreshToken() {
        return localStorage.getItem(this.refreshTokenKey) || sessionStorage.getItem(this.refreshTokenKey);
    }

    // Check if user is authenticated
    isAuthenticated() {
        const token = this.getToken();
        const session = this.getSession();
        
        if (!token || !session) {
            return false;
        }

        // Check if token is expired (simple check)
        try {
            const payload = JSON.parse(atob(token.split('.')[1]));
            const now = Date.now() / 1000;
            return payload.exp > now;
        } catch (e) {
            return false;
        }
    }

    // Get user session
    getSession() {
        const sessionStr = localStorage.getItem(this.sessionKey) || 
                          sessionStorage.getItem(this.sessionKey);
        return sessionStr ? JSON.parse(sessionStr) : null;
    }

    // Get user role
    getUserRole() {
        const session = this.getSession();
        return session?.role || sessionStorage.getItem('userRole');
    }

    // Get user hierarchy level
    getUserRoleLevel() {
        const role = this.getUserRole();
        const roleHierarchy = {
            'SuperAdmin': 5,
            'Operations': 4,
            'TenantAdmin': 3,
            'Pharmacist': 2,
            'Cashier': 1
        };
        return roleHierarchy[role] || 0;
    }

    // Check if user can manage specified role
    canManageRole(targetRole) {
        const userLevel = this.getUserRoleLevel();
        const targetLevel = this.getRoleLevel(targetRole);
        return userLevel > targetLevel;
    }

    // Get role level for specific role
    getRoleLevel(role) {
        const roleHierarchy = {
            'SuperAdmin': 5,
            'Operations': 4,
            'TenantAdmin': 3,
            'Pharmacist': 2,
            'Cashier': 1
        };
        return roleHierarchy[role] || 0;
    }

    // Check if user is being impersonated
    isImpersonated() {
        const session = this.getSession();
        return session?.isImpersonated === true || sessionStorage.getItem('isImpersonated') === 'true';
    }

    // Get original user info (for impersonation)
    getOriginalUserInfo() {
        const session = this.getSession();
        return session?.originalUser || null;
    }

    // Get user permissions
    getUserPermissions() {
        const session = this.getSession();
        const permissions = session?.permissions || 
                          JSON.parse(sessionStorage.getItem('userPermissions') || '[]');
        return Array.isArray(permissions) ? permissions : [];
    }

    // Check if user has specific permission
    hasPermission(permission) {
        const permissions = this.getUserPermissions();
        return permissions.includes(permission);
    }

    // Check if user has any of the specified permissions
    hasAnyPermission(permissions) {
        return permissions.some(permission => this.hasPermission(permission));
    }

    // Check if user has all specified permissions
    hasAllPermissions(permissions) {
        return permissions.every(permission => this.hasPermission(permission));
    }

    // Refresh access token
    async refreshToken() {
        const refreshToken = this.getRefreshToken();
        if (!refreshToken) {
            throw new Error('No refresh token available');
        }

        try {
            const response = await fetch(`${this.apiBase}/api/auth/refresh-token`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({ refreshToken })
            });

            if (!response.ok) {
                throw new Error('Token refresh failed');
            }

            const data = await response.json();
            
            // Store new tokens
            this.storeTokens(data.accessToken, data.refreshToken);
            
            return data.accessToken;
        } catch (error) {
            console.error('Token refresh error:', error);
            this.logout();
            throw error;
        }
    }

    // Store tokens in appropriate storage
    storeTokens(accessToken, refreshToken, remember = true) {
        if (remember) {
            localStorage.setItem(this.tokenKey, accessToken);
            if (refreshToken) {
                localStorage.setItem(this.refreshTokenKey, refreshToken);
            }
        } else {
            sessionStorage.setItem(this.tokenKey, accessToken);
            if (refreshToken) {
                sessionStorage.setItem(this.refreshTokenKey, refreshToken);
            }
        }
    }

    // Make authenticated API request
    async apiRequest(url, options = {}) {
        const token = this.getToken();
        
        if (!token) {
            throw new Error('No authentication token available');
        }

        const defaultOptions = {
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`
            }
        };

        const mergedOptions = {
            ...defaultOptions,
            ...options,
            headers: {
                ...defaultOptions.headers,
                ...options.headers
            }
        };

        try {
            let response = await fetch(url, mergedOptions);

            // If token is expired, try to refresh and retry once
            if (response.status === 401) {
                try {
                    const newToken = await this.refreshToken();
                    mergedOptions.headers.Authorization = `Bearer ${newToken}`;
                    response = await fetch(url, mergedOptions);
                } catch (refreshError) {
                    // Refresh failed, redirect to login
                    this.redirectToLogin();
                    throw new Error('Session expired. Please login again.');
                }
            }

            if (!response.ok) {
                throw new Error(`API request failed: ${response.status} ${response.statusText}`);
            }

            return response;
        } catch (error) {
            console.error('API request error:', error);
            throw error;
        }
    }

    // Logout user
    async logout() {
        try {
            const refreshToken = this.getRefreshToken();
            if (refreshToken) {
                await fetch(`${this.apiBase}/api/auth/logout`, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'Authorization': `Bearer ${this.getToken()}`
                    },
                    body: JSON.stringify({ refreshToken })
                });
            }
        } catch (error) {
            console.error('Logout API error:', error);
        } finally {
            // Clear local storage regardless of API call success
            this.clearSession();
            this.redirectToLogin();
        }
    }

    // Clear session data
    clearSession() {
        // Clear localStorage
        localStorage.removeItem(this.sessionKey);
        localStorage.removeItem(this.tokenKey);
        localStorage.removeItem(this.refreshTokenKey);
        localStorage.removeItem('umihealthTenantId');
        localStorage.removeItem('umihealthUserId');
        localStorage.removeItem('umihealthEmail');
        localStorage.removeItem('umihealthUserName');
        localStorage.removeItem('umihealthTenantName');
        localStorage.removeItem('umihealthUserRole');
        localStorage.removeItem('umihealthPlan');

        // Clear sessionStorage
        sessionStorage.removeItem(this.sessionKey);
        sessionStorage.removeItem(this.tokenKey);
        sessionStorage.removeItem(this.refreshTokenKey);
        sessionStorage.removeItem('userRole');
        sessionStorage.removeItem('userPermissions');
        sessionStorage.removeItem('userEmail');
        sessionStorage.removeItem('userName');
        sessionStorage.removeItem('umihealthTenantName');
        sessionStorage.removeItem('umihealthTenantId');
        sessionStorage.removeItem('umihealthUserId');
    }

    // Redirect to login page
    redirectToLogin() {
        const currentPath = window.location.pathname;
        const loginUrl = '../auth/signin.html';
        
        // Store the intended destination for redirect after login
        sessionStorage.setItem('umihealth_redirect_after_login', currentPath);
        
        window.location.href = loginUrl;
    }

    // Redirect to appropriate dashboard based on role
    redirectToDashboard(role, redirectPath = null) {
        const rolePaths = {
            'SuperAdmin': '../Super-Admin/home.html',
            'Operations': '../Sales-Operations/home.html',
            'TenantAdmin': '../Tenant-Admin/home.html',
            'Pharmacist': '../Pharmacist/home.html',
            'Cashier': '../Cashier/home.html'
        };
        
        // Store user role for dashboard verification
        if (role) {
            localStorage.setItem('umihealthUserRole', role);
            sessionStorage.setItem('umihealthUserRole', role);
        }
        
        // Use provided redirect path or role-based default
        const targetPath = redirectPath || rolePaths[role];
        
        if (targetPath) {
            window.location.href = targetPath;
        } else {
            // Fallback to tenant admin if role not recognized
            window.location.href = '../Tenant-Admin/home.html';
        }
    }

    // Enhanced login method with role-based redirection
    async login(email, password, remember = true) {
        try {
            this.loading = true;
            this.error = '';
            this.success = '';

            const response = await fetch(`${this.apiBase}/api/auth/login`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({ email, password, remember })
            });

            if (!response.ok) {
                const errorData = await response.json().catch(() => ({}));
                throw new Error(errorData.message || 'Login failed');
            }

            const data = await response.json();
            
            // Store tokens
            this.storeTokens(data.accessToken, data.refreshToken, remember);
            
            // Store user session with role information
            const userSession = {
                userId: data.user.userId,
                email: data.user.email,
                name: data.user.name,
                role: data.user.role,
                tenantId: data.user.tenantId,
                tenantName: data.user.tenantName,
                permissions: data.user.permissions || [],
                isImpersonated: data.isImpersonated || false,
                originalUser: data.originalUser || null
            };
            
            const sessionKey = remember ? localStorage : sessionStorage;
            sessionKey.setItem(this.sessionKey, JSON.stringify(userSession));
            
            // Store additional user info in localStorage for easy access
            localStorage.setItem('umihealthUserId', data.user.userId);
            localStorage.setItem('umihealthEmail', data.user.email);
            localStorage.setItem('umihealthUserName', data.user.name);
            localStorage.setItem('umihealthTenantId', data.user.tenantId);
            localStorage.setItem('umihealthTenantName', data.user.tenantName);
            
            // Redirect to appropriate dashboard
            this.redirectToDashboard(data.user.role);
            
            return data;
        } catch (error) {
            console.error('Login error:', error);
            this.error = error.message || 'Login failed. Please try again.';
            throw error;
        } finally {
            this.loading = false;
        }
    }

    // Check if current user can access specific role-based page
    canAccessPage(requiredRole) {
        const userRole = this.getUserRole();
        const userLevel = this.getUserRoleLevel();
        const requiredLevel = this.getRoleLevel(requiredRole);
        
        // Super Admin can access everything
        if (userRole === 'SuperAdmin') {
            return true;
        }
        
        // Check if user has sufficient level
        return userLevel >= requiredLevel;
    }

    // Redirect to login page
    redirectToLogin() {
        const currentPath = window.location.pathname;
        const loginUrl = '../auth/signin.html';
        
        // Store intended destination for redirect after login
        sessionStorage.setItem('umihealth_redirect_after_login', currentPath);
        
        window.location.href = loginUrl;
    }

    // Validate user access to current page
    validatePageAccess(requiredRole = null, requiredPermissions = []) {
        if (!this.isAuthenticated()) {
            this.redirectToLogin();
            return false;
        }

        const userRole = this.getUserRole();
        
        // Check role requirement
        if (requiredRole && userRole !== requiredRole) {
            console.error(`Access denied. Required role: ${requiredRole}, User role: ${userRole}`);
            this.redirectToUnauthorized();
            return false;
        }

        // Check permissions requirement
        if (requiredPermissions.length > 0 && !this.hasAllPermissions(requiredPermissions)) {
            console.error('Access denied. Insufficient permissions.');
            this.redirectToUnauthorized();
            return false;
        }

        return true;
    }

    // Redirect to unauthorized page
    redirectToUnauthorized() {
        window.location.href = '../auth/unauthorized.html';
    }

    // Initialize authentication for page
    initPageAuth(requiredRole = null, requiredPermissions = []) {
        // Check if we should redirect after login
        const redirectAfter = sessionStorage.getItem('umihealth_redirect_after_login');
        if (redirectAfter && window.location.pathname.includes(redirectAfter)) {
            sessionStorage.removeItem('umihealth_redirect_after_login');
        }

        // Validate access
        return this.validatePageAccess(requiredRole, requiredPermissions);
    }

    // Get user info for display
    getUserInfo() {
        const session = this.getSession();
        return {
            name: session?.name || sessionStorage.getItem('userName') || 'User',
            email: session?.email || sessionStorage.getItem('userEmail') || '',
            role: this.getUserRole(),
            tenantName: session?.tenantName || sessionStorage.getItem('umihealthTenantName') || '',
            plan: session?.plan || localStorage.getItem('umihealthPlan') || 'starter'
        };
    }
}

// Global auth instance
window.umiAuth = new UmiAuth();

// Auto-initialize when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    // Add global logout handler
    window.umiLogout = () => {
        if (confirm('Are you sure you want to logout?')) {
            window.umiAuth.logout();
        }
    };
});

// Export for module usage
if (typeof module !== 'undefined' && module.exports) {
    module.exports = UmiAuth;
}
