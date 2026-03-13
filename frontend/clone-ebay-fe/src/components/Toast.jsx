import { createContext, useContext, useState, useCallback, useRef } from 'react';
import './Toast.css';

const ToastContext = createContext();

export const useToast = () => {
    const context = useContext(ToastContext);
    if (!context) {
        throw new Error('useToast must be used within a ToastProvider');
    }
    return context;
};

let toastId = 0;

export const ToastProvider = ({ children }) => {
    const [toasts, setToasts] = useState([]);
    const timersRef = useRef({});

    const removeToast = useCallback((id) => {
        clearTimeout(timersRef.current[id]);
        delete timersRef.current[id];
        setToasts((prev) => prev.filter((t) => t.id !== id));
    }, []);

    const addToast = useCallback(
        (message, type = 'error', duration = 6000) => {
            const id = ++toastId;
            setToasts((prev) => [...prev, { id, message, type }]);
            timersRef.current[id] = setTimeout(() => removeToast(id), duration);
            return id;
        },
        [removeToast],
    );

    const showError = useCallback(
        (msg, details) => {
            const text = details ? `${msg} [${details}]` : msg;
            return addToast(text, 'error');
        },
        [addToast],
    );

    const showSuccess = useCallback(
        (msg) => addToast(msg, 'success', 4000),
        [addToast],
    );

    const showInfo = useCallback(
        (msg) => addToast(msg, 'info', 4000),
        [addToast],
    );

    return (
        <ToastContext.Provider value={{ addToast, showError, showSuccess, showInfo, removeToast }}>
            {children}
            <div className="toast-container" aria-live="polite">
                {toasts.map((toast) => (
                    <div
                        key={toast.id}
                        className={`toast toast-${toast.type}`}
                        role="alert"
                    >
                        <span className="toast-icon">
                            {toast.type === 'error' && '✕'}
                            {toast.type === 'success' && '✓'}
                            {toast.type === 'info' && 'ℹ'}
                        </span>
                        <span className="toast-message">{toast.message}</span>
                        <button
                            className="toast-close"
                            onClick={() => removeToast(toast.id)}
                            aria-label="Dismiss"
                        >
                            ×
                        </button>
                    </div>
                ))}
            </div>
        </ToastContext.Provider>
    );
};
