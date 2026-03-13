import { useState, useEffect } from 'react';
import { useSearchParams, Link } from 'react-router-dom';
import { verifyEmail as verifyEmailApi } from '../api/authApi';
import { parseApiError } from '../utils/errorUtils';
import LoadingSpinner from '../components/ui/LoadingSpinner';
import './Login.css';

const VerifyEmail = () => {
    const [searchParams] = useSearchParams();
    const token = searchParams.get('token');
    const email = searchParams.get('email');

    const [status, setStatus] = useState('idle'); // idle | loading | success | error
    const [message, setMessage] = useState('');
    const [errorDetails, setErrorDetails] = useState(null);

    useEffect(() => {
        if (token) {
            handleVerify();
        }
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [token]);

    const handleVerify = async () => {
        setStatus('loading');
        try {
            const response = await verifyEmailApi(token);
            if (response.success) {
                setStatus('success');
                setMessage('Your email has been verified successfully!');
            } else {
                setStatus('error');
                setMessage(response.message || 'Verification failed.');
            }
        } catch (err) {
            setStatus('error');
            const parsed = parseApiError(err, 'Verification failed. The link may have expired.');
            setMessage(parsed.message);
            setErrorDetails(parsed);
        }
    };

    return (
        <div className="auth-container">
            <div className="auth-card">
                <h1 className="auth-title">Email Verification</h1>

                {!token && (
                    <div className="verify-info">
                        <div className="verify-icon">📧</div>
                        <p className="auth-subtitle">
                            We've sent a verification link to{' '}
                            {email ? <strong>{email}</strong> : 'your email address'}.
                        </p>
                        <p className="verify-hint">
                            Please check your inbox (and spam folder) and click the
                            verification link to activate your account.
                        </p>
                    </div>
                )}

                {status === 'loading' && (
                    <div className="verify-info">
                        <LoadingSpinner size="md" message="Verifying your email…" />
                    </div>
                )}

                {status === 'success' && (
                    <div className="verify-info">
                        <div className="verify-icon verify-icon-success">✓</div>
                        <p className="auth-subtitle success-text">{message}</p>
                        <Link to="/login" className="submit-button" style={{ textDecoration: 'none', textAlign: 'center', display: 'block', marginTop: '1rem' }}>
                            Go to Login
                        </Link>
                    </div>
                )}

                {status === 'error' && (
                    <div className="verify-info">
                        <div className="verify-icon verify-icon-error">✕</div>
                        <p className="error-message">{message}</p>
                        {errorDetails?.code && (
                            <p style={{ fontSize: '0.8rem', color: '#9ca3af', marginTop: '0.25rem' }}>
                                Code: {errorDetails.code}
                            </p>
                        )}
                        {errorDetails?.correlationId && (
                            <p style={{ fontSize: '0.75rem', color: '#9ca3af' }}>
                                Ref: {errorDetails.correlationId}
                            </p>
                        )}
                        <button
                            onClick={handleVerify}
                            className="submit-button"
                            style={{ marginTop: '1rem' }}
                        >
                            Retry
                        </button>
                        <Link to="/login" className="auth-link" style={{ marginTop: '0.75rem', display: 'inline-block' }}>
                            Back to Login
                        </Link>
                    </div>
                )}
            </div>
        </div>
    );
};

export default VerifyEmail;
