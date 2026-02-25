import axiosInstance from './axios';

// Register new user
export const register = async (username, email, password) => {
    const response = await axiosInstance.post('/auth/register', {
        username,
        email,
        password,
    });
    return response.data;
};

// Login user
export const login = async (email, password) => {
    const response = await axiosInstance.post('/auth/login', {
        email,
        password,
    });
    return response.data;
};

// Get current user info
export const getMe = async () => {
    const response = await axiosInstance.get('/auth/me');
    return response.data;
};

// Logout user
export const logout = async () => {
    const response = await axiosInstance.post('/auth/logout');
    return response.data;
};
