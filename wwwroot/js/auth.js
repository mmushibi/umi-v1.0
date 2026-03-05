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
        // In development, point to backend API server
        if (window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1') {
            return 'http://127.0.0.1:8080';
        }
        // In production, use configured API base from environment or fallback to same origin
        return window.UMI_CONFIG?.API_BASE_URL || window.location.origin;
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

    // Enhanced login method with role-based redirection and UI permissions loading
    async login(email, password, remember = true) {
        try {
            this.loading = true;
            this.error = '';
            this.success = '';

            console.log('Attempting login to:', `${this.apiBase}/api/auth/login`);
            console.log('Login payload:', { email, password, remember });

            const response = await fetch(`${this.apiBase}/api/auth/login`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({ email, password, remember })
            });

            console.log('Login response status:', response.status);
            console.log('Login response headers:', response.headers);

            if (!response.ok) {
                const errorData = await response.json().catch(() => ({}));
                console.error('Login error response:', errorData);
                throw new Error(errorData.message || 'Login failed');
            }

            const data = await response.json();
            console.log('Login success data:', data);
            
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
            
            // Load UI permissions from server
            try {
                await this.loadUIPermissions();
            } catch (error) {
                console.warn('Failed to load UI permissions:', error);
            }
            
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

    // Load UI permissions from server
    async loadUIPermissions() {
        try {
            const response = await this.apiRequest(`${this.apiBase}/api/uipermissions/current`);
            if (response.ok) {
                const uiPermissions = await response.json();
                
                // Store UI permissions in sessionStorage for quick access
                sessionStorage.setItem('umihealth_ui_permissions', JSON.stringify(uiPermissions));
                
                console.log('UI permissions loaded:', uiPermissions);
                return uiPermissions;
            }
        } catch (error) {
            console.error('Failed to load UI permissions:', error);
            throw error;
        }
    }

    // Get cached UI permissions
    getUIPermissions() {
        const permissions = sessionStorage.getItem('umihealth_ui_permissions');
        return permissions ? JSON.parse(permissions) : null;
    }

    // Check if user can access specific UI element
    canAccessUIElement(elementId) {
        const uiPermissions = this.getUIPermissions();
        if (!uiPermissions || !uiPermissions.UIElements) {
            return false; // Default to deny if permissions not loaded
        }
        return uiPermissions.UIElements[elementId] === true;
    }

    // Check if user can perform specific action
    canPerformAction(action) {
        const uiPermissions = this.getUIPermissions();
        if (!uiPermissions || !uiPermissions.Actions) {
            return false; // Default to deny if permissions not loaded
        }
        return uiPermissions.Actions[action] === true;
    }

    // Get navigation items for current user
    getNavigationItems() {
        const uiPermissions = this.getUIPermissions();
        return uiPermissions?.Navigation || [];
    }

    // Get dashboard widgets for current user
    getDashboardWidgets() {
        const uiPermissions = this.getUIPermissions();
        return uiPermissions?.DashboardWidgets || [];
    }

    // Validate UI element access with server fallback
    async validateUIElementAccess(elementId) {
        try {
            // Check local permissions first
            if (this.canAccessUIElement(elementId)) {
                return true;
            }

            // Fallback to server validation
            const response = await this.apiRequest(`${this.apiBase}/api/uipermissions/validate-element`, {
                method: 'POST',
                body: JSON.stringify({ elementId })
            });

            if (response.ok) {
                const result = await response.json();
                return result.HasAccess;
            }

            return false;
        } catch (error) {
            console.error('Error validating UI element access:', error);
            return false;
        }
    }

    // Validate action access with server fallback
    async validateActionAccess(action, permission) {
        try {
            // Check local permissions first
            if (this.canPerformAction(action)) {
                return true;
            }

            // Fallback to server validation
            const response = await this.apiRequest(`${this.apiBase}/api/uipermissions/validate-action`, {
                method: 'POST',
                body: JSON.stringify({ action, permission })
            });

            if (response.ok) {
                const result = await response.json();
                return result.HasAccess;
            }

            return false;
        } catch (error) {
            console.error('Error validating action access:', error);
            return false;
        }
    }

    // Enhanced page access validation with UI permissions and advanced security
    validatePageAccess(requiredRole = null, requiredPermissions = []) {
        // Check authentication
        if (!this.isAuthenticated()) {
            this.redirectToLogin(window.location.href);
            return false;
        }

        // Check role-based access
        if (requiredRole && !this.canAccessPage(requiredRole)) {
            this.redirectToUnauthorized();
            return false;
        }

        // Check permission-based access
        if (requiredPermissions.length > 0 && !this.hasAllPermissions(requiredPermissions)) {
            this.redirectToUnauthorized();
            return false;
        }

        // Check UI permissions for current page
        const currentPage = window.location.pathname.split('/').pop();
        if (currentPage && !this.canAccessUIElement(currentPage.replace('.html', '_page'))) {
            this.redirectToUnauthorized();
            return false;
        }

        // Enhanced security: Validate UI access with server
        this.validateUIElementAccess(currentPage.replace('.html', '_page')).then(hasAccess => {
            if (!hasAccess) {
                this.redirectToUnauthorized();
                return false;
            }
        }).catch(error => {
            console.error('Error validating UI access:', error);
            // Continue with local validation if server validation fails
        });

        return true;
    }

    // Enhanced UI element validation with caching and fallback
    async validateUIElementAccessWithCache(elementId) {
        try {
            const cacheKey = `ui_access_${elementId}`;
            const cached = sessionStorage.getItem(cacheKey);
            
            if (cached) {
                const { timestamp, result } = JSON.parse(cached);
                const age = Date.now() - timestamp;
                
                // Use cached result if less than 5 minutes old
                if (age < 300000) {
                    return result;
                }
            }

            // Validate with server
            const response = await this.apiRequest(`${this.apiBase}/api/compliance/validate-ui-access`, {
                method: 'POST',
                body: JSON.stringify({ elementId, action: 'view' })
            });

            if (response.ok) {
                const result = await response.json();
                
                // Cache the result
                sessionStorage.setItem(cacheKey, JSON.stringify({
                    timestamp: Date.now(),
                    result: result.data.hasAccess
                }));
                
                return result.data.hasAccess;
            }

            return false;
        } catch (error) {
            console.error('Error validating UI element access:', error);
            return false;
        }
    }

    // Enhanced action validation with security context
    async validateActionAccessWithSecurity(action, permission) {
        try {
            // Check local permissions first
            if (this.canPerformAction(action)) {
                return true;
            }

            // Get security context for enhanced validation
            const securityContext = await this.getSecurityContext();
            
            // Time-based validation
            if (!this.isWithinAllowedHours(securityContext.role, action)) {
                console.warn(`Action ${action} not allowed outside business hours`);
                return false;
            }

            // Validate with server
            const response = await this.apiRequest(`${this.apiBase}/api/compliance/validate-ui-access`, {
                method: 'POST',
                body: JSON.stringify({ elementId: action, action, permission })
            });

            if (response.ok) {
                const result = await response.json();
                return result.data.hasAccess;
            }

            return false;
        } catch (error) {
            console.error('Error validating action access:', error);
            return false;
        }
    }

    // Get security context for current user
    async getSecurityContext() {
        const session = this.getSession();
        return {
            userId: session?.userId,
            role: session?.role,
            tenantId: session?.tenantId,
            permissions: session?.permissions || []
        };
    }

    // Check if action is allowed within business hours
    isWithinAllowedHours(role, action) {
        const restrictedActions = ['user_management', 'system_settings', 'financial_reports'];
        const restrictedRoles = ['Pharmacist', 'Cashier'];
        
        if (!restrictedActions.includes(action) || !restrictedRoles.includes(role)) {
            return true;
        }

        const now = new Date();
        const hours = now.getHours();
        const isBusinessHours = hours >= 8 && hours <= 18;
        
        return isBusinessHours;
    }

    // Enhanced security: Anti-CSRF token validation
    async validateAntiForgeryToken(token) {
        try {
            const userId = this.getUserId();
            const response = await this.apiRequest(`${this.apiBase}/api/compliance/validate-csrf`, {
                method: 'POST',
                body: JSON.stringify({ userId, token })
            });

            if (response.ok) {
                const result = await response.json();
                return result.data.isValid;
            }

            return false;
        } catch (error) {
            console.error('Error validating CSRF token:', error);
            return false;
        }
    }

    // Generate CSRF token for forms
    generateCSRFToken() {
        const array = new Uint8Array(32);
        crypto.getRandomValues(array);
        return Array.from(array, byte => byte.toString(16).padStart(2, '0')).join('');
    }

    // Enhanced security headers for API requests
    async apiRequest(url, options = {}) {
        const defaultOptions = {
            headers: {
                'Content-Type': 'application/json',
                'X-Requested-With': 'XMLHttpRequest',
                'X-CSRF-Token': this.generateCSRFToken()
            }
        };

        const finalOptions = { ...defaultOptions, ...options };

        // Add security headers
        if (this.isAuthenticated()) {
            finalOptions.headers.Authorization = `Bearer ${this.getAccessToken()}`;
        }

        try {
            const response = await fetch(url, finalOptions);
            
            // Check for security headers
            const sessionExpired = response.headers.get('X-Session-Expired');
            const timeoutWarning = response.headers.get('X-Session-Timeout-Warning');
            const remainingMinutes = response.headers.get('X-Session-Remaining-Minutes');

            if (sessionExpired === 'true') {
                alert('Your session has expired. Please log in again.');
                this.logout();
                return null;
            }

            if (timeoutWarning === 'true' && remainingMinutes && parseInt(remainingMinutes) <= 5) {
                alert(`Your session will expire in ${remainingMinutes} minutes. Please save your work.`);
            }

            return response;
        } catch (error) {
            console.error('API request failed:', error);
            throw error;
        }
    }

    // Handle session timeout warnings
    handleSessionTimeout() {
        // Check for session timeout headers
        const sessionExpired = document.querySelector('meta[name="session-expired"]')?.content;
        const timeoutWarning = document.querySelector('meta[name="session-warning"]')?.content;

        if (sessionExpired === 'true') {
            alert('Your session has expired. Please log in again.');
            this.logout();
            return;
        }

        if (timeoutWarning === 'true') {
            const remainingMinutes = document.querySelector('meta[name="session-remaining-minutes"]')?.content;
            if (remainingMinutes && parseInt(remainingMinutes) <= 5) {
                alert(`Your session will expire in ${remainingMinutes} minutes. Please save your work.`);
            }
        }
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
