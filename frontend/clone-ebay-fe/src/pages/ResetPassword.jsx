import { useState } from 'react';
import { useSearchParams, Link } from 'react-router-dom';
import { resetPassword as resetPasswordApi } from '../api/authApi';
import { parseApiError } from '../utils/errorUtils';
import { validateResetPassword, hasErrors } from '../utils/formValidation';
import { useToast } from '../components/Toast';
import ErrorAlert from '../components/ui/ErrorAlert';
import FormFieldError from '../components/ui/FormFieldError';
import './Login.css';

const ResetPassword = () => {
    const [searchParams] = useSearchParams();
    const token = searchParams.get('token');

    const [newPassword, setNewPassword] = useState('');
    const [confirmPassword, setConfirmPassword] = useState('');
    const [loading, setLoading] = useState(false);
    const [fieldErrors, setFieldErrors] = useState({});
    const [apiError, setApiError] = useState(null);
    const [success, setSuccess] = useState(false);
    const { showSuccess } = useToast();

    const values = { newPassword, confirmPassword };

    const clearFieldError = (field) => {
        if (fieldErrors[field]) {
            setFieldErrors((prev) => ({ ...prev, [field]: '' }));
        }
    };

    const handleBlur = (field) => {
        const errors = validateResetPassword(values);
        setFieldErrors((prev) => ({ ...prev, [field]: errors[field] || '' }));
    };

    const handleSubmit = async (e) => {
        e.preventDefault();
        setApiError(null);

        if (!token) {
            setApiError({ message: 'Invalid or missing reset token' });
            return;
        }

        const errors = validateResetPassword(values);
        setFieldErrors(errors);
        if (hasErrors(errors)) {
            const first = Object.keys(errors).find((k) => errors[k]);
            if (first) document.getElementById(first)?.focus();
            return;
        }

        setLoading(true);
        try {
            const response = await resetPasswordApi(token, newPassword);
            if (response.success) {
                setSuccess(true);
                showSuccess('Password has been reset successfully!');
            } else {
                setApiError({ message: response.message || 'Reset failed' });
            }
        } catch (err) {
            setApiError(parseApiError(err, 'Failed to reset password. The link may have expired.'));
        } finally {
            setLoading(false);
        }
    };

    if (!token) {
        return (
            <div className="auth-container">
                <div className="auth-card">
                    <h1 className="auth-title">Invalid Link</h1>
                    <ErrorAlert error={{ message: 'This password reset link is invalid or has expired.' }} />
                    <div className="auth-footer">
                        <Link to="/forgot-password" className="auth-link">Request a new link</Link>
                    </div>
                </div>
            </div>
        );
    }

    return (
        <div className="auth-container">
            <div className="auth-card">
                <h1 className="auth-title">Reset Password</h1>
                <p className="auth-subtitle">Enter your new password below.</p>

                {success ? (
                    <div className="verify-info">
                        <div className="verify-icon verify-icon-success">✓</div>
                        <p className="auth-subtitle success-text">
                            Your password has been reset successfully!
                        </p>
                        <Link
                            to="/login"
                            className="submit-button"
                            style={{ textDecoration: 'none', textAlign: 'center', display: 'block', marginTop: '1rem' }}
                        >
                            Go to Login
                        </Link>
                    </div>
                ) : (
                    <form onSubmit={handleSubmit} className="auth-form" noValidate>
                        <ErrorAlert error={apiError} />

                        <div className="form-group">
                            <label htmlFor="newPassword" className="form-label">New Password</label>
                            <input
                                type="password"
                                id="newPassword"
                                value={newPassword}
                                onChange={(e) => { setNewPassword(e.target.value); clearFieldError('newPassword'); }}
                                onBlur={() => handleBlur('newPassword')}
                                className={`form-input${fieldErrors.newPassword ? ' form-input--error' : ''}`}
                                placeholder="••••••••"
                                disabled={loading}
                            />
                            <FormFieldError error={fieldErrors.newPassword} />
                        </div>

                        <div className="form-group">
                            <label htmlFor="confirmPassword" className="form-label">Confirm New Password</label>
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
                            {loading ? 'Resetting…' : 'Reset Password'}
                        </button>
                    </form>
                )}

                <div className="auth-footer">
                    <p><Link to="/login" className="auth-link">Back to Login</Link></p>
                </div>
            </div>
        </div>
    );
};

export default ResetPassword;
