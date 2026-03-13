import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { login as loginApi, getMe } from '../api/authApi';
import { parseApiError } from '../utils/errorUtils';
import { validateLogin, hasErrors } from '../utils/formValidation';
import { useToast } from '../components/Toast';
import ErrorAlert from '../components/ui/ErrorAlert';
import FormFieldError from '../components/ui/FormFieldError';
import './Login.css';

const Login = () => {
    const [email, setEmail] = useState('');
    const [password, setPassword] = useState('');
    const [rememberMe, setRememberMe] = useState(false);
    const [fieldErrors, setFieldErrors] = useState({});
    const [apiError, setApiError] = useState(null);
    const [loading, setLoading] = useState(false);

    const { loginUser } = useAuth();
    const navigate = useNavigate();
    const { showSuccess } = useToast();

    const clearFieldError = (field) => {
        if (fieldErrors[field]) {
            setFieldErrors((prev) => ({ ...prev, [field]: '' }));
        }
    };

    const handleBlur = (field) => {
        const errors = validateLogin({ email, password });
        setFieldErrors((prev) => ({ ...prev, [field]: errors[field] || '' }));
    };

    const handleSubmit = async (e) => {
        e.preventDefault();
        setApiError(null);

        const errors = validateLogin({ email, password });
        setFieldErrors(errors);
        if (hasErrors(errors)) {
            const first = Object.keys(errors).find((k) => errors[k]);
            if (first) document.getElementById(first)?.focus();
            return;
        }

        setLoading(true);
        try {
            const response = await loginApi(email.trim(), password, rememberMe);

            if (response.success && response.data) {
                const { accessToken } = response.data;
                localStorage.setItem('accessToken', accessToken);

                try {
                    const meResponse = await getMe();
                    if (meResponse.success && meResponse.data) {
                        loginUser(meResponse.data, accessToken);
                    } else {
                        const { accessToken: _t, ...user } = response.data;
                        loginUser(user, accessToken);
                    }
                } catch {
                    const { accessToken: _t, ...user } = response.data;
                    loginUser(user, accessToken);
                }

                showSuccess('Logged in successfully!');
                navigate('/');
            } else {
                setApiError({ message: response.message || 'Login failed' });
            }
        } catch (err) {
            setApiError(parseApiError(err, 'An error occurred during login'));
        } finally {
            setLoading(false);
        }
    };

    return (
        <div className="auth-container">
            <div className="auth-card">
                <h1 className="auth-title">Welcome Back</h1>
                <p className="auth-subtitle">Sign in to your account</p>

                <form onSubmit={handleSubmit} className="auth-form" noValidate>
                    <ErrorAlert error={apiError} />

                    <div className="form-group">
                        <label htmlFor="email" className="form-label">Email</label>
                        <input
                            type="email"
                            id="email"
                            value={email}
                            onChange={(e) => { setEmail(e.target.value); clearFieldError('email'); }}
                            onBlur={() => handleBlur('email')}
                            className={`form-input${fieldErrors.email ? ' form-input--error' : ''}`}
                            placeholder="you@example.com"
                            disabled={loading}
                        />
                        <FormFieldError error={fieldErrors.email} />
                    </div>

                    <div className="form-group">
                        <label htmlFor="password" className="form-label">Password</label>
                        <input
                            type="password"
                            id="password"
                            value={password}
                            onChange={(e) => { setPassword(e.target.value); clearFieldError('password'); }}
                            onBlur={() => handleBlur('password')}
                            className={`form-input${fieldErrors.password ? ' form-input--error' : ''}`}
                            placeholder="••••••••"
                            disabled={loading}
                        />
                        <FormFieldError error={fieldErrors.password} />
                    </div>

                    <div className="form-row">
                        <label className="checkbox-label" htmlFor="rememberMe">
                            <input
                                type="checkbox"
                                id="rememberMe"
                                checked={rememberMe}
                                onChange={(e) => setRememberMe(e.target.checked)}
                                disabled={loading}
                            />
                            <span>Remember me</span>
                        </label>
                        <Link to="/forgot-password" className="auth-link">
                            Forgot password?
                        </Link>
                    </div>

                    <button type="submit" className="submit-button" disabled={loading}>
                        {loading ? 'Signing in…' : 'Sign In'}
                    </button>
                </form>

                <div className="auth-footer">
                    <p>
                        Don't have an account?{' '}
                        <Link to="/register" className="auth-link">Create one</Link>
                    </p>
                </div>
            </div>
        </div>
    );
};

export default Login;
