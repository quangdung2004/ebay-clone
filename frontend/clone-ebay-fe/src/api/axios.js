import axios from 'axios';

// ---------- Custom API Error ----------
export class ApiError extends Error {
    constructor(message, code, correlationId, status) {
        super(message);
        this.name = 'ApiError';
        this.code = code || null;
        this.correlationId = correlationId || null;
        this.status = status || null;
    }
}

// ---------- Axios Instance ----------
// In dev, Vite proxy forwards /api/* to the backend, so baseURL is just '/api'.
// In production, set VITE_API_BASE_URL to the full backend URL.
const API_BASE = import.meta.env.VITE_API_BASE_URL
    ? `${import.meta.env.VITE_API_BASE_URL}/api`
    : '/api';

const axiosInstance = axios.create({
    baseURL: API_BASE,
    headers: { 'Content-Type': 'application/json' },
    withCredentials: true, // send HttpOnly cookies cross-origin
});

// ---------- Refresh state ----------
let isRefreshing = false;
let failedQueue = [];

const processQueue = (error, token = null) => {
    failedQueue.forEach(({ resolve, reject }) => {
        if (error) reject(error);
        else resolve(token);
    });
    failedQueue = [];
};

/**
 * Routes that should NEVER trigger a token-refresh attempt.
 * If these return 401 it means the credentials are wrong,
 * not that a token expired.
 */
const AUTH_ROUTES = ['/auth/login', '/auth/register', '/auth/forgot-password', '/auth/reset-password', '/auth/verify-email', '/auth/logout', '/auth/refresh'];

function isAuthRoute(url) {
    if (!url) return false;
    return AUTH_ROUTES.some((r) => url.includes(r));
}

/**
 * Extract a user-friendly error message from any backend response body.
 * Supports:
 *  - Custom wrapper:    { success: false, message, code, correlationId }
 *  - ASP.NET ProblemDetails: { title, detail, status, traceId }
 *  - Validation errors:     { errors: { field: [...] } }
 *  - Simple object:         { error: "..." } or { message: "..." }
 */
function extractBackendError(data, httpStatus) {
    if (!data || typeof data !== 'object') {
        return { message: null, code: null, correlationId: null };
    }

    // --- Custom API wrapper (success: false) ---
    if (data.success === false) {
        return {
            message: data.message || null,
            code: data.code || null,
            correlationId: data.correlationId || null,
        };
    }

    // --- ASP.NET ProblemDetails ---
    if (data.title || data.detail) {
        let message = data.detail || data.title;
        // Attach validation errors if present
        if (data.errors && typeof data.errors === 'object') {
            const fieldErrors = Object.entries(data.errors)
                .map(([field, msgs]) => {
                    const list = Array.isArray(msgs) ? msgs.join(', ') : String(msgs);
                    return `${field}: ${list}`;
                })
                .join('; ');
            if (fieldErrors) {
                message = message ? `${message} — ${fieldErrors}` : fieldErrors;
            }
        }
        return {
            message,
            code: data.type || null,
            correlationId: data.traceId || null,
        };
    }

    // --- Simple { message } or { error } ---
    const message = data.message || data.error || null;
    return {
        message,
        code: data.code || null,
        correlationId: data.correlationId || data.traceId || null,
    };
}

// ---------- Request interceptor ----------
axiosInstance.interceptors.request.use(
    (config) => {
        const token = localStorage.getItem('accessToken');
        if (token) {
            config.headers.Authorization = `Bearer ${token}`;
        }
        return config;
    },
    (error) => Promise.reject(error),
);

// ---------- Response interceptor ----------
axiosInstance.interceptors.response.use(
    (response) => {
        // Backend may return 200 with success: false
        if (response.data && response.data.success === false) {
            const info = extractBackendError(response.data, response.status);
            throw new ApiError(
                info.message || 'Request failed',
                info.code,
                info.correlationId,
                response.status,
            );
        }
        return response;
    },
    async (error) => {
        const originalRequest = error.config;
        const status = error.response?.status;

        // --- 401 handling ---
        if (status === 401) {
            // ★ Auth routes (login, register, etc.) should NEVER attempt refresh.
            //   A 401 here means wrong credentials — surface the error directly.
            if (isAuthRoute(originalRequest?.url)) {
                const info = extractBackendError(error.response?.data, status);
                throw new ApiError(
                    info.message || 'Invalid email or password',
                    info.code,
                    info.correlationId,
                    status,
                );
            }

            // For non-auth routes: attempt token refresh (once)
            if (!originalRequest._retry) {
                if (isRefreshing) {
                    return new Promise((resolve, reject) => {
                        failedQueue.push({ resolve, reject });
                    }).then((token) => {
                        originalRequest.headers.Authorization = `Bearer ${token}`;
                        return axiosInstance(originalRequest);
                    });
                }

                originalRequest._retry = true;
                isRefreshing = true;

                try {
                    const refreshRes = await axios.post(
                        `${API_BASE}/auth/refresh`,
                        {},
                        { withCredentials: true },
                    );

                    const newToken =
                        refreshRes.data?.data?.accessToken || refreshRes.data?.accessToken;

                    if (newToken) {
                        localStorage.setItem('accessToken', newToken);
                        axiosInstance.defaults.headers.common.Authorization = `Bearer ${newToken}`;
                        processQueue(null, newToken);
                        originalRequest.headers.Authorization = `Bearer ${newToken}`;
                        return axiosInstance(originalRequest);
                    }

                    throw new Error('Refresh failed — no new token');
                } catch (refreshError) {
                    processQueue(refreshError, null);
                    localStorage.removeItem('accessToken');
                    // Redirect only if we're NOT already on the login page
                    if (!window.location.pathname.startsWith('/login')) {
                        window.location.href = '/login';
                    }
                    return Promise.reject(refreshError);
                } finally {
                    isRefreshing = false;
                }
            }
        }

        // --- Extract backend error payload for all other status codes ---
        if (error.response?.data) {
            const info = extractBackendError(error.response.data, status);
            if (info.message) {
                throw new ApiError(
                    info.message,
                    info.code,
                    info.correlationId,
                    status,
                );
            }
        }

        // --- Network / timeout / unknown errors ---
        throw new ApiError(
            error.message || 'Network error',
            null,
            null,
            status || null,
        );
    },
);

export default axiosInstance;
