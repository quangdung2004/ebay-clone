import { useState, useEffect, useCallback } from 'react';
import { getMyStore, updateMyStore } from '../api/storeApi';
import { parseApiError } from '../utils/errorUtils';
import { useToast } from '../components/Toast';
import StoreForm from '../components/store/StoreForm';
import StoreEmptyState from '../components/store/StoreEmptyState';
import LoadingSpinner from '../components/ui/LoadingSpinner';
import ErrorAlert from '../components/ui/ErrorAlert';
import './MyStorePage.css';

const MyStorePage = () => {
    const { showSuccess } = useToast();

    const [store, setStore] = useState(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const [updateSuccess, setUpdateSuccess] = useState(false);

    const fetchStore = useCallback(async () => {
        setLoading(true);
        setError(null);
        try {
            const data = await getMyStore();
            setStore(data);
        } catch (err) {
            if (err.code === 'STORE_NOT_FOUND' || err.status === 404) {
                setStore(null);
            } else {
                const parsed = parseApiError(err, 'Failed to load store');
                setError(parsed);
            }
        } finally {
            setLoading(false);
        }
    }, []);

    useEffect(() => {
        fetchStore();
    }, [fetchStore]);

    const handleUpdate = async (payload) => {
        // StoreForm handles try/catch/loading internally
        const updated = await updateMyStore(payload);
        setStore(updated);
        setUpdateSuccess(true);
        showSuccess('Store updated successfully!');
        // Hide success banner after 3 seconds
        setTimeout(() => setUpdateSuccess(false), 3000);
    };

    if (loading) {
        return (
            <div className="my-store-page">
                <div className="msp-card">
                    <LoadingSpinner size="lg" message="Loading your store…" overlay />
                </div>
            </div>
        );
    }

    if (error) {
        return (
            <div className="my-store-page">
                <div className="msp-card">
                    <div style={{ padding: '2rem' }}>
                        <ErrorAlert error={error} onRetry={fetchStore} />
                    </div>
                </div>
            </div>
        );
    }

    if (!store) {
        return (
            <div className="my-store-page">
                <div className="msp-card">
                    <StoreEmptyState
                        title="No Store Yet"
                        message="You haven't created a store yet. Create one to start selling."
                    />
                </div>
            </div>
        );
    }

    return (
        <div className="my-store-page">
            <div className="msp-card">
                <h1 className="msp-title">My Store</h1>
                <p className="msp-subtitle">Manage your store information</p>

                {updateSuccess && (
                    <div className="msp-success">
                        ✅ Store updated successfully!
                    </div>
                )}

                <div className="msp-info">
                    <div className="msp-info-row">
                        <span className="msp-info-label">Store ID:</span>
                        <span className="msp-info-value">{store.id || store.storeId || '—'}</span>
                    </div>
                    <div className="msp-info-row">
                        <span className="msp-info-label">Created:</span>
                        <span className="msp-info-value">
                            {store.createdAt
                                ? new Date(store.createdAt).toLocaleDateString()
                                : '—'}
                        </span>
                    </div>
                </div>

                <h2 className="msp-section-title">Edit Store</h2>
                <StoreForm
                    initialData={{
                        storeName: store.storeName,
                        description: store.description,
                        bannerImageURL: store.bannerImageURL,
                    }}
                    onSubmit={handleUpdate}
                    submitLabel="Update Store"
                />
            </div>
        </div>
    );
};

export default MyStorePage;
