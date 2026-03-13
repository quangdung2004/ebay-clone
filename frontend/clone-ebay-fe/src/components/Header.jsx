import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { logout as logoutApi } from '../api/authApi';
import { useToast } from '../components/Toast';
import './Header.css';

const Header = () => {
    const { isAuthenticated, user, logoutUser } = useAuth();
    const navigate = useNavigate();
    const { showError } = useToast();

    const handleLogout = async () => {
        try {
            await logoutApi();
        } catch (error) {
            console.error('Logout error:', error);
            showError(error.message || 'Logout failed, clearing local session.');
        } finally {
            logoutUser();
            navigate('/login');
        }
    };

    return (
        <header className="header">
            <div className="header-container">
                <Link to="/" className="header-logo">
                    CloneEbay
                </Link>

                <nav className="header-nav">
                    {isAuthenticated ? (
                        <>
                            <Link to="/sell/my-products" className="header-link">
                                My Products
                            </Link>
                            <Link to="/sell" className="header-link sell-link">
                                + Sell
                            </Link>
                            <Link to="/stores/me" className="header-link">
                                My Store
                            </Link>
                            {user && (
                                <span className="header-user">
                                    {user.username || user.email}
                                </span>
                            )}
                            <Link to="/profile" className="header-link">
                                Profile
                            </Link>
                            <button onClick={handleLogout} className="header-button">
                                Logout
                            </button>
                        </>
                    ) : (
                        <>
                            <Link to="/login" className="header-link">
                                Login
                            </Link>
                            <Link to="/register" className="header-link register-link">
                                Register
                            </Link>
                        </>
                    )}
                </nav>
            </div>
        </header>
    );
};

export default Header;
