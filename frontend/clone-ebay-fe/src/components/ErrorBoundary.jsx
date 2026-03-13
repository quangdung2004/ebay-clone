import { Component } from 'react';

class ErrorBoundary extends Component {
    constructor(props) {
        super(props);
        this.state = { hasError: false, error: null };
    }

    static getDerivedStateFromError(error) {
        return { hasError: true, error };
    }

    componentDidCatch(error, errorInfo) {
        console.error('ErrorBoundary caught:', error, errorInfo);
    }

    handleReset = () => {
        this.setState({ hasError: false, error: null });
    };

    render() {
        if (this.state.hasError) {
            return (
                <div style={styles.container}>
                    <div style={styles.card}>
                        <h2 style={styles.title}>Something went wrong</h2>
                        <p style={styles.message}>
                            {this.state.error?.message || 'An unexpected error occurred.'}
                        </p>
                        {import.meta.env.DEV && this.state.error?.stack && (
                            <pre style={styles.stack}>{this.state.error.stack}</pre>
                        )}
                        <button style={styles.button} onClick={this.handleReset}>
                            Try Again
                        </button>
                    </div>
                </div>
            );
        }

        return this.props.children;
    }
}

const styles = {
    container: {
        minHeight: '100vh',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
        padding: '2rem',
    },
    card: {
        backgroundColor: 'white',
        borderRadius: '0.75rem',
        padding: '2.5rem',
        maxWidth: '520px',
        width: '100%',
        boxShadow: '0 20px 25px -5px rgba(0,0,0,0.1)',
        textAlign: 'center',
    },
    title: {
        fontSize: '1.5rem',
        fontWeight: 700,
        color: '#dc2626',
        marginBottom: '1rem',
    },
    message: {
        color: '#374151',
        marginBottom: '1.5rem',
        lineHeight: 1.6,
    },
    stack: {
        fontSize: '0.75rem',
        color: '#6b7280',
        backgroundColor: '#f9fafb',
        padding: '1rem',
        borderRadius: '0.5rem',
        textAlign: 'left',
        overflow: 'auto',
        maxHeight: '200px',
        marginBottom: '1.5rem',
    },
    button: {
        padding: '0.75rem 2rem',
        background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
        color: 'white',
        border: 'none',
        borderRadius: '0.5rem',
        fontSize: '1rem',
        fontWeight: 600,
        cursor: 'pointer',
    },
};

export default ErrorBoundary;
