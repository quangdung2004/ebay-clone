import { useState, useEffect, useCallback } from 'react';
import { getMe } from '../api/authApi';
import { parseApiError } from '../utils/errorUtils';
import LoadingSpinner from '../components/ui/LoadingSpinner';
import ErrorAlert from '../components/ui/ErrorAlert';
import './Profile.css';

const Profile = () => {
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);

    const [userData, setUserData] = useState({
        username: '',
        email: '',
        role: '',
        avatarURL: '',
    });

    const fetchUserData = useCallback(async () => {
        setLoading(true);
        setError(null);
        try {
            const response = await getMe();
            if (response.success && response.data) {
                setUserData(response.data);
            }
        } catch (err) {
            const parsed = parseApiError(err, 'Failed to load profile data');
            setError(parsed);
        } finally {
            setLoading(false);
        }
    }, []);

    useEffect(() => {
        fetchUserData();
    }, [fetchUserData]);

    if (loading) {
        return (
            <div className="profile-container">
                <div className="profile-card">
                    <LoadingSpinner size="lg" message="Loading profile…" overlay />
                </div>
            </div>
        );
    }

    return (
        <div className="profile-container">
            <div className="profile-card">
                <h1 className="profile-title">My Profile</h1>

                {error && (
                    <ErrorAlert error={error} onRetry={fetchUserData} />
                )}

                {!error && (
                    <>
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
                    </>
                )}
            </div>
        </div>
    );
};

export default Profile;
