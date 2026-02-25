import axios from 'axios';

// Create axios instance
const axiosInstance = axios.create({
    baseURL: '/api',
    headers: {
        'Content-Type': 'application/json',
    },
});

// Request interceptor - Add token to requests
axiosInstance.interceptors.request.use(
    (config) => {
        const token = localStorage.getItem('accessToken');
        if (token) {
            config.headers.Authorization = `Bearer ${token}`;
        }
        return config;
    },
    (error) => {
        return Promise.reject(error);
    }
);

// Response interceptor - Handle 401 and check response.data.success
axiosInstance.interceptors.response.use(
    (response) => {
        // Check if response.data.success is false
        if (response.data && response.data.success === false) {
            throw new Error(response.data.message || 'Request failed');
        }
        return response;
    },
    (error) => {
        // Handle 401 Unauthorized
        if (error.response && error.response.status === 401) {
            localStorage.removeItem('accessToken');
            window.location.href = '/login';
        }

        // If there's a response with success: false, throw the message
        if (error.response && error.response.data && error.response.data.success === false) {
            throw new Error(error.response.data.message || 'Request failed');
        }

        return Promise.reject(error);
    }
);

export default axiosInstance;
