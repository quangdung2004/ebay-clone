import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { getMyStore } from '../api/storeApi';
import { parseApiError } from '../utils/errorUtils';
import StoreEmptyState from '../components/store/StoreEmptyState';
import LoadingSpinner from '../components/ui/LoadingSpinner';
import ErrorAlert from '../components/ui/ErrorAlert';
import './SellEntryPage.css';

const SellEntryPage = () => {
    const navigate = useNavigate();
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);

    const checkStore = async () => {
        setLoading(true);
        setError(null);
        try {
            const store = await getMyStore();
            if (store) {
                // User already has a store → go to create product
                navigate('/sell/new', { replace: true });
            }
        } catch (err) {
            // STORE_NOT_FOUND means user has no store – show CTA
            if (err.code === 'STORE_NOT_FOUND' || err.status === 404) {
                setLoading(false);
                return;
            }
            // Any other error
            const parsed = parseApiError(err, 'Failed to check store status');
            setError(parsed);
            setLoading(false);
        }
    };

    useEffect(() => {
        let cancelled = false;
        const run = async () => {
            await checkStore();
        };
        if (!cancelled) run();
        return () => { cancelled = true; };
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);

    if (loading && !error) {
        return (
            <div className="sell-entry-page">
                <div className="sell-entry-card">
                    <LoadingSpinner size="md" message="Checking your store…" overlay />
                </div>
            </div>
        );
    }

    if (error) {
        return (
            <div className="sell-entry-page">
                <div className="sell-entry-card">
                    <div style={{ padding: '2rem' }}>
                        <ErrorAlert error={error} onRetry={checkStore} />
                    </div>
                </div>
            </div>
        );
    }

    return (
        <div className="sell-entry-page">
            <div className="sell-entry-card">
                <StoreEmptyState />
            </div>
        </div>
    );
};

export default SellEntryPage;
