import { useState } from 'react';
import { Link } from 'react-router-dom';
import { forgotPassword as forgotPasswordApi } from '../api/authApi';
import { parseApiError } from '../utils/errorUtils';
import { validateForgotPassword, hasErrors } from '../utils/formValidation';
import ErrorAlert from '../components/ui/ErrorAlert';
import FormFieldError from '../components/ui/FormFieldError';
import './Login.css';

const ForgotPassword = () => {
    const [email, setEmail] = useState('');
    const [loading, setLoading] = useState(false);
    const [submitted, setSubmitted] = useState(false);
    const [fieldErrors, setFieldErrors] = useState({});
    const [apiError, setApiError] = useState(null);

    const clearFieldError = (field) => {
        if (fieldErrors[field]) {
            setFieldErrors((prev) => ({ ...prev, [field]: '' }));
        }
    };

    const handleBlur = (field) => {
        const errors = validateForgotPassword({ email });
        setFieldErrors((prev) => ({ ...prev, [field]: errors[field] || '' }));
    };

    const handleSubmit = async (e) => {
        e.preventDefault();
        setApiError(null);

        const errors = validateForgotPassword({ email });
        setFieldErrors(errors);
        if (hasErrors(errors)) {
            document.getElementById('forgot-email')?.focus();
            return;
        }

        setLoading(true);
        try {
            await forgotPasswordApi(email.trim());
            setSubmitted(true);
        } catch (err) {
            setApiError(parseApiError(err, 'Something went wrong. Please try again.'));
        } finally {
            setLoading(false);
        }
    };

    return (
        <div className="auth-container">
            <div className="auth-card">
                <h1 className="auth-title">Forgot Password</h1>
                <p className="auth-subtitle">
                    Enter your email and we'll send you a reset link.
                </p>

                {submitted ? (
                    <div className="verify-info">
                        <div className="verify-icon verify-icon-success">✓</div>
                        <p className="auth-subtitle success-text">
                            If the email exists, a password reset link has been sent. Please
                            check your inbox.
                        </p>
                        <Link
                            to="/login"
                            className="submit-button"
                            style={{ textDecoration: 'none', textAlign: 'center', display: 'block', marginTop: '1rem' }}
                        >
                            Back to Login
                        </Link>
                    </div>
                ) : (
                    <form onSubmit={handleSubmit} className="auth-form" noValidate>
                        <ErrorAlert error={apiError} />

                        <div className="form-group">
                            <label htmlFor="forgot-email" className="form-label">Email</label>
                            <input
                                type="email"
                                id="forgot-email"
                                value={email}
                                onChange={(e) => { setEmail(e.target.value); clearFieldError('email'); }}
                                onBlur={() => handleBlur('email')}
                                className={`form-input${fieldErrors.email ? ' form-input--error' : ''}`}
                                placeholder="you@example.com"
                                disabled={loading}
                            />
                            <FormFieldError error={fieldErrors.email} />
                        </div>

                        <button type="submit" className="submit-button" disabled={loading}>
                            {loading ? 'Sending…' : 'Send Reset Link'}
                        </button>
                    </form>
                )}

                <div className="auth-footer">
                    <p>
                        Remember your password?{' '}
                        <Link to="/login" className="auth-link">Sign in</Link>
                    </p>
                </div>
            </div>
        </div>
    );
};

export default ForgotPassword;
