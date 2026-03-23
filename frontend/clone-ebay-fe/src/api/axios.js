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
// Dev: Vite proxy sẽ forward /api sang backend local
// Production: Nginx trên VPS sẽ forward /api sang backend containers
// => frontend chỉ cần gọi relative path /api
const API_BASE = import.meta.env.VITE_API_URL || '/api';

const axiosInstance = axios.create({
    baseURL: API_BASE,
    headers: { 'Content-Type': 'application/json' },
    withCredentials: true,
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
const AUTH_ROUTES = [
    '/auth/login',
    '/auth/register',
    '/auth/forgot-password',
    '/auth/reset-password',
    '/auth/verify-email',
    '/auth/logout',
    '/auth/refresh',
];

function isAuthRoute(url) {
    if (!url) return false;
    return AUTH_ROUTES.some((r) => url.includes(r));
}

/**
 * Extract a user-friendly error message from any backend response body.
 */
function extractBackendError(data, httpStatus) {
    if (!data || typeof data !== 'object') {
        return { message: null, code: null, correlationId: null };
    }

    if (data.success === false) {
        return {
            message: data.message || null,
            code: data.code || null,
            correlationId: data.correlationId || null,
        };
    }

    if (data.title || data.detail) {
        let message = data.detail || data.title;

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

        if (status === 401) {
            if (isAuthRoute(originalRequest?.url)) {
                const info = extractBackendError(error.response?.data, status);
                throw new ApiError(
                    info.message || 'Invalid email or password',
                    info.code,
                    info.correlationId,
                    status,
                );
            }

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

                    if (!window.location.pathname.startsWith('/login')) {
                        window.location.href = '/login';
                    }

                    return Promise.reject(refreshError);
                } finally {
                    isRefreshing = false;
                }
            }
        }

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

        throw new ApiError(
            error.message || 'Network error',
            null,
            null,
            status || null,
        );
    },
);

export default axiosInstance;