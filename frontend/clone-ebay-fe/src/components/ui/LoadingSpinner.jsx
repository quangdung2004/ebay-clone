import './LoadingSpinner.css';

/**
 * Reusable loading spinner.
 *
 * Props:
 *   size     – 'sm' | 'md' | 'lg' (default 'md')
 *   message  – optional text below the spinner
 *   overlay  – if true wraps in a centered overlay container
 */
const LoadingSpinner = ({ size = 'md', message, overlay = false }) => {
    const spinner = (
        <div className={`loading-spinner loading-spinner--${size}`}>
            <div className="loading-spinner__circle" />
            {message && <p className="loading-spinner__text">{message}</p>}
        </div>
    );

    if (overlay) {
        return <div className="loading-spinner__overlay">{spinner}</div>;
    }

    return spinner;
};

export default LoadingSpinner;
