import './ErrorAlert.css';

/**
 * Reusable error alert box for forms and pages.
 *
 * Props:
 *   error       – string OR { message, code, correlationId }
 *   onRetry     – optional callback, shows a Retry button
 *   className   – optional extra classes
 */
const ErrorAlert = ({ error, onRetry, className = '' }) => {
    if (!error) return null;

    const isObj = typeof error === 'object';
    const message = isObj ? error.message : error;
    const code = isObj ? error.code : null;
    const correlationId = isObj ? error.correlationId : null;

    return (
        <div className={`error-alert ${className}`} role="alert">
            <div className="error-alert__icon">!</div>
            <div className="error-alert__body">
                <p className="error-alert__message">{message}</p>
            </div>
            {onRetry && (
                <button className="error-alert__retry" onClick={onRetry} type="button">
                    Retry
                </button>
            )}
        </div>
    );
};

export default ErrorAlert;
