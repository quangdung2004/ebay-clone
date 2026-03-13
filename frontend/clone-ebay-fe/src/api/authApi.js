import axiosInstance from './axios';

/**
 * Register new user.
 * NOTE: The interceptor already throws ApiError for success:false,
 * so when this returns, the request was successful.
 * We return the full wrapper { success, data, message, code } for auth APIs
 * because some pages inspect `.success` and `.data.accessToken` separately.
 */
export const register = async (username, email, password) => {
    const response = await axiosInstance.post('/auth/register', {
        username,
        email,
        password,
    });
    return response.data; // { success, data, message, code }
};

/**
 * Login user (with rememberMe support).
 * Returns the full wrapper so Login page can read .data.accessToken.
 */
export const login = async (email, password, rememberMe = false) => {
    const response = await axiosInstance.post('/auth/login', {
        email,
        password,
        rememberMe,
    });
    return response.data;
};

/**
 * Get current user info.
 * Returns the full wrapper so AuthContext can check .success and .data.
 */
export const getMe = async () => {
    const response = await axiosInstance.get('/auth/me');
    return response.data;
};

/** Logout user */
export const logout = async () => {
    const response = await axiosInstance.post('/auth/logout');
    return response.data;
};

/** Verify email by token */
export const verifyEmail = async (token) => {
    const response = await axiosInstance.post('/auth/verify-email', { token });
    return response.data;
};

/** Forgot password – request reset link */
export const forgotPassword = async (email) => {
    const response = await axiosInstance.post('/auth/forgot-password', { email });
    return response.data;
};

/** Reset password with token */
export const resetPassword = async (token, newPassword) => {
    const response = await axiosInstance.post('/auth/reset-password', {
        token,
        newPassword,
    });
    return response.data;
};
