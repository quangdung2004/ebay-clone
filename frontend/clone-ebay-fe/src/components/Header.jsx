import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { logout as logoutApi } from '../api/authApi';
import './Header.css';

const Header = () => {
    const { isAuthenticated, logoutUser } = useAuth();
    const navigate = useNavigate();

    const handleLogout = async () => {
        try {
            await logoutApi();
        } catch (error) {
            console.error('Logout error:', error);
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
