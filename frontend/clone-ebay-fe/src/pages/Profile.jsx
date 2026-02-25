import { useState, useEffect } from 'react';
import { getMe } from '../api/authApi';
import './Profile.css';

const Profile = () => {
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState('');

    const [userData, setUserData] = useState({
        username: '',
        email: '',
        role: '',
        avatarURL: '',
    });

    useEffect(() => {
        fetchUserData();
    }, []);

    const fetchUserData = async () => {
        try {
            const response = await getMe();
            if (response.success && response.data) {
                setUserData(response.data);
            }
        } catch (err) {
            setError(err.message || 'Failed to load profile data');
        } finally {
            setLoading(false);
        }
    };

    if (loading) {
        return (
            <div className="profile-container">
                <div className="profile-card">
                    <p>Loading profile...</p>
                </div>
            </div>
        );
    }

    return (
        <div className="profile-container">
            <div className="profile-card">
                <h1 className="profile-title">My Profile</h1>

                {error && (
                    <div className="error-message">
                        {error}
                    </div>
                )}

                <div className="profile-avatar-section">
                    {userData.avatarURL ? (
                        <img
                            src={userData.avatarURL}
                            alt="Avatar"
                            className="profile-avatar"
                            onError={(e) => {
                                e.target.style.display = 'none';
                            }}
                        />
                    ) : (
                        <div className="profile-avatar-placeholder">
                            {userData.username?.charAt(0).toUpperCase() || 'U'}
                        </div>
                    )}
                </div>

                <div className="profile-info">
                    <div className="info-row">
                        <span className="info-label">Username:</span>
                        <span className="info-value">{userData.username}</span>
                    </div>

                    <div className="info-row">
                        <span className="info-label">Email:</span>
                        <span className="info-value">{userData.email}</span>
                    </div>

                    <div className="info-row">
                        <span className="info-label">Role:</span>
                        <span className="info-value role-badge">{userData.role}</span>
                    </div>

                    {userData.avatarURL && (
                        <div className="info-row">
                            <span className="info-label">Avatar URL:</span>
                            <span className="info-value url-value">{userData.avatarURL}</span>
                        </div>
                    )}
                </div>
            </div>
        </div>
    );
};

export default Profile;
