import { createContext, useState, useEffect, useContext } from 'react';
import { getMe } from '../api/authApi';

const AuthContext = createContext();

export const useAuth = () => {
    const context = useContext(AuthContext);
    if (!context) {
        throw new Error('useAuth must be used within an AuthProvider');
    }
    return context;
};

export const AuthProvider = ({ children }) => {
    const [user, setUser] = useState(null);
    const [loading, setLoading] = useState(true);

    // Check if user is logged in on mount
    useEffect(() => {
        const initAuth = async () => {
            const token = localStorage.getItem('accessToken');
            if (token) {
                try {
                    const response = await getMe();
                    if (response.success && response.data) {
                        setUser(response.data);
                    } else {
                        localStorage.removeItem('accessToken');
                    }
                } catch (error) {
                    console.error('Failed to fetch user:', error);
                    localStorage.removeItem('accessToken');
                }
            }
            setLoading(false);
        };

        initAuth();
    }, []);

    const loginUser = (userData, token) => {
        localStorage.setItem('accessToken', token);
        setUser(userData);
    };

    const logoutUser = () => {
        localStorage.removeItem('accessToken');
        setUser(null);
    };

    const value = {
        user,
        loading,
        loginUser,
        logoutUser,
        isAuthenticated: !!user,
    };

    return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};
