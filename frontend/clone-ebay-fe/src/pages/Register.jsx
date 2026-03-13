import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { register as registerApi } from '../api/authApi';
import { parseApiError } from '../utils/errorUtils';
import { validateRegister, hasErrors } from '../utils/formValidation';
import { useToast } from '../components/Toast';
import ErrorAlert from '../components/ui/ErrorAlert';
import FormFieldError from '../components/ui/FormFieldError';
import './Login.css';

const Register = () => {
    const [username, setUsername] = useState('');
    const [email, setEmail] = useState('');
    const [password, setPassword] = useState('');
    const [confirmPassword, setConfirmPassword] = useState('');
    const [fieldErrors, setFieldErrors] = useState({});
    const [apiError, setApiError] = useState(null);
    const [loading, setLoading] = useState(false);

    const navigate = useNavigate();
    const { showSuccess } = useToast();

    const values = { username, email, password, confirmPassword };

    const clearFieldError = (field) => {
        if (fieldErrors[field]) {
            setFieldErrors((prev) => ({ ...prev, [field]: '' }));
        }
    };

    const handleBlur = (field) => {
        const errors = validateRegister(values);
        setFieldErrors((prev) => ({ ...prev, [field]: errors[field] || '' }));
    };

    const handleSubmit = async (e) => {
        e.preventDefault();
        setApiError(null);

        const errors = validateRegister(values);
        setFieldErrors(errors);
        if (hasErrors(errors)) {
            const first = Object.keys(errors).find((k) => errors[k]);
            if (first) document.getElementById(first)?.focus();
            return;
        }

        setLoading(true);
        try {
            const response = await registerApi(username.trim(), email.trim(), password);

            if (response.success) {
                showSuccess('Registration successful! Please verify your email.');
                navigate(`/verify-email?email=${encodeURIComponent(email.trim())}`);
            } else {
                setApiError({ message: response.message || 'Registration failed' });
            }
        } catch (err) {
            setApiError(parseApiError(err, 'An error occurred during registration'));
        } finally {
            setLoading(false);
        }
    };

    return (
        <div className="auth-container">
            <div className="auth-card">
                <h1 className="auth-title">Create Account</h1>
                <p className="auth-subtitle">Join CloneEbay today</p>

                <form onSubmit={handleSubmit} className="auth-form" noValidate>
                    <ErrorAlert error={apiError} />

                    <div className="form-group">
                        <label htmlFor="username" className="form-label">Username</label>
                        <input
                            type="text"
                            id="username"
                            value={username}
                            onChange={(e) => { setUsername(e.target.value); clearFieldError('username'); }}
                            onBlur={() => handleBlur('username')}
                            className={`form-input${fieldErrors.username ? ' form-input--error' : ''}`}
                            placeholder="johndoe"
                            disabled={loading}
                        />
                        <FormFieldError error={fieldErrors.username} />
                    </div>

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

                    <div className="form-group">
                        <label htmlFor="confirmPassword" className="form-label">Confirm Password</label>
                        <input
                            type="password"
                            id="confirmPassword"
                            value={confirmPassword}
                            onChange={(e) => { setConfirmPassword(e.target.value); clearFieldError('confirmPassword'); }}
                            onBlur={() => handleBlur('confirmPassword')}
                            className={`form-input${fieldErrors.confirmPassword ? ' form-input--error' : ''}`}
                            placeholder="••••••••"
                            disabled={loading}
                        />
                        <FormFieldError error={fieldErrors.confirmPassword} />
                    </div>

                    <button type="submit" className="submit-button" disabled={loading}>
                        {loading ? 'Creating account…' : 'Create Account'}
                    </button>
                </form>

                <div className="auth-footer">
                    <p>
                        Already have an account?{' '}
                        <Link to="/login" className="auth-link">Sign in</Link>
                    </p>
                </div>
            </div>
        </div>
    );
};

export default Register;
